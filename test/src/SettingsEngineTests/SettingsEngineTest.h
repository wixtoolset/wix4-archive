// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#pragma warning(push)
#pragma warning(disable:4995)
#include <vector>
#pragma warning(pop)

namespace CfgTests
{
    public struct BackgroundConflicts
    {
        CFGDB_HANDLE cdHandle;
        CONFLICT_PRODUCT *rgcpProduct;
        DWORD cProduct;
    };

    public struct TestContext
    {
    public:
        TestContext() { ResetAutoSyncStats(); }

        void ResetAutoSyncStats() { m_cSyncingProduct = 0; m_cSyncProductFinished = 0; m_cRedetectingProducts = 0; m_cRedetectProductsFinished = 0; m_cSyncingRemote = 0; m_cSyncRemoteFinished = 0; m_cAutoSyncRunning = 0; m_cRemoteGood = 0; }

        std::vector<BackgroundConflicts> m_backgroundConflicts;
        // Notification counts
        int m_cSyncingProduct;
        int m_cSyncProductFinished;
        int m_cRedetectingProducts;
        int m_cRedetectProductsFinished;
        int m_cSyncingRemote;
        int m_cSyncRemoteFinished;
        int m_cAutoSyncRunning;
        int m_cRemoteGood;
    };

    // Raw Callbacks
    void __stdcall BackgroundStatusCallback(HRESULT hr, BACKGROUND_STATUS_TYPE type, LPCWSTR wzString1, LPCWSTR wzString2, LPCWSTR wzString3, LPVOID pvContext);
    void __stdcall BackgroundConflictsFoundCallback(CFGDB_HANDLE cdHandle, CONFLICT_PRODUCT *rgcpProduct, DWORD cProduct, LPVOID pvContext);

    public ref class CfgTest
    {
    public:
        CfgTest() { m_pContext = new TestContext(); }
        ~CfgTest() { delete m_pContext; }

        // Init / uninit
        void TestInitialize();
        void TestUninitialize();
        void RedirectDatabases();

        // Current time manipulation
        void AddToSystemTime(DWORD dwSeconds);

        // Non-Cfg Commands
        void SetARP(LPCWSTR wzKeyName, LPCWSTR wzDisplayName, LPCWSTR wzInstallLocation, LPCWSTR wzUninstallString);
        void SetApplication(LPCWSTR wzFileName, LPCWSTR wzFilePath);
        void ResetApplications();

        // Cfg Commands
        void WaitForSqlCeTimestampChange();
        void WaitForAutoSync(CFGDB_HANDLE cdhDb);
        void WaitForSyncNoResolve(CFGDB_HANDLE cdhDb);
        void WaitForSyncResolveAll(CFGDB_HANDLE cdhDb, RESOLUTION_CHOICE rcChoice);
        void WaitForSyncLatestLegacy(CFGDB_HANDLE cdhDb);

        // Plain read Expectations
        void ExpectProductRegistered(CFGDB_HANDLE cdhAdminDb, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey);
        void ExpectProductUnregistered(CFGDB_HANDLE cdhAdminDb, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey);
        void ExpectAdminProductRegistered(CFGDB_HANDLE cdhAdminDb, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey);
        void ExpectAdminProductUnregistered(CFGDB_HANDLE cdhAdminDb, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey);
        void CheckCfgAndRegValueFlag(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName, BOOL fExpectedValue, DWORD dwOffset);
        void CheckCfgAndRegValueString(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName, LPCWSTR wzExpectedValue);
        void CheckCfgAndRegValueDword(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName, DWORD dwExpectedValue);
        void CheckCfgAndRegValueQword(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName, DWORD64 qwExpectedValue);
        void CheckCfgAndRegValueDeleted(CFGDB_HANDLE cdhDb, HKEY hk, LPCWSTR wzCfgName, LPCWSTR wzName);
        void CheckCfgAndFile(CFGDB_HANDLE cdhDb, LPCWSTR wzFileName, LPCWSTR wzFilePath, BYTE *pbBuffer, SIZE_T cbBuffer);
        void CheckCfgAndFileDeleted(CFGDB_HANDLE cdhDb, LPCWSTR wzFileName, LPCWSTR wzFilePath);
        void ExpectString(CFGDB_HANDLE cdhDb, LPCWSTR wzValueName, LPCWSTR wzExpectedValue);
        void ExpectDword(CFGDB_HANDLE cdhDb, LPCWSTR wzValueName, DWORD dwExpectedValue);
        void ExpectBool(CFGDB_HANDLE cdhDb, LPCWSTR wzValueName, BOOL fExpectedValue);
        void ExpectNoValue(CFGDB_HANDLE cdhDb, LPCWSTR wzValueName);
        void ExpectFile(CFGDB_HANDLE cdhDb, LPCWSTR wzFileName, BYTE *pbBuffer, SIZE_T cbBuffer);
        void ExpectNoFile(CFGDB_HANDLE cdhDb, LPCWSTR wzFileName);
        void ExpectNoKey(HKEY hk, LPCWSTR wzKeyName);
        void ExpectIniValue(INI_HANDLE iniHandle, LPCWSTR wzValueName, LPCWSTR wzExpectedValue);
        void ExpectNoIniValue(INI_HANDLE iniHandle, LPCWSTR wzValueName);

        // Expectations for Enumerations
        void ExpectDatabaseInEnum(CFG_ENUMERATION_HANDLE cehHandle, LPCWSTR wzExpectedFriendlyName, BOOL fExpectedSyncByDefault, LPCWSTR wzExpectedPath);
        void ExpectNoDatabaseInEnum(CFG_ENUMERATION_HANDLE cehHandle, LPCWSTR wzExpectedFriendlyName);

        // Past values enumeration verification
        void VerifyHistoryString(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, LPCWSTR wzValue);
        void VerifyHistoryDword(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, DWORD dwInValue);
        void VerifyHistoryBool(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, BOOL fInValue);
        void VerifyHistoryBlob(CFGDB_HANDLE cdhLocal, CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, const BYTE *pbExpected, SIZE_T cbExpected);
        void VerifyHistoryDeleted(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex);

    protected:
        TestContext *m_pContext;

    private:
        void WaitForDbToBeIdle(CFGDB_HANDLE cdHandle);
    };
}
