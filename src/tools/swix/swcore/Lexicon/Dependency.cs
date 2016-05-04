// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Item that defines a requirement on another package.
    /// </summary>
    public class Dependency : PackageItem
    {
        /// <summary>
        /// Name of the target of the dependency.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Publisher of the target of the dependency.
        /// </summary>
        /// <remarks>When building a .vsix package the Publisher defines the human readable name for the target.</remarks>
        public string Publisher { get; set; }

        /// <summary>
        /// Maximum version of the target that will satisfy the dependency.
        /// </summary>
        [TypeConverter(typeof(VersionTypeConverter))]
        public Version MaxVersion { get; set; }

        /// <summary>
        /// Minimum version of the target that will satisfy the dependency.
        /// </summary>
        [TypeConverter(typeof(VersionTypeConverter))]
        public Version Version { get; set; }
    }
}
