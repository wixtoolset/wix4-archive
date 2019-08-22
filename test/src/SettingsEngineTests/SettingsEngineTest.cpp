// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace Xunit;

static SYSTEMTIME stCurrent = { };

const WCHAR ARP_REG_KEY[] = L"Software\\CfgTest\\Arp";
const WCHAR APPLICATIONS_REG_KEY[] = L"Software\\CfgTest\\Applications";

namespace CfgTests
{
    void SystemTimeGetter(SYSTEMTIME *pst)
    {
        *pst = stCurrent;
    }

    void BackgroundStatusCallback(HRESULT hr, BACKGROUND_STATUS_TYPE type, LPCWSTR /*wzString1*/, LPCWSTR /*wzString2*/, LPCWSTR /*wzString3*/, LPVOID pvContext)
    {
        TestContext *pContext = (TestContext *)pvContext;
        ExitOnFailure(hr, "Failure message from background thread");

        switch (type)
        {
        case BACKGROUND_STATUS_SYNCING_PRODUCT:
            ++pContext->m_cSyncingProduct;
            break;
        case BACKGROUND_STATUS_SYNC_PRODUCT_FINISHED:
            ++pContext->m_cSyncProductFinished;
            break;
        case BACKGROUND_STATUS_REDETECTING_PRODUCTS:
            ++pContext->m_cRedetectingProducts;
            break;
        case BACKGROUND_STATUS_REDETECT_PRODUCTS_FINISHED:
            ++pContext->m_cRedetectProductsFinished;
            break;
        case BACKGROUND_STATUS_SYNCING_REMOTE:
            ++pContext->m_cSyncingRemote;
            break;
        case BACKGROUND_STATUS_SYNC_REMOTE_FINISHED:
            ++pContext->m_cSyncRemoteFinished;
            break;
        case BACKGROUND_STATUS_AUTOSYNC_RUNNING:
            ++pContext->m_cAutoSyncRunning;
            break;
        case BACKGROUND_STATUS_REMOTE_GOOD:
            ++pContext->m_cRemoteGood;
            break;
        case BACKGROUND_STATUS_GENERAL_ERROR:
        case BACKGROUND_STATUS_PRODUCT_ERROR:
        default:
            hr = E_FAIL;
            ExitOnFailure(hr, "Unhappy message type from background thread");
        }

    LExit:
        return;
    }

    void BackgroundConflictsFoundCallback(CFGDB_HANDLE cdHandle, CONFLICT_PRODUCT *rgcpProduct, DWORD cProduct, LPVOID pvContext)
    {
        TestContext *pContext = (TestContext *)pvContext;
        BackgroundConflicts conflicts;
        conflicts.cdHandle = cdHandle;
        conflicts.rgcpProduct = rgcpProduct;
        conflicts.cProduct = cProduct;
        for (DWORD i = 0; i < pContext->m_backgroundConflicts.size(); ++i)
        {
            if (pContext->m_backgroundConflicts[i].cdHandle == cdHandle)
            {
                // We're getting new conflicts for the same DB, so release the old results and overwrite our array
                CfgReleaseConflictProductArray(pContext->m_backgroundConflicts[i].rgcpProduct, pContext->m_backgroundConflicts[i].cProduct);
                pContext->m_backgroundConflicts[i] = conflicts;
                return;
            }
        }

        pContext->m_backgroundConflicts.push_back(conflicts);
    }

    void CfgTest::TestInitialize()
    {
        HRESULT hr = S_OK;
        HKEY hk = NULL;

        ::GetSystemTime(&stCurrent);

        TestHookOverrideGetSystemTime(SystemTimeGetter);

        hr = RegInitialize();
        ExitOnFailure(hr, "Failed to initialize regutil");

        // Override Arp regkey path
        hr = RegDelete(HKEY_CURRENT_USER, ARP_REG_KEY, REG_KEY_32BIT, TRUE);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to delete fake ARP regkey: %ls", ARP_REG_KEY);

        hr = RegCreate(HKEY_CURRENT_USER, ARP_REG_KEY, REG_KEY_32BIT, &hk);
        ExitOnFailure(hr, "Failed to create fake ARP regkey: %ls", ARP_REG_KEY);

        hr = TestHookOverrideArpPath(ARP_REG_KEY);
        ExitOnFailure(hr, "Failed to override ARP path for test");

        // Override Applications regkey path
        hr = RegDelete(HKEY_CURRENT_USER, APPLICATIONS_REG_KEY, REG_KEY_32BIT, TRUE);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to delete fake Applications regkey: %ls", APPLICATIONS_REG_KEY);

        hr = RegCreate(HKEY_CURRENT_USER, APPLICATIONS_REG_KEY, REG_KEY_32BIT, &hk);
        ExitOnFailure(hr, "Failed to create fake Applications regkey: %ls", APPLICATIONS_REG_KEY);

        hr = TestHookOverrideApplicationsPath(APPLICATIONS_REG_KEY);
        ExitOnFailure(hr, "Failed to override Applications path for test");

        RedirectDatabases();

    LExit:
        ReleaseRegKey(hk);
    }

    void CfgTest::TestUninitialize()
    {
        HRESULT hr = S_OK;

        hr = RegDelete(HKEY_CURRENT_USER, ARP_REG_KEY, REG_KEY_32BIT, TRUE);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to delete fake ARP regkey: %ls", ARP_REG_KEY);

        hr = RegDelete(HKEY_CURRENT_USER, APPLICATIONS_REG_KEY, REG_KEY_32BIT, TRUE);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to delete fake Applications regkey: %ls", APPLICATIONS_REG_KEY);

    LExit:
        return;
    }

    void CfgTest::RedirectDatabases()
    {
        HRESULT hr = S_OK;
        LPWSTR sczPathUser = NULL;
        LPWSTR sczPathLegacy = NULL;
        LPWSTR sczPathAdmin = NULL;

        // Override user DB directory
        hr = PathExpand(&sczPathUser, L"%TEMP%\\TestUserDb\\", PATH_EXPAND_ENVIRONMENT);
        ExitOnFailure(hr, "Failed to expand path");

        hr = DirEnsureDelete(sczPathUser, TRUE, TRUE);
        if (E_PATHNOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to ensure directory %ls is deleted", sczPathUser);

        TestHookOverrideUserDatabasePath(L"%TEMP%\\TestUserDb\\");

        // Override legacy DB directory
        hr = PathExpand(&sczPathAdmin, L"%TEMP%\\TestAdminDb\\", PATH_EXPAND_ENVIRONMENT);
        ExitOnFailure(hr, "Failed to expand path");

        hr = DirEnsureDelete(sczPathAdmin, TRUE, TRUE);
        if (E_PATHNOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to ensure directory %ls is deleted", sczPathAdmin);

        TestHookOverrideAdminDatabasePath(L"%TEMP%\\TestAdminDb\\");

    LExit:
        ReleaseStr(sczPathUser);
        ReleaseStr(sczPathLegacy);
        ReleaseStr(sczPathAdmin);
    }
    
    void CfgTest::AddToSystemTime(DWORD dwSeconds)
    {
        HRESULT hr = S_OK;
        FILETIME ft = { };

        if (!::SystemTimeToFileTime(&stCurrent, &ft))
        {
            ExitWithLastError(hr, "Failed to convert system time to file time");
        }

        DWORD64 ul;
        C_ASSERT(sizeof(ul) == sizeof(ft));
        memcpy(&ul, &ft, sizeof(ul));
        ul += (DWORD64)(dwSeconds) * 10000000;
        memcpy(&ft, &ul, sizeof(ft));

        if (!FileTimeToSystemTime(&ft, &stCurrent))
        {
            ExitWithLastError(hr, "Failed to convert file time to system time");
        }
    LExit:
        return;
    }

    void CfgTest::SetARP(LPCWSTR wzKeyName, LPCWSTR wzDisplayName, LPCWSTR wzInstallLocation, LPCWSTR wzUninstallString)
    {
        HRESULT hr = S_OK;
        HKEY hkArp = NULL;
        HKEY hkNew = NULL;

        hr = RegOpen(HKEY_CURRENT_USER, ARP_REG_KEY, KEY_WOW64_32KEY | KEY_ALL_ACCESS, &hkArp);
        ExitOnFailure(hr, "Failed to open fake ARP regkey: %ls", ARP_REG_KEY);

        hr = RegDelete(hkArp, wzKeyName, REG_KEY_32BIT, TRUE);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to delete subkey: %ls", wzKeyName);

        if (NULL != wzDisplayName)
        {
            hr = RegCreate(hkArp, wzKeyName, KEY_WOW64_32KEY | KEY_ALL_ACCESS, &hkNew);
            ExitOnFailure(hr, "Failed to create subkey: %ls", wzKeyName);

            hr = RegWriteString(hkNew, L"DisplayName", wzDisplayName);
            ExitOnFailure(hr, "Failed to write DisplayName to registry");

            hr = RegWriteString(hkNew, L"UninstallString", wzUninstallString);
            ExitOnFailure(hr, "Failed to write UninstallString to registry");

            hr = RegWriteString(hkNew, L"InstallLocation", wzInstallLocation);
            ExitOnFailure(hr, "Failed to write InstallLocation to registry");
        }

    LExit:
        ReleaseRegKey(hkArp);
        ReleaseRegKey(hkNew);
    }

    void CfgTest::SetApplication(LPCWSTR wzFileName, LPCWSTR wzFilePath)
    {
        HRESULT hr = S_OK;
        HKEY hk = NULL;
        LPWSTR sczFullPath = NULL;
        LPWSTR sczQuotedCommand = NULL;

        hr = StrAllocFormatted(&sczFullPath, L"%ls\\%ls\\shell\\open\\command", APPLICATIONS_REG_KEY, wzFileName);
        ExitOnFailure(hr, "Failed to format string to full shell\\open\\command path");

        hr = RegCreate(HKEY_CURRENT_USER, sczFullPath, KEY_WOW64_32KEY | KEY_ALL_ACCESS, &hk);
        ExitOnFailure(hr, "Failed to create key: %ls", sczFullPath);

        hr = StrAllocFormatted(&sczQuotedCommand, L"\"%ls\" \"%%1\"", wzFilePath);
        ExitOnFailure(hr, "Failed to format quoted command string");

        hr = RegWriteString(hk, NULL, sczQuotedCommand);
        ExitOnFailure(hr, "Failed to write quoted command to registry");

    LExit:
        ReleaseRegKey(hk);
        ReleaseStr(sczFullPath);
        ReleaseStr(sczQuotedCommand);
    }

    void CfgTest::ResetApplications()
    {
        HRESULT hr = S_OK;

        hr = RegDelete(HKEY_CURRENT_USER, APPLICATIONS_REG_KEY, REG_KEY_32BIT, TRUE);
        if (E_FILENOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to delete fake Applications regkey: %ls", APPLICATIONS_REG_KEY);

    LExit:
        return;
    }

    void CfgTest::WaitForSqlCeTimestampChange()
    {
        // Increment timestamp so that SQL CE will get different timestamps
        AddToSystemTime(1);
    }

    void CfgTest::WaitForAutoSync(CFGDB_HANDLE cdhDb)
    {
        // TODO: eliminate sleep and create a more stable form of waiting for all monutil requests to go from pending to fired
        // This will allow tests to run much faster
        AddToSystemTime(1);
        ::Sleep(2000);
        AddToSystemTime(1);
        WaitForDbToBeIdle(cdhDb);
    }

    void CfgTest::WaitForSyncNoResolve(CFGDB_HANDLE cdhDb)
    {
        HRESULT hr = S_OK;

        WaitForAutoSync(cdhDb);
        if (m_pContext->m_backgroundConflicts.size() > 0)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Unexpected conflicts found");
        }
    LExit:
        return;
    }

    void CfgTest::WaitForSyncResolveAll(CFGDB_HANDLE cdhDb, RESOLUTION_CHOICE rcChoice)
    {
        HRESULT hr = S_OK;

        WaitForAutoSync(cdhDb);
        Assert::Equal<int>(1, m_pContext->m_backgroundConflicts.size());
        if (cdhDb != m_pContext->m_backgroundConflicts.front().cdHandle)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Conflicts found in different database than expected");
        }

        CONFLICT_PRODUCT *pcplProductConflictList = m_pContext->m_backgroundConflicts.front().rgcpProduct;
        DWORD dwProductConflictCount = m_pContext->m_backgroundConflicts.front().cProduct;

        if (0 == dwProductConflictCount)
        {
            if (NULL != pcplProductConflictList)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "There should have been conflicts after syncing, but none were found, and in addition the list wasn't NULL!");
            }

            hr = E_FAIL;
            ExitOnFailure(hr, "There should have been conflicts after syncing, but none were found!");
        }

        for (DWORD dwProductIndex = 0; dwProductIndex < dwProductConflictCount; ++dwProductIndex)
        {
            for (DWORD dwValueIndex = 0; dwValueIndex < pcplProductConflictList[dwProductIndex].cValues; ++dwValueIndex)
            {
                pcplProductConflictList[dwProductIndex].rgrcValueChoices[dwValueIndex] = rcChoice;
            }
        }

        hr = CfgResolve(cdhDb, pcplProductConflictList, dwProductConflictCount);
        ExitOnFailure(hr, "Failed to resolve values");

    LExit:
        if (NULL != pcplProductConflictList)
        {
            CfgReleaseConflictProductArray(pcplProductConflictList, dwProductConflictCount);
            dwProductConflictCount = 0;
            m_pContext->m_backgroundConflicts.clear();
        }
    }

    void CfgTest::WaitForSyncLatestLegacy(CFGDB_HANDLE cdhDb)
    {
        WaitForAutoSync(cdhDb);
    }

    void CfgTest::ExpectProductRegistered(CFGDB_HANDLE cdhUserDb, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey)
    {
        HRESULT hr = S_OK;
        BOOL fRegistered = FALSE;

        hr = CfgIsProductRegistered(cdhUserDb, wzProductName, wzVersion, wzPublicKey, &fRegistered);
        ExitOnFailure(hr, "Failed to check if product is registered (per-user): %ls, %ls, %ls", wzProductName, wzVersion, wzPublicKey);

        if (!fRegistered)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Product should be registered (per-user), but it isn't!: %ls, %ls, %ls", wzProductName, wzVersion, wzPublicKey);
        }

    LExit:
        return;
    }

    void CfgTest::ExpectProductUnregistered(CFGDB_HANDLE cdhUserDb, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey)
    {
        HRESULT hr = S_OK;
        BOOL fRegistered = FALSE;

        hr = CfgIsProductRegistered(cdhUserDb, wzProductName, wzVersion, wzPublicKey, &fRegistered);
        ExitOnFailure(hr, "Failed to check if product is registered (per-user): %ls, %ls, %ls", wzProductName, wzVersion, wzPublicKey);

        if (fRegistered)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Product shouldn't be registered (per-user), but it is!: %ls, %ls, %ls", wzProductName, wzVersion, wzPublicKey);
        }

    LExit:
        return;
    }

    void CfgTest::ExpectAdminProductRegistered(CFGDB_HANDLE cdhAdminDb, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey)
    {
        HRESULT hr = S_OK;
        BOOL fRegistered = FALSE;

        hr = CfgAdminIsProductRegistered(cdhAdminDb, wzProductName, wzVersion, wzPublicKey, &fRegistered);
        ExitOnFailure(hr, "Failed to check if product is registered (per-machine): %ls, %ls, %ls", wzProductName, wzVersion, wzPublicKey);

        if (!fRegistered)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Product should be registered (per-machine), but it isn't!: %ls, %ls, %ls", wzProductName, wzVersion, wzPublicKey);
        }

    LExit:
        return;
    }

    void CfgTest::ExpectAdminProductUnregistered(CFGDB_HANDLE cdhAdminDb, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey)
    {
        HRESULT hr = S_OK;
        BOOL fRegistered = FALSE;

        hr = CfgAdminIsProductRegistered(cdhAdminDb, wzProductName, wzVersion, wzPublicKey, &fRegistered);
        ExitOnFailure(hr, "Failed to check if product is registered (per-machine): %ls, %ls, %ls", wzProductName, wzVersion, wzPublicKey);

        if (fRegistered)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Product shouldn't be registered (per-machine), but it is!: %ls, %ls, %ls", wzProductName, wzVersion, wzPublicKey);
        }

    LExit:
        return;
    }

    void CfgTest::CheckCfgAndRegValueFlag(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName, BOOL fExpectedValue, DWORD dwOffset)
    {
        HRESULT hr = S_OK;
        BYTE *pbBytes;
        DWORD cbBytes;
        BOOL fRegFoundValue = FALSE;
        BOOL fCfgFoundValue = FALSE;

        hr = RegReadBinary(hk, wzName, &pbBytes, &cbBytes);
        ExitOnFailure(hr, "Failed to read binary value from registry named %ls", wzName);

        if (dwOffset >= cbBytes * 8)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Not enough bytes found in registry value %ls to check offset %u", wzName, dwOffset);
        }

        fRegFoundValue = (pbBytes[dwOffset / 8] & (0x1 << (dwOffset % 8))) == 0 ? FALSE : TRUE;

        if (fRegFoundValue != fExpectedValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Boolean flag value in registry didn't match expected! RegName: %ls, Offset: %ls, Expected: %ls", wzName, dwOffset, fExpectedValue ? L"TRUE" : L"FALSE");
        }

        hr = CfgGetBool(cdhDb, wzCfgName, &fCfgFoundValue);
        ExitOnFailure(hr, "Failed to read boolean from cfg db named %ls", wzCfgName);

        if (fCfgFoundValue != fExpectedValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Boolean flag value in cfg db didn't match expected! CfgName: %ls, Expected: %ls", wzCfgName, fExpectedValue ? L"TRUE" : L"FALSE");
        }
    LExit:
        ReleaseMem(pbBytes);
    }

    void CfgTest::CheckCfgAndRegValueString(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName, LPCWSTR wzExpectedValue)
    {
        HRESULT hr = S_OK;
        LPWSTR sczValue = NULL;

        hr = RegReadString(hk, wzName, &sczValue);
        ExitOnFailure(hr, "Failed to read registry string:%ls", wzName);

        if (0 != lstrcmpW(sczValue, wzExpectedValue))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Wrong value in registry! Expected value '%ls', found value '%ls'", wzExpectedValue, sczValue);
        }

        hr = CfgGetString(cdhDb, wzCfgName, &sczValue);
        ExitOnFailure(hr, "Failed to read cfg db string:%ls", wzCfgName);

        if (0 != lstrcmpW(sczValue, wzExpectedValue))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Wrong value in cfg db! Expected value '%ls', found value '%ls'", wzExpectedValue, sczValue);
        }

    LExit:
        ReleaseStr(sczValue);
    }

    void CfgTest::CheckCfgAndRegValueDword(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName, DWORD dwExpectedValue)
    {
        HRESULT hr = S_OK;
        DWORD dwValue = 0;

        hr = RegReadNumber(hk, wzName, &dwValue);
        ExitOnFailure(hr, "Failed to read registry dword:%ls", wzName);

        if (dwExpectedValue != dwValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Wrong value in registry! Expected value %u, found value %u", dwExpectedValue, dwValue);
        }

        hr = CfgGetDword(cdhDb, wzCfgName, &dwValue);
        ExitOnFailure(hr, "Failed to read cfg db dword:%ls", wzCfgName);

        if (dwValue != dwExpectedValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Wrong value in cfg db! Expected dword value %u, found dword value %u", dwExpectedValue, dwValue);
        }

    LExit:
        return;
    }

    void CfgTest::CheckCfgAndRegValueQword(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName, DWORD64 qwExpectedValue)
    {
        HRESULT hr = S_OK;
        DWORD64 qwValue = 0;

        hr = RegReadQword(hk, wzName, &qwValue);
        ExitOnFailure(hr, "Failed to read registry qword:%ls", wzName);

        if (qwExpectedValue != qwValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Wrong value in registry! Expected value %I64u, found value %I64u", qwExpectedValue, qwValue);
        }

        hr = CfgGetQword(cdhDb, wzCfgName, &qwValue);
        ExitOnFailure(hr, "Failed to read cfg db qword:%ls", wzCfgName);

        if (qwValue != qwExpectedValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Wrong value in cfg db! Expected qword value %I64u, found qword value %I64u", qwExpectedValue, qwValue);
        }

    LExit:
        return;
    }

    void CfgTest::CheckCfgAndRegValueDeleted(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName)
    {
        HRESULT hr = S_OK;
        LPWSTR sczValue = NULL;

        hr = RegReadString(hk, wzName, &sczValue);
        if (E_FILENOTFOUND == hr || HRESULT_FROM_WIN32(ERROR_KEY_DELETED) == hr)
        {
            hr = S_OK;
        }
        else if (S_OK == hr)
        {
            hr = E_FAIL;
        }
        ExitOnFailure(hr, "Registry string should not exist:%ls", wzName);

        hr = CfgGetString(cdhDb, wzCfgName, &sczValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (S_OK == hr)
        {
            hr = E_FAIL;
        }
        ExitOnFailure(hr, "Cfg db value should not exist:%ls", wzName);

    LExit:
        ReleaseStr(sczValue);
    }

    void CfgTest::CheckCfgAndFile(CFGDB_HANDLE cdhDb, LPCWSTR wzFileName, LPCWSTR wzFilePath, BYTE *pbBuffer, SIZE_T cbBuffer)
    {
        HRESULT hr = S_OK;
        BYTE *pbDataReadFromFile = NULL;
        DWORD cbDataReadFromFile = 0;

        ExpectFile(cdhDb, wzFileName, pbBuffer, cbBuffer);

        hr = FileRead(&pbDataReadFromFile, &cbDataReadFromFile, wzFilePath);
        ExitOnFailure(hr, "Failed to read file: %ls", wzFilePath);

        if (cbBuffer != cbDataReadFromFile)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "File %ls should be size %u, but was size %u instead", wzFilePath, cbBuffer, cbDataReadFromFile);
        }

        if (0 != memcmp(pbBuffer, pbDataReadFromFile, cbBuffer))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "File contents don't match for file of name: %ls", wzFilePath);
        }
    LExit:
        ReleaseMem(pbDataReadFromFile);
    }

    void CfgTest::CheckCfgAndFileDeleted(CFGDB_HANDLE cdhDb, LPCWSTR wzFileName, LPCWSTR wzFilePath)
    {
        HRESULT hr = S_OK;
        BYTE *pbDataReadFromFile = NULL;
        DWORD cbDataReadFromFile = 0;

        ExpectNoFile(cdhDb, wzFileName);

        hr = FileRead(&pbDataReadFromFile, &cbDataReadFromFile, wzFilePath);
        if (E_FILENOTFOUND != hr && E_PATHNOTFOUND != hr)
        {
            ExitOnFailure(hr, "Shouldn't have found file: %ls", wzFilePath);
        }
    LExit:
        ReleaseMem(pbDataReadFromFile);
    }

    void CfgTest::ExpectNoValue(CFGDB_HANDLE cdhDb, LPCWSTR wzValueName)
    {
        HRESULT hr = S_OK;
        LPWSTR sczValue = NULL;

        hr = CfgGetString(cdhDb, wzValueName, &sczValue);
        if (E_NOTFOUND != hr)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Value shouldn't be here, but it is!");
        }

    LExit:
        ReleaseStr(sczValue);
    }

    void CfgTest::ExpectString(CFGDB_HANDLE cdhDb, LPCWSTR wzValueName, LPCWSTR wzExpectedValue)
    {
        HRESULT hr = S_OK;
        LPWSTR sczValue = NULL;

        hr = CfgGetString(cdhDb, wzValueName, &sczValue);
        ExitOnFailure(hr, "Couldn't read string: %ls", wzValueName);

        if (0 != lstrcmpW(sczValue, wzExpectedValue))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Value should have been '%ls', but was '%ls' instead", wzExpectedValue, sczValue);
        }

    LExit:
        ReleaseStr(sczValue);
    }

    void CfgTest::ExpectDword(CFGDB_HANDLE cdhDb, LPCWSTR wzValueName, DWORD dwExpectedValue)
    {
        HRESULT hr = S_OK;
        DWORD dwValue = 0;

        hr = CfgGetDword(cdhDb, wzValueName, &dwValue);
        ExitOnFailure(hr, "Couldn't read string: %ls", wzValueName);

        if (dwExpectedValue != dwValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Value should have been '%u', but was '%u' instead", dwExpectedValue, dwValue);
        }

    LExit:
        return;
    }

    void CfgTest::ExpectBool(CFGDB_HANDLE cdhDb, LPCWSTR wzValueName, BOOL fExpectedValue)
    {
        HRESULT hr = S_OK;
        BOOL fValue = 0;

        hr = CfgGetBool(cdhDb, wzValueName, &fValue);
        ExitOnFailure(hr, "Couldn't read string: %ls", wzValueName);

        if (fExpectedValue != fValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Value should have been '%u', but was '%u' instead", fExpectedValue, fValue);
        }

    LExit:
        return;
    }

    void CfgTest::ExpectFile(CFGDB_HANDLE cdhDb, LPCWSTR wzFileName, BYTE *pbBuffer, SIZE_T cbBuffer)
    {
        HRESULT hr = S_OK;
        BYTE *pbLocalBuffer = NULL;
        SIZE_T cbLocalBuffer = 0;

        hr = CfgGetBlob(cdhDb, wzFileName, &pbLocalBuffer, &cbLocalBuffer);
        ExitOnFailure(hr, "Failed to get file: %ls", wzFileName);

        if (cbBuffer != cbLocalBuffer)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "File should be size %u, but was size %u instead", cbBuffer, cbLocalBuffer);
        }

        if (0 != memcmp(pbBuffer, pbLocalBuffer, cbBuffer))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "File contents don't match for file of name: %ls", wzFileName);
        }

    LExit:
        ReleaseMem(pbLocalBuffer);
    }

    void CfgTest::ExpectNoFile(CFGDB_HANDLE cdhDb, LPCWSTR wzFileName)
    {
        HRESULT hr = S_OK;
        BYTE *pbLocalBuffer = NULL;
        SIZE_T cbLocalBuffer = 0;

        hr = CfgGetBlob(cdhDb, wzFileName, &pbLocalBuffer, &cbLocalBuffer);
        if (E_NOTFOUND != hr)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "File shouldn't be here, but it is!");
        }

    LExit:
        ReleaseMem(pbLocalBuffer);
    }

    void CfgTest::ExpectNoKey(HKEY hk, LPCWSTR wzKeyName)
    {
        HRESULT hr = S_OK;
        HKEY hkSub = NULL;

        hr = RegOpen(hk, wzKeyName, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hkSub);
        if (E_FILENOTFOUND != hr)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Regkey should not exist, but it does: %ls", wzKeyName);
        }

    LExit:
        ReleaseRegKey(hkSub);
    }

    void CfgTest::ExpectIniValue(INI_HANDLE iniHandle, LPCWSTR wzValueName, LPCWSTR wzExpectedValue)
    {
        HRESULT hr = S_OK;
        INI_VALUE* rgivValues = NULL;
        DWORD civValues = 0;

        hr = IniGetValueList(iniHandle, &rgivValues, &civValues);
        ExitOnFailure(hr, "Failed to get value list from INI handle");

        for (DWORD i = 0; i < civValues; ++i)
        {
            if (0 == wcscmp(rgivValues[i].wzName, wzValueName))
            {
                if (0 == wcscmp(rgivValues[i].wzValue, wzExpectedValue))
                {
                    return;
                }
                else
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "Found ini value %ls, but we expected to find value %ls, and found value %ls instead", wzValueName, wzExpectedValue, rgivValues[i].wzValue);
                }
            }
        }

        hr = E_NOTFOUND;
        ExitOnFailure(hr, "Failed to find value %ls in INI", wzValueName);

    LExit:
        return;
    }

    void CfgTest::ExpectNoIniValue(INI_HANDLE iniHandle, LPCWSTR wzValueName)
    {
        HRESULT hr = S_OK;
        INI_VALUE* rgivValues = NULL;
        DWORD civValues = 0;

        hr = IniGetValueList(iniHandle, &rgivValues, &civValues);
        ExitOnFailure(hr, "Failed to get value list from INI handle");

        for (DWORD i = 0; i < civValues; ++i)
        {
            if (0 == wcscmp(rgivValues[i].wzName, wzValueName))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Found ini value %ls, but we shouldn't have!");
            }
        }

    LExit:
        return;
    }

    // Expectations for Enumerations
    void CfgTest::ExpectDatabaseInEnum(CFG_ENUMERATION_HANDLE cehHandle, LPCWSTR wzExpectedFriendlyName, BOOL fExpectedSyncByDefault, LPCWSTR wzExpectedPath)
    {
        HRESULT hr = S_OK;
        DWORD dwCount = 0;
        LPCWSTR wzFriendlyName = NULL;
        BOOL fSyncByDefault = FALSE;
        LPCWSTR wzPath = NULL;

        hr = CfgEnumReadDword(cehHandle, 0, ENUM_DATA_COUNT, &dwCount);
        ExitOnFailure(hr, "Failed to get count of items in database list enumeration");

        for (DWORD i = 0; i < dwCount; ++i)
        {
            hr = CfgEnumReadString(cehHandle, i, ENUM_DATA_FRIENDLY_NAME, &wzFriendlyName);
            ExitOnFailure(hr, "Failed to read friendly name from database list enum at index %u", i);

            hr = CfgEnumReadBool(cehHandle, i, ENUM_DATA_SYNC_BY_DEFAULT, &fSyncByDefault);
            ExitOnFailure(hr, "Failed to read path from database list enum at index %u", i);

            hr = CfgEnumReadString(cehHandle, i, ENUM_DATA_PATH, &wzPath);
            ExitOnFailure(hr, "Failed to read path from database list enum at index %u", i);

            // If it isn't the right friendly name, skip to the next database in the enumeration
            if (0 != lstrcmpW(wzExpectedFriendlyName, wzFriendlyName))
            {
                continue;
            }

            if (0 != lstrcmpW(wzExpectedPath, wzPath))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Database list friendly name '%ls' has incorrect path (expected '%ls', found '%ls')", wzExpectedFriendlyName, wzExpectedPath, wzPath);
            }

            if (fSyncByDefault != fExpectedSyncByDefault)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Database list friendly name '%ls' has incorrect 'sync by default' flag (expected %u, found %u)", wzExpectedFriendlyName, fExpectedSyncByDefault, fSyncByDefault);
            }

            // We found the right one, so return successfully now
            return;
        }

        hr = E_FAIL;
        ExitOnFailure(hr, "Expected to find database in list, but didn't find it: %ls, %ls", wzFriendlyName, wzPath);

    LExit:
        return;
    }

    void CfgTest::ExpectNoDatabaseInEnum(CFG_ENUMERATION_HANDLE cehHandle, LPCWSTR wzExpectedFriendlyName)
    {
        HRESULT hr = S_OK;
        DWORD dwCount = 0;
        LPCWSTR wzFriendlyName = NULL;
        BOOL fSyncByDefault = FALSE;
        LPCWSTR wzPath = NULL;

        hr = CfgEnumReadDword(cehHandle, 0, ENUM_DATA_COUNT, &dwCount);
        ExitOnFailure(hr, "Failed to get count of items in database list enumeration");

        for (DWORD i = 0; i < dwCount; ++i)
        {
            hr = CfgEnumReadString(cehHandle, i, ENUM_DATA_FRIENDLY_NAME, &wzFriendlyName);
            ExitOnFailure(hr, "Failed to read friendly name from database list enum at index %u", i);

            hr = CfgEnumReadBool(cehHandle, i, ENUM_DATA_SYNC_BY_DEFAULT, &fSyncByDefault);
            ExitOnFailure(hr, "Failed to read path from database list enum at index %u", i);

            hr = CfgEnumReadString(cehHandle, i, ENUM_DATA_PATH, &wzPath);
            ExitOnFailure(hr, "Failed to read path from database list enum at index %u", i);

            // If it isn't the right friendly name, skip to the next database in the enumeration
            if (0 == lstrcmpW(wzExpectedFriendlyName, wzFriendlyName))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Should not have found friendly name '%ls' in database list, but it was found!", wzExpectedFriendlyName);
            }
        }

    LExit:
        return;
    }

    void CfgTest::WaitForDbToBeIdle(CFGDB_HANDLE cdHandle)
    {
        CFG_ENUMERATION_HANDLE cehProductList = NULL;
        HRESULT hr = CfgEnumerateProducts(cdHandle, &cehProductList, NULL);
        ExitOnFailure(hr, "Failed to enumerate products to confirm DB is idle");

    LExit:
        CfgReleaseEnumeration(cehProductList);
    }

    void CfgTest::VerifyHistoryString(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, LPCWSTR wzValue)
    {
        HRESULT hr = S_OK;
        CONFIG_VALUETYPE cvType = VALUE_INVALID;
        LPCWSTR wzValueFromEnum = NULL;
        LPCWSTR wzByFromEnum = NULL;
        SYSTEMTIME st;

        hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
        ExitOnFailure(hr, "Failed to get value type: %u", dwIndex);

        if (VALUE_STRING != cvType)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Expected to find string value, found type: %d", cvType);
        }

        hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_VALUESTRING, &wzValueFromEnum);
        ExitOnFailure(hr, "Failed to enumerate string value: %u", dwIndex);
        if (0 != lstrcmpW(wzValue, wzValueFromEnum))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Expected value '%ls', found value '%ls'", wzValue, wzValueFromEnum);
        }

        hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
        ExitOnFailure(hr, "Failed to read when value: %u", dwIndex);
        if (0 == st.wYear || 0 == st.wMonth)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'when' time encountered!");
        }

        hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzByFromEnum);
        ExitOnFailure(hr, "Failed to read by value: %u", dwIndex);
        if (0 == lstrlenW(wzByFromEnum))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'by' string encountered!");
        }

    LExit:
        return;
    }

    void CfgTest::VerifyHistoryDword(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, DWORD dwInValue)
    {
        HRESULT hr = S_OK;
        CONFIG_VALUETYPE cvType = VALUE_INVALID;
        DWORD dwValue;
        LPCWSTR wzBy = NULL;
        SYSTEMTIME st;

        hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
        ExitOnFailure(hr, "Failed to get value type: %u", dwIndex);

        if (VALUE_DWORD != cvType)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Expected to find dword value, found type: %d", cvType);
        }

        hr = CfgEnumReadDword(cehHandle, dwIndex, ENUM_DATA_VALUEDWORD, &dwValue);
        ExitOnFailure(hr, "Failed to enumerate dword value: %u", dwIndex);
        if (dwValue != dwInValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Expected value %u, found value %u", dwInValue, dwValue);
        }

        hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
        ExitOnFailure(hr, "Failed to read when value: %u", dwIndex);
        if (0 == st.wYear || 0 == st.wMonth)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'when' time encountered!");
        }

        hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzBy);
        ExitOnFailure(hr, "Failed to read by value: %u", dwIndex);
        if (0 == lstrlenW(wzBy))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'by' string encountered!");
        }

    LExit:
        return;
    }

    void CfgTest::VerifyHistoryBool(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, BOOL fInValue)
    {
        HRESULT hr = S_OK;
        CONFIG_VALUETYPE cvType = VALUE_INVALID;
        BOOL fValue;
        LPCWSTR wzBy = NULL;
        SYSTEMTIME st;

        hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
        ExitOnFailure(hr, "Failed to get value type: %u", dwIndex);

        if (VALUE_BOOL != cvType)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Expected to find bool value, found type: %d", cvType);
        }

        hr = CfgEnumReadBool(cehHandle, dwIndex, ENUM_DATA_VALUEBOOL, &fValue);
        ExitOnFailure(hr, "Failed to enumerate bool value: %u", dwIndex);
        if (fValue != fInValue)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Expected value %ls, found value %ls", fInValue ? L"TRUE" : L"FALSE", fValue ? L"TRUE" : L"FALSE");
        }

        hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
        ExitOnFailure(hr, "Failed to read when value: %u", dwIndex);
        if (0 == st.wYear || 0 == st.wMonth)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'when' time encountered!");
        }

        hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzBy);
        ExitOnFailure(hr, "Failed to read by value: %u", dwIndex);
        if (0 == lstrlenW(wzBy))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'by' string encountered!");
        }

    LExit:
        return;
    }

    void CfgTest::VerifyHistoryBlob(CFGDB_HANDLE cdhLocal, CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, const BYTE *pbExpected, SIZE_T cbExpected)
    {
        HRESULT hr = S_OK;
        CONFIG_VALUETYPE cvType = VALUE_INVALID;
        BYTE *pbValue = NULL;
        SIZE_T cbValue = 0;
        LPCWSTR wzBy = NULL;
        SYSTEMTIME st;

        hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
        ExitOnFailure(hr, "Failed to get value type: %u", dwIndex);

        if (VALUE_BLOB != cvType)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Expected to find blob value, found type: %d", cvType);
        }

        hr = CfgEnumReadBinary(cdhLocal, cehHandle, dwIndex, ENUM_DATA_BLOBCONTENT, &pbValue, &cbValue);
        ExitOnFailure(hr, "Failed to enumerate blob value: %u", dwIndex);
        if (cbValue != cbExpected)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Expected blob of size %u, found blob of size %u", cbExpected, cbValue);
        }
        if (0 != memcmp(pbValue, pbExpected, cbExpected))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Blob history data didn't match expected blob");
        }

        hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
        ExitOnFailure(hr, "Failed to read when value: %u", dwIndex);
        if (0 == st.wYear || 0 == st.wMonth)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'when' time encountered!");
        }

        hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzBy);
        ExitOnFailure(hr, "Failed to read by value: %u", dwIndex);
        if (0 == lstrlenW(wzBy))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'by' string encountered!");
        }

    LExit:
        ReleaseMem(pbValue);

        return;
    }

    void CfgTest::VerifyHistoryDeleted(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex)
    {
        HRESULT hr = S_OK;
        CONFIG_VALUETYPE cvType = VALUE_INVALID;
        LPCWSTR wzBy = NULL;
        SYSTEMTIME st;

        hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
        ExitOnFailure(hr, "Failed to read deleted value: %u", dwIndex);
        if (VALUE_DELETED != cvType)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Value should have been deleted, but it exists still!");
        }

        hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
        ExitOnFailure(hr, "Failed to read when value: %u", dwIndex);
        if (0 == st.wYear || 0 == st.wMonth)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'when' time encountered!");
        }

        hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzBy);
        ExitOnFailure(hr, "Failed to read by value: %u", dwIndex);
        if (0 == lstrlenW(wzBy))
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "Empty 'by' string encountered!");
        }

    LExit:
        return;
    }
}
