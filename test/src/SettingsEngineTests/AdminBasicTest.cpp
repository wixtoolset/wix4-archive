// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;

namespace CfgTests
{
    public ref class AdminBasic : public CfgTest
    {
    public:
        void ExpectProduct(CFGDB_HANDLE cdbHandle, DWORD dwIndex, LPCWSTR wzProductName, LPCWSTR wzVersion, LPCWSTR wzPublicKey)
        {
            HRESULT hr = S_OK;
            LPCWSTR wzString = NULL;
            DWORD dwCount = 0;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;

            hr = CfgAdminEnumerateProducts(cdbHandle, NULL, &cehHandle, &dwCount);
            ExitOnFailure(hr, "Failed to enumerate products in admin DB");

            if (dwIndex >= dwCount)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Planned to check product at index %u, but only %u products exist in the data store!", dwIndex, dwCount);
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_PRODUCTNAME, &wzString);
            if (0 != lstrcmpW(wzString, wzProductName))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected product name: %ls, found product name:%ls", wzProductName, wzString);
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_VERSION, &wzString);
            if (0 != lstrcmpW(wzString, wzVersion))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected version: %ls, found version:%ls", wzVersion, wzString);
            }

            hr = CfgEnumReadString(cehHandle, dwIndex, ENUM_DATA_PUBLICKEY, &wzString);
            if (0 != lstrcmpW(wzString, wzPublicKey))
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Expected public key: %ls, found public key:%ls", wzPublicKey, wzString);
            }

        LExit:
            CfgReleaseEnumeration(cehHandle);
        }

        void ExpectNumberOfProductsRegistered(CFGDB_HANDLE cdbHandle, DWORD dwExpectedNumber)
        {
            HRESULT hr = S_OK;
            DWORD dwCount = 0;
            CFG_ENUMERATION_HANDLE cehHandle = NULL;

            hr = CfgAdminEnumerateProducts(cdbHandle, NULL, &cehHandle, &dwCount);
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
        void AdminBasicTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczString = NULL;
            BOOL fRegistered = FALSE;
            CFGDB_HANDLE cdhAdmin = NULL;

            TestInitialize();

            hr = CfgAdminInitialize(&cdhAdmin, TRUE);
            ExitOnFailure(hr, "Failed to initialize admin settings engine");

            hr = CfgAdminIsProductRegistered(cdhAdmin, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", &fRegistered);
            ExitOnFailure(hr, "Failed to check if product is registered");
            if (FALSE != fRegistered)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Product shouldn't be registered at this time!");
            }

            ExpectNumberOfProductsRegistered(cdhAdmin, 0);

            hr = CfgAdminRegisterProduct(cdhAdmin, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567");
            ExitOnFailure(hr, "Failed to register product");

            hr = CfgAdminIsProductRegistered(cdhAdmin, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", &fRegistered);
            ExitOnFailure(hr, "Failed to check if product is registered");
            if (TRUE != fRegistered)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Product should be registered at this time!");
            }

            ExpectNumberOfProductsRegistered(cdhAdmin, 1);
            ExpectProduct(cdhAdmin, 0, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567");

            hr = CfgAdminRegisterProduct(cdhAdmin, L"TestProduct2", L"1.0.0.0", L"abcdabcd01234567");
            ExitOnFailure(hr, "Failed to register product");

            ExpectNumberOfProductsRegistered(cdhAdmin, 2);
            ExpectProduct(cdhAdmin, 0, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567");
            ExpectProduct(cdhAdmin, 1, L"TestProduct2", L"1.0.0.0", L"abcdabcd01234567");

            hr = CfgAdminUnregisterProduct(cdhAdmin, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567");
            ExitOnFailure(hr, "Failed to register product");

            hr = CfgAdminIsProductRegistered(cdhAdmin, L"TestProduct", L"1.0.0.0", L"abcdabcd01234567", &fRegistered);
            ExitOnFailure(hr, "Failed to check if product is registered");
            if (FALSE != fRegistered)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Product shouldn't be registered at this time!");
            }

            ExpectNumberOfProductsRegistered(cdhAdmin, 1);
            ExpectProduct(cdhAdmin, 0, L"TestProduct2", L"1.0.0.0", L"abcdabcd01234567");

            hr = CfgAdminUninitialize(cdhAdmin);
            ExitOnFailure(hr, "Failed to shutdown user settings engine");

            TestUninitialize();

        LExit:
            ReleaseStr(sczString);
        }
    };
}
