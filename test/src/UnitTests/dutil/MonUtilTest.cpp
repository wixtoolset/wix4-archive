//-------------------------------------------------------------------------------------------------
// <copyright file="MonUtilTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Text;
using namespace Xunit;

namespace DutilTests
{
    const int PREWAIT = 20;
    const int POSTWAIT = 480;
    const int FULLWAIT = 500;
    const int SILENCEPERIOD = 100;

    struct RegKey
    {
        HRESULT hr;
        HKEY hkRoot;
        LPCWSTR wzSubKey;
        REG_KEY_BITNESS kbKeyBitness;
        BOOL fRecursive;
    };
    struct Directory
    {
        HRESULT hr;
        LPCWSTR wzPath;
        BOOL fRecursive;
    };
    struct Results
    {
        RegKey *rgRegKeys;
        DWORD cRegKeys;
        Directory *rgDirectories;
        DWORD cDirectories;
    };

    public delegate void MonGeneralDelegate(HRESULT, LPVOID);

    public delegate void MonDriveStatusDelegate(WCHAR, BOOL, LPVOID);

    public delegate void MonDirectoryDelegate(HRESULT, LPCWSTR, BOOL, LPVOID, LPVOID);

    public delegate void MonRegKeyDelegate(HRESULT, HKEY, LPCWSTR, REG_KEY_BITNESS, BOOL, LPVOID, LPVOID);

    static void MonGeneral(
        __in HRESULT /*hrResult*/,
        __in_opt LPVOID /*pvContext*/
        )
    {
        Assert::True(false);
    }

    static void MonDriveStatus(
        __in WCHAR /*chDrive*/,
        __in BOOL /*fArriving*/,
        __in_opt LPVOID /*pvContext*/
        )
    {
    }

    static void MonDirectory(
        __in HRESULT hrResult,
        __in_z LPCWSTR wzPath,
        __in_z BOOL fRecursive,
        __in_opt LPVOID pvContext,
        __in_opt LPVOID pvDirectoryContext
        )
    {
        Assert::Equal<HRESULT>(S_OK, hrResult);
        Assert::Equal<DWORD_PTR>(0, reinterpret_cast<DWORD_PTR>(pvDirectoryContext));

        HRESULT hr = S_OK;
        Results *pResults = reinterpret_cast<Results *>(pvContext);

        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pResults->rgDirectories), pResults->cDirectories + 1, sizeof(Directory), 5);
        Assert::Equal<HRESULT>(S_OK, hr);
        ++pResults->cDirectories;

        pResults->rgDirectories[pResults->cDirectories - 1].hr = hrResult;
        pResults->rgDirectories[pResults->cDirectories - 1].wzPath = wzPath;
        pResults->rgDirectories[pResults->cDirectories - 1].fRecursive = fRecursive;
    }

    static void MonRegKey(
        __in HRESULT hrResult,
        __in HKEY hkRoot,
        __in_z LPCWSTR wzSubKey,
        __in REG_KEY_BITNESS kbKeyBitness,
        __in_z BOOL fRecursive,
        __in_opt LPVOID pvContext,
        __in_opt LPVOID pvRegKeyContext
        )
    {
        Assert::Equal<HRESULT>(S_OK, hrResult);
        Assert::Equal<DWORD_PTR>(0, reinterpret_cast<DWORD_PTR>(pvRegKeyContext));

        HRESULT hr = S_OK;
        Results *pResults = reinterpret_cast<Results *>(pvContext);

        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pResults->rgRegKeys), pResults->cRegKeys + 1, sizeof(RegKey), 5);
        Assert::Equal<HRESULT>(S_OK, hr);
        ++pResults->cRegKeys;

        pResults->rgRegKeys[pResults->cRegKeys - 1].hr = hrResult;
        pResults->rgRegKeys[pResults->cRegKeys - 1].hkRoot = hkRoot;
        pResults->rgRegKeys[pResults->cRegKeys - 1].wzSubKey = wzSubKey;
        pResults->rgRegKeys[pResults->cRegKeys - 1].kbKeyBitness = kbKeyBitness;
        pResults->rgRegKeys[pResults->cRegKeys - 1].fRecursive = fRecursive;
    }

    public ref class MonUtil
    {
    public:
        void ClearResults(Results *pResults)
        {
            ReleaseNullMem(pResults->rgDirectories);
            pResults->cDirectories = 0;
            ReleaseNullMem(pResults->rgRegKeys);
            pResults->cRegKeys = 0;
        }

        void RemoveDirectory(LPCWSTR wzPath)
        {
            DWORD dwRetryCount = 0;
            const DWORD c_dwMaxRetryCount = 100;
            const DWORD c_dwRetryInterval = 50;

            HRESULT hr = DirEnsureDelete(wzPath, TRUE, TRUE);

            // Monitoring a directory opens a handle to that directory, which means delete requests for that directory will succeed
            // (and deletion will be "pending" until our monitor handle is closed)
            // but deletion of the directory containing that directory cannot complete until the handle is closed. This means DirEnsureDelete()
            // can sometimes encounter HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY) failures, which just means it needs to retry a bit later
            // (after the waiter thread wakes up, it will release the handle)
            while (hr == HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY) && dwRetryCount < c_dwMaxRetryCount)
            {
                ::Sleep(c_dwRetryInterval);
                ++dwRetryCount;
                hr = DirEnsureDelete(wzPath, TRUE, TRUE);
            }

            Assert::True(S_OK == hr || S_FALSE == hr || E_PATHNOTFOUND == hr);
        }

        void TestDirectory(MON_HANDLE handle, Results *pResults)
        {
            HRESULT hr  = S_OK;
            LPWSTR sczShallowPath = NULL;
            LPWSTR sczParentPath = NULL;
            LPWSTR sczDeepPath = NULL;
            LPWSTR sczChildPath = NULL;
            LPWSTR sczChildFilePath = NULL;

            hr = PathExpand(&sczShallowPath, L"%TEMP%\\MonUtilTest\\", PATH_EXPAND_ENVIRONMENT);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathExpand(&sczParentPath, L"%TEMP%\\MonUtilTest\\sub\\folder\\that\\might\\not\\", PATH_EXPAND_ENVIRONMENT);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathExpand(&sczDeepPath, L"%TEMP%\\MonUtilTest\\sub\\folder\\that\\might\\not\\exist\\", PATH_EXPAND_ENVIRONMENT);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathExpand(&sczChildPath, L"%TEMP%\\MonUtilTest\\sub\\folder\\that\\might\\not\\exist\\some\\sub\\folder\\", PATH_EXPAND_ENVIRONMENT);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = PathExpand(&sczChildFilePath, L"%TEMP%\\MonUtilTest\\sub\\folder\\that\\might\\not\\exist\\some\\sub\\folder\\file.txt", PATH_EXPAND_ENVIRONMENT);
            Assert::Equal<HRESULT>(S_OK, hr);

            RemoveDirectory(sczShallowPath);

            hr = MonAddDirectory(handle, sczDeepPath, TRUE, SILENCEPERIOD, NULL);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = DirEnsureExists(sczParentPath, NULL);
            Assert::True(S_OK == hr || S_FALSE == hr);
            // Make sure creating the parent directory does nothing, even after silence period
            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(0, pResults->cDirectories);

            // Now create the target path, no notification until after the silence period
            hr = DirEnsureExists(sczDeepPath, NULL);
            Assert::True(S_OK == hr || S_FALSE == hr);
            ::Sleep(PREWAIT);
            Assert::Equal<DWORD>(0, pResults->cDirectories);

            // Now after the full silence period, it should have triggered
            ::Sleep(POSTWAIT);
            Assert::Equal<DWORD>(1, pResults->cDirectories);
            Assert::Equal<HRESULT>(S_OK, pResults->rgDirectories[0].hr);

            // Now delete the directory, along with a ton of parents. This verifies MonUtil will keep watching the closest parent that still exists.
            RemoveDirectory(sczShallowPath);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(2, pResults->cDirectories);
            Assert::Equal<HRESULT>(S_OK, pResults->rgDirectories[1].hr);

            // Create the parent directory again, still should be nothing even after full silence period
            hr = DirEnsureExists(sczParentPath, NULL);
            Assert::True(S_OK == hr || S_FALSE == hr);
            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(2, pResults->cDirectories);

            hr = DirEnsureExists(sczChildPath, NULL);
            Assert::True(S_OK == hr || S_FALSE == hr);
            ::Sleep(PREWAIT);
            Assert::Equal<DWORD>(2, pResults->cDirectories);

            ::Sleep(POSTWAIT);
            Assert::Equal<DWORD>(3, pResults->cDirectories);
            Assert::Equal<HRESULT>(S_OK, pResults->rgDirectories[2].hr);

            // Write a file to a deep child subfolder, and make sure it's detected
            hr = FileFromString(sczChildFilePath, 0, L"contents", FILE_ENCODING_UTF16_WITH_BOM);
            Assert::Equal<HRESULT>(S_OK, hr);
            ::Sleep(PREWAIT);
            Assert::Equal<DWORD>(3, pResults->cDirectories);

            ::Sleep(POSTWAIT);
            Assert::Equal<DWORD>(4, pResults->cDirectories);
            Assert::Equal<HRESULT>(S_OK, pResults->rgDirectories[2].hr);

            RemoveDirectory(sczParentPath);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(5, pResults->cDirectories);
            Assert::Equal<HRESULT>(S_OK, pResults->rgDirectories[3].hr);

            // Now remove the directory from the list of things to monitor, and confirm changes are no longer tracked
            hr = MonRemoveDirectory(handle, sczDeepPath, TRUE);
            Assert::Equal<HRESULT>(S_OK, hr);
            ::Sleep(PREWAIT);

            hr = DirEnsureExists(sczDeepPath, NULL);
            Assert::True(S_OK == hr || S_FALSE == hr);
            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(5, pResults->cDirectories);
            Assert::Equal<HRESULT>(S_OK, pResults->rgDirectories[3].hr);

            // Finally, add it back so we can test multiple things to monitor at once
            hr = MonAddDirectory(handle, sczDeepPath, TRUE, SILENCEPERIOD, NULL);
            Assert::Equal<HRESULT>(S_OK, hr);

            ReleaseStr(sczShallowPath);
            ReleaseStr(sczDeepPath);
            ReleaseStr(sczParentPath);
        }

        void TestRegKey(MON_HANDLE handle, Results *pResults)
        {
            HRESULT hr = S_OK;
            LPCWSTR wzShallowRegKey = L"Software\\MonUtilTest\\";
            LPCWSTR wzParentRegKey = L"Software\\MonUtilTest\\sub\\folder\\that\\might\\not\\";
            LPCWSTR wzDeepRegKey = L"Software\\MonUtilTest\\sub\\folder\\that\\might\\not\\exist\\";
            LPCWSTR wzChildRegKey = L"Software\\MonUtilTest\\sub\\folder\\that\\might\\not\\exist\\some\\sub\\folder\\";
            HKEY hk = NULL;

            hr = RegDelete(HKEY_CURRENT_USER, wzShallowRegKey, REG_KEY_32BIT, TRUE);
            Assert::True(S_OK == hr || S_FALSE == hr || E_PATHNOTFOUND == hr);

            hr = MonAddRegKey(handle, HKEY_CURRENT_USER, wzDeepRegKey, REG_KEY_DEFAULT, TRUE, SILENCEPERIOD, NULL);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = RegCreate(HKEY_CURRENT_USER, wzParentRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            ReleaseRegKey(hk);
            // Make sure creating the parent key does nothing, even after silence period
            ::Sleep(FULLWAIT);
            Assert::True(S_OK == hr || S_FALSE == hr);
            Assert::Equal<DWORD>(0, pResults->cRegKeys);

            // Now create the target path, no notification until after the silence period
            hr = RegCreate(HKEY_CURRENT_USER, wzDeepRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            Assert::True(S_OK == hr || S_FALSE == hr);
            ReleaseRegKey(hk);
            ::Sleep(PREWAIT);
            Assert::Equal<DWORD>(0, pResults->cRegKeys);

            // Now after the full silence period, it should have triggered
            ::Sleep(POSTWAIT);
            Assert::Equal<DWORD>(1, pResults->cRegKeys);
            Assert::Equal<HRESULT>(S_OK, pResults->rgRegKeys[0].hr);

            // Now delete the directory, along with a ton of parents. This verifies MonUtil will keep watching the closest parent that still exists.
            hr = RegDelete(HKEY_CURRENT_USER, wzShallowRegKey, REG_KEY_32BIT, TRUE);
            Assert::True(S_OK == hr || S_FALSE == hr || E_PATHNOTFOUND == hr);
            ::Sleep(PREWAIT);
            Assert::Equal<DWORD>(1, pResults->cRegKeys);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(2, pResults->cRegKeys);
            Assert::Equal<HRESULT>(S_OK, pResults->rgRegKeys[1].hr);

            // Create the parent directory again, still should be nothing even after full silence period
            hr = RegCreate(HKEY_CURRENT_USER, wzParentRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            Assert::True(S_OK == hr || S_FALSE == hr);
            ReleaseRegKey(hk);
            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(2, pResults->cRegKeys);

            hr = RegCreate(HKEY_CURRENT_USER, wzChildRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            Assert::True(S_OK == hr || S_FALSE == hr);
            ::Sleep(PREWAIT);
            Assert::Equal<DWORD>(2, pResults->cRegKeys);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(3, pResults->cRegKeys);
            Assert::Equal<HRESULT>(S_OK, pResults->rgRegKeys[2].hr);

            // Write a registry value to some deep child subkey, and make sure it's detected
            hr = RegWriteString(hk, L"valuename", L"testvalue");
            Assert::Equal<HRESULT>(S_OK, hr);
            ReleaseRegKey(hk);
            ::Sleep(PREWAIT);
            Assert::Equal<DWORD>(3, pResults->cRegKeys);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(4, pResults->cRegKeys);
            Assert::Equal<HRESULT>(S_OK, pResults->rgRegKeys[2].hr);

            hr = RegDelete(HKEY_CURRENT_USER, wzDeepRegKey, REG_KEY_32BIT, TRUE);
            Assert::Equal<HRESULT>(S_OK, hr);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(5, pResults->cRegKeys);

            // Now remove the regkey from the list of things to monitor, and confirm changes are no longer tracked
            hr = MonRemoveRegKey(handle, HKEY_CURRENT_USER, wzDeepRegKey, REG_KEY_DEFAULT, TRUE);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = RegCreate(HKEY_CURRENT_USER, wzDeepRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            Assert::True(S_OK == hr || S_FALSE == hr);
            ReleaseRegKey(hk);
            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(5, pResults->cRegKeys);

            ReleaseRegKey(hk);
        }

        void TestMoreThan64(MON_HANDLE handle, Results *pResults)
        {
            HRESULT hr  = S_OK;
            LPWSTR sczBaseDir = NULL;
            LPWSTR sczDir = NULL;
            LPWSTR sczFile = NULL;

            hr = PathExpand(&sczBaseDir, L"%TEMP%\\ScalabilityTest\\", PATH_EXPAND_ENVIRONMENT);
            Assert::Equal<HRESULT>(S_OK, hr);

            for (DWORD i = 0; i < 200; ++i)
            {
                hr = StrAllocFormatted(&sczDir, L"%ls%u\\", sczBaseDir, i);
                Assert::Equal<HRESULT>(S_OK, hr);

                hr = DirEnsureExists(sczDir, NULL);
                Assert::True(hr == S_OK || hr == S_FALSE);

                hr = MonAddDirectory(handle, sczDir, FALSE, SILENCEPERIOD, NULL);
                Assert::Equal<HRESULT>(S_OK, hr);
            }

            hr = PathConcat(sczDir, L"file.txt", &sczFile);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = FileFromString(sczFile, 0, L"contents", FILE_ENCODING_UTF16_WITH_BOM);
            Assert::Equal<HRESULT>(S_OK, hr);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(1, pResults->cDirectories);

            for (DWORD i = 0; i < 199; ++i)
            {
                hr = StrAllocFormatted(&sczDir, L"%ls%u\\", sczBaseDir, i);
                Assert::Equal<HRESULT>(S_OK, hr);

                hr = MonRemoveDirectory(handle, sczDir, FALSE);
                Assert::Equal<HRESULT>(S_OK, hr);
            }
            ::Sleep(FULLWAIT);

            hr = FileFromString(sczFile, 0, L"contents2", FILE_ENCODING_UTF16_WITH_BOM);
            Assert::Equal<HRESULT>(S_OK, hr);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(2, pResults->cDirectories);

            for (DWORD i = 0; i < 199; ++i)
            {
                hr = StrAllocFormatted(&sczDir, L"%ls%u\\", sczBaseDir, i);
                Assert::Equal<HRESULT>(S_OK, hr);

                hr = MonAddDirectory(handle, sczDir, FALSE, SILENCEPERIOD, NULL);
                Assert::Equal<HRESULT>(S_OK, hr);
            }
            ::Sleep(FULLWAIT);

            hr = FileFromString(sczFile, 0, L"contents3", FILE_ENCODING_UTF16_WITH_BOM);
            Assert::Equal<HRESULT>(S_OK, hr);

            ::Sleep(FULLWAIT);
            Assert::Equal<DWORD>(3, pResults->cDirectories);

            ReleaseStr(sczBaseDir);
            ReleaseStr(sczDir);
            ReleaseStr(sczFile);
        }

        [Fact]
        void MonUtilTest()
        {
            HRESULT hr = S_OK;
            MON_HANDLE handle = NULL;
            Results *pResults = (Results *)MemAlloc(sizeof(Results), TRUE);
            Assert::True(NULL != pResults);

            // These ensure the function pointers we send point to this thread's appdomain, which helps with assembly binding when running tests within msbuild
            MonGeneralDelegate^ fpMonGeneral = gcnew MonGeneralDelegate(MonGeneral);
            GCHandle gchMonGeneral = GCHandle::Alloc(fpMonGeneral);
            IntPtr ipMonGeneral = Marshal::GetFunctionPointerForDelegate(fpMonGeneral);

            MonDriveStatusDelegate^ fpMonDriveStatus = gcnew MonDriveStatusDelegate(MonDriveStatus);
            GCHandle gchMonDriveStatus = GCHandle::Alloc(fpMonDriveStatus);
            IntPtr ipMonDriveStatus = Marshal::GetFunctionPointerForDelegate(fpMonDriveStatus);

            MonDirectoryDelegate^ fpMonDirectory = gcnew MonDirectoryDelegate(MonDirectory);
            GCHandle gchMonDirectory = GCHandle::Alloc(fpMonDirectory);
            IntPtr ipMonDirectory = Marshal::GetFunctionPointerForDelegate(fpMonDirectory);

            MonRegKeyDelegate^ fpMonRegKey = gcnew MonRegKeyDelegate(MonRegKey);
            GCHandle gchMonRegKey = GCHandle::Alloc(fpMonRegKey);
            IntPtr ipMonRegKey = Marshal::GetFunctionPointerForDelegate(fpMonRegKey);

            // "Silence period" is 100 ms
            hr = MonCreate(&handle, static_cast<PFN_MONGENERAL>(ipMonGeneral.ToPointer()), static_cast<PFN_MONDRIVESTATUS>(ipMonDriveStatus.ToPointer()), static_cast<PFN_MONDIRECTORY>(ipMonDirectory.ToPointer()), static_cast<PFN_MONREGKEY>(ipMonRegKey.ToPointer()), pResults);
            Assert::Equal<HRESULT>(S_OK, hr);

            hr = RegInitialize();
            Assert::Equal<HRESULT>(S_OK, hr);

            TestDirectory(handle, pResults);
            ClearResults(pResults);
            TestRegKey(handle, pResults);
            ClearResults(pResults);
            TestMoreThan64(handle, pResults);
            ClearResults(pResults);

            ReleaseMon(handle);

            gchMonGeneral.Free();
            gchMonDriveStatus.Free();
            gchMonDirectory.Free();
            gchMonRegKey.Free();

            ReleaseMem(pResults->rgDirectories);
            ReleaseMem(pResults->rgRegKeys);
            ReleaseMem(pResults);
        }
    };
}