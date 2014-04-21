//-------------------------------------------------------------------------------------------------
// <copyright file="Metadata.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
