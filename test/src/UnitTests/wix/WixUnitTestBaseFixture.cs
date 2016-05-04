// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.WixUnitTest
{
    using WixToolset.Data;

    /// <summary>
    /// Base class for all WixUnitTest fixtures.
    /// </summary>
    public abstract class WixUnitTestBaseFixture
    {
        /// <summary>
        /// Base constructor that ensures the messaging infrastructure is always reset between tests.
        /// </summary>
        public WixUnitTestBaseFixture()
        {
            Messaging.Instance.InitializeAppName(null, null);
        }
    }
}
