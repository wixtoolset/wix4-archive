//-------------------------------------------------------------------------------------------------
// <copyright file="Generator.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Helper class to scan object files for unit tests.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Lux
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Wix = WixToolset.Data.Serialize;
    using WixLux = WixToolset.Extensions.Serialize.Lux;

    /// <summary>
    /// Helper class to scan objects for unit tests.
    /// </summary>
    public sealed class Generator : IMessageHandler
    {
        private StringCollection extensionList = new StringCollection();
        private List<string> inputFiles = new List<string>();
        private List<string> inputFragments;
        private string outputFile;

        /// <summary>
        /// Sets the list of WiX extensions used by the input files.
        /// </summary>
        public StringCollection Extensions
        {
            set
            {
                this.extensionList = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of WiX object and library files to scan for unit tests.
        /// </summary>
        public List<string> InputFiles
        {
            get
            {
                return this.inputFiles;
            }

            set
            {
                this.inputFiles = value;
            }
        }

        /// <summary>
        /// Gets the subset of InputFiles that contain unit tests and should be included in a test package.
        /// </summary>
        public List<string> InputFragments
        {
            get
            {
                return this.inputFragments;
            }
        }

        /// <summary>
        /// Sets the optional generated test package source file.
        /// </summary>
        public string OutputFile
        {
            set
            {
                this.outputFile = value;
            }
        }

        /// <summary>
        /// Scan the input files for unit tests and, if specified, generate a test package source file.
        /// </summary>
        /// <param name="extensions">The WiX extensions used by the input files.</param>
        /// <param name="inputFiles">The WiX object and library files to scan for unit tests.</param>
        /// <param name="outputFile">The optional generated test package source file.</param>
        /// <param name="inputFragments">The subset of InputFiles that are fragments (i.e., are not entry sections like Product) and should be included in a test package.</param>
        /// <returns>True if successful or False if there were no unit tests in the input files or a test package couldn't be created.</returns>
        public static bool Generate(StringCollection extensions, List<string> inputFiles, string outputFile, out List<string> inputFragments)
        {
            Generator generator = new Generator();
            generator.Extensions = extensions;
            generator.InputFiles = inputFiles;
            generator.OutputFile = outputFile;

            bool success = generator.Generate();
            inputFragments = generator.InputFragments;
            return success;
        }

        /// <summary>
        /// Scan the input files for unit tests and, if specified, generate a test package source file.
        /// </summary>
        /// <returns>True if successful or False if there were no unit tests in the input files or a test package couldn't be created.</returns>
        public bool Generate()
        {
            // get the unit tests included in all the objects
            List<string> unitTestIds = this.FindUnitTests();
            if (null == unitTestIds || 0 == unitTestIds.Count)
            {
                this.OnMessage(LuxBuildErrors.NoUnitTests());
                return false;
            }

            // and write the WiX source that consumes them all
            if (!String.IsNullOrEmpty(this.outputFile))
            {
                this.GenerateTestSource(unitTestIds);
            }

            return true;
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            Messaging.Instance.OnMessage(mea);
        }

        /// <summary>
        /// Find all the unit tests from the WixUnitTest tables in all the input files' sections.
        /// </summary>
        /// <returns>Returns a list of unit test ids.</returns>
        private List<string> FindUnitTests()
        {
            // get the primary keys for every row from every WixUnitTest table in our sections:
            // voila, we have our unit test ids
            this.inputFragments = new List<string>();
            List<string> unitTestIds = new List<string>();
            Dictionary<Section, string> sections = this.LoadSections();

            if (null != sections && 0 < sections.Count)
            {
                foreach (Section section in sections.Keys)
                {
                    if (SectionType.Fragment == section.Type)
                    {
                        string file = sections[section];
                        if (!this.inputFragments.Contains(file))
                        {
                            this.inputFragments.Add(file);
                        }

                        Table unitTestTable = section.Tables["WixUnitTest"];
                        if (null != unitTestTable)
                        {
                            foreach (Row row in unitTestTable.Rows)
                            {
                                unitTestIds.Add(row.GetPrimaryKey('/'));
                            }
                        }
                    }
                }
            }

            return unitTestIds;
        }

        /// <summary>
        /// Generates a WiX serialization object tree for a product that consumes the
        /// given unit tests.
        /// </summary>
        /// <param name="unitTestIds">List of unit test ids.</param>
        private void GenerateTestSource(List<string> unitTestIds)
        {
            Wix.Product product = new Wix.Product();
            product.Id = "*";
            product.Language = "1033";
            product.Manufacturer = "Lux";
            product.Name = Path.GetFileNameWithoutExtension(this.outputFile) + " Lux test project";
            product.Version = "1.0";
            product.UpgradeCode = "{FBBDFC60-6EFF-427E-8B6B-7696A3C7066B}";

            Wix.Package package = new Wix.Package();
            package.Compressed = Wix.YesNoType.yes;
            package.InstallScope = Wix.Package.InstallScopeType.perUser;
            product.AddChild(package);

            foreach (string unitTestId in unitTestIds)
            {
                WixLux.UnitTestRef unitTestRef = new WixLux.UnitTestRef();
                unitTestRef.Id = unitTestId;
                product.AddChild(unitTestRef);
            }

            Wix.Wix wix = new Wix.Wix();
            wix.AddChild(product);

            // now write to the file
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            this.OnMessage(LuxBuildVerboses.GeneratingConsumer(this.outputFile, unitTestIds.Count));
            using (XmlWriter writer = XmlWriter.Create(this.outputFile, settings))
            {
                writer.WriteStartDocument();
                wix.OutputXml(writer);
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Load sections from the input files.
        /// </summary>
        /// <returns>Returns a section collection.</returns>
        private Dictionary<Section, string> LoadSections()
        {
            // we need a Linker and the extensions for their table definitions
            Linker linker = new Linker();

            if (null != this.extensionList)
            {
                ExtensionManager extensionManager = new ExtensionManager();
                foreach (string extension in this.extensionList)
                {
                    extensionManager.Load(extension);
                }

                foreach (IExtensionData data in extensionManager.Create<IExtensionData>())
                {
                    linker.AddExtensionData(data);
                }
            }

            // load each intermediate and library file and get their sections
            Dictionary<Section, string> sectionFiles = new Dictionary<Section, string>();

            if (null != this.inputFiles)
            {
                foreach (string inputFile in this.inputFiles)
                {
                    string inputFileFullPath = Path.GetFullPath(inputFile);
                    if (File.Exists(inputFileFullPath))
                    {
                        FileFormat format = FileStructure.GuessFileFormatFromExtension(Path.GetExtension(inputFileFullPath));
                        bool retry;
                        do
                        {
                            retry = false;

                            try
                            {
                                switch (format)
                                {
                                    case FileFormat.Wixobj:
                                        Intermediate intermediate = Intermediate.Load(inputFile, linker.TableDefinitions, false);
                                        foreach (Section section in intermediate.Sections)
                                        {
                                            sectionFiles[section] = inputFile;
                                        }
                                        break;

                                    default:
                                        Library library = Library.Load(inputFile, linker.TableDefinitions, false);
                                        foreach (Section section in library.Sections)
                                        {
                                            sectionFiles[section] = inputFile;
                                        }
                                        break;
                                }
                            }
                            catch (WixUnexpectedFileFormatException e)
                            {
                                format = e.FileFormat;
                                retry = (FileFormat.Wixobj != format && FileFormat.Wixlib != format); // .wixobj and .wixout are supported by lux.
                                if (!retry)
                                {
                                    this.OnMessage(LuxBuildErrors.CouldntLoadInput(inputFile));
                                }
                            }
                        } while (retry);
                    }
                }
            }

            return sectionFiles;
        }
    }
}