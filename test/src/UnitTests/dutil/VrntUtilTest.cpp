//-------------------------------------------------------------------------------------------------
// <copyright file="VrntUtilTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include <vrntutilhelpers.h>

namespace DutilTests
{
    using namespace System;
    using namespace Xunit;
    using namespace WixTest;

    public ref class VrntUtil
    {
    public:
        [NamedFact]
        void VrntUninitializeTest()
        {
            HRESULT hr = S_OK;
            LPWSTR scz = NULL;
            VRNTUTIL_VARIANT variant = { };

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            try
            {
                hr = StrAlloc(&scz, 1);
                NativeAssert::Succeeded(hr, "Failed to allocate string.");

                variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                variant.sczValue = scz;

                vrntHelper->ActualValue = NULL;
                VrntUninitializeHelper(&functions, &variant);
                NativeAssert::PointerEqual(scz, vrntHelper->ActualValue);
                scz = NULL;

                NativeAssert::PointerEqual(NULL, variant.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NONE, variant.Type);
            }
            finally
            {
                ReleaseStr(scz);
            }
        }

        [NamedFact]
        void VrntGetNumericTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT variant = { };
            LONGLONG llValue = 0;
            LONGLONG llExpected = 4;
            LPWSTR wzExpected = L"44";
            LONGLONG llExpectedWz = 44;

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            variant.Type = VRNTUTIL_VARIANT_TYPE_NONE;
            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::ValidReturnCode(hr, E_INVALIDARG);

            variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
            variant.llValue = llExpected;

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumeric failed.");
            NativeAssert::Equal<LONGLONG>(llExpected, llValue);

            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.sczValue = L"xyz";

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::ValidReturnCode(hr, DISP_E_TYPEMISMATCH);

            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.sczValue = wzExpected;

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumeric failed.");
            NativeAssert::Equal<LONGLONG>(llExpectedWz, llValue);

            variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
            variant.qwValue = MAKEQWORDVERSION(65535, 65535, 65535, 65535);

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumeric failed.");
            NativeAssert::Equal<LONGLONG>(-1, llValue);
        }

        [NamedFact]
        void VrntGetStringTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT variant = { };
            LPWSTR sczValue = NULL;
            LPWSTR wzExpected = L"Expected";
            LONGLONG llExpected = 55;
            LPWSTR wzExpectedLL = L"55";

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            try
            {
                variant.Type = VRNTUTIL_VARIANT_TYPE_NONE;
                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::ValidReturnCode(hr, E_INVALIDARG);

                variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
                variant.llValue = llExpected;

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetString failed.");
                NativeAssert::StringEqual(wzExpectedLL, sczValue);

                variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                variant.sczValue = wzExpected;

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetString failed.");
                NativeAssert::StringEqual(wzExpected, sczValue);

                variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
                variant.qwValue = MAKEQWORDVERSION(65535, 65535, 65535, 65535);

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetString failed.");
                NativeAssert::StringEqual(L"65535.65535.65535.65535", sczValue);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        [NamedFact]
        void VrntGetVersionTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT variant = { };
            DWORD64 qwValue = 0;
            DWORD64 qwExpected = 754451245;
            LPWSTR wzExpected = L"4.3.2.1";
            DWORD64 qwExpectedWz = MAKEQWORDVERSION(4, 3, 2, 1);

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            variant.Type = VRNTUTIL_VARIANT_TYPE_NONE;
            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::ValidReturnCode(hr, E_INVALIDARG);

            variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
            variant.llValue = (LONGLONG)qwExpected;

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersion failed.");
            NativeAssert::Equal<LONGLONG>(qwExpected, qwValue);

            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.sczValue = L"xyz";

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::ValidReturnCode(hr, DISP_E_TYPEMISMATCH);

            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.sczValue = wzExpected;

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersion failed.");
            NativeAssert::Equal<LONGLONG>(qwExpectedWz, qwValue);

            variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
            variant.qwValue = qwExpected;

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersion failed.");
            NativeAssert::Equal<LONGLONG>(qwExpected, qwValue);
        }

        [NamedFact]
        void VrntSetNumericTest()
        {
            HRESULT hr = S_OK;
            LONGLONG llValue = 2;
            VRNTUTIL_VARIANT variant = { };

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            hr = VrntSetNumericHelper(&functions, &variant, llValue);
            NativeAssert::Succeeded(hr, "VrntSetNumeric failed.");
            NativeAssert::Equal<LONGLONG>(llValue, variant.llValue);
            NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, variant.Type);
        }

        [NamedFact]
        void VrntSetStringTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczFirst = NULL;
            LPWSTR sczSecond = NULL;
            LPWSTR wzFirst = L"First";
            LPWSTR wzSecond = L"Second";
            VRNTUTIL_VARIANT variant = { };

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            try
            {
                hr = StrAllocString(&sczFirst, wzFirst, 0);
                NativeAssert::Succeeded(hr, "StrAllocString failed.");

                hr = VrntSetStringHelper(&functions, &variant, sczFirst, 0);
                NativeAssert::Succeeded(hr, "VrntSetString failed.");
                NativeAssert::StringEqual(wzFirst, variant.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, variant.Type);

                hr = StrAllocString(&sczSecond, wzSecond, 0);
                NativeAssert::Succeeded(hr, "StrAllocString failed.");

                vrntHelper->ActualValue = NULL;
                vrntHelper->ExpectedValue = variant.sczValue;
                hr = VrntSetStringHelper(&functions, &variant, sczSecond, 0);
                NativeAssert::Succeeded(hr, "VrntSetString failed.");
                NativeAssert::PointerEqual(vrntHelper->ExpectedValue, vrntHelper->ActualValue);
                NativeAssert::StringEqual(wzSecond, variant.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, variant.Type);

                vrntHelper->ActualValue = NULL;
                vrntHelper->ExpectedValue = variant.sczValue;
                hr = VrntSetStringHelper(&functions, &variant, NULL, 0);
                NativeAssert::Succeeded(hr, "VrntSetString failed.");
                NativeAssert::PointerEqual(vrntHelper->ExpectedValue, vrntHelper->ActualValue);
                NativeAssert::PointerEqual(NULL, variant.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NONE, variant.Type);
            }
            finally
            {
                ReleaseStr(sczFirst);
                ReleaseStr(sczSecond);
            }
        }

        [NamedFact]
        void VrntSetVersionTest()
        {
            HRESULT hr = S_OK;
            DWORD64 qwValue = MAKEQWORDVERSION(1, 2, 3, 4);
            VRNTUTIL_VARIANT variant = { };

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            hr = VrntSetVersionHelper(&functions, &variant, qwValue);
            NativeAssert::Succeeded(hr, "VrntSetVersion failed.");
            NativeAssert::Equal<DWORD64>(qwValue, variant.qwValue);
            NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, variant.Type);
        }

        [NamedFact]
        void VrntSetValueTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT source = { };
            VRNTUTIL_VARIANT target = { };
            LONGLONG llValue = 42;
            LPWSTR wzValue = L"String";
            DWORD64 qwValue = MAKEQWORDVERSION(4, 2, 4, 2);

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            try
            {
                source.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
                source.llValue = llValue;

                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValue failed.");
                NativeAssert::Equal<LONGLONG>(llValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                hr = StrAllocString(&source.sczValue, wzValue, 0);
                NativeAssert::Succeeded(hr, "StrAllocString failed.");

                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValue failed.");
                NativeAssert::StringEqual(wzValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
                source.qwValue = qwValue;

                vrntHelper->ActualValue = NULL;
                vrntHelper->ExpectedValue = target.sczValue;
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValue failed.");
                NativeAssert::PointerEqual(vrntHelper->ExpectedValue, vrntHelper->ActualValue);
                NativeAssert::Equal<DWORD64>(qwValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_STRING;

                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValue failed.");
                NativeAssert::PointerEqual(NULL, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NONE, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = (VRNTUTIL_VARIANT_TYPE)MAXDWORD;

                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::ValidReturnCode(hr, E_INVALIDARG);
            }
            finally
            {
                VrntUninitializeHelper(&functions, &source);
                VrntUninitializeHelper(&functions, &target);
            }
        }

        [NamedFact]
        void VrntCopyTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT source = { };
            VRNTUTIL_VARIANT target = { };
            LONGLONG llValue = 42;
            LPWSTR wzValue = L"String";
            DWORD64 qwValue = MAKEQWORDVERSION(4, 2, 4, 2);

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntCoreFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString
            };

            try
            {
                source.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
                source.llValue = llValue;

                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopy failed.");
                NativeAssert::Equal<LONGLONG>(llValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                hr = StrAllocString(&source.sczValue, wzValue, 0);
                NativeAssert::Succeeded(hr, "StrAllocString failed.");

                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopy failed.");
                NativeAssert::StringEqual(wzValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
                source.qwValue = qwValue;

                vrntHelper->ActualValue = NULL;
                vrntHelper->ExpectedValue = target.sczValue;
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopy failed.");
                NativeAssert::PointerEqual(vrntHelper->ExpectedValue, vrntHelper->ActualValue);
                NativeAssert::Equal<DWORD64>(qwValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_STRING;

                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopy failed.");
                NativeAssert::PointerEqual(NULL, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NONE, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = (VRNTUTIL_VARIANT_TYPE)MAXDWORD;

                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::ValidReturnCode(hr, E_INVALIDARG);
            }
            finally
            {
                VrntUninitializeHelper(&functions, &source);
                VrntUninitializeHelper(&functions, &target);
            }
        }
    };
}