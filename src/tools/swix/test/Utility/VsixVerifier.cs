//-------------------------------------------------------------------------------------------------
// <copyright file="VsixVerifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
