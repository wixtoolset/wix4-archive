// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace Microsoft::Win32;

namespace CfgTests
{
    public ref class ReadWrite : public CfgTest
    {
    public:
        [Fact]
        void ReadWriteTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            DWORD dwValue = 0;
            DWORD64 qwValue = 0;
            BOOL fValue = FALSE;
            CFGDB_HANDLE cdhLocal = NULL;

            TestInitialize();

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = CfgSetProduct(cdhLocal, L"Test", L"1.0.0.0", L"abcdabcdabcdabcd");
            ExitOnFailure(hr, "Failed to set product");

            BYTE rgbFile[1000] = { };
            rgbFile[0] = 0x37;
            rgbFile[50] = 0xFF;
            hr = CfgSetBlob(cdhLocal, L"File1", rgbFile, sizeof(rgbFile));
            ExitOnFailure(hr, "Failed to add file File1");

            hr = CfgSetBlob(cdhLocal, L"SameAsFile1", rgbFile, sizeof(rgbFile));
            ExitOnFailure(hr, "Failed to add file File1");

            BYTE *pBuffer = NULL;
            ExpectFile(cdhLocal, L"File1", rgbFile, sizeof(rgbFile));

            WaitForSqlCeTimestampChange();
            hr = CfgDeleteValue(cdhLocal, L"File1");
            ExitOnFailure(hr, "Failed to delete file File1");

            ExpectNoFile(cdhLocal, L"File1");

            hr = CfgSetString(cdhLocal, L"Test2", L"Value2");
            ExitOnFailure(hr, "Failed to set string");

            WaitForSqlCeTimestampChange();
            hr = CfgSetString(cdhLocal, L"Test2", L"Value2Changed");
            ExitOnFailure(hr, "Failed to set string");

            WaitForSqlCeTimestampChange();
            hr = CfgSetString(cdhLocal, L"Test2", L"Value2ChangedBack");
            ExitOnFailure(hr, "Failed to set string");

            WaitForSqlCeTimestampChange();
            hr = CfgDeleteValue(cdhLocal, L"Test2");
            ExitOnFailure(hr, "Failed to delete test2 value");

            WaitForSqlCeTimestampChange();
            hr = CfgSetString(cdhLocal, L"Test2", L"ResurrectedValue");
            ExitOnFailure(hr, "Failed to set string");

            hr = CfgSetString(cdhLocal, L"Test", L"New Value");
            ExitOnFailure(hr, "Failed to set string");

            hr = CfgSetDword(cdhLocal, L"Num1", 10);
            ExitOnFailure(hr, "Failed to set dword");

            WaitForSqlCeTimestampChange();
            hr = CfgSetDword(cdhLocal, L"Num1", 100);
            ExitOnFailure(hr, "Failed to set dword");

            WaitForSqlCeTimestampChange();
            hr = CfgSetDword(cdhLocal, L"Num1", 30);
            ExitOnFailure(hr, "Failed to set dword");

            hr = CfgSetDword(cdhLocal, L"Num2", 20);
            ExitOnFailure(hr, "Failed to set dword");

            hr = CfgSetQword(cdhLocal, L"Qword", 5000000000000);
            ExitOnFailure(hr, "Failed to set qword");

            hr = CfgSetBool(cdhLocal, L"BoolTrueVal", TRUE);
            ExitOnFailure(hr, "Failed to set boolean true value");

            hr = CfgSetBool(cdhLocal, L"BoolFalseVal", FALSE);
            ExitOnFailure(hr, "Failed to set boolean true value");

            hr = CfgSetProduct(cdhLocal, L"Test2", L"1.0.0.0", L"abcdabcdabcdabcd");
            ExitOnFailure(hr, "Failed to set product");

            // Shouldn't find value "Test" under the newly set product, because each product has their own namespace!
            hr = CfgGetString(cdhLocal, L"Test", &sczValue);
            if (E_NOTFOUND != hr)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected to not find string, but found string!");
            }

            // Set back to a product that actually has values, so we can test reading them
            // Make sure it works case-insensitively
            hr = CfgSetProduct(cdhLocal, L"test", L"1.0.0.0", L"abcdabcdabcdabcd");
            ExitOnFailure(hr, "Failed to set product");

            // Check we can query values case-insensitively
            hr = CfgGetBool(cdhLocal, L"booltrueval", &fValue);
            ExitOnFailure(hr, "Failed to read BoolTrueVal");

            if (TRUE != fValue)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "BoolTrueVal should be TRUE, but wasn't");
            }

            hr = CfgGetBool(cdhLocal, L"BoolFalseVal", &fValue);
            ExitOnFailure(hr, "Failed to read BoolFalseVal");

            if (FALSE != fValue)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "BoolFalseVal should be FALSE, but wasn't");
            }

            WaitForSqlCeTimestampChange();
            hr = CfgDeleteValue(cdhLocal, L"BoolFalseVal");
            ExitOnFailure(hr, "Failed to delete BoolFalseVal");

            hr = CfgGetBool(cdhLocal, L"BoolFalseVal", &fValue);
            if (E_NOTFOUND != hr)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "BoolFalseVal should no longer exist at this point!");
            }

            hr = CfgGetDword(cdhLocal, L"Num1", &dwValue);
            ExitOnFailure(hr, "Failed to get dword value");

            hr = CfgGetDword(cdhLocal, L"Num2", &dwValue);
            ExitOnFailure(hr, "Failed to get dword value");

            hr = CfgGetQword(cdhLocal, L"Qword", &qwValue);
            ExitOnFailure(hr, "Failed to get qword value");

            hr = CfgGetString(cdhLocal, L"Test", &sczValue);
            ExitOnFailure(hr, "Failed to get string");

            hr = CfgGetDword(cdhLocal, L"Num2", &dwValue);
            ExitOnFailure(hr, "Failed to get dword value");

            hr = CfgGetString(cdhLocal, L"Test2", &sczValue);
            ExitOnFailure(hr, "Failed to get string");

            hr = CfgSetString(cdhLocal, L"Test2", L"Changed2");
            ExitOnFailure(hr, "Failed to set string");

            hr = CfgGetString(cdhLocal, L"Test2", &sczValue);
            ExitOnFailure(hr, "Failed to get string");

            hr = CfgGetString(cdhLocal, L"Test", &sczValue);
            ExitOnFailure(hr, "Failed to get string");

            hr = CfgDeleteValue(cdhLocal, L"Num1");
            ExitOnFailure(hr, "Failed to delete value Num1");

            hr = CfgDeleteValue(cdhLocal, L"Qword");
            ExitOnFailure(hr, "Failed to delete value Qword");

            WaitForSqlCeTimestampChange();
            hr = CfgGetString(cdhLocal, L"Test2", &sczValue);
            ExitOnFailure(hr, "Failed to get string");

            hr = CfgDeleteValue(cdhLocal, L"Test2");
            ExitOnFailure(hr, "Failed to delete value Test2");

            hr = CfgGetString(cdhLocal, L"Test", &sczValue);
            ExitOnFailure(hr, "Failed to get string");

            hr = CfgGetString(cdhLocal, L"Num2", &sczValue);
            if (HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH) != hr)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Should have gotten an error here!");
            }

            hr = CfgGetDword(cdhLocal, L"Test", &dwValue);
            if (HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH) != hr)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Should have gotten an error here!");
            }

            hr = CfgGetString(cdhLocal, L"Test2", &sczValue);
            if (E_NOTFOUND != hr)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected to not find string, but found string!");
            }

            hr = CfgGetDword(cdhLocal, L"Num1", &dwValue);
            if (E_NOTFOUND != hr)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected to not find dword, but found dword!");
            }

            hr = CfgGetQword(cdhLocal, L"Qword", &qwValue);
            if (E_NOTFOUND != hr)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected to not find qword, but found qword!");
            }

            hr = CfgGetDword(cdhLocal, L"Num2", &dwValue);
            ExitOnFailure(hr, "Failed to get dword value");

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            TestUninitialize();

        LExit:
            ReleaseMem(pBuffer);
            ReleaseStr(sczValue);
        }
    };
}
