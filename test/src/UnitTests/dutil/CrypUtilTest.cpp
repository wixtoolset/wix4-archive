//-------------------------------------------------------------------------------------------------
// <copyright file="CrypUtilTest.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include <cryputilhelpers.h>

namespace DutilTests
{
    using namespace System;
    using namespace Xunit;
    using namespace WixTest;

    public ref class CrypUtil
    {
    public:
        [NamedFact]
        void CrypUtilEncryptionDecryptionTest()
        {
            HRESULT hr = S_OK;
            DWORD encryptionFlags = CRYPTPROTECTMEMORY_SAME_LOGON;
            DWORD cbData = CRYP_ENCRYPT_MEMORY_SIZE * 2;
            LPVOID pData = NULL;
            LPWSTR wzPlainText = L"IAmPlain";
            LPWSTR wzCipherText = L"Ciphered";

            CrypUtilHelper^ crypHelper = gcnew CrypUtilHelper();
            crypHelper->ExpectedFlags = encryptionFlags;
            crypHelper->AddMapping(wzPlainText, wzCipherText);
            CrypUtilFunctions functions = { NULL, NULL, NULL, NULL };

            try
            {
                pData = MemAlloc(cbData, TRUE);
                NativeAssert::NotPointerEqual(NULL, pData);

                hr = CrypEncryptMemory(pData, cbData, encryptionFlags);
                NativeAssert::Equal(E_INVALIDSTATE, hr);

                hr = CrypDecryptMemory(pData, cbData, encryptionFlags);
                NativeAssert::Equal(E_INVALIDSTATE, hr);

                functions.pfnCryptProtectMemory = NULL;
                functions.pfnCryptUnprotectMemory = NULL;
                functions.pfnRtlEncryptMemory = crypHelper->PfnRtlEncryptMemory;
                functions.pfnRtlDecryptMemory = crypHelper->PfnRtlDecryptMemory;

                hr = CrypEncryptMemoryHelper(&functions, pData, cbData + 1, encryptionFlags);
                NativeAssert::Equal(E_INVALIDARG, hr);

                hr = CrypDecryptMemoryHelper(&functions, pData, cbData + 1, encryptionFlags);
                NativeAssert::Equal(E_INVALIDARG, hr);

                hr = ::StringCchCopyNExW(reinterpret_cast<LPWSTR>(pData), cbData / sizeof(WCHAR), wzPlainText, wcslen(wzPlainText), NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
                NativeAssert::Succeeded(hr, "StringCchCopyNExW failed.");

                hr = CrypEncryptMemoryHelper(&functions, pData, cbData, encryptionFlags);
                NativeAssert::Succeeded(hr, "CrypEncryptMemory failed.");
                NativeAssert::StringEqual(wzCipherText, (LPWSTR)pData);

                hr = CrypDecryptMemoryHelper(&functions, pData, cbData, encryptionFlags);
                NativeAssert::Succeeded(hr, "CrypDecryptMemory failed.");
                NativeAssert::StringEqual(wzPlainText, (LPWSTR)pData);

                functions.pfnCryptProtectMemory = crypHelper->PfnCryptProtectMemory;
                functions.pfnCryptUnprotectMemory = crypHelper->PfnCryptUnprotectMemory;
                functions.pfnRtlEncryptMemory = NULL;
                functions.pfnRtlDecryptMemory = NULL;

                hr = ::StringCchCopyNExW(reinterpret_cast<LPWSTR>(pData), cbData / sizeof(WCHAR), wzPlainText, wcslen(wzPlainText), NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
                NativeAssert::Succeeded(hr, "StringCchCopyNExW failed.");

                hr = CrypEncryptMemoryHelper(&functions, pData, cbData, encryptionFlags);
                NativeAssert::Succeeded(hr, "CrypEncryptMemory failed.");
                NativeAssert::StringEqual(wzCipherText, (LPWSTR)pData);

                hr = CrypDecryptMemoryHelper(&functions, pData, cbData, encryptionFlags);
                NativeAssert::Succeeded(hr, "CrypDecryptMemory failed.");
                NativeAssert::StringEqual(wzPlainText, (LPWSTR)pData);
            }
            finally
            {
                ReleaseMem(pData);
            }
        }
    };
}