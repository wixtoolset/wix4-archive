// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// This does various corrections to the name seen in arp like trimming whitespace, etc.
static HRESULT CorrectArpName(
    __in LPWSTR sczOriginalName,
    __out LPWSTR *psczCorrectedName
    );
static HRESULT GetArpProducts(
    __out ARP_PRODUCTS *pArpProducts,
    __in HKEY hkArp
    );
static HRESULT GetExeProductsFromApplications(
    __out EXE_PRODUCTS *pExeProducts,
    __in HKEY hkArp
    );
static HRESULT ReadArp(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwArpID,
    __out LEGACY_DETECT *pDetect
    );
static HRESULT ReadExe(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwArpID,
    __out LEGACY_DETECT *pDetect
    );
// Intentionally returns S_OK if no cached value is available
// If the cached value exists, adds it to the list of "values seen" this sync so it won't be deleted
static HRESULT ReadCache(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzPropertyName,
    __inout STRINGDICT_HANDLE shDictValuesSeen,
    __inout LEGACY_DETECTION *pDetection
    );
// If the cached value exists, adds it to the list of "values seen" this sync so it won't be deleted
static HRESULT ReadCachedValue(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzPropertyName,
    __inout STRINGDICT_HANDLE shDictValuesSeen,
    __out_z LPWSTR *psczPropertyValue
    );
static void FreeSingleDetect(
    LEGACY_DETECT *pDetect
    );

HRESULT DetectUpdateCache(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValueName = NULL;
    CONFIG_VALUE cvValue = { };
    LEGACY_DETECTION *pDetect = &pSyncProductSession->product.detect;

    for (DWORD i = 0; i < pDetect->cDetects; ++i)
    {
        // No point updating the cache for a product that wasn't found
        if (!pDetect->rgDetects[i].fFound)
        {
            continue;
        }

        ReleaseNullCfgValue(cvValue);
        switch (pDetect->rgDetects[i].ldtType)
        {
        case LEGACY_DETECT_TYPE_ARP:
            if (pDetect->rgDetects[i].arp.sczInstallLocationProperty && 0 < lstrlenW(pDetect->rgDetects[i].arp.wzInstallLocationValue))
            {
                hr = StrAllocString(&sczValueName, wzLegacyDetectCacheValuePrefix, 0);
                ExitOnFailure(hr, "Failed to copy detect cache value prefix");
                
                hr = StrAllocConcat(&sczValueName, pDetect->rgDetects[i].arp.sczInstallLocationProperty, 0);
                ExitOnFailure(hr, "Failed to concat value name to value prefix");

                hr = ValueSetString(pDetect->rgDetects[i].arp.wzInstallLocationValue, FALSE, NULL, pcdb->sczGuid, &cvValue);
                ExitOnFailure(hr, "Failed to set manifest contents as string value in memory");

                hr = ValueWrite(pcdb, pcdb->dwAppID, sczValueName, &cvValue, TRUE, NULL);
                ExitOnFailure(hr, "Failed to set cached value named '%ls' to '%ls'", sczValueName, pDetect->rgDetects[i].arp.wzInstallLocationValue);

                hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczValueName);
                ExitOnFailure(hr, "Failed to add file to list of files seen: %ls", sczValueName);
            }
            if (pDetect->rgDetects[i].arp.sczUninstallStringDirProperty && 0 < lstrlenW(pDetect->rgDetects[i].arp.wzUninstallStringDirValue))
            {
                hr = StrAllocString(&sczValueName, wzLegacyDetectCacheValuePrefix, 0);
                ExitOnFailure(hr, "Failed to copy detect cache value prefix");
                
                hr = StrAllocConcat(&sczValueName, pDetect->rgDetects[i].arp.sczUninstallStringDirProperty, 0);
                ExitOnFailure(hr, "Failed to concat value name to value prefix");

                hr = ValueSetString(pDetect->rgDetects[i].arp.wzUninstallStringDirValue, FALSE, NULL, pcdb->sczGuid, &cvValue);
                ExitOnFailure(hr, "Failed to set manifest contents as string value in memory");

                hr = ValueWrite(pcdb, pcdb->dwAppID, sczValueName, &cvValue, TRUE, NULL);
                ExitOnFailure(hr, "Failed to set cached value named '%ls' to '%ls'", sczValueName, pDetect->rgDetects[i].arp.wzUninstallStringDirValue);

                hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczValueName);
                ExitOnFailure(hr, "Failed to add file to list of files seen: %ls", sczValueName);
            }
            if (pDetect->rgDetects[i].arp.sczDisplayIconDirProperty && 0 < lstrlenW(pDetect->rgDetects[i].arp.wzDisplayIconDirValue))
            {
                hr = StrAllocString(&sczValueName, wzLegacyDetectCacheValuePrefix, 0);
                ExitOnFailure(hr, "Failed to copy detect cache value prefix");
                
                hr = StrAllocConcat(&sczValueName, pDetect->rgDetects[i].arp.sczDisplayIconDirProperty, 0);
                ExitOnFailure(hr, "Failed to concat value name to value prefix");

                hr = ValueSetString(pDetect->rgDetects[i].arp.wzDisplayIconDirValue, FALSE, NULL, pcdb->sczGuid, &cvValue);
                ExitOnFailure(hr, "Failed to set manifest contents as string value in memory");

                hr = ValueWrite(pcdb, pcdb->dwAppID, sczValueName, &cvValue, TRUE, NULL);
                ExitOnFailure(hr, "Failed to set cached value named '%ls' to '%ls'", sczValueName, pDetect->rgDetects[i].arp.wzDisplayIconDirValue);

                hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczValueName);
                ExitOnFailure(hr, "Failed to add file to list of files seen: %ls", sczValueName);
            }
            break;

        case LEGACY_DETECT_TYPE_EXE:
            if (pDetect->rgDetects[i].exe.sczFileDirProperty && 0 < lstrlenW(pDetect->rgDetects[i].exe.sczDetectedFileDir))
            {
                hr = StrAllocString(&sczValueName, wzLegacyDetectCacheValuePrefix, 0);
                ExitOnFailure(hr, "Failed to copy detect cache value prefix");
                
                hr = StrAllocConcat(&sczValueName, pDetect->rgDetects[i].exe.sczFileDirProperty, 0);
                ExitOnFailure(hr, "Failed to concat value name to value prefix");

                hr = ValueSetString(pDetect->rgDetects[i].exe.sczDetectedFileDir, FALSE, NULL, pcdb->sczGuid, &cvValue);
                ExitOnFailure(hr, "Failed to set manifest contents as string value in memory");

                hr = ValueWrite(pcdb, pcdb->dwAppID, sczValueName, &cvValue, TRUE, NULL);
                ExitOnFailure(hr, "Failed to set cached value named '%ls' to '%ls'", sczValueName, pDetect->rgDetects[i].exe.sczDetectedFileDir);

                hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczValueName);
                ExitOnFailure(hr, "Failed to add file to list of files seen: %ls", sczValueName);
            }
            break;

        default:
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected detect type encountered while updating cache: %u", pDetect->rgDetects[i].ldtType);
            break;
        }

        // If a product was found, break out of the loop
        if (pDetect->rgDetects[i].fFound)
        {
            break;
        }
    }

LExit:
    ReleaseCfgValue(cvValue);
    ReleaseStr(sczValueName);

    return hr;
}

HRESULT DetectGetArpProducts(
    __out ARP_PRODUCTS *pArpProducts
    )
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;

    hr = RegOpen(HKEY_CURRENT_USER, wzArpPath, KEY_READ | KEY_WOW64_32KEY, &hk);
    if (SUCCEEDED(hr))
    {
        ExitOnFailure(hr, "Failed to open 32-bit HKCU ARP key");

        hr = GetArpProducts(pArpProducts, hk);
        ExitOnFailure(hr, "Failed to enumerate ARP products in 32-bit HKCU ARP key");

        ReleaseRegKey(hk);
    }

    hr = RegOpen(HKEY_LOCAL_MACHINE, wzArpPath, KEY_READ | KEY_WOW64_32KEY, &hk);
    if (SUCCEEDED(hr))
    {
        ExitOnFailure(hr, "Failed to open 32-bit HKLM ARP key");

        hr = GetArpProducts(pArpProducts, hk);
        ExitOnFailure(hr, "Failed to enumerate ARP products in 32-bit HKLM ARP key");

        ReleaseRegKey(hk);
    }

    if (UtilIs64BitSystem())
    {
        hr = RegOpen(HKEY_LOCAL_MACHINE, wzArpPath, KEY_READ | KEY_WOW64_64KEY, &hk);
        if (SUCCEEDED(hr))
        {
            ExitOnFailure(hr, "Failed to open 64-bit HKLM ARP key");

            hr = GetArpProducts(pArpProducts, hk);
            ExitOnFailure(hr, "Failed to enumerate ARP products in 64-bit HKLM ARP key");

            ReleaseRegKey(hk);
        }
    }

    hr = DictCreateWithEmbeddedKey(&pArpProducts->shProductsFoundByDisplayName, pArpProducts->cProducts, reinterpret_cast<void **>(&pArpProducts->rgProducts), offsetof(ARP_PRODUCT, sczDisplayName), DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary with embedded key to track products seen in ARP by display name");

    hr = DictCreateWithEmbeddedKey(&pArpProducts->shProductsFoundByRegKeyName, pArpProducts->cProducts, reinterpret_cast<void **>(&pArpProducts->rgProducts), offsetof(ARP_PRODUCT, sczRegKeyName), DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary with embedded key to track products seen in ARP by reg key name");

    for (DWORD i = 0; i < pArpProducts->cProducts; ++i)
    {
        hr = DictAddValue(pArpProducts->shProductsFoundByDisplayName, pArpProducts->rgProducts + i);
        ExitOnFailure(hr, "Failed to add to dictionary product with display name: %ls", pArpProducts->rgProducts[i].sczDisplayName);

        hr = DictAddValue(pArpProducts->shProductsFoundByRegKeyName, pArpProducts->rgProducts + i);
        ExitOnFailure(hr, "Failed to add to dictionary product with regkey name: %ls", pArpProducts->rgProducts[i].sczRegKeyName);
    }

    hr = S_OK;

LExit:
    ReleaseRegKey(hk);

    return hr;
}

HRESULT DetectGetExeProducts(
    __out EXE_PRODUCTS *pExeProducts
    )
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;

    hr = RegOpen(HKEY_CURRENT_USER, wzApplicationsPath, KEY_READ, &hk);
    if (SUCCEEDED(hr))
    {
        ExitOnFailure(hr, "Failed to open HKCU Applications key");

        hr = GetExeProductsFromApplications(pExeProducts, hk);
        ExitOnFailure(hr, "Failed to enumerate Applications products in HKCU Applications key");

        ReleaseRegKey(hk);
    }

    hr = RegOpen(HKEY_LOCAL_MACHINE, wzApplicationsPath, KEY_WOW64_32KEY | KEY_READ, &hk);
    if (SUCCEEDED(hr))
    {
        ExitOnFailure(hr, "Failed to open HKLM Applications key");

        hr = GetExeProductsFromApplications(pExeProducts, hk);
        ExitOnFailure(hr, "Failed to enumerate Applications products in 32-bit HKLM Applications key");

        ReleaseRegKey(hk);
    }

    if (UtilIs64BitSystem())
    {
        hr = RegOpen(HKEY_LOCAL_MACHINE, wzApplicationsPath, KEY_WOW64_64KEY | KEY_READ, &hk);
        if (SUCCEEDED(hr))
        {
            ExitOnFailure(hr, "Failed to open HKLM Applications key");

            hr = GetExeProductsFromApplications(pExeProducts, hk);
            ExitOnFailure(hr, "Failed to enumerate Applications products in 64-bit HKLM Applications key");

            ReleaseRegKey(hk);
        }
    }

    hr = DictCreateWithEmbeddedKey(&pExeProducts->shProductsFound, pExeProducts->cProducts, reinterpret_cast<void **>(&pExeProducts->rgProducts), offsetof(EXE_PRODUCT, sczFileName), DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary with embedded key to track exes seen");

    for (DWORD i = 0; i < pExeProducts->cProducts; ++i)
    {
        hr = DictAddValue(pExeProducts->shProductsFound, pExeProducts->rgProducts + i);
        ExitOnFailure(hr, "Failed to add to dictionary product with filename: %ls", pExeProducts->rgProducts[i].sczFilePath);
    }

    hr = S_OK;

LExit:
    ReleaseRegKey(hk);

    return hr;
}

HRESULT DetectProduct(
    __in CFGDB_STRUCT *pcdb,
    __in BOOL fJustReadCache,
    __in ARP_PRODUCTS *pArpProducts,
    __in EXE_PRODUCTS *pExeProducts,
    __inout LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    )
{
    HRESULT hr = S_OK;
    BOOL fProductDetected = FALSE;
    ARP_PRODUCT *pFoundArpProduct = NULL;
    EXE_PRODUCT *pFoundExeProduct = NULL;
    LEGACY_DETECTION *pDetect = &pSyncProductSession->product.detect;

    // Go through each detect and fill out the relevant information
    if (!fJustReadCache)
    {
        for (DWORD i = 0; i < pDetect->cDetects; ++i)
        {
            pDetect->rgDetects[i].fFound = FALSE;
            switch (pDetect->rgDetects[i].ldtType)
            {
            case LEGACY_DETECT_TYPE_ARP:
                if (pDetect->rgDetects[i].arp.sczDisplayName != NULL)
                {
                    hr = DictGetValue(pArpProducts->shProductsFoundByDisplayName, pDetect->rgDetects[i].arp.sczDisplayName, reinterpret_cast<void **>(&pFoundArpProduct));
                }
                else
                {
                    hr = DictGetValue(pArpProducts->shProductsFoundByRegKeyName, pDetect->rgDetects[i].arp.sczRegKeyName, reinterpret_cast<void **>(&pFoundArpProduct));
                }

                if (E_NOTFOUND == hr)
                {
                    hr = S_OK;
                }
                else if (FAILED(hr))
                {
                    ExitOnFailure(hr, "Failed to lookup display name %ls in arp dictionary", pDetect->rgDetects[i].arp.sczDisplayName);
                }
                else
                {
                    pDetect->rgDetects[i].fFound = TRUE;
                    pDetect->rgDetects[i].arp.wzInstallLocationValue = pFoundArpProduct->sczInstallLocation;
                    pDetect->rgDetects[i].arp.wzUninstallStringDirValue = pFoundArpProduct->sczUninstallStringDir;
                    pDetect->rgDetects[i].arp.wzDisplayIconDirValue = pFoundArpProduct->sczDisplayIconDir;
                    break;
                }
                break;

            case LEGACY_DETECT_TYPE_EXE:
                hr = DictGetValue(pExeProducts->shProductsFound, pDetect->rgDetects[i].exe.sczFileName, reinterpret_cast<void **>(&pFoundExeProduct));
                if (E_NOTFOUND == hr)
                {
                    hr = S_OK;
                }
                else
                {
                    pDetect->rgDetects[i].fFound = TRUE;
                    pDetect->rgDetects[i].exe.wzFileDirValue = pFoundExeProduct->sczFileDir;
                }

                if (NULL != pDetect->rgDetects[i].exe.wzFileDirValue)
                {
                    hr = PathGetDirectory(pDetect->rgDetects[i].exe.wzFileDirValue, &pDetect->rgDetects[i].exe.sczDetectedFileDir);
                    ExitOnFailure(hr, "Failed to get directory from path: %ls", pDetect->rgDetects[i].exe.wzFileDirValue);
                }

                break;

            default:
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Unexpected detect type encountered: %d", pDetect->rgDetects[i].ldtType);
                break;
            }

            if (pDetect->rgDetects[i].fFound)
            {
                fProductDetected = TRUE;
                break;
            }
        }
    }

    if (!fProductDetected)
    {
        for (DWORD i = 0; i < pDetect->cDetects; ++i)
        {
            switch (pDetect->rgDetects[i].ldtType)
            {
            case LEGACY_DETECT_TYPE_ARP:
                // Read in any values from cache
                hr = ReadCache(pcdb, pDetect->rgDetects[i].arp.sczInstallLocationProperty, pSyncProductSession->shDictValuesSeen, pDetect);
                ExitOnFailure(hr, "Failed to read cached value %ls", pDetect->rgDetects[i].arp.sczInstallLocationProperty);

                hr = ReadCache(pcdb, pDetect->rgDetects[i].arp.sczUninstallStringDirProperty, pSyncProductSession->shDictValuesSeen, pDetect);
                ExitOnFailure(hr, "Failed to read cached value %ls", pDetect->rgDetects[i].arp.sczUninstallStringDirProperty);

                hr = ReadCache(pcdb, pDetect->rgDetects[i].arp.sczDisplayIconDirProperty, pSyncProductSession->shDictValuesSeen, pDetect);
                ExitOnFailure(hr, "Failed to read cached value %ls", pDetect->rgDetects[i].arp.sczDisplayIconDirProperty);
                break;
            case LEGACY_DETECT_TYPE_EXE:
                hr = ReadCache(pcdb, pDetect->rgDetects[i].exe.sczFileDirProperty, pSyncProductSession->shDictValuesSeen, pDetect);
                ExitOnFailure(hr, "Failed to read cached value %ls", pDetect->rgDetects[i].exe.sczFileDirProperty);
                break;
            default:
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Unexpected detect type encountered in 2nd loop: %d", pDetect->rgDetects[i].ldtType);
                break;
            }
        }
    }
    else if (!fJustReadCache)
    {
        hr = DetectUpdateCache(pcdb, pSyncProductSession);
        ExitOnFailure(hr, "Failed to update cached detection results for AppID: %u", pcdb->dwAppID);
    }

LExit:
    return hr;
}

HRESULT DetectExpandDirectoryPath(
    __in LPCWSTR wzInput,
    __in LEGACY_DETECTION *pDetect,
    __deref_out LPWSTR *psczOutput
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPropertyValue = NULL;
    LPWSTR sczPropertyName = NULL;
    LPCWSTR wzFindResult = NULL;
    LPCWSTR wzPathAfterColon = NULL;
    LEGACY_CACHED_DETECTION_RESULT *pCacheLookupResult = NULL;

    wzFindResult = wcsstr(wzInput, L":\\");
    if (NULL == wzFindResult)
    {
        return E_NOTFOUND;
    }

    hr = StrAllocString(&sczPropertyName, wzInput, wzFindResult - wzInput);
    ExitOnFailure(hr, "Failed to allocate copy of property name from front of value path string");

    // Get past the ":\"
    wzPathAfterColon = wzFindResult + 2;

    for (DWORD i = 0; i < pDetect->cDetects; ++i)
    {
        switch (pDetect->rgDetects[i].ldtType)
        {
        case LEGACY_DETECT_TYPE_ARP:
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pDetect->rgDetects[i].arp.sczInstallLocationProperty, -1, sczPropertyName, -1)
                && 0 < lstrlenW(pDetect->rgDetects[i].arp.wzInstallLocationValue))
            {
                hr = StrAllocString(&sczPropertyValue, pDetect->rgDetects[i].arp.wzInstallLocationValue, 0);
                ExitOnFailure(hr, "Failed to copy value of InstallLocation", pDetect->rgDetects[i].arp.wzInstallLocationValue);

                hr = PathConcat(sczPropertyValue, wzPathAfterColon, psczOutput);
                ExitOnFailure(hr, "Failed to concatenate paths while expanding detected arp path");

                ExitFunction1(hr = S_OK);
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pDetect->rgDetects[i].arp.sczUninstallStringDirProperty, -1, sczPropertyName, -1)
                && 0 < lstrlenW(pDetect->rgDetects[i].arp.wzUninstallStringDirValue))
            {
                hr = StrAllocString(&sczPropertyValue, pDetect->rgDetects[i].arp.wzUninstallStringDirValue, 0);
                ExitOnFailure(hr, "Failed to copy value of InstallLocation", pDetect->rgDetects[i].arp.wzUninstallStringDirValue);

                hr = PathConcat(sczPropertyValue, wzPathAfterColon, psczOutput);
                ExitOnFailure(hr, "Failed to concatenate paths while expanding detected arp path");

                ExitFunction1(hr = S_OK);
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pDetect->rgDetects[i].arp.sczDisplayIconDirProperty, -1, sczPropertyName, -1)
                && 0 < lstrlenW(pDetect->rgDetects[i].arp.wzInstallLocationValue))
            {
                hr = StrAllocString(&sczPropertyValue, pDetect->rgDetects[i].arp.wzDisplayIconDirValue, 0);
                ExitOnFailure(hr, "Failed to copy value of InstallLocation", pDetect->rgDetects[i].arp.wzDisplayIconDirValue);

                hr = PathConcat(sczPropertyValue, wzPathAfterColon, psczOutput);
                ExitOnFailure(hr, "Failed to concatenate paths while expanding detected arp path");

                ExitFunction1(hr = S_OK);
            }
            break;

        case LEGACY_DETECT_TYPE_EXE:
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pDetect->rgDetects[i].exe.sczFileDirProperty, -1, sczPropertyName, -1)
                && 0 < lstrlenW(pDetect->rgDetects[i].exe.wzFileDirValue))
            {
                hr = StrAllocString(&sczPropertyValue, pDetect->rgDetects[i].exe.wzFileDirValue, 0);
                ExitOnFailure(hr, "Failed to copy value of file dir", pDetect->rgDetects[i].exe.wzFileDirValue);

                hr = PathConcat(sczPropertyValue, wzPathAfterColon, psczOutput);
                ExitOnFailure(hr, "Failed to concatenate paths while expanding detected exe path");

                ExitFunction1(hr = S_OK);
            }
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "An invalid detect type %d was found while expanding legacy directory path", pDetect->rgDetects[i].ldtType);
            break;
        }
    }

    // Fall back to cached value
    hr = DictGetValue(pDetect->shCachedDetectionPropertyValues, sczPropertyName, reinterpret_cast<void**>(&pCacheLookupResult));
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to lookup cached property value");

    hr = StrAllocString(&sczPropertyValue, pCacheLookupResult->sczPropertyValue, 0);
    ExitOnFailure(hr, "Failed to copy value of cached dir: %ls", pCacheLookupResult->sczPropertyValue);

    hr = PathConcat(sczPropertyValue, wzPathAfterColon, psczOutput);
    ExitOnFailure(hr, "Failed to concatenate paths while expanding detected exe path");

LExit:
    ReleaseStr(sczPropertyName);
    ReleaseStr(sczPropertyValue);

    return hr;
}

void DetectFree(
    __in LEGACY_DETECTION *pDetection
    )
{
    for (DWORD i = 0; i < pDetection->cDetects; ++i)
    {
        FreeSingleDetect(pDetection->rgDetects + i);
    }
    pDetection->cDetects = 0;
    ReleaseNullMem(pDetection->rgDetects);
    ReleaseNullDict(pDetection->shCachedDetectionPropertyValues);

    for (DWORD i = 0; i < pDetection->cCachedDetectionProperties; ++i)
    {
        ReleaseStr(pDetection->rgCachedDetectionProperties[i].sczPropertyName);
        ReleaseStr(pDetection->rgCachedDetectionProperties[i].sczPropertyValue);
    }
    ReleaseNullMem(pDetection->rgCachedDetectionProperties);
    pDetection->cCachedDetectionProperties = 0;
}

void DetectFreeArpProducts(
    __in ARP_PRODUCTS *pArpProducts
    )
{
    for (DWORD i = 0; i < pArpProducts->cProducts; ++i)
    {
        ReleaseStr(pArpProducts->rgProducts[i].sczDisplayName);
        ReleaseStr(pArpProducts->rgProducts[i].sczRegKeyName);
        ReleaseStr(pArpProducts->rgProducts[i].sczInstallLocation);
        ReleaseStr(pArpProducts->rgProducts[i].sczUninstallStringDir);
        ReleaseStr(pArpProducts->rgProducts[i].sczDisplayIconDir);
    }

    ReleaseNullMem(pArpProducts->rgProducts);
    pArpProducts->cProducts = 0;
    ReleaseNullDict(pArpProducts->shProductsFoundByDisplayName);
    ReleaseNullDict(pArpProducts->shProductsFoundByRegKeyName);
}

void DetectFreeExeProducts(
    __in EXE_PRODUCTS *pExeProducts
    )
{
    for (DWORD i = 0; i < pExeProducts->cProducts; ++i)
    {
        ReleaseStr(pExeProducts->rgProducts[i].sczFilePath);
        ReleaseStr(pExeProducts->rgProducts[i].sczFileName);
        ReleaseStr(pExeProducts->rgProducts[i].sczFileDir);
    }

    ReleaseNullMem(pExeProducts->rgProducts);
    pExeProducts->cProducts = 0;
    ReleaseNullDict(pExeProducts->shProductsFound);
}

// static functions
static HRESULT CorrectArpName(
    __in LPWSTR sczOriginalName,
    __out LPWSTR *psczCorrectedName
    )
{
    HRESULT hr = S_OK;

    // trim leading whitespace
    while (*sczOriginalName == L' ' || *sczOriginalName == L'\t')
    {
        ++sczOriginalName;
    }

    DWORD dwLastIndex = lstrlenW(sczOriginalName) - 1;

    // trim trailing whitespace
    while(dwLastIndex > 0 && (sczOriginalName[dwLastIndex] == L' ' || sczOriginalName[dwLastIndex] == L'\t'))
    {
        sczOriginalName[dwLastIndex] = L'\0';
        --dwLastIndex;
    }

    // TODO: allow some way (specified in manifest) of removing versions

    hr = StrAllocString(psczCorrectedName, sczOriginalName, 0);
    ExitOnFailure(hr, "Failed to allocate copy of string while correcting arp product name: %ls", sczOriginalName);

LExit:
    return hr;
}

static HRESULT GetArpProducts(
    __out ARP_PRODUCTS *pArpProducts,
    __in HKEY hkArp
    )
{
    HRESULT hr = S_OK;
    HKEY hkSubkey = NULL;
    LPWSTR sczSubkeyName = NULL;
    LPWSTR sczDisplayName = NULL;
    LPWSTR sczUninstallString = NULL;
    LPWSTR sczDisplayIcon = NULL;
    DWORD dwEnumIndex = 0;
    DWORD dwProductIndex = 0;

    while (E_NOMOREITEMS != (hr = RegKeyEnum(hkArp, dwEnumIndex, &sczSubkeyName)))
    {
        if (FAILED(hr))
        {
            goto Skip;
        }

        dwProductIndex = pArpProducts->cProducts;

        hr = RegOpen(hkArp, sczSubkeyName, KEY_READ, &hkSubkey);
        if (FAILED(hr))
        {
            hr = S_OK;
            goto Skip;
        }
        ExitOnFailure(hr, "Failed to open arp subkey: %ls", sczSubkeyName);

        hr = RegReadString(hkSubkey, L"DisplayName", &sczDisplayName);
        if (FAILED(hr))
        {
            hr = S_OK;
            goto Skip;
        }
        else
        {
            ExitOnFailure(hr, "Failed to read DisplayName from registry for subkey: %ls", sczSubkeyName);

            if (DWORD_MAX == pArpProducts->cProducts)
            {
                hr = E_UNEXPECTED;
                ExitOnFailure(hr, "DWORD Overflow while enumerating products in ARP");
            }

            hr = MemEnsureArraySize(reinterpret_cast<void **>(&pArpProducts->rgProducts), pArpProducts->cProducts + 1, sizeof(ARP_PRODUCT), 50);
            ExitOnFailure(hr, "Failed to resize products array while enumerating products in arp");
            ++pArpProducts->cProducts;

            pArpProducts->rgProducts[dwProductIndex].sczRegKeyName = sczSubkeyName;
            sczSubkeyName = NULL;

            hr = CorrectArpName(sczDisplayName, &pArpProducts->rgProducts[dwProductIndex].sczDisplayName);
            ExitOnFailure(hr, "Failed to correct displayname seen in arp: %ls", sczDisplayName);

            hr = RegReadString(hkSubkey, L"InstallLocation", &pArpProducts->rgProducts[dwProductIndex].sczInstallLocation);
            if (E_FILENOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to read registry string InstallLocation under subkey: %ls", sczSubkeyName);

            hr = RegReadString(hkSubkey, L"UninstallString", &sczUninstallString);
            if (E_FILENOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to read registry string UninstallString under subkey: %ls", sczSubkeyName);

            hr = PathGetDirectory(sczUninstallString, &pArpProducts->rgProducts[dwProductIndex].sczUninstallStringDir);
            ExitOnFailure(hr, "Failed to get directory portion of UninstallString");

            hr = RegReadString(hkSubkey, L"DisplayIcon", &sczDisplayIcon);
            if (E_FILENOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to read registry string DisplayIcon under subkey: %ls", sczSubkeyName);

            hr = PathGetDirectory(sczDisplayIcon, &pArpProducts->rgProducts[dwProductIndex].sczDisplayIconDir);
            ExitOnFailure(hr, "Failed to get directory portion of DisplayIcon string");
        }

    Skip:
        ReleaseRegKey(hkSubkey);
        ++dwEnumIndex;
    }

    hr = S_OK;

LExit:
    ReleaseRegKey(hkSubkey);
    ReleaseStr(sczSubkeyName);
    ReleaseStr(sczDisplayName);
    ReleaseStr(sczUninstallString);
    ReleaseStr(sczDisplayIcon);

    return hr;
}

static HRESULT GetExeProductsFromApplications(
    __out EXE_PRODUCTS *pExeProducts,
    __in HKEY hkApplications
    )
{
    HRESULT hr = S_OK;
    HKEY hkSubkey = NULL;
    HKEY hkShellkey = NULL;
    HKEY hkCommandkey = NULL;
    LPWSTR sczSubkeyName = NULL;
    LPWSTR sczInnerSubkeyName = NULL;
    LPWSTR sczShellPath = NULL;
    LPWSTR sczCommandPath = NULL;
    LPWSTR sczRawCommand = NULL;
    LPWSTR sczFilePath = NULL;
    LPWSTR wzSecondQuote = NULL;
    DWORD dwEnumIndex = 0;
    DWORD dwInnerEnumIndex = 0;
    DWORD dwProductIndex = 0;

    while (E_NOMOREITEMS != (hr = RegKeyEnum(hkApplications, dwEnumIndex, &sczSubkeyName)))
    {
        if (FAILED(hr))
        {
            goto Skip;
        }

        dwProductIndex = pExeProducts->cProducts;

        hr = StrAllocFormatted(&sczShellPath, L"%ls\\shell", sczSubkeyName);
        ExitOnFailure(hr, "Failed to allocate path to shell subkey");

        ReleaseRegKey(hkShellkey);
        hr = RegOpen(hkApplications, sczShellPath, KEY_READ, &hkShellkey);
        if (FAILED(hr))
        {
            hr = S_OK;
            goto Skip;
        }

        dwInnerEnumIndex = 0;
        while (E_NOMOREITEMS != (hr = RegKeyEnum(hkShellkey, dwInnerEnumIndex, &sczInnerSubkeyName)))
        {
            if (FAILED(hr))
            {
                goto InnerSkip;
            }

            hr = StrAllocFormatted(&sczCommandPath, L"%ls\\command", sczInnerSubkeyName);
            ExitOnFailure(hr, "Failed to allocate path to command subkey");

            ReleaseRegKey(hkCommandkey);
            hr = RegOpen(hkShellkey, sczCommandPath, KEY_READ, &hkCommandkey);
            if (FAILED(hr))
            {
                hr = S_OK;
                goto InnerSkip;
            }

            hr = RegReadString(hkCommandkey, NULL, &sczRawCommand);
            if (FAILED(hr))
            {
                hr = S_OK;
                goto InnerSkip;
            }

            if (L'\"' == sczRawCommand[0])
            {
                wzSecondQuote = wcschr(sczRawCommand + 1, L'\"');
                if (NULL == wzSecondQuote)
                {
                    // The quote never ended, so this is probably a bad command - skip it
                    Trace(REPORT_DEBUG, "Failed to find ending quote in applications path: %ls", sczRawCommand);
                    goto InnerSkip;
                }

                *wzSecondQuote = L'\0';
                hr = StrAllocString(&sczFilePath, sczRawCommand + 1, 0);
                ExitOnFailure(hr, "Failed to allocate file path string from command string");

                // Only consider it valid if the found file actually exists
                if (FileExistsEx(sczFilePath, NULL))
                {
                    dwProductIndex = pExeProducts->cProducts;

                    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pExeProducts->rgProducts), pExeProducts->cProducts + 1, sizeof(EXE_PRODUCT), 50);
                    ExitOnFailure(hr, "Failed to resize products array while enumerating products in exe");
                    ++pExeProducts->cProducts;
            
                    hr = PathGetDirectory(sczFilePath, &pExeProducts->rgProducts[dwProductIndex].sczFileDir);
                    ExitOnFailure(hr, "Failed to get directory of file path: %ls", sczFilePath);

                    hr = StrAllocString(&pExeProducts->rgProducts[dwProductIndex].sczFileName, sczSubkeyName, 0);
                    ExitOnFailure(hr, "Failed to copy subkey name");

                    pExeProducts->rgProducts[dwProductIndex].sczFilePath = sczFilePath;
                    sczFilePath = NULL;
                }
            }
            else
            {
                // We don't support non-quoted file paths today - skip it.
                Trace(REPORT_DEBUG, "This applications path is not quoted, and is not yet supported: %ls", sczRawCommand);
                goto InnerSkip;
            }

        InnerSkip:
            ++dwInnerEnumIndex;
        }


    Skip:
        ++dwEnumIndex;
    }

    hr = S_OK;

LExit:
    ReleaseRegKey(hkSubkey);
    ReleaseRegKey(hkShellkey);
    ReleaseRegKey(hkCommandkey);
    ReleaseStr(sczSubkeyName);
    ReleaseStr(sczInnerSubkeyName);
    ReleaseStr(sczShellPath);
    ReleaseStr(sczCommandPath);
    ReleaseStr(sczRawCommand);
    ReleaseStr(sczFilePath);

    return hr;
}

static HRESULT ReadCache(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzPropertyName,
    __inout STRINGDICT_HANDLE shDictValuesSeen,
    __inout LEGACY_DETECTION *pDetection
    )
{
    HRESULT hr = S_OK;
    DWORD dwInsertedIndex = 0;
    LPWSTR sczPropertyValue = NULL;
    LPCWSTR wzFetchedValue = NULL;

    if (0 == lstrlenW(wzPropertyName))
    {
        ExitFunction1(hr = S_OK);
    }

    hr = DictGetValue(pDetection->shCachedDetectionPropertyValues, wzPropertyName, const_cast<void **>(reinterpret_cast<const void**>(&wzFetchedValue)));
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;

        hr = ReadCachedValue(pcdb, wzPropertyName, shDictValuesSeen, &sczPropertyValue);
        if (E_NOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        ExitOnFailure(hr, "Failed to read cached value for property %ls", wzPropertyName);

        hr = MemEnsureArraySize(reinterpret_cast<void**>(&pDetection->rgCachedDetectionProperties), pDetection->cCachedDetectionProperties + 1, sizeof(LEGACY_CACHED_DETECTION_RESULT), 3);
        ExitOnFailure(hr, "Failed to resize cached detection property values array to size %u", pDetection->cCachedDetectionProperties + 1);
        dwInsertedIndex = pDetection->cCachedDetectionProperties;
        ++pDetection->cCachedDetectionProperties;

        hr = StrAllocString(&pDetection->rgCachedDetectionProperties[dwInsertedIndex].sczPropertyName, wzPropertyName, 0);
        ExitOnFailure(hr, "Failed to copy property name: %ls", wzPropertyName);

        pDetection->rgCachedDetectionProperties[dwInsertedIndex].sczPropertyValue = sczPropertyValue;
        sczPropertyValue = NULL;

        hr = DictAddValue(pDetection->shCachedDetectionPropertyValues, pDetection->rgCachedDetectionProperties + dwInsertedIndex);
        ExitOnFailure(hr, "Failed to add item to dictionary");
    }
    ExitOnFailure(hr, "Failed to lookup property name in cached detection property values dict: %ls", wzPropertyName);

LExit:
    ReleaseStr(sczPropertyValue);

    return hr;
}

static HRESULT ReadCachedValue(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzPropertyName,
    __inout STRINGDICT_HANDLE shDictValuesSeen,
    __out_z LPWSTR *psczPropertyValue
    )
{
    HRESULT hr = S_OK;
    CONFIG_VALUE cvValue = { };
    LPWSTR sczValueName = NULL;
    SCE_ROW_HANDLE sceValueRow = NULL;

    hr = StrAllocString(&sczValueName, wzLegacyDetectCacheValuePrefix, 0);
    ExitOnFailure(hr, "Failed to copy detect cache value prefix");

    hr = StrAllocConcat(&sczValueName, wzPropertyName, 0);
    ExitOnFailure(hr, "Failed to concat value name to value prefix");

    hr = ValueFindRow(pcdb, pcdb->dwAppID, sczValueName, &sceValueRow);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to find config value for cached directory named: %ls", sczValueName);

    hr = DictAddKey(shDictValuesSeen, sczValueName);
    ExitOnFailure(hr, "Failed to add cached value to values seen during sync");

    hr = ValueRead(pcdb, sceValueRow, &cvValue);
    ExitOnFailure(hr, "Failed to read cached directory");

    if (VALUE_DELETED == cvValue.cvType)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    else if (VALUE_STRING != cvValue.cvType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Stored cached value was not of type string");
    }

    ReleaseStr(*psczPropertyValue);
    *psczPropertyValue = cvValue.string.sczValue;
    cvValue.string.sczValue = NULL;

LExit:
    ReleaseCfgValue(cvValue);
    ReleaseStr(sczValueName);
    ReleaseSceRow(sceValueRow);

    return hr;
}

void FreeSingleDetect(
    LEGACY_DETECT *pDetect
    )
{
    switch (pDetect->ldtType)
    {
    case LEGACY_DETECT_TYPE_ARP:
        ReleaseStr(pDetect->arp.sczDisplayName);
        ReleaseStr(pDetect->arp.sczRegKeyName);
        ReleaseStr(pDetect->arp.sczInstallLocationProperty);
        ReleaseStr(pDetect->arp.sczUninstallStringDirProperty);
        ReleaseStr(pDetect->arp.sczDisplayIconDirProperty);
        break;

    case LEGACY_DETECT_TYPE_EXE:
        ReleaseStr(pDetect->exe.sczFileName);
        ReleaseStr(pDetect->exe.sczDetectedFileDir);
        ReleaseStr(pDetect->exe.sczFileDirProperty);
        ReleaseStr(pDetect->exe.sczFileDirCachedValue);
        break;

    case LEGACY_DETECT_TYPE_INVALID:
        // Nothing to free, but don't error, because this one likely was just never initialized
        break;

    default:
        AssertSz(FALSE, "Unexpected legacy detect type found while freeing detect list");
        break;
    }
}

