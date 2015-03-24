//-------------------------------------------------------------------------------------------------
// <copyright file="BurnTestFixture.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
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

    public ref class BurnTestFixture
    {
    public:
        BurnTestFixture()
        {
            HRESULT hr = XmlInitialize();
            TestThrowOnFailure(hr, L"Failed to initialize XML support.");

            hr = RegInitialize();
            TestThrowOnFailure(hr, L"Failed to initialize Regutil.");

            PlatformInitialize();
        }

        ~BurnTestFixture()
        {
            XmlUninitialize();
            RegUninitialize();
        }
    };
}
}
}
}
}
