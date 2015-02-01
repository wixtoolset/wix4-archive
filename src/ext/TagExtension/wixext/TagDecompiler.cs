//-------------------------------------------------------------------------------------------------
// <copyright file="TagDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the WiX Toolset Software Id Tag Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensions
{
    using System;
    using WixToolset;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Tag = WixToolset.Extensions.Serialize.Tag;

    /// <summary>
    /// The Binder for the WiX Toolset Software Id Tag Extension.
    /// </summary>
    public sealed class TagDecompiler : DecompilerExtension
    {
       /// <summary>
        /// Creates a decompiler for Tag Extension.
        /// </summary>
        public TagDecompiler()
        {
            this.TableDefinitions = TagExtensionData.GetExtensionTableDefinitions();
        }

        /// <summary>
        /// Get the extensions library to be removed.
        /// </summary>
        /// <param name="tableDefinitions">Table definitions for library.</param>
        /// <returns>Library to remove from decompiled output.</returns>
        public override Library GetLibraryToRemove(TableDefinitionCollection tableDefinitions)
        {
            return TagExtensionData.GetExtensionLibrary(tableDefinitions);
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(ITable table)
        {
            switch (table.Name)
            {
                case "SoftwareIdentificationTag":
                    this.DecompileSoftwareIdentificationTag(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the SoftwareIdentificationTag table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSoftwareIdentificationTag(ITable table)
        {
            foreach (Row row in table.Rows)
            {
                Tag.Tag tag= new Tag.Tag();

                tag.Regid = (string)row[1];
                tag.Name = (string)row[2];
                tag.Licensed = null == row[3] ? Tag.YesNoType.NotSet : 1 == (int)row[3] ? Tag.YesNoType.yes : Tag.YesNoType.no;

                this.Core.RootElement.AddChild(tag);
            }
        }
    }
}
