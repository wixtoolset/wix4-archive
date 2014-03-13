//-------------------------------------------------------------------------------------------------
// <copyright file="UIDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The decompiler for the WiX Toolset UI Extension.
    /// </summary>
    public sealed class UIDecompiler : DecompilerExtension
    {
        private bool removeLibraryRows;

        /// <summary>
        /// Get the extensions library to be removed.
        /// </summary>
        /// <param name="tableDefinitions">Table definitions for library.</param>
        /// <returns>Library to remove from decompiled output.</returns>
        public override Library GetLibraryToRemove(TableDefinitionCollection tableDefinitions)
        {
            return removeLibraryRows ? UIExtensionData.GetExtensionLibrary(tableDefinitions) : null;
        }

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void Initialize(TableIndexedCollection tables)
        {
            Table propertyTable = tables["Property"];

            if (null != propertyTable)
            {
                foreach (Row row in propertyTable.Rows)
                {
                    if ("WixUI_Mode" == (string)row[0])
                    {
                        Wix.UIRef uiRef = new Wix.UIRef();

                        uiRef.Id = String.Concat("WixUI_", (string)row[1]);

                        this.Core.RootElement.AddChild(uiRef);
                        this.removeLibraryRows = true;

                        break;
                    }
                }
            }
        }
    }
}
