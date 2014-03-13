//-------------------------------------------------------------------------------------------------
// <copyright file="Protocol.cs" company="Outercurve Foundation">
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
