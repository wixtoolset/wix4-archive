//-------------------------------------------------------------------------------------------------
// <copyright file="WixBundleMsiPropertyRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// Specialization of a row for the upgrade table.
    /// </summary>
    public sealed class WixBundleMsiPropertyRow : Row
    {
        /// <summary>
        /// Creates an Upgrade row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Upgrade row belongs to and should get its column definitions from.</param>
        public WixBundleMsiPropertyRow(SourceLineNumber sourceLineNumbers, Table table) :
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
        /// Gets and sets the property identity.
        /// </summary>
        public string Name
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets and sets the value for the row.
        /// </summary>
        /// <value>MsiProperty value for the row.</value>
        public string Value
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets and sets the condition for the row.
        /// </summary>
        /// <value>MsiProperty condition for the row.</value>
        public string Condition
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }
    }
}
