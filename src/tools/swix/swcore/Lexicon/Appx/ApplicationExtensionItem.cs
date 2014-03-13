//-------------------------------------------------------------------------------------------------
// <copyright file="ApplicationExtensionItem.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Base class for all Appx application extension items.
    /// </summary>
    public abstract class ApplicationExtensionItem : PackageItem
    {
        /// <summary>
        /// Optional file that is launched to host the functionality.
        /// </summary>
        /// <remarks>Defaults to the parent Application/@File.</remarks>
        [TypeConverter(typeof(IdTypeConverter))]
        public File File { get; set; }

        /// <summary>
        /// Optional programmatic identifier that implements the functionality.
        /// </summary>
        /// <remarks>Defaults to the parent Application/@Name.</remarks>
        public string Implementation { get; set; }

        /// <summary>
        /// Optional value to specify the runtime provider.
        /// </summary>
        /// <remarks>Typically used in mixed framework native client applications.</remarks>
        public string RuntimeType { get; set; }
    }
}
