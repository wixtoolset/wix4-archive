//-------------------------------------------------------------------------------------------------
// <copyright file="WixMergeRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Specialization of a row for tracking merge statements.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Specialization of a row for tracking merge statements.
    /// </summary>
    public sealed class WixMergeRow : Row
    {
        /// <summary>
        /// Creates a Merge row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Merge row belongs to and should get its column definitions from.</param>
        public WixMergeRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>Creates a Merge row that belongs to a table.</summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Merge row belongs to and should get its column definitions from.</param>
        public WixMergeRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets and sets the id for a merge row.
        /// </summary>
        /// <value>Id for the row.</value>
        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets and sets the language for a merge row.
        /// </summary>
        /// <value>Language for the row.</value>
        public string Language
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets and sets the directory for a merge row.
        /// </summary>
        /// <value>Direcotory for the row.</value>
        public string Directory
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets and sets the path to the merge module for a merge row.
        /// </summary>
        /// <value>Source path for the row.</value>
        public string SourceFile
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets and sets the disk id the merge module should be placed on for a merge row.
        /// </summary>
        /// <value>Disk identifier for row.</value>
        public int DiskId
        {
            get { return (int)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets and sets the compression value for a merge row.
        /// </summary>
        /// <value>Compression for a merge row.</value>
        public YesNoType FileCompression
        {
            get
            {
                if (null == this.Fields[5].Data)
                {
                    return YesNoType.NotSet;
                }
                else if (1 == (int)this.Fields[5].Data)
                {
                    return YesNoType.Yes;
                }
                else if (0 == (int)this.Fields[5].Data)
                {
                    return YesNoType.No;
                }
                else
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixDataStrings.EXP_MergeTableFileCompressionColumnContainsInvalidValue, this.Fields[5].Data));
                }
            }
            set
            {
                if (YesNoType.Yes == value)
                {
                    this.Fields[5].Data = 1;
                }
                else if (YesNoType.No == value)
                {
                    this.Fields[5].Data = 0;
                }
                else if (YesNoType.NotSet == value)
                {
                    this.Fields[5].Data = null;
                }
                else
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixDataStrings.EXP_CannotSetMergeTableFileCompressionColumnToInvalidValue, value));
                }
            }
        }

        /// <summary>
        /// Gets and sets the configuration data for a merge row.
        /// </summary>
        /// <value>Comma delimited string of "name=value" pairs.</value>
        public string ConfigurationData
        {
            get { return (string)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }

        /// <summary>
        /// Gets and sets the primary feature for a merge row.
        /// </summary>
        /// <value>The primary feature for a merge row.</value>
        public string Feature
        {
            get { return (string)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }
    }
}
