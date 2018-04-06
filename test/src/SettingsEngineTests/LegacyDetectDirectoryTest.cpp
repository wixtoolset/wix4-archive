// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class LegacyDetectDirectory : public CfgTest
    {
    public:
        void GetPaths(LPWSTR *psczPathA, LPWSTR *psczPathB, LPWSTR *psczPathEmpty)
        {
            HRESULT hr = S_OK;

            hr = PathExpand(psczPathA, L"%TEMP%\\PathA\\", PATH_EXPAND_ENVIRONMENT);
            ExitOnFailure(hr, "Failed to expand path A");

            hr = PathExpand(psczPathB, L"%TEMP%\\PathB\\", PATH_EXPAND_ENVIRONMENT);
            ExitOnFailure(hr, "Failed to expand path B");

            hr = PathExpand(psczPathEmpty, L"%TEMP%\\PathEmpty\\", PATH_EXPAND_ENVIRONMENT);
            ExitOnFailure(hr, "Failed to expand path Empty");

            hr = DirEnsureDelete(*psczPathA, TRUE, TRUE);
            if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to ensure old path A is deleted");

            hr = DirEnsureExists(*psczPathA, NULL);
            ExitOnFailure(hr, "Failed to ensure path A exists");

            hr = DirEnsureDelete(*psczPathB, TRUE, TRUE);
            if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to ensure old path B is deleted");

            hr = DirEnsureExists(*psczPathB, NULL);
            ExitOnFailure(hr, "Failed to ensure path B exists");

            hr = DirEnsureDelete(*psczPathEmpty, TRUE, TRUE);
            if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to ensure old path Empty is deleted");

            hr = DirEnsureExists(*psczPathEmpty, NULL);
            ExitOnFailure(hr, "Failed to ensure path Empty exists");

        LExit:
            return;
        }

        [Fact]
        void LegacyDetectDirectoryTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczLegacySpecialsPath = NULL;
            LPWSTR sczPathA = NULL;
            LPWSTR sczPathB = NULL;
            LPWSTR sczPathEmpty = NULL;
            LPWSTR sczPathASubDir = NULL;
            LPWSTR sczPathBSubDir = NULL;
            LPWSTR sczPathEmptySubDir = NULL;
            LPWSTR sczFileA1 = NULL;
            LPWSTR sczFileAIni = NULL;
            LPWSTR sczFileB1 = NULL;
            LPWSTR sczFileB2 = NULL;
            LPWSTR sczUninstallFileA = NULL;
            LPWSTR sczIndividualFile = NULL;
            LPWSTR sczIndividualFileFake = NULL;
            LPWSTR sczIndividualFileIgnored = NULL;
            INI_HANDLE iniHandle = NULL;
            CFGDB_HANDLE cdhLocal = NULL;
            BYTE rgbFileA1[1000] = { };
            rgbFileA1[0] = 0x99;
            rgbFileA1[50] = 0x88;
            BYTE rgbFileB1[2000] = { };
            rgbFileB1[10] = 0x77;
            rgbFileB1[20] = 0x66;
            BYTE rgbFileB1v2[55] = { };
            rgbFileB1v2[0] = 0x11;
            rgbFileB1v2[40] = 0x22;
            BYTE rgbFileB2[3000] = { };
            rgbFileB2[10] = 0x55;
            rgbFileB2[20] = 0x44;
            LPCWSTR wzAIni = L"NonSectionedValue=Foo\r\n[Section1]\r\nSectionedValue1=Bar=With=Equals=In=Value\r\nName=WithEqualsSign=Value=WithStuffAfterIt\r\n[Section2]\r\nSectionedValue2=Cha";

            TestInitialize();
            
            GetPaths(&sczPathA, &sczPathB, &sczPathEmpty);

            hr = PathConcat(sczPathA, L"SubDir", &sczPathASubDir);
            ExitOnFailure(hr, "Failed to get subdir under Path A");

            hr = DirEnsureExists(sczPathASubDir, NULL);
            ExitOnFailure(hr, "Failed to create subdir under A");

            hr = PathConcat(sczPathB, L"SubDir", &sczPathBSubDir);
            ExitOnFailure(hr, "Failed to get subdir under Path B");

            hr = DirEnsureExists(sczPathBSubDir, NULL);
            ExitOnFailure(hr, "Failed to create subdir under B");

            hr = PathConcat(sczPathEmpty, L"SubDir", &sczPathEmptySubDir);
            ExitOnFailure(hr, "Failed to get subdir under Path Empty");

            hr = DirEnsureExists(sczPathEmptySubDir, NULL);
            ExitOnFailure(hr, "Failed to create subdir under Empty");

            hr = PathConcat(sczPathASubDir, L"1.bin", &sczFileA1);
            ExitOnFailure(hr, "Failed to get path to file A1");

            hr = PathConcat(sczPathASubDir, L"data.ini", &sczFileAIni);
            ExitOnFailure(hr, "Failed to get path to file data.ini under A");

            hr = PathConcat(sczPathBSubDir, L"1.bin", &sczFileB1);
            ExitOnFailure(hr, "Failed to get path to file B1");

            hr = PathConcat(sczPathBSubDir, L"2.bin", &sczFileB2);
            ExitOnFailure(hr, "Failed to get path to file B2");

            hr = PathConcat(sczPathA, L"Uninstall.exe", &sczUninstallFileA);
            ExitOnFailure(hr, "Failed to get path to uninstall.exe");

            hr = PathConcat(sczPathA, L"File.txt", &sczIndividualFile);
            ExitOnFailure(hr, "Failed to get path to individual file at path A");

            hr = PathConcat(sczPathA, L"Fake.txt", &sczIndividualFileFake);
            ExitOnFailure(hr, "Failed to get path to individual fake file at path A");

            hr = PathConcat(sczPathASubDir, L"Ignored.txt", &sczIndividualFileIgnored);
            ExitOnFailure(hr, "Failed to get path to individual ignored file at path A");

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");
            
            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = PathExpand(&sczLegacySpecialsPath, L"detectdirectory.udm", PATH_EXPAND_FULLPATH);
            ExitOnFailure(hr, "Failed to get full path to detect directory legacy XML file");

            // A is the directory it points to now
            SetARP(L"IncorrectKeyName", L"Random productname", sczPathEmpty, NULL);
            SetARP(L"RandomKeyName", L"Cfg Test Displayname", sczPathA, NULL);
            SetARP(L"OtherIncorrectKeyName", L"Cfg Test Displayname B", sczPathB, NULL);

            hr = CfgLegacyImportProductFromXMLFile(cdhLocal, sczLegacySpecialsPath);
            ExitOnFailure(hr, "Failed to load legacy product data from XML File");

            WaitForAutoSync(cdhLocal);
            ExpectProductRegistered(cdhLocal, L"CfgTestDetectDirectory", L"1.0.0.0", L"0000000000000000");
            ExpectNoFile(cdhLocal, L"File:\\1.bin");
            ExpectNoFile(cdhLocal, L"File:\\2.bin");
            ExpectNoFile(cdhLocal, L"IndividualFile");
            ExpectNoFile(cdhLocal, L"File:\\Ignored.txt");

            hr = FileWrite(sczFileA1, 0, rgbFileA1, sizeof(rgbFileA1), NULL);
            ExitOnFailure(hr, "Failed to write file A1");

            WaitForSqlCeTimestampChange();
            hr = FileFromString(sczFileAIni, 0, wzAIni, FILE_ENCODING_UTF16_WITH_BOM);
            ExitOnFailure(hr, "Failed to write file data.ini to A directory");

            WaitForSqlCeTimestampChange();
            hr = FileWrite(sczFileB1, 0, rgbFileB1, sizeof(rgbFileB1), NULL);
            ExitOnFailure(hr, "Failed to write file B1");

            WaitForSqlCeTimestampChange();
            hr = FileWrite(sczFileB2, 0, rgbFileB2, sizeof(rgbFileB2), NULL);
            ExitOnFailure(hr, "Failed to write file B2");

            WaitForSqlCeTimestampChange();
            hr = FileWrite(sczIndividualFile, 0, rgbFileA1, sizeof(rgbFileA1), NULL);
            ExitOnFailure(hr, "Failed to write file Individual File");

            WaitForSqlCeTimestampChange();
            hr = FileWrite(sczIndividualFileFake, 0, rgbFileB1, sizeof(rgbFileB1), NULL);
            ExitOnFailure(hr, "Failed to write file Individual File Fake");

            WaitForSqlCeTimestampChange();
            hr = FileWrite(sczIndividualFileIgnored, 0, rgbFileB1, sizeof(rgbFileB1), NULL);
            ExitOnFailure(hr, "Failed to write file Individual File Ignored");

            WaitForAutoSync(cdhLocal);
            ExpectProductRegistered(cdhLocal, L"CfgTestDetectDirectory", L"1.0.0.0", L"0000000000000000");
            ExpectFile(cdhLocal, L"File:\\1.bin", rgbFileA1, sizeof(rgbFileA1));
            ExpectNoFile(cdhLocal, L"File:\\2.bin");
            ExpectFile(cdhLocal, L"IndividualFile", rgbFileA1, sizeof(rgbFileA1));
            ExpectNoFile(cdhLocal, L"File:\\Ignored.txt");
            ExpectString(cdhLocal, L"Ini:\\NonSectionedValue", L"Foo");
            ExpectString(cdhLocal, L"Ini:\\Section1\\SectionedValue1", L"Bar=With=Equals=In=Value");
            ExpectString(cdhLocal, L"Ini:\\Section1\\Name=WithEqualsSign", L"Value=WithStuffAfterIt");
            ExpectString(cdhLocal, L"Ini:\\Section2\\SectionedValue2", L"Cha");

            // Modify the INI and see if the strings changed, and nothing else did
            hr = IniInitialize(&iniHandle);
            ExitOnFailure(hr, "Failed to initialize INI object");

            hr = IniSetOpenTag(iniHandle, L"[", L"]");
            ExitOnFailure(hr, "Failed to set open tag settings on ini handle");

            hr = IniSetValueStyle(iniHandle, NULL, L"=");
            ExitOnFailure(hr, "Failed to set value separator setting on ini handle");

            hr = IniSetCommentStyle(iniHandle, L";");
            ExitOnFailure(hr, "Failed to set comment style setting on ini handle");

            hr = IniParse(iniHandle, sczFileAIni, NULL);
            ExitOnFailure(hr, "Failed to parse INI file");

            hr = IniSetValue(iniHandle, L"NonSectionedValue", L"Modified1");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"IgnoreMe\\Blah", L"Cha");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"IgnoreMe\\Cha", L"Blah");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"Ignatious", L"Foo1");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"blared", L"Foo2");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"Ignored", L"Ignored");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"Ignared", L"Ignared");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"Section1\\SectionedValue1", NULL);
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"Section2\\SectionedValue2", L"Modified2");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniSetValue(iniHandle, L"Section2\\SectionedValue3", L"New");
            ExitOnFailure(hr, "Failed to set value");

            hr = IniWriteFile(iniHandle, NULL, FILE_ENCODING_UNSPECIFIED);
            ReleaseNullIni(iniHandle);

            WaitForAutoSync(cdhLocal);
            ExpectProductRegistered(cdhLocal, L"CfgTestDetectDirectory", L"1.0.0.0", L"0000000000000000");
            ExpectFile(cdhLocal, L"File:\\1.bin", rgbFileA1, sizeof(rgbFileA1));
            ExpectNoFile(cdhLocal, L"File:\\2.bin");
            ExpectFile(cdhLocal, L"IndividualFile", rgbFileA1, sizeof(rgbFileA1));
            ExpectNoFile(cdhLocal, L"File:\\Ignored.txt");
            ExpectString(cdhLocal, L"Ini:\\NonSectionedValue", L"Modified1");
            ExpectNoValue(cdhLocal, L"Ini:\\Section1\\SectionedValue1");
            ExpectNoValue(cdhLocal, L"Ini:\\IgnoreMe\\Blah");
            ExpectNoValue(cdhLocal, L"Ini:\\IgnoreMe\\Cha");
            ExpectString(cdhLocal, L"Ini:\\Ignatious", L"Foo1");
            ExpectString(cdhLocal, L"Ini:\\blared", L"Foo2");
            ExpectNoValue(cdhLocal, L"Ini:\\Ignored");
            ExpectString(cdhLocal, L"Ini:\\Ignared", L"Ignared");
            ExpectString(cdhLocal, L"Ini:\\Section2\\SectionedValue2", L"Modified2");
            ExpectString(cdhLocal, L"Ini:\\Section2\\SectionedValue3", L"New");

            // Modify INI file via cfg values, sync, and check the INI for the right changes
            hr = CfgSetString(cdhLocal, L"Ini:\\NewSection\\ValueFromCfg", L"Yes");
            ExitOnFailure(hr, "Failed to set value");

            hr = CfgDeleteValue(cdhLocal, L"Ini:\\Section2\\SectionedValue3");
            ExitOnFailure(hr, "Failed to set value");

            WaitForAutoSync(cdhLocal);

            hr = IniInitialize(&iniHandle);
            ExitOnFailure(hr, "Failed to initialize INI object");

            hr = IniSetOpenTag(iniHandle, L"[", L"]");
            ExitOnFailure(hr, "Failed to set open tag settings on ini handle");

            hr = IniSetValueStyle(iniHandle, NULL, L"=");
            ExitOnFailure(hr, "Failed to set value separator setting on ini handle");

            hr = IniSetCommentStyle(iniHandle, L";");
            ExitOnFailure(hr, "Failed to set comment style setting on ini handle");

            hr = IniParse(iniHandle, sczFileAIni, NULL);
            ExitOnFailure(hr, "Failed to parse INI file");

            ExpectIniValue(iniHandle, L"NewSection\\ValueFromCfg", L"Yes");
            ExpectNoIniValue(iniHandle, L"Section2\\SectionedValue3");
            ExpectIniValue(iniHandle, L"NonSectionedValue", L"Modified1");
            ExpectNoIniValue(iniHandle, L"Section1\\SectionedValue1");
            ExpectIniValue(iniHandle, L"Section2\\SectionedValue2", L"Modified2");

            // Empty is the directory it points to now
            SetARP(L"IncorrectKeyName", L"   Cfg Test Displayname    ", sczPathEmpty, NULL);
            SetARP(L"RandomKeyName", L"Cfg Test Displayname A", sczPathA, NULL);
            SetARP(L"OtherIncorrectKeyName", L"Cfg Test Displayname B", sczPathB, NULL);

            WaitForAutoSync(cdhLocal);
            ExpectProductRegistered(cdhLocal, L"CfgTestDetectDirectory", L"1.0.0.0", L"0000000000000000");
            ExpectNoFile(cdhLocal, L"File:\\1.bin");
            ExpectNoFile(cdhLocal, L"File:\\2.bin");
            ExpectNoFile(cdhLocal, L"IndividualFile");
            ExpectNoFile(cdhLocal, L"File:\\Ignored.txt");

            // B is the directory it points to now
            SetARP(L"IncorrectKeyName", L"Random productname", sczPathEmpty, NULL);
            SetARP(L"RandomKeyName", L"Cfg Test Displayname A", sczPathA, NULL);
            SetARP(L"OtherIncorrectKeyName", L"   Cfg Test Displayname    ", sczPathB, NULL);

            WaitForAutoSync(cdhLocal);
            ExpectProductRegistered(cdhLocal, L"CfgTestDetectDirectory", L"1.0.0.0", L"0000000000000000");
            ExpectFile(cdhLocal, L"File:\\1.bin", rgbFileB1, sizeof(rgbFileB1));
            ExpectFile(cdhLocal, L"File:\\2.bin", rgbFileB2, sizeof(rgbFileB2));
            ExpectNoFile(cdhLocal, L"IndividualFile");
            ExpectNoFile(cdhLocal, L"File:\\Ignored.txt");
            ExpectNoValue(cdhLocal, L"Ini:\\NonSectionedValue");
            ExpectNoValue(cdhLocal, L"Ini:\\Section1\\SectionedValue1");
            ExpectNoValue(cdhLocal, L"Ini:\\Section2\\SectionedValue2");

            // Now it doesn't point anywhere
            SetARP(L"IncorrectKeyName", L"Cfg Test Displayname Empty", sczPathEmpty, NULL);
            SetARP(L"RandomKeyName", L"Cfg Test Displayname A", sczPathA, NULL);
            SetARP(L"OtherIncorrectKeyName", L"Cfg Test Displayname B", sczPathB, NULL);

            WaitForSqlCeTimestampChange();
            hr = FileWrite(sczFileB1, 0, rgbFileB1v2, sizeof(rgbFileB1v2), NULL);
            ExitOnFailure(hr, "Failed to write file B1v2");

            WaitForAutoSync(cdhLocal);
            ExpectProductUnregistered(cdhLocal, L"CfgTestDetectDirectory", L"1.0.0.0", L"0000000000000000");
            // We should still find the files due to caching of the previous ARP location
            ExpectFile(cdhLocal, L"File:\\1.bin", rgbFileB1v2, sizeof(rgbFileB1v2));
            ExpectFile(cdhLocal, L"File:\\2.bin", rgbFileB2, sizeof(rgbFileB2));
            ExpectNoFile(cdhLocal, L"IndividualFile");
            ExpectNoFile(cdhLocal, L"File:\\Ignored.txt");
            ExpectNoValue(cdhLocal, L"Ini:\\NonSectionedValue");
            ExpectNoValue(cdhLocal, L"Ini:\\Section1\\SectionedValue1");
            ExpectNoValue(cdhLocal, L"Ini:\\Section2\\SectionedValue2");

            // Now test we can get directories from the uninstallstring as well
            SetARP(L"IncorrectKeyName", L"Cfg Test Displayname None", NULL, NULL);
            SetARP(L"RandomKeyName", L"Cfg Test Displayname", NULL, sczUninstallFileA);
            SetARP(L"OtherIncorrectKeyName", L"Cfg Test Displayname B", NULL, NULL);

            WaitForAutoSync(cdhLocal);
            ExpectProductRegistered(cdhLocal, L"CfgTestDetectDirectory", L"1.0.0.0", L"0000000000000000");
            // On freshly registered products, we don't pull deletes, instead we write the files from cfg db out
            ExpectFile(cdhLocal, L"File:\\1.bin", rgbFileB1v2, sizeof(rgbFileB1v2));
            ExpectFile(cdhLocal, L"File:\\2.bin", rgbFileB2, sizeof(rgbFileB2));
            ExpectFile(cdhLocal, L"UninstallFile:\\1.bin", rgbFileA1, sizeof(rgbFileA1));
            ExpectNoFile(cdhLocal, L"IndividualFile");
            ExpectNoFile(cdhLocal, L"File:\\Ignored.txt");
            ExpectNoValue(cdhLocal, L"Ini:\\NonSectionedValue");
            ExpectNoValue(cdhLocal, L"Ini:\\Section1\\SectionedValue1");
            ExpectNoValue(cdhLocal, L"Ini:\\Section2\\SectionedValue2");

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

        LExit:
            ReleaseIni(iniHandle);
            ReleaseStr(sczLegacySpecialsPath);
            ReleaseStr(sczPathA);
            ReleaseStr(sczPathB);
            ReleaseStr(sczPathEmpty);
            ReleaseStr(sczPathASubDir);
            ReleaseStr(sczPathBSubDir);
            ReleaseStr(sczPathEmptySubDir);
            ReleaseStr(sczFileA1);
            ReleaseStr(sczFileAIni);
            ReleaseStr(sczFileB1);
            ReleaseStr(sczFileB2);
            ReleaseStr(sczIndividualFile);
            ReleaseStr(sczIndividualFileFake);
            ReleaseStr(sczIndividualFileIgnored);
            TestUninitialize();
        }
    };
}
