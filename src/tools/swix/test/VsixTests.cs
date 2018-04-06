// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using WixToolset.Simplified.Test.Utility;
using Xunit;

namespace WixToolset.Simplified.Test
{
    public class VsixTests
    {
        [Fact]
        public void Swix()
        {
            VsixVerifier verifier = new VsixVerifier().Load("Data\\vsix\\swix");
            verifier.Verify("swix");
        }
    }
}
