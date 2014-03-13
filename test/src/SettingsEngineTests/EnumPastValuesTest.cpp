//-------------------------------------------------------------------------------------------------
// <copyright file="EnumPastValuesTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Test enumerating past values.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class EnumPastValues : public CfgTest
    {
    public:
        void VerifyHistoryString(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, LPCWSTR wzValue)
        {
            HRESULT hr = S_OK;
            CONFIG_VALUETYPE cvType = VALUE_INVALID;
            LPCWSTR wzValueFromEnum = NULL;
            LPCWSTR wzByFromEnum = NULL;
            SYSTEMTIME st;

            hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure1(hr, "Failed to get value type: %u", dwIndex);

            if (VALUE_STRING != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find string value, found type: %d", cvType);
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_VALUESTRING, &wzValueFromEnum);
            ExitOnFailure1(hr, "Failed to enumerate string value: %u", dwIndex);
            if (0 != lstrcmpW(wzValue, wzValueFromEnum))
            {
                hr = E_FAIL;
                ExitOnFailure2(hr, "Expected value '%ls', found value '%ls'", wzValue, wzValueFromEnum);
            }

            hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
            ExitOnFailure1(hr, "Failed to read when value: %u", dwIndex);
            if (0 == st.wYear || 0 == st.wMonth)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'when' time encountered!");
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzByFromEnum);
            ExitOnFailure1(hr, "Failed to read by value: %u", dwIndex);
            if (0 == lstrlenW(wzByFromEnum))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'by' string encountered!");
            }

        LExit:
            return;
        }

        void VerifyHistoryDword(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, DWORD dwInValue)
        {
            HRESULT hr = S_OK;
            CONFIG_VALUETYPE cvType = VALUE_INVALID;
            DWORD dwValue;
            LPCWSTR wzBy = NULL;
            SYSTEMTIME st;

            hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure1(hr, "Failed to get value type: %u", dwIndex);

            if (VALUE_DWORD != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find dword value, found type: %d", cvType);
            }

            hr = CfgEnumReadDword(cehHandle, dwIndex, ENUM_DATA_VALUEDWORD, &dwValue);
            ExitOnFailure1(hr, "Failed to enumerate dword value: %u", dwIndex);
            if (dwValue != dwInValue)
            {
                hr = E_FAIL;
                ExitOnFailure2(hr, "Expected value %u, found value %u", dwInValue, dwValue);
            }

            hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
            ExitOnFailure1(hr, "Failed to read when value: %u", dwIndex);
            if (0 == st.wYear || 0 == st.wMonth)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'when' time encountered!");
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzBy);
            ExitOnFailure1(hr, "Failed to read by value: %u", dwIndex);
            if (0 == lstrlenW(wzBy))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'by' string encountered!");
            }

        LExit:
            return;
        }

        void VerifyHistoryBool(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, BOOL fInValue)
        {
            HRESULT hr = S_OK;
            CONFIG_VALUETYPE cvType = VALUE_INVALID;
            BOOL fValue;
            LPCWSTR wzBy = NULL;
            SYSTEMTIME st;

            hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure1(hr, "Failed to get value type: %u", dwIndex);

            if (VALUE_BOOL != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find bool value, found type: %d", cvType);
            }

            hr = CfgEnumReadBool(cehHandle, dwIndex, ENUM_DATA_VALUEBOOL, &fValue);
            ExitOnFailure1(hr, "Failed to enumerate bool value: %u", dwIndex);
            if (fValue != fInValue)
            {
                hr = E_FAIL;
                ExitOnFailure2(hr, "Expected value %ls, found value %ls", fInValue ? L"TRUE" : L"FALSE", fValue ? L"TRUE" : L"FALSE");
            }

            hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
            ExitOnFailure1(hr, "Failed to read when value: %u", dwIndex);
            if (0 == st.wYear || 0 == st.wMonth)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'when' time encountered!");
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzBy);
            ExitOnFailure1(hr, "Failed to read by value: %u", dwIndex);
            if (0 == lstrlenW(wzBy))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'by' string encountered!");
            }

        LExit:
            return;
        }

        void VerifyHistoryBlob(CFGDB_HANDLE cdhLocal, CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex, const BYTE *pbExpected, SIZE_T cbExpected)
        {
            HRESULT hr = S_OK;
            CONFIG_VALUETYPE cvType = VALUE_INVALID;
            BYTE *pbValue = NULL;
            SIZE_T cbValue = 0;
            LPCWSTR wzBy = NULL;
            SYSTEMTIME st;

            hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure1(hr, "Failed to get value type: %u", dwIndex);

            if (VALUE_BLOB != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "Expected to find blob value, found type: %d", cvType);
            }

            hr = CfgEnumReadBinary(cdhLocal, cehHandle, dwIndex, ENUM_DATA_BLOBCONTENT, &pbValue, &cbValue);
            ExitOnFailure1(hr, "Failed to enumerate blob value: %u", dwIndex);
            if (cbValue != cbExpected)
            {
                hr = E_FAIL;
                ExitOnFailure2(hr, "Expected blob of size %u, found blob of size %u", cbExpected, cbValue);
            }
            if (0 != memcmp(pbValue, pbExpected, cbExpected))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Blob history data didn't match expected blob");
            }

            hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
            ExitOnFailure1(hr, "Failed to read when value: %u", dwIndex);
            if (0 == st.wYear || 0 == st.wMonth)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'when' time encountered!");
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzBy);
            ExitOnFailure1(hr, "Failed to read by value: %u", dwIndex);
            if (0 == lstrlenW(wzBy))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'by' string encountered!");
            }

        LExit:
            ReleaseMem(pbValue);

            return;
        }

        void VerifyHistoryDeleted(CFG_ENUMERATION_HANDLE cehHandle, DWORD dwIndex)
        {
            HRESULT hr = S_OK;
            CONFIG_VALUETYPE cvType = VALUE_INVALID;
            LPCWSTR wzBy = NULL;
            SYSTEMTIME st;

            hr = CfgEnumReadDataType(cehHandle, dwIndex, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure1(hr, "Failed to read deleted value: %u", dwIndex);
            if (VALUE_DELETED != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been deleted, but it exists still!");
            }

            hr = CfgEnumReadSystemTime(cehHandle, dwIndex, ENUM_DATA_WHEN, &st);
            ExitOnFailure1(hr, "Failed to read when value: %u", dwIndex);
            if (0 == st.wYear || 0 == st.wMonth)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'when' time encountered!");
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_BY, &wzBy);
            ExitOnFailure1(hr, "Failed to read by value: %u", dwIndex);
            if (0 == lstrlenW(wzBy))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Empty 'by' string encountered!");
            }

        LExit:
            return;
        }

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

            ::Sleep(5);
            hr = CfgSetString(cdhLocal, L"Test1", L"Value2");
            ExitOnFailure(hr, "Failed to set Test1 string (to 'Value2')");

            ::Sleep(5);
            hr = CfgSetDword(cdhLocal, L"Test1", 500);
            ExitOnFailure(hr, "Failed to set Test1 string (to 500)");

            ::Sleep(5);
            hr = CfgSetString(cdhLocal, L"Test1", L"Value3");
            ExitOnFailure(hr, "Failed to set Test1 string (to 'Value3')");

            ::Sleep(5);
            hr = CfgDeleteValue(cdhLocal, L"Test1");
            ExitOnFailure(hr, "Failed to delete value Test1");

            ::Sleep(5);
            hr = CfgSetBool(cdhLocal, L"Test1", FALSE);
            ExitOnFailure(hr, "Failed to set Test1 string (to FALSE)");

            ::Sleep(5);
            hr = CfgSetBlob(cdhLocal, L"Test1", rgbData, sizeof(rgbData));
            ExitOnFailure(hr, "Failed to set Test1 string (to blob)");

            ::Sleep(5);
            hr = CfgSetBool(cdhLocal, L"Test1", TRUE);
            ExitOnFailure(hr, "Failed to set Test1 string (to TRUE)");

            ::Sleep(5);
            hr = CfgSetString(cdhLocal, L"Test1", L"LatestValue");
            ExitOnFailure(hr, "Failed to set Test1 string (to 'Value3')");

            ::Sleep(5);
            hr = CfgEnumPastValues(cdhLocal, L"Test1", &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate past values of Test1");

            if (dwCount < 9)
            {
                hr = E_FAIL;
                ExitOnFailure1(hr, "There should be at least 9 values in Test1's history - there were only %u found", dwCount);
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
