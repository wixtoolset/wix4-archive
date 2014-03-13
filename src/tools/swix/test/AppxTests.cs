//-------------------------------------------------------------------------------------------------
// <copyright file="AppxTests.cs" company="Outercurve Foundation">
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
    public class AppxTests
    {
        [Fact]
        public void BackgroundTask()
        {
            AppxVerifier verifier = new AppxVerifier().Load("Data\\appx\\backgroundtask");
            verifier.Verify("backgroundtask");
        }

        [Fact]
        public void Charms()
        {
            AppxVerifier verifier = new AppxVerifier().Load("Data\\appx\\charms");
            verifier.Verify("charms");
        }

        [Fact]
        public void VersionWithInsignificantZeros()
        {
            AppxVerifier verifier = new AppxVerifier().Load("Data\\appx\\version_with_insignificant_zeros");
            verifier.Verify("version_with_insignificant_zeros");
        }

        [Fact]
        public void Wwa()
        {
            AppxVerifier verifier = new AppxVerifier().Load("Data\\appx\\wwa");
            verifier.Verify("wwa");
        }
    }
}
