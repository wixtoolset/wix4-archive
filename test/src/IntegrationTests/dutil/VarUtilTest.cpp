//-------------------------------------------------------------------------------------------------
// <copyright file="VarUtilTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#undef GetTempPath
#undef GetEnvironmentVariable

using namespace System;
using namespace Xunit;
using namespace WixTest;

namespace DutilTests
{
    typedef struct _VarUtilContext
    {
        DWORD dw;
        LPWSTR scz;
    } VarUtilContext;

    void DAPI FreeValueContext(LPVOID pvContext)
    {
        if (pvContext)
        {
            MemFree(pvContext);
        }
    }

    HRESULT DAPI VarUtilVariableNotFoundTestHelper(LPCWSTR wzVariable, LPVOID /*pvContext*/, BOOL* pfLog, VARIABLE_VALUE** ppValue)
    {
        HRESULT hr = S_OK;
        VARIABLE_VALUE* pValue = NULL;

        pValue = reinterpret_cast<VARIABLE_VALUE*>(MemAlloc(sizeof(VARIABLE_VALUE), TRUE));
        ExitOnNull(pValue, hr, E_OUTOFMEMORY, "Failed to allocate memory for new variable value: %ls", wzVariable);

        pValue->type = VARIABLE_VALUE_TYPE_STRING;
        hr = StrAllocString(&pValue->sczValue, wzVariable, 0);
        ExitOnFailure(hr, "Failed to alloc value string: %ls", wzVariable);

        *ppValue = pValue;
        pValue = NULL;

        hr = S_OK;
        *pfLog = TRUE;

    LExit:
        ReleaseVariableValue(pValue);

        return hr;
    }

    public ref class VarUtil : WixTestBase
    {
    public:
        void TestInitialize() override
        {
            WixTestBase::TestInitialize();

            HRESULT hr = S_OK;

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"VarUtilIntegrationTest", NULL, L"txt", FALSE, FALSE, NULL);
            NativeAssert::Succeeded(hr, "Failed to open log.");

            hr = CrypInitialize();
            NativeAssert::Succeeded(hr, "CrypInitialize failed.");
        }

        void TestUninitialize() override
        {
            CrypUninitialize();
            LogUninitialize(FALSE);

            WixTestBase::TestUninitialize();
        }

        [NamedFact]
        void VarUtilBasicTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;

            try
            {
                hr = VarCreate(NULL, NULL, &pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                // set variables
                VarSetStringTestHelper(pVariables, L"PROP1", L"VAL1");
                VarSetNumericTestHelper(pVariables, L"PROP2", 2);
                VarSetStringTestHelper(pVariables, L"PROP5", L"VAL5");
                VarSetStringTestHelper(pVariables, L"PROP3", L"VAL3");
                VarSetStringTestHelper(pVariables, L"PROP4", L"VAL4");
                VarSetStringTestHelper(pVariables, L"PROP6", L"VAL6");
                VarSetStringTestHelper(pVariables, L"PROP7", L"7");
                VarSetVersionTestHelper(pVariables, L"PROP8", MAKEQWORDVERSION(1, 1, 0, 0));

                // set overwritten variables
                VarSetStringTestHelper(pVariables, L"OVERWRITTEN_STRING", L"ORIGINAL");
                VarSetNumericTestHelper(pVariables, L"OVERWRITTEN_STRING", 42);

                VarSetNumericTestHelper(pVariables, L"OVERWRITTEN_NUMBER", 5);
                VarSetStringTestHelper(pVariables, L"OVERWRITTEN_NUMBER", L"NEW");

                // get and verify variable values
                VarGetStringTestHelper(pVariables, L"PROP1", L"VAL1");
                VarGetNumericTestHelper(pVariables, L"PROP2", 2);
                VarGetStringTestHelper(pVariables, L"PROP2", L"2");
                VarGetStringTestHelper(pVariables, L"PROP3", L"VAL3");
                VarGetStringTestHelper(pVariables, L"PROP4", L"VAL4");
                VarGetStringTestHelper(pVariables, L"PROP5", L"VAL5");
                VarGetStringTestHelper(pVariables, L"PROP6", L"VAL6");
                VarGetNumericTestHelper(pVariables, L"PROP7", 7);
                VarGetVersionTestHelper(pVariables, L"PROP8", MAKEQWORDVERSION(1, 1, 0, 0));
                VarGetStringTestHelper(pVariables, L"PROP8", L"1.1.0.0");

                VarGetNumericTestHelper(pVariables, L"OVERWRITTEN_STRING", 42);
                VarGetStringTestHelper(pVariables, L"OVERWRITTEN_NUMBER", L"NEW");
            }
            finally
            {
                ReleaseVariables(pVariables);
            }
        }

        [NamedFact]
        void VarUtilFormatTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;
            LPWSTR scz = NULL;
            DWORD cch = 0;
            try
            {
                hr = VarCreate(NULL, NULL, &pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                // set variables
                VarSetStringTestHelper(pVariables, L"PROP1", L"VAL1");
                VarSetStringTestHelper(pVariables, L"PROP2", L"VAL2");
                VarSetNumericTestHelper(pVariables, L"PROP3", 3);

                // test string formatting
                VarFormatStringTestHelper(pVariables, L"NOPROP", L"NOPROP");
                VarFormatStringTestHelper(pVariables, L"[PROP1]", L"VAL1");
                VarFormatStringTestHelper(pVariables, L" [PROP1] ", L" VAL1 ");
                VarFormatStringTestHelper(pVariables, L"PRE [PROP1]", L"PRE VAL1");
                VarFormatStringTestHelper(pVariables, L"[PROP1] POST", L"VAL1 POST");
                VarFormatStringTestHelper(pVariables, L"PRE [PROP1] POST", L"PRE VAL1 POST");
                VarFormatStringTestHelper(pVariables, L"[PROP1] MID [PROP2]", L"VAL1 MID VAL2");
                VarFormatStringTestHelper(pVariables, L"[NONE]", L"");
                VarFormatStringTestHelper(pVariables, L"[prop1]", L"");
                VarFormatStringTestHelper(pVariables, L"[\\[]", L"[");
                VarFormatStringTestHelper(pVariables, L"[\\]]", L"]");
                VarFormatStringTestHelper(pVariables, L"[]", L"[]");
                VarFormatStringTestHelper(pVariables, L"[NONE", L"[NONE");
                VarGetFormattedTestHelper(pVariables, L"PROP2", L"VAL2");
                VarGetFormattedTestHelper(pVariables, L"PROP3", L"3");

                hr = VarFormatString(pVariables, L"PRE [PROP1] POST", &scz, &cch);
                NativeAssert::Succeeded(hr, "Failed to format string.");

                Assert::Equal<DWORD>(lstrlenW(scz), cch);

                hr = VarFormatString(pVariables, L"PRE [PROP1] POST", NULL, &cch);
                NativeAssert::Succeeded(hr, "Failed to format string.");

                Assert::Equal<DWORD>(lstrlenW(scz), cch);
            }
            finally
            {
                ReleaseVariables(pVariables);
                ReleaseStr(scz);
            }
        }

        [NamedFact]
        void VarUtilEscapeTest()
        {
            // test string escaping
            VarEscapeStringTestHelper(L"[", L"[\\[]");
            VarEscapeStringTestHelper(L"]", L"[\\]]");
            VarEscapeStringTestHelper(L" [TEXT] ", L" [\\[]TEXT[\\]] ");
        }

        [NamedFact]
        void VarUtilConditionTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;

            try
            {
                hr = VarCreate(NULL, NULL, &pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                // set variables
                VarSetStringTestHelper(pVariables, L"PROP1", L"VAL1");
                VarSetStringTestHelper(pVariables, L"PROP2", L"VAL2");
                VarSetStringTestHelper(pVariables, L"PROP3", L"VAL3");
                VarSetStringTestHelper(pVariables, L"PROP4", L"BEGIN MID END");
                VarSetNumericTestHelper(pVariables, L"PROP5", 5);
                VarSetNumericTestHelper(pVariables, L"PROP6", 6);
                VarSetStringTestHelper(pVariables, L"PROP7", L"");
                VarSetNumericTestHelper(pVariables, L"PROP8", 0);
                VarSetStringTestHelper(pVariables, L"_PROP9", L"VAL9");
                VarSetNumericTestHelper(pVariables, L"PROP10", -10);
                VarSetNumericTestHelper(pVariables, L"PROP11", 9223372036854775807ll);
                VarSetNumericTestHelper(pVariables, L"PROP12", -9223372036854775808ll);
                VarSetNumericTestHelper(pVariables, L"PROP13", 0x00010000);
                VarSetNumericTestHelper(pVariables, L"PROP14", 0x00000001);
                VarSetNumericTestHelper(pVariables, L"PROP15", 0x00010001);
                VarSetVersionTestHelper(pVariables, L"PROP16", MAKEQWORDVERSION(0, 0, 0, 0));
                VarSetVersionTestHelper(pVariables, L"PROP17", MAKEQWORDVERSION(1, 0, 0, 0));
                VarSetVersionTestHelper(pVariables, L"PROP18", MAKEQWORDVERSION(1, 1, 0, 0));
                VarSetVersionTestHelper(pVariables, L"PROP19", MAKEQWORDVERSION(1, 1, 1, 0));
                VarSetVersionTestHelper(pVariables, L"PROP20", MAKEQWORDVERSION(1, 1, 1, 1));
                VarSetNumericTestHelper(pVariables, L"vPROP21", 1);
                VarSetVersionTestHelper(pVariables, L"PROP22", MAKEQWORDVERSION(65535, 65535, 65535, 65535));
                VarSetStringTestHelper(pVariables, L"PROP23", L"1.1.1");

                // test conditions
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP7"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP8"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"_PROP9"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP16"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP17"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"NONE = \"NOT\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP1 <> \"VAL1\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"NONE <> \"NOT\""));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 ~= \"val1\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"val1\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP1 ~<> \"val1\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 <> \"val1\""));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5 = 5"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP5 = 0"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP5 <> 5"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5 <> 0"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP10 = -10"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP10 <> -10"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP17 = v1"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP17 = v0"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP17 <> v1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP17 <> v0"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP16 = v0"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP17 = v1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP18 = v1.1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP19 = v1.1.1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP20 = v1.1.1.1"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"PROP20 = v1.1.1.1.0"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"PROP20 = v1.1.1.1.1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"vPROP21 = 1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP23 = v1.1.1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"v1.1.1 = PROP23"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 <> v1.1.1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"v1.1.1 <> PROP1"));

                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP11 = 9223372036854775806"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP11 = 9223372036854775807"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"PROP11 = 9223372036854775808"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"PROP11 = 92233720368547758070000"));

                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP12 = -9223372036854775807"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP12 = -9223372036854775808"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"PROP12 = -9223372036854775809"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"PROP12 = -92233720368547758080000"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP22 = v65535.65535.65535.65535"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"PROP22 = v65536.65535.65535.65535"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"PROP22 = v65535.655350000.65535.65535"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5 < 6"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP5 < 5"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5 > 4"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP5 > 5"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5 <= 6"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5 <= 5"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP5 <= 4"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5 >= 4"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP5 >= 5"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP5 >= 6"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP4 << \"BEGIN\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP4 << \"END\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP4 >> \"END\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP4 >> \"BEGIN\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP4 >< \"MID\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP4 >< \"NONE\""));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP16 < v1.1"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP16 < v0"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP17 > v0.12"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP17 > v1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP18 >= v1.0"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP18 >= v1.1"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP18 >= v2.1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP19 <= v1.1234.1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP19 <= v1.1.1"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP19 <= v1.0.123"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP6 = \"6\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"\"6\" = PROP6"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP6 = \"ABC\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"\"ABC\" = PROP6"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"\"ABC\" = PROP6"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP13 << 1"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP13 << 0"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP14 >> 1"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP14 >> 0"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP15 >< 65537"));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP15 >< 0"));

                Assert::False(EvaluateConditionTestHelper(pVariables, L"NOT PROP1"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"NOT (PROP1 <> \"VAL1\")"));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"NOT\" AND PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"NOT\" AND PROP2 = \"NOT\""));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\" OR PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\" OR PROP2 = \"NOT\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"NOT\" OR PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"NOT\" OR PROP2 = \"NOT\""));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"VAL2\" OR PROP3 = \"NOT\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\" OR PROP3 = \"VAL3\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\" OR PROP3 = \"NOT\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP1 = \"VAL1\" AND (PROP2 = \"NOT\" OR PROP3 = \"VAL3\")"));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"(PROP1 = \"VAL1\" AND PROP2 = \"VAL2\") OR PROP3 = \"NOT\""));

                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP3 = \"NOT\" OR PROP1 = \"VAL1\" AND PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP3 = \"VAL3\" OR PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::False(EvaluateConditionTestHelper(pVariables, L"PROP3 = \"NOT\" OR PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"(PROP3 = \"NOT\" OR PROP1 = \"VAL1\") AND PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionTestHelper(pVariables, L"PROP3 = \"NOT\" OR (PROP1 = \"VAL1\" AND PROP2 = \"VAL2\")"));

                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"="));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"(PROP1"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"(PROP1 = \""));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"1A"));
                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"*"));

                Assert::True(EvaluateFailureConditionTestHelper(pVariables, L"1 == 1"));
            }
            finally
            {
                ReleaseVariables(pVariables);
            }
        }

        [NamedFact]
        void VarUtilValueTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;
            VARIABLE_VALUE values[8];

            try
            {
                hr = VarCreate(NULL, NULL, &pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                // set variables
                InitNumericValue(pVariables, values + 0, 2, FALSE, 1, L"PROP1");
                VerifyValue(pVariables, values + 0);

                InitStringValue(pVariables, values + 1, L"VAL2", FALSE, 2, L"PROP2");
                VerifyValue(pVariables, values + 1);

                InitVersionValue(pVariables, values + 2, MAKEQWORDVERSION(1, 1, 0, 0), FALSE, 3, L"PROP3");
                VerifyValue(pVariables, values + 2);

                InitNoneValue(pVariables, values + 3, FALSE, 4, L"PROP4");
                VerifyValue(pVariables, values + 3);

                InitNoneValue(pVariables, values + 4, TRUE, 5, L"PROP5");
                VerifyValue(pVariables, values + 4);

                InitVersionValue(pVariables, values + 5, MAKEQWORDVERSION(1, 1, 1, 0), TRUE, 6, L"PROP6");
                VerifyValue(pVariables, values + 5);

                InitStringValue(pVariables, values + 6, L"7", TRUE, 7, L"PROP7");
                VerifyValue(pVariables, values + 6);

                InitNumericValue(pVariables, values + 7, 11, TRUE, 8, L"PROP8");
                VerifyValue(pVariables, values + 7);

                // This loop is unreliable, it fails ~10% of the time.
                // This whole test was ported to a native C++ console app, and it worked 100% of the time.
                // The problem is that sometimes the variant (which is in the middle of the struct, all other values are fine) gives incorrect values.
                //for (DWORD i = 0; i < 8; i++)
                //{
                //    VerifyValue(pVariables, values + i);
                //}
            }
            finally
            {
                VarDestroy(pVariables, FreeValueContext);
            }
        }

        [NamedFact(Skip = "varutil Not Implemented Yet.")]
        void VarUtilEnumTest()
        {
            HRESULT hr = S_OK;
            const DWORD dwIndex = 8;
            VARIABLES_HANDLE pVariables = NULL;
            VARIABLE_ENUM_HANDLE pEnum = NULL;
            VARIABLE_VALUE values[dwIndex];
            VARIABLE_ENUM_VALUE* pValue = NULL;

            try
            {
                hr = VarCreate(NULL, NULL, &pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                hr = VarStartEnum(pVariables, &pEnum, &pValue);
                NativeAssert::ValidReturnCode(hr, E_NOMOREITEMS);

                // set variables
                InitNumericValue(pVariables, values + 0, 2, FALSE, 0, L"PROP1");
                InitStringValue(pVariables, values + 1, L"VAL2", FALSE, 0, L"PROP2");
                InitVersionValue(pVariables, values + 2, MAKEQWORDVERSION(1, 1, 0, 0), FALSE, 0, L"PROP3");
                InitNoneValue(pVariables, values + 3, FALSE, 0, L"PROP4");
                InitNoneValue(pVariables, values + 4, TRUE, 0, L"PROP5");
                InitVersionValue(pVariables, values + 5, MAKEQWORDVERSION(1, 1, 1, 0), TRUE, 0, L"PROP6");
                InitStringValue(pVariables, values + 6, L"7", TRUE, 0, L"PROP7");
                InitNumericValue(pVariables, values + 7, 11, TRUE, 0, L"PROP8");

                hr = VarStartEnum(pVariables, &pEnum, &pValue);
                
                for (DWORD i = dwIndex - 1; i; --i)
                {
                    NativeAssert::ValidReturnCode(hr, S_OK);

                    VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(pValue->value.pvContext);
                    pContext->dw += 1;

                    hr = VarNextVariable(pEnum, &pValue);
                }

                NativeAssert::ValidReturnCode(hr, E_NOMOREITEMS);

                for (DWORD j = 0; j < dwIndex; j++)
                {
                    VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(values[j].pvContext);
                    NativeAssert::Equal<DWORD>(1, pContext->dw);
                }

                VarFinishEnum(pEnum);
                pEnum = NULL;

                hr = VarStartEnum(pVariables, &pEnum, &pValue);

                for (DWORD i = dwIndex - 1; i; --i)
                {
                    NativeAssert::ValidReturnCode(hr, S_OK);

                    VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(pValue->value.pvContext);
                    pContext->dw += 1;

                    hr = VarNextVariable(pEnum, &pValue);
                }

                NativeAssert::ValidReturnCode(hr, E_NOMOREITEMS);

                for (DWORD j = 0; j < dwIndex; j++)
                {
                    VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(values[j].pvContext);
                    NativeAssert::Equal<DWORD>(2, pContext->dw);
                }
            }
            finally
            {
                VarFinishEnum(pEnum);
                ReleaseVariableEnumValue(pValue);
                VarDestroy(pVariables, FreeValueContext);
            }
        }

        [NamedFact]
        void VarUtilVariableNotFoundTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;
            LPWSTR sczValue = NULL;

            try
            {
                hr = VarCreate(VarUtilVariableNotFoundTestHelper, NULL, &pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                hr = VarFormatString(pVariables, L"[Test1] [Hi] [Three]", &sczValue, NULL);
                NativeAssert::Succeeded(hr, "VarFormatString failed.");
                NativeAssert::StringEqual(L"Test1 Hi Three", sczValue);
            }
            finally
            {
                ReleaseStr(sczValue);
                VarDestroy(pVariables, FreeValueContext);
            }
        }

    private:
        void InitNoneValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pValue, BOOL fHidden, DWORD dw, LPCWSTR wz)
        {
            InitValueContext(pValue, dw, wz);

            pValue->type = VARIABLE_VALUE_TYPE_NONE;
            pValue->fHidden = fHidden;

            HRESULT hr = VarSetValue(pVariables, wz, pValue, TRUE);
            NativeAssert::Succeeded(hr, "Failed to set value for variable {0}", wz);
        }

        void InitNumericValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pValue, LONGLONG llValue, BOOL fHidden, DWORD dw, LPCWSTR wz)
        {
            InitValueContext(pValue, dw, wz);

            pValue->type = VARIABLE_VALUE_TYPE_NUMERIC;
            pValue->fHidden = fHidden;

            pValue->llValue = llValue;

            HRESULT hr = VarSetValue(pVariables, wz, pValue, TRUE);
            NativeAssert::Succeeded(hr, "Failed to set value for variable {0}", wz);
        }

        void InitStringValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pValue, LPWSTR wzValue, BOOL fHidden, DWORD dw, LPCWSTR wz)
        {
            InitValueContext(pValue, dw, wz);

            pValue->type = VARIABLE_VALUE_TYPE_STRING;
            pValue->fHidden = fHidden;

            HRESULT hr = StrAllocString(&pValue->sczValue, wzValue, 0);
            NativeAssert::Succeeded(hr, "Failed to alloc string: {0}", wzValue);

            hr = VarSetValue(pVariables, wz, pValue, TRUE);
            NativeAssert::Succeeded(hr, "Failed to set value for variable {0}", wz);
        }

        void InitVersionValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pValue, DWORD64 qwValue, BOOL fHidden, DWORD dw, LPCWSTR wz)
        {
            InitValueContext(pValue, dw, wz);

            pValue->type = VARIABLE_VALUE_TYPE_VERSION;
            pValue->fHidden = fHidden;

            pValue->qwValue = qwValue;

            HRESULT hr = VarSetValue(pVariables, wz, pValue, TRUE);
            NativeAssert::Succeeded(hr, "Failed to set value for variable {0}", wz);
        }

        void InitValueContext(VARIABLE_VALUE* pValue, DWORD dw, LPCWSTR wz)
        {
            memset(pValue, 0, sizeof(VARIABLE_VALUE));
            pValue->pvContext = MemAlloc(sizeof(VarUtilContext), TRUE);
            VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(pValue->pvContext);
            if (!pContext)
            {
                throw gcnew OutOfMemoryException();
            }

            pContext->dw = dw;

            HRESULT hr = StrAllocString(&pContext->scz, wz, 0);
            NativeAssert::Succeeded(hr, "Failed to alloc string: {0}", wz);
        }

        void VerifyValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pExpectedValue)
        {
            VARIABLE_VALUE* pActualValue = NULL;

            try
            {
                VarUtilContext* pExpectedContext = reinterpret_cast<VarUtilContext*>(pExpectedValue->pvContext);
                NativeAssert::True(NULL != pExpectedContext);

                LogStringLine(REPORT_STANDARD, "Verifying Variable: %ls", pExpectedContext->scz);

                HRESULT hr = VarGetValue(pVariables, pExpectedContext->scz, &pActualValue);
                NativeAssert::Succeeded(hr, "Failed to get value: {0}", pExpectedContext->scz);

                NativeAssert::Equal<DWORD>(pExpectedValue->type, pActualValue->type);
                NativeAssert::InRange<DWORD>(pExpectedValue->type, VARIABLE_VALUE_TYPE_NONE, VARIABLE_VALUE_TYPE_VERSION);

                switch (pExpectedValue->type)
                {
                case VARIABLE_VALUE_TYPE_NONE:
                case VARIABLE_VALUE_TYPE_VERSION:
                    NativeAssert::Equal(pExpectedValue->qwValue, pActualValue->qwValue);
                    break;
                case VARIABLE_VALUE_TYPE_NUMERIC:
                    NativeAssert::Equal(pExpectedValue->llValue, pActualValue->llValue);
                    break;
                case VARIABLE_VALUE_TYPE_STRING:
                    NativeAssert::StringEqual(pExpectedValue->sczValue, pActualValue->sczValue);
                    break;
                }

                NativeAssert::Equal(pExpectedValue->fHidden, pActualValue->fHidden);
                NativeAssert::True(pExpectedValue->pvContext == pActualValue->pvContext);
            }
            finally
            {
                ReleaseVariableValue(pActualValue);
            }
        }
    };
}