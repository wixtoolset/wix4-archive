//-------------------------------------------------------------------------------------------------
// <copyright file="DifxAppExtensionData.cs" company="Outercurve Foundation">
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
    /// The WiX Toolset Driver Install Frameworks for Applications Extension.
    /// </summary>
    public sealed class DifxAppExtensionData : ExtensionData
    {
        private static TableDefinitionCollection tableDefinitions;

        /// <summary>
        /// Gets the optional table definitions for this extension.
        /// </summary>
        /// <value>The optional table definitions for this extension.</value>
        public override TableDefinitionCollection TableDefinitions
        {
            get
            {
                return DifxAppExtensionData.GetExtensionTableDefinitions();
            }
        }

        /// <summary>
        /// Internal mechanism to access the extension's table definitions.
        /// </summary>
        /// <returns>Extension's table definitions.</returns>
        internal static TableDefinitionCollection GetExtensionTableDefinitions()
        {
            if (null == DifxAppExtensionData.tableDefinitions)
            {
                DifxAppExtensionData.tableDefinitions = ExtensionData.LoadTableDefinitionHelper(Assembly.GetExecutingAssembly(), "WixToolset.Extensions.Data.tables.xml");
            }

            return DifxAppExtensionData.tableDefinitions;
        }
    }
}
