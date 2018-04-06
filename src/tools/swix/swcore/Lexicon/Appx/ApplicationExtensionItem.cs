// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
