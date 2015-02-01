//-------------------------------------------------------------------------------------------------
// <copyright file="Output.cs" company="Outercurve Foundation">
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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Output is generated by the linker.
    /// </summary>
    public sealed class Output
    {
        public const string XmlNamespaceUri = "http://wixtoolset.org/schemas/v4/wixout";
        private static readonly Version CurrentVersion = new Version("4.0.0.0");

        private Section entrySection;

        /// <summary>
        /// Creates a new empty output object.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the output.</param>
        public Output(SourceLineNumber sourceLineNumbers)
        {
            this.Sections = new List<Section>();
            this.SourceLineNumbers = sourceLineNumbers;
            this.SubStorages = new List<SubStorage>();
            this.Tables = new TableIndexedCollection();
        }

        /// <summary>
        /// Gets the entry section for the output
        /// </summary>
        /// <value>Entry section for the output.</value>
        public Section EntrySection
        {
            get
            {
                return this.entrySection;
            }

            set
            {
                this.entrySection = value;
                this.Codepage = value.Codepage;

                switch (this.entrySection.Type)
                {
                    case SectionType.Bundle:
                        this.Type = OutputType.Bundle;
                        break;
                    case SectionType.Product:
                        this.Type = OutputType.Product;
                        break;
                    case SectionType.Module:
                        this.Type = OutputType.Module;
                        break;
                    case SectionType.PatchCreation:
                        this.Type = OutputType.PatchCreation;
                        break;
                    case SectionType.Patch:
                        this.Type = OutputType.Patch;
                        break;
                    default:
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixDataStrings.EXP_UnexpectedEntrySectionType, this.entrySection.Type));
                }
            }
        }

        /// <summary>
        /// Gets the type of the output.
        /// </summary>
        /// <value>Type of the output.</value>
        public OutputType Type { get; set; }

        /// <summary>
        /// Gets or sets the codepage for this output.
        /// </summary>
        /// <value>Codepage of the output.</value>
        public int Codepage { get; set; }

        /// <summary>
        /// Gets the sections contained in the output.
        /// </summary>
        /// <value>Sections in the output.</value>
        public ICollection<Section> Sections { get; private set; }

        /// <summary>
        /// Gets the source line information for this output.
        /// </summary>
        /// <value>The source line information for this output.</value>
        public SourceLineNumber SourceLineNumbers { get; private set; }

        /// <summary>
        /// Gets the substorages in this output.
        /// </summary>
        /// <value>The substorages in this output.</value>
        public ICollection<SubStorage> SubStorages { get; private set; }

        /// <summary>
        /// Gets the tables contained in this output.
        /// </summary>
        /// <value>Collection of tables.</value>
        public TableIndexedCollection Tables { get; private set; }

        /// <summary>
        /// Gets the output type corresponding to a given output filename extension.
        /// </summary>
        /// <param name="extension">Case-insensitive output filename extension.</param>
        /// <returns>Output type for the extension.</returns>
        public static OutputType GetOutputType(string extension)
        {
            if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return OutputType.Bundle;
            }
            if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
            {
                return OutputType.Product;
            }
            else if (extension.Equals(".msm", StringComparison.OrdinalIgnoreCase))
            {
                return OutputType.Module;
            }
            else if (extension.Equals(".msp", StringComparison.OrdinalIgnoreCase))
            {
                return OutputType.Patch;
            }
            else if (extension.Equals(".mst", StringComparison.OrdinalIgnoreCase))
            {
                return OutputType.Transform;
            }
            else if (extension.Equals(".pcp", StringComparison.OrdinalIgnoreCase))
            {
                return OutputType.PatchCreation;
            }
            else
            {
                return OutputType.Unknown;
            }
        }

        /// <summary>
        /// Gets the filename extension corresponding to a given output type.
        /// </summary>
        /// <param name="type">One of the WiX output types.</param>
        /// <returns>Filename extension for the output type, for example ".msi".</returns>
        public static string GetExtension(OutputType type)
        {
            switch (type)
            {
                case OutputType.Bundle:
                    return ".exe";
                case OutputType.Product:
                    return ".msi";
                case OutputType.Module:
                    return ".msm";
                case OutputType.Patch:
                    return ".msp";
                case OutputType.Transform:
                    return ".mst";
                case OutputType.PatchCreation:
                    return ".pcp";
                default:
                    return ".wix";
            }
        }

        /// <summary>
        /// Loads an output from a path on disk.
        /// </summary>
        /// <param name="path">Path to output file saved on disk.</param>
        /// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
        /// <returns>Output object.</returns>
        public static Output Load(string path, bool suppressVersionCheck)
        {
            using (FileStream stream = File.OpenRead(path))
            using (FileStructure fs = FileStructure.Read(stream))
            {
                if (FileFormat.Wixout != fs.FileFormat)
                {
                    throw new WixUnexpectedFileFormatException(path, FileFormat.Wixout, fs.FileFormat);
                }

                Uri uri = new Uri(Path.GetFullPath(path));
                using (XmlReader reader = XmlReader.Create(fs.GetDataStream(), null, uri.AbsoluteUri))
                {
                    try
                    {
                        reader.MoveToContent();
                        return Output.Read(reader, suppressVersionCheck);
                    }
                    catch (XmlException xe)
                    {
                        throw new WixCorruptFileException(path, fs.FileFormat, xe);
                    }
                }
            }
        }

        /// <summary>
        /// Saves an output to a path on disk.
        /// </summary>
        /// <param name="path">Path to save output file to on disk.</param>
        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            using (FileStream stream = File.Create(path))
            using (FileStructure fs = FileStructure.Create(stream, FileFormat.Wixout, null))
            using (XmlWriter writer = XmlWriter.Create(fs.GetDataStream()))
            {
                writer.WriteStartDocument();
                this.Write(writer);
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Processes an XmlReader and builds up the output object.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
        /// <returns>The Output represented by the Xml.</returns>
        internal static Output Read(XmlReader reader, bool suppressVersionCheck)
        {
            if (!reader.LocalName.Equals("wixOutput"))
            {
                throw new XmlException();
            }

            bool empty = reader.IsEmptyElement;
            Output output = new Output(SourceLineNumber.CreateFromUri(reader.BaseURI));
            SectionType sectionType = SectionType.Unknown;
            Version version = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "codepage":
                        output.Codepage = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
                        break;
                    case "type":
                        switch (reader.Value)
                        {
                            case "Bundle":
                                output.Type = OutputType.Bundle;
                                sectionType = SectionType.Bundle;
                                break;
                            case "Module":
                                output.Type = OutputType.Module;
                                sectionType = SectionType.Module;
                                break;
                            case "Patch":
                                output.Type = OutputType.Patch;
                                break;
                            case "PatchCreation":
                                output.Type = OutputType.PatchCreation;
                                sectionType = SectionType.PatchCreation;
                                break;
                            case "Product":
                                output.Type = OutputType.Product;
                                sectionType = SectionType.Product;
                                break;
                            case "Transform":
                                output.Type = OutputType.Transform;
                                break;
                            default:
                                throw new XmlException();
                        }
                        break;
                    case "version":
                        version = new Version(reader.Value);
                        break;
                }
            }

            if (!suppressVersionCheck && null != version && !Output.CurrentVersion.Equals(version))
            {
                throw new WixException(WixDataErrors.VersionMismatch(SourceLineNumber.CreateFromUri(reader.BaseURI), "wixOutput", version.ToString(), Output.CurrentVersion.ToString()));
            }

            // create a section for all the rows to belong to
            output.entrySection = new Section(null, sectionType, output.Codepage);

            // loop through the rest of the xml building up the Output object
            TableDefinitionCollection tableDefinitions = null;
            List<ITable> tables = new List<ITable>();
            if (!empty)
            {
                bool done = false;

                // loop through all the fields in a row
                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "subStorage":
                                    output.SubStorages.Add(SubStorage.Read(reader));
                                    break;
                                case "table":
                                    if (null == tableDefinitions)
                                    {
                                        throw new XmlException();
                                    }
                                    tables.Add(Table.Read(reader, output.entrySection, tableDefinitions));
                                    break;
                                case "tableDefinitions":
                                    tableDefinitions = TableDefinitionCollection.Read(reader);
                                    break;
                                default:
                                    throw new XmlException();
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new XmlException();
                }
            }

            output.Tables = new TableIndexedCollection(tables);
            return output;
        }

        /// <summary>
        /// Ensure this output contains a particular table.
        /// </summary>
        /// <param name="tableDefinition">Definition of the table that should exist.</param>
        /// <param name="section">Optional section to use for the table. If one is not provided, the entry section will be used.</param>
        /// <returns>The table in this output.</returns>
        public ITable EnsureTable(TableDefinition tableDefinition, Section section = null)
        {
            ITable table;
            if (!this.Tables.TryGetTable(tableDefinition.Name, out table))
            {
                table = new Table(section ?? this.entrySection, tableDefinition);
                this.Tables.Add(table);
            }

            return table;
        }

        /// <summary>
        /// Persists an output in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Output should persist itself as XML.</param>
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement("wixOutput", XmlNamespaceUri);

            writer.WriteAttributeString("type", this.Type.ToString());

            if (0 != this.Codepage)
            {
                writer.WriteAttributeString("codepage", this.Codepage.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteAttributeString("version", Output.CurrentVersion.ToString());

            // Collect all the table definitions and write them.
            TableDefinitionCollection tableDefinitions = new TableDefinitionCollection();
            foreach (ITable table in this.Tables)
            {
                tableDefinitions.Add(table.Definition);
            }
            tableDefinitions.Write(writer);

            foreach (ITable table in this.Tables.OrderBy(t => t.Name))
            {
                table.Write(writer);
            }

            foreach (SubStorage subStorage in this.SubStorages)
            {
                subStorage.Write(writer);
            }

            writer.WriteEndElement();
        }
    }
}
