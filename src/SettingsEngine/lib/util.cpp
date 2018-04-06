// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

PFN_GETSYSTEMTIME SystemTimeGetter = GetSystemTime;

const LEGACY_DIRECTORY_MAP LEGACY_DIRECTORIES[] = {
    { CSIDL_MYDOCUMENTS, L"MyDocumentsFolder:\\", NULL },
    { CSIDL_APPDATA, L"AppDataFolder:\\", NULL },
    { CSIDL_LOCAL_APPDATA, L"LocalAppDataFolder:\\", NULL },
    { CSIDL_COMMON_APPDATA, L"CommonAppDataFolder:\\", NULL },
    { CSIDL_MYDOCUMENTS, L"MyGamesFolder:\\", L"My Games\\" },
    { CSIDL_PROFILE, L"ProfileFolder:\\", NULL },
    };

static HRESULT IsProductInDictAndAddToDict(
    __in STRINGDICT_HANDLE shDictProductsSeen,
    __in LPCWSTR wzName,
    __in LPCWSTR wzVersion,
    __in LPCWSTR wzPublicKey,
    __out BOOL *pfResult
    );
// Expects the same product to already be set in both databases. Syncs only that product.
static HRESULT SyncSingleProduct(
    __in CFGDB_STRUCT *pcdb1,
    __in CFGDB_STRUCT *pcdb2,
    __in BOOL fRegistered,
    __out CONFLICT_PRODUCT **prgConflictProducts
    );
static HRESULT UtilSyncAllProductsHelp(
    __in CFGDB_STRUCT *pcdb1,
    __in LEGACY_SYNC_SESSION *pSyncSession,
    __in CFGDB_STRUCT *pcdb2,
    __in_opt STRINGDICT_HANDLE shDictProductsSeen,
    __out CONFLICT_PRODUCT **prgConflictProducts,
    __out DWORD *pcConflictProducts
    );

HRESULT UtilSyncDb(
    __in CFGDB_STRUCT *pcdbRemote,
    __deref_out_ecount_opt(*pcProduct) CONFLICT_PRODUCT **prgcpProductList,
    __out DWORD *pcProduct
    )
{
    HRESULT hr = S_OK;
    DWORD dwOriginalAppIDLocal = 0;
    DWORD dwOriginalAppIDRemote = 0;

    dwOriginalAppIDLocal = pcdbRemote->pcdbLocal->dwAppID;
    dwOriginalAppIDRemote = pcdbRemote->dwAppID;

    CfgReleaseConflictProductArray(*prgcpProductList, *pcProduct);
    *prgcpProductList = NULL;
    *pcProduct = 0;
    hr = UtilSyncAllProducts(pcdbRemote, prgcpProductList, pcProduct);
    ExitOnFailure(hr, "Failed to sync with remote database");

LExit:
    // Restore the previous AppIDs that were set before syncing began
    pcdbRemote->dwAppID = dwOriginalAppIDRemote;
    pcdbRemote->pcdbLocal->dwAppID = dwOriginalAppIDLocal;

    return hr;
}

HRESULT UtilSyncAllProducts(
    __in CFGDB_STRUCT *pcdbRemote,
    __out CONFLICT_PRODUCT **prgConflictProducts,
    __out DWORD *pcConflictProducts
    )
{
    HRESULT hr = S_OK;
    STRINGDICT_HANDLE shDictProductsSeen = NULL;
    LEGACY_SYNC_SESSION syncSession = { };
    CONFLICT_PRODUCT *pConflictProductTemp = NULL;

    hr = ProductSet(pcdbRemote->pcdbLocal, wzCfgProductId, wzCfgVersion, wzCfgPublicKey, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set product to cfg product in local db");

    hr = ProductSet(pcdbRemote, wzCfgProductId, wzCfgVersion, wzCfgPublicKey, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set product to cfg product in remote db");

    hr = SyncSingleProduct(pcdbRemote->pcdbLocal, pcdbRemote, TRUE, &pConflictProductTemp);
    ExitOnFailure(hr, "Failed to sync single product");

    if (NULL != pConflictProductTemp)
    {
        hr = StrAllocString(&pConflictProductTemp->sczProductName, wzCfgProductId, 0);
        ExitOnFailure(hr, "Failed to copy product name");

        hr = StrAllocString(&pConflictProductTemp->sczVersion, wzCfgVersion, 0);
        ExitOnFailure(hr, "Failed to copy product version");

        hr = StrAllocString(&pConflictProductTemp->sczPublicKey, wzCfgPublicKey, 0);
        ExitOnFailure(hr, "Failed to copy product public key");

        hr = DisplayNameEnumerate(pcdbRemote->pcdbLocal, pcdbRemote->pcdbLocal->dwAppID, &pConflictProductTemp->rgDisplayNames, &pConflictProductTemp->cDisplayNames);
        ExitOnFailure(hr, "Failed to enumerate display names for product conflict array");

        ++(*pcConflictProducts);
        hr = MemEnsureArraySize(reinterpret_cast<void **>(prgConflictProducts), *pcConflictProducts, sizeof(CONFLICT_PRODUCT), 0);
        ExitOnFailure(hr, "Failed to grow product conflict list array");

        (*prgConflictProducts)[*pcConflictProducts - 1] = *pConflictProductTemp;
        ReleaseNullMem(pConflictProductTemp);
    }

    hr = DictCreateStringList(&shDictProductsSeen, 0, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary");

    hr = LegacySyncInitializeSession(TRUE, TRUE, &syncSession);
    ExitOnFailure(hr, "Failed to initialize legacy sync session");

    hr = UtilSyncAllProductsHelp(pcdbRemote, &syncSession, pcdbRemote->pcdbLocal, shDictProductsSeen, prgConflictProducts, pcConflictProducts);
    ExitOnFailure(hr, "Failed to synchronize values in product list found in remote database");

    hr = UtilSyncAllProductsHelp(pcdbRemote->pcdbLocal, &syncSession, pcdbRemote, shDictProductsSeen, prgConflictProducts, pcConflictProducts);
    ExitOnFailure(hr, "Failed to synchronize values in product list found in local database");

LExit:
    LegacySyncUninitializeSession(pcdbRemote->pcdbLocal, &syncSession);
    ReleaseDict(shDictProductsSeen);

    return hr;
}

int UtilCompareSystemTimes(
    __in const SYSTEMTIME *pst1,
    __in const SYSTEMTIME *pst2
    )
{
    if (pst1->wYear > pst2->wYear)
    {
        return 1;
    }
    else if (pst1->wYear < pst2->wYear)
    {
        return -1;
    }
    else if (pst1->wMonth > pst2->wMonth)
    {
        return 1;
    }
    else if (pst1->wMonth < pst2->wMonth)
    {
        return -1;
    }
    else if (pst1->wDay > pst2->wDay)
    {
        return 1;
    }
    else if (pst1->wDay < pst2->wDay)
    {
        return -1;
    }
    else if (pst1->wHour > pst2->wHour)
    {
        return 1;
    }
    else if (pst1->wHour < pst2->wHour)
    {
        return -1;
    }
    else if (pst1->wMinute > pst2->wMinute)
    {
        return 1;
    }
    else if (pst1->wMinute < pst2->wMinute)
    {
        return -1;
    }
    else if (pst1->wSecond > pst2->wSecond)
    {
        return 1;
    }
    else if (pst1->wSecond < pst2->wSecond)
    {
        return -1;
    }
    else
    {
        return 0;
    }
}

HRESULT UtilSubtractSystemTimes(
    __in const SYSTEMTIME *pst1,
    __in const SYSTEMTIME *pst2,
    __out LONGLONG *pSeconds
    )
{
    HRESULT hr = S_OK;
    FILETIME ft1 = { };
    FILETIME ft2 = { };
    ULARGE_INTEGER uli1 = { };
    ULARGE_INTEGER uli2 = { };

    if (pst1->wYear > 0 && !::SystemTimeToFileTime(pst1, &ft1))
    {
        ExitOnLastError(hr, "Failed to convert system time 1 to file time");
    }
    if (pst2->wYear > 0 && !::SystemTimeToFileTime(pst2, &ft2))
    {
        ExitOnLastError(hr, "Failed to convert system time 2 to file time");
    }

    uli1.LowPart = ft1.dwLowDateTime;
    uli1.HighPart = ft1.dwHighDateTime;
    uli2.LowPart = ft2.dwLowDateTime;
    uli2.HighPart = ft2.dwHighDateTime;

    uli1.QuadPart -= uli2.QuadPart;

    // FILETIME is in 100-nanosecond intervals, and there are 10 Million of those per second
    *pSeconds = uli1.QuadPart / 10000000;

LExit:
    return hr;
}

HRESULT UtilAddToSystemTime(
    __in DWORD dwSeconds,
    __inout SYSTEMTIME *pst
    )
{
    HRESULT hr = S_OK;
    FILETIME ft = { };

    if (!::SystemTimeToFileTime(pst, &ft))
    {
        ExitWithLastError(hr, "Failed to convert system time to file time");
    }

    DWORD64 ul;
    C_ASSERT(sizeof(ul) == sizeof(ft));
    memcpy(&ul, &ft, sizeof(ul));
    ul += dwSeconds * 10000000;
    memcpy(&ft, &ul, sizeof(ft));

    if (!FileTimeToSystemTime(&ft, pst))
    {
        ExitWithLastError(hr, "Failed to convert file time to system time");
    }

LExit:
    return hr;
}

// Static functions
static HRESULT IsProductInDictAndAddToDict(
    __in STRINGDICT_HANDLE shDictProductsSeen,
    __in LPCWSTR wzName,
    __in LPCWSTR wzVersion,
    __in LPCWSTR wzPublicKey,
    __out BOOL *pfResult
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczProductKey = NULL;

    hr = StrAllocFormatted(&sczProductKey, L"%ls%ls%ls", wzName, wzVersion, wzPublicKey);
    ExitOnFailure(hr, "Failed to allocate product key");

    hr = DictKeyExists(shDictProductsSeen, sczProductKey);
    if (E_NOTFOUND == hr)
    {
        hr = DictAddKey(shDictProductsSeen, sczProductKey);
        ExitOnFailure(hr, "Failed to add product key to dict: %ls", sczProductKey);

        *pfResult = FALSE;

        ExitFunction1(hr = S_OK);
    }
    else
    {
        ExitOnFailure(hr, "Failed to check if key exists in dict: %ls", sczProductKey);
    }

    *pfResult = TRUE;

LExit:
    ReleaseStr(sczProductKey);

    return hr;
}

HRESULT UtilExpandLegacyPath(
    __in LPCWSTR wzInput,
    __in LEGACY_DETECTION *pDetect,
    __deref_out LPWSTR *psczOutput
    )
{
    HRESULT hr = S_OK;
    int nInputLen = lstrlenW(wzInput);
    LPCWSTR wzFindResult = NULL;
    LPWSTR sczExpandedPath = NULL;
    LPWSTR sczTemp = NULL;

    for (DWORD i = 0; i < _countof(LEGACY_DIRECTORIES); ++i)
    {
        wzFindResult = wcsstr(wzInput, LEGACY_DIRECTORIES[i].wzInput);
        
        // Only if it's found right at the beginning
        if (wzFindResult == wzInput)
        {
            hr = PathGetKnownFolder(LEGACY_DIRECTORIES[i].nFolder, &sczExpandedPath);
            ExitOnFailure(hr, "Failed to get known folder with ID: %d", LEGACY_DIRECTORIES[i].nFolder);

            if (NULL != LEGACY_DIRECTORIES[i].wzAppend)
            {
                hr = PathConcat(sczExpandedPath, LEGACY_DIRECTORIES[i].wzAppend, &sczTemp);
                ExitOnFailure(hr, "Failed to combine paths while expanding legacy directory paths");

                ReleaseStr(sczExpandedPath);
                sczExpandedPath = sczTemp;
                sczTemp = NULL;
            }

            if (nInputLen > lstrlenW(LEGACY_DIRECTORIES[i].wzInput))
            {
                hr = PathConcat(sczExpandedPath, wzInput + lstrlenW(LEGACY_DIRECTORIES[i].wzInput), psczOutput);
                ExitOnFailure(hr, "Failed to concat trailing path from XML while expanding legacy path");
            }
            else
            {
                ReleaseStr(*psczOutput);
                *psczOutput = sczExpandedPath;
                sczExpandedPath = NULL;
            }

            ExitFunction1(hr = S_OK);
        }
    }

    // If we failed to find it in the standard directory list, only then do we check custom directories detected
    // by the product
    hr = DetectExpandDirectoryPath(wzInput, pDetect, psczOutput);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to expand directory path from detection results: %ls", wzInput);

LExit:
    ReleaseStr(sczExpandedPath);
    ReleaseStr(sczTemp);

    return hr;
}

HRESULT UtilTestWriteAccess(
    __in HANDLE hToken,
    __in_z LPCWSTR wzPath
    )
{
    const DWORD ACCESS_READ  = 1;
    const DWORD ACCESS_WRITE = 2;

    HRESULT hr = S_OK;
    LPWSTR sczTempFilePath = NULL;
    LPWSTR sczDir = NULL;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    SECURITY_DESCRIPTOR *pSecurityDescriptor = NULL;
    BOOL fResult = FALSE;
    BOOL fAccessStatus = FALSE;
    DWORD cbPrivilegeSetLength = sizeof(PRIVILEGE_SET);
    DWORD dwGrantedAccess = 0;
    GENERIC_MAPPING genericMapping = { };
    PRIVILEGE_SET privilegeSet = { };

    genericMapping.GenericRead    = ACCESS_READ;
    genericMapping.GenericWrite   = ACCESS_WRITE;
    genericMapping.GenericExecute = 0;
    genericMapping.GenericAll     = ACCESS_READ | ACCESS_WRITE;

    hr = PathGetDirectory(wzPath, &sczDir);
    ExitOnFailure(hr, "Failed to get directory portion of path: %ls", wzPath);

    if (!FileExistsEx(wzPath, NULL) && !DirExists(wzPath, NULL))
    {
        ExitFunction1(hr = E_PATHNOTFOUND);
    }

    hr = AclGetSecurityDescriptor(sczDir, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION | OWNER_SECURITY_INFORMATION | GROUP_SECURITY_INFORMATION, &pSecurityDescriptor);
    if (E_PATHNOTFOUND == hr)
    {
        ExitFunction();
    }
    else if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = E_PATHNOTFOUND);
    }
    ExitOnFailure(hr, "Failed to get security descriptor for directory: %ls", sczDir);

    fResult = ::AccessCheck(pSecurityDescriptor, hToken, ACCESS_WRITE, &genericMapping, &privilegeSet, &cbPrivilegeSetLength, &dwGrantedAccess, &fAccessStatus);
    if (!fResult)
    {
        ExitWithLastError(hr, "Failed to check for access to directory: %ls", wzPath);
    }

    if (fAccessStatus)
    {
        ExitFunction1(hr = S_OK);
    }
    else
    {
        ExitFunction1(hr = E_ACCESSDENIED);
    }

LExit:
    if (NULL != pSecurityDescriptor)
    {
        AclFreeSecurityDescriptor(pSecurityDescriptor);
    }
    ReleaseFile(hFile);
    ReleaseStr(sczTempFilePath);
    ReleaseStr(sczDir);

    return hr;
}

HRESULT UtilConvertToVirtualStorePath(
    __in_z LPCWSTR wzOriginalPath,
    __out LPWSTR *psczOutput
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczTempFilePath = NULL;
    LPWSTR sczLocalAppData = NULL;
    LPWSTR sczVirtualStore = NULL;
    LPWSTR sczFullPathToVirtual = NULL;

    hr = PathGetKnownFolder(CSIDL_LOCAL_APPDATA, &sczLocalAppData);
    ExitOnFailure(hr, "Failed to get known folder with ID: %d", CSIDL_LOCAL_APPDATA);

    hr = PathConcat(sczLocalAppData, L"VirtualStore\\", &sczVirtualStore);
    ExitOnFailure(hr, "Failed to append VirtualStore to localappdata path");

    hr = PathConcat(sczVirtualStore, wzOriginalPath + 3, psczOutput);
    ExitOnFailure(hr, "Failed to append '%ls' to virtualstore directory", wzOriginalPath + 2);

LExit:
    ReleaseStr(sczTempFilePath);
    ReleaseStr(sczLocalAppData);
    ReleaseStr(sczVirtualStore);
    ReleaseStr(sczFullPathToVirtual);

    return hr;
}

static HRESULT SyncSingleProduct(
    __in CFGDB_STRUCT *pcdb1,
    __in CFGDB_STRUCT *pcdb2,
    __in BOOL fRegistered,
    __inout CONFLICT_PRODUCT **ppcpProductTemp
    )
{
    HRESULT hr = S_OK;
    STRINGDICT_HANDLE shDictItemsSeen = NULL;

    hr = DictCreateStringList(&shDictItemsSeen, 0, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary of values seen");

    hr = ProductSyncValues(pcdb1, pcdb2, fRegistered, shDictItemsSeen, ppcpProductTemp);
    ExitOnFailure(hr, "Failed to sync product values for application (1)");

    hr = ProductSyncValues(pcdb2, pcdb1, fRegistered, shDictItemsSeen, ppcpProductTemp);
    ExitOnFailure(hr, "Failed to sync product values for application (2)");

LExit:
    ReleaseDict(shDictItemsSeen);

    return hr;
}

static HRESULT UtilSyncAllProductsHelp(
    __in CFGDB_STRUCT *pcdb1,
    __in LEGACY_SYNC_SESSION *pSyncSession,
    __in CFGDB_STRUCT *pcdb2,
    __in_opt STRINGDICT_HANDLE shDictProductsSeen,
    __out CONFLICT_PRODUCT **prgConflictProducts,
    __out DWORD *pcConflictProducts
    )
{
    HRESULT hr = S_OK;
    DISPLAY_NAME *rgDisplayNames = NULL;
    DWORD cDisplayNames = 0;
    CONFLICT_PRODUCT *pConflictProductTemp = NULL;
    LPWSTR sczName = NULL;
    LPWSTR sczVersion = NULL;
    LPWSTR sczPublicKey = NULL;
    DWORD dwAppID = 0;
    BOOL fResult = FALSE;
    BOOL fRegistered = FALSE;
    BOOL fDontCreateFlag = FALSE;
    BOOL fLegacyProduct = FALSE;
    SCE_ROW_HANDLE sceRow = NULL;
    BOOL fFirstIsLocal = (NULL == pcdb1->pcdbLocal);
    BOOL fLocalInSceTransaction = FALSE;
    BOOL fRemoteInSceTransaction = FALSE;

    hr = SceGetFirstRow(pcdb1->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get row in PRODUCT_INDEX_TABLE table");

        hr = SceGetColumnDword(sceRow, PRODUCT_ID, &dwAppID);
        ExitOnFailure(hr, "Failed to get app ID");

        if (dwAppID == pcdb1->dwCfgAppID)
        {
            goto Skip;
        }

        hr = SceGetColumnString(sceRow, PRODUCT_NAME, &sczName);
        ExitOnFailure(hr, "Failed to get app name");

        hr = SceGetColumnString(sceRow, PRODUCT_VERSION, &sczVersion);
        ExitOnFailure(hr, "Failed to get app version");

        hr = SceGetColumnString(sceRow, PRODUCT_PUBLICKEY, &sczPublicKey);
        ExitOnFailure(hr, "Failed to get app public key");

        hr = IsProductInDictAndAddToDict(shDictProductsSeen, sczName, sczVersion, sczPublicKey, &fResult);
        ExitOnFailure(hr, "Failed to check if product has already been synced");

        // If product is already in the dict, skip it - so we don't try to sync the same product both ways
        if (fResult)
        {
            goto Skip;
        }

        hr = SceGetColumnBool(sceRow, PRODUCT_IS_LEGACY, &fLegacyProduct);
        ExitOnFailure(hr, "Failed to get IsLegacy flag");

        hr = ProductIsRegistered(fFirstIsLocal ? pcdb1 : pcdb2, sczName, sczVersion, sczPublicKey, &fRegistered);
        ExitOnFailure(hr, "Failed to check if product is registered");

        fDontCreateFlag = (fFirstIsLocal && !fRegistered) || (fLegacyProduct && fFirstIsLocal);
        hr = ProductSet(pcdb1, sczName, sczVersion, sczPublicKey, fDontCreateFlag, NULL);
        if (E_NOTFOUND == hr && fDontCreateFlag)
        {
            hr = S_OK;
            goto Skip;
        }
        ExitOnFailure(hr, "Failed to set product in database");

        fDontCreateFlag = (!fFirstIsLocal && !fRegistered) || (fLegacyProduct && !fFirstIsLocal);
        hr = ProductSet(pcdb2, sczName, sczVersion, sczPublicKey, fDontCreateFlag, NULL);
        if (E_NOTFOUND == hr && fDontCreateFlag)
        {
            hr = S_OK;
            goto Skip;
        }
        ExitOnFailure(hr, "Failed to set product in other database");

        if (fLegacyProduct)
        {
            hr = LegacySyncSetProduct(fFirstIsLocal ? pcdb1 : pcdb2, pSyncSession, sczName);
            ExitOnFailure(hr, "Failed to set product in legacy sync session");

            hr = LegacyProductMachineToDb(fFirstIsLocal ? pcdb1 : pcdb2, &pSyncSession->syncProductSession);
            ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");
        }
        else
        {
            hr = SceBeginTransaction(fFirstIsLocal ? pcdb1->psceDb : pcdb2->psceDb);
            ExitOnFailure(hr, "Failed to begin local transaction");
            fLocalInSceTransaction = TRUE;
        }

        // Put all remote db operations in a single transaction
        hr = SceBeginTransaction(fFirstIsLocal ? pcdb2->psceDb : pcdb1->psceDb);
        ExitOnFailure(hr, "Failed to begin remote transaction");
        fRemoteInSceTransaction = TRUE;

        hr = SyncSingleProduct(pcdb1, pcdb2, fRegistered, &pConflictProductTemp);
        ExitOnFailure(hr, "Failed to sync single product");

        if (fLegacyProduct)
        {
            hr = LegacySyncFinalizeProduct(fFirstIsLocal ? pcdb1 : pcdb2, pSyncSession);
            ExitOnFailure(hr, "Failed to finalize product %u in legacy sync session", fFirstIsLocal ? pcdb1->dwAppID : pcdb2->dwAppID);
        }
        else
        {
            // In legacy case, LegacySyncFinalizeProduct will handle committing for us (or in case of failure, rollback will occur in LegacySyncUninitializeSession)
            hr = SceCommitTransaction(fFirstIsLocal ? pcdb1->psceDb : pcdb2->psceDb);
            ExitOnFailure(hr, "Failed to commit local transaction");
            fLocalInSceTransaction = FALSE;
        }

        // Commit remote db changes AFTER local has been commited
        // This is important or we can create conflicts by putting changes in remote db that came from the local db, but are no longer in local db due to rollback
        hr = SceCommitTransaction(fFirstIsLocal ? pcdb2->psceDb : pcdb1->psceDb);
        ExitOnFailure(hr, "Failed to commit remote transaction");
        fRemoteInSceTransaction = FALSE;

        if (NULL != pConflictProductTemp)
        {
            hr = DisplayNameEnumerate(fFirstIsLocal ? pcdb1 : pcdb2, fFirstIsLocal ? pcdb1->dwAppID : pcdb2->dwAppID, &rgDisplayNames, &cDisplayNames);
            ExitOnFailure(hr, "Failed to enumerate display names for product conflict array");

            pConflictProductTemp->sczProductName = sczName;
            sczName = NULL;
            pConflictProductTemp->sczVersion = sczVersion;
            sczVersion = NULL;
            pConflictProductTemp->sczPublicKey = sczPublicKey;
            sczPublicKey = NULL;
            pConflictProductTemp->rgDisplayNames = rgDisplayNames;
            rgDisplayNames = NULL;
            pConflictProductTemp->cDisplayNames = cDisplayNames;
            cDisplayNames = 0;

            hr = MemEnsureArraySize(reinterpret_cast<void **>(prgConflictProducts), *pcConflictProducts + 1, sizeof(CONFLICT_PRODUCT), 0);
            ExitOnFailure(hr, "Failed to grow product conflict list array");
            ++(*pcConflictProducts);

            (*prgConflictProducts)[*pcConflictProducts - 1] = *pConflictProductTemp;
            ReleaseNullMem(pConflictProductTemp);
        }

    Skip:
        ReleaseNullSceRow(sceRow);
        hr = SceGetNextRow(pcdb1->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
    }

    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }

LExit:
    if (fRemoteInSceTransaction)
    {
        SceRollbackTransaction(fFirstIsLocal ? pcdb2->psceDb : pcdb1->psceDb);
    }

    if (fLocalInSceTransaction)
    {
        SceRollbackTransaction(fFirstIsLocal ? pcdb1->psceDb : pcdb2->psceDb);
    }

    ReleaseSceRow(sceRow);
    ReleaseStr(sczName);
    ReleaseStr(sczVersion);
    ReleaseStr(sczPublicKey);
    ReleaseMem(pConflictProductTemp);
    ReleaseDisplayNameArray(rgDisplayNames, cDisplayNames);

    return hr;
}

void UtilGetSystemTime(
    __inout SYSTEMTIME *pst
    )
{
    SystemTimeGetter(pst);
}

BOOL UtilIs64BitSystem()
{
    HRESULT hr = S_OK;

    static BOOL s_fCheckRan = FALSE;
    static BOOL s_f64BitSystem = FALSE;
    static BOOL (*s_pfnIsWow64Process) (HANDLE, PBOOL) = NULL;

    if (!s_fCheckRan)
    {
        s_fCheckRan = TRUE;

        HMODULE hKernel32 = ::GetModuleHandleW(L"kernel32.dll");
        if (!hKernel32)
        {
            hr = HRESULT_FROM_WIN32(ERROR_MOD_NOT_FOUND);
            TraceError(hr, "Failed to get module handle for kernel32.dll - assuming this is not a 64-bit system");
        }
        else
        {
            s_pfnIsWow64Process = (BOOL (*)(HANDLE, PBOOL))::GetProcAddress(hKernel32, "IsWow64Process");
            if (NULL != s_pfnIsWow64Process)
            {
                if (!s_pfnIsWow64Process(::GetCurrentProcess(), &s_f64BitSystem))
                {
                    hr = HRESULT_FROM_WIN32(::GetLastError());
                    TraceError(hr, "Failed to check if Wow64 process - assuming this is not a 64-bit system");
                }
            }
        }
    }

    return s_f64BitSystem;
}
