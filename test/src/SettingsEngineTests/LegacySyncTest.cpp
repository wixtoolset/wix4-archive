// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class LegacySync : public CfgTest
    {
    public:
        HRESULT GetFilePaths(
            LPWSTR *psczFileSettingsDir,
            LPWSTR *psczFileA,
            LPWSTR *psczFileB
            )
        {
            HRESULT hr = S_OK;
            LPWSTR sczMyDocumentsDir = NULL;
            LPWSTR sczSubDir = NULL;

            hr = PathGetKnownFolder(CSIDL_MYDOCUMENTS, &sczMyDocumentsDir);
            ExitOnFailure(hr, "Failed to get my documents folder location");

            hr = PathConcat(sczMyDocumentsDir, L"CfgFileTest", psczFileSettingsDir);
            ExitOnFailure(hr, "Failed to concat CfgFileTest to my documents path");

            hr = DirEnsureExists(*psczFileSettingsDir, NULL);
            ExitOnFailure(hr, "Failed to ensure directory exists: %ls", *psczFileSettingsDir);

            hr = PathConcat(*psczFileSettingsDir, L"FileA.tst", psczFileA);
            ExitOnFailure(hr, "Failed to create path to file A");

            hr = PathConcat(*psczFileSettingsDir, L"SubDir", &sczSubDir);
            ExitOnFailure(hr, "Failed to get path to subdir");

            hr = DirEnsureExists(sczSubDir, NULL);
            ExitOnFailure(hr, "Failed to ensure subdirectory exists: %ls", sczSubDir);

            hr = PathConcat(*psczFileSettingsDir, L"SubDir\\FileB.del", psczFileB);
            ExitOnFailure(hr, "Failed to create path to file B");

        LExit:
            ReleaseStr(sczMyDocumentsDir);
            ReleaseStr(sczSubDir);

            return hr;
        }

        [Fact]
        void LegacySyncTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            LPWSTR sczSampleLegacyPath = NULL;
            HKEY hk = NULL;
            CFGDB_HANDLE cdhLocal = NULL;
            LPCWSTR wzRegKey = L"Software\\CfgRegTest";
            LPCWSTR wzString2CfgName = L"Main:\\String2\\\\With\\\\Backslashes";
            LPCWSTR wzString2RegValueName = L"String2\\With\\Backslashes";
            LPCWSTR wzDword1CfgName = L"Main:\\DwordValue1";
            LPCWSTR wzDword1RegValueName = L"DwordValue1";
            LPCWSTR wzDword2CfgName = L"Main:\\DwordValue2";
            LPCWSTR wzDword2RegValueName = L"DwordValue2";
            LPCWSTR wzQword1CfgName = L"Main:\\QwordValue1";
            LPCWSTR wzQword1RegValueName = L"QwordValue1";
            LPCWSTR wzFileNameA = L"File:\\FileA.tst";
            LPCWSTR wzFileNameB = L"File:\\SubDir\\FileB.del";
            LPWSTR sczFileSettingsDir = NULL;
            LPWSTR sczFileA = NULL;
            LPWSTR sczFileBDir = NULL;
            LPWSTR sczFileB = NULL;
            BYTE rgbFile1[1000] = { };
            rgbFile1[0] = 0x51;
            rgbFile1[50] = 0x52;
            BYTE rgbFile2[2000] = { };
            rgbFile2[10] = 0x41;
            rgbFile2[20] = 0x42;

            TestInitialize();

            // Delete the key before we start in case the last test failed
            hr = RegDelete(HKEY_CURRENT_USER, wzRegKey, REG_KEY_32BIT, TRUE);
            if (E_FILENOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to cleanup before main portion of test by deleting regkey:%ls", wzRegKey);

            hr = GetFilePaths(&sczFileSettingsDir, &sczFileA, &sczFileB);
            ExitOnFailure(hr, "Failed to get file paths for test");

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = PathExpand(&sczSampleLegacyPath, L"samplelegacy.udm", PATH_EXPAND_FULLPATH);
            ExitOnFailure(hr, "Failed to get full path to sample legacy XML file");

            hr = CfgLegacyImportProductFromXMLFile(cdhLocal, sczSampleLegacyPath);
            ExitOnFailure(hr, "Failed to load legacy product data from XML File");
            // Make sure the initial auto sync has started before proceeding
            ::Sleep(200);

            hr = CfgSetProduct(cdhLocal, L"CfgTest", L"1.0.0.0", L"0000000000000000");
            ExitOnFailure(hr, "Failed to set product");

            // Make sure the regkey exists
            hr = RegCreate(HKEY_CURRENT_USER, wzRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            ExitOnFailure(hr, "Failed to ensure registry key exists");

            hr = CfgSetString(cdhLocal, wzString2CfgName, L"SetValueFromCfg");
            ExitOnFailure(hr, "Failed to set string from cfg db");

            hr = CfgSetDword(cdhLocal, wzDword1CfgName, 1);
            ExitOnFailure(hr, "Failed to set dword 1 from cfg db");

            hr = CfgSetDword(cdhLocal, wzDword2CfgName, 2);
            ExitOnFailure(hr, "Failed to set dword 2 from cfg db");

            hr = DirEnsureExists(sczFileSettingsDir, NULL);
            ExitOnFailure(hr, "Failed to recreate directory");

            hr = FileWrite(sczFileA, 0, rgbFile1, sizeof(rgbFile1), NULL);
            ExitOnFailure(hr, "Failed to write file A with contents of array 1");

            hr = PathGetDirectory(sczFileB, &sczFileBDir);
            ExitOnFailure(hr, "Failed to get path of directory fileB will live in");

            hr = DirEnsureExists(sczFileBDir, NULL);
            ExitOnFailure(hr, "Failed to recreate directory");

            hr = FileWrite(sczFileB, 0, rgbFile2, sizeof(rgbFile2), NULL);
            ExitOnFailure(hr, "Failed to write file B with contents of array 2");

            WaitForAutoSync(cdhLocal);
            CheckCfgAndRegValueString(cdhLocal, hk, wzString2CfgName, wzString2RegValueName, L"SetValueFromCfg");
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword1CfgName, wzDword1RegValueName, 1);
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword2CfgName, wzDword2RegValueName, 2);
            CheckCfgAndFile(cdhLocal, wzFileNameA, sczFileA, rgbFile1, sizeof(rgbFile1));
            CheckCfgAndFile(cdhLocal, wzFileNameB, sczFileB, rgbFile2, sizeof(rgbFile2));

            hr = RegWriteString(hk, wzString2RegValueName, L"SetValueFromReg");
            ExitOnFailure(hr, "Failed to write registry string");

            hr = RegWriteNumber(hk, wzDword1RegValueName, 0);
            ExitOnFailure(hr, "Failed to write registry dword 1");

            hr = RegWriteNumber(hk, wzDword2RegValueName, 500);
            ExitOnFailure(hr, "Failed to write registry dword 2");

            hr = RegWriteQword(hk, wzQword1RegValueName, 5000000000);
            ExitOnFailure(hr, "Failed to write registry qword 1");

            hr = FileEnsureDelete(sczFileA);
            ExitOnFailure(hr, "Failed to delete file A");

            hr = FileEnsureDelete(sczFileB);
            ExitOnFailure(hr, "Failed to delete file B");

            WaitForAutoSync(cdhLocal);
            CheckCfgAndRegValueString(cdhLocal, hk, wzString2CfgName, wzString2RegValueName, L"SetValueFromReg");
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword1CfgName, wzDword1RegValueName, 0);
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword2CfgName, wzDword2RegValueName, 500);
            CheckCfgAndRegValueQword(cdhLocal, hk, wzQword1CfgName, wzQword1RegValueName, 5000000000);
            CheckCfgAndFileDeleted(cdhLocal, wzFileNameA, sczFileA);
            CheckCfgAndFileDeleted(cdhLocal, wzFileNameB, sczFileB);

            hr = RegWriteString(hk, wzString2RegValueName, L"NewValueFromReg");
            ExitOnFailure(hr, "Failed to write registry string");
           
            hr = RegWriteNumber(hk, wzDword1RegValueName, 5);
            ExitOnFailure(hr, "Failed to write registry dword 1");

            hr = RegWriteNumber(hk, wzDword2RegValueName, 1000);
            ExitOnFailure(hr, "Failed to write registry dword 2");

            hr = RegWriteQword(hk, wzQword1RegValueName, 0);
            ExitOnFailure(hr, "Failed to write registry qword 1");

            hr = CfgSetBlob(cdhLocal, wzFileNameA, rgbFile2, sizeof(rgbFile2));
            ExitOnFailure(hr, "Failed to set file A with contents of array 2");

            WaitForAutoSync(cdhLocal);
            CheckCfgAndRegValueString(cdhLocal, hk, wzString2CfgName, wzString2RegValueName, L"NewValueFromReg");
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword1CfgName, wzDword1RegValueName, 5);
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword2CfgName, wzDword2RegValueName, 1000);
            CheckCfgAndRegValueQword(cdhLocal, hk, wzQword1CfgName, wzQword1RegValueName, 0);
            CheckCfgAndFile(cdhLocal, wzFileNameA, sczFileA, rgbFile2, sizeof(rgbFile2));
            CheckCfgAndFileDeleted(cdhLocal, wzFileNameB, sczFileB);
            WaitForSqlCeTimestampChange();

            hr = CfgSetString(cdhLocal, wzString2CfgName, L"NewValueFromCfg");
            ExitOnFailure(hr, "Failed to set string from cfg db");

            hr = CfgSetDword(cdhLocal, wzDword1CfgName, 0);
            ExitOnFailure(hr, "Failed to set dword 1 from cfg db");

            hr = CfgSetDword(cdhLocal, wzDword2CfgName, 1);
            ExitOnFailure(hr, "Failed to set dword 2 from cfg db");

            hr = RegWriteQword(hk, wzQword1RegValueName, 100);
            ExitOnFailure(hr, "Failed to write registry qword 1");

            hr = CfgDeleteValue(cdhLocal, wzFileNameA);
            ExitOnFailure(hr, "Failed to delete file A from cfg db");

            WaitForAutoSync(cdhLocal);
            CheckCfgAndRegValueString(cdhLocal, hk, wzString2CfgName, wzString2RegValueName, L"NewValueFromCfg");
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword1CfgName, wzDword1RegValueName, 0);
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword2CfgName, wzDword2RegValueName, 1);
            CheckCfgAndRegValueQword(cdhLocal, hk, wzQword1CfgName, wzQword1RegValueName, 100);
            CheckCfgAndFileDeleted(cdhLocal, wzFileNameA, sczFileA);
            CheckCfgAndFileDeleted(cdhLocal, wzFileNameB, sczFileB);

            WaitForSqlCeTimestampChange();
            hr = CfgDeleteValue(cdhLocal, wzString2CfgName);
            ExitOnFailure(hr, "Failed to delete string value by cfg api");

            hr = CfgDeleteValue(cdhLocal, wzDword1CfgName);
            ExitOnFailure(hr, "Failed to delete dword 1 by cfg api");

            hr = CfgDeleteValue(cdhLocal, wzDword2CfgName);
            ExitOnFailure(hr, "Failed to delete dword 2 by cfg api");

            hr = CfgDeleteValue(cdhLocal, wzQword1CfgName);
            ExitOnFailure(hr, "Failed to delete qword 1 by cfg api");

            CheckCfgAndRegValueDeleted(cdhLocal, hk, wzString2CfgName, wzString2RegValueName);
            CheckCfgAndRegValueDeleted(cdhLocal, hk, wzDword1CfgName, wzDword1RegValueName);
            CheckCfgAndRegValueDeleted(cdhLocal, hk, wzDword2CfgName, wzDword2RegValueName);
            CheckCfgAndRegValueDeleted(cdhLocal, hk, wzQword1CfgName, wzQword1RegValueName);

            WaitForSqlCeTimestampChange();
            hr = CfgSetString(cdhLocal, wzString2CfgName, L"ResurrectedbyCfg");
            ExitOnFailure(hr, "Failed to set string from cfg db");

            hr = CfgSetDword(cdhLocal, wzDword1CfgName, 50);
            ExitOnFailure(hr, "Failed to set dword 1 from cfg db");

            hr = CfgSetQword(cdhLocal, wzQword1CfgName, 80);
            ExitOnFailure(hr, "Failed to set qword 1 from cfg db");

            hr = CfgSetDword(cdhLocal, wzDword2CfgName, 0);
            ExitOnFailure(hr, "Failed to set dword 2 from cfg db");

            hr = RegOpen(HKEY_CURRENT_USER, wzRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            ExitOnFailure(hr, "Failed to open registry key: %ls", wzRegKey);
            CheckCfgAndRegValueString(cdhLocal, hk, wzString2CfgName, wzString2RegValueName, L"ResurrectedbyCfg");
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword1CfgName, wzDword1RegValueName, 50);
            CheckCfgAndRegValueDword(cdhLocal, hk, wzDword2CfgName, wzDword2RegValueName, 0);
            CheckCfgAndRegValueQword(cdhLocal, hk, wzQword1CfgName, wzQword1RegValueName, 80);

            WaitForSqlCeTimestampChange();
            hr = RegWriteString(hk, wzString2RegValueName, NULL);
            ExitOnFailure(hr, "Failed to delete string value by registry");

            hr = RegWriteString(hk, wzDword1RegValueName, NULL);
            ExitOnFailure(hr, "Failed to delete dword 1 value by registry");

            hr = RegWriteString(hk, wzDword2RegValueName, NULL);
            ExitOnFailure(hr, "Failed to delete dword 2 value by registry");

            hr = RegWriteString(hk, wzQword1RegValueName, NULL);
            ExitOnFailure(hr, "Failed to delete qword 1 value by registry");

            WaitForAutoSync(cdhLocal);
            CheckCfgAndRegValueDeleted(cdhLocal, hk, wzString2CfgName, wzString2RegValueName);
            CheckCfgAndRegValueDeleted(cdhLocal, hk, wzDword1CfgName, wzDword1RegValueName);
            CheckCfgAndRegValueDeleted(cdhLocal, hk, wzDword2CfgName, wzDword2RegValueName);
            CheckCfgAndRegValueDeleted(cdhLocal, hk, wzQword1CfgName, wzQword1RegValueName);
            ReleaseRegKey(hk);

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            // Only cleanup the key if the test succeeded
            hr = RegDelete(HKEY_CURRENT_USER, wzRegKey, REG_KEY_32BIT, TRUE);
            if (E_FILENOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to cleanup after test by deleting key: %ls", wzRegKey);
            }

        LExit:
            ReleaseRegKey(hk);
            ReleaseStr(sczValue);
            ReleaseStr(sczFileSettingsDir);
            ReleaseStr(sczFileBDir);
            ReleaseStr(sczFileB);
            ReleaseStr(sczSampleLegacyPath);

            TestUninitialize();
        }
    };
}
