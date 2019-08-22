// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT FindByHash(
    __in CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __out_opt DWORD *pdwContentID,
    __out_opt SCE_ROW_HANDLE *pSceRow
    );

HRESULT StreamRead(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwContentID,
    __deref_opt_out_bcount_opt(CFG_HASH_LEN) BYTE **ppbHashBuffer,
    __deref_out_bcount_opt(*pcbSize) BYTE **ppbFileBuffer,
    __out SIZE_T *pcbSize
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRow = NULL;
    SIZE_T cbHashSize = 0;
    BYTE *pbHashBuffer = NULL;
    BYTE *pbContent = NULL;
    LPWSTR sczStreamPath = NULL;
    DWORD cbDbSize = 0;
    DWORD cbDiskSize = 0;
    COMPRESSION_FORMAT cfCompressionFormat = COMPRESSION_NONE;

    hr = SceBeginQuery(pcdb->psceDb, BINARY_CONTENT_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into binary content table");

    hr = SceSetQueryColumnDword(sqhHandle, dwContentID);
    ExitOnFailure(hr, "Failed to set query column dword for ContentID column while querying binary content table, ContentID: %u", dwContentID);

    hr = SceRunQueryExact(&sqhHandle, &sceRow);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to run query into binary content table");

    hr = SceGetColumnDword(sceRow, BINARY_RAW_SIZE, &cbDbSize);
    ExitOnFailure(hr, "Failed to get raw size for binary content of ID: %u", dwContentID);

    hr = SceGetColumnBinary(sceRow, BINARY_HASH, &pbHashBuffer, &cbHashSize);
    ExitOnFailure(hr, "Failed to get hash for binary content of ID: %u", dwContentID);

    if (CFG_HASH_LEN != cbHashSize)
    {
        hr = E_NOTFOUND;
        ExitOnFailure(hr, "Wrong size of hash encountered in database - expected %u, found %u", CFG_HASH_LEN, cbHashSize);
    }

    hr = SceGetColumnDword(sceRow, BINARY_COMPRESSION, reinterpret_cast<DWORD *>(&cfCompressionFormat));
    ExitOnFailure(hr, "Failed to get compression format for binary content of ID: %u", dwContentID);

    if (NULL != ppbFileBuffer)
    {
        hr = CompressReadStream(pcdb, pbHashBuffer, cfCompressionFormat, &pbContent, &cbDiskSize);
        ExitOnFailure(hr, "Failed to read stream file from disk for content ID %u", dwContentID);

        if (cbDiskSize != cbDbSize)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Stream with ID %u has error - size mismatch - stream size on disk = %u, stream size in DB = %u", cbDiskSize, cbDbSize);
        }

        *ppbFileBuffer = pbContent;
        pbContent = NULL;
    }

    if (NULL != ppbHashBuffer)
    {
        *ppbHashBuffer = pbHashBuffer;
        pbHashBuffer = NULL;
    }
    if (NULL != pcbSize)
    {
        *pcbSize = cbDbSize;
    }

LExit:
    ReleaseMem(pbHashBuffer);
    ReleaseMem(pbContent);
    ReleaseSceQuery(sqhHandle);
    ReleaseSceRow(sceRow);
    ReleaseStr(sczStreamPath);

    return hr;
}

HRESULT StreamWrite(
    __in CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE* pbHash,
    __in_bcount_opt(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __out DWORD *pdwContentID
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE sceRow = NULL;
    const BYTE *pbHashToUse = NULL;
    DWORD dwContentID = 0;
    BYTE rgbActualHash[CFG_HASH_LEN] = { };
    BOOL fInSceTransaction = FALSE;
    LPWSTR sczStreamPath = NULL;
    COMPRESSION_FORMAT cfCompressionFormat = COMPRESSION_NONE;

    if (0 == cbBuffer || NULL == pbBuffer)
    {
        // For an empty file, override and use all zeros for the hash
        pbHashToUse = rgbActualHash;
    }
    else
    {
        pbHashToUse = pbHash;
    }

    // If a file with this hash already exists, recycle the same row
    hr = FindByHash(pcdb, pbHashToUse, &dwContentID, NULL);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    else
    {
        ExitOnFailure(hr, "Failed to find file by hash while setting stream");

        hr = StreamIncreaseRefcount(pcdb, dwContentID, 1);
        ExitOnFailure(hr, "Failed to increase stream ref count of Content ID %u", dwContentID);

        *pdwContentID = dwContentID;
        ExitFunction1(hr = S_OK);
    }

    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction while inserting record into binary content table");
    fInSceTransaction = TRUE;

    hr = ScePrepareInsert(pcdb->psceDb, BINARY_CONTENT_TABLE, &sceRow);
    ExitOnFailure(hr, "Failed to prepare for insert to binary content table");

    hr = SceSetColumnDword(sceRow, BINARY_REFCOUNT, 1);
    ExitOnFailure(hr, "Failed to set RefCount column to 1");

    hr = SceSetColumnDword(sceRow, BINARY_RAW_SIZE, cbBuffer);
    ExitOnFailure(hr, "Failed to set Raw Size column");

    hr = SceSetColumnBinary(sceRow, BINARY_HASH, pbHashToUse, CFG_HASH_LEN);
    ExitOnFailure(hr, "Failed to set Hash column");

    hr = CompressWriteStream(pcdb, pbHashToUse, pbBuffer, cbBuffer, &cfCompressionFormat);
    ExitOnFailure(hr, "Failed to write stream to disk");

    hr = SceSetColumnDword(sceRow, BINARY_COMPRESSION, static_cast<DWORD>(cfCompressionFormat));
    ExitOnFailure(hr, "Failed to set Compression column to 0");

    hr = SceFinishUpdate(sceRow);
    ExitOnFailure(hr, "Failed to finish update inserting new record to binary content table");

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit new record to binary content table");
    fInSceTransaction = FALSE;

    hr = SceGetColumnDword(sceRow, BINARY_ID, &dwContentID);
    ExitOnFailure(hr, "Failed to get content ID from binary content table");

    *pdwContentID = dwContentID;

LExit:
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    ReleaseSceRow(sceRow);
    ReleaseStr(sczStreamPath);

    return hr;
}

HRESULT StreamCopy(
    __in CFGDB_STRUCT *pcdbFrom,
    __in DWORD dwContentIDFrom,
    __in CFGDB_STRUCT *pcdbTo,
    __out DWORD *pdwContentIDTo
    )
{
    HRESULT hr = S_OK;
    DWORD dwContentIDTemp = 0;
    LPWSTR sczStreamPath = NULL;
    LPWSTR sczStreamPathFrom = NULL;
    LPWSTR sczStreamPathTo = NULL;
    COMPRESSION_FORMAT cfCompressionFormat = COMPRESSION_NONE;
    SIZE_T cbSize = 0;
    BYTE *pbHashBuffer = NULL;
    SIZE_T cbHashSize = 0;
    BOOL fInSceTransaction = FALSE;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRowRead = NULL;
    SCE_ROW_HANDLE sceRowInsert = NULL;

    hr = SceBeginQuery(pcdbFrom->psceDb, BINARY_CONTENT_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into binary content table");

    hr = SceSetQueryColumnDword(sqhHandle, dwContentIDFrom);
    ExitOnFailure(hr, "Failed to set query column dword for ContentID column while querying binary content table, ContentID: %u", dwContentIDFrom);

    hr = SceRunQueryExact(&sqhHandle, &sceRowRead);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to run query into binary content table");

    hr = SceGetColumnBinary(sceRowRead, BINARY_HASH, &pbHashBuffer, reinterpret_cast<DWORD *>(&cbHashSize));
    ExitOnFailure(hr, "Failed to get hash for binary content of ID: %u", dwContentIDFrom);

    // If a file with this hash already exists, recycle the same row
    hr = FindByHash(pcdbTo, pbHashBuffer, &dwContentIDTemp, NULL);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    else
    {
        ExitOnFailure(hr, "Failed to find file by hash while setting stream");

        hr = StreamIncreaseRefcount(pcdbTo, dwContentIDTemp, 1);
        ExitOnFailure(hr, "Failed to increase stream ref count of Content ID %u", dwContentIDTemp);

        *pdwContentIDTo = dwContentIDTemp;
        ExitFunction1(hr = S_OK);
    }

    hr = SceGetColumnDword(sceRowRead, BINARY_COMPRESSION, reinterpret_cast<DWORD *>(&cfCompressionFormat));
    ExitOnFailure(hr, "Failed to get compression format for binary content of ID: %u", dwContentIDFrom);

    hr = SceGetColumnDword(sceRowRead, BINARY_RAW_SIZE, reinterpret_cast<DWORD *>(&cbSize));
    ExitOnFailure(hr, "Failed to get compression format for binary content of ID: %u", dwContentIDFrom);

    if (CFG_HASH_LEN != cbHashSize)
    {
        hr = E_NOTFOUND;
        ExitOnFailure(hr, "Wrong size of hash encountered in database - expected %u, found %u", CFG_HASH_LEN, cbHashSize);
    }

    hr = SceBeginTransaction(pcdbTo->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction while inserting record into binary content table");
    fInSceTransaction = TRUE;

    hr = ScePrepareInsert(pcdbTo->psceDb, BINARY_CONTENT_TABLE, &sceRowInsert);
    ExitOnFailure(hr, "Failed to prepare for insert to binary content table");

    hr = SceSetColumnDword(sceRowInsert, BINARY_REFCOUNT, 1);
    ExitOnFailure(hr, "Failed to set RefCount column to 1");

    hr = SceSetColumnDword(sceRowInsert, BINARY_RAW_SIZE, cbSize);
    ExitOnFailure(hr, "Failed to set Raw Size column");

    hr = SceSetColumnBinary(sceRowInsert, BINARY_HASH, pbHashBuffer, cbHashSize);
    ExitOnFailure(hr, "Failed to set Hash column");

    hr = StreamGetFilePath(&sczStreamPathFrom, pcdbFrom, pbHashBuffer, FALSE);
    ExitOnFailure(hr, "Failed to get stream file path");

    hr = StreamGetFilePath(&sczStreamPathTo, pcdbTo, pbHashBuffer, TRUE);
    ExitOnFailure(hr, "Failed to get stream file path");

    hr = FileEnsureCopy(sczStreamPathFrom, sczStreamPathTo, FALSE);
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        LogStringLine(REPORT_STANDARD, "Stream %ls was missing. If syncing to a cloud-managed directory, this is normal, and autosync (when the file is downloaded to the machine) will automatically fix the situation.", sczStreamPathFrom);
        ExitFunction1(hr = HRESULT_FROM_WIN32(PEERDIST_ERROR_MISSING_DATA));
    }
    ExitOnFailure(hr, "Failed to copy file from %ls to %ls", sczStreamPathFrom, sczStreamPathTo);

    hr = SceSetColumnDword(sceRowInsert, BINARY_COMPRESSION, static_cast<DWORD>(cfCompressionFormat));
    ExitOnFailure(hr, "Failed to set Compression column to 0");

    hr = SceFinishUpdate(sceRowInsert);
    ExitOnFailure(hr, "Failed to finish update inserting new record to binary content table");

    hr = SceCommitTransaction(pcdbTo->psceDb);
    ExitOnFailure(hr, "Failed to commit new record to binary content table");
    fInSceTransaction = FALSE;

    hr = SceGetColumnDword(sceRowInsert, BINARY_ID, pdwContentIDTo);
    ExitOnFailure(hr, "Failed to get content ID from binary content table");

LExit:
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdbTo->psceDb);
    }
    ReleaseSceQuery(sqhHandle);
    ReleaseSceRow(sceRowRead);
    ReleaseSceRow(sceRowInsert);
    ReleaseStr(sczStreamPath);
    ReleaseStr(sczStreamPathFrom);
    ReleaseStr(sczStreamPathTo);

    return hr;
}

HRESULT StreamIncreaseRefcount(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwContentID,
    __in DWORD dwAmount
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRow = NULL;
    DWORD dwRefCount = 0;

    hr = SceBeginQuery(pcdb->psceDb, BINARY_CONTENT_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into binary content table");

    hr = SceSetQueryColumnDword(sqhHandle, dwContentID);
    ExitOnFailure(hr, "Failed to set query column dword for ContentID column while querying binary content table, ContentID: %u", dwContentID);

    hr = SceRunQueryExact(&sqhHandle, &sceRow);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to run query into binary content table");

    hr = SceGetColumnDword(sceRow, BINARY_REFCOUNT, &dwRefCount);
    ExitOnFailure(hr, "Failed to get current refcount for binary ID: %u", dwContentID);

    dwRefCount += dwAmount;

    hr = SceSetColumnDword(sceRow, BINARY_REFCOUNT, dwRefCount);
    ExitOnFailure(hr, "Failed to refcount binary of ID: %u", dwContentID);

    hr = SceFinishUpdate(sceRow);
    ExitOnFailure(hr, "Failed to finish update while refcounting binary of ID: %u", dwContentID);

LExit:
    ReleaseSceQuery(sqhHandle);
    ReleaseSceRow(sceRow);

    return hr;
}

HRESULT StreamDecreaseRefcount(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwContentID,
    __in DWORD dwAmount
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRow = NULL;
    BYTE *pbHashBuffer = NULL;
    SIZE_T cbHashSize = 0;
    LPWSTR sczStreamPath = NULL;
    DWORD dwRefCount = 0;

    hr = SceBeginQuery(pcdb->psceDb, BINARY_CONTENT_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into binary content table");

    hr = SceSetQueryColumnDword(sqhHandle, dwContentID);
    ExitOnFailure(hr, "Failed to set query column dword for ContentID column while querying binary content table, ContentID: %u", dwContentID);

    hr = SceRunQueryExact(&sqhHandle, &sceRow);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to run query into binary content table");

    hr = SceGetColumnDword(sceRow, BINARY_REFCOUNT, &dwRefCount);
    ExitOnFailure(hr, "Failed to get current refcount for binary ID: %u", dwContentID);

    hr = SceGetColumnBinary(sceRow, BINARY_HASH, &pbHashBuffer, &cbHashSize);
    ExitOnFailure(hr, "Failed to get hash for binary content of ID: %u", dwContentID);

    // Don't let us do a negative overflow
    if (dwRefCount < dwAmount)
    {
        dwRefCount = 0;
    }
    else
    {
        dwRefCount -= dwAmount;
    }

    if (0 == dwRefCount)
    {
        // Add to list of streams to be deleted after commit
        hr = MemEnsureArraySize(reinterpret_cast<void **>(&pcdb->rgsczStreamsToDelete), pcdb->cStreamsToDelete + 1, sizeof(LPWSTR), 10);
        ExitOnFailure(hr, "Failed to grow stream delete array");
        ++pcdb->cStreamsToDelete;

        hr = StreamGetFilePath(&pcdb->rgsczStreamsToDelete[pcdb->cStreamsToDelete - 1], pcdb, pbHashBuffer, FALSE);
        ExitOnFailure(hr, "Failed to get stream file path");

        hr = SceDeleteRow(&sceRow);
        ExitOnFailure(hr, "Failed to delete row of binary ID: %u", dwContentID);
    }
    else
    {
        hr = SceSetColumnDword(sceRow, BINARY_REFCOUNT, dwRefCount);
        ExitOnFailure(hr, "Failed to refcount binary of ID: %u", dwContentID);

        hr = SceFinishUpdate(sceRow);
        ExitOnFailure(hr, "Failed to finish update while de-refcounting binary of ID: %u", dwContentID);
    }

LExit:
    ReleaseSceQuery(sqhHandle);
    ReleaseSceRow(sceRow);
    ReleaseMem(pbHashBuffer);
    ReleaseStr(sczStreamPath);

    return hr;
}

HRESULT StreamGetFilePath(
    __deref_out_z LPWSTR *psczPath,
    __in const CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __in BOOL fCreateDirectory
    )
{
    HRESULT hr = S_OK;
    WCHAR wzHashAsString[CFG_HASH_LEN * 2 + 1]; // Two hex characters for each byte, plus one null terminator
    LPWSTR sczHexDigits12 = NULL;
    LPWSTR sczHexDigits34 = NULL;
    LPWSTR sczTempPath = NULL;
    LPWSTR sczDirectory = NULL;
    LPWSTR sczOutputPath = NULL;

    hr = StrHexEncode(pbHash, CFG_HASH_LEN, wzHashAsString, _countof(wzHashAsString));
    ExitOnFailure(hr, "Failed to encode hash as hex while getting file stream path");

    hr = StrAllocString(&sczHexDigits12, wzHashAsString, 2);
    ExitOnFailure(hr, "Failed to copy first set of 2 digits of hash to string");

    hr = StrAllocString(&sczHexDigits34, wzHashAsString + 2, 2);
    ExitOnFailure(hr, "Failed to copy second set of 2 digits of hash to string");

    hr = PathConcat(pcdb->sczStreamsDir, sczHexDigits12, &sczTempPath);
    ExitOnFailure(hr, "Failed to add first set of 2 digits of hex string to output path");

    hr = PathConcat(sczTempPath, sczHexDigits34, &sczDirectory);
    ExitOnFailure(hr, "Failed to add second set of 2 digits of hex string to output path");

    hr = PathConcat(sczDirectory, &wzHashAsString[4], psczPath);
    ExitOnFailure(hr, "Failed to add remaining hash characters to output path");

    if (fCreateDirectory)
    {
        hr = DirEnsureExists(sczDirectory, NULL);
        ExitOnFailure(hr, "Failed to create directory: %ls", sczDirectory);
    }

LExit:
    ReleaseStr(sczHexDigits12);
    ReleaseStr(sczHexDigits34);
    ReleaseStr(sczTempPath);
    ReleaseStr(sczDirectory);
    ReleaseStr(sczOutputPath);

    return hr;
}

HRESULT StreamValidateFileMatchesHash(
    __in_bcount(cbBuffer) const BYTE *pbBuffer,
    __in SIZE_T cbBuffer,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __out BOOL *pfValid
    )
{
    HRESULT hr = S_OK;
    BYTE rgbHash[CFG_HASH_LEN] = { };

    if (0 < cbBuffer)
    {
        hr = CrypHashBuffer(pbBuffer, cbBuffer, PROV_RSA_FULL, CALG_SHA1, rgbHash, sizeof(rgbHash));
        ExitOnFailure(hr, "Failed to hash buffer while validating file matches hash");
    }

    *pfValid = (0 == memcmp(rgbHash, pbHash, CFG_HASH_LEN));

LExit:
    return hr;
}

// Static functions
static HRESULT FindByHash(
    __in CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __out DWORD *pdwContentID,
    __out SCE_ROW_HANDLE *pSceRow
    )
{
    HRESULT hr = S_OK;
    DWORD dwContentID = 0;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRow = NULL;

    hr = SceBeginQuery(pcdb->psceDb, BINARY_CONTENT_TABLE, 1, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into binary content table");

    hr = SceSetQueryColumnBinary(sqhHandle, pbHash, CFG_HASH_LEN);
    ExitOnFailure(hr, "Failed to set query column binary for Hash column while querying binary content table");

    hr = SceRunQueryExact(&sqhHandle, &sceRow);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to run query by hash into binary content table");

    hr = SceGetColumnDword(sceRow, BINARY_ID, &dwContentID);
    ExitOnFailure(hr, "Failed to get binary ID after finding binary row by hash");

    if (NULL != pdwContentID)
    {
        *pdwContentID = dwContentID;
    }
    if (NULL != pSceRow)
    {
        *pSceRow = sceRow;
        sceRow = NULL;
    }

LExit:
    ReleaseSceQuery(sqhHandle);
    ReleaseSceRow(sceRow);

    return hr;
}
