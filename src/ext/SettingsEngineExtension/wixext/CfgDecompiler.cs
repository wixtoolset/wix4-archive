//-------------------------------------------------------------------------------------------------
// <copyright file="CfgDecompiler.cs" company="Outercurve Foundation">
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
    using Cfg = WixToolset.Extensions.Serialize.Cfg;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset Cfg Extension.
    /// </summary>
    public sealed class CfgDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Creates a decompiler for Cfg Extension.
        /// </summary>
        public CfgDecompiler()
        {
            this.TableDefinitions = CfgExtensionData.GetExtensionTableDefinitions();
        }

        /// <summary>
        /// Get the extensions library to be removed.
        /// </summary>
        /// <param name="tableDefinitions">Table definitions for library.</param>
        /// <returns>Library to remove from decompiled output.</returns>
        public override Library GetLibraryToRemove(TableDefinitionCollection tableDefinitions)
        {
            return CfgExtensionData.GetExtensionLibrary(tableDefinitions);
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                default:
                    base.DecompileTable(table);
                    break;
            }
        }
    }
}
