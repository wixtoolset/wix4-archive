// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const LPCWSTR wzCompressedTokenName = L"A";
const DWORD EXTERNAL_STREAM_FILE_FLAGS = FILE_ATTRIBUTE_HIDDEN;
const DWORD CAB_MINIMUM_RAW_FILE_SIZE = 4097;
const float CAB_MINIMUM_SPACE_SAVED = 0.1f;

static DWORD cbSizeFound = 0;

HRESULT CompressToCab(
    __in_bcount(cbBuffer) const BYTE *pbBuffer,
    __in SIZE_T cbBuffer,
    __in COMPRESSION_TYPE compressionType,
    __out LPWSTR *psczPath,
    __out DWORD *pcbCompressedSize
    )
{
    HRESULT hr = S_OK;
    HANDLE hCab = NULL;
    BOOL fFreeCab = FALSE;
    LONGLONG llSize = 0;
    WCHAR wzTempDir[MAX_PATH] = { };
    WCHAR wzTempFilePath[MAX_PATH] = { };
    WCHAR wzCabPath[MAX_PATH] = { };

    if (!::GetTempPathW(countof(wzTempDir), wzTempDir))
    {
        ExitWithLastError(hr, "Failed to get temp path.");
    }

    if (!::GetTempFileNameW(wzTempDir, L"CFGCAB", 0, wzCabPath))
    {
        ExitWithLastError(hr, "Failed to create a temp cab file name.");
    }

    if (!::GetTempFileNameW(wzTempDir, L"CFGFILE", 0, wzTempFilePath))
    {
        ExitWithLastError(hr, "Failed to create a input file name for cab.");
    }

    hr = CabCBegin(PathFile(wzCabPath), wzTempDir, 1, 0, 0, compressionType, &hCab);
    ExitOnFailure(hr, "Failed to begin compressing stream into cab %ls", wzCabPath);
    fFreeCab = TRUE;

    hr = FileWrite(wzTempFilePath, FILE_ATTRIBUTE_TEMPORARY, pbBuffer, cbBuffer, NULL);
    ExitOnFailure(hr, "Failed to write file out to path %ls", wzTempFilePath);

    hr = CabCAddFile(wzTempFilePath, wzCompressedTokenName, NULL, hCab);
    ExitOnFailure(hr, "Failed to add only file %ls to cab %ls", wzTempFilePath, wzCabPath);

    hr = CabCFinish(hCab, NULL);
    ExitOnFailure(hr, "Failed to finish cab %ls", wzCabPath);
    fFreeCab = FALSE;

    // Ignore failures
    FileEnsureDelete(wzTempFilePath);

    hr = StrAllocString(psczPath, wzCabPath, 0);
    ExitOnFailure(hr, "Failed to copy cab path");

    hr = FileSize(*psczPath, &llSize);
    ExitOnFailure(hr, "Failed to get size of cab file");

    if (static_cast<LONGLONG>(DWORD_MAX) < llSize)
    {
        hr = E_FAIL;
        ExitOnFailure(hr, "CAB file bigger than DWORD can track: %ls", *psczPath);
    }

    *pcbCompressedSize = static_cast<DWORD>(llSize);

LExit:
    if (fFreeCab)
    {
        CabCCancel(hCab);
    }

    return hr;
}

HRESULT CompressFromCab(
    __in LPCWSTR wzPath,
    __deref_out_bcount(*pcbSize) BYTE **ppbFileBuffer,
    __out SIZE_T *pcbSize
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczInputFile = NULL;
    WCHAR wzTempDir[MAX_PATH] = { };

    if (!::GetTempPathW(countof(wzTempDir), wzTempDir))
    {
        ExitWithLastError(hr, "Failed to get temp path.");
    }

    hr = PathConcat(wzTempDir, wzCompressedTokenName, &sczInputFile);
    ExitOnFailure(hr, "Failed to get path to extracted cab file");

    hr = FileEnsureDelete(sczInputFile);
    ExitOnFailure(hr, "Failed to delete file: %ls", sczInputFile);

    hr = CabExtract(wzPath, wzCompressedTokenName, wzTempDir, NULL, NULL, 0);
    ExitOnFailure(hr, "Failed to extract only file from cabinet: %ls", wzPath);

    hr = FileRead(ppbFileBuffer, pcbSize, sczInputFile);
    ExitOnFailure(hr, "Failed to read extracted file: %ls", sczInputFile);

LExit:
    if (sczInputFile)
    {
        FileEnsureDelete(sczInputFile);
    }
    ReleaseStr(sczInputFile);

    return hr;
}

HRESULT CompressWriteStream(
    __in const CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __in_bcount(cbBuffer) const BYTE *pbBuffer,
    __in SIZE_T cbBuffer,
    __out COMPRESSION_FORMAT *pcfCompressionFormat
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczStreamPath = NULL;
    LPWSTR sczCabPath = NULL;
    DWORD cbCabSize = 0;

    hr = StreamGetFilePath(&sczStreamPath, pcdb, pbHash, TRUE);
    ExitOnFailure(hr, "Failed to get stream file path");

    // Don't bother trying to cab really small files
    if (CAB_MINIMUM_RAW_FILE_SIZE <= cbBuffer)
    {
        hr = CompressToCab(pbBuffer, cbBuffer, COMPRESSION_TYPE_HIGH, &sczCabPath, &cbCabSize);
        ExitOnFailure(hr, "Failed to create cab file");
    }

    // If we have a CAB and it's smaller by enough, use the cab file
    if (sczCabPath && cbCabSize < cbBuffer && ((cbBuffer - cbCabSize) > cbBuffer * CAB_MINIMUM_SPACE_SAVED))
    {
        *pcfCompressionFormat = COMPRESSION_CAB;

        // We saved enough space to bother with CAB, so use that
        // It's important to overwrite - in some cases (such as writing streams to a db,
        // then rolling back the transaction or crashing or losing connection to remote)
        // we'll leave a stream behind that db doesn't know about, and is possibly incomplete. So just overwrite it.
        hr = FileEnsureMove(sczCabPath, sczStreamPath, TRUE, TRUE);
        ExitOnFailure(hr, "Failed to move file from cab path %ls to stream path %ls", sczCabPath, sczStreamPath);

        ReleaseNullStr(sczCabPath);
    }
    else
    {
        *pcfCompressionFormat = COMPRESSION_NONE;

        hr = FileWrite(sczStreamPath, EXTERNAL_STREAM_FILE_FLAGS, pbBuffer, cbBuffer, NULL);
        ExitOnFailure(hr, "Failed to write file stream of size %u to disk at location: %ls", cbBuffer, sczStreamPath);
    }

    if (sczCabPath)
    {
        // Ignore failures, worst is that we'll leave a file in temp directory
        FileEnsureDelete(sczCabPath);
    }

LExit:
    ReleaseStr(sczStreamPath);
    ReleaseStr(sczCabPath);

    return hr;
}

HRESULT CompressReadStream(
    __in const CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __in COMPRESSION_FORMAT cfCompressionFormat,
    __deref_opt_out_bcount_opt(*pcbSize) BYTE **ppbFileBuffer,
    __out DWORD *pcbSize
    )
{
    HRESULT hr = S_OK;
    BOOL fMatches;
    LPWSTR sczStreamPath = NULL;
    BYTE *pbContent = NULL;
    DWORD cbContent = 0;

    hr = StreamGetFilePath(&sczStreamPath, pcdb, pbHash, FALSE);
    ExitOnFailure(hr, "Failed to get stream file path");

    if (!FileExistsEx(sczStreamPath, NULL))
    {
        LogStringLine(REPORT_STANDARD, "Stream %ls was missing. If syncing to a cloud-managed directory, this is normal, and autosync (when the file is downloaded to the machine) will automatically fix the situation.", sczStreamPath);
        ExitFunction1(hr = HRESULT_FROM_WIN32(PEERDIST_ERROR_MISSING_DATA));
    }

    switch (cfCompressionFormat)
    {
    case COMPRESSION_CAB:
        hr = CompressFromCab(sczStreamPath, &pbContent, &cbContent);
        ExitOnFailure(hr, "Failed to read cab stream on disk: %ls", sczStreamPath);
        break;

    case COMPRESSION_NONE:
        hr = FileRead(&pbContent, &cbContent, sczStreamPath);
        ExitOnFailure(hr, "Failed to read uncompressed stream file: %ls", sczStreamPath);
        break;

    default:
        hr = E_FAIL;
        ExitOnFailure(hr, "Unrecognized compression option found while reading stream from disk: %u", cfCompressionFormat);
        break;
    }

    if (NULL != ppbFileBuffer)
    {
        hr = StreamValidateFileMatchesHash(pbContent, cbContent, pbHash, &fMatches);
        ExitOnFailure(hr, "Failed to check if file at path matches expected hash: %ls", sczStreamPath);

        if (!fMatches)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Stream file on disk appears to be corrupted: %ls", sczStreamPath);
        }
    }

    *pcbSize = cbContent;

    if (NULL != ppbFileBuffer)
    {
        *ppbFileBuffer = pbContent;
        pbContent = NULL;
    }

LExit:
    ReleaseMem(pbContent);
    ReleaseStr(sczStreamPath);

    return hr;
}
