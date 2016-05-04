// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Test.Utility
{
    internal class VsixVerifier : OpcVerifier
    {
        private static readonly string[] ExpectedFiles = new string[] { "[Content_Types].xml", "extension.vsixmanifest" };

        public VsixVerifier Load(string folder)
        {
            return (VsixVerifier)base.Load(folder, ExpectedFiles);
        }

        public VsixVerifier Verify(string prefix)
        {
            return (VsixVerifier) base.Verify("test_vsix", prefix, ".vsix");
        }
    }
}
