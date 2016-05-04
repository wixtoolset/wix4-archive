// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
    __in CONFIG_VALUE *pcvValue,
    __in CFGDB_STRUCT *pcdbReferencedBy
    );
static HRESULT ExpireOldRows(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzValueName,
    __in DWORD *rgAllHistoryIDs,
    __in DWORD cAllHistoryIDs
    );
static HRESULT ForgetHistoryRow(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE *pValueHistoryRow,
    __in_z LPCWSTR wzValueName
    );
static BOOL ReferencedByStringContainsId(
    __in_z_opt LPCWSTR wzReferencedBy,
    __in_z LPCWSTR wzId
    );
// Goes through entire history for the value. If pcdbReferencedBy is specified, removes any references to wzId,
// excluding the exact history entry that matches the value valueToKeepReference.
// Whether pcdbReferencedBy is specified or not, clean up old history entries
static HRESULT RemoveOutdatedReferencesFromDatabase(
    __in CFGDB_STRUCT *pcdb,
    __in const CONFIG_VALUE *pValueToKeepReference,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzValueName,
    __in_opt CFGDB_STRUCT *pcdbReferencedBy
    );
// Reads a value out of sceValueHistoryRow parameter, and passes it on to RemoveOutdatedReferencesFromDatabase()
// sceValueHistoryRow
static HRESULT HistoryRowRemoveReferencesFromDatabase(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE sceValueHistoryRow,
    __in CFGDB_STRUCT *pcdbReferencedBy
    );
// Ensures that pcdbOther's guid id is present or not present in the VALUE_HISTORY_DB_REFERENCES column of the row
static HRESULT HistoryRowEnsureReferenceState(
    __in CFGDB_STRUCT *pcdb,
    __in CFGDB_STRUCT *pcdbOther,
    __in SCE_ROW_HANDLE sceValueHistoryRow,
    __in BOOL fDesiredState
    );
static HRESULT FindHistoryRow(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzValueName,
    __in const SYSTEMTIME *pWhen,
    __in LPCWSTR wzBy,
    __out SCE_ROW_HANDLE *pRowHandle
    );
static HRESULT FindHistoryRowById(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwHistoryId,
    __out SCE_ROW_HANDLE *pRowHandle
    );

HRESULT ValueCompare(
    __in const CONFIG_VALUE *pcvValue1,
    __in const CONFIG_VALUE *pcvValue2,
    __in BOOL fCompareSource,
    __out BOOL *pfResult
    )
{
    HRESULT hr = S_OK;
    BOOL fResult = FALSE;

    if (pcvValue1->cvType != pcvValue2->cvType)
    {
        *pfResult = FALSE;
        ExitFunction1(hr = S_OK);
    }

    switch (pcvValue1->cvType)
    {
    case VALUE_DELETED:
        fResult = TRUE;
        break;
    case VALUE_BLOB:
        fResult = (pcvValue1->blob.cbValue == pcvValue2->blob.cbValue && 0 == memcmp(pcvValue1->blob.rgbHash, pcvValue2->blob.rgbHash, sizeof(pcvValue2->blob.rgbHash))) ? TRUE : FALSE;
        break;
    case VALUE_STRING:
        fResult = (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pcvValue1->string.sczValue, -1, pcvValue2->string.sczValue, -1)) ? TRUE : FALSE;
        break;
    case VALUE_DWORD:
        fResult = (pcvValue1->dword.dwValue == pcvValue2->dword.dwValue) ? TRUE : FALSE;
        break;
    case VALUE_QWORD:
        fResult = (pcvValue1->qword.qwValue == pcvValue2->qword.qwValue) ? TRUE : FALSE;
        break;
    case VALUE_BOOL:
        fResult = (pcvValue1->boolean.fValue == pcvValue2->boolean.fValue) ? TRUE : FALSE;
        break;
    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid value type compared (both left and right): %d", pcvValue1->cvType);
        break;
    }

    if (fResult && fCompareSource)
    {
        if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, pcvValue1->sczBy, -1, pcvValue2->sczBy, -1) || 0 != UtilCompareSystemTimes(&pcvValue1->stWhen, &pcvValue2->stWhen))
        {
            fResult = FALSE;
        }
    }

    *pfResult = fResult;

LExit:
    return hr;
}

HRESULT ValueCopy(
    __in CONFIG_VALUE *pcvInput,
    __out CONFIG_VALUE *pcvOutput
    )
{
    HRESULT hr = S_OK;

    ReleaseNullCfgValue(*pcvOutput);

    switch (pcvInput->cvType)
    {
        case VALUE_DELETED:
            // Nothing specific to do
            break;
        case VALUE_BLOB:
            if (CFG_BLOB_DB_STREAM != pcvInput->blob.cbType)
            {
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "ValueCopy currently only supports db stream blob types");
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

    ReleaseNullCfgValue(*pcvValue);

    pcvValue->cvType = VALUE_DELETED;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        UtilGetSystemTime(&st);
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

    ReleaseNullCfgValue(*pcvValue);

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
        UtilGetSystemTime(&st);
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

    ReleaseNullCfgValue(*pcvValue);

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
        UtilGetSystemTime(&st);
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

    ReleaseNullCfgValue(*pcvValue);

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
        UtilGetSystemTime(&st);
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

    ReleaseNullCfgValue(*pcvValue);

    pcvValue->cvType = VALUE_DWORD;
    pcvValue->dword.dwValue = dwValue;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        UtilGetSystemTime(&st);
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

    ReleaseNullCfgValue(*pcvValue);

    pcvValue->cvType = VALUE_QWORD;
    pcvValue->qword.qwValue = qwValue;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        UtilGetSystemTime(&st);
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

    ReleaseNullCfgValue(*pcvValue);

    pcvValue->cvType = VALUE_BOOL;
    pcvValue->boolean.fValue = fValue;
    if (pst)
    {
        pcvValue->stWhen = *pst;
    }
    else
    {
        UtilGetSystemTime(&st);
        pcvValue->stWhen = st;
    }
    pcvValue->sczBy = const_cast<LPWSTR>(wzBy);
    pcvValue->fReleaseBy = FALSE;

    return S_OK;
}

HRESULT ValueTransferFromHistory(
    __in CFGDB_STRUCT *pcdb,
    __in const CFG_ENUMERATION *pceValueHistoryEnum,
    __in DWORD dwStartingEnumIndex,
    __in CFGDB_STRUCT *pcdbReferencedBy
    )
{
    HRESULT hr = S_OK;
    DWORD dwHistoryId = 0;
    DWORD dwLastEnumIndex = pceValueHistoryEnum->dwNumValues - 1;
    LPWSTR wzValueName = pceValueHistoryEnum->valueHistory.sczName;
    SYSTEMTIME stValue = { };
    BOOL fValueExists = FALSE;
    BOOL fLastValue = FALSE;
    BOOL fInSceTransaction = FALSE;
    BOOL fReferencedByInSceTransaction = FALSE;
    SCE_ROW_HANDLE sceValueRow = NULL;
    SCE_ROW_HANDLE sceValueHistoryRow = NULL;

    if (dwStartingEnumIndex >= pceValueHistoryEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Incorrect starting enum index passed to ValueTransferFromHistory()");
    }

    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction");
    fInSceTransaction = TRUE;

    hr = ValueFindRow(pcdb, pcdb->dwAppID, wzValueName, &sceValueRow);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    else
    {
        ExitOnFailure(hr, "Failed to find existing value row");
        fValueExists = TRUE;

        hr = SceGetColumnSystemTime(sceValueRow, VALUE_COMMON_WHEN, &stValue);
        ExitOnFailure(hr, "Failed to get system time of value row");

        // Get the latest good history row
        hr = SceGetColumnDword(sceValueRow, VALUE_LAST_HISTORY_ID, &dwHistoryId);
        ExitOnFailure(hr, "Failed to get last history ID from value row");

        hr = FindHistoryRowById(pcdb, dwHistoryId, &sceValueHistoryRow);
        ExitOnFailure(hr, "Failed to get history row by ID");

        hr = HistoryRowEnsureReferenceState(pcdb, pcdbReferencedBy, sceValueHistoryRow, FALSE);
        ExitOnFailure(hr, "Failed to clear reference state of database");
        ReleaseNullSceRow(sceValueHistoryRow);
    }

    for (DWORD i = dwStartingEnumIndex; i < pceValueHistoryEnum->dwNumValues; ++i)
    {
        fLastValue = (i == pceValueHistoryEnum->dwNumValues - 1);

        if (fValueExists && 0 >= UtilCompareSystemTimes(&pceValueHistoryEnum->valueHistory.rgcValues[i].stWhen, &stValue))
        {
            // If we're not on the last loop iteration, just don't transfer this enum
            // TODO: we could write historical values by inserting them in the old history, someday if we have a separate timestamp for arrival-at-this-db time vs original-modified-time
            if (!fLastValue)
            {
                continue;
            }

            pceValueHistoryEnum->valueHistory.rgcValues[i].stWhen = stValue;
            UtilAddToSystemTime(1, &pceValueHistoryEnum->valueHistory.rgcValues[i].stWhen);

            // Since we changed the timestamp, make sure the updated timestamp appears in both databases
            hr = ValueWrite(pcdbReferencedBy, pcdbReferencedBy->dwAppID, wzValueName, pceValueHistoryEnum->valueHistory.rgcValues + i, FALSE, pcdb);
            ExitOnFailure(hr, "Failed to write value in referenced by %ls index %u", wzValueName, i);
        }
        
        // Make sure to set the referenced by column for the last value only
        hr = EnumWriteValue(pcdb, wzValueName, pceValueHistoryEnum, i, fLastValue ? pcdbReferencedBy : NULL);
        ExitOnFailure(hr, "Failed to write value %ls index %u", wzValueName, i);
    }

    hr = SceBeginTransaction(pcdbReferencedBy->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction in referenced by db");
    fReferencedByInSceTransaction = TRUE;

    hr = FindHistoryRow(pcdbReferencedBy, pcdbReferencedBy->dwAppID, wzValueName, &pceValueHistoryEnum->valueHistory.rgcValues[dwLastEnumIndex].stWhen, pceValueHistoryEnum->valueHistory.rgcValues[dwLastEnumIndex].sczBy, &sceValueHistoryRow);
    ExitOnFailure(hr, "Failed to find value history row in referenced db");

    // Make sure the database the value came from knows about the latest value in the db we wrote to
    hr = HistoryRowEnsureReferenceState(pcdbReferencedBy, pcdb, sceValueHistoryRow, TRUE);
    ExitOnFailure(hr, "Failed to set reference state of database");

    hr = SceCommitTransaction(pcdbReferencedBy->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    fReferencedByInSceTransaction = FALSE;

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    fInSceTransaction = FALSE;

LExit:
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    if (fReferencedByInSceTransaction)
    {
        SceRollbackTransaction(pcdbReferencedBy->psceDb);
    }
    ReleaseSceRow(sceValueRow);
    ReleaseSceRow(sceValueHistoryRow);

    return hr;
}

HRESULT ValueWrite(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzName,
    __in CONFIG_VALUE *pcvValue,
    __in BOOL fIgnoreSameValue,
    __in_opt CFGDB_STRUCT *pcdbReferencedBy
    )
{
    HRESULT hr = S_OK;
    DWORD dwHistoryID = 0;
    DWORD dwContentID = 0;
    int iCompareResult = 0;
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

    UtilGetSystemTime(&stNow);
    if (NULL != sceRow)
    {
        hr = ValueRead(pcdb, sceRow, &cvExistingValue);
        ExitOnFailure(hr, "Failed to read existing value for value named: %ls", wzName);

        // If fIgnoreSameValue is set to true and we found an existing value row, check if we're setting to an identical value.
        // If we do, ignore it to avoid polluting history.
        if (fIgnoreSameValue)
        {
            hr = ValueCompare(pcvValue, &cvExistingValue, FALSE, &fSameValue);
            ExitOnFailure(hr, "Failed to compare to existing value for value named: %ls", wzName);

            if (fSameValue)
            {
                ExitFunction1(hr = S_OK);
            }
        }

        // If current value in database is newer than or exactly the same as current time, error out, as this can cause weird sync behavior
        if (0 >= UtilCompareSystemTimes(&stNow, &cvExistingValue.stWhen))
        {
            hr = HRESULT_FROM_WIN32(ERROR_TIME_SKEW);
            ExitOnFailure(hr, "Found already-existing future value named %ls, appID %u! Please ensure all syncing desktop machines are set to use internet time.", wzName, dwAppID);
        }

        // If new value is not newer than latest value's timestamp, error out, as this can cause last value index history table row to not match the value in value table,
        // and we expect these to be in order. Allow same time values from different sources, as their order doesn't matter.
        iCompareResult = UtilCompareSystemTimes(&pcvValue->stWhen, &cvExistingValue.stWhen);
        if (0 > iCompareResult)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_TIME);
            ExitOnFailure(hr, "Tried to set older time or same time with same value named %ls, appid %u!", wzName, dwAppID);
        }
        else if (0 == iCompareResult && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pcvValue->sczBy, -1, cvExistingValue.sczBy, -1))
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_TIME);
            ExitOnFailure(hr, "Tried to set same time with same source named %ls, appid %u!", wzName, dwAppID);
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

    hr = ValueWriteHelp(pcdb, NULL, TRUE, dwAppID, wzName, &dwHistoryID, &dwContentID, pcvValue, pcdbReferencedBy);
    ExitOnFailure(hr, "Failed to set value in value history table (regular value set)");

    hr = ValueWriteHelp(pcdb, sceRow, FALSE, dwAppID, wzName, &dwHistoryID, &dwContentID, pcvValue, pcdbReferencedBy);
    ExitOnFailure(hr, "Failed to set value in value table");

    // Now clean up old references that are out of date in the database we wrote to
    hr = RemoveOutdatedReferencesFromDatabase(pcdb, pcvValue, dwAppID, wzName, pcdbReferencedBy);
    ExitOnFailure(hr, "Failed to remove outdated references from database");

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
    __in_z LPCWSTR wzName,
    __in CFGDB_STRUCT *pcdb1,
    __in CFGDB_STRUCT *pcdb2,
    __in SCE_ROW_HANDLE sceRow1,
    __out BOOL *pfResult
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE sceRetrievedHistoryRow = NULL;
    SCE_ROW_HANDLE sceRow2 = NULL;
    SCE_ROW_HANDLE sceHistoryRow1 = NULL;
    SCE_ROW_HANDLE sceHistoryRow2 = NULL;
    DWORD dwHistoryId = 0;
    int iTimeCompareResult = 0;
    CONFIG_VALUE cvValue1 = { };
    CONFIG_VALUE cvValue2 = { };
    BOOL fResult = FALSE;
    BOOL fAlreadyWrittenReferenceState = FALSE;

    *pfResult = FALSE;

    hr = ValueFindRow(pcdb2, pcdb2->dwAppID, wzName, &sceRow2);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to find value while checking for identical values for AppID: %u, name: %ls", pcdb2->dwAppID, wzName);

    hr = ValueRead(pcdb1, sceRow1, &cvValue1);
    ExitOnFailure(hr, "Failed to read value from db 1");

    hr = SceGetColumnDword(sceRow1, VALUE_LAST_HISTORY_ID, &dwHistoryId);
    ExitOnFailure(hr, "Failed to get last history id from db 1");

    hr = FindHistoryRowById(pcdb1, dwHistoryId, &sceHistoryRow1);
    ExitOnFailure(hr, "Failed to find history row by id in db 1");

    hr = ValueRead(pcdb2, sceRow2, &cvValue2);
    ExitOnFailure(hr, "Failed to read value from db 2");

    hr = SceGetColumnDword(sceRow2, VALUE_LAST_HISTORY_ID, &dwHistoryId);
    ExitOnFailure(hr, "Failed to get last history id from db 2");

    hr = FindHistoryRowById(pcdb2, dwHistoryId, &sceHistoryRow2);
    ExitOnFailure(hr, "Failed to find history row by id in db 2");

    hr = ValueCompare(&cvValue1, &cvValue2, FALSE, &fResult);
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
            hr = ValueWrite(pcdb2, pcdb2->dwAppID, wzName, &cvValue1, FALSE, pcdb1);
            ExitOnFailure(hr, "Failed to set value in value history table while matching value (1 newer than 2)");

            hr = HistoryRowEnsureReferenceState(pcdb1, pcdb2, sceHistoryRow1, TRUE);
            ExitOnFailure(hr, "Failed to ensure reference state of value (1 newer than 2)");
        }
        else if (0 > iTimeCompareResult)
        {
            hr = ValueWrite(pcdb1, pcdb1->dwAppID, wzName, &cvValue2, FALSE, pcdb2);
            ExitOnFailure(hr, "Failed to set value in value history table while matching value (2 newer than 1)");

            hr = HistoryRowEnsureReferenceState(pcdb2, pcdb1, sceHistoryRow2, TRUE);
            ExitOnFailure(hr, "Failed to ensure reference state of value (2 newer than 1)");
        }
        // If timestamps are the same and sources differ, we need to make sure both sources are persisted in both stores
        // with this timestamp (history values can have identical values and timestamps, as long as they have different sources)
        else if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, cvValue1.sczBy, -1, cvValue2.sczBy, -1))
        {
            fAlreadyWrittenReferenceState = FALSE;

            hr = FindHistoryRow(pcdb1, pcdb1->dwAppID, wzName, &cvValue2.stWhen, cvValue2.sczBy, &sceRetrievedHistoryRow);
            if (E_NOTFOUND == hr)
            {
                hr = ValueWrite(pcdb1, pcdb1->dwAppID, wzName, &cvValue2, FALSE, pcdb2);
                ExitOnFailure(hr, "Failed to set value in value history table while matching value (2's source missing from 1)");

                hr = HistoryRowEnsureReferenceState(pcdb1, pcdb2, sceHistoryRow1, TRUE);
                ExitOnFailure(hr, "Failed to ensure reference state of value (2's source missing from 1)");
                fAlreadyWrittenReferenceState = TRUE;
            }
            else
            {
                ExitOnFailure(hr, "Failed to query for specific value find history record when matching value %ls for db1", wzName);
            }
            ReleaseNullSceRow(sceRetrievedHistoryRow);

            hr = FindHistoryRow(pcdb2, pcdb2->dwAppID, wzName, &cvValue1.stWhen, cvValue1.sczBy, &sceRetrievedHistoryRow);
            if (E_NOTFOUND == hr)
            {
                hr = ValueWrite(pcdb2, pcdb2->dwAppID, wzName, &cvValue1, FALSE, pcdb1);
                ExitOnFailure(hr, "Failed to set value in value history table while matching value (1's source missing from 2)");

                // Only need one reference between databases
                if (!fAlreadyWrittenReferenceState)
                {
                    hr = HistoryRowEnsureReferenceState(pcdb2, pcdb1, sceHistoryRow2, TRUE);
                    ExitOnFailure(hr, "Failed to ensure reference state of value (1's source missing from 2)");
                }
            }
            else
            {
                ExitOnFailure(hr, "Failed to query for specific value find history record when matching value %ls for db1", wzName);
            }
            ReleaseNullSceRow(sceRetrievedHistoryRow);
        }
        // Timestamps are the same and sources are the same, so just ensure references are correct
        else
        {
            // Don't bother to cleanup old references in this case, because there are only two cases we should hit this:
            // A) References have never been set before (user just upgraded database to this version), so there are none to cleanup
            // B) References are already set on these two values (there should be no references to cleanup)

            hr = HistoryRowEnsureReferenceState(pcdb1, pcdb2, sceHistoryRow1, TRUE);
            ExitOnFailure(hr, "Failed to ensure reference state of value in 1 (identical source)");

            hr = HistoryRowEnsureReferenceState(pcdb2, pcdb1, sceHistoryRow2, TRUE);
            ExitOnFailure(hr, "Failed to ensure reference state of value in 2 (identical source)");

            // Do look and see if any old history entries can be removed in both databases.
            hr = RemoveOutdatedReferencesFromDatabase(pcdb1, &cvValue1, pcdb1->dwAppID, wzName, NULL);
            ExitOnFailure(hr, "Failed to remove outdated history entries from database 1");

            hr = RemoveOutdatedReferencesFromDatabase(pcdb2, &cvValue2, pcdb2->dwAppID, wzName, NULL);
            ExitOnFailure(hr, "Failed to remove outdated history entries from database 2");
        }
    }

LExit:
    ReleaseSceRow(sceHistoryRow1);
    ReleaseSceRow(sceHistoryRow2);
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

        hr = ForgetHistoryRow(pcdb, &sceValueHistoryRow, sczValueName);
        ExitOnFailure(hr, "Failed to forget value history row");

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
    __in CONFIG_VALUE *pcvValue,
    __in CFGDB_STRUCT *pcdbReferencedBy
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
    else
    {
        if (pcdbReferencedBy)
        {
            if (pcdb->fRemote)
            {
                hr = SceSetColumnString(sceRowInput, VALUE_HISTORY_DB_REFERENCES, pcdb->sczGuidLocalInRemoteKey);
                ExitOnFailure(hr, "Failed to set local guid key in remote database");
            }
            else
            {
                hr = SceSetColumnString(sceRowInput, VALUE_HISTORY_DB_REFERENCES, pcdbReferencedBy->sczGuidRemoteInLocalKey);
                ExitOnFailure(hr, "Failed to set remote guid key in local database");
            }
        }
        else
        {
            hr = SceSetColumnNull(sceRowInput, VALUE_HISTORY_DB_REFERENCES);
            ExitOnFailure(hr, "Failed to set db references to null");
        }
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

static HRESULT ExpireOldRows(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzValueName,
    __in DWORD *rgAllHistoryIDs,
    __in DWORD cAllHistoryIDs
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    BOOL fHasReferences;
    BOOL fKeepThisValue;
    DWORD cKeptHistoryRows = cAllHistoryIDs;
    LPWSTR sczReferencedBy = NULL;
    LONGLONG llTimeDiffFromNow = 0;
    LONGLONG llTimeDiffFromLastKept = 0;
    LONGLONG llTimeDiffFromFirst = 0;
    SCE_ROW_HANDLE valueHistoryRow = NULL;
    SYSTEMTIME stNow = { };
    SYSTEMTIME stCurrent = { };
    SYSTEMTIME stLastKept = { };
    SYSTEMTIME stFirst = { };

    // Snap time
    UtilGetSystemTime(&stNow);

    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction");
    fInSceTransaction = TRUE;

    // Go through history rows from the newest to oldest
    // Do not delete index zero, because there is no sense expiring the newest history value, we are not changing the current value just expiring old things
    for (LONGLONG i = cAllHistoryIDs - 1; i >= 0; --i)
    {
        ReleaseNullSceRow(valueHistoryRow);
        hr = FindHistoryRowById(pcdb, rgAllHistoryIDs[i], &valueHistoryRow);
        ExitOnFailure(hr, "Failed to get history row with ID %u", rgAllHistoryIDs[i]);

        hr = SceGetColumnSystemTime(valueHistoryRow, VALUE_COMMON_WHEN, &stCurrent);
        ExitOnFailure(hr, "Failed to get system time of row with index %I64d", i);

        // Remember the timestamp of the very latest value
        if (i == cAllHistoryIDs - 1)
        {
            stFirst = stCurrent;
            stLastKept = stCurrent;
            continue;
        }

        // We cannot do any actions on the first value in the array, or any other value with an equivalent timestamp. Since these records are all sorted by timestamp descending,
        // Any value with the latest timestamp may be the actual one that is pointed to by the value table.
        hr = UtilSubtractSystemTimes(&stFirst, &stCurrent, &llTimeDiffFromFirst);
        if (llTimeDiffFromFirst == 0)
        {
            continue;   
        }

        hr = SceGetColumnString(valueHistoryRow, VALUE_HISTORY_DB_REFERENCES, &sczReferencedBy);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;

            fHasReferences = FALSE;
        }
        else
        {
            ExitOnFailure(hr, "Failed to get references of row with index %I64d", i);

            fHasReferences = TRUE;
        }

        hr = UtilSubtractSystemTimes(&stNow, &stCurrent, &llTimeDiffFromNow);
        ExitOnFailure(hr, "Failed to subtract system times");

        hr = UtilSubtractSystemTimes(&stLastKept, &stCurrent, &llTimeDiffFromLastKept);
        ExitOnFailure(hr, "Failed to subtract system times");

        // TODO: kill off ancient values without creating conflicts
        // If it's newer than that and has any references at all, keep it to avoid sync conflicts.
        // There is a chance we have references to a remote database we will never see again, but even those will go away eventually in the above if block.
        if (fHasReferences)
        {
            fKeepThisValue = TRUE;
        }
        // If it has no references but we don't have many history entries left for this value, try to keep them so the user has some history to revert back to.
        else if (cKeptHistoryRows <= 5)
        {
            fKeepThisValue = TRUE;
        }
        // If it's older than 30 days, keep values that are at least 30 days apart, and discard ones closer together
        else if (llTimeDiffFromNow > 60 * 60 * 24 * 30)
        {
            fKeepThisValue = (llTimeDiffFromLastKept >= 60 * 60 * 24 * 30);
        }
        // If it's older than 2 weeks, keep values that are at least 3 day apart, and discard ones closer together
        else if (llTimeDiffFromNow > 60 * 60 * 24 * 14)
        {
            fKeepThisValue = (llTimeDiffFromLastKept >= 60 * 60 * 24 * 3);
        }
        // If it's older than a week, keep values that are at least 1 day apart, and discard ones closer together
        else if (llTimeDiffFromNow > 60 * 60 * 24 * 7)
        {
            fKeepThisValue = (llTimeDiffFromLastKept >= 60 * 60 * 24);
        }
        // If it's older than 3 days, keep values that are at least 12 hours apart, and discard ones closer together
        else if (llTimeDiffFromNow > 60 * 60 * 24 * 3)
        {
            fKeepThisValue = (llTimeDiffFromLastKept >= 60 * 60 * 12);
        }
        // If it's older than a day, keep values that are at least 3 hours apart, and discard ones closer together
        else if (llTimeDiffFromNow > 60 * 60 * 24)
        {
            fKeepThisValue = (llTimeDiffFromLastKept >= 60 * 60 * 3);
        }
        // If it's older than an hour, keep values at least 15 minutes apart, and discard ones closer together
        else if (llTimeDiffFromNow > 60 * 60)
        {
            fKeepThisValue = (llTimeDiffFromLastKept >= 60 * 15);
        }
        // It must be newer than an hour, so keep all values. This is unfortunately critical in the dropbox-like scenario,
        // where we can commit a database, and later discover that perhaps it didn't really commit after all (due to two machines
        // updating the database file at around the same time, which is common when a value is being modified on machine A regularly,
        // and machine B is trying to update the database to indicate a recent value is referenced).
        // A better solution should be explored because this just dramatically lowers the chance of conflicts, but does
        // not completely eliminate it. It is important for now to make sure that the expiration feature doesn't usually have conflicts.
        else
        {
            fKeepThisValue = TRUE;
        }

        if (fKeepThisValue)
        {
            // Remember the last kept value so we can only keep values at decent intervals apart. No point in maintaining changes seconds apart if they happened over a month ago.
            stLastKept = stCurrent;
        }
        else
        {
            // Don't bother trying to update a remote's references for this deletion - the only time we delete a value that is referenced (and so might have references to delete)
            // is when the value is ancient, which means we haven't synced to that remote in a long time. If we never found it in the past year, we're not going to find it now.
            --cKeptHistoryRows;
            hr = ForgetHistoryRow(pcdb, &valueHistoryRow, wzValueName);
            ExitOnFailure(hr, "Failed to forget history row %I64d for value %ls", i, wzValueName);
        }
    }

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    fInSceTransaction = FALSE;

LExit:
    ReleaseSceRow(valueHistoryRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    ReleaseStr(sczReferencedBy);

    return hr;
}

HRESULT ForgetHistoryRow(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE *pValueHistoryRow,
    __in_z LPCWSTR wzValueName
    )
{
    HRESULT hr = S_OK;
    CONFIG_VALUETYPE cvType = VALUE_INVALID;
    DWORD dwContentID = DWORD_MAX;

    hr = SceGetColumnDword(*pValueHistoryRow, VALUE_COMMON_TYPE, reinterpret_cast<DWORD*>(&cvType));
    ExitOnFailure(hr, "Failed to get value type of value history for value named: %ls", wzValueName);

    if (VALUE_BLOB == cvType)
    {
        hr = SceGetColumnDword(*pValueHistoryRow, VALUE_COMMON_BLOBCONTENTID, &dwContentID);
        ExitOnFailure(hr, "Failed to get content ID of value history for value named: %ls", wzValueName);
    }

    hr = SceDeleteRow(pValueHistoryRow);
    ExitOnFailure(hr, "Failed to delete history row for value named: %ls", wzValueName);

    if (VALUE_BLOB == cvType)
    {
        // Refcounts are only counted for history entries
        hr = StreamDecreaseRefcount(pcdb, dwContentID, 1);
        ExitOnFailure(hr, "Failed to decrease refcount of content with ID: %u", dwContentID);
    }

LExit:
    return hr;
}

BOOL ReferencedByStringContainsId(
    __in_z_opt LPCWSTR wzReferencedBy,
    __in_z LPCWSTR wzId
    )
{
    LPCWSTR wzIndexFound = NULL;
    int cReferencedByChars = wzReferencedBy != NULL ? lstrlenW(wzReferencedBy) : 0;
    int cIdChars = lstrlenW(wzId);

    if (NULL == wzReferencedBy)
    {
        return FALSE;
    }

    if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzReferencedBy, -1, wzId, -1))
    {
        return TRUE;
    }
    // Before we go doing strings assuming length, just return false is referenced by string is not enough to hold id length + 2 (delimiter + a 1 character id)
    else if (cReferencedByChars < cIdChars + 2)
    {
        return FALSE;
    }
    // If the string starts with the id and is followed by semicolon, it's a match
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzReferencedBy, cIdChars, wzId, cIdChars) && wzReferencedBy[cIdChars] == L';')
    {
        return TRUE;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzReferencedBy + cReferencedByChars - cIdChars, cIdChars, wzId, cIdChars) && wzReferencedBy[cReferencedByChars - cIdChars - 1] == L';')
    {
        return TRUE;
    }
    else
    {
        wzIndexFound = wcsstr(wzReferencedBy, wzId);

        while (wzIndexFound != NULL)
        {
            if (wzIndexFound != NULL)
            {
                if (wzIndexFound[-1] == L';' && wzIndexFound[cIdChars] == L';')
                {
                    return TRUE;
                }
                else
                {
                    wzReferencedBy = wzIndexFound + 1;
                }
            }

            wzIndexFound = wcsstr(wzReferencedBy, wzId);
        }
    }

    return FALSE;
}

HRESULT RemoveOutdatedReferencesFromDatabase(
    __in CFGDB_STRUCT *pcdb,
    __in const CONFIG_VALUE *pValueToKeepReference,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzValueName,
    __in_opt CFGDB_STRUCT *pcdbReferencedBy
    )
{
    HRESULT hr = S_OK;
    BOOL fComparisonResult = FALSE;
    BOOL fMatchingValueFound = FALSE;
    LPWSTR sczReferencedBy = NULL;
    CONFIG_VALUE value = { };
    SCE_ROW_HANDLE valueHistoryRow = NULL;
    DWORD *rgAllHistoryIDs = NULL;
    DWORD cAllHistoryIDs = 0;
    SCE_QUERY_HANDLE query = NULL;
    SCE_QUERY_RESULTS_HANDLE results = NULL;

    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_HISTORY_TABLE, 1, &query);
    ExitOnFailure(hr, "Failed to begin query into value index history table");

    hr = SceSetQueryColumnDword(query, dwAppID);
    ExitOnFailure(hr, "Failed to set query column id to app ID");

    hr = SceSetQueryColumnString(query, wzValueName);
    ExitOnFailure(hr, "Failed to set query column id to value name");

    hr = SceRunQueryRange(&query, &results);
    if (E_NOTFOUND == hr)
    {
        // Nothing to clean up
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query");

    hr = SceGetNextResultRow(results, &valueHistoryRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next history row");

        hr = MemEnsureArraySize(reinterpret_cast<void **>(&rgAllHistoryIDs), cAllHistoryIDs + 1, sizeof(DWORD), 10);
        ExitOnFailure(hr, "Failed to reserve space for all history rows");

        // Store the new ID in the array
        hr = SceGetColumnDword(valueHistoryRow, VALUE_COMMON_ID, rgAllHistoryIDs + cAllHistoryIDs);
        ++cAllHistoryIDs;

        hr = ValueRead(pcdb, valueHistoryRow, &value);
        ExitOnFailure(hr, "Failed to read value from history row");

        hr = ValueCompare(&value, pValueToKeepReference, TRUE, &fComparisonResult);
        ExitOnFailure(hr, "Failed to compare values for value named: %ls", wzValueName);
        if (fComparisonResult)
        {
            if (!fMatchingValueFound)
            {
                fMatchingValueFound = TRUE;
            }
            else
            {
                // That's odd - two values are completely identical. No need to fail, but do report the situation and only mark one of them as referenced
                TraceError(HRESULT_FROM_WIN32(ERROR_ALREADY_ASSIGNED), "A matching value was already found while removing outdated references, continuing.");
                fComparisonResult = FALSE;
            }
        }

        if (pcdbReferencedBy)
        {
            hr = HistoryRowEnsureReferenceState(pcdb, pcdbReferencedBy, valueHistoryRow, fComparisonResult);
            ExitOnFailure(hr, "Failed to update reference state of value history row: %ls", wzValueName);
        }

        ReleaseNullCfgValue(value);
        ReleaseNullSceRow(valueHistoryRow);
        hr = SceGetNextResultRow(results, &valueHistoryRow);
    }
    hr = S_OK;

    if (!fMatchingValueFound)
    {
        hr = E_NOTFOUND;
        ExitOnFailure(hr, "A matching value was not found while removing outdated references.");
    }

    hr = ExpireOldRows(pcdb, wzValueName, rgAllHistoryIDs, cAllHistoryIDs);
    ExitOnFailure(hr, "Failed to expire old rows");

LExit:
    ReleaseMem(rgAllHistoryIDs);
    ReleaseStr(sczReferencedBy);
    ReleaseCfgValue(value);
    ReleaseSceRow(valueHistoryRow);
    ReleaseSceQuery(query);
    ReleaseSceQueryResults(results);

    return hr;
}

HRESULT HistoryRowRemoveReferencesFromDatabase(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE sceValueHistoryRow,
    __in CFGDB_STRUCT *pcdbReferencedBy
    )
{
    HRESULT hr = S_OK;
    DWORD dwAppID = 0;
    LPWSTR sczName = NULL;
    CONFIG_VALUE value = { };

    hr = ValueRead(pcdb, sceValueHistoryRow, &value);
    ExitOnFailure(hr, "Failed to read value");

    hr = SceGetColumnDword(sceValueHistoryRow, VALUE_COMMON_APPID, &dwAppID);
    ExitOnFailure(hr, "Failed to get value appid");

    hr = SceGetColumnString(sceValueHistoryRow, VALUE_COMMON_NAME, &sczName);
    ExitOnFailure(hr, "Failed to get value name");

    hr = RemoveOutdatedReferencesFromDatabase(pcdb, &value, dwAppID, sczName, pcdbReferencedBy);
    ExitOnFailure(hr, "Failed to remove outdated references from database");

LExit:
    ReleaseStr(sczName);
    ReleaseCfgValue(value);

    return hr;
}

HRESULT HistoryRowEnsureReferenceState(
    __in CFGDB_STRUCT *pcdb,
    __in CFGDB_STRUCT *pcdbOther,
    __in SCE_ROW_HANDLE sceValueHistoryRow,
    __in BOOL fDesiredState
    )
{
    HRESULT hr = S_OK;
    LPWSTR *rgsczDbReferenceArray = NULL;
    UINT cDbReferenceArray = 0;
    LPWSTR sczDbReferences = NULL;
    LPCWSTR wzId = NULL;
    BOOL fFoundInArray = FALSE;
    BOOL fArrayModified = FALSE;

    if (pcdb->fRemote)
    {
        wzId = pcdb->sczGuidLocalInRemoteKey;
    }
    else
    {
        wzId = pcdbOther->sczGuidRemoteInLocalKey;
    }

    DWORD dwId = 0;
    hr = SceGetColumnDword(sceValueHistoryRow, VALUE_COMMON_ID, &dwId);
    ExitOnFailure(hr, "Failed to get ID");

    hr = SceGetColumnString(sceValueHistoryRow, VALUE_HISTORY_DB_REFERENCES, &sczDbReferences);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;

        if (fDesiredState)
        {
            hr = SceSetColumnString(sceValueHistoryRow, VALUE_HISTORY_DB_REFERENCES, wzId);
            ExitOnFailure(hr, "Failed to set Db References column value");

            hr = SceFinishUpdate(sceValueHistoryRow);
            ExitOnFailure(hr, "Failed to finish simple update to history row");

            hr = HistoryRowRemoveReferencesFromDatabase(pcdb, sceValueHistoryRow, pcdbOther);
            ExitOnFailure(hr, "Failed to remove references to outdated value history rows");
        }
        // else already not present in the column, so nothing to do

        // Get out now, we're done
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to get Db References column value");

    // Perf shortcut - if already in the right state based on string comparison (most common case),
    // don't bother with all of these allocs and frees
    if (ReferencedByStringContainsId(sczDbReferences, wzId) == fDesiredState)
    {
        return S_OK;
    }

    hr = StrSplitAllocArray(&rgsczDbReferenceArray, &cDbReferenceArray, sczDbReferences, L";");
    ExitOnFailure(hr, "Failed to split db references column value into array based on delimited semicolons: ", sczDbReferences);

    for (DWORD i = 0; i < cDbReferenceArray; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, rgsczDbReferenceArray[i], -1, wzId, -1))
        {
            fFoundInArray = TRUE;

            // If it's in the references and we were asked to remove it, remove it now while we know the index
            if (!fDesiredState)
            {
                fArrayModified = TRUE;

                MemRemoveFromArray(rgsczDbReferenceArray, i, 1, cDbReferenceArray, sizeof(LPCWSTR), FALSE);
                --cDbReferenceArray;
            }

            break;
        }
    }

    // If it was NOT found in the array and we were asked to add it, do it now
    if (!fFoundInArray && fDesiredState)
    {
        fArrayModified = TRUE;

        hr = MemEnsureArraySize(reinterpret_cast<void **>(&rgsczDbReferenceArray), cDbReferenceArray + 1, sizeof(LPCWSTR), 0);
        ExitOnFailure(hr, "Failed to grow array when adding reference");
        ++cDbReferenceArray;

        hr = StrAllocString(rgsczDbReferenceArray + cDbReferenceArray - 1, wzId, 0);
        ExitOnFailure(hr, "Failed to copy id into array");
    }

    // Finally, if array was modified, write it back to the column
    if (fArrayModified)
    {
        // Wipe out the string for concatenation of each array item
        // This string is guaranteed to not be NULL because SceUtil returned success for getting the column
        sczDbReferences[0] = L'\0';

        for (DWORD i = 0; i < cDbReferenceArray; ++i)
        {
            hr = StrAllocConcat(&sczDbReferences, rgsczDbReferenceArray[i], 0);
            ExitOnFailure(hr, "Failed to concatenate reference string: %ls", rgsczDbReferenceArray[i]);

            if (i < cDbReferenceArray - 1)
            {
                hr = StrAllocConcat(&sczDbReferences, L";", 1);
                ExitOnFailure(hr, "Failed to concatenate semicolon");
            }
        }

        if (0 == cDbReferenceArray)
        {
            hr = SceSetColumnNull(sceValueHistoryRow, VALUE_HISTORY_DB_REFERENCES);
            ExitOnFailure(hr, "Failed to set Db References column value to null after array modification");
        }
        else
        {
            hr = SceSetColumnString(sceValueHistoryRow, VALUE_HISTORY_DB_REFERENCES, sczDbReferences);
            ExitOnFailure(hr, "Failed to set Db References column value after array modification");
        }

        hr = SceBeginTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to begin transaction");

        hr = SceFinishUpdate(sceValueHistoryRow);
        ExitOnFailure(hr, "Failed to finish array update to history row");

        hr = SceCommitTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to commit transaction");

        if (fDesiredState)
        {
            hr = HistoryRowRemoveReferencesFromDatabase(pcdb, sceValueHistoryRow, pcdbOther);
            ExitOnFailure(hr, "Failed to remove references to outdated value history rows");
        }
    }

LExit:
    ReleaseStr(sczDbReferences);
    ReleaseStrArray(rgsczDbReferenceArray, cDbReferenceArray);

    return hr;
}

HRESULT FindHistoryRow(
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

HRESULT FindHistoryRowById(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwHistoryId,
    __out SCE_ROW_HANDLE *pRowHandle
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE query = NULL;

    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_HISTORY_TABLE, 0, &query);
    ExitOnFailure(hr, "Failed to begin query into value index history table by id");

    hr = SceSetQueryColumnDword(query, dwHistoryId);
    ExitOnFailure(hr, "Failed to set query column dword to last history id");

    hr = SceRunQueryExact(&query, pRowHandle);
    ExitOnFailure(hr, "Failed to get value history row");

LExit:
    ReleaseSceQuery(query);

    return hr;
}
