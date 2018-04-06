// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const DWORD STALE_WRITETIME_RETRY = 100;


// Dropbox and similar solutions can have their own conflicts on the database file, in which they rename the database.
// this cleans them up. We always sync again in cases like that, so just delete those conflicts without requiring user intervention.
static HRESULT CleanConflictedDatabases(
    __in LPCWSTR wzDbDir,
    __in LPCWSTR wzDbPath
    );
// Deletes a stream and attempts to delete any empty parent directories
// Modifies the string in the process for perf (instead of copying the string)
static HRESULT DeleteStream(
    __in_z LPWSTR sczStreamPath
    );

// There is some fancy footwork here to handle dropbox-like scenarios well.
// Keep in mind SQL CE stupidly always modifies the database every time you open it (even if you make no changes),
// so we typically can't trust a timestamp of the database, or even a file hash of the DB to detect changes (it will give a ton of false positives, causing
// two machines to take turns infinitely syncing the file)
// We used to use timestamp of a "LAST_REAL_CHANGE" file right next to remote to detect this. However, this also cannot work with dropbox-like scenarios
// because what if that file gets downloaded first before the database? That will cause us to sync an unchanged database, and then ignore when the actual database
// gets downloaded which corresponds to the LAST_REAL_CHANGE update.
// To handle this scenario, we copy the remote db locally, sync with it, and then only if it changed, re-upload the database file in a transactional manner,
// doing our best not to overwrite a file some other remote just uploaded
// (though we cannot 100% guarantee this collision will never happen, no data loss occurs if it does happen).
// Note we don't copy locally all the streams for performance.
// Copying locally makes our LAN perf worse, and certain scenarios may have more retries because the DB file isn't locked.
// HOWEVER, this also fixes a long-standing issue with syncing over wireless LAN - previously, if you lost your wireless connection while syncing to a remote
// (and losing a wifi connection seems to be more common in heavy usage scenarios), the database file would be locked for quite a while until the lock timed out,
// and all machines would fail to sync to it until the lock expired (which took quite a long time). This issue almost completely ruined the ability to sync over
// wireless, if the wifi connection was not 100% reliable. Now we have worse network perf, but we can actually support wireless because we don't lock the file.
// Eventually we need a real cloud solution, but until we do, dropbox support is an enormously important feature for the project.

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
        // If database path is present right now, clean up conflicted databases
        if (FileExistsEx(pcdb->sczDbPath, NULL))
        {
            hr = CleanConflictedDatabases(pcdb->sczDbDir, pcdb->sczDbPath);
            TraceError(hr, "Failed to clean conflicted databases, continuing");
            hr = S_OK;
        }

        hr = FileCreateTempW(L"Remote", L".sdf", &pcdb->sczDbCopiedPath, NULL);
        ExitOnFailure(hr, "Failed to create temp file to copy database file to");
    
        hr = FileEnsureCopy(pcdb->sczDbPath, pcdb->sczDbCopiedPath, TRUE);
        ExitOnFailure(hr, "Failed to copy remote database locally");

        hr = FileGetTime(pcdb->sczDbCopiedPath, NULL, NULL, &pcdb->ftBeforeModify);
        ExitOnFailure(hr, "Failed to get modified time of copied remote %ls", pcdb->sczDbCopiedPath);

        hr = SceEnsureDatabase(pcdb->sczDbCopiedPath, wzSqlCeDllPath, L"CfgRemote", 1, &pcdb->dsSceDb, &pcdb->psceDb);
        ExitOnFailure(hr, "Failed to ensure SQL CE database at %ls exists", pcdb->sczDbPath);

        // If the remote wasn't up when we initialized, we couldn't get cfg app id or GUID, so get it now
        if (DWORD_MAX == pcdb->dwCfgAppID)
        {
            hr = HandleEnsureSummaryDataTable(pcdb);
            ExitOnFailure(hr, "Failed to ensure remote database summary data");

            hr = GuidListEnsure(pcdb->pcdbLocal, pcdb->sczGuid, &pcdb->sczGuidRemoteInLocalKey);
            ExitOnFailure(hr, "Failed to ensure remote database is in local database's guid table");

            hr = GuidListEnsure(pcdb, pcdb->pcdbLocal->sczGuid, &pcdb->sczGuidLocalInRemoteKey);
            ExitOnFailure(hr, "Failed to ensure local database is in remote database's guid table");

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
    FILETIME ftRemote = { };
    LPWSTR sczTempRemotePath = NULL;
    BOOL fCopyRemoteBack = FALSE;

    if (1 < pcdb->dwLockRefCount)
    {
        ExitFunction1(hr = S_OK);
    }

    Assert(0 < pcdb->dwLockRefCount);

    // Disconnect from database, if it's a connected remote database
    if (pcdb->fRemote && NULL != pcdb->psceDb)
    {
        fCopyRemoteBack = SceDatabaseChanged(pcdb->psceDb);

        hr = SceCloseDatabase(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to close remote database");
        pcdb->psceDb = NULL;

        if (fCopyRemoteBack)
        {
            hr = FileGetTime(pcdb->sczDbPath, NULL, NULL, &ftRemote);
            ExitOnFailure(hr, "Failed to get modified time of actual remote %ls", &ftRemote);

            // Since DB file wasn't locked, we have to verify that nobody changed it in the meantime.
            // Do it once before we try uploading to the remote (because uploading could be a lengthy operation on a slow connection to remote path)
            if (0 != ::CompareFileTime(&ftRemote, &pcdb->ftBeforeModify))
            {
                hr = HRESULT_FROM_WIN32(ERROR_LOCK_VIOLATION);
                ExitOnFailure(hr, "database %ls was modified (before copy), we can't overwrite it!", pcdb->sczDbPath);
            }

            // Get it on the volume first which may take time
            // TODO: in some cases such as crashes or connection lost to network remote,
            // we'll leave a file behind here. We need a feature to look for and cleanup old files.
            hr = PathConcat(pcdb->sczDbDir, pcdb->sczGuid, &sczTempRemotePath);
            ExitOnFailure(hr, "Failed to get temp path in remote directory");

            hr = FileEnsureCopy(pcdb->sczDbCopiedPath, sczTempRemotePath, TRUE);
            ExitOnFailure(hr, "Failed to copy remote database back to remote location (from %ls to %ls) due to changes", pcdb->sczDbCopiedPath, pcdb->sczDbPath);

            // Now do it again after the upload right before we do the actual move
            hr = FileGetTime(pcdb->sczDbPath, NULL, NULL, &ftRemote);
            ExitOnFailure(hr, "Failed to get modified time of original remote (again) %ls", &ftRemote);

            if (0 != ::CompareFileTime(&ftRemote, &pcdb->ftBeforeModify))
            {
                hr = HRESULT_FROM_WIN32(ERROR_LOCK_VIOLATION);
                ExitOnFailure(hr, "database %ls was modified (after copy), we can't overwrite it!", pcdb->sczDbPath);
            }

            // Use MoveFile to ensure it's done as an atomic operation, so remote can never be left not existing.
            // There is a tiny chance we're reverting someone else's changes here if some other machine just moved the file between
            // the last timestamp check and this MoveFile call. I don't believe there is a way to fix that (we could open a lock on the file,
            // but then we can't use atomic MoveFile() API, meaning we could leave a partial file around in some cases, a HUGE no-no)
            // However, inadvertently overwriting a just-written db file is not a problem - Autosync on all machines will notice the fact
            // that the DB changed, re-sync it, at which time we will try again to re-propagate the changes.
            if (!::MoveFileExW(sczTempRemotePath, pcdb->sczDbPath, MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
            {
                ExitWithLastError(hr, "Failed to move uploaded database path back to original remote location %ls", pcdb->sczDbPath);
            }
        }

        if (pcdb->fUpdateLastModified)
        {
            hr = FileGetTime(pcdb->sczDbCopiedPath, NULL, NULL, &pcdb->ftLastModified);
            if (E_FILENOTFOUND == hr || E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to get modified time of copied db: %ls", pcdb->sczDbCopiedPath);
        }

        hr = FileEnsureDelete(pcdb->sczDbCopiedPath);
        ExitOnFailure(hr, "Failed to delete copied remote database from %ls", pcdb->sczDbCopiedPath);

        ReleaseNullStr(pcdb->sczDbCopiedPath);
    }

    for (DWORD i = 0; i < pcdb->cStreamsToDelete; ++i)
    {
        DeleteStream(pcdb->rgsczStreamsToDelete[i]);
    }
    ReleaseNullStrArray(pcdb->rgsczStreamsToDelete, pcdb->cStreamsToDelete);

    pcdb->fUpdateLastModified = FALSE;

LExit:
    --pcdb->dwLockRefCount;
    ::LeaveCriticalSection(&pcdb->cs);
    if (FAILED(hr) && sczTempRemotePath)
    {
        // In case we left a temp file around, try to delete it before exiting (ignoring failure)
        FileEnsureDelete(sczTempRemotePath);
    }
    ReleaseStr(sczTempRemotePath);
}

HRESULT HandleEnsureSummaryDataTable(
    __in CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    BOOL fEmpty = FALSE;
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
        hr = GuidCreate(&pcdb->sczGuid);
        ExitOnRootFailure(hr, "Failed to generate guid string");

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

HRESULT DeleteStream(
    __in_z LPWSTR sczStreamPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwcLastBackslash = NULL;

    hr = FileEnsureDelete(sczStreamPath);
    ExitOnFailure(hr, "Failed to delete file: %ls", sczStreamPath);

    // Try to delete the two parent directories, in case they're empty
    pwcLastBackslash = wcsrchr(sczStreamPath, '\\');
    if (pwcLastBackslash == NULL)
    {
        hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME);
        ExitOnFailure(hr, "Stream path unexpectedly doesn't contain backslash: %ls", sczStreamPath);
    }
    *pwcLastBackslash = '\0';

    hr = DirEnsureDelete(sczStreamPath, FALSE, FALSE);
    if (FAILED(hr))
    {
        hr = S_OK;
    }
    else
    {
        // Try to delete the next parent up
        // (but don't go further, because the DB file is a sibling of this)
        pwcLastBackslash = wcsrchr(sczStreamPath, '\\');
        if (pwcLastBackslash == NULL)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Stream path unexpectedly doesn't contain backslash: %ls", sczStreamPath);
        }
        *pwcLastBackslash = '\0';

        hr = DirEnsureDelete(sczStreamPath, FALSE, FALSE);
        if (FAILED(hr))
        {
            hr = S_OK;
        }
    }

LExit:
    return hr;
}


static HRESULT CleanConflictedDatabases(
    __in LPCWSTR wzDbDir,
    __in LPCWSTR wzDbPath
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPCWSTR wzDbFile = NULL;
    LPWSTR sczQuery = NULL;
    LPWSTR sczPath = NULL;
    WIN32_FIND_DATAW wfd = { };
    HANDLE hFind = NULL;

    wzDbFile = PathFile(wzDbPath);

    hr = PathConcat(wzDbDir, L"*", &sczQuery);
    ExitOnFailure(hr, "Failed to generate query path to delete conflicted databases");

    hFind = ::FindFirstFileW(sczQuery, &wfd);
    if (INVALID_HANDLE_VALUE == hFind)
    {
        er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);
        if (E_PATHNOTFOUND == hr)
        {
            ExitFunction();
        }
        ExitWithLastError(hr, "Failed to find first file with query: %ls", sczQuery);
    }

    do
    {
        // Safety / silence code analysis tools
        wfd.cFileName[MAX_PATH - 1] = L'\0';

        // Don't use "." or ".."
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wfd.cFileName, -1, L".", -1))
        {
            continue;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wfd.cFileName, -1, L"..", -1))
        {
            continue;
        }
        // Don't delete the actual database file
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT | NORM_IGNORECASE, 0, wfd.cFileName, -1, wzDbFile, -1))
        {
            continue;
        }

        if (!(wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
        {
            hr = PathConcat(wzDbDir, wfd.cFileName, &sczPath);
            ExitOnFailure(hr, "Failed to concat filename '%ls' to directory: %ls", wfd.cFileName, wzDbDir);

            hr = FileEnsureDelete(sczPath);
            TraceError(hr, "Failed to delete remote database file %ls, continuing", sczPath);
            hr = S_OK;
        }
    }
    while (::FindNextFileW(hFind, &wfd));

LExit:
    if (NULL != hFind)
    {
        FindClose(hFind);
    }
    ReleaseStr(sczQuery);
    ReleaseStr(sczPath);

    return hr;
}
