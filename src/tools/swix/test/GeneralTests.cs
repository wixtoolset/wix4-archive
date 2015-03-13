//-------------------------------------------------------------------------------------------------
// <copyright file="GeneralTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using WixToolset.Simplified.Test.Utility;
using Xunit;

namespace WixToolset.Simplified.Test
{
    public class GeneralTests
    {
        [Fact]
        public void TestHelp()
        {
            SwcTool swc = new SwcTool().Help();
            Assert.Empty(swc.Errors);
        }
    }
}