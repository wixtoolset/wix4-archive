//-------------------------------------------------------------------------------------------------
// <copyright file="DirectXExtensionData.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensions
{
    using System;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The WiX Toolset DirectX Extension.
    /// </summary>
    public sealed class DirectXExtensionData : ExtensionData
    {
        /// <summary>
        /// Gets the library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <param name="allowIncompleteSections">Whether a WixMissingTableDefinitionException should be thrown if a section has a table without a table definition.</param>
        /// <returns>The loaded library.</returns>
        public override Library GetLibrary(TableDefinitionCollection tableDefinitions, bool allowIncompleteSections)
        {
            return DirectXExtensionData.GetExtensionLibrary(tableDefinitions, allowIncompleteSections);
        }

        /// <summary>
        /// Internal mechanism to access the extension's library.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <param name="allowIncompleteSections">Whether a WixMissingTableDefinitionException should be thrown if a section has a table without a table definition.</param>
        /// <returns>Extension's library.</returns>
        internal static Library GetExtensionLibrary(TableDefinitionCollection tableDefinitions, bool allowIncompleteSections = false)
        {
            return ExtensionData.LoadLibraryHelper(Assembly.GetExecutingAssembly(), "WixToolset.Extensions.Data.DirectX.wixlib", tableDefinitions, allowIncompleteSections);
        }
    }
}
