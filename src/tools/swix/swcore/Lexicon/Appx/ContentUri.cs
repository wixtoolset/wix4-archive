//-------------------------------------------------------------------------------------------------
// <copyright file="ContentUri.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Appx
{
    public enum ContentUriRule
    {
        exclude,
        include,
    }

    public class ContentUri : PackageItem
    {
        public ContentUriRule Rule { get; set; }

        public string Match { get; set; }
    }
}
