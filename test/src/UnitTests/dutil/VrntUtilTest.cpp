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
            VrntMockableFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
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
        void VrntGetTypeTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT variant = { };
            VRNTUTIL_VARIANT_TYPE type = VRNTUTIL_VARIANT_TYPE_NONE;
            VRNTUTIL_VARIANT_TYPE expectedType = VRNTUTIL_VARIANT_TYPE_NONE;

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
            };

            expectedType = VRNTUTIL_VARIANT_TYPE_NUMERIC;
            variant.Type = expectedType;

            hr = VrntGetTypeHelper(&functions, &variant, &type);
            NativeAssert::Succeeded(hr, "VrntGetTypeHelper failed.");
            NativeAssert::Equal<DWORD>(expectedType, type);

            expectedType = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.Type = expectedType;

            hr = VrntGetTypeHelper(&functions, &variant, &type);
            NativeAssert::Succeeded(hr, "VrntGetTypeHelper failed.");
            NativeAssert::Equal<DWORD>(expectedType, type);

            expectedType = VRNTUTIL_VARIANT_TYPE_VERSION;
            variant.Type = expectedType;

            hr = VrntGetTypeHelper(&functions, &variant, &type);
            NativeAssert::Succeeded(hr, "VrntGetTypeHelper failed.");
            NativeAssert::Equal<DWORD>(expectedType, type);

            expectedType = VRNTUTIL_VARIANT_TYPE_NONE;
            variant.Type = expectedType;
            hr = VrntGetTypeHelper(&functions, &variant, &type);
            NativeAssert::Succeeded(hr, "VrntGetTypeHelper failed.");
            NativeAssert::Equal<DWORD>(expectedType, type);
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
            VrntMockableFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
            };

            variant.Type = VRNTUTIL_VARIANT_TYPE_NONE;
            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Equal(E_INVALIDARG, hr);

            variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
            variant.llValue = llExpected;

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumericHelper failed.");
            NativeAssert::Equal<LONGLONG>(llExpected, llValue);

            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.sczValue = L"xyz";

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Equal(DISP_E_TYPEMISMATCH, hr);

            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.sczValue = wzExpected;

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumericHelper failed.");
            NativeAssert::Equal<LONGLONG>(llExpectedWz, llValue);

            variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
            variant.qwValue = MAKEQWORDVERSION(65535, 65535, 65535, 65535);

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumericHelper failed.");
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
            VrntMockableFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
            };

            try
            {
                variant.Type = VRNTUTIL_VARIANT_TYPE_NONE;
                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Equal(E_INVALIDARG, hr);

                variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                variant.sczValue = NULL;
                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetStringHelper failed.");
                NativeAssert::PointerEqual(NULL, sczValue);

                variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
                variant.llValue = llExpected;

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetStringHelper failed.");
                NativeAssert::StringEqual(wzExpectedLL, sczValue);

                variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                variant.sczValue = wzExpected;

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetStringHelper failed.");
                NativeAssert::StringEqual(wzExpected, sczValue);

                variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
                variant.qwValue = MAKEQWORDVERSION(65535, 65535, 65535, 65535);

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetStringHelper failed.");
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
            VrntMockableFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
            };

            variant.Type = VRNTUTIL_VARIANT_TYPE_NONE;
            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Equal(E_INVALIDARG, hr);

            variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
            variant.llValue = (LONGLONG)qwExpected;

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersionHelper failed.");
            NativeAssert::Equal<LONGLONG>(qwExpected, qwValue);

            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.sczValue = L"xyz";

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Equal(DISP_E_TYPEMISMATCH, hr);

            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.sczValue = wzExpected;

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersionHelper failed.");
            NativeAssert::Equal<LONGLONG>(qwExpectedWz, qwValue);

            variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
            variant.qwValue = qwExpected;

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersionHelper failed.");
            NativeAssert::Equal<LONGLONG>(qwExpected, qwValue);
        }

        [NamedFact]
        void VrntSetNumericTest()
        {
            HRESULT hr = S_OK;
            LONGLONG llValue = 2;
            VRNTUTIL_VARIANT variant = { };

            VrntUtilHelper^ vrntHelper = gcnew VrntUtilHelper();
            VrntMockableFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
            };

            hr = VrntSetNumericHelper(&functions, &variant, llValue);
            NativeAssert::Succeeded(hr, "VrntSetNumericHelper failed.");
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
            VrntMockableFunctions functions =
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
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, variant.Type);
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
            VrntMockableFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
            };

            hr = VrntSetVersionHelper(&functions, &variant, qwValue);
            NativeAssert::Succeeded(hr, "VrntSetVersionHelper failed.");
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
            VrntMockableFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
            };

            try
            {
                source.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
                source.llValue = llValue;

                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::Equal<LONGLONG>(llValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                hr = StrAllocString(&source.sczValue, wzValue, 0);
                NativeAssert::Succeeded(hr, "StrAllocString failed.");

                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::StringEqual(wzValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
                source.qwValue = qwValue;

                vrntHelper->ActualValue = NULL;
                vrntHelper->ExpectedValue = target.sczValue;
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::PointerEqual(vrntHelper->ExpectedValue, vrntHelper->ActualValue);
                NativeAssert::Equal<DWORD64>(qwValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_STRING;

                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::PointerEqual(NULL, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = (VRNTUTIL_VARIANT_TYPE)MAXDWORD;

                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Equal(E_INVALIDARG, hr);
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
            VrntMockableFunctions functions =
            {
                vrntHelper->PfnStrSecureZeroFreeString,
                CrypEncryptMemory,
                CrypDecryptMemory,
            };

            try
            {
                source.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
                source.llValue = llValue;

                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::Equal<LONGLONG>(llValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                hr = StrAllocString(&source.sczValue, wzValue, 0);
                NativeAssert::Succeeded(hr, "StrAllocString failed.");

                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::StringEqual(wzValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
                source.qwValue = qwValue;

                vrntHelper->ActualValue = NULL;
                vrntHelper->ExpectedValue = target.sczValue;
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::PointerEqual(vrntHelper->ExpectedValue, vrntHelper->ActualValue);
                NativeAssert::Equal<DWORD64>(qwValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = VRNTUTIL_VARIANT_TYPE_STRING;

                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::PointerEqual(NULL, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);

                VrntUninitializeHelper(&functions, &source);
                source.Type = (VRNTUTIL_VARIANT_TYPE)MAXDWORD;

                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Equal(E_INVALIDARG, hr);
            }
            finally
            {
                VrntUninitializeHelper(&functions, &source);
                VrntUninitializeHelper(&functions, &target);
            }
        }

        [NamedFact]
        void VrntSetEncryptionTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT variant = { };
            LONGLONG llValue = 42;
            LONGLONG llEncryptedValue = 24;
            LPWSTR wzValue = L"Decryptd";
            LPWSTR wzEncryptedValue = L"Encryptd";
            DWORD64 qwValue = MAKEQWORDVERSION(4, 2, 4, 2);
            DWORD64 qwEncryptedValue = MAKEQWORDVERSION(2, 4, 2, 4);

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(llValue, llEncryptedValue);
            crypHelper->AddMapping(wzValue, wzEncryptedValue);
            crypHelper->AddMapping(qwValue, qwEncryptedValue);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            try
            {
                hr = VrntSetNumericHelper(&functions, &variant, llValue);
                NativeAssert::Succeeded(hr, "VrntSetNumericHelper failed.");

                hr = VrntSetEncryptionHelper(&functions, &variant, TRUE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");
                NativeAssert::True(variant.fValueIsEncrypted);
                NativeAssert::Equal(llEncryptedValue, variant.llValue);

                VrntUninitializeHelper(&functions, &variant);

                hr = VrntSetStringHelper(&functions, &variant, wzValue, 0);
                NativeAssert::Succeeded(hr, "VrntSetStringHelper failed.");

                hr = VrntSetEncryptionHelper(&functions, &variant, TRUE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");
                NativeAssert::True(variant.fValueIsEncrypted);
                NativeAssert::StringEqual(wzEncryptedValue, variant.sczValue);

                hr = VrntSetEncryptionHelper(&functions, &variant, FALSE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptioHelpern failed.");
                NativeAssert::False(variant.fValueIsEncrypted);
                NativeAssert::StringEqual(wzValue, variant.sczValue);

                VrntUninitializeHelper(&functions, &variant);

                hr = VrntSetVersionHelper(&functions, &variant, qwValue);
                NativeAssert::Succeeded(hr, "VrntSetVersionHelper failed.");

                hr = VrntSetEncryptionHelper(&functions, &variant, TRUE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");
                NativeAssert::True(variant.fValueIsEncrypted);
                NativeAssert::Equal(qwEncryptedValue, variant.qwValue);

                hr = VrntSetEncryptionHelper(&functions, &variant, TRUE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");
                NativeAssert::True(variant.fValueIsEncrypted);
                NativeAssert::Equal(qwEncryptedValue, variant.qwValue);

                VrntUninitializeHelper(&functions, &variant);

                hr = VrntSetEncryptionHelper(&functions, &variant, TRUE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");
                NativeAssert::True(variant.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &variant);

                variant.Type = (VRNTUTIL_VARIANT_TYPE)MAXDWORD;
                hr = VrntSetEncryptionHelper(&functions, &variant, TRUE);
                NativeAssert::Equal(E_INVALIDARG, hr);
            }
            finally
            {
                VrntUninitializeHelper(&functions, &variant);
            }
        }

        [NamedFact]
        void VrntGetNumericEncryptedTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT variant = { };
            LONGLONG llValue = 0;
            LONGLONG llPlainText = 4;
            LONGLONG llCipherText = 8;
            LPWSTR wzPlainText = L"44";
            LPWSTR wzCipherText = L"88";
            LONGLONG llExpectedWz = 44;
            LPWSTR wzAlphaPlainText = L"xyz";
            LPWSTR wzAlphaCipherText = L"zyx";

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(llPlainText, llCipherText);
            crypHelper->AddMapping(wzPlainText, wzCipherText);
            crypHelper->AddMapping(wzAlphaPlainText, wzAlphaCipherText);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            VrntUninitializeHelper(&functions, &variant);
            variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
            variant.fValueIsEncrypted = TRUE;
            variant.llValue = llCipherText;

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumericHelper failed.");
            NativeAssert::Equal(llPlainText, llValue);
            NativeAssert::Equal(llCipherText, variant.llValue);
            NativeAssert::True(variant.fValueIsEncrypted);

            llValue = 0;
            variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumericHelper failed.");
            NativeAssert::Equal(llPlainText, llValue);
            NativeAssert::Equal(llCipherText, variant.llValue);
            NativeAssert::True(variant.fValueIsEncrypted);

            VrntUninitializeHelper(&functions, &variant);
            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.fValueIsEncrypted = TRUE;
            hr = CrypAllocStringForEncryption(&variant.sczValue, wzAlphaCipherText, 0, NULL);
            NativeAssert::Succeeded(hr, "CrypAllocStringForEncryption failed.");

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Equal(DISP_E_TYPEMISMATCH, hr);
            NativeAssert::StringEqual(wzAlphaCipherText, variant.sczValue);
            NativeAssert::True(variant.fValueIsEncrypted);

            VrntUninitializeHelper(&functions, &variant);
            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.fValueIsEncrypted = TRUE;
            hr = CrypAllocStringForEncryption(&variant.sczValue, wzCipherText, 0, NULL);
            NativeAssert::Succeeded(hr, "CrypAllocStringForEncryption failed.");

            hr = VrntGetNumericHelper(&functions, &variant, &llValue);
            NativeAssert::Succeeded(hr, "VrntGetNumericHelper failed.");
            NativeAssert::Equal<LONGLONG>(llExpectedWz, llValue);
            NativeAssert::StringEqual(wzCipherText, variant.sczValue);
            NativeAssert::True(variant.fValueIsEncrypted);
        }

        [NamedFact]
        void VrntGetStringEncryptedTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT variant = { };
            LPWSTR sczValue = NULL;
            LPWSTR wzPlainText = L"Expected";
            LPWSTR wzCipherText = L"Unexpected";
            LONGLONG llPlainText = 55555;
            LONGLONG llCipherText = 7777777;
            LPWSTR wzExpectedLL = L"55555";
            DWORD64 qwPlainText = MAKEQWORDVERSION(1, 22, 333, 4444);
            DWORD64 qwCipherText = MAKEQWORDVERSION(4, 33, 22, 1111);
            LPWSTR wzExpectedQW = L"1.22.333.4444";

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(wzPlainText, wzCipherText);
            crypHelper->AddMapping(llPlainText, llCipherText);
            crypHelper->AddMapping(qwPlainText, qwCipherText);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            try
            {
                variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                variant.fValueIsEncrypted = TRUE;
                variant.sczValue = NULL;

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetStringHelper failed.");
                NativeAssert::PointerEqual(NULL, sczValue);
                NativeAssert::PointerEqual(NULL, variant.sczValue);
                NativeAssert::True(variant.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &variant);
                variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
                variant.fValueIsEncrypted = TRUE;
                variant.llValue = llCipherText;

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetStringHelper failed.");
                NativeAssert::StringEqual(wzExpectedLL, sczValue);
                NativeAssert::Equal(llCipherText, variant.llValue);
                NativeAssert::True(variant.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &variant);
                variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
                variant.fValueIsEncrypted = TRUE;
                hr = CrypAllocStringForEncryption(&variant.sczValue, wzCipherText, 0, NULL);
                NativeAssert::Succeeded(hr, "CrypAllocStringForEncryption failed.");

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetStringHelper failed.");
                NativeAssert::StringEqual(wzPlainText, sczValue);
                NativeAssert::StringEqual(wzCipherText, variant.sczValue);
                NativeAssert::True(variant.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &variant);
                variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
                variant.fValueIsEncrypted = TRUE;
                variant.qwValue = qwCipherText;

                hr = VrntGetStringHelper(&functions, &variant, &sczValue);
                NativeAssert::Succeeded(hr, "VrntGetStringHelper failed.");
                NativeAssert::StringEqual(wzExpectedQW, sczValue);
                NativeAssert::Equal(qwCipherText, variant.qwValue);
                NativeAssert::True(variant.fValueIsEncrypted);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        [NamedFact]
        void VrntGetVersionEncryptedTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT variant = { };
            DWORD64 qwValue = 0;
            DWORD64 qwPlainText = MAKEQWORDVERSION(6, 7, 8, 9);
            DWORD64 qwCipherText = MAKEQWORDVERSION(9, 8, 7, 6);
            LPWSTR wzPlainText = L"1.2.3.4";
            LPWSTR wzCipherText = L"4.3.2.1";
            DWORD64 qwExpectedWz = MAKEQWORDVERSION(1, 2, 3, 4);
            LPWSTR wzUnparseablePlainText = L"abc.def.ghi.jkl";
            LPWSTR wzUnparseableCipherText = L"100.100.100.100";

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(qwPlainText, qwCipherText);
            crypHelper->AddMapping(wzPlainText, wzCipherText);
            crypHelper->AddMapping(wzUnparseablePlainText, wzUnparseableCipherText);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            VrntUninitializeHelper(&functions, &variant);
            variant.Type = VRNTUTIL_VARIANT_TYPE_VERSION;
            variant.fValueIsEncrypted = TRUE;
            variant.qwValue = qwCipherText;

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersionHelper failed.");
            NativeAssert::Equal(qwPlainText, qwValue);
            NativeAssert::Equal(qwCipherText, variant.qwValue);
            NativeAssert::True(variant.fValueIsEncrypted);

            qwValue = 0;
            variant.Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersionHelper failed.");
            NativeAssert::Equal(qwPlainText, qwValue);
            NativeAssert::Equal(qwCipherText, variant.qwValue);
            NativeAssert::True(variant.fValueIsEncrypted);

            VrntUninitializeHelper(&functions, &variant);
            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.fValueIsEncrypted = TRUE;
            hr = CrypAllocStringForEncryption(&variant.sczValue, wzUnparseableCipherText, 0, NULL);
            NativeAssert::Succeeded(hr, "CrypAllocStringForEncryption failed.");

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Equal(DISP_E_TYPEMISMATCH, hr);
            NativeAssert::StringEqual(wzUnparseableCipherText, variant.sczValue);
            NativeAssert::True(variant.fValueIsEncrypted);

            VrntUninitializeHelper(&functions, &variant);
            variant.Type = VRNTUTIL_VARIANT_TYPE_STRING;
            variant.fValueIsEncrypted = TRUE;
            hr = CrypAllocStringForEncryption(&variant.sczValue, wzCipherText, 0, NULL);
            NativeAssert::Succeeded(hr, "CrypAllocStringForEncryption failed.");

            hr = VrntGetVersionHelper(&functions, &variant, &qwValue);
            NativeAssert::Succeeded(hr, "VrntGetVersionHelper failed.");
            NativeAssert::Equal<LONGLONG>(qwExpectedWz, qwValue);
            NativeAssert::StringEqual(wzCipherText, variant.sczValue);
            NativeAssert::True(variant.fValueIsEncrypted);
        }

        [NamedFact]
        void VrntSetNumericEncryptedTest()
        {
            HRESULT hr = S_OK;
            LONGLONG llValue = 2;
            LONGLONG llEncryptedValue = 7;
            VRNTUTIL_VARIANT variant = { };

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(llValue, llEncryptedValue);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            VrntUninitializeHelper(&functions, &variant);
            variant.fValueIsEncrypted = TRUE;

            hr = VrntSetNumericHelper(&functions, &variant, llValue);
            NativeAssert::Succeeded(hr, "VrntSetNumericHelper failed.");
            NativeAssert::Equal(llEncryptedValue, variant.llValue);
            NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, variant.Type);
            NativeAssert::True(variant.fValueIsEncrypted);
        }

        [NamedFact]
        void VrntSetStringEncryptedTest()
        {
            HRESULT hr = S_OK;
            LPWSTR wzValue = L"First";
            LPWSTR wzEncryptedValue = L"Second";
            LPWSTR sczEncryptedValue = NULL;
            VRNTUTIL_VARIANT variant = { };

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(wzValue, wzEncryptedValue);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            try
            {
                hr = CrypAllocStringForEncryption(&sczEncryptedValue, wzEncryptedValue, 0, NULL);
                NativeAssert::Succeeded(hr, "CrypAllocStringForEncryption failed.");

                VrntUninitializeHelper(&functions, &variant);
                variant.fValueIsEncrypted = TRUE;

                hr = VrntSetStringHelper(&functions, &variant, wzValue, 0);
                NativeAssert::Succeeded(hr, "VrntSetStringHelper failed.");
                NativeAssert::StringEqual(sczEncryptedValue, variant.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, variant.Type);
                NativeAssert::True(variant.fValueIsEncrypted);
            }
            finally
            {
                VrntUninitializeHelper(&functions, &variant);
                ReleaseStr(sczEncryptedValue);
            }
        }

        [NamedFact]
        void VrntSetVersionEncryptedTest()
        {
            HRESULT hr = S_OK;
            DWORD64 qwValue = MAKEQWORDVERSION(1, 22, 333, 4444);
            DWORD64 qwEncryptedValue = MAKEQWORDVERSION(4, 33, 22, 1111);
            VRNTUTIL_VARIANT variant = { };

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(qwValue, qwEncryptedValue);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            VrntUninitializeHelper(&functions, &variant);
            variant.fValueIsEncrypted = TRUE;

            hr = VrntSetVersionHelper(&functions, &variant, qwValue);
            NativeAssert::Succeeded(hr, "VrntSetVersionHelper failed.");
            NativeAssert::Equal(qwEncryptedValue, variant.qwValue);
            NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, variant.Type);
            NativeAssert::True(variant.fValueIsEncrypted);
        }

        [NamedFact]
        void VrntSetValueEncryptedTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT source = { };
            VRNTUTIL_VARIANT target = { };
            LONGLONG llValue = 42;
            LONGLONG llEncryptedValue = 24;
            LPWSTR wzValue = L"Decrypted";
            LPWSTR wzEncryptedValue = L"Encrypted";
            DWORD64 qwValue = MAKEQWORDVERSION(4, 2, 4, 2);
            DWORD64 qwEncryptedValue = MAKEQWORDVERSION(2, 4, 2, 4);

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(llValue, llEncryptedValue);
            crypHelper->AddMapping(wzValue, wzEncryptedValue);
            crypHelper->AddMapping(qwValue, qwEncryptedValue);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            try
            {
                VrntUninitializeHelper(&functions, &source);
                source.fValueIsEncrypted = TRUE;
                hr = VrntSetNumericHelper(&functions, &source, llValue);
                NativeAssert::Succeeded(hr, "VrntSetNumericHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::Equal(llValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);
                NativeAssert::False(target.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::Equal(llEncryptedValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                hr = VrntSetEncryptionHelper(&functions, &source, FALSE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::Equal(llEncryptedValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);


                VrntUninitializeHelper(&functions, &source);
                source.fValueIsEncrypted = TRUE;
                hr = VrntSetStringHelper(&functions, &source, wzValue, 0);
                NativeAssert::Succeeded(hr, "VrntSetStringHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::StringEqual(wzValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);
                NativeAssert::False(target.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::StringEqual(wzEncryptedValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                hr = VrntSetEncryptionHelper(&functions, &source, FALSE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::StringEqual(wzEncryptedValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);


                VrntUninitializeHelper(&functions, &source);
                source.fValueIsEncrypted = TRUE;
                hr = VrntSetVersionHelper(&functions, &source, qwValue);
                NativeAssert::Succeeded(hr, "VrntSetVersionHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::Equal(qwValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);
                NativeAssert::False(target.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::Equal(qwEncryptedValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                hr = VrntSetEncryptionHelper(&functions, &source, FALSE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntSetValueHelper(&functions, &target, &source);
                NativeAssert::Succeeded(hr, "VrntSetValueHelper failed.");
                NativeAssert::Equal(qwEncryptedValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);
            }
            finally
            {
                VrntUninitializeHelper(&functions, &source);
                VrntUninitializeHelper(&functions, &target);
            }
        }

        [NamedFact]
        void VrntCopyEncryptedTest()
        {
            HRESULT hr = S_OK;
            VRNTUTIL_VARIANT source = { };
            VRNTUTIL_VARIANT target = { };
            LONGLONG llValue = 42;
            LONGLONG llEncryptedValue = 24;
            LPWSTR wzValue = L"Decrypted";
            LPWSTR wzEncryptedValue = L"Encrypted";
            DWORD64 qwValue = MAKEQWORDVERSION(4, 2, 4, 2);
            DWORD64 qwEncryptedValue = MAKEQWORDVERSION(2, 4, 2, 4);

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = VARIANT_ENCRYPTION_SCOPE;
            crypHelper->AddMapping(llValue, llEncryptedValue);
            crypHelper->AddMapping(wzValue, wzEncryptedValue);
            crypHelper->AddMapping(qwValue, qwEncryptedValue);

            VrntMockableFunctions functions =
            {
                StrSecureZeroFreeString,
                crypHelper->PfnRtlEncryptMemory,
                crypHelper->PfnRtlDecryptMemory,
            };

            try
            {
                VrntUninitializeHelper(&functions, &source);
                source.fValueIsEncrypted = TRUE;
                hr = VrntSetNumericHelper(&functions, &source, llValue);
                NativeAssert::Succeeded(hr, "VrntSetNumericHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::Equal(llEncryptedValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::Equal(llEncryptedValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                hr = VrntSetEncryptionHelper(&functions, &source, FALSE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::Equal(llValue, target.llValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_NUMERIC, target.Type);
                NativeAssert::False(target.fValueIsEncrypted);


                VrntUninitializeHelper(&functions, &source);
                source.fValueIsEncrypted = TRUE;
                hr = VrntSetStringHelper(&functions, &source, wzValue, 0);
                NativeAssert::Succeeded(hr, "VrntSetStringHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::StringEqual(wzEncryptedValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::StringEqual(wzEncryptedValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                hr = VrntSetEncryptionHelper(&functions, &source, FALSE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::StringEqual(wzValue, target.sczValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_STRING, target.Type);
                NativeAssert::False(target.fValueIsEncrypted);


                VrntUninitializeHelper(&functions, &source);
                source.fValueIsEncrypted = TRUE;
                hr = VrntSetVersionHelper(&functions, &source, qwValue);
                NativeAssert::Succeeded(hr, "VrntSetVersionHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::Equal(qwEncryptedValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::Equal(qwEncryptedValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);
                NativeAssert::True(target.fValueIsEncrypted);

                hr = VrntSetEncryptionHelper(&functions, &source, FALSE);
                NativeAssert::Succeeded(hr, "VrntSetEncryptionHelper failed.");

                VrntUninitializeHelper(&functions, &target);
                target.fValueIsEncrypted = TRUE;
                hr = VrntCopyHelper(&functions, &source, &target);
                NativeAssert::Succeeded(hr, "VrntCopyHelper failed.");
                NativeAssert::Equal(qwValue, target.qwValue);
                NativeAssert::Equal<DWORD>(VRNTUTIL_VARIANT_TYPE_VERSION, target.Type);
                NativeAssert::False(target.fValueIsEncrypted);
            }
            finally
            {
                VrntUninitializeHelper(&functions, &source);
                VrntUninitializeHelper(&functions, &target);
            }
        }
    };
}