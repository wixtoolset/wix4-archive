//-------------------------------------------------------------------------------------------------
// <copyright file="WixChainRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the WixChain table.
    /// </summary>
    public sealed class WixChainRow : Row
    {
        /// <summary>
        /// Creates a WixChain row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public WixChainRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixChainRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixChainRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the raw chain attributes.
        /// </summary>
        public WixChainAttributes Attributes
        {
            get { return (WixChainAttributes)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets the disable rollback state of a chain.
        /// </summary>
        public bool DisableRollback
        {
            get { return 0 != (this.Attributes & WixChainAttributes.DisableRollback); }
        }

        /// <summary>
        /// Gets disable system restore state of a chain.
        /// </summary>
        public bool DisableSystemRestore
        {
            get { return 0 != (this.Attributes & WixChainAttributes.DisableSystemRestore); }
        }

        /// <summary>
        /// Gets parallel cache of a chain.
        /// </summary>
        public bool ParallelCache
        {
            get { return 0 != (this.Attributes & WixChainAttributes.ParallelCache); }
        }
    }
}
