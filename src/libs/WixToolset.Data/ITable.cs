//-------------------------------------------------------------------------------------------------
// <copyright file="ITable.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    public interface ITable
    {
        /// <summary>
        /// Gets the section for the table.
        /// </summary>
        /// <value>Section for the table.</value>
        Section Section { get; }

        /// <summary>
        /// Gets the table definition.
        /// </summary>
        /// <value>Definition of the table.</value>
        TableDefinition Definition { get; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>Name of the table.</value>
        string Name { get; }

        /// <summary>
        /// Gets or sets the table transform operation.
        /// </summary>
        /// <value>The table transform operation.</value>
        TableOperation Operation { get; set; }

        /// <summary>
        /// Gets the rows contained in the table.
        /// </summary>
        /// <value>Rows contained in the table.</value>
        // TODO: Convert to IEnumerable<Row>?
        IList<Row> Rows { get; }

        /// <summary>
        /// Creates a new row in the table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="add">Specifies whether to only create the row or add it to the table automatically.</param>
        /// <returns>Row created in table.</returns>
        Row CreateRow(SourceLineNumber sourceLineNumbers, bool add = true);

        /// <summary>
        /// Modularize the table.
        /// </summary>
        /// <param name="modularizationGuid">String containing the GUID of the Merge Module, if appropriate.</param>
        /// <param name="suppressModularizationIdentifiers">Optional collection of identifiers that should not be modularized.</param>
        void Modularize(string modularizationGuid, ISet<string> suppressModularizationIdentifiers);

        /// <summary>
        /// Writes the table in IDT format to the provided stream.
        /// </summary>
        /// <param name="writer">Stream to write the table to.</param>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        void ToIdtDefinition(StreamWriter writer, bool keepAddedColumns);

        /// <summary>
        /// Validates the rows of this OutputTable and throws if it collides on
        /// primary keys.
        /// </summary>
        void ValidateRows();

        /// <summary>
        /// Persists a table in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the ITable should persist itself as XML.</param>
        void Write(XmlWriter writer);
    }
}
