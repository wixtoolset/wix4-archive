//-------------------------------------------------------------------------------------------------
// <copyright file="GamingExtensionData.cs" company="Outercurve Foundation">
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

    public sealed class GamingExtensionData : ExtensionData
    {
        /// <summary>
        /// Gets the optional table definitions for this extension.
        /// </summary>
        /// <value>The optional table definitions for this extension.</value>
        public override TableDefinitionCollection TableDefinitions
        {
            get
            {
                return GamingExtensionData.GetExtensionTableDefinitions();
            }
        }

        /// <summary>
        /// Gets the library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <param name="allowIncompleteSections">Whether a WixMissingTableDefinitionException should be thrown if a section has a table without a table definition.</param>
        /// <returns>The loaded library.</returns>
        public override Library GetLibrary(TableDefinitionCollection tableDefinitions, bool allowIncompleteSections)
        {
            return GamingExtensionData.GetExtensionLibrary(tableDefinitions, allowIncompleteSections);
        }

        /// <summary>
        /// Internal mechanism to access the extension's table definitions.
        /// </summary>
        /// <returns>Extension's table definitions.</returns>
        internal static TableDefinitionCollection GetExtensionTableDefinitions()
        {
            return ExtensionData.LoadTableDefinitionHelper(Assembly.GetExecutingAssembly(), "WixToolset.Extensions.Data.tables.xml");
        }

        /// <summary>
        /// Internal mechanism to access the extension's library.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <param name="allowIncompleteSections">Whether a WixMissingTableDefinitionException should be thrown if a section has a table without a table definition.</param>
        /// <returns>Extension's library.</returns>
        internal static Library GetExtensionLibrary(TableDefinitionCollection tableDefinitions, bool allowIncompleteSections = false)
        {
            return ExtensionData.LoadLibraryHelper(Assembly.GetExecutingAssembly(), "WixToolset.Extensions.Data.gaming.wixlib", tableDefinitions, allowIncompleteSections);
        }
    }
}
