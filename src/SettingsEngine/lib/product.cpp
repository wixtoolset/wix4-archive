// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR wzLegacyManifestValuePrefix = L"Reserved:\\Legacy\\Manifest\\";

HRESULT ProductValidateName(
    __in_z LPCWSTR wzProductName
    )
{
    HRESULT hr = S_OK;

    // For now: if it isn't empty, it's legal!
    // TODO: filter out things like purely whitespace, or other crazy-ish names
    if (0 == lstrlenW(wzProductName))
    {
        hr = HRESULT_FROM_WIN32(ERROR_CLUSTER_INVALID_STRING_FORMAT);
        ExitOnFailure(hr, "ProductName \"%ls\" wasn't in a valid format - expected a non-empty string", wzProductName);
    }

LExit:
    return hr;
}

HRESULT ProductValidateVersion(
    __in_z LPCWSTR wzVersion
    )
{
    HRESULT hr = S_OK;

    DWORD dw1 = 0;
    DWORD dw2 = 0;
    DWORD dw3 = 0;
    DWORD dw4 = 0;
    DWORD dwResult = 0;
    WCHAR wcExtra;

    dwResult = swscanf_s(wzVersion, L"%u.%u.%u.%u%lc", &dw1, &dw2, &dw3, &dw4, &wcExtra, 1);

    // Must have 4 components to the version
    if (4 != dwResult)
    {
        hr = HRESULT_FROM_WIN32(ERROR_CLUSTER_INVALID_STRING_FORMAT);
        ExitOnFailure(hr, "Version \"%ls\" wasn't in a valid format - expected version like: 1.0.0.0", wzVersion);
    }

    // TODO: Any additional validation here? Is "0.*" accepted? What about a version component > 65535?

LExit:
    return hr;
}

HRESULT ProductValidatePublicKey(
    __in_z LPCWSTR wzPublicKey
    )
{
    HRESULT hr = S_OK;
    DWORD dwIndex;

    if (16 != lstrlenW(wzPublicKey))
    {
        hr = HRESULT_FROM_WIN32(ERROR_CLUSTER_INVALID_STRING_FORMAT);
        ExitOnFailure(hr, "Public key wasn't in a valid format - expect 16 characters: %ls", wzPublicKey);
    }

    for (dwIndex = 0; dwIndex < 16; ++dwIndex)
    {
        // Must be a valid lowercase hex character (0-9, a-f)
        // If it's a valid character, move on and check the next one
        if (   (wzPublicKey[dwIndex] <= L'f' && wzPublicKey[dwIndex] >= L'a')
            || (wzPublicKey[dwIndex] <= L'9' && wzPublicKey[dwIndex] >= L'0'))
        {
            continue;
        }

        // This wasn't a valid character - return false
        hr = HRESULT_FROM_WIN32(ERROR_CLUSTER_INVALID_STRING_FORMAT);
        ExitOnFailure(hr, "Public key wasn't in a valid format - found non-hex digit: %lc in string \"%ls\"", wzPublicKey[dwIndex], wzPublicKey);
    }

LExit:
    return hr;
}

HRESULT ProductFindRow(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwTableIndex,
    __in_z LPCWSTR wzProductName,
    __in_z_opt LPCWSTR wzVersion,
    __in_z_opt LPCWSTR wzPublicKey,
    __out SCE_ROW_HANDLE *pSceRow
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;

    // First query for an existing value - if it exists, we'll update it in place
    hr = SceBeginQuery(pcdb->psceDb, dwTableIndex, 1, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into product index table");

    hr = SceSetQueryColumnString(sqhHandle, wzProductName);
    ExitOnFailure(hr, "Failed to set query column name string to: %ls", wzProductName);

    hr = SceSetQueryColumnString(sqhHandle, wzVersion);
    ExitOnFailure(hr, "Failed to set query column version string to: %ls", wzVersion);

    hr = SceSetQueryColumnString(sqhHandle, wzPublicKey);
    ExitOnFailure(hr, "Failed to set query column publickey string to: %ls", wzPublicKey);

    hr = SceRunQueryExact(&sqhHandle, pSceRow);
    if (E_NOTFOUND == hr)
    {
        // Don't pollute our log with unnecessary messages
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to find product: %ls", wzProductName);

LExit:
    ReleaseSceQuery(sqhHandle);

    return hr;
}

HRESULT ProductSyncValues(
    __in CFGDB_STRUCT *pcdb1,
    __in CFGDB_STRUCT *pcdb2,
    __in BOOL fAllowLocalToReceiveData,
    __in STRINGDICT_HANDLE shDictValuesSeen,
    __out CONFLICT_PRODUCT **ppcpProduct
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczName = NULL;
    DWORD dwInserting = 0;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_QUERY_RESULTS_HANDLE sqrhResults = NULL;
    SCE_ROW_HANDLE sceRow = NULL;
    DWORD dwFoundIndex = 0;
    DWORD dwSubsumeIndex = 0;
    BOOL fSame = FALSE;
    BOOL fFirstIsLocal = (NULL == pcdb1->pcdbLocal);

    CFG_ENUMERATION * valueHistory1 = NULL;
    DWORD dwCfgCount1 = 0;
    CFG_ENUMERATION * valueHistory2 = NULL;
    DWORD dwCfgCount2 = 0;

    hr = SceBeginQuery(pcdb1->psceDb, VALUE_INDEX_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into value table");

    hr = SceSetQueryColumnDword(sqhHandle, pcdb1->dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", pcdb1->dwAppID);

    hr = SceRunQueryRange(&sqhHandle, &sqrhResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to enumerate values for product %u", pcdb1->dwAppID);

    hr = SceGetNextResultRow(sqrhResults, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next row from query into value table");

        CfgReleaseEnumeration(valueHistory1);
        valueHistory1 = NULL;
        CfgReleaseEnumeration(valueHistory2);
        valueHistory2 = NULL;

        hr = SceGetColumnString(sceRow, VALUE_COMMON_NAME, &sczName);
        ExitOnFailure(hr, "Failed to get value name");

        if (NULL != shDictValuesSeen)
        {
            hr = DictKeyExists(shDictValuesSeen, sczName);
            if (E_NOTFOUND == hr)
            {
                hr = DictAddKey(shDictValuesSeen, sczName);
                ExitOnFailure(hr, "Failed to add to dictionary value: %ls", sczName);
            }
            else
            {
                ExitOnFailure(hr, "Failed to check if key exists: %ls", sczName);

                // This value was already synced; skip it!
                goto Skip;
            }
        }

        // Exclude legacy detect cache values, they should never be synced off the machine
        // TODO: when we support per-machine settings, migrate this to use that feature
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, sczName, lstrlenW(wzLegacyDetectCacheValuePrefix), wzLegacyDetectCacheValuePrefix, lstrlenW(wzLegacyDetectCacheValuePrefix)))
        {
            goto Skip;
        }

        // First check if the values are identical. Even if they were set by different folks at different times,
        // same value means nothing to sync.
        hr = ValueMatch(sczName, pcdb1, pcdb2, sceRow, &fSame);
        ExitOnFailure(hr, "Failed to check if values are identical");

        if (fSame)
        {
            goto Skip;
        }

        // Get history of the value in db2
        hr = EnumPastValues(pcdb2, sczName, &valueHistory2, &dwCfgCount2);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to enumerate previous values in db2");

        // Get history of the value in db1
        hr = EnumPastValues(pcdb1, sczName, &valueHistory1, &dwCfgCount1);
        ExitOnFailure(hr, "Found value in db1, but failed to enumerate previous values in db1 while searching for conflicts");

        if (0 == dwCfgCount2)
        {
            if (fFirstIsLocal || fAllowLocalToReceiveData)
            {
                hr = ValueTransferFromHistory(pcdb2, valueHistory1, 0, pcdb1);
                ExitOnFailure(hr, "Failed to transfer history (due to value not present) from db 2 to db 1 for value %ls", sczName);
            }

            goto Skip;
        }

        // Don't write anything to db2 if it's local and we're told not to
        if (fFirstIsLocal || fAllowLocalToReceiveData)
        {
            // We first check the latest value. However, if the previous value is identical (same type & value, just different source), check for subsumation of that too.
            // This reduces unnecessary conflicts in rare corner case scenarios.
            dwSubsumeIndex = dwCfgCount2;
            do
            {
                --dwSubsumeIndex;

                // Check if the last history entry for database 2 exists in the database 1 - if it does, database 2's changes are subsumed
                hr = EnumFindValueInHistory(valueHistory1, dwCfgCount1, valueHistory2->valueHistory.rgcValues + dwSubsumeIndex, &dwFoundIndex);
                if (S_OK == hr)
                {
                    // Database 2 is subsumed - pipe over all the newest history entries
                    hr = ValueTransferFromHistory(pcdb2, valueHistory1, dwFoundIndex + 1, pcdb1);
                    ExitOnFailure(hr, "Failed to transfer history (due to history subsumed) from db 1 to db 2 for value %ls", sczName);

                    goto Skip;
                }
                else if (E_NOTFOUND == hr)
                {
                    hr = S_OK;
                }
                else
                {
                    ExitOnFailure(hr, "Failed to check if db2's value history is subsumed by db1's value history");
                }

                if (0 < dwSubsumeIndex)
                {
                    hr = ValueCompare(valueHistory2->valueHistory.rgcValues + dwSubsumeIndex, valueHistory2->valueHistory.rgcValues + dwSubsumeIndex - 1, FALSE, &fSame);
                    ExitOnFailure(hr, "Failed to check if value and previous value in database 2 are equivalent");
                }
            }
            while (0 < dwSubsumeIndex && fSame);
        }

        // Don't write anything to db1 if it's local and we're told not to
        if (!fFirstIsLocal || fAllowLocalToReceiveData)
        {
            // We first check the latest value. However, if the previous value is identical (same type & value, just different source), check for subsumation of that too.
            // This reduces unnecessary conflicts in rare corner case scenarios.
            dwSubsumeIndex = dwCfgCount1;
            do
            {
                --dwSubsumeIndex;

                hr = EnumFindValueInHistory(valueHistory2, dwCfgCount2, valueHistory1->valueHistory.rgcValues + dwSubsumeIndex, &dwFoundIndex);
                if (S_OK == hr)
                {
                    // Database 1 is subsumed - pipe over all the newest history entries
                    hr = ValueTransferFromHistory(pcdb1, valueHistory2, dwFoundIndex + 1, pcdb2);
                    ExitOnFailure(hr, "Failed to transfer history (due to history subsumed) from db 2 to db 1 for value %ls", sczName);

                    goto Skip;
                }
                else if (E_NOTFOUND == hr)
                {
                    hr = S_OK;
                }
                else
                {
                    ExitOnFailure(hr, "Failed to check if db1's value history is subsumed by db2's value history");
                }

                if (0 < dwSubsumeIndex)
                {
                    hr = ValueCompare(valueHistory1->valueHistory.rgcValues + dwSubsumeIndex, valueHistory1->valueHistory.rgcValues + dwSubsumeIndex - 1, FALSE, &fSame);
                    ExitOnFailure(hr, "Failed to check if value and previous value in database 1 are equivalent");
                }
            }
            while (0 < dwSubsumeIndex && fSame);
        }

        // OK, we have a conflict. Report it.
        if (NULL == *ppcpProduct)
        {
            *ppcpProduct = static_cast<CONFLICT_PRODUCT *>(MemAlloc(sizeof(CONFLICT_PRODUCT), TRUE));
            ExitOnNull(*ppcpProduct, hr, E_OUTOFMEMORY, "Failed to allocate product conflict struct");

            (*ppcpProduct)->cValues = 1;
        }
        else
        {
            ++(*ppcpProduct)->cValues;
        }

        hr = MemEnsureArraySize(reinterpret_cast<void **>(&(*ppcpProduct)->rgcesValueEnumLocal), (*ppcpProduct)->cValues, sizeof(CFG_ENUMERATION *), 10);
        ExitOnFailure(hr, "Failed to ensure product local value conflict array size");
        hr = MemEnsureArraySize(reinterpret_cast<void **>(&(*ppcpProduct)->rgdwValueCountLocal), (*ppcpProduct)->cValues, sizeof(DWORD), 10);
        ExitOnFailure(hr, "Failed to ensure product local value conflict count array size");
        hr = MemEnsureArraySize(reinterpret_cast<void **>(&(*ppcpProduct)->rgcesValueEnumRemote), (*ppcpProduct)->cValues, sizeof(CFG_ENUMERATION *), 10);
        ExitOnFailure(hr, "Failed to ensure product remote value conflict array size");
        hr = MemEnsureArraySize(reinterpret_cast<void **>(&(*ppcpProduct)->rgdwValueCountRemote), (*ppcpProduct)->cValues, sizeof(DWORD), 10);
        ExitOnFailure(hr, "Failed to ensure product remote value conflict count array size");
        hr = MemEnsureArraySize(reinterpret_cast<void **>(&(*ppcpProduct)->rgrcValueChoices), (*ppcpProduct)->cValues, sizeof(RESOLUTION_CHOICE), 10);
        ExitOnFailure(hr, "Failed to ensure product value resolution choice array size");

        dwInserting = (*ppcpProduct)->cValues - 1;

        // Neither is subsumed by the other, so we have conflicts - report them
        if (fFirstIsLocal)
        {
            hr = ConflictGetList(reinterpret_cast<const CFG_ENUMERATION *>(valueHistory1), dwCfgCount1,
                reinterpret_cast<const CFG_ENUMERATION *>(valueHistory2), dwCfgCount2,
                &((*ppcpProduct)->rgcesValueEnumLocal[dwInserting]), &(*ppcpProduct)->rgdwValueCountLocal[dwInserting],
                &((*ppcpProduct)->rgcesValueEnumRemote[dwInserting]), &(*ppcpProduct)->rgdwValueCountRemote[dwInserting]);
        }
        else
        {
            hr = ConflictGetList(reinterpret_cast<const CFG_ENUMERATION *>(valueHistory2), dwCfgCount2,
                reinterpret_cast<const CFG_ENUMERATION *>(valueHistory1), dwCfgCount1,
                &((*ppcpProduct)->rgcesValueEnumLocal[dwInserting]), &(*ppcpProduct)->rgdwValueCountLocal[dwInserting],
                &((*ppcpProduct)->rgcesValueEnumRemote[dwInserting]), &(*ppcpProduct)->rgdwValueCountRemote[dwInserting]);
        }
        ExitOnFailure(hr, "Failed to get conflict list");

    Skip:
        ReleaseNullSceRow(sceRow);
        hr = SceGetNextResultRow(sqrhResults, &sceRow);
    }

    hr = S_OK;

LExit:
    ReleaseSceQuery(sqhHandle);
    ReleaseSceQueryResults(sqrhResults);
    ReleaseSceRow(sceRow);
    CfgReleaseEnumeration(valueHistory1);
    CfgReleaseEnumeration(valueHistory2);
    ReleaseStr(sczName);

    return hr;
}

HRESULT ProductEnsureCreated(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __out_opt DWORD *pdwAppID,
    __out_opt BOOL *pfLegacy
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    SCE_ROW_HANDLE sceRow = NULL;
    BOOL fLegacyProduct = (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzPublicKey, -1, wzLegacyPublicKey, -1));

    // First query for an existing value - if it exists, we'll update it in place
    hr = ProductFindRow(pcdb, PRODUCT_INDEX_TABLE, wzProductName, wzVersion, wzPublicKey, &sceRow);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;

        hr = SceBeginTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to begin transaction");
        fInSceTransaction = TRUE;

        hr = ScePrepareInsert(pcdb->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
        ExitOnFailure(hr, "Failed to prepare for insert");

        hr = SceSetColumnString(sceRow, PRODUCT_NAME, wzProductName);
        ExitOnFailure(hr, "Failed to set product name column");

        hr = SceSetColumnString(sceRow, PRODUCT_VERSION, wzVersion);
        ExitOnFailure(hr, "Failed to set version column");

        hr = SceSetColumnString(sceRow, PRODUCT_PUBLICKEY, wzPublicKey);
        ExitOnFailure(hr, "Failed to set publickey column");

        hr = SceSetColumnBool(sceRow, PRODUCT_REGISTERED, FALSE);
        ExitOnFailure(hr, "Failed to set registered column");

        hr = SceSetColumnBool(sceRow, PRODUCT_IS_LEGACY, fLegacyProduct);
        ExitOnFailure(hr, "Failed to set IsLegacy column");

        hr = SceFinishUpdate(sceRow);
        ExitOnFailure(hr, "Failed to finish insert");

        hr = SceCommitTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to commit transaction");
        fInSceTransaction = FALSE;
    }
    else
    {
        ExitOnFailure(hr, "Failed to query for product");
    }

    if (pdwAppID)
    {
        hr = SceGetColumnDword(sceRow, PRODUCT_ID, pdwAppID);
        ExitOnFailure(hr, "Failed to get appID of newly inserted product");
    }
    if (pfLegacy)
    {
        *pfLegacy = fLegacyProduct;
    }

LExit:
    ReleaseSceRow(sceRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }

    return hr;
}

HRESULT ProductSet(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __in BOOL fDontCreate,
    __out_opt BOOL *pfLegacy
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE sceRow = NULL;

    pcdb->fProductSet = FALSE;
    pcdb->fProductIsLegacy = FALSE;
    pcdb->dwAppID = DWORD_MAX;

    if (fDontCreate)
    {
        // Just query for an existing product - if it exists, we'll use it
        hr = ProductFindRow(pcdb, PRODUCT_INDEX_TABLE, wzProductName, wzVersion, wzPublicKey, &sceRow);
        if (E_NOTFOUND == hr)
        {
            ExitFunction();
        }
        ExitOnFailure(hr, "Failed to query for product");

        hr = SceGetColumnDword(sceRow, PRODUCT_ID, &pcdb->dwAppID);
        ExitOnFailure(hr, "Failed to get App ID of application");

        hr = SceGetColumnBool(sceRow, PRODUCT_IS_LEGACY, &pcdb->fProductIsLegacy);
        ExitOnFailure(hr, "Failed to get IsLegacy flag of application");
    }
    else
    {
        hr = ProductEnsureCreated(pcdb, wzProductName, wzVersion, wzPublicKey, &pcdb->dwAppID, &pcdb->fProductIsLegacy);
        ExitOnFailure(hr, "Failed to ensure product exists: %ls", wzProductName);
    }

    // Get the AppID (of either the found row, or the recently created row)
    hr = StrAllocString(&pcdb->sczProductName, wzProductName, 0);
    ExitOnFailure(hr, "Failed to copy product name string");

    if (pfLegacy)
    {
        *pfLegacy = pcdb->fProductIsLegacy;
    }

    pcdb->fProductSet = TRUE;

LExit:
    ReleaseSceRow(sceRow);

    return hr;
}

HRESULT ProductUnset(
    __in CFGDB_STRUCT *pcdb
    )
{
    pcdb->fProductSet = FALSE;
    pcdb->fProductIsLegacy = FALSE;
    pcdb->dwAppID = DWORD_MAX;
    ReleaseNullStr(pcdb->sczProductName);

    return S_OK;
}

HRESULT ProductForget(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    DWORD dwAppID = 0;
    BOOL fLegacyProduct = FALSE;
    LPWSTR sczLegacyManifestValueName = NULL;
    SCE_ROW_HANDLE sceRowProduct = NULL;
    SCE_QUERY_RESULTS_HANDLE sqrhResults = NULL;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRowValue = NULL;
    CONFIG_VALUE cvValue = { };

    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction");
    fInSceTransaction = TRUE;

    hr = ProductFindRow(pcdb, PRODUCT_INDEX_TABLE, wzProductName, wzVersion, wzPublicKey, &sceRowProduct);
    if (E_NOTFOUND == hr)
    {
        // If it's not found, consider it success
        ExitFunction1(hr = S_OK);
    }
    else
    {
        ExitOnFailure(hr, "Failed to check if product already exists in product index");
    }

    hr = SceGetColumnDword(sceRowProduct, PRODUCT_ID, &dwAppID);
    ExitOnFailure(hr, "Failed to get AppID of existing application");

    hr = SceGetColumnBool(sceRowProduct, PRODUCT_IS_LEGACY, &fLegacyProduct);
    ExitOnFailure(hr, "Failed to get IsLegacy flag of existing application");

    // Now go through and forget all values, which causes history entries to be removed
    // and streams to be decremented (and possibly also deleted)
    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into value table");

    hr = SceSetQueryColumnDword(sqhHandle, dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", dwAppID);

    hr = SceRunQueryRange(&sqhHandle, &sqrhResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query");

    hr = SceGetNextResultRow(sqrhResults, &sceRowValue);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next result row");

        hr = ValueForget(pcdb, dwAppID, &sceRowValue);
        ExitOnFailure(hr, "Failed to forget value for AppID %u", dwAppID);

        ReleaseNullSceRow(sceRowValue);
        hr = SceGetNextResultRow(sqrhResults, &sceRowValue);
    }
    hr = S_OK;

    hr = SceDeleteRow(&sceRowProduct);
    ExitOnFailure(hr, "Failed to delete product row while forgetting product");

    if (fLegacyProduct)
    {
        // If there is a legacy manifest stored, tombstone it so it's forgotten on other machines too
        hr = ProductGetLegacyManifestValueName(wzProductName, &sczLegacyManifestValueName);
        ExitOnFailure(hr, "Failed to get legacy manifest value name");

        hr = ValueSetDelete(NULL, pcdb->sczGuid, &cvValue);
        ExitOnFailure(hr, "Failed to set delete value in memory");

        hr = ValueWrite(pcdb, pcdb->dwCfgAppID, sczLegacyManifestValueName, &cvValue, TRUE, NULL);
        ExitOnFailure(hr, "Failed to tombstone legacy manifest for product %ls", wzProductName);
    }

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    fInSceTransaction = FALSE;

    // If it's a legacy product, tell the background thread to stop monitoring it
    if (fLegacyProduct && !pcdb->fRemote)
    {
        hr = BackgroundRemoveProduct(pcdb, wzProductName);
        ExitOnFailure(hr, "Failed to notify background that of product update");
    }

LExit:
    ReleaseStr(sczLegacyManifestValueName);
    ReleaseSceRow(sceRowProduct);
    ReleaseSceQuery(sqhHandle);
    ReleaseSceQueryResults(sqrhResults);
    ReleaseSceRow(sceRowValue);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

HRESULT ProductGetLegacyManifestValueName(
    __in_z LPCWSTR wzProductName,
    __deref_out_z LPWSTR* psczManifestValueName
    )
{
    HRESULT hr = S_OK;

    hr = StrAllocString(psczManifestValueName, wzLegacyManifestValuePrefix, 0);
    ExitOnFailure(hr, "Failed to allocate legacy manifest value prefix");

    hr = StrAllocConcat(psczManifestValueName, wzProductName, 0);
    ExitOnFailure(hr, "Failed to concat product name %ls to manifest value name", wzProductName);

LExit:
    return hr;
}

HRESULT ProductIsLegacyManifestValueName(
    __in_z LPCWSTR wzValueName,
    __deref_out_z LPWSTR* psczProductName
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzFindResult = NULL;

    wzFindResult = wcsstr(wzValueName, wzLegacyManifestValuePrefix);

    if (wzFindResult == wzValueName)
    {
        hr = StrAllocString(psczProductName, wzFindResult + lstrlenW(wzLegacyManifestValuePrefix), 0);
        ExitOnFailure(hr, "Failed to allocate copy of legacy manifest product name");
    }
    else
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

LExit:
    return hr;
}

HRESULT ProductIsRegistered(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __out BOOL *pfRegistered
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE sceRow = NULL;

    *pfRegistered = FALSE;

    hr = ProductFindRow(pcdb, PRODUCT_INDEX_TABLE, wzProductName, wzVersion, wzPublicKey, &sceRow);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    else
    {
        ExitOnFailure(hr, "Failed to query for product");

        hr = SceGetColumnBool(sceRow, PRODUCT_REGISTERED, pfRegistered);
        ExitOnFailure(hr, "Failed to check if product is already installed");
    }

    // Fall back to admin database, if it exists
    if (!*pfRegistered && !pcdb->pcdbAdmin->fMissing)
    {
        ReleaseNullSceRow(sceRow);
        hr = ProductFindRow(pcdb->pcdbAdmin, ADMIN_PRODUCT_INDEX_TABLE, wzProductName, wzVersion, wzPublicKey, &sceRow);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else
        {
            *pfRegistered = TRUE;
        }
    }

LExit:
    ReleaseSceRow(sceRow);

    return hr;
}

HRESULT ProductRegister(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __in BOOL fRegister
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE sceRow = NULL;
    BOOL fAlreadyRegistered = FALSE;
    BOOL fInSceTransaction = FALSE;

    hr = ProductFindRow(pcdb, PRODUCT_INDEX_TABLE, wzProductName, wzVersion, wzPublicKey, &sceRow);
    if (E_NOTFOUND == hr)
    {
        if (!fRegister)
        {
            // Row doesn't exist, and we were told to unregister, so nothing to do
            ExitFunction1(hr = S_OK);
        }

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzPublicKey, -1, wzLegacyPublicKey, -1))
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Cannot register legacy product for which we have no legacy manifest!");
        }

        hr = SceBeginTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to begin transaction");
        fInSceTransaction = TRUE;

        hr = ScePrepareInsert(pcdb->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
        ExitOnFailure(hr, "Failed to prepare for insert");

        hr = SceSetColumnString(sceRow, PRODUCT_NAME, wzProductName);
        ExitOnFailure(hr, "Failed to set product name column");

        hr = SceSetColumnString(sceRow, PRODUCT_VERSION, wzVersion);
        ExitOnFailure(hr, "Failed to set version column");

        hr = SceSetColumnString(sceRow, PRODUCT_PUBLICKEY, wzPublicKey);
        ExitOnFailure(hr, "Failed to set publickey column");

        hr = SceSetColumnBool(sceRow, PRODUCT_REGISTERED, TRUE);
        ExitOnFailure(hr, "Failed to set registered column");

        hr = SceSetColumnBool(sceRow, PRODUCT_IS_LEGACY, FALSE);
        ExitOnFailure(hr, "Failed to set registered column");

        hr = SceFinishUpdate(sceRow);
        ExitOnFailure(hr, "Failed to finish update");

        hr = SceCommitTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to commit transaction");
        fInSceTransaction = FALSE;
    }
    else
    {
        ExitOnFailure(hr, "Failed to query for product");

        hr = SceGetColumnBool(sceRow, PRODUCT_REGISTERED, &fAlreadyRegistered);
        ExitOnFailure(hr, "Failed to check if product is already registered");

        if (fRegister != fAlreadyRegistered)
        {
            hr = SceBeginTransaction(pcdb->psceDb);
            ExitOnFailure(hr, "Failed to begin transaction");
            fInSceTransaction = TRUE;

            hr = SceSetColumnBool(sceRow, PRODUCT_REGISTERED, fRegister);
            ExitOnFailure(hr, "Failed to set registered flag to true");

            hr = SceFinishUpdate(sceRow);
            ExitOnFailure(hr, "Failed to finish update into product index table");

            hr = SceCommitTransaction(pcdb->psceDb);
            ExitOnFailure(hr, "Failed to commit transaction");
            fInSceTransaction = FALSE;
        }
    }

LExit:
    ReleaseSceRow(sceRow);
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }

    return hr;
}

