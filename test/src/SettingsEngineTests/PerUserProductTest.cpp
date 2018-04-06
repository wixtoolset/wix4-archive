// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class PerUserProduct : public CfgTest
    {
    public:
        void ExpectProduct(CFGDB_HANDLE cdbHandle, DWORD dwIndex, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey, BOOL fRegistered)
        {
            HRESULT hr = S_OK;
            LPCWSTR wzString = NULL;
            DWORD dwCount = 0;
            BOOL fBool = FALSE;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;

            hr = CfgEnumerateProducts(cdbHandle, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate products in user DB");

            if (dwIndex >= dwCount)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Planned to check product at index %u, but only %u products exist in the data store!", dwIndex, dwCount);
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_PRODUCTNAME, &wzString);
            ExitOnFailure(hr, "Failed to read product name from enumeration");

            if (0 != lstrcmpW(wzString, wzProductName))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected product name: %ls, found product name:%ls", wzProductName, wzString);
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_VERSION, &wzString);
            ExitOnFailure(hr, "Failed to read product version from enumeration");

            if (0 != lstrcmpW(wzString, wzVersion))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected version: %ls, found version:%ls", wzVersion, wzString);
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_PUBLICKEY, &wzString);
            ExitOnFailure(hr, "Failed to read product publickey from enumeration");

            if (0 != lstrcmpW(wzString, wzPublicKey))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected public key: %ls, found public key:%ls", wzPublicKey, wzString);
            }

            hr = CfgEnumReadBool(cehHandle, dwIndex, ENUM_DATA_REGISTERED, &fBool);
            ExitOnFailure(hr, "Failed to read product registered flag from enumeration");

            if (fRegistered != fBool)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected registered flag to be %ls, but it was %ls!", fRegistered ? L"TRUE" : L"FALSE", fBool ? L"TRUE" : L"FALSE");
            }

        LExit:
            CfgReleaseEnumeration(cehHandle);
        }

        void ExpectNumberOfProductsRegistered(CFGDB_HANDLE cdbHandle, DWORD dwExpectedNumber)
        {
            HRESULT hr = S_OK;
            DWORD dwCount = 0;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;

            hr = CfgEnumerateProducts(cdbHandle, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate products in admin DB");

            if (dwExpectedNumber != dwCount)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected %u products registered, but found %u products!", dwExpectedNumber, dwCount);
            }

        LExit:
            CfgReleaseEnumeration(cehHandle);
        }

        [Fact]
        void PerUserProductTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczString = NULL;
            BOOL fRegistered = FALSE;
            CFGDB_HANDLE cdhLocal = NULL;

            TestInitialize();

            hr = CfgInitialize(&cdhLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(m_pContext));
            ExitOnFailure(hr, "Failed to initialize admin settings engine");

            hr = CfgResumeBackgroundThread(cdhLocal);
            ExitOnFailure(hr, "Failed to resume background thread");

            hr = CfgIsProductRegistered(cdhLocal, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", &fRegistered);
            ExitOnFailure(hr, "Failed to check if product is registered");
            if (FALSE != fRegistered)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Product shouldn't be registered at this time!");
            }

            ExpectNumberOfProductsRegistered(cdhLocal, 1);

            hr = CfgRegisterProduct(cdhLocal, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567");
            ExitOnFailure(hr, "Failed to register product");

            hr = CfgIsProductRegistered(cdhLocal, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", &fRegistered);
            ExitOnFailure(hr, "Failed to check if product is registered");
            if (TRUE != fRegistered)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Product should be registered at this time!");
            }

            ExpectNumberOfProductsRegistered(cdhLocal, 2);
            ExpectProduct(cdhLocal, 1, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", TRUE);

            hr = CfgRegisterProduct(cdhLocal, L"TestProduct2", L"1.0.0.0", L"abcdabcd01234567");
            ExitOnFailure(hr, "Failed to register product");

            ExpectNumberOfProductsRegistered(cdhLocal, 3);
            ExpectProduct(cdhLocal, 1, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", TRUE);
            ExpectProduct(cdhLocal, 2, L"TestProduct2", L"1.0.0.0", L"abcdabcd01234567", TRUE);

            hr = CfgUnregisterProduct(cdhLocal, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567");
            ExitOnFailure(hr, "Failed to register product");

            hr = CfgIsProductRegistered(cdhLocal, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", &fRegistered);
            ExitOnFailure(hr, "Failed to check if product is registered");
            if (FALSE != fRegistered)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Product shouldn't be registered at this time!");
            }

            // For per-user app registration, a flag is cleared on unregistration, but the product will still show up in enumerations
            ExpectNumberOfProductsRegistered(cdhLocal, 3);
            ExpectProduct(cdhLocal, 1, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", FALSE);
            ExpectProduct(cdhLocal, 2, L"TestProduct2", L"1.0.0.0", L"abcdabcd01234567", TRUE);

            hr = CfgUninitialize(cdhLocal);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            TestUninitialize();

        LExit:
            ReleaseStr(sczString);
        }
    };
}
