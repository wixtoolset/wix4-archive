//-------------------------------------------------------------------------------------------------
// <copyright file="AppxVerifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Test.Utility
{
    internal class AppxVerifier : OpcVerifier
    {
        private static readonly string[] ExpectedFiles = new string[] { "[Content_Types].xml", "AppxBlockmap.xml", "AppxManifest.xml" };

        public AppxVerifier Load(string folder)
        {
            return (AppxVerifier)base.Load(folder, ExpectedFiles);
        }

        public AppxVerifier Verify(string prefix)
        {
            return (AppxVerifier)base.Verify("test_appx", prefix, ".appx");
        }
    }
}
