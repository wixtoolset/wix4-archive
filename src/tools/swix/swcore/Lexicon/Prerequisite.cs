// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    public class Prerequisite : PackageItem
    {
        /// <summary>
        /// Maximum version of the target that will satisfy the prerequisite.
        /// </summary>
        [TypeConverter(typeof(VersionTypeConverter))]
        public Version MaxVersion { get; set; }

        /// <summary>
        /// Name of platform the prequisite targets. For example: os, netfx, nuget or vs.
        /// </summary>
        public string On { get; set; }

        /// <summary>
        /// Minimum version required to satisfy the prerequisite.
        /// </summary>
        [TypeConverter(typeof(VersionTypeConverter))]
        public Version Version { get; set; }
    }
}
