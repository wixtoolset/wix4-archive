//-------------------------------------------------------------------------------------------------
// <copyright file="CondUtilTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

namespace DutilTests
{
    using namespace System;
    using namespace Xunit;
    using namespace WixTest;

    public ref class CondUtil : WixTestBase
    {
    public:
        void TestInitialize() override
        {
            WixTestBase::TestInitialize();

            HRESULT hr = S_OK;

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"CondUtilIntegrationTest", NULL, L"txt", FALSE, FALSE, NULL);
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
        void CondEvaluateTest()
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
    };
}