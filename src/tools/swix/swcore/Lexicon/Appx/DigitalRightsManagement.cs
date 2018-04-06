// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    public class DigitalRightsManagement : PackageItem
    {
        [TypeConverter(typeof(IdTypeConverter))]
        public File File { get; set; }
    }
}
