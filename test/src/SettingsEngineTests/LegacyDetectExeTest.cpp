//-------------------------------------------------------------------------------------------------
// <copyright file="LegacyDetectDirectoryTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Test detecting a directory via file extension associations and syncing data to/from it via legacy manifest.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class LegacyDetectExe : public CfgTest
    {
    public:
        [Fact]
        void LegacyDetectExeTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczLegacySpecialsPath = NULL;
            LPWSTR sczDummyExePath = NULL;
            LPWSTR sczDummyIniPath = NULL;
            CFGDB_HANDLE cdhLocal = NULL;
            BYTE rgbFileA[1010] = { };
            rgbFileA[0] = 0xAA;
            rgbFileA[50] = 0xBB;

            TestInitialize();
            
            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");
            
            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = PathExpand(&sczLegacySpecialsPath, L"DetectExe.udm", PATH_EXPAND_FULLPATH);
            ExitOnFailure(hr, "Failed to get full path to detect exe legacy XML file");

            hr = PathExpand(&sczDummyExePath, L"%TEMP%\\Test.exe", PATH_EXPAND_ENVIRONMENT);
            ExitOnFailure(hr, "Failed to get full path to dummy exe");

            hr = PathExpand(&sczDummyIniPath, L"%TEMP%\\Test.ini", PATH_EXPAND_ENVIRONMENT);
            ExitOnFailure(hr, "Failed to get full path to dummy ini");

            hr = FileWrite(sczDummyExePath, 0, NULL, 0, NULL);
            ExitOnFailure(hr, "Failed to write blank dummy EXE to build output directory");

            hr = FileWrite(sczDummyIniPath, 0, rgbFileA, sizeof(rgbFileA), NULL);
            ExitOnFailure(hr, "Failed to write blank dummy EXE to build output directory");

            hr = CfgLegacyImportProductFromXMLFile(cdhLocal, sczLegacySpecialsPath);
            ExitOnFailure(hr, "Failed to load legacy product data from XML File");
            // Make sure the initial auto sync has started before proceeding
            ::Sleep(200);

            hr = CfgSetProduct(cdhLocal, L"CfgTestDetectExe", L"1.0.0.0", L"0000000000000000");
            ExitOnFailure(hr, "Failed to set product");

            ExpectProductUnregistered(cdhLocal, L"CfgTestDetectExe", L"1.0.0.0", L"0000000000000000");
            ExpectNoFile(cdhLocal, L"IniFile");

            ExpectProductUnregistered(cdhLocal, L"CfgTestDetectExe", L"1.0.0.0", L"0000000000000000");
            ExpectNoFile(cdhLocal, L"IniFile");

            SetApplication(L"Test.exe", sczDummyExePath);

            WaitForAutoSync(cdhLocal);
            ExpectProductRegistered(cdhLocal, L"CfgTestDetectExe", L"1.0.0.0", L"0000000000000000");
            ExpectFile(cdhLocal, L"IniFile", rgbFileA, sizeof(rgbFileA));

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

        LExit:
            ReleaseStr(sczLegacySpecialsPath);
            ReleaseStr(sczDummyExePath);
            ReleaseStr(sczDummyIniPath);
            TestUninitialize();
        }
    };
}
