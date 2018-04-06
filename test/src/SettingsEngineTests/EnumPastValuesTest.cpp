// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class EnumPastValues : public CfgTest
    {
    public:
        [Fact]
        void EnumPastValuesTest()
        {
            HRESULT hr = S_OK;
            DWORD dwCount = 0;
            CFGDB_HANDLE cdhLocal = NULL;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;
            BYTE rgbData[1500] = { };
            rgbData[10] = 0xA1;
            rgbData[20] = 0xA2;

            TestInitialize();

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = CfgSetProduct(cdhLocal, L"TestEnumPastValues", L"1.0.0.0", L"abcdabcdabcdabcd");
            ExitOnFailure(hr, "Failed to set product");

            hr = CfgSetString(cdhLocal, L"Test1", L"Value1");
            ExitOnFailure(hr, "Failed to set Test1 string (to 'Value1')");

            AddToSystemTime(900);
            hr = CfgSetString(cdhLocal, L"Test1", L"Value2");
            ExitOnFailure(hr, "Failed to set Test1 string (to 'Value2')");

            AddToSystemTime(900);
            hr = CfgSetDword(cdhLocal, L"Test1", 500);
            ExitOnFailure(hr, "Failed to set Test1 string (to 500)");

            AddToSystemTime(900);
            hr = CfgSetString(cdhLocal, L"Test1", L"Value3");
            ExitOnFailure(hr, "Failed to set Test1 string (to 'Value3')");

            AddToSystemTime(900);
            hr = CfgDeleteValue(cdhLocal, L"Test1");
            ExitOnFailure(hr, "Failed to delete value Test1");

            AddToSystemTime(900);
            hr = CfgSetBool(cdhLocal, L"Test1", FALSE);
            ExitOnFailure(hr, "Failed to set Test1 string (to FALSE)");

            AddToSystemTime(900);
            hr = CfgSetBlob(cdhLocal, L"Test1", rgbData, sizeof(rgbData));
            ExitOnFailure(hr, "Failed to set Test1 string (to blob)");

            AddToSystemTime(900);
            hr = CfgSetString(cdhLocal, L"Test1", L"ValueThatWillBeExpiredDueToShortLife");
            ExitOnFailure(hr, "Failed to set Test1 string (to 'ValueThatWillBeExpiredDueToShortLife')");

            AddToSystemTime(1);
            hr = CfgSetBool(cdhLocal, L"Test1", TRUE);
            ExitOnFailure(hr, "Failed to set Test1 string (to TRUE)");

            // After values are older than an hour, they must be kept 
            AddToSystemTime(3601);
            hr = CfgSetString(cdhLocal, L"Test1", L"LatestValue");
            ExitOnFailure(hr, "Failed to set Test1 string (to 'LatestValue')");

            hr = CfgEnumPastValues(cdhLocal, L"Test1", &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate past values of Test1");

            if (dwCount != 9)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "There should be 9 values in Test1's history - there were %u found", dwCount);
            }

            VerifyHistoryString(cehHandle, dwCount - 1, L"LatestValue");
            VerifyHistoryBool(cehHandle, dwCount - 2, TRUE);
            VerifyHistoryBlob(cdhLocal, cehHandle, dwCount - 3, rgbData, sizeof(rgbData));
            VerifyHistoryBool(cehHandle, dwCount - 4, FALSE);
            VerifyHistoryDeleted(cehHandle, dwCount - 5);
            VerifyHistoryString(cehHandle, dwCount - 6, L"Value3");
            VerifyHistoryDword(cehHandle, dwCount - 7, 500);
            VerifyHistoryString(cehHandle, dwCount - 8, L"Value2");
            VerifyHistoryString(cehHandle, dwCount - 9, L"Value1");
            CfgReleaseEnumeration(cehHandle);
            cehHandle = NULL;

            hr = CfgForgetProduct(cdhLocal, L"TestEnumPastValues", L"1.0.0.0", L"abcdabcdabcdabcd");
            ExitOnFailure(hr, "Failed to forget product");

            hr = CfgSetProduct(cdhLocal, L"TestEnumPastValues", L"1.0.0.0", L"abcdabcdabcdabcd");
            ExitOnFailure(hr, "Failed to set product");

            hr = CfgEnumPastValues(cdhLocal, L"Test1", &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate past values of Test1");

            if (dwCount > 0)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Test1 value should no longer have any history");
            }

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            TestUninitialize();

        LExit:
            CfgReleaseEnumeration(cehHandle);
        }
    };
}
