//-------------------------------------------------------------------------------------------------
// <copyright file="BurnSdkIntegrationTest.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Base class for Burn SDK Integration tests.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace WixTest;
    using namespace Xunit;

    public ref class BurnSdkIntegrationTest : WixTestBase, IUseFixture<BurnTestFixture^>
    {
    public:
        BurnSdkIntegrationTest()
        {
        }

        void TestInitialize() override
        {
            WixTestBase::TestInitialize();

            HRESULT hr = S_OK;

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"BurnSdkIntegrationTest", NULL, L"txt", FALSE, FALSE, NULL);
            TestThrowOnFailure(hr, L"Failed to open log.");
        }

        void TestUninitialize() override
        {
            LogUninitialize(FALSE);

            WixTestBase::TestUninitialize();
        }

        virtual void SetFixture(BurnTestFixture^ fixture)
        {
            // Don't care about the fixture, just need it to be created and disposed.
            UNREFERENCED_PARAMETER(fixture);
        }
    }; 
}
}
}
}
}
