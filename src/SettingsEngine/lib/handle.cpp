//-------------------------------------------------------------------------------------------------
// <copyright file="handle.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions related to interacting with settings engine handles
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

const DWORD STALE_WRITETIME_RETRY = 100;

HRESULT HandleLock(
    __inout CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&pcdb->cs);
    ++pcdb->dwLockRefCount;

    if (1 < pcdb->dwLockRefCount)
    {
        ExitFunction1(hr = S_OK);
    }

    // This should only be set to TRUE if the database was successfully completely synced with local upon unlock
    pcdb->fUpdateLastModified = FALSE;

    // Connect to database, if it's a remote database
    if (pcdb->fRemote)
    {
        hr = SceEnsureDatabase(pcdb->sczDbPath, wzSqlCeDllPath, L"CfgRemote", 1, &pcdb->dsSceDb, &pcdb->psceDb);
        ExitOnFailure1(hr, "Failed to ensure SQL CE database at %ls exists", pcdb->sczDbPath);

        // If the remote wasn't up when we initialized, we couldn't get cfg app id or GUID, so get it now
        if (DWORD_MAX == pcdb->dwCfgAppID)
        {
            hr = HandleEnsureSummaryDataTable(pcdb);
            ExitOnFailure(hr, "Failed to ensure remote database summary data");

            hr = ProductSet(pcdb, wzCfgProductId, wzCfgVersion, wzCfgPublicKey, TRUE, NULL);
            ExitOnFailure(hr, "Failed to set product to cfg product id");
            pcdb->dwCfgAppID = pcdb->dwAppID;
        }
    }

LExit:
    if (FAILED(hr))
    {
        --pcdb->dwLockRefCount;
        ::LeaveCriticalSection(&pcdb->cs);
    }

    return hr;
}

void HandleUnlock(
    __inout CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;
    FILETIME ftOriginalLastModified = { };
    FILETIME ftNewLastModified = { };
    LONG lCompareResult = 0;

    if (1 < pcdb->dwLockRefCount)
    {
        ExitFunction1(hr = S_OK);
    }

    Assert(0 < pcdb->dwLockRefCount);

    // Disconnect from database, if it's a connected remote database
    if (pcdb->fRemote && NULL != pcdb->psceDb)
    {
        if (SceDatabaseChanged(pcdb->psceDb))
        {
            hr = FileGetTime(pcdb->sczDbChangesPath, NULL, NULL, &ftOriginalLastModified);
            ExitOnFailure1(hr, "Failed to get file time of remote db changes path: %ls", pcdb->sczDbChangesPath);

            // Check if our updated timestamp will be recognized by filesystem as new. If not, keep trying again and sleeping until it is.
            // Last written timestamp granularity can vary by filesystem
            do
            {
                hr = FileWrite(pcdb->sczDbChangesPath, FILE_ATTRIBUTE_HIDDEN, NULL, 0, NULL);
                ExitOnFailure1(hr, "Failed to write new db changes file: %ls", pcdb->sczDbChangesPath);

                hr = FileGetTime(pcdb->sczDbChangesPath, NULL, NULL, &ftNewLastModified);
                ExitOnFailure1(hr, "Failed to re-get file time of remote db changes path: %ls", pcdb->sczDbChangesPath);

                lCompareResult = ::CompareFileTime(&ftOriginalLastModified, &ftNewLastModified);

                if (0 == lCompareResult)
                {
                    ::Sleep(STALE_WRITETIME_RETRY);
                }
            } while (0 == lCompareResult);
        }

        if (pcdb->fUpdateLastModified)
        {
            hr = FileGetTime(pcdb->sczDbChangesPath, NULL, NULL, &pcdb->ftLastModified);
            if (E_FILENOTFOUND == hr || E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure1(hr, "Failed to get file time of remote db: %ls", pcdb->sczDbPath);
        }

        hr = SceCloseDatabase(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to close remote database");
        pcdb->psceDb = NULL;
    }

    pcdb->fUpdateLastModified = FALSE;

LExit:
    --pcdb->dwLockRefCount;
    ::LeaveCriticalSection(&pcdb->cs);

    return;
}

HRESULT HandleEnsureSummaryDataTable(
    __in CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    RPC_STATUS rs = RPC_S_OK;
    BOOL fEmpty = FALSE;
    UUID guid = { };
    const DWORD_PTR cchGuid = 39;
    SCE_ROW_HANDLE sceRow = NULL;

    hr = SceGetFirstRow(pcdb->psceDb, SUMMARY_DATA_TABLE, &sceRow);
    if (E_NOTFOUND == hr)
    {
        fEmpty = TRUE;
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get first row of summary data table");

    if (fEmpty)
    {
        hr = StrAlloc(&pcdb->sczGuid, cchGuid);
        ExitOnFailure(hr, "Failed to allocate space for guid");

        // Create the unique endpoint name.
        rs = ::UuidCreate(&guid);
        hr = HRESULT_FROM_RPC(rs);
        ExitOnFailure(hr, "Failed to create endpoint guid.");

        if (!::StringFromGUID2(guid, pcdb->sczGuid, cchGuid))
        {
            hr = E_OUTOFMEMORY;
            ExitOnRootFailure(hr, "Failed to convert endpoint guid into string.");
        }

        hr = SceBeginTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to begin transaction");
        fInSceTransaction = TRUE;

        hr = ScePrepareInsert(pcdb->psceDb, SUMMARY_DATA_TABLE, &sceRow);
        ExitOnFailure(hr, "Failed to prepare for insert");

        hr = SceSetColumnString(sceRow, SUMMARY_GUID, pcdb->sczGuid);
        ExitOnFailure(hr, "Failed to set column string of summary data table guid");

        hr = SceFinishUpdate(sceRow);
        ExitOnFailure(hr, "Failed to finish insert into summary data table");

        hr = SceCommitTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to commit transaction");
        fInSceTransaction = FALSE;

        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to move to first row in SummaryData table");

    hr = SceGetColumnString(sceRow, SUMMARY_GUID, &pcdb->sczGuid);
    ExitOnFailure(hr, "Failed to get GUID from summary data table");

LExit:
    ReleaseSceRow(sceRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    if (FAILED(hr))
    {
        ReleaseNullStr(pcdb->sczGuid);
    }

    return hr;
}
