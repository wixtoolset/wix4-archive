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

            hr = GuidFixedCreate(wzGuid1);
            NativeAssert::Succeeded(hr, "Failed to create first guid.");
            Guid firstGuid = Guid::Parse(gcnew String(wzGuid1));

            hr = GuidFixedCreate(wzGuid2);
            NativeAssert::Succeeded(hr, "Failed to create second guid.");
            Guid secondGuid = Guid::Parse(gcnew String(wzGuid2));

            NativeAssert::NotStringEqual(wzGuid1, wzGuid2);
            NativeAssert::NotEqual(firstGuid, secondGuid);
        }

        [Fact]
        void GuidCreateSczTest()
        {
            HRESULT hr = S_OK;
            LPWSTR sczGuid1 = NULL;
            LPWSTR sczGuid2 = NULL;

            try
            {
                hr = GuidCreate(&sczGuid1);
                NativeAssert::Succeeded(hr, "Failed to create first guid.");
                Guid firstGuid = Guid::Parse(gcnew String(sczGuid1));

                hr = GuidCreate(&sczGuid2);
                NativeAssert::Succeeded(hr, "Failed to create second guid.");
                Guid secondGuid = Guid::Parse(gcnew String(sczGuid2));

                NativeAssert::NotStringEqual(sczGuid1, sczGuid2);
                NativeAssert::NotEqual(firstGuid, secondGuid);
            }
            finally
            {
                ReleaseStr(sczGuid1);
                ReleaseStr(sczGuid2);
            }
        }
    };
}
