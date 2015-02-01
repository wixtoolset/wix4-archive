//-------------------------------------------------------------------------------------------------
// <copyright file="PropertyRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    using System;

    /// <summary>
    /// Specialization of a row for the upgrade table.
    /// </summary>
    public sealed class PropertyRow : Row
    {
        /// <summary>
        /// Creates an Upgrade row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Upgrade row belongs to and should get its column definitions from.</param>
        public PropertyRow(SourceLineNumber sourceLineNumbers, ITable table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets and sets the upgrade code for the row.
        /// </summary>
        /// <value>Property identifier for the row.</value>
        public string Property
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets and sets the value for the row.
        /// </summary>
        /// <value>Property value for the row.</value>
        public string Value
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }
    }
}
