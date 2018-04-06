// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class LegacyRegistrySpecials : public CfgTest
    {
    public:
        [Fact]
        void LegacyRegistrySpecialsTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            LPWSTR sczLegacySpecialsPath = NULL;
            HKEY hk = NULL;
            CFGDB_HANDLE cdhLocal = NULL;
            BYTE bByte;
            LPCWSTR wzRegKey = L"Software\\CfgRegTestSpecials";

            TestInitialize();

            // Delete the key before we start in case the last test failed
            hr = RegDelete(HKEY_CURRENT_USER, wzRegKey, REG_KEY_32BIT, TRUE);
            if (E_FILENOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to cleanup before main portion of test by deleting regkey:%ls", wzRegKey);

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = PathExpand(&sczLegacySpecialsPath, L"legacyspecialstest.udm", PATH_EXPAND_FULLPATH);
            ExitOnFailure(hr, "Failed to get full path to sample legacy XML file");

            hr = CfgLegacyImportProductFromXMLFile(cdhLocal, sczLegacySpecialsPath);
            ExitOnFailure(hr, "Failed to load legacy product data from XML File");

            hr = CfgSetProduct(cdhLocal, L"CfgTestSpecials", L"1.0.0.0", L"0000000000000000");
            ExitOnFailure(hr, "Failed to set product");

            // Register the product so we get normal syncing behavior
            SetARP(L"SomeKeyName", L"Cfg Test Specials", NULL, NULL);

            // Make sure the regkey exists
            hr = RegCreate(HKEY_CURRENT_USER, wzRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            ExitOnFailure(hr, "Failed to ensure registry key exists");

            bByte = 0xFF;
            hr = RegWriteBinary(hk, L"FlagsValue", &bByte, 1);
            ExitOnFailure(hr, "Failed to write flagsvalue binary");

            hr = RegWriteNumber(hk, L"BoolValue", 1);
            ExitOnFailure(hr, "Failed to write boolvalue");

            hr = RegWriteNumber(hk, L"IgnoreMe", 100);
            ExitOnFailure(hr, "Failed to write ignoreme value");

            WaitForAutoSync(cdhLocal);
            ExpectNoValue(cdhLocal, L"Main:\\IgnoreMe");
            CheckCfgAndRegValueDword(cdhLocal, hk, L"Main:\\BoolValue", L"BoolValue", 1);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag00", L"FlagsValue", TRUE, 0);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag01", L"FlagsValue", TRUE, 1);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag02", L"FlagsValue", TRUE, 2);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag03", L"FlagsValue", TRUE, 3);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag04", L"FlagsValue", TRUE, 4);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag05", L"FlagsValue", TRUE, 5);

            hr = CfgSetBool(cdhLocal, L"Main:Flag03", FALSE);
            ExitOnFailure(hr, "Failed to set Flag03 to FALSE");

            hr = CfgSetBool(cdhLocal, L"Main:Flag04", TRUE);
            ExitOnFailure(hr, "Failed to set Flag04 to TRUE");

            hr = CfgSetBool(cdhLocal, L"Main:Flag00", FALSE);
            ExitOnFailure(hr, "Failed to set Flag00 to FALSE");

            hr = RegWriteNumber(hk, L"BoolValue", 300);
            ExitOnFailure(hr, "Failed to write BoolValue");

            hr = RegWriteString(hk, L"IgnoreMe", L"Blah");
            ExitOnFailure(hr, "Failed to write ignoreme value");

            WaitForAutoSync(cdhLocal);
            ExpectNoValue(cdhLocal, L"Main:\\IgnoreMe");
            CheckCfgAndRegValueDword(cdhLocal, hk, L"Main:\\BoolValue", L"BoolValue", 300);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag00", L"FlagsValue", FALSE, 0);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag01", L"FlagsValue", TRUE, 1);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag02", L"FlagsValue", TRUE, 2);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag03", L"FlagsValue", FALSE, 3);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag04", L"FlagsValue", TRUE, 4);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag05", L"FlagsValue", TRUE, 5);

            bByte = 0x01;
            hr = RegWriteBinary(hk, L"FlagsValue", &bByte, 1);
            ExitOnFailure(hr, "Failed to write flagsvalue binary");

            hr = RegWriteNumber(hk, L"BoolValue", 0);
            ExitOnFailure(hr, "Failed to write BoolValue");

            hr = RegWriteString(hk, L"IgnoreMe", NULL);
            ExitOnFailure(hr, "Failed to write ignoreme value");

            WaitForAutoSync(cdhLocal);
            ExpectNoValue(cdhLocal, L"Main:\\IgnoreMe");
            CheckCfgAndRegValueDword(cdhLocal, hk, L"Main:\\BoolValue", L"BoolValue", 0);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag00", L"FlagsValue", TRUE, 0);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag01", L"FlagsValue", FALSE, 1);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag02", L"FlagsValue", FALSE, 2);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag03", L"FlagsValue", FALSE, 3);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag04", L"FlagsValue", FALSE, 4);
            CheckCfgAndRegValueFlag(cdhLocal, hk, L"Main:Flag05", L"FlagsValue", FALSE, 5);

            // Product won't be registered, so the deletes shouldn't have any effect
            SetARP(L"SomeKeyName", L"Wrong Name", NULL, NULL);
            WaitForAutoSync(cdhLocal);

            hr = RegWriteString(hk, L"FlagsValue", NULL);
            ExitOnFailure(hr, "Failed to delete flagsvalue binary");

            hr = RegWriteString(hk, L"BoolValue", NULL);
            ExitOnFailure(hr, "Failed to delete BoolValue");

            hr = RegWriteBinary(hk, L"IgnoreMe", &bByte, 1);
            ExitOnFailure(hr, "Failed to write ignoreme value");

            WaitForAutoSync(cdhLocal);
            ExpectNoValue(cdhLocal, L"Main:\\IgnoreMe");
            ExpectDword(cdhLocal, L"Main:\\BoolValue", 0);
            ExpectBool(cdhLocal, L"Main:Flag00", TRUE);
            ExpectBool(cdhLocal, L"Main:Flag01", FALSE);
            ExpectBool(cdhLocal, L"Main:Flag02", FALSE);
            ExpectBool(cdhLocal, L"Main:Flag03", FALSE);
            ExpectBool(cdhLocal, L"Main:Flag04", FALSE);
            ExpectBool(cdhLocal, L"Main:Flag05", FALSE);

            // OK now register the product, sync again, and confirm the data is pushed back out to disk due to fresh re-registration
            SetARP(L"SomeKeyName", L"Cfg Test Specials", NULL, NULL);
            WaitForAutoSync(cdhLocal);
            ExpectNoValue(cdhLocal, L"Main:\\IgnoreMe");
            ExpectDword(cdhLocal, L"Main:\\BoolValue", 0);
            ExpectBool(cdhLocal, L"Main:Flag00", TRUE);
            ExpectBool(cdhLocal, L"Main:Flag01", FALSE);
            ExpectBool(cdhLocal, L"Main:Flag02", FALSE);
            ExpectBool(cdhLocal, L"Main:Flag03", FALSE);
            ExpectBool(cdhLocal, L"Main:Flag04", FALSE);
            ExpectBool(cdhLocal, L"Main:Flag05", FALSE);

            // Verify that for an ignored value, writing to it from cfg db doesn't affect the registry, and in fact will delete it from the cfg db on sync,
            // even if the value doesn't exist in the registry
            hr = RegWriteString(hk, L"IgnoreMe", NULL);
            ExitOnFailure(hr, "Failed to write ignoreme value");

            hr = CfgSetString(cdhLocal, L"Main:\\IgnoreMe", L"FromCfg");
            ExitOnFailure(hr, "Failed to set ignoreme string in cfg db");

            WaitForAutoSync(cdhLocal);
            ExpectNoValue(cdhLocal, L"Main:\\IgnoreMe");

            // Verify that deleting it in the CFG db doesn't delete it in the registry
            ReleaseRegKey(hk);
            hr = RegCreate(HKEY_CURRENT_USER, wzRegKey, KEY_SET_VALUE | KEY_QUERY_VALUE | KEY_WOW64_32KEY, &hk);
            ExitOnFailure(hr, "Failed to ensure registry key exists");

            hr = RegWriteString(hk, L"IgnoreMe", L"Blah");
            ExitOnFailure(hr, "Failed to write ignoreme value 2nd time");

            WaitForSqlCeTimestampChange();
            hr = CfgSetString(cdhLocal, L"Main:\\IgnoreMe", L"FromCfg");
            ExitOnFailure(hr, "Failed to set ignoreme string in cfg db 2nd time");

            WaitForSqlCeTimestampChange();
            hr = CfgDeleteValue(cdhLocal, L"Main:\\IgnoreMe");
            ExitOnFailure(hr, "Failed to delete value in cfg db");

            WaitForAutoSync(cdhLocal);
            ExpectDword(cdhLocal, L"Main:\\BoolValue", 0);

            // Should still be in the registry
            hr = RegReadString(hk, L"IgnoreMe", &sczValue);
            ExitOnFailure(hr, "Failed to read ignoreme from registry");

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            // Only cleanup the key if the test succeeded
            hr = RegDelete(HKEY_CURRENT_USER, wzRegKey, REG_KEY_32BIT, TRUE);
            ExitOnFailure(hr, "Failed to cleanup after test by deleting key: %ls", wzRegKey);

LExit:
            ReleaseRegKey(hk);
            ReleaseStr(sczValue);
            ReleaseStr(sczLegacySpecialsPath);

            TestUninitialize();
        }
    };
}
