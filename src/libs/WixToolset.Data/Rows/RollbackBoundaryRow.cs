//-------------------------------------------------------------------------------------------------
// <copyright file="RollbackBoundaryRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the ChainMspPackage table.
    /// </summary>
    public sealed class RollbackBoundaryRow : Row
    {
        /// <summary>
        /// Creates a ChainMspPackage row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public RollbackBoundaryRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a RollbackBoundaryRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public RollbackBoundaryRow(SourceLineNumber sourceLineNumbers, Table table) :
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
        /// Gets or sets whether the package is vital.
        /// </summary>
        /// <value>Vitality of the package.</value>
        public YesNoType Vital
        {
            get { return (null == this.Fields[1].Data) ? YesNoType.NotSet : (YesNoType)this.Fields[1].Data; }
            set { this.Fields[1].Data = (int)value; }
        }
    }
}
