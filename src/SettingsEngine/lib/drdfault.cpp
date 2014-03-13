//-------------------------------------------------------------------------------------------------
// <copyright file="drdfault.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for Cfg Legacy API (for purposes of default directory handling)
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

HRESULT DirDefaultReadFile(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzName,
    __in_z LPCWSTR wzFilePath,
    __in_z LPCWSTR wzSubPath
    )
{
    HRESULT hr = S_OK;
    FILETIME ft;
    SYSTEMTIME st;
    LPWSTR sczValueName = NULL;
    SCE_ROW_HANDLE sceValueRow = NULL;
    BOOL fRet = FALSE;
    BOOL fIgnore = FALSE;
    BYTE *pbBuffer = NULL;
    DWORD cbBuffer = 0;
    BOOL fRefreshTimestamp = FALSE;
    int iTimestampCompare = 0;
    CONFIG_VALUE cvExistingValue = { };
    CONFIG_VALUE cvNewValue = { };

    hr = MapFileToCfgName(wzName, wzSubPath, &sczValueName);
    ExitOnFailure2(hr, "Failed to get cfg name for file at name %ls, subpath %ls", wzName, wzSubPath);

    hr = FilterCheckValue(&pSyncProductSession->product, sczValueName, &fIgnore);
    ExitOnFailure1(hr, "Failed to check if cfg blob value should be ignored: %ls", sczValueName);

    if (fIgnore)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = FileGetTime(wzFilePath, NULL, NULL, &ft);
    ExitOnFailure1(hr, "failed to get modified time of file : %ls", wzFilePath);

    fRet = FileTimeToSystemTime(&ft, &st);
    if (!fRet)
    {
        hr = E_INVALIDARG;
        ExitOnFailure1(hr, "Failed to convert file time to system time for file: %ls", wzFilePath);
    }

    // In the case of files susceptible to the "VirtualStore\" directory,
    // we check virtualstore first, then the regular directory, so skip the file
    // if we've already seen it on an earlier pass
    hr = DictKeyExists(pSyncProductSession->shDictValuesSeen, sczValueName);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    else
    {
        ExitOnFailure(hr, "Failed to check if file was already seen before reading");

        ExitFunction1(hr = S_OK);
    }

    hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczValueName);
    ExitOnFailure1(hr, "Failed to add file to list of files seen: %ls", sczValueName);

    hr = ValueFindRow(pcdb, VALUE_INDEX_TABLE, pcdb->dwAppID, sczValueName, &sceValueRow);
    if (S_OK == hr)
    {
        hr = ValueRead(pcdb, sceValueRow, &cvExistingValue);
        ExitOnFailure(hr, "Failed to get existing file in database's timestamp when setting file");

        // If we already have a blob and the timestamps are identical, don't bother reading the file
        iTimestampCompare = UtilCompareSystemTimes(&st, &cvExistingValue.stWhen);
        if (VALUE_BLOB == cvExistingValue.cvType && 0 == iTimestampCompare)
        {
            ExitFunction();
        }

        if (0 > iTimestampCompare)
        {
            // The value on disk is OLDER than our current database value. Since Cfg Db expects values to always be getting newer, refresh the file's timestamp
            fRefreshTimestamp = TRUE;
        }
    }

    hr = FileRead(&pbBuffer, &cbBuffer, wzFilePath);
    ExitOnFailure1(hr, "Failed to read file into memory: %ls", wzFilePath);

    if (fRefreshTimestamp)
    {
        ::GetSystemTime(&st);
        fRet = ::SystemTimeToFileTime(&st, &ft);
        if (!fRet)
        {
            ExitWithLastError(hr, "Failed to convert system time to file time");
        }

        hr = FileSetTime(wzFilePath, NULL, NULL, &ft);
        ExitOnFailure1(hr, "Failed to refresh timestamp on file: %ls", wzFilePath);
    }

    hr = ValueSetBlob(pbBuffer, cbBuffer, FALSE, &st, pcdb->sczGuid, &cvNewValue);
    ExitOnFailure1(hr, "Failed to set value in memory for file %ls", sczValueName);

    // Important: we must write the value even if the actual data didn't change, to update the timestamp in the database
    // This keeps perf high for future syncs, so we can rely on timestamp check
    hr = ValueWrite(pcdb, pcdb->dwAppID, sczValueName, &cvNewValue, FALSE);
    ExitOnFailure1(hr, "Failed to set blob in cfg database, blob is named: %ls", sczValueName);

LExit:
    ReleaseSceRow(sceValueRow);
    ReleaseStr(sczValueName);
    ReleaseMem(pbBuffer);
    ReleaseCfgValue(cvExistingValue);
    ReleaseCfgValue(cvNewValue);

    return hr;
}

HRESULT DirDefaultWriteFile(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzName,
    __in const CONFIG_VALUE *pcvValue,
    __out BOOL *pfHandled
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDir = NULL;
    LPWSTR sczPath = NULL;
    LPWSTR sczVirtualStorePath = NULL;
    SYSTEMTIME stDisk = { };
    FILETIME ftDisk = { };
    FILETIME ftCfg = { };
    BYTE *pbBuffer = NULL;
    DWORD cbBuffer = 0;
    int iTimestampCompare = 0;
    BOOL fRet = FALSE;
    BOOL fFileExists = FALSE;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HANDLE hFileDelete = INVALID_HANDLE_VALUE; // A separate handle specifically for opening the file for "delete on close" behavior

    *pfHandled = FALSE;

    hr = MapCfgNameToFile(pProduct, wzName, &sczPath);
    if (E_INVALIDARG == hr)
    {
        // Doesn't map to an actual file, so leave it alone
        ExitFunction1(hr = S_OK);
    }
    if (E_NOTFOUND == hr)
    {
        // This means it was a detected directory that we were unable to detect, so this is the appropriate handler for the value, but there is no action that can be done
        *pfHandled = TRUE;
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure1(hr, "Failed to get path to write for legacy file: %ls", wzName);

    *pfHandled = TRUE;

    // If it's not a valid cfg type for a file, or the file doesn't exist and we're intending to delete it, just exit with no error
    // This doesn't need to be transactional because if a new file was written right after this, autosync will pick it up
    if ((VALUE_BLOB != pcvValue->cvType && VALUE_DELETED != pcvValue->cvType) || (!FileExistsEx(sczPath, NULL) && VALUE_DELETED == pcvValue->cvType))
    {
        ExitFunction1(hr = S_OK);
    }

    if (VALUE_BLOB == pcvValue->cvType)
    {
        hr = PathGetDirectory(sczPath, &sczDir);
        ExitOnFailure1(hr, "Failed to get directory of file: %ls", sczPath);

        hr = DirEnsureExists(sczDir, NULL);
        if (E_ACCESSDENIED == hr)
        {
            hr = UtilConvertToVirtualStorePath(sczDir, &sczVirtualStorePath);
            ExitOnFailure1(hr, "Failed to convert directory path to virtualstore path: %ls", sczDir);

            hr = DirEnsureExists(sczVirtualStorePath, NULL);
        }
        ExitOnFailure1(hr, "Failed to ensure directory exists: %ls", sczDir);
    }

    // Very important - this file handle makes our action against the file transactional in the sense that some other program
    // cannot write data to it while we're operating on it (because if we allowed that, we might permanently overwrite user data before we had a chance to read it)
    // We must use FILE_SHARE_DELETE because we actually use a second delete-on-close handle to delete the file within this function if we need to
    // we MUST release the hFile handle AFTER the hFileDelete handle to ensure it's deleted transactionally
    // This does carry with it a slight risk that an app could read the file while we're writing it or be denied permission to write to it
    // but this is brief, and it is not expected that we should need to write data to the local machine from another machine for an app while the app is writing data locally
    hFile = ::CreateFileW(sczPath, GENERIC_WRITE, FILE_SHARE_DELETE, NULL, OPEN_ALWAYS, 0, NULL);
    if (INVALID_HANDLE_VALUE == hFile && ERROR_ACCESS_DENIED == ::GetLastError())
    {
        hr = UtilConvertToVirtualStorePath(sczPath, &sczVirtualStorePath);
        ExitOnFailure1(hr, "Failed to convert file path to virtualstore path: %ls", sczPath);

        // Switch path for the rest of the file to the virtualstore path
        ReleaseStr(sczPath);
        sczPath = sczVirtualStorePath;
        ReleaseNullStr(sczVirtualStorePath);

        hFile = ::CreateFileW(sczPath, GENERIC_WRITE, FILE_SHARE_READ, NULL, OPEN_ALWAYS, 0, NULL);
    }
    ExitOnInvalidHandleWithLastError1(hFile, hr, "Failed to open file for write: %ls", sczPath);
    fFileExists = ::GetLastError() == ERROR_ALREADY_EXISTS;

    if (fFileExists)
    {
        // If the file exists, check if it has the same timestamp - if it does, don't write it, or if it has newer timestamp, break out so we can re-do the whole sync
        hr = FileGetTime(sczPath, NULL, NULL, &ftDisk);
        ExitOnFailure1(hr, "Failed to get time of file: %ls", sczPath);

        fRet = ::FileTimeToSystemTime(&ftDisk, &stDisk);
        if (!fRet)
        {
            ExitWithLastError(hr, "Failed to convert disk file time to system time");
        }

        iTimestampCompare = UtilCompareSystemTimes(&stDisk, &pcvValue->stWhen);
        if (0 == iTimestampCompare)
        {
            // Modified time on disk matches what's in the cfg database - so no need to write the file out to disk again
            ExitFunction1(hr = S_OK);
        }
        else if (0 < iTimestampCompare)
        {
            // File changed during sync - abort with a retryable error code.
            hr = HRESULT_FROM_WIN32(ERROR_TIME_SKEW);
            ExitOnFailure1(hr, "Found newer file on disk at path %ls while trying to write file from DB, file must have changed during sync, aborting", sczPath);
        }

        fRet = ::SetEndOfFile(hFile);
        if (!fRet)
        {
            ExitWithLastError1(hr, "Failed to truncate file %ls", sczPath);
        }
    }

    // Delete if appropriate
    if (VALUE_DELETED == pcvValue->cvType)
    {
        hFileDelete = ::CreateFileW(sczPath, 0, FILE_SHARE_WRITE, NULL, OPEN_ALWAYS, FILE_FLAG_DELETE_ON_CLOSE, NULL);
        ExitOnInvalidHandleWithLastError1(hFile, hr, "Failed to mark file for deletion: %ls", sczPath);

        ExitFunction1(hr = S_OK);
    }

    if (CFG_BLOB_DB_STREAM != pcvValue->blob.cbType)
    {
        hr = E_INVALIDARG;
        ExitOnFailure2(hr, "Unexpected blob value type %d while writing file %ls", pcvValue->blob.cbType, sczPath);
    }

    hr = StreamRead(pcvValue->blob.dbstream.pcdb, pcvValue->blob.dbstream.dwContentID, NULL, &pbBuffer, &cbBuffer);
    ExitOnFailure1(hr, "Failed to get binary content of ID: %u", pcvValue->blob.dbstream.dwContentID);

    fRet = ::SystemTimeToFileTime(&pcvValue->stWhen, &ftCfg);
    if (!fRet)
    {
        ExitWithLastError(hr, "Failed to convert cfg system time to file time");
    }

    hr = FileWriteHandle(hFile, pbBuffer, cbBuffer);
    ExitOnFailure1(hr, "Failed to write content to file: %ls", sczPath);

    // Important: release file handles before setting time, or it won't be guaranteed to take effect
    ReleaseFile(hFileDelete); // must be before releasing hFile, to ensure it's deleted as part of a transaction
    ReleaseFile(hFile);

    hr = FileSetTime(sczPath, NULL, NULL, &ftCfg);
    ExitOnFailure1(hr, "Failed to set timestamp on file: %ls", sczPath);

LExit:
    ReleaseFile(hFileDelete); // must be before releasing hFile, to ensure it's deleted as part of a transaction
    ReleaseFile(hFile);
    ReleaseMem(pbBuffer);
    ReleaseStr(sczDir);
    ReleaseStr(sczPath);
    ReleaseStr(sczVirtualStorePath);

    return hr;
}
