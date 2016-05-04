// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
