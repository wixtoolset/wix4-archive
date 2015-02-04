//-------------------------------------------------------------------------------------------------
// <copyright file="value.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for dealing with user value data in Cfg API
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// Static function declarations
static HRESULT ValueWriteHelp(
    __in CFGDB_STRUCT *pcdb,
    __in_opt SCE_ROW_HANDLE sceRowInput,
    __in BOOL fHistory, // If TRUE, write to VALUE_INDEX_HISTORY_TABLE table - if FALSE, write to VALUE_INDEX_TABLE table
    __in DWORD dwAppID,
    __in_z LPCWSTR wzName,
    __in DWORD *pdwHistoryID,
    __inout DWORD *pdwContentID,
    __in CONFIG_VALUE *pcvValue
    );


HRESULT ValueCompare(
    __in const CONFIG_VALUE *pcvValue1,
    __in const CONFIG_VALUE *pcvValue2,
    __out BOOL *pfResult
    )
{
    HRESULT hr = S_OK;

    if (pcvValue1->cvType != pcvValue2->cvType)
    {
        *pfResult = FALSE;
        ExitFunction1(hr = S_OK);
    }

    switch (pcvValue1->cvType)
    {
    case VALUE_DELETED:
        *pfResult = TRUE;
        break;
    case VALUE_BLOB:
        *pfResult = (pcvValue1->blob.cbValue == pcvValue2->blob.cbValue && 0 == memcmp(pcvValue1->blob.rgbHash, pcvValue2->blob.rgbHash, sizeof(pcvValue2->blob.rgbHash))) ? TRUE : FALSE;
        break;
    case VALUE_STRING:
        *pfResult = (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pcvValue1->string.sczValue, -1, pcvValue2->string.sczValue, -1)) ? TRUE : FALSE;
        break;
    case VALUE_DWORD:
        *pfResult = (pcvValue1->dword.dwValue == pcvValue2->dword.dwValue) ? TRUE : FALSE;
        break;
    case VALUE_QWORD:
        *pfResult = (pcvValue1->qword.qwValue == pcvValue2->qword.qwValue) ? TRUE : FALSE;
        break;
    case VALUE_BOOL:
        *pfResult = (pcvValue1->boolean.fValue == pcvValue2->boolean.fValue) ? TRUE : FALSE;
        break;
    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid value type compared (both left and right): %d", pcvValue1->cvType);
        break;
    }

LExit:
    return hr;
}

HRESULT ValueCopy(
    __in CONFIG_VALUE *pcvInput,
    __out CONFIG_VALUE *pcvOutput
    )
{
    HRESULT hr = S_OK;

    switch (pcvInput->cvType)
    {
        case VALUE_DELETED:
            // Nothing specific to do
            break;
        case VALUE_BLOB:
            if (CFG_BLOB_DB_STREAM != pcvInput->blob.cbType)
            {
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "EnumCopy currently only supports db stream blob types");
            }
            pcvOutput->blob.cbType = CFG_BLOB_DB_STREAM;
            pcvOutput->blob.cbValue = pcvInput->blob.cbValue;
            memcpy(pcvOutput->blob.rgbHash, pcvInput->blob.rgbHash, sizeof(pcvOutput->blob.rgbHash));
            pcvOutput->blob.dbstream.pcdb = pcvInput->blob.dbstream.pcdb;
            pcvOutput->blob.dbstream.dwContentID = pcvInput->blob.dbstream.dwContentID;
            break;
        case VALUE_STRING:
            pcvOutput->string.fRelease = TRUE;
            hr = StrAllocString(&pcvOutput->string.sczValue, pcvInput->string.sczValue, 0);
            ExitOnFailure(hr, "Failed to copy string while copying value");
            break;
        case VALUE_DWORD:
            pcvOutput->dword.dwValue = pcvInput->dword.dwValue;
            break;
        case VALUE_QWORD:
            pcvOutput->qword.qwValue = pcvInput->qword.qwValue;
            break;
        case VALUE_BOOL:
            pcvOutput->boolean.fValue = pcvInput->boolean.fValue;
            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unexpected value type encountered while copying value: %d", pcvInput->cvType);
            break;
    }

    pcvOutput->cvType = pcvInput->cvType;
    pcvOutput->stWhen = pcvInput->stWhen;

    pcvOutput->fReleaseBy = TRUE;
    hr = StrAllocString(&pcvOutput->sczBy, pcvInput->sczBy, 0);
    ExitOnFailure(hr, "Failed to copy by string while copying value");

LExit:
    return hr;
}

HRESULT ValueSetDelete(
    __in_opt const SYSTEMTIME *pst,
    __in_z LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    )
{
    SYSTEMTIME st;

    pcvValue->cvType = VALUE_DELETED;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        ::GetSystemTime(&st);
        pcvValue->stWhen = st;
    }
    pcvValue->sczBy = const_cast<LPWSTR>(wzBy);
    pcvValue->fReleaseBy = FALSE;

    return S_OK;
}

HRESULT ValueSetBlob(
    __in const BYTE* pbValue,
    __in SIZE_T cbValue,
    __in BOOL fCopy,
    __in_opt const SYSTEMTIME *pst,
    __in_z LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    )
{
    HRESULT hr = S_OK;
    SYSTEMTIME st;

    hr = CrypHashBuffer(pbValue, cbValue, PROV_RSA_FULL, CALG_SHA1, pcvValue->blob.rgbHash, sizeof(pcvValue->blob.rgbHash));
    ExitOnFailure(hr, "Failed to calculate hash while setting file of size %u", cbValue);

    pcvValue->blob.cbValue = cbValue;
    pcvValue->cvType = VALUE_BLOB;
    pcvValue->blob.cbType = CFG_BLOB_POINTER;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        ::GetSystemTime(&st);
        pcvValue->stWhen = st;
    }
    pcvValue->sczBy = const_cast<LPWSTR>(wzBy);
    pcvValue->fReleaseBy = FALSE;

    if (fCopy)
    {
        pcvValue->blob.pointer.pbValue = static_cast<BYTE *>(MemAlloc(cbValue, FALSE));
        ExitOnNull(pcvValue->blob.pointer.pbValue, hr, E_OUTOFMEMORY, "Failed to allocate space for blob of size %u", cbValue);
        memcpy(const_cast<BYTE *>(pcvValue->blob.pointer.pbValue), pbValue, cbValue);
        pcvValue->blob.pointer.fRelease = true;
    }
    else
    {
        pcvValue->blob.pointer.pbValue = pbValue;
        pcvValue->cvType = VALUE_BLOB;
        pcvValue->blob.cbType = CFG_BLOB_POINTER;
        pcvValue->blob.pointer.fRelease = false;
    }

LExit:
    return hr;
}

HRESULT ValueSetBlobDbStream(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwContentID,
    __in_opt const SYSTEMTIME *pst,
    __in_z LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    )
{
    HRESULT hr = S_OK;
    SYSTEMTIME st;

    pcvValue->blob.dbstream.dwContentID = dwContentID;
    pcvValue->blob.dbstream.pcdb = pcdb;
    pcvValue->cvType = VALUE_BLOB;
    pcvValue->blob.cbType = CFG_BLOB_DB_STREAM;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        ::GetSystemTime(&st);
        pcvValue->stWhen = st;
    }
    pcvValue->sczBy = const_cast<LPWSTR>(wzBy);
    pcvValue->fReleaseBy = FALSE;

    return hr;
}

HRESULT ValueSetString(
    __in_z LPCWSTR wzValue,
    __in BOOL fCopy,
    __in_opt const SYSTEMTIME *pst,
    __in_z LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    )
{
    HRESULT hr = S_OK;
    SYSTEMTIME st;

    if (fCopy)
    {
        hr = StrAllocString(&pcvValue->string.sczValue, wzValue, 0);
        ExitOnFailure(hr, "Failed to copy string into variant");

        pcvValue->cvType = VALUE_STRING;
        pcvValue->string.fRelease = true;
    }
    else
    {
        pcvValue->string.sczValue = const_cast<LPWSTR>(wzValue);
        pcvValue->cvType = VALUE_STRING;
        pcvValue->string.fRelease = false;
    }

    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        ::GetSystemTime(&st);
        pcvValue->stWhen = st;
    }
    pcvValue->sczBy = const_cast<LPWSTR>(wzBy);
    pcvValue->fReleaseBy = FALSE;

LExit:
    return hr;
}

HRESULT ValueSetDword(
    __in DWORD dwValue,
    __in_opt const SYSTEMTIME *pst,
    __in_z LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    )
{
    SYSTEMTIME st;

    pcvValue->cvType = VALUE_DWORD;
    pcvValue->dword.dwValue = dwValue;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        ::GetSystemTime(&st);
        pcvValue->stWhen = st;
    }
    pcvValue->sczBy = const_cast<LPWSTR>(wzBy);
    pcvValue->fReleaseBy = FALSE;

    return S_OK;
}

HRESULT ValueSetQword(
    __in DWORD64 qwValue,
    __in_opt const SYSTEMTIME *pst,
    __in_z LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    )
{
    SYSTEMTIME st;

    pcvValue->cvType = VALUE_QWORD;
    pcvValue->qword.qwValue = qwValue;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        ::GetSystemTime(&st);
        pcvValue->stWhen = st;
    }
    pcvValue->sczBy = const_cast<LPWSTR>(wzBy);
    pcvValue->fReleaseBy = FALSE;

    return S_OK;
}

HRESULT ValueSetBool(
    __in BOOL fValue,
    __in_opt const SYSTEMTIME *pst,
    __in_z LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    )
{
    SYSTEMTIME st;

    pcvValue->cvType = VALUE_BOOL;
    pcvValue->boolean.fValue = fValue;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        ::GetSystemTime(&st);
        pcvValue->stWhen = st;
    }
    pcvValue->sczBy = const_cast<LPWSTR>(wzBy);
    pcvValue->fReleaseBy = FALSE;

    return S_OK;
}

HRESULT ValueWrite(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzName,
    __in CONFIG_VALUE *pcvValue,
    __in BOOL fIgnoreSameValue
    )
{
    HRESULT hr = S_OK;
    DWORD dwHistoryID = 0;
    DWORD dwContentID = 0;
    BOOL fSameValue = FALSE;
    SCE_ROW_HANDLE sceRow = NULL;
    BOOL fInSceTransaction = FALSE;
    LPWSTR sczProductName = NULL;
    CONFIG_VALUE cvExistingValue = { };
    SYSTEMTIME stNow = { };

    hr = ValueFindRow(pcdb, dwAppID, wzName, &sceRow);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to find value for AppID: %u, Value named: %ls", dwAppID, wzName);

    ::GetSystemTime(&stNow);
    if (NULL != sceRow)
    {
        hr = ValueRead(pcdb, sceRow, &cvExistingValue);
        ExitOnFailure(hr, "Failed to read existing value for value named: %ls", wzName);

        // If fIgnoreSameValue is set to true and we found an existing value row, check if we're setting to an identical value.
        // If we do, ignore it to avoid polluting history.
        if (fIgnoreSameValue)
        {
            hr = ValueCompare(pcvValue, &cvExistingValue, &fSameValue);
            ExitOnFailure(hr, "Failed to compare to existing value for value named: %ls", wzName);

            if (fSameValue)
            {
                ExitFunction1(hr = S_OK);
            }
        }

        // If current value in database is newer than current time, error out, as this can cause weird sync behavior
        if (0 >= UtilCompareSystemTimes(&stNow, &cvExistingValue.stWhen))
        {
            hr = HRESULT_FROM_WIN32(ERROR_TIME_SKEW);
            ExitOnFailure(hr, "Found already-existing future value named %ls, appID %u! Please ensure all syncing desktop machines are set to use internet time.", wzName, dwAppID);
        }
    }

    // If value's new timestamp is newer than current time, error out
    if (0 > UtilCompareSystemTimes(&stNow, &pcvValue->stWhen))
    {
        hr = HRESULT_FROM_WIN32(ERROR_TIME_SKEW);
        ExitOnFailure(hr, "Cannot write a new value from the future named %ls, appID %u! Please ensure all syncing desktop machines are set to use internet time.", wzName, dwAppID);
    }

    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction");
    fInSceTransaction = TRUE;

    hr = ValueWriteHelp(pcdb, NULL, TRUE, dwAppID, wzName, &dwHistoryID, &dwContentID, pcvValue);
    ExitOnFailure(hr, "Failed to set value in value history table (regular value set)");

    hr = ValueWriteHelp(pcdb, sceRow, FALSE, dwAppID, wzName, &dwHistoryID, &dwContentID, pcvValue);
    ExitOnFailure(hr, "Failed to set value in value table");

    // Special handling when internal settings are updated, like legacy manifests
    if (dwAppID == pcdb->dwCfgAppID)
    {
        hr = ProductIsLegacyManifestValueName(wzName, &sczProductName);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to check if value name is legacy manifest path: %ls", wzName);

            if (VALUE_BLOB == pcvValue->cvType)
            {
                hr = LogStringLine(REPORT_STANDARD, "Received new manifest for product %ls, %ls, %ls", sczProductName, wzLegacyVersion, wzLegacyPublicKey);
                ExitOnFailure(hr, "Failed to log line");

                // If it's a string value, ensure the legacy product exists
                hr = ProductEnsureCreated(pcdb, sczProductName, wzLegacyVersion, wzLegacyPublicKey, NULL, NULL);
                ExitOnFailure(hr, "Failed to set legacy product to product ID: %ls", sczProductName);

                if (!pcdb->fRemote)
                {
                    hr = BackgroundUpdateProduct(pcdb, sczProductName);
                    ExitOnFailure(hr, "Failed to notify background thread of updated manifest for product %ls", sczProductName);
                }
            }
            else
            {
                hr = LogStringLine(REPORT_STANDARD, "Forgetting product %ls, %ls, %ls", sczProductName, wzLegacyVersion, wzLegacyPublicKey);
                ExitOnFailure(hr, "Failed to log line");

                // Otherwise, ensure it is forgotten
                hr = ProductForget(pcdb, sczProductName, wzLegacyVersion, wzLegacyPublicKey);
                ExitOnFailure(hr, "Failed to forget product ID: %ls", sczProductName);
            }
        }
    }

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    fInSceTransaction = FALSE;

LExit:
    ReleaseStr(sczProductName);
    ReleaseSceRow(sceRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    ReleaseCfgValue(cvExistingValue);

    return hr;
}

HRESULT ValueRead(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE sceRow,
    __deref_out CONFIG_VALUE *pcvValue
    )
{
    HRESULT hr = S_OK;
    BYTE *pbBuffer = NULL;
    SIZE_T cbBuffer = 0;

    pcvValue->cvType = VALUE_INVALID;
    hr = SceGetColumnDword(sceRow, VALUE_COMMON_TYPE, reinterpret_cast<DWORD *>(&pcvValue->cvType));
    ExitOnFailure(hr, "Failed to get data type from value row");

    switch(pcvValue->cvType)
    {
    case VALUE_DELETED:
        // Nothing to read here
        break;
    case VALUE_BLOB:
        pcvValue->blob.cbType = CFG_BLOB_DB_STREAM;
        pcvValue->blob.dbstream.pcdb = pcdb;

        hr = SceGetColumnDword(sceRow, VALUE_COMMON_BLOBSIZE, &pcvValue->blob.cbValue);
        ExitOnFailure(hr, "Failed to get blob size from value row");

        hr = SceGetColumnBinary(sceRow, VALUE_COMMON_BLOBHASH, &pbBuffer, &cbBuffer);
        ExitOnFailure(hr, "Failed to get blob hash from value row");

        if (cbBuffer != sizeof(pcvValue->blob.rgbHash))
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Expected hash size %u, instead found hash size %u", sizeof(pcvValue->blob.rgbHash), cbBuffer);
        }
        memcpy(pcvValue->blob.rgbHash, pbBuffer, cbBuffer);

        hr = SceGetColumnDword(sceRow, VALUE_COMMON_BLOBCONTENTID, &pcvValue->blob.dbstream.dwContentID);
        ExitOnFailure(hr, "Failed to get blob content ID from value row");
        break;
    case VALUE_STRING:
        pcvValue->string.fRelease = TRUE;

        hr = SceGetColumnString(sceRow, VALUE_COMMON_STRINGVALUE, &pcvValue->string.sczValue);
        ExitOnFailure(hr, "Failed to get string value from value row");
        break;
    case VALUE_DWORD:
        hr = SceGetColumnDword(sceRow, VALUE_COMMON_LONGVALUE, &pcvValue->dword.dwValue);
        ExitOnFailure(hr, "Failed to get dword value from value row");
        break;
    case VALUE_QWORD:
        hr = SceGetColumnQword(sceRow, VALUE_COMMON_LONGLONGVALUE, &pcvValue->qword.qwValue);
        ExitOnFailure(hr, "Failed to get qword value from value row");
        break;
    case VALUE_BOOL:
        hr = SceGetColumnBool(sceRow, VALUE_COMMON_BOOLVALUE, &pcvValue->boolean.fValue);
        ExitOnFailure(hr, "Failed to get bool value from value row");
        break;
    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid value type found in database: %d", pcvValue->cvType);
        break;
    }

    hr = SceGetColumnSystemTime(sceRow, VALUE_COMMON_WHEN, &pcvValue->stWhen);
    ExitOnFailure(hr, "Failed to get 'when' field for value row");

    pcvValue->fReleaseBy = TRUE;
    hr = SceGetColumnString(sceRow, VALUE_COMMON_BY, &pcvValue->sczBy);
    ExitOnFailure(hr, "Failed to get 'by' field for value row");

LExit:
    ReleaseMem(pbBuffer);

    return hr;
}

HRESULT ValueMatch(
    __in_z LPCWSTR sczName,
    __in CFGDB_STRUCT *pcdb1,
    __in CFGDB_STRUCT *pcdb2,
    __in SCE_ROW_HANDLE sceRow1,
    __out BOOL *pfResult
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE sceRetrievedHistoryRow = NULL;
    SCE_ROW_HANDLE sceRow2 = NULL;
    int iTimeCompareResult = 0;
    CONFIG_VALUE cvValue1 = { };
    CONFIG_VALUE cvValue2 = { };
    BOOL fResult = FALSE;

    *pfResult = FALSE;

    hr = ValueFindRow(pcdb2, pcdb2->dwAppID, sczName, &sceRow2);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to find value while checking for identical values for AppID: %u, name: %ls", pcdb2->dwAppID, sczName);

    hr = ValueRead(pcdb1, sceRow1, &cvValue1);
    ExitOnFailure(hr, "Failed to read value from db 1");

    hr = ValueRead(pcdb2, sceRow2, &cvValue2);
    ExitOnFailure(hr, "Failed to read value from db 2");

    hr = ValueCompare(&cvValue1, &cvValue2, &fResult);
    ExitOnFailure(hr, "Failed to compare values");

    // If the two have the same value, check if they have different timestamps / from strings - if they do, make them match
    // by copying the newer history entry of the two over to the other one's history value
    if (fResult)
    {
        *pfResult = TRUE;

        iTimeCompareResult = UtilCompareSystemTimes(&cvValue1.stWhen, &cvValue2.stWhen);
        // if the ST1 timestamp is newer, write database 1's newest history entry to database 2
        if (0 < iTimeCompareResult)
        {
            hr = ValueWrite(pcdb2, pcdb2->dwAppID, sczName, &cvValue1, FALSE);
            ExitOnFailure(hr, "Failed to set value in value history table while matching value (1 newer than 2)");
        }
        else if (0 > iTimeCompareResult)
        {
            hr = ValueWrite(pcdb1, pcdb1->dwAppID, sczName, &cvValue2, FALSE);
            ExitOnFailure(hr, "Failed to set value in value history table while matching value (2 newer than 1)");
        }
        // If timestamps are the same and sources differ, we need to make sure both sources are persisted in both stores
        // with this timestamp (history values can have identical values and timestamps, as long as they have different sources)
        else if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, cvValue1.sczBy, -1, cvValue2.sczBy, -2))
        {
			hr = ValueFindHistoryRow(pcdb1, pcdb1->dwAppID, sczName, &cvValue2.stWhen, cvValue2.sczBy, &sceRetrievedHistoryRow);
			if (E_NOTFOUND == hr)
			{
                hr = ValueWrite(pcdb1, pcdb1->dwAppID, sczName, &cvValue2, FALSE);
                ExitOnFailure(hr, "Failed to set value in value history table while matching value (2's source missing from 1)");
			}
			else
			{
				ExitOnFailure(hr, "Failed to query for specific value find history record when matching value %ls for db1", sczName);
			}
			ReleaseNullSceRow(sceRetrievedHistoryRow);

			hr = ValueFindHistoryRow(pcdb2, pcdb2->dwAppID, sczName, &cvValue1.stWhen, cvValue1.sczBy, &sceRetrievedHistoryRow);
			if (E_NOTFOUND == hr)
			{
                hr = ValueWrite(pcdb2, pcdb2->dwAppID, sczName, &cvValue1, FALSE);
                ExitOnFailure(hr, "Failed to set value in value history table while matching value (1's source missing from 2)");
			}
			else
			{
				ExitOnFailure(hr, "Failed to query for specific value find history record when matching value %ls for db1", sczName);
			}
			ReleaseNullSceRow(sceRetrievedHistoryRow);
        }
    }

LExit:
    ReleaseSceRow(sceRetrievedHistoryRow);
    ReleaseSceRow(sceRow2);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
        *pfResult = FALSE;
    }
    ReleaseCfgValue(cvValue1);
    ReleaseCfgValue(cvValue2);

    return hr;
}

void ValueFree(
    __inout CONFIG_VALUE * pcvValue
    )
{
    if (pcvValue->fReleaseBy)
    {
        ReleaseStr(pcvValue->sczBy);
    }

    if (VALUE_STRING == pcvValue->cvType && pcvValue->string.fRelease)
    {
        ReleaseStr(pcvValue->string.sczValue);
    }
    if (VALUE_BLOB == pcvValue->cvType && CFG_BLOB_POINTER == pcvValue->blob.cbType
        && pcvValue->blob.pointer.fRelease)
    {
        ReleaseMem(const_cast<BYTE *>(pcvValue->blob.pointer.pbValue));
    }
}

HRESULT ValueFindRow(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzValueName,
    __out SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;

    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into VALUE_INDEX_TABLE table");

    hr = SceSetQueryColumnDword(sqhHandle, dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", dwAppID);

    hr = SceSetQueryColumnString(sqhHandle, wzValueName);
    ExitOnFailure(hr, "Failed to set query column string to: %ls", wzValueName);

    hr = SceRunQueryExact(&sqhHandle, pRowHandle);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to query for value appID: %u, named: %ls", dwAppID, wzValueName);

LExit:
    ReleaseSceQuery(sqhHandle);

    return hr;
}

HRESULT ValueFindHistoryRow(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzValueName,
    __in const SYSTEMTIME *pWhen,
    __in LPCWSTR wzBy,
    __out SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;

    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_HISTORY_TABLE, 1, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into VALUE_INDEX_HISTORY_TABLE table");

    hr = SceSetQueryColumnDword(sqhHandle, dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", dwAppID);

    hr = SceSetQueryColumnString(sqhHandle, wzValueName);
    ExitOnFailure(hr, "Failed to set query column string to: %ls", wzValueName);

    hr = SceSetQueryColumnSystemTime(sqhHandle, pWhen);
    ExitOnFailure(hr, "Failed to set query column timestamp");

    hr = SceSetQueryColumnString(sqhHandle, wzBy);
    ExitOnFailure(hr, "Failed to set query column string");

    hr = SceRunQueryExact(&sqhHandle, pRowHandle);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to query for value appID: %u, named: %ls, by: %ls", dwAppID, wzValueName, wzBy);

LExit:
    ReleaseSceQuery(sqhHandle);

    return hr;
}

// Static functions

// Helper function for other ValueWrite functions - call this after starting your own transaction
// For blob types, writes (or refcounts) the underlyling stream only when fHistory is set to TRUE
HRESULT ValueWriteHelp(
    __in CFGDB_STRUCT *pcdb,
    __in_opt SCE_ROW_HANDLE sceRowInput,
    __in BOOL fHistory, // If TRUE, write to VALUE_INDEX_HISTORY_TABLE table - if FALSE, write to VALUE_INDEX_TABLE table
    __in DWORD dwAppID,
    __in_z LPCWSTR wzName,
    __inout DWORD *pdwHistoryID,
    __inout DWORD *pdwContentID,
    __in CONFIG_VALUE *pcvValue
    )
{
    HRESULT hr = S_OK;
    DWORD dwTableIndex = fHistory ? VALUE_INDEX_HISTORY_TABLE : VALUE_INDEX_TABLE;
    SCE_ROW_HANDLE sceRow = NULL;

    if (fHistory || NULL == sceRowInput)
    {
        hr = ScePrepareInsert(pcdb->psceDb, dwTableIndex, &sceRow);
        ExitOnFailure(hr, "Failed to prepare for insert");

        sceRowInput = sceRow;
    }

    hr = SceSetColumnDword(sceRowInput, VALUE_COMMON_APPID, dwAppID);
    ExitOnFailure(hr, "Failed to set AppID column to: %u", dwAppID);

    hr = SceSetColumnString(sceRowInput, VALUE_COMMON_NAME, wzName);
    ExitOnFailure(hr, "Failed to set name column to: %ls", wzName);

    hr = SceSetColumnDword(sceRowInput, VALUE_COMMON_TYPE, pcvValue->cvType);
    ExitOnFailure(hr, "Failed to set type column to: %d", pcvValue->cvType);

    if (VALUE_BLOB == pcvValue->cvType)
    {
        // Only refcount the underlying stream when writing the history entry
        if (fHistory)
        {
            switch (pcvValue->blob.cbType)
            {
            case CFG_BLOB_POINTER:
                hr = StreamWrite(pcdb, static_cast<BYTE *>(pcvValue->blob.rgbHash), pcvValue->blob.pointer.pbValue, pcvValue->blob.cbValue, pdwContentID);
                ExitOnFailure(hr, "Failed to write stream while setting value %ls", wzName);
                break;
            case CFG_BLOB_DB_STREAM:
                if (pcvValue->blob.dbstream.pcdb == pcdb)
                {
                    // Same database, just refcount it
                    *pdwContentID = pcvValue->blob.dbstream.dwContentID;
                    hr = StreamIncreaseRefcount(pcdb, pcvValue->blob.dbstream.dwContentID, 1);
                    ExitOnFailure(hr, "Failed to increase refcount for stream ID %u while setting value %ls", pcvValue->blob.dbstream.dwContentID, wzName);
                }
                else
                {
                    hr = StreamCopy(pcvValue->blob.dbstream.pcdb, pcvValue->blob.dbstream.dwContentID, pcdb, pdwContentID);
                    ExitOnFailure(hr, "Failed to copy stream %u from one database to another while setting value %ls", pcvValue->blob.dbstream.dwContentID, wzName);
                }
                break;
            default:
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Invalid blob type encountered while writing value: %d", pcvValue->blob.cbType);
                break;
            }   
        }

        hr = SceSetColumnDword(sceRowInput, VALUE_COMMON_BLOBSIZE, pcvValue->blob.cbValue);
        ExitOnFailure(hr, "Failed to set blob size from value row");

        hr = SceSetColumnBinary(sceRowInput, VALUE_COMMON_BLOBHASH, static_cast<BYTE *>(pcvValue->blob.rgbHash), sizeof(pcvValue->blob.rgbHash));
        ExitOnFailure(hr, "Failed to set blob hash from value row");

        hr = SceSetColumnDword(sceRowInput, VALUE_COMMON_BLOBCONTENTID, *pdwContentID);
        ExitOnFailure(hr, "Failed to set blob content ID from value row");
    }
    else
    {
        hr = SceSetColumnNull(sceRowInput, VALUE_COMMON_BLOBSIZE);
        ExitOnFailure(hr, "Failed to set empty blob size column");

        hr = SceSetColumnNull(sceRowInput, VALUE_COMMON_BLOBHASH);
        ExitOnFailure(hr, "Failed to set empty blob hash column");

        hr = SceSetColumnNull(sceRowInput, VALUE_COMMON_BLOBCONTENTID);
        ExitOnFailure(hr, "Failed to set empty blob content ID column");
    }

    if (VALUE_STRING == pcvValue->cvType && NULL != pcvValue->string.sczValue)
    {
        hr = SceSetColumnString(sceRowInput, VALUE_COMMON_STRINGVALUE, pcvValue->string.sczValue);
        ExitOnFailure(hr, "Failed to set value column to: %ls", pcvValue->string.sczValue);
    }
    else
    {
        hr = SceSetColumnString(sceRowInput, VALUE_COMMON_STRINGVALUE, L"");
        ExitOnFailure(hr, "Failed to set empty string value column");
    }

    if (VALUE_DWORD == pcvValue->cvType)
    {
        hr = SceSetColumnDword(sceRowInput, VALUE_COMMON_LONGVALUE, pcvValue->dword.dwValue);
        ExitOnFailure(hr, "Failed to set value column to: %u", pcvValue->dword.dwValue);
    }
    else
    {
        hr = SceSetColumnDword(sceRowInput, VALUE_COMMON_LONGVALUE, 0);
        ExitOnFailure(hr, "Failed to set DWORD value column to nothing");
    }

    if (VALUE_QWORD == pcvValue->cvType)
    {
        hr = SceSetColumnQword(sceRowInput, VALUE_COMMON_LONGLONGVALUE, pcvValue->qword.qwValue);
        ExitOnFailure(hr, "Failed to set value column to: %I64u", pcvValue->qword.qwValue);
    }
    else
    {
        hr = SceSetColumnQword(sceRowInput, VALUE_COMMON_LONGLONGVALUE, 0);
        ExitOnFailure(hr, "Failed to set QWORD value column to nothing");
    }

    if (VALUE_BOOL == pcvValue->cvType)
    {
        hr = SceSetColumnBool(sceRowInput, VALUE_COMMON_BOOLVALUE, pcvValue->boolean.fValue);
        ExitOnFailure(hr, "Failed to set value column to: %ls", pcvValue->boolean.fValue ? L"TRUE" : L"FALSE");
    }
    else
    {
        hr = SceSetColumnBool(sceRowInput, VALUE_COMMON_BOOLVALUE, FALSE);
        ExitOnFailure(hr, "Failed to set BOOL value column to FALSE");
    }

    hr = SceSetColumnSystemTime(sceRowInput, VALUE_COMMON_WHEN, &pcvValue->stWhen);
    ExitOnFailure(hr, "Failed to set 'date' in value index history");

    hr = SceSetColumnString(sceRowInput, VALUE_COMMON_BY, pcvValue->sczBy);
    ExitOnFailure(hr, "Failed to set 'by' field in value index history");

    if (!fHistory)
    {
        hr = SceSetColumnDword(sceRowInput, VALUE_LAST_HISTORY_ID, *pdwHistoryID);
        ExitOnFailure(hr, "Failed to set last history ID to value: %u", *pdwHistoryID);
    }

    hr = SceFinishUpdate(sceRowInput);
    ExitOnFailure(hr, "Failed to finish update");

    if (fHistory)
    {
        hr = SceGetColumnDword(sceRowInput, VALUE_COMMON_ID, pdwHistoryID);
        ExitOnFailure(hr, "Failed to read ID of new history entry");
    }

LExit:
    ReleaseSceRow(sceRow);

    return hr;
}

HRESULT ValueForget(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __inout SCE_ROW_HANDLE *psceValueRow
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_RESULTS_HANDLE sqrhResults = NULL;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceValueHistoryRow = NULL;
    LPWSTR sczValueName = NULL;
    CONFIG_VALUETYPE cvType = VALUE_INVALID;
    DWORD dwContentID = 0;
    BOOL fInSceTransaction = FALSE;

    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction");
    fInSceTransaction = TRUE;

    hr = SceGetColumnString(*psceValueRow, VALUE_COMMON_NAME, &sczValueName);
    ExitOnFailure(hr, "Failed to get value name to forget value");

    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_HISTORY_TABLE, 1, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into value table");

    hr = SceSetQueryColumnDword(sqhHandle, dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", dwAppID);

    hr = SceSetQueryColumnString(sqhHandle, sczValueName);
    ExitOnFailure(hr, "Failed to set query column string to: %ls", sczValueName);

    hr = SceRunQueryRange(&sqhHandle, &sqrhResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query");

    hr = SceDeleteRow(psceValueRow);
    ExitOnFailure(hr, "Failed to delete value row for value named: %ls", sczValueName);

    hr = SceGetNextResultRow(sqrhResults, &sceValueHistoryRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next result row");

        hr = SceGetColumnDword(sceValueHistoryRow, VALUE_COMMON_TYPE, reinterpret_cast<DWORD*>(&cvType));
        ExitOnFailure(hr, "Failed to get value type of value history for value named: %ls", sczValueName);

        if (VALUE_BLOB == cvType)
        {
            hr = SceGetColumnDword(sceValueHistoryRow, VALUE_COMMON_BLOBCONTENTID, &dwContentID);
            ExitOnFailure(hr, "Failed to get content ID of value history for value named: %ls", sczValueName);
        }

        hr = SceDeleteRow(&sceValueHistoryRow);
        ExitOnFailure(hr, "Failed to delete history row for value named: %ls", sczValueName);

        if (VALUE_BLOB == cvType)
        {
            // Refcounts are only counted for history entries
            hr = StreamDecreaseRefcount(pcdb, dwContentID, 1);
            ExitOnFailure(hr, "Failed to decrease refcount of content with ID: %u", dwContentID);
        }

        ReleaseNullSceRow(sceValueHistoryRow);
        hr = SceGetNextResultRow(sqrhResults, &sceValueHistoryRow);
    }
    hr = S_OK;

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    fInSceTransaction = FALSE;

LExit:
    ReleaseStr(sczValueName);
    ReleaseSceQuery(sqhHandle);
    ReleaseSceQueryResults(sqrhResults);
    ReleaseSceRow(sceValueHistoryRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }

    return hr;
}
