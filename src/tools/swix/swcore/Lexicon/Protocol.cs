// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Defines a protocol handler.
    /// </summary>
    public class Protocol : PackageItemTargetsFile
    {
        public string DisplayName { get; set; }

        /// <summary>
        /// Image displayed with the protocol.
        /// </summary>
        [TypeConverter(typeof(IdTypeConverter))]
        public File Image { get; set; }

        public string Implementation { get; set; }

        /// <summary>
        /// Name for the protocol handler.
        /// </summary>
        public string Name { get; set; }
    }
}
