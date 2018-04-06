// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
