// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Nuget
{
    public class Metadata : PackageItem
    {
        public bool DevelopmentDependency { get; set; }

        public string ReleaseNotes { get; set; }

        public bool RequireLicenseAcceptance { get; set; }

        public string Summary { get; set; }

        // TODO: consider creating type converter to make this a string[]
        public string Tags { get; set; }
    }
}
