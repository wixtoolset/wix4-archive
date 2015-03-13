//-------------------------------------------------------------------------------------------------
// <copyright file="WixUnitTestBaseFixture.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
