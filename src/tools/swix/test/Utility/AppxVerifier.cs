// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
