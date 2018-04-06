// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT FindSpecialInLegacyRegistryKey(
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in_z LPCWSTR wzSubKey,
    __in_z LPCWSTR wzValueName,
    __out LEGACY_REGISTRY_SPECIAL **ppRegistrySpecial
    );
static HRESULT RegSpecialValueReadFlags(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in DWORD dwValueType
    );
static HRESULT RegSpecialValueReadNonTypecasted(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName
    );
static HRESULT RegSpecialProductWriteBinary(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzRegKey,
    __in_z LPCWSTR wzRegValueName,
    __in HKEY *phkKey,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial
    );

extern "C" HRESULT RegSpecialValueRead(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in HKEY hkKey,
    __in_z LPCWSTR wzSubKey,
    __in_z LPCWSTR wzValueName,
    __in DWORD dwValueType,
    __out BOOL *pfContinueProcessing
    )
{
    HRESULT hr = S_OK;
    LEGACY_REGISTRY_SPECIAL *pRegKeySpecial = NULL;

    hr = FindSpecialInLegacyRegistryKey(pRegKey, wzSubKey, wzValueName, &pRegKeySpecial);
    if (E_NOTFOUND == hr)
    {
        *pfContinueProcessing = TRUE;
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to find special in legacy registry key");

    *pfContinueProcessing = FALSE;

    if (0 < pRegKeySpecial->cFlagsInfo)
    {
        hr = RegSpecialValueReadFlags(pcdb, pSyncProductSession, pRegKey, pRegKeySpecial, hkKey, wzValueName, dwValueType);
        ExitOnFailure(hr, "Failed to handle registry special type flags on read for value name: %ls, type: %u", wzValueName, dwValueType);
    }

    if (pRegKeySpecial->fHandleNonTypecasted)
    {
        hr = RegSpecialValueReadNonTypecasted(pcdb, pSyncProductSession, pRegKey, pRegKeySpecial, hkKey, wzValueName);
        ExitOnFailure(hr, "Failed to handle registry special basic on read for value name: %ls, type: %u", wzValueName, dwValueType);
    }
    
LExit:
    return hr;
}

extern "C" HRESULT RegSpecialsProductWrite(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    )
{
    HRESULT hr = S_OK;
    HKEY hkKey = NULL;
    LPWSTR sczRegValuePath = NULL;
    LPCWSTR wzRegKey = NULL;
    LPCWSTR wzRegValueName = NULL;
    LPWSTR wzLastBackslash = NULL;
    const LEGACY_REGISTRY_KEY *pRegKey = NULL;
    const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial = NULL;

    for (DWORD i = 0; i < pSyncProductSession->product.cRegKeys; ++i)
    {
        pRegKey = pSyncProductSession->product.rgRegKeys + i;

        for (DWORD j = 0; j < pRegKey->cRegKeySpecials; ++j)
        {
            pRegKeySpecial = &(pRegKey->rgRegKeySpecials[j]);

            hr = StrAllocString(&sczRegValuePath, pRegKey->sczKey, 0);
            ExitOnFailure(hr, "Failed to allocate copy of key for appending");

            hr = StrAllocConcat(&sczRegValuePath, pRegKeySpecial->sczRegValueName, 0);
            ExitOnFailure(hr, "Failed to concatenate registry value name");

            wzRegKey = sczRegValuePath;
            wzLastBackslash = const_cast<LPWSTR>(wcsrchr(sczRegValuePath, L'\\'));
            ExitOnNull(wzLastBackslash, hr, E_UNEXPECTED, "Invalid registry path (no backslash found, and we just added one)!");

            *wzLastBackslash = L'\0';
            wzRegValueName = wzLastBackslash + 1;

            ReleaseRegKey(hkKey);
            hr = RegOpen(ManifestConvertToRootKey(pRegKey->dwRoot), wzRegKey, KEY_SET_VALUE, &hkKey);
            if (E_FILENOTFOUND == hr)
            {
                // Don't create it here, because it may not need to be created at all. Delay creation until a value needs to be written.
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to open or create registry key at root: %u, key: %ls", pRegKey->dwRoot, wzRegKey);

            switch (pRegKeySpecial->dwRegValueType)
            {
            case REG_BINARY:
                hr = RegSpecialProductWriteBinary(pcdb, wzRegKey, wzRegValueName, &hkKey, pRegKey, pRegKeySpecial);
                ExitOnFailure(hr, "Failed to do special product write for individual registry special (binary)");
                break;
            default:
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Registry value type unhandled in RegSpecialsProductWrite(): %u", pRegKeySpecial->dwRegValueType);
                break;

            }
        }
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczRegValuePath);

    return hr;
}

// Static functions
static HRESULT FindSpecialInLegacyRegistryKey(
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in_z LPCWSTR wzSubKey,
    __in_z LPCWSTR wzValueName,
    __out LEGACY_REGISTRY_SPECIAL **ppRegistrySpecial
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSearchString = NULL;
    LPCWSTR wzSearchString = NULL;

    if (*wzSubKey == L'\0')
    {
        wzSearchString = wzValueName;
    }
    else
    {
        hr = StrAllocFormatted(&sczSearchString, L"%ls\\%ls", wzSubKey, wzValueName);
        ExitOnFailure(hr, "Failed to allocate search string while searching for special in registry legacy key information");

        wzSearchString = sczSearchString;
    }

    for (DWORD i = 0; i < pRegKey->cRegKeySpecials; ++i)
    {
        if (lstrcmpiW(pRegKey->rgRegKeySpecials[i].sczRegValueName, wzSearchString) == 0)
        {
            *ppRegistrySpecial = &pRegKey->rgRegKeySpecials[i];
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    ReleaseStr(sczSearchString);

    return hr;
}

static HRESULT RegSpecialValueReadFlags(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in DWORD dwValueType
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCfgValueName = NULL;
    BYTE *rgbBuffer = NULL;
    SIZE_T cbBuffer = 0;
    DWORD dwOffset = 0;
    BOOL fNewValue = FALSE;
    CONFIG_VALUE cvNewValue = { };

    switch (dwValueType)
    {
    case REG_BINARY:
        hr = RegReadBinary(hkKey, wzValueName, &rgbBuffer, &cbBuffer);
        ExitOnFailure(hr, "Failed to read binary value from registry");

        for (DWORD i = 0; i < pRegKeySpecial->cFlagsInfo; ++i)
        {
            hr = StrAllocFormatted(&sczCfgValueName, L"%ls%wc%ls", pRegKey->sczNamespace, NAMESPACE_DELIMITER_CHARACTER, pRegKeySpecial->rgFlagsInfo[i].sczCfgValueName);
            ExitOnFailure(hr, "Failed to format Cfg Value name with namespace");

            hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczCfgValueName);
            ExitOnFailure(hr, "Failed to add to dictionary value: %ls", sczCfgValueName);

            dwOffset = pRegKeySpecial->rgFlagsInfo[i].dwOffset;

            // If the offset requested is out of range, then it's deleted
            if (dwOffset / 8 > cbBuffer - 1)
            {
                ReleaseNullCfgValue(cvNewValue);
                hr = ValueSetDelete(NULL, pcdb->sczGuid, &cvNewValue);
                ExitOnFailure(hr, "Failed to initialize deleted value in memory");

                hr = ValueWrite(pcdb, pcdb->dwAppID, sczCfgValueName, &cvNewValue, TRUE, NULL);
                ExitOnFailure(hr, "Failed to delete boolean value");
            }
            else
            {
                fNewValue = (0x1 << (dwOffset % 8)) == (rgbBuffer[dwOffset / 8] & (0x1 << (dwOffset % 8)));
                ReleaseNullCfgValue(cvNewValue);
                hr = ValueSetBool(fNewValue, NULL, pcdb->sczGuid, &cvNewValue);
                ExitOnFailure(hr, "Failed to initialize deleted value in memory");

                hr = ValueWrite(pcdb, pcdb->dwAppID, sczCfgValueName, &cvNewValue, TRUE, NULL);
                ExitOnFailure(hr, "Failed to set boolean value");
            }
        }
        break;
    }

LExit:
    ReleaseStr(sczCfgValueName);
    ReleaseMem(rgbBuffer);
    ReleaseCfgValue(cvNewValue);

    return hr;
}

static HRESULT RegSpecialValueReadNonTypecasted(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCfgValueName = NULL;
    DWORD dwValue = 0;
    CONFIG_VALUE cvNewValue = { };
    BOOL fFound = TRUE;

    hr = MapRegSpecialToCfgValueName(pRegKey, pRegKeySpecial, &sczCfgValueName);
    ExitOnFailure(hr, "Failed to map registry special to cfg value name");

    switch (pRegKeySpecial->dwRegValueType)
    {
    case REG_DWORD:
        hr = RegReadNumber(hkKey, wzValueName, &dwValue);
        if (E_NOTFOUND == hr)
        {
            fFound = FALSE;
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to read dword value from registry");

        hr = ValueSetDword(dwValue, NULL, pcdb->sczGuid, &cvNewValue);
        ExitOnFailure(hr, "Failed to set dword value %ls in memory", sczCfgValueName);

        hr = ValueWrite(pcdb, pcdb->dwAppID, sczCfgValueName, &cvNewValue, TRUE, NULL);
        ExitOnFailure(hr, "Failed to set dword value: %ls", sczCfgValueName);
        break;
    case REG_NONE:
        fFound = FALSE;
        break;
    }

    if (fFound)
    {
        hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczCfgValueName);
        ExitOnFailure(hr, "Failed to add to dictionary value: %ls", sczCfgValueName);
    }

LExit:
    ReleaseStr(sczCfgValueName);
    ReleaseCfgValue(cvNewValue);

    return hr;
}

static HRESULT RegSpecialProductWriteBinary(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzRegKey,
    __in_z LPCWSTR wzRegValueName,
    __in HKEY *phkKey,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCfgValueName = NULL;
    BYTE *rgbBuffer = NULL;
    DWORD cbBuffer = 0;
    BOOL fValue = FALSE;
    BOOL fValueExists = FALSE;
    SCE_ROW_HANDLE sceRow = NULL;
    CONFIG_VALUE cvExistingValue = { };

    // Handle flags
    for (DWORD i = 0; i < pRegKeySpecial->cFlagsInfo; ++i)
    {
        const LEGACY_FLAGS_PARSE_INFO *pFlagsInfo = &(pRegKeySpecial->rgFlagsInfo[i]);

        hr = StrAllocFormatted(&sczCfgValueName, L"%ls%wc%ls", pRegKey->sczNamespace, NAMESPACE_DELIMITER_CHARACTER, pRegKeySpecial->rgFlagsInfo[i].sczCfgValueName);
        ExitOnFailure(hr, "Failed to format Cfg Value name with namespace");

        ReleaseNullSceRow(sceRow);

        hr = ValueFindRow(pcdb, pcdb->dwAppID, sczCfgValueName, &sceRow);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
            continue;
        }
        ExitOnFailure(hr, "Failed to find value for AppID: %u, Config Value named: %ls", pcdb->dwAppID, sczCfgValueName);

        ReleaseNullCfgValue(cvExistingValue);
        hr = ValueRead(pcdb, sceRow, &cvExistingValue);
        ExitOnFailure(hr, "Failed to read value: %ls", sczCfgValueName);

        if (VALUE_BOOL != cvExistingValue.cvType)
        {
            fValue = FALSE;
        }
        else
        {
            fValueExists = TRUE;
        }

        if (fValueExists)
        {
            cbBuffer = (pFlagsInfo->dwOffset / 8) + 1;
            hr = MemEnsureArraySize(reinterpret_cast<void **>(&rgbBuffer), cbBuffer, sizeof(BYTE), 0);
            ExitOnFailure(hr, "Failed to ensure byte array is of size: %u", cbBuffer);

            if (cvExistingValue.boolean.fValue)
            {
                rgbBuffer[pFlagsInfo->dwOffset / 8] |= (0x1 << (pFlagsInfo->dwOffset % 8));
            }
            else
            {
                rgbBuffer[pFlagsInfo->dwOffset / 8] &= ~(0x1 << (pFlagsInfo->dwOffset % 8));
            }
        }
        else
        {
            // The value doesn't exist in the database anymore, so don't allocate any more bytes to put it in,
            // but if the byte space for this flag is already allocated, make sure this value is set to zero
#pragma prefast(push)
#pragma prefast(disable:26017)
            if ((pFlagsInfo->dwOffset / 8) < cbBuffer)
            {
                rgbBuffer[pFlagsInfo->dwOffset / 8] &= ~(0x1 << (pFlagsInfo->dwOffset % 8));
            }
#pragma prefast(pop)
        }
    }

    if (NULL == rgbBuffer)
    {
        if (NULL != *phkKey)
        {
            hr = RegWriteString(*phkKey, wzRegValueName, NULL);
            ExitOnFailure(hr, "Failed to delete binary value: %ls", wzRegValueName);
        }
    }
    else
    {
        if (NULL == *phkKey)
        {
            hr = RegCreate(ManifestConvertToRootKey(pRegKey->dwRoot), wzRegKey, KEY_SET_VALUE | KEY_CREATE_SUB_KEY, phkKey);
            ExitOnFailure(hr, "Failed to create registry key in root: %u, key %ls", pRegKey->dwRoot, wzRegKey);
        }

        hr = RegWriteBinary(*phkKey, wzRegValueName, rgbBuffer, cbBuffer);
        ExitOnFailure(hr, "Failed to write binary value: %ls with %u bytes", wzRegValueName, cbBuffer);
    }

LExit:
    ReleaseSceRow(sceRow);
    ReleaseStr(sczCfgValueName);
    ReleaseMem(rgbBuffer);
    ReleaseCfgValue(cvExistingValue);

    return hr;
}
