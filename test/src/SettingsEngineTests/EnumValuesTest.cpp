// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class EnumValues : public CfgTest
    {
    public: 
        void ExpectNoValues(CFGDB_HANDLE cdbHandle)
        {
            HRESULT hr = S_OK;
            DWORD dwCount = 0;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;

            hr = CfgEnumerateValues(cdbHandle, VALUE_ANY_TYPE, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate values");

            if (0 != dwCount)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Enum shouldn't have found any values!");
            }

        LExit:
            CfgReleaseEnumeration(cehHandle);
        }

        void ExpectNoFiles(CFGDB_HANDLE cdbHandle)
        {
            HRESULT hr = S_OK;
            DWORD dwCount = 0;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;

            hr = CfgEnumerateValues(cdbHandle, VALUE_BLOB, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate files");

            if (0 != dwCount)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Enum shouldn't have found any files!");
            }

        LExit:
            CfgReleaseEnumeration(cehHandle);
        }

        [Fact]
        void EnumValuesTest()
        {
            HRESULT hr = S_OK;
            LPCWSTR wzName = NULL;
            LPCWSTR wzValue = NULL;
            DWORD dwValue = 0;
            BOOL fValue = FALSE;
            DWORD dwCount = 0;
            CFGDB_HANDLE cdhLocal = NULL;
            CONFIG_VALUETYPE cvType = VALUE_INVALID;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;

            TestInitialize();

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize user settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = CfgSetProduct(cdhLocal, L"TestEnum", L"1.0.0.0", L"abcdabcdabcdabcd");
            ExitOnFailure(hr, "Failed to set product");

            hr = CfgSetProduct(cdhLocal, L"TestEnum", L"1.0.0.0", L"abcdabcdabcdabcd");
            ExitOnFailure(hr, "Failed to set product");

            ExpectNoValues(cdhLocal);
            ExpectNoFiles(cdhLocal);

            BYTE rgbFile1[1000] = { };
            rgbFile1[0] = 0x22;
            rgbFile1[50] = 0x88;
            hr = CfgSetBlob(cdhLocal, L"File1", rgbFile1, sizeof(rgbFile1));
            ExitOnFailure(hr, "Failed to add file File1");

            hr = CfgEnumerateValues(cdhLocal, VALUE_BLOB, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate blobs");

            if (dwCount != 1)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "CfgEnumerateFiles should have found 1 file!");
            }

            BYTE rgbFile2[500] = { };
            rgbFile2[0] = 0x33;
            rgbFile2[50] = 0x04;
            hr = CfgSetBlob(cdhLocal, L"File2", rgbFile2, sizeof(rgbFile2));
            ExitOnFailure(hr, "Failed to add file File2");

            hr = CfgEnumerateValues(cdhLocal, VALUE_BLOB, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate files");

            if (dwCount != 2)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "CfgEnumerateFiles should have found 2 files!");
            }

            hr = CfgEnumReadString(cehHandle, 0, ENUM_DATA_VALUENAME, &wzName);
            ExitOnFailure(hr, "Failed to read file name 0 from file enumeration");

            if (0 != lstrcmpW(wzName, L"File1"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been 'File1', found '%ls' instead", wzName);
            }

            hr = CfgEnumReadString(cehHandle, 1, ENUM_DATA_VALUENAME, &wzName);
            ExitOnFailure(hr, "Failed to read file name 1 from file enumeration");

            if (0 != lstrcmpW(wzName, L"File2"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been 'File2', found '%ls' instead", wzValue);
            }

            hr = CfgEnumReadDword(cehHandle, 0, ENUM_DATA_BLOBSIZE, &dwValue);
            ExitOnFailure(hr, "Failed to read file size 0 from file enumeration");

            if (1000 != dwValue)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Size for file 1 should have been 1000, found %u instead", dwValue);
            }

            hr = CfgEnumReadDword(cehHandle, 1, ENUM_DATA_BLOBSIZE, &dwValue);
            ExitOnFailure(hr, "Failed to read file size 1 from file enumeration");

            if (500 != dwValue)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Size for file 1 should have been 500, found %u instead", dwValue);
            }

            CfgReleaseEnumeration(cehHandle);
            hr = CfgSetString(cdhLocal, L"Test1", L"Value1");
            ExitOnFailure(hr, "Failed to set Test1 string");

            hr = CfgEnumerateValues(cdhLocal, VALUE_ANY_BUT_BLOB, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate values");

            if (1 != dwCount)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Enum should have found 1 value!");
            }

            hr = CfgEnumReadString(cehHandle, 0, ENUM_DATA_VALUENAME, &wzName);
            ExitOnFailure(hr, "Failed to get first value name from enum");
            if (0 != lstrcmpW(wzName, L"Test1"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuename should have been 'Test1', found '%ls' instead", wzName);
            }

            hr = CfgEnumReadDataType(cehHandle, 0, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure(hr, "Failed to get first value type from enum");
            if (VALUE_STRING != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuetype should have been VALUE_STRING, was %d instead", cvType);
            }

            hr = CfgEnumReadString(cehHandle, 0, ENUM_DATA_VALUESTRING, &wzValue);
            ExitOnFailure(hr, "Failed to get value from enum");

            if (0 != lstrcmpW(wzValue, L"Value1"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been 'Test1', found '%ls' instead", wzValue);
            }

            hr = CfgSetDword(cdhLocal, L"Test2", 200);
            ExitOnFailure(hr, "Failed to set dword value");

            CfgReleaseEnumeration(cehHandle);
            hr = CfgEnumerateValues(cdhLocal, VALUE_ANY_BUT_BLOB, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate values");

            if (2 != dwCount)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected 2 values in enumeration, found: %u values", dwCount);
            }

            hr = CfgEnumReadString(cehHandle, 0, ENUM_DATA_VALUENAME, &wzName);
            ExitOnFailure(hr, "Failed to get first value name from enum");
            if (0 != lstrcmpW(wzName, L"Test1"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuename should have been 'Test1', found '%ls' instead", wzName);
            }

            hr = CfgEnumReadDataType(cehHandle, 0, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure(hr, "Failed to get first value type from enum");
            if (VALUE_STRING != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuetype should have been VALUE_STRING, was %d instead", cvType);
            }

            hr = CfgEnumReadString(cehHandle, 0, ENUM_DATA_VALUESTRING, &wzValue);
            ExitOnFailure(hr, "Failed to get value from enum");

            if (0 != lstrcmpW(wzValue, L"Value1"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been 'Test1', found '%ls' instead", wzValue);
            }

            hr = CfgEnumReadString(cehHandle, 1, ENUM_DATA_VALUENAME, &wzName);
            ExitOnFailure(hr, "Failed to get 2nd value name from enum");
            if (0 != lstrcmpW(wzName, L"Test2"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuename should have been 'Test2', found '%ls' instead", wzName);
            }

            hr = CfgEnumReadDataType(cehHandle, 1, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure(hr, "Failed to get 2nd value type from enum");
            if (VALUE_DWORD != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuetype should have been VALUE_DWORD, was %d instead", cvType);
            }

            hr = CfgEnumReadDword(cehHandle, 1, ENUM_DATA_VALUEDWORD, &dwValue);
            ExitOnFailure(hr, "Failed to get value from enum");

            if (200 != dwValue)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been 200, found %u instead", dwValue);
            }

            hr = CfgSetBool(cdhLocal, L"Test3", TRUE);
            ExitOnFailure(hr, "Failed to set bool value");

            CfgReleaseEnumeration(cehHandle);
            hr = CfgEnumerateValues(cdhLocal, VALUE_ANY_BUT_BLOB, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate values");

            if (3 != dwCount)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected 3 values in enumeration, found: %u values", dwCount);
            }

            hr = CfgEnumReadString(cehHandle, 0, ENUM_DATA_VALUENAME, &wzName);
            ExitOnFailure(hr, "Failed to get first value name from enum");
            if (0 != lstrcmpW(wzName, L"Test1"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuename should have been 'Test1', found '%ls' instead", wzName);
            }

            hr = CfgEnumReadDataType(cehHandle, 0, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure(hr, "Failed to get first value type from enum");
            if (VALUE_STRING != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuetype should have been VALUE_STRING, was %d instead", cvType);
            }

            hr = CfgEnumReadString(cehHandle, 0, ENUM_DATA_VALUESTRING, &wzValue);
            ExitOnFailure(hr, "Failed to get value from enum");

            if (0 != lstrcmpW(wzValue, L"Value1"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been 'Test1', found '%ls' instead", wzValue);
            }

            hr = CfgEnumReadString(cehHandle, 1, ENUM_DATA_VALUENAME, &wzName);
            ExitOnFailure(hr, "Failed to get 2nd value name from enum");
            if (0 != lstrcmpW(wzName, L"Test2"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuename should have been 'Test2', found '%ls' instead", wzName);
            }

            hr = CfgEnumReadDataType(cehHandle, 1, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure(hr, "Failed to get 2nd value type from enum");
            if (VALUE_DWORD != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuetype should have been VALUE_DWORD, was %d instead", cvType);
            }

            hr = CfgEnumReadDword(cehHandle, 1, ENUM_DATA_VALUEDWORD, &dwValue);
            ExitOnFailure(hr, "Failed to get value from enum");

            if (200 != dwValue)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been 200, found %u instead", dwValue);
            }

            hr = CfgEnumReadString(cehHandle, 2, ENUM_DATA_VALUENAME, &wzName);
            ExitOnFailure(hr, "Failed to get 3rd value name from enum");
            if (0 != lstrcmpW(wzName, L"Test3"))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuename should have been 'Test3', found '%ls' instead", wzName);
            }

            hr = CfgEnumReadDataType(cehHandle, 2, ENUM_DATA_VALUETYPE, &cvType);
            ExitOnFailure(hr, "Failed to get 3rd value type from enum");
            if (VALUE_BOOL != cvType)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Valuetype should have been VALUE_BOOL, was %d instead", cvType);
            }

            hr = CfgEnumReadBool(cehHandle, 2, ENUM_DATA_VALUEBOOL, &fValue);
            ExitOnFailure(hr, "Failed to get value from enum");

            if (TRUE != fValue)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Value should have been TRUE, found %ls instead", fValue ? L"TRUE" : L"FALSE");
            }

            WaitForSqlCeTimestampChange();
            hr = CfgDeleteValue(cdhLocal, L"Test1");
            ExitOnFailure(hr, "Failed to delete Test1 value");

            hr = CfgDeleteValue(cdhLocal, L"Test2");
            ExitOnFailure(hr, "Failed to delete Test2 value");

            hr = CfgDeleteValue(cdhLocal, L"Test3");
            ExitOnFailure(hr, "Failed to delete Test3 value");

            hr = CfgDeleteValue(cdhLocal, L"File1");
            ExitOnFailure(hr, "Failed to delete File1 file");

            hr = CfgDeleteValue(cdhLocal, L"File2");
            ExitOnFailure(hr, "Failed to delete File2 file");

            ExpectNoValues(cdhLocal);
            ExpectNoFiles(cdhLocal);

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            TestUninitialize();

        LExit:
            CfgReleaseEnumeration(cehHandle);
        }
    };
}
