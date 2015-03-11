//-------------------------------------------------------------------------------------------------
// <copyright file="CrypUtilHelper.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

static HRESULT DAPI CrypAllocStringForEncryption(
    __deref_out_z LPWSTR* pscz,
    __in_z LPWSTR wzSource,
    __in DWORD_PTR cchSource,
    __out_opt SIZE_T* pcbData
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbBuffer = 0;
    SIZE_T cbData = 0;
    DWORD_PTR cchData = 0;

    if (*pscz)
    {
        cbBuffer = MemSize(*pscz);

        if (-1 == cbBuffer)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Failed to get the size of the string.");
        }
    }

    if (!cchSource)
    {
        cchSource = lstrlenW(wzSource);
    }

    DWORD_PTR cchNeeded;
    hr = ::ULongPtrAdd(cchSource, 1, &cchNeeded); // add one for the null terminator.
    ExitOnFailure(hr, "Source string is too long.");

    hr = ::ULongPtrMult(cchNeeded, sizeof(WCHAR), &cbData);
    ExitOnFailure(hr, "Source string is too long.");

    DWORD remainder = cbData % CRYP_ENCRYPT_MEMORY_SIZE;
    DWORD extraNeeded = remainder ? CRYP_ENCRYPT_MEMORY_SIZE - remainder : 0;

    if ((MAXDWORD - extraNeeded) < cbData)
    {
        hr = E_INVALIDDATA;
        ExitOnFailure(hr, "Source string is too long.");
    }
    else if (extraNeeded)
    {
        cbData += extraNeeded;
    }

    cchData = cbData / sizeof(WCHAR);

    if (cbBuffer != cbData)
    {
        hr = StrAllocSecure(pscz, cchData);
        ExitOnFailure(hr, "Failed to allocate string.");
    }

    // Copy everything (the NULL terminator will be included).
    hr = ::StringCchCopyNExW(*pscz, cchData, wzSource, cchSource, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);

    if (pcbData)
    {
        *pcbData = cbData;
    }

LExit:
    return hr;
}

namespace DutilTests
{
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Runtime::InteropServices;
    using namespace System::Text;
    using namespace WixTest;

    public ref class CrypUtilHelper : EqualityComparer<array<Byte>^>
    {
    public:
        delegate NTSTATUS RtlEncryptMemoryDelegate(PVOID Memory, ULONG MemoryLength, ULONG OptionFlags);
        delegate NTSTATUS RtlDecryptMemoryDelegate(PVOID Memory, ULONG MemoryLength, ULONG OptionFlags);
        delegate BOOL CryptProtectMemoryDelegate(LPVOID pData, DWORD cbData, DWORD dwFlags);
        delegate BOOL CryptUnprotectMemoryDelegate(LPVOID pData, DWORD cbData, DWORD dwFlags);

        property PFN_RTLENCRYPTMEMORY PfnRtlEncryptMemory
        {
            PFN_RTLENCRYPTMEMORY get()
            {
                RtlEncryptMemoryDelegate^ remDelegate = gcnew RtlEncryptMemoryDelegate(this, &CrypUtilHelper::CustomRtlEncryptMemory);
                return reinterpret_cast<PFN_RTLENCRYPTMEMORY>((void*)Marshal::GetFunctionPointerForDelegate(remDelegate));
            }
        }

        property PFN_RTLDECRYPTMEMORY PfnRtlDecryptMemory
        {
            PFN_RTLDECRYPTMEMORY get()
            {
                RtlDecryptMemoryDelegate^ rdmDelegate = gcnew RtlDecryptMemoryDelegate(this, &CrypUtilHelper::CustomRtlDecryptMemory);
                return reinterpret_cast<PFN_RTLDECRYPTMEMORY>((void*)Marshal::GetFunctionPointerForDelegate(rdmDelegate));
            }
        }

        property PFN_CRYPTPROTECTMEMORY PfnCryptProtectMemory
        {
            PFN_CRYPTPROTECTMEMORY get()
            {
                CryptProtectMemoryDelegate^ cpmDelegate = gcnew CryptProtectMemoryDelegate(this, &CrypUtilHelper::CustomCryptProtectMemory);
                return reinterpret_cast<PFN_CRYPTPROTECTMEMORY>((void*)Marshal::GetFunctionPointerForDelegate(cpmDelegate));
            }
        }

        property PFN_CRYPTUNPROTECTMEMORY PfnCryptUnprotectMemory
        {
            PFN_CRYPTUNPROTECTMEMORY get()
            {
                CryptUnprotectMemoryDelegate^ cupmDelegate = gcnew CryptUnprotectMemoryDelegate(this, &CrypUtilHelper::CustomCryptUnprotectMemory);
                return reinterpret_cast<PFN_CRYPTUNPROTECTMEMORY>((void*)Marshal::GetFunctionPointerForDelegate(cupmDelegate));
            }
        }

        property DWORD ExpectedFlags;

        NTSTATUS CustomRtlEncryptMemory(PVOID Memory, ULONG MemoryLength, ULONG OptionFlags)
        {
            CustomCryptProtectMemory(Memory, MemoryLength, OptionFlags);

            return STATUS_SUCCESS;
        }

        NTSTATUS CustomRtlDecryptMemory(PVOID Memory, ULONG MemoryLength, ULONG OptionFlags)
        {
            CustomCryptUnprotectMemory(Memory, MemoryLength, OptionFlags);

            return STATUS_SUCCESS;
        }

        BOOL CustomCryptProtectMemory(LPVOID pData, DWORD cbData, DWORD dwFlags)
        {
            NativeAssert::Equal(this->ExpectedFlags, dwFlags);

            array<Byte>^ plainText = gcnew array<Byte>(cbData);
            Marshal::Copy((IntPtr)pData, plainText, 0, cbData);
            array<Byte>^ cipherText = encryptionMap[plainText];
            Marshal::Copy(cipherText, 0, (IntPtr)pData, cbData);

            return TRUE;
        }

        BOOL CustomCryptUnprotectMemory(LPVOID pData, DWORD cbData, DWORD dwFlags)
        {
            NativeAssert::Equal(this->ExpectedFlags, dwFlags);

            array<Byte>^ cipherText = gcnew array<Byte>(cbData);
            Marshal::Copy((IntPtr)pData, cipherText, 0, cbData);
            array<Byte>^ plainText = decryptionMap[cipherText];
            Marshal::Copy(plainText, 0, (IntPtr)pData, cbData);

            return TRUE;
        }

        void AddMapping(DWORD64 qwPlainText, DWORD64 qwCipherText)
        {
            NativeAssert::InRange<DWORD>(sizeof(DWORD64), 1, CRYP_ENCRYPT_MEMORY_SIZE);

            array<Byte>^ plainText = gcnew array<Byte>(CRYP_ENCRYPT_MEMORY_SIZE);
            Marshal::Copy((IntPtr)(&qwPlainText), plainText, 0, sizeof(DWORD64));

            array<Byte>^ cipherText = gcnew array<Byte>(CRYP_ENCRYPT_MEMORY_SIZE);
            Marshal::Copy((IntPtr)(&qwCipherText), cipherText, 0, sizeof(DWORD64));

            encryptionMap->Add(plainText, cipherText);
            decryptionMap->Add(cipherText, plainText);
        }

        void AddMapping(LPWSTR wzPlainText, LPWSTR wzCipherText)
        {
            HRESULT hr = 0;
            LPWSTR sczPlainText = NULL;
            LPWSTR sczCipherText = NULL;
            SIZE_T cbPlainText = 0;
            SIZE_T cbCipherText = 0;

            try
            {
                hr = CrypAllocStringForEncryption(&sczPlainText, wzPlainText, 0, &cbPlainText);
                NativeAssert::Succeeded(hr, "CrypAllocStringForEncryption failed.");

                hr = CrypAllocStringForEncryption(&sczCipherText, wzCipherText, 0, &cbCipherText);
                NativeAssert::Succeeded(hr, "CrypAllocStringForEncryption failed.");

                NativeAssert::Equal(cbPlainText, cbCipherText);

                array<Byte>^ plainText = gcnew array<Byte>(cbPlainText);
                Marshal::Copy((IntPtr)sczPlainText, plainText, 0, cbPlainText);

                array<Byte>^ cipherText = gcnew array<Byte>(cbCipherText);
                Marshal::Copy((IntPtr)sczCipherText, cipherText, 0, cbCipherText);

                encryptionMap->Add(plainText, cipherText);
                decryptionMap->Add(cipherText, plainText);
            }
            finally
            {
                ReleaseStr(sczPlainText);
                ReleaseStr(sczCipherText);
            }
        }

        bool Equals(array<Byte>^ x, array<Byte>^ y) override
        {
            return System::Collections::StructuralComparisons::StructuralEqualityComparer->Equals(x, y);
        }

        int GetHashCode(array<Byte>^ x) override
        {
            String^ s = Encoding::Unicode->GetString(x);
            return s->GetHashCode();
        }

    private:
        Dictionary<array<Byte>^, array<Byte>^>^ encryptionMap = gcnew Dictionary<array<Byte>^, array<Byte>^>(this);
        Dictionary<array<Byte>^, array<Byte>^>^ decryptionMap = gcnew Dictionary<array<Byte>^, array<Byte>^>(this);
    };
}