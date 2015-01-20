//-------------------------------------------------------------------------------------------------
// <copyright file="GuidUtilTests.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixTest;

namespace DutilTests
{
    public ref class GuidUtil
    {
    public:
        [Fact]
        void GuidCreateTest()
        {
            HRESULT hr = S_OK;
            WCHAR wzGuid1[GUID_STRING_LENGTH];
            WCHAR wzGuid2[GUID_STRING_LENGTH];

            hr = GuidCreate(wzGuid1);
            NativeAssert::Succeeded(hr, "Failed to create first guid.");

            hr = GuidCreate(wzGuid2);
            NativeAssert::Succeeded(hr, "Failed to create second guid.");

            NativeAssert::NotStringEqual(wzGuid1, wzGuid2);
        }

        [Fact]
        void GuidCreateSczTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczGuid1 = NULL;
            LPWSTR sczGuid2 = NULL;

            try
            {
                hr = GuidCreateScz(&sczGuid1);
                NativeAssert::Succeeded(hr, "Failed to create first guid.");

                hr = GuidCreateScz(&sczGuid2);
                NativeAssert::Succeeded(hr, "Failed to create second guid.");

                NativeAssert::NotStringEqual(sczGuid1, sczGuid2);
            }
            finally
            {
                ReleaseStr(sczGuid1);
                ReleaseStr(sczGuid2);
            }
        }
    };
}
