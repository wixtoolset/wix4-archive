// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

extern "C" HRESULT DatabaseListInsert(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzFriendlyName,
    __in BOOL fSyncByDefault,
    __in LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    SCE_ROW_HANDLE sceRow = NULL;

    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction");
    fInSceTransaction = TRUE;

    hr = ScePrepareInsert(pcdb->psceDb, DATABASE_INDEX_TABLE, &sceRow);
    ExitOnFailure(hr, "Failed to prepare for insert");

    hr = SceSetColumnString(sceRow, DATABASE_INDEX_FRIENDLY_NAME, wzFriendlyName);
    ExitOnFailure(hr, "Failed to set friendly name column");

    hr = SceSetColumnBool(sceRow, DATABASE_INDEX_SYNC_BY_DEFAULT, fSyncByDefault);
    ExitOnFailure(hr, "Failed to set 'sync by default' column");

    if (fSyncByDefault)
    {
        hr = BackgroundAddRemote(pcdb, wzPath);
        ExitOnFailure(hr, "Failed to add remote path to background thread for automatic synchronization: %ls", wzPath);
    }

    hr = SceSetColumnString(sceRow, DATABASE_INDEX_PATH, wzPath);
    ExitOnFailure(hr, "Failed to set path column");

    hr = SceFinishUpdate(sceRow);
    ExitOnFailure(hr, "Failed to finish insert");

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    fInSceTransaction = FALSE;

LExit:
    ReleaseSceRow(sceRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }

    return hr;
}

HRESULT DatabaseListFind(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzFriendlyName,
    __out SCE_ROW_HANDLE *pSceRow
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;

    hr = SceBeginQuery(pcdb->psceDb, DATABASE_INDEX_TABLE, 1, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into database index table");

    hr = SceSetQueryColumnString(sqhHandle, wzFriendlyName);
    ExitOnFailure(hr, "Failed to set query column name string to: %ls", wzFriendlyName);

    hr = SceRunQueryExact(&sqhHandle, pSceRow);
    if (E_NOTFOUND == hr)
    {
        // Don't pollute our log with unnecessary messages
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to query for database '%ls' in database list", wzFriendlyName);

LExit:
    ReleaseSceQuery(sqhHandle);

    return hr;
}

extern "C" HRESULT DatabaseListDelete(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzFriendlyName
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRow = NULL;

    hr = DatabaseListFind(pcdb, wzFriendlyName, &sceRow);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to search for database '%ls' in database list", wzFriendlyName);

    hr = SceDeleteRow(&sceRow);
    ExitOnFailure(hr, "Failed to delete database '%ls' from database list", wzFriendlyName);

LExit:
    ReleaseSceRow(sceRow);
    ReleaseSceQuery(sqhHandle);

    return hr;
}
