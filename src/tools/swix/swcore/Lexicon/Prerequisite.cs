//-------------------------------------------------------------------------------------------------
// <copyright file="Prerequisite.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
