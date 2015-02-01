//-------------------------------------------------------------------------------------------------
// <copyright file="WixRelatedBundleRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    using Serialize = WixToolset.Data.Serialize;

    /// <summary>
    /// Specialization of a row for the RelatedBundle table.
    /// </summary>
    public sealed class WixRelatedBundleRow : Row
    {
        /// <summary>
        /// Creates a RelatedBundle row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this RelatedBundle row belongs to and should get its column definitions from.</param>
        public WixRelatedBundleRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a RelatedBundle row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this RelatedBundle row belongs to and should get its column definitions from.</param>
        public WixRelatedBundleRow(SourceLineNumber sourceLineNumbers, ITable table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the related bundle identifier.
        /// </summary>
        /// <value>The related bundle identifier.</value>
        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the related bundle action.
        /// </summary>
        /// <value>The related bundle action.</value>
        public Serialize.RelatedBundle.ActionType Action
        {
            get { return (Serialize.RelatedBundle.ActionType)this.Fields[1].Data; }
            set { this.Fields[1].Data = (int)value; }
        }
    }
}
