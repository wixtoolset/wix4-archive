//-------------------------------------------------------------------------------------------------
// <copyright file="ChainMsiPackageRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Specialization of a row for the ChainMsiPackage table.
    /// </summary>
    public sealed class ChainMsiPackageRow : Row
    {
        /// <summary>
        /// Creates a ChainMsiPackage row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this row belongs to and should get its column definitions from.</param>
        public ChainMsiPackageRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a ChainMsiPackageRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public ChainMsiPackageRow(SourceLineNumber sourceLineNumbers, Table table) :
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
        /// Gets or sets the raw MSI attributes of a package.
        /// </summary>
        public ChainMsiPackageAttributes Attributes
        {
            get { return (ChainMsiPackageAttributes)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the MSI package's product code.
        /// </summary>
        public string ProductCode
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the MSI package's upgrade code.
        /// </summary>
        public string UpgradeCode
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the product version of the MSI package.
        /// </summary>
        public string ProductVersion
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the language of the MSI package.
        /// </summary>
        public int ProductLanguage
        {
            get { return Convert.ToInt32(this.Fields[5].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the product name of the MSI package.
        /// </summary>
        public string ProductName
        {
            get { return (string)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }

        /// <summary>
        /// Gets or sets the MSI package's manufacturer.
        /// </summary>
        public string Manufacturer
        {
            get { return (string)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }

        /// <summary>
        /// Gets the display internal UI of a package.
        /// </summary>
        public bool DisplayInternalUI
        {
            get { return 0 != (this.Attributes & ChainMsiPackageAttributes.DisplayInternalUI); }
        }

        /// <summary>
        /// Gets the display internal UI of a package.
        /// </summary>
        public bool EnableFeatureSelection
        {
            get { return 0 != (this.Attributes & ChainMsiPackageAttributes.EnableFeatureSelection); }
        }

        /// <summary>
        /// Gets the display internal UI of a package.
        /// </summary>
        public bool ForcePerMachine
        {
            get { return 0 != (this.Attributes & ChainMsiPackageAttributes.ForcePerMachine); }
        }

        /// <summary>
        /// Gets the suppress loose file payload generation of a package.
        /// </summary>
        public bool SuppressLooseFilePayloadGeneration
        {
            get { return 0 != (this.Attributes & ChainMsiPackageAttributes.SuppressLooseFilePayloadGeneration); }
        }
    }
}
