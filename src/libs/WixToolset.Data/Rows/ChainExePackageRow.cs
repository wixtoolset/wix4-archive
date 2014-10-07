//-------------------------------------------------------------------------------------------------
// <copyright file="ChainExePackageRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the ChainExePackage table.
    /// </summary>
    public sealed class ChainExePackageRow : Row
    {
        /// <summary>
        /// Creates a ChainExePackage row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public ChainExePackageRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a ChainExePackageRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public ChainExePackageRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the foreign key identifier to the ChainPackage row.
        /// </summary>
        public string ChainPackageId
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the raw Exe attributes of a patch.
        /// </summary>
        public ChainExePackageAttributes Attributes
        {
            get { return (ChainExePackageAttributes)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the protcol for the executable package.
        /// </summary>
        public string DetectCondition
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the install command for the executable package.
        /// </summary>
        public string InstallCommand
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the repair command for the executable package.
        /// </summary>
        public string RepairCommand
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the uninstall command for the executable package.
        /// </summary>
        public string UninstallCommand
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the protcol for the executable package.
        /// </summary>
        public string ExeProtocol
        {
            get { return (string)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }

        /// <summary>
        /// Gets whether the executable package is repairable.
        /// </summary>
        public bool Repairable
        {
            get { return 0 != (this.Attributes & ChainExePackageAttributes.Repairable); }
        }
    }
}
