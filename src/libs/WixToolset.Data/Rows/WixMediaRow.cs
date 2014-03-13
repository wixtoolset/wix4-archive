//-------------------------------------------------------------------------------------------------
// <copyright file="WixMediaRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Specialization of a row for the media table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Specialization of a row for the WixMedia table.
    /// </summary>
    public sealed class WixMediaRow : Row
    {
        /// <summary>
        /// Creates a WixMedia row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public WixMediaRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixMedia row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
        public WixMediaRow(SourceLineNumber sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the compression level for this media row.
        /// </summary>
        /// <value>Compression level.</value>
        public CompressionLevel CompressionLevel
        {
            get
            {
                CompressionLevel level = CompressionLevel.Mszip;
                switch ((string)this.Fields[1].Data)
                {
                    case "low":
                        level = CompressionLevel.Low;
                        break;
                    case "medium":
                        level = CompressionLevel.Medium;
                        break;
                    case "high":
                        level = CompressionLevel.High;
                        break;
                    case "none":
                        level = CompressionLevel.None;
                        break;
                    case "mszip":
                        level = CompressionLevel.Mszip;
                        break;
                }

                return level;
            }

            set
            {
                switch (value)
                {
                    case CompressionLevel.None:
                        this.Fields[1].Data = "none";
                        break;
                    case CompressionLevel.Low:
                        this.Fields[1].Data = "low";
                        break;
                    case CompressionLevel.Medium:
                        this.Fields[1].Data = "medium";
                        break;
                    case CompressionLevel.High:
                        this.Fields[1].Data = "high";
                        break;
                    case CompressionLevel.Mszip:
                        this.Fields[1].Data = "mszip";
                        break;
                    default:
                        throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, WixDataStrings.EXP_UnknownCompressionLevelType, value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the disk id for this media.
        /// </summary>
        /// <value>Disk id for the media.</value>
        public int DiskId
        {
            get { return Convert.ToInt32(this.Fields[0].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the compression level for this media row has been set.
        /// </summary>
        /// <value>Compression level.</value>
        public bool HasExplicitCompressionLevel
        {
            get { return !String.IsNullOrEmpty((string)this.Fields[1].Data); }
        }

        /// <summary>
        /// Gets or sets the layout location for this media row.
        /// </summary>
        /// <value>Layout location to the root of the media.</value>
        public string Layout
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }
    }
}
