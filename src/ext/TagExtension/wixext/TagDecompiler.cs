// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
        public override void DecompileTable(Table table)
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
        private void DecompileSoftwareIdentificationTag(Table table)
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
