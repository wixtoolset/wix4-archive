// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT RemoteDatabaseInitialize(
    __in LPCWSTR wzPath,
    __in BOOL fCreate,
    __in BOOL fSyncByDefault,
    __in BOOL fKnown, // If the database is known, we tolerate the database not being reachable right now due to network being down, etc. MonUtil will tell us when is available
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle
    );
static BOOL FindPathInOpenDatabasesList(
    __in CFGDB_STRUCT *pcdbLocal,
    __in_z LPCWSTR wzPath,
    __out DWORD *pdwIndex
    );

extern "C" HRESULT CFGAPI CfgCreateRemoteDatabase(
    __in LPCWSTR wzPath,
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(wzPath, hr, E_INVALIDARG, "Remote path must not be NULL");
    ExitOnNull(pcdHandle, hr, E_INVALIDARG, "Handle output pointer must not be NULL");

    if (FileExistsEx(wzPath, NULL))
    {
        hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
        ExitOnFailure(hr, "Tried to create remote database %ls, but it already exists!", wzPath);
    }

    hr = RemoteDatabaseInitialize(wzPath, TRUE, FALSE, FALSE, pcdHandle);
    ExitOnFailure(hr, "Failed to create remote database at path: %ls", wzPath);

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgOpenRemoteDatabase(
    __in LPCWSTR wzPath,
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(wzPath, hr, E_INVALIDARG, "Remote path must not be NULL");
    ExitOnNull(pcdHandle, hr, E_INVALIDARG, "Handle output pointer must not be NULL");

    if (!FileExistsEx(wzPath, NULL))
    {
        hr = E_NOTFOUND;
        ExitOnFailure(hr, "Tried to open remote database %ls, but it doesn't exist!", wzPath);
    }

    hr = RemoteDatabaseInitialize(wzPath, FALSE, FALSE, FALSE, pcdHandle);
    ExitOnFailure(hr, "Failed to open remote database at path: %ls", wzPath);

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgRememberDatabase(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdLocalHandle,
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdRemoteHandle,
    __in LPCWSTR wzFriendlyName,
    __in BOOL fSyncByDefault
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    SCE_ROW_HANDLE sceRow = NULL;
    CFGDB_STRUCT *pcdbLocal = static_cast<CFGDB_STRUCT *>(cdLocalHandle);
    CFGDB_STRUCT *pcdbRemote = static_cast<CFGDB_STRUCT *>(cdRemoteHandle);

    ExitOnNull(pcdbLocal, hr, E_INVALIDARG, "Local database handle input pointer must not be NULL");
    ExitOnNull(pcdbRemote, hr, E_INVALIDARG, "Remote database handle input pointer must not be NULL");
    ExitOnNull(wzFriendlyName, hr, E_INVALIDARG, "Friendly name must not be NULL");

    hr = DatabaseListFind(pcdbLocal, wzFriendlyName, &sceRow);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to find database index row for friendly name '%ls'", wzFriendlyName);

        hr = SceBeginTransaction(pcdbLocal->psceDb);
        ExitOnFailure(hr, "Failed to begin transaction");
        fInSceTransaction = TRUE;

        hr = SceSetColumnBool(sceRow, DATABASE_INDEX_SYNC_BY_DEFAULT, fSyncByDefault);
        ExitOnFailure(hr, "Failed to update 'sync by default' column for existing database in list");

        hr = SceFinishUpdate(sceRow);
        ExitOnFailure(hr, "Failed to finish update while updating existing database in list");

        hr = SceCommitTransaction(pcdbLocal->psceDb);
        ExitOnFailure(hr, "Failed to commit transaction");
        fInSceTransaction = FALSE;

        if (pcdbRemote->fSyncByDefault && !fSyncByDefault)
        {
            pcdbRemote->fSyncByDefault = fSyncByDefault;
            hr = BackgroundRemoveRemote(pcdbLocal, pcdbRemote->sczOriginalDbPath);
            ExitOnFailure(hr, "Failed to remove remote path to background thread for automatic synchronization: %ls", pcdbRemote->sczOriginalDbPath);
        }
        else if (!pcdbRemote->fSyncByDefault && fSyncByDefault)
        {
            pcdbRemote->fSyncByDefault = fSyncByDefault;
            hr = BackgroundAddRemote(pcdbLocal, pcdbRemote->sczOriginalDbPath);
            ExitOnFailure(hr, "Failed to add remote path to background thread for automatic synchronization: %ls", pcdbRemote->sczOriginalDbPath);
        }
    }
    else
    {
        pcdbRemote->fSyncByDefault = fSyncByDefault;
        hr = DatabaseListInsert(pcdbLocal, wzFriendlyName, fSyncByDefault, pcdbRemote->sczOriginalDbPath);
        ExitOnFailure(hr, "Failed to remember database '%ls' in database list", wzFriendlyName);
    }

    pcdbRemote->fSyncByDefault = fSyncByDefault;

LExit:
    ReleaseSceRow(sceRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdbLocal->psceDb);
    }

    return hr;
}

extern "C" HRESULT CFGAPI CfgOpenKnownRemoteDatabase(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdLocalHandle,
    __in LPCWSTR wzFriendlyName,
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdbLocal = static_cast<CFGDB_STRUCT *>(cdLocalHandle);
    SCE_ROW_HANDLE sceRow = NULL;
    LPWSTR sczPath = NULL;
    BOOL fSyncByDefault = FALSE;
    BOOL fLocked = FALSE;

    ExitOnNull(pcdbLocal, hr, E_INVALIDARG, "Local database handle input pointer must not be NULL");
    ExitOnNull(wzFriendlyName, hr, E_INVALIDARG, "Friendly name must not be NULL");
    ExitOnNull(pcdHandle, hr, E_INVALIDARG, "Output database handle input pointer must not be NULL");

    hr = HandleLock(pcdbLocal);
    ExitOnFailure(hr, "Failed to lock handle while opening known remote database");
    fLocked = TRUE;

    hr = DatabaseListFind(pcdbLocal, wzFriendlyName, &sceRow);
    ExitOnFailure(hr, "Failed to find database index row for friendly name '%ls'", wzFriendlyName);

    hr = SceGetColumnString(sceRow, DATABASE_INDEX_PATH, &sczPath);
    ExitOnFailure(hr, "Failed to get path from database list for database '%ls'", wzFriendlyName);

    hr = SceGetColumnBool(sceRow, DATABASE_INDEX_SYNC_BY_DEFAULT, &fSyncByDefault);
    ExitOnFailure(hr, "Failed to get 'sync by default' column for existing database in list");

    hr = RemoteDatabaseInitialize(sczPath, FALSE, fSyncByDefault, TRUE, pcdHandle);
    ExitOnFailure(hr, "Failed to open known database '%ls' at path '%ls'", wzFriendlyName, sczPath);

LExit:
    ReleaseSceRow(sceRow);
    if (fLocked)
    {
        HandleUnlock(pcdbLocal);
    }
    ReleaseStr(sczPath);

    return hr;
}

extern "C" HRESULT CFGAPI CfgForgetDatabase(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdLocalHandle,
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdRemoteHandle,
    __in LPCWSTR wzFriendlyName
    )
{
    HRESULT hr = S_OK;
    BOOL fLocked = FALSE;
    CFGDB_STRUCT *pcdbLocal = static_cast<CFGDB_STRUCT *>(cdLocalHandle);
    CFGDB_STRUCT *pcdbRemote = static_cast<CFGDB_STRUCT *>(cdRemoteHandle);

    ExitOnNull(pcdbLocal, hr, E_INVALIDARG, "Local database handle input pointer must not be NULL");
    ExitOnNull(cdRemoteHandle, hr, E_INVALIDARG, "Remote database handle input pointer must not be NULL");
    ExitOnNull(wzFriendlyName, hr, E_INVALIDARG, "Friendly name must not be NULL");

    hr = HandleLock(pcdbLocal);
    ExitOnFailure(hr, "Failed to lock handle while forgetting database");
    fLocked = TRUE;

    if (pcdbRemote->fSyncByDefault)
    {
        hr = BackgroundRemoveRemote(pcdbLocal, pcdbRemote->sczDbPath);
        ExitOnFailure(hr, "Failed to remove remote path to background thread for automatic synchronization: %ls", pcdbRemote->sczDbPath);

        pcdbRemote->fSyncByDefault = FALSE;
    }

    hr = DatabaseListDelete(pcdbLocal, wzFriendlyName);
    ExitOnFailure(hr, "Failed to delete database '%ls' from database list", wzFriendlyName);

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdbLocal);
    }

    return hr;
}

extern "C" HRESULT CFGAPI CfgRemoteDisconnect(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    )
{
    HRESULT hr = S_OK;
    DWORD dwIndex = DWORD_MAX;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    CFGDB_STRUCT *pcdbLocal = pcdb->pcdbLocal;
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Remote database handle input pointer must not be NULL");

    LogStringLine(REPORT_STANDARD, "Disconnecting from remote database at path: %ls", pcdb->sczDbPath);

    hr = HandleLock(pcdbLocal);
    ExitOnFailure(hr, "Failed to lock handle while disconnecting from remote");
    fLocked = TRUE;

    if (pcdb->fSyncByDefault)
    {
        hr = BackgroundRemoveRemote(pcdb->pcdbLocal, pcdb->sczDbPath);
        ExitOnFailure(hr, "Failed to remove remote database from automatic sync list");
    }

    if (FindPathInOpenDatabasesList(pcdb->pcdbLocal, pcdb->sczDbPath, &dwIndex))
    {
        MemRemoveFromArray(reinterpret_cast<void *>(pcdb->pcdbLocal->rgpcdbOpenDatabases), dwIndex, 1, pcdb->pcdbLocal->cOpenDatabases, sizeof(CFGDB_STRUCT *), TRUE);
        --pcdb->pcdbLocal->cOpenDatabases;
    }

    pcdb->dwAppID = DWORD_MAX;
    pcdb->fProductSet = FALSE;
    ReleaseStr(pcdb->sczGuid);
    ReleaseStr(pcdb->sczGuidLocalInRemoteKey);
    ReleaseStr(pcdb->sczGuidRemoteInLocalKey);
    ReleaseStr(pcdb->sczOriginalDbPath);
    ReleaseStr(pcdb->sczOriginalDbDir);
    ReleaseStr(pcdb->sczDbPath);
    ReleaseStr(pcdb->sczDbCopiedPath);
    ReleaseStr(pcdb->sczDbDir);
    ReleaseStr(pcdb->sczStreamsDir);
    ReleaseNullStrArray(pcdb->rgsczStreamsToDelete, pcdb->cStreamsToDelete);

    hr = SceCloseDatabase(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to close remote database");

    DatabaseReleaseSceSchema(&pcdb->dsSceDb);

    hr = CfgUninitialize(pcdb->pcdbLocal);
    ExitOnFailure(hr, "Failed to uninitialize Cfg Db");

    ::DeleteCriticalSection(&pcdb->cs);

    ReleaseMem(pcdb);

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdbLocal);
    }

    return hr;
}

// Static functions
static HRESULT RemoteDatabaseInitialize(
    __in LPCWSTR wzPath,
    __in BOOL fCreate,
    __in BOOL fSyncByDefault,
    __in BOOL fKnown,
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle
    )
{
    HRESULT hr = S_OK;
    DWORD dwIndex = DWORD_MAX;
    CFGDB_STRUCT *pcdbLocal = NULL;
    CFGDB_STRUCT *pcdb = NULL;
    BOOL fLocked = FALSE;

    hr = CfgInitialize(reinterpret_cast<CFGDB_HANDLE *>(&pcdbLocal), NULL, NULL, NULL);
    ExitOnFailure(hr, "Failed to initialize Cfg Db");

    hr = HandleLock(pcdbLocal);
    ExitOnFailure(hr, "Failed to lock handle while initializing remote database");
    fLocked = TRUE;

    // See if we already exist in the open databases list - if we do, return existing pointer and don't grow the list
    if (FindPathInOpenDatabasesList(pcdbLocal, wzPath, &dwIndex))
    {
        *pcdHandle = static_cast<CFGDB_HANDLE>(pcdbLocal->rgpcdbOpenDatabases[dwIndex]);
        ExitFunction1(hr = S_OK);
    }

    pcdb = static_cast<CFGDB_STRUCT *>(MemAlloc(sizeof(CFGDB_STRUCT), TRUE));
    ExitOnNull(pcdb, hr, E_OUTOFMEMORY, "Failed to allocate memory for cfg db struct");

    ::InitializeCriticalSection(&pcdb->cs);
    pcdb->dwAppID = DWORD_MAX;
    pcdb->fRemote = TRUE;

    hr = StrAllocString(&pcdb->sczOriginalDbPath, wzPath, 0);
    ExitOnFailure(hr, "Failed to copy original path: %ls", wzPath);

    hr = PathGetDirectory(wzPath, &pcdb->sczOriginalDbDir);
    ExitOnFailure(hr, "Failed to get directory of original path: %ls", wzPath);

    if (wzPath[0] != L'\\' || wzPath[1] != L'\\')
    {
        hr = UncConvertFromMountedDrive(&pcdb->sczDbPath, wzPath);
        if (HRESULT_FROM_WIN32(ERROR_NOT_CONNECTED) == hr)
        {
            ReleaseNullStr(pcdb->sczDbPath);
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to convert remote path %ls to unc path", wzPath);
    }
    if (NULL == pcdb->sczDbPath)
    {
        // Likely not a mounted drive - just copy the path then
        hr = S_OK;

        hr = StrAllocString(&pcdb->sczDbPath, wzPath, 0);
        ExitOnFailure(hr, "Failed to copy path request: %ls", wzPath);
    }
    else
    {
        pcdb->fNetwork = TRUE;
    }

    if (!fKnown && !fCreate && !FileExistsEx(pcdb->sczDbPath, NULL))
    {
        // database file doesn't exist, error out
        hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        ExitOnFailure(hr, "Tried to open remote database that doesn't exist at path: %ls", pcdb->sczDbPath);
    }

    hr = PathGetDirectory(pcdb->sczDbPath, &pcdb->sczDbDir);
    ExitOnFailure(hr, "Failed to copy remote database directory");

    if (fCreate)
    {
        hr = DirEnsureExists(pcdb->sczDbDir, NULL);
        ExitOnFailure(hr, "Failed to ensure remote database directory exists after UNC conversion");
    }

    // Setup expected schema in memory
    hr = DatabaseSetupSchema(DATABASE_TYPE_REMOTE, &pcdb->dsSceDb);
    ExitOnFailure(hr, "Failed to setup user database schema structure in memory");

    // Open the database (or create if it doesn't exist)
    if (!fKnown)
    {
        hr = SceEnsureDatabase(pcdb->sczDbPath, wzSqlCeDllPath, L"CfgRemote", 1, &pcdb->dsSceDb, &pcdb->psceDb);
        ExitOnFailure(hr, "Failed to create SQL CE database");

        hr = HandleEnsureSummaryDataTable(pcdb);
        ExitOnFailure(hr, "Failed to ensure remote database summary data");

        hr = GuidListEnsure(pcdbLocal, pcdb->sczGuid, &pcdb->sczGuidRemoteInLocalKey);
        ExitOnFailure(hr, "Failed to ensure remote database is in local database's guid table");

        hr = GuidListEnsure(pcdb, pcdbLocal->sczGuid, &pcdb->sczGuidLocalInRemoteKey);
        ExitOnFailure(hr, "Failed to ensure local database is in remote database's guid table");

        hr = ProductSet(pcdb, wzCfgProductId, wzCfgVersion, wzCfgPublicKey, FALSE, NULL);
        ExitOnFailure(hr, "Failed to set product to cfg product id");
        pcdb->dwCfgAppID = pcdb->dwAppID;
    }
    else
    {
        pcdb->dwCfgAppID = DWORD_MAX;
    }

    hr = PathConcat(pcdb->sczDbDir, L"Streams", &pcdb->sczStreamsDir);
    ExitOnFailure(hr, "Failed to get path to streams directory");

    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pcdbLocal->rgpcdbOpenDatabases), pcdbLocal->cOpenDatabases + 1, sizeof(CFGDB_STRUCT *), 0);
    ExitOnFailure(hr, "Failed to ensure open database list array size");
    pcdbLocal->rgpcdbOpenDatabases[pcdbLocal->cOpenDatabases] = pcdb;
    ++pcdbLocal->cOpenDatabases;

    if (fSyncByDefault)
    {
        hr = BackgroundAddRemote(pcdbLocal, pcdb->sczOriginalDbPath);
        ExitOnFailure(hr, "Failed to add remote path to background thread for automatic synchronization: %ls", pcdb->sczDbDir);
    }
    pcdb->pcdbLocal = pcdbLocal;
    pcdb->fSyncByDefault = fSyncByDefault;

    if (!fKnown)
    {
        hr = SceCloseDatabase(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to close remote database");
        pcdb->psceDb = NULL;
    }

    *pcdHandle = pcdb;

LExit:
    if (NULL != pcdbLocal && FAILED(hr))
    {
        CfgUninitialize(pcdbLocal);
    }
    if (fLocked)
    {
        HandleUnlock(pcdbLocal);
    }

    return hr;
}

static BOOL FindPathInOpenDatabasesList(
    __in CFGDB_STRUCT *pcdbLocal,
    __in_z LPCWSTR wzPath,
    __out DWORD *pdwIndex
    )
{
    for (DWORD i = 0; i < pcdbLocal->cOpenDatabases; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pcdbLocal->rgpcdbOpenDatabases[i]->sczDbPath, -1, wzPath, -1))
        {
            *pdwIndex = i;
            return TRUE;
        }
    }

    return FALSE;
}
