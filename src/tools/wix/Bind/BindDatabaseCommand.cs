//-------------------------------------------------------------------------------------------------
// <copyright file="BindDatabaseCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using WixToolset.Bind.Databases;
    using WixToolset.Cab;
    using WixToolset.Clr.Interop;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;
    using WixToolset.MergeMod;
    using WixToolset.Msi;
    using WixToolset.Msi.Interop;

    /// <summary>
    /// Binds a databse.
    /// </summary>
    internal class BindDatabaseCommand : ICommand
    {
        // As outlined in RFC 4122, this is our namespace for generating name-based (version 3) UUIDs.
        private static readonly Guid WixComponentGuidNamespace = new Guid("{3064E5C6-FB63-4FE9-AC49-E446A792EFA5}");

        public int Codepage { private get; set; }

        public int CabbingThreadCount { private get; set; }

        public CompressionLevel DefaultCompressionLevel { private get; set; }

        public IEnumerable<IBinderExtension> Extensions { private get; set; }

        public BinderFileManagerCore FileManagerCore { private get; set; }

        public IEnumerable<IBinderFileManager> FileManagers { private get; set; }

        public IEnumerable<InspectorExtension> InspectorExtensions { private get; set; }

        public Localizer Localizer { private get; set; }

        public string PdbFile { private get; set; }

        public Output Output { private get; set; }

        public string OutputPath { private get; set; }

        public bool SuppressAddingValidationRows { private get; set; }

        public bool SuppressLayout { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public string TempFilesLocation { private get; set; }

        public Validator Validator { private get; set; }

        public WixVariableResolver WixVariableResolver { private get; set; }

        public IEnumerable<FileTransfer> FileTransfers { get; private set; }

        public IEnumerable<string> ContentFilePaths { get; private set; }

        public void Execute()
        {
            foreach (BinderExtension extension in this.Extensions)
            {
                extension.Initialize(this.Output);
            }

            List<FileTransfer> fileTransfers = new List<FileTransfer>();

            HashSet<string> suppressedTableNames = new HashSet<string>();

            // Localize fields, resolve wix variables, and resolve file paths.
            ExtractEmbeddedFiles filesWithEmbeddedFiles = new ExtractEmbeddedFiles();

            IEnumerable<DelayedField> delayedFields;
            {
                ResolveFieldsCommand command = new ResolveFieldsCommand();
                command.Tables = this.Output.Tables;
                command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                command.FileManagerCore = this.FileManagerCore;
                command.FileManagers = this.FileManagers;
                command.SupportDelayedResolution = true;
                command.TempFilesLocation = this.TempFilesLocation;
                command.WixVariableResolver = this.WixVariableResolver;
                command.Execute();

                delayedFields = command.DelayedFields;
            }

            // if there are any fields to resolve later, create the cache to populate during bind
            IDictionary<string, string> variableCache = null;
            if (delayedFields.Any())
            {
                variableCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }

            this.LocalizeUI(this.Output.Tables);

            // Process the summary information table before the other tables.
            bool compressed;
            bool longNames;
            string modularizationGuid;

            {
                BindSummaryInfoCommand command = new BindSummaryInfoCommand();
                command.Output = this.Output;
                command.Execute();

                compressed = command.Compressed;
                longNames = command.LongNames;
                modularizationGuid = command.ModularizationGuid;
            }

            // Stop processing if an error previously occurred.
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // modularize identifiers and add tables with real streams to the import tables
            if (OutputType.Module == this.Output.Type)
            {
                // Gather all the suppress modularization identifiers
                HashSet<string> suppressModularizationIdentifiers = null;
                Table wixSuppressModularizationTable = this.Output.Tables["WixSuppressModularization"];
                if (null != wixSuppressModularizationTable)
                {
                    suppressModularizationIdentifiers = new HashSet<string>(wixSuppressModularizationTable.Rows.Select(row => (string)row[0]));
                }

                foreach (Table table in this.Output.Tables)
                {
                    table.Modularize(modularizationGuid, suppressModularizationIdentifiers);
                }
            }

            // Merge unreal table data into the real tables
            // This must occur after all variables and source paths have been resolved and after modularization.
            this.MergeUnrealTables(this.Output, this.Output.Tables);

            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            if (OutputType.Patch == this.Output.Type)
            {
                foreach (SubStorage substorage in this.Output.SubStorages)
                {
                    Output transform = substorage.Data;

                    ResolveFieldsCommand command = new ResolveFieldsCommand();
                    command.Tables = transform.Tables;
                    command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                    command.FileManagerCore = this.FileManagerCore;
                    command.FileManagers = this.FileManagers;
                    command.SupportDelayedResolution = false;
                    command.TempFilesLocation = this.TempFilesLocation;
                    command.WixVariableResolver = this.WixVariableResolver;
                    command.Execute();

                    this.MergeUnrealTables(this.Output, transform.Tables);
                }
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // add binder variables for all properties
            Table propertyTable = this.Output.Tables["Property"];
            if (null != propertyTable)
            {
                foreach (PropertyRow propertyRow in propertyTable.Rows)
                {
                    // Set the ProductCode if it is to be generated.
                    if (OutputType.Product == this.Output.Type && "ProductCode".Equals(propertyRow.Property, StringComparison.Ordinal) && "*".Equals(propertyRow.Value, StringComparison.Ordinal))
                    {
                        propertyRow.Value = Common.GenerateGuid();

                        // Update the target ProductCode in any instance transforms.
                        foreach (SubStorage subStorage in this.Output.SubStorages)
                        {
                            Output subStorageOutput = subStorage.Data;
                            if (OutputType.Transform != subStorageOutput.Type)
                            {
                                continue;
                            }

                            Table instanceSummaryInformationTable = subStorageOutput.Tables["_SummaryInformation"];
                            foreach (Row row in instanceSummaryInformationTable.Rows)
                            {
                                if ((int)SummaryInformation.Transform.ProductCodes == row.FieldAsInteger(0))
                                {
                                    row[1] = row.FieldAsString(1).Replace("*", propertyRow.Value);
                                    break;
                                }
                            }
                        }
                    }

                    // Add the property name and value to the variableCache.
                    if (null != variableCache)
                    {
                        string key = String.Concat("property.", this.Demodularize(this.Output, modularizationGuid, propertyRow.Property));
                        variableCache[key] = propertyRow.Value;
                    }
                }
            }

            // Extract files that come from cabinet files (this does not extract files from merge modules).
            {
                ExtractEmbeddedFilesCommand command = new ExtractEmbeddedFilesCommand();
                command.FilesWithEmbeddedFiles = filesWithEmbeddedFiles;
                command.Execute();
            }

            // Start with a collection of all files from the File table. The list of all files that need
            // to be processed may grow if we add things like Merge Modules or patches with multiple
            // transforms.
            // Collecting these files must occur AFTER the unreal data has been merged in.
            List<FileRow> fileRows = this.Output.Tables["File"].RowsAs<FileRow>().ToList();

            if (OutputType.Product == this.Output.Type)
            {
                // Retrieve files and their information from merge modules.
                this.ProcessMergeModules(this.Output, fileRows);
            }
            else if (OutputType.Patch == this.Output.Type)
            {
                // Merge transform data into the output object.
                IEnumerable<FileRow> fileRowsFromTransform = this.CopyFromTransformData(this.Output);

                fileRows.AddRange(fileRowsFromTransform);
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // Assign files to media.
            RowDictionary<MediaRow> assignedMediaRows;
            Dictionary<MediaRow, IEnumerable<FileRow>> fileRowsByCabinetMedia;
            IEnumerable<FileRow> uncompressedFileRows;
            {
                AssignMediaCommand command = new AssignMediaCommand();
                command.FilesCompressed = compressed;
                command.FileRows = fileRows;
                command.Output = this.Output;
                command.TableDefinitions = this.TableDefinitions;
                command.Execute();

                assignedMediaRows = command.MediaRows;
                fileRowsByCabinetMedia = command.FileRowsByCabinetMedia;
                uncompressedFileRows = command.UncompressedFileRows;
            }

            // Update file sequence.
            Messaging.Instance.OnMessage(WixVerboses.UpdatingFileInformation());
            this.UpdateMediaSequences(this.Output, fileRows, assignedMediaRows);

            // Gather information about files that did not come from merge modules (i.e. rows with a reference to the File table).
            foreach (FileRow row in fileRows.Where(r => null != r.Table))
            {
                this.UpdateFileRow(this.Output, variableCache, modularizationGuid, fileRows, row, false);
            }

            // Set generated component guids.
            this.SetComponentGuids(this.Output);

            // With the Component Guids set now we can create instance transforms.
            this.CreateInstanceTransforms(this.Output);

            this.ValidateComponentGuids(this.Output);

            this.UpdateControlText(this.Output);

            if (delayedFields.Any())
            {
                ResolveDelayedFieldsCommand command = new ResolveDelayedFieldsCommand();
                command.OutputType = this.Output.Type;
                command.DelayedFields = delayedFields;
                command.ModularizationGuid = null;
                command.VariableCache = variableCache;
                command.Execute();
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // Extended binder extensions can be called now that fields are resolved.
            Table updatedFiles = this.Output.EnsureTable(this.TableDefinitions["WixBindUpdatedFiles"]);

            foreach (BinderExtension extension in this.Extensions)
            {
                extension.AfterResolvedFields(this.Output);
            }

            foreach (Row updatedFile in updatedFiles.Rows)
            {
                // TODO: Is this too slow? It's basically NxM. Okay'ish, if updatedFiles is small count.
                FileRow updatedFileRow = fileRows.Single(r => r.File.Equals((string)updatedFile[0], StringComparison.Ordinal));
                this.UpdateFileRow(this.Output, null, modularizationGuid, fileRows, updatedFileRow, true);
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            Directory.CreateDirectory(this.TempFilesLocation);

            // create cabinet files and process uncompressed files
            string layoutDirectory = Path.GetDirectoryName(this.OutputPath);
            if (!this.SuppressLayout || OutputType.Module == this.Output.Type)
            {
                Messaging.Instance.OnMessage(WixVerboses.CreatingCabinetFiles());

                CreateCabinetsCommand command = new CreateCabinetsCommand();
                command.CabbingThreadCount = this.CabbingThreadCount;
                command.DefaultCompressionLevel = this.DefaultCompressionLevel;
                command.Output = this.Output;
                command.FileManagers = this.FileManagers;
                command.LayoutDirectory = layoutDirectory;
                command.Compressed = compressed;
                command.FileRowsByCabinet = fileRowsByCabinetMedia;
                command.TableDefinitions = this.TableDefinitions;
                command.TempFilesLocation = this.TempFilesLocation;
                command.Execute();

                fileTransfers.AddRange(command.FileTransfers);
            }

            if (OutputType.Patch == this.Output.Type)
            {
                // copy output data back into the transforms
                this.CopyToTransformData(this.Output);
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // add back suppressed tables which must be present prior to merging in modules
            if (OutputType.Product == this.Output.Type)
            {
                Table wixMergeTable = this.Output.Tables["WixMerge"];

                if (null != wixMergeTable && 0 < wixMergeTable.Rows.Count)
                {
                    foreach (SequenceTable sequence in Enum.GetValues(typeof(SequenceTable)))
                    {
                        string sequenceTableName = sequence.ToString();
                        Table sequenceTable = this.Output.Tables[sequenceTableName];

                        if (null == sequenceTable)
                        {
                            sequenceTable = this.Output.EnsureTable(this.TableDefinitions[sequenceTableName]);
                        }

                        if (0 == sequenceTable.Rows.Count)
                        {
                            suppressedTableNames.Add(sequenceTableName);
                        }
                    }
                }
            }

            foreach (BinderExtension extension in this.Extensions)
            {
                extension.Finish(this.Output);
            }

            // generate database file
            Messaging.Instance.OnMessage(WixVerboses.GeneratingDatabase());
            string tempDatabaseFile = Path.Combine(this.TempFilesLocation, Path.GetFileName(this.OutputPath));
            this.GenerateDatabase(this.Output, tempDatabaseFile, false, false);

            FileTransfer transfer;
            if (FileTransfer.TryCreate(tempDatabaseFile, this.OutputPath, true, this.Output.Type.ToString(), null, out transfer)) // note where this database needs to move in the future
            {
                transfer.Built = true;
                fileTransfers.Add(transfer);
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // Output the output to a file
            Pdb pdb = new Pdb();
            pdb.Output = this.Output;
            if (!String.IsNullOrEmpty(this.PdbFile))
            {
                pdb.Save(this.PdbFile);
            }

            // merge modules
            if (OutputType.Product == this.Output.Type)
            {
                Messaging.Instance.OnMessage(WixVerboses.MergingModules());
                this.MergeModules(tempDatabaseFile, this.Output, fileRows, suppressedTableNames);

                // stop processing if an error previously occurred
                if (Messaging.Instance.EncounteredError)
                {
                    return;
                }
            }

            // inspect the MSI prior to running ICEs
            InspectorCore inspectorCore = new InspectorCore();
            foreach (InspectorExtension inspectorExtension in this.InspectorExtensions)
            {
                inspectorExtension.Core = inspectorCore;
                inspectorExtension.InspectDatabase(tempDatabaseFile, pdb);

                inspectorExtension.Core = null; // reset.
            }

            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // validate the output if there is an MSI validator
            if (null != this.Validator)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                // set the output file for source line information
                this.Validator.Output = this.Output;

                Messaging.Instance.OnMessage(WixVerboses.ValidatingDatabase());

                this.Validator.Validate(tempDatabaseFile);

                stopwatch.Stop();
                Messaging.Instance.OnMessage(WixVerboses.ValidatedDatabase(stopwatch.ElapsedMilliseconds));

                // Stop processing if an error occurred.
                if (Messaging.Instance.EncounteredError)
                {
                    return;
                }
            }

            // process uncompressed files
            if (!this.SuppressLayout)
            {
                this.ProcessUncompressedFiles(tempDatabaseFile, uncompressedFileRows, fileTransfers, assignedMediaRows, layoutDirectory, compressed, longNames);
            }

            this.FileTransfers = fileTransfers;
            this.ContentFilePaths = fileRows.Select(r => r.Source).ToList();
        }

        /// <summary>
        /// Localize dialogs and controls.
        /// </summary>
        /// <param name="tables">The tables to localize.</param>
        private void LocalizeUI(TableIndexedCollection tables)
        {
            Table dialogTable = tables["Dialog"];
            if (null != dialogTable)
            {
                foreach (Row row in dialogTable.Rows)
                {
                    string dialog = (string)row[0];
                    LocalizedControl localizedControl = this.Localizer.GetLocalizedControl(dialog, null);
                    if (null != localizedControl)
                    {
                        if (CompilerConstants.IntegerNotSet != localizedControl.X)
                        {
                            row[1] = localizedControl.X;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Y)
                        {
                            row[2] = localizedControl.Y;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Width)
                        {
                            row[3] = localizedControl.Width;
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Height)
                        {
                            row[4] = localizedControl.Height;
                        }

                        row[5] = (int)row[5] | localizedControl.Attributes;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            row[6] = localizedControl.Text;
                        }
                    }
                }
            }

            Table controlTable = tables["Control"];
            if (null != controlTable)
            {
                foreach (Row row in controlTable.Rows)
                {
                    string dialog = (string)row[0];
                    string control = (string)row[1];
                    LocalizedControl localizedControl = this.Localizer.GetLocalizedControl(dialog, control);
                    if (null != localizedControl)
                    {
                        if (CompilerConstants.IntegerNotSet != localizedControl.X)
                        {
                            row[3] = localizedControl.X.ToString();
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Y)
                        {
                            row[4] = localizedControl.Y.ToString();
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Width)
                        {
                            row[5] = localizedControl.Width.ToString();
                        }

                        if (CompilerConstants.IntegerNotSet != localizedControl.Height)
                        {
                            row[6] = localizedControl.Height.ToString();
                        }

                        row[7] = (int)row[7] | localizedControl.Attributes;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            row[9] = localizedControl.Text;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Merge data from the unreal tables into the real tables.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void MergeUnrealTables(Output output, TableIndexedCollection tables)
        {
            // Create the special properties.
            Table wixPropertyTable = output.Tables["WixProperty"];
            if (null != wixPropertyTable)
            {
                // Create lists of the properties that contribute to the special lists of properties.
                SortedSet<string> adminProperties = new SortedSet<string>();
                SortedSet<string> secureProperties = new SortedSet<string>();
                SortedSet<string> hiddenProperties = new SortedSet<string>();

                foreach (WixPropertyRow wixPropertyRow in wixPropertyTable.Rows)
                {
                    if (wixPropertyRow.Admin)
                    {
                        adminProperties.Add(wixPropertyRow.Id);
                    }

                    if (wixPropertyRow.Hidden)
                    {
                        hiddenProperties.Add(wixPropertyRow.Id);
                    }

                    if (wixPropertyRow.Secure)
                    {
                        secureProperties.Add(wixPropertyRow.Id);
                    }
                }

                Table propertyTable = output.Tables["Property"];
                if (0 < adminProperties.Count)
                {
                    PropertyRow row = (PropertyRow)propertyTable.CreateRow(null);
                    row.Property = "AdminProperties";
                    row.Value = String.Join(";", adminProperties);
                }

                if (0 < secureProperties.Count)
                {
                    PropertyRow row = (PropertyRow)propertyTable.CreateRow(null);
                    row.Property = "SecureCustomProperties";
                    row.Value = String.Join(";", secureProperties);
                }

                if (0 < hiddenProperties.Count)
                {
                    PropertyRow row = (PropertyRow)propertyTable.CreateRow(null);
                    row.Property = "MsiHiddenProperties";
                    row.Value = String.Join(";", hiddenProperties);
                }
            }

            // Merge data from the WixFile rows into the File rows.
            Table wixFileTable = tables["WixFile"];
            if (null != wixFileTable)
            {
                // index all the File rows by their identifier
                Table fileTable = tables["File"];
                Hashtable indexedFileRows = new Hashtable(fileTable.Rows.Count, StringComparer.OrdinalIgnoreCase);

                foreach (FileRow fileRow in fileTable.Rows)
                {
                    try
                    {
                        indexedFileRows.Add(fileRow.File, fileRow);
                    }
                    catch (ArgumentException)
                    {
                        Messaging.Instance.OnMessage(WixErrors.DuplicateFileId(fileRow.File));
                    }
                }

                if (Messaging.Instance.EncounteredError)
                {
                    return;
                }

                foreach (WixFileRow row in wixFileTable.Rows)
                {
                    FileRow fileRow = (FileRow)indexedFileRows[row.File];

                    if (null != row[1])
                    {
                        fileRow.AssemblyType = (FileAssemblyType)Enum.ToObject(typeof(FileAssemblyType), row.AssemblyAttributes);
                    }
                    else
                    {
                        fileRow.AssemblyType = FileAssemblyType.NotAnAssembly;
                    }
                    fileRow.AssemblyApplication = row.AssemblyApplication;
                    fileRow.AssemblyManifest = row.AssemblyManifest;
                    fileRow.Directory = row.Directory;
                    fileRow.DiskId = row.DiskId;
                    fileRow.Source = row.Source;
                    fileRow.PreviousSource = row.PreviousSource;
                    fileRow.ProcessorArchitecture = row.ProcessorArchitecture;
                    fileRow.PatchGroup = row.PatchGroup;
                    fileRow.PatchAttributes = row.PatchAttributes;
                    fileRow.RetainLengths = row.RetainLengths;
                    fileRow.IgnoreOffsets = row.IgnoreOffsets;
                    fileRow.IgnoreLengths = row.IgnoreLengths;
                    fileRow.RetainOffsets = row.RetainOffsets;
                    fileRow.PreviousRetainLengths = row.PreviousRetainLengths;
                    fileRow.PreviousIgnoreOffsets = row.PreviousIgnoreOffsets;
                    fileRow.PreviousIgnoreLengths = row.PreviousIgnoreLengths;
                    fileRow.PreviousRetainOffsets = row.PreviousRetainOffsets;
                }
            }

            // merge data from the WixPatchSymbolPaths rows into the File rows
            Table wixPatchSymbolPathsTable = tables["WixPatchSymbolPaths"];
            if (null != wixPatchSymbolPathsTable)
            {
                Table fileTable = tables["File"];
                Table mediaTable = tables["Media"];
                Table directoryTable = tables["Directory"];
                Table componentTable = tables["Component"];

                int fileRowNum = (null != fileTable) ? fileTable.Rows.Count : 0;
                int componentRowNum = (null != componentTable) ? componentTable.Rows.Count : 0;
                int directoryRowNum = (null != directoryTable) ? directoryTable.Rows.Count : 0;
                int mediaRowNum = (null != mediaTable) ? mediaTable.Rows.Count : 0;

                Hashtable fileRowsByFile = new Hashtable(fileRowNum);
                Hashtable fileRowsByComponent = new Hashtable(componentRowNum);
                Hashtable fileRowsByDirectory = new Hashtable(directoryRowNum);
                Hashtable fileRowsByDiskId = new Hashtable(mediaRowNum);

                // index all the File rows by their identifier
                if (null != fileTable)
                {
                    foreach (FileRow fileRow in fileTable.Rows)
                    {
                        fileRowsByFile.Add(fileRow.File, fileRow);

                        ArrayList fileRows = (ArrayList)fileRowsByComponent[fileRow.Component];
                        if (null == fileRows)
                        {
                            fileRows = new ArrayList();
                            fileRowsByComponent.Add(fileRow.Component, fileRows);
                        }
                        fileRows.Add(fileRow);

                        fileRows = (ArrayList)fileRowsByDirectory[fileRow.Directory];
                        if (null == fileRows)
                        {
                            fileRows = new ArrayList();
                            fileRowsByDirectory.Add(fileRow.Directory, fileRows);
                        }
                        fileRows.Add(fileRow);

                        fileRows = (ArrayList)fileRowsByDiskId[fileRow.DiskId];
                        if (null == fileRows)
                        {
                            fileRows = new ArrayList();
                            fileRowsByDiskId.Add(fileRow.DiskId, fileRows);
                        }
                        fileRows.Add(fileRow);
                    }
                }

                foreach (Row row in wixPatchSymbolPathsTable.Rows.OrderBy(r => r, new WixPatchSymbolPathsComparer()))
                {
                    switch ((string)row[0])
                    {
                        case "File":
                            this.MergeSymbolPaths(row, (FileRow)fileRowsByFile[row[1]]);
                            break;

                        case "Product":
                            foreach (FileRow fileRow in fileRowsByFile)
                            {
                                this.MergeSymbolPaths(row, fileRow);
                            }
                            break;

                        case "Component":
                            ArrayList fileRowsByThisComponent = (ArrayList)(fileRowsByComponent[row[1]]);
                            if (null != fileRowsByThisComponent)
                            {
                                foreach (FileRow fileRow in fileRowsByThisComponent)
                                {
                                    this.MergeSymbolPaths(row, fileRow);
                                }
                            }

                            break;

                        case "Directory":
                            ArrayList fileRowsByThisDirectory = (ArrayList)(fileRowsByDirectory[row[1]]);
                            if (null != fileRowsByThisDirectory)
                            {
                                foreach (FileRow fileRow in fileRowsByThisDirectory)
                                {
                                    this.MergeSymbolPaths(row, fileRow);
                                }
                            }
                            break;

                        case "Media":
                            ArrayList fileRowsByThisDiskId = (ArrayList)(fileRowsByDiskId[row[1]]);
                            if (null != fileRowsByThisDiskId)
                            {
                                foreach (FileRow fileRow in fileRowsByThisDiskId)
                                {
                                    this.MergeSymbolPaths(row, fileRow);
                                }
                            }
                            break;

                        default:
                            // error
                            break;
                    }
                }
            }

            // copy data from the WixMedia rows into the Media rows
            Table wixMediaTable = tables["WixMedia"];
            if (null != wixMediaTable)
            {
                Table mediaTable = tables["Media"];

                // index all the Media rows by their identifier
                Hashtable indexedMediaRows = new Hashtable(mediaTable.Rows.Count);
                foreach (MediaRow mediaRow in mediaTable.Rows)
                {
                    indexedMediaRows.Add(mediaRow.DiskId, mediaRow);
                }

                foreach (Row row in wixMediaTable.Rows)
                {
                    MediaRow mediaRow = (MediaRow)indexedMediaRows[row[0]];

                    // compression level
                    if (null != row[1])
                    {
                        mediaRow.CompressionLevel = WixCreateCab.CompressionLevelFromString((string)row[1]);
                    }

                    // layout
                    if (null != row[2])
                    {
                        mediaRow.Layout = (string)row[2];
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve files and their information from merge modules.
        /// </summary>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        private void ProcessMergeModules(Output output, ICollection<FileRow> fileRows)
        {
            Table wixMergeTable = output.Tables["WixMerge"];
            if (null != wixMergeTable)
            {
                IMsmMerge2 merge = NativeMethods.GetMsmMerge();

                // Get the output's minimum installer version
                int outputInstallerVersion = int.MinValue;
                Table summaryInformationTable = output.Tables["_SummaryInformation"];
                if (null != summaryInformationTable)
                {
                    foreach (Row row in summaryInformationTable.Rows)
                    {
                        if (14 == (int)row[0])
                        {
                            outputInstallerVersion = Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                            break;
                        }
                    }
                }

                // Index all of the file rows to be able to detect collisions with files in the Merge Modules.
                // It may seem a bit expensive to build up this index solely for the purpose of checking collisions
                // and you may be thinking, "Surely, we must need the file rows indexed elsewhere." It turns out
                // there are other cases where we need all the file rows indexed, however they are not common cases.
                // Now since Merge Modules are already slow and generally less desirable than .wixlibs we'll let
                // this case be slightly more expensive because the cost of maintaining an indexed file row collection
                // is a lot more costly for the common cases.
                Dictionary<string, FileRow> indexedFileRows = fileRows.ToDictionary(r => r.File, StringComparer.Ordinal);

                foreach (Row row in wixMergeTable.Rows)
                {
                    bool containsFiles = false;
                    WixMergeRow wixMergeRow = (WixMergeRow)row;

                    try
                    {
                        // read the module's File table to get its FileMediaInformation entries and gather any other information needed from the module.
                        using (Database db = new Database(wixMergeRow.SourceFile, OpenDatabase.ReadOnly))
                        {
                            if (db.TableExists("File") && db.TableExists("Component"))
                            {
                                Dictionary<string, FileRow> uniqueModuleFileIdentifiers = new Dictionary<string, FileRow>(StringComparer.OrdinalIgnoreCase);

                                using (View view = db.OpenExecuteView("SELECT `File`, `Directory_` FROM `File`, `Component` WHERE `Component_`=`Component`"))
                                {
                                    // add each file row from the merge module into the file row collection (check for errors along the way)
                                    while (true)
                                    {
                                        using (Record record = view.Fetch())
                                        {
                                            if (null == record)
                                            {
                                                break;
                                            }

                                            // NOTE: this is very tricky - the merge module file rows are not added to the
                                            // file table because they should not be created via idt import.  Instead, these
                                            // rows are created by merging in the actual modules.
                                            FileRow fileRow = new FileRow(null, this.TableDefinitions["File"]);
                                            fileRow.File = record[1];
                                            fileRow.Compressed = wixMergeRow.FileCompression;
                                            fileRow.Directory = record[2];
                                            fileRow.DiskId = wixMergeRow.DiskId;
                                            fileRow.FromModule = true;
                                            fileRow.PatchGroup = -1;
                                            fileRow.Source = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, "MergeId.", wixMergeRow.Number.ToString(CultureInfo.InvariantCulture.NumberFormat), Path.DirectorySeparatorChar, record[1]);

                                            FileRow collidingFileRow;

                                            // If case-sensitive collision with another merge module or a user-authored file identifier.
                                            if (indexedFileRows.TryGetValue(fileRow.File, out collidingFileRow))
                                            {
                                                Messaging.Instance.OnMessage(WixErrors.DuplicateModuleFileIdentifier(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, collidingFileRow.File));
                                            }
                                            else if (uniqueModuleFileIdentifiers.TryGetValue(fileRow.File, out collidingFileRow)) // case-insensitive collision with another file identifier in the same merge module
                                            {
                                                Messaging.Instance.OnMessage(WixErrors.DuplicateModuleCaseInsensitiveFileIdentifier(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, fileRow.File, collidingFileRow.File));
                                            }
                                            else // no collision
                                            {
                                                fileRows.Add(fileRow);

                                                // Keep updating the indexes as new rows are added.
                                                indexedFileRows.Add(fileRow.File, fileRow);
                                                uniqueModuleFileIdentifiers.Add(fileRow.File, fileRow);
                                            }

                                            containsFiles = true;
                                        }
                                    }
                                }
                            }

                            // Get the summary information to detect the Schema
                            using (SummaryInformation summaryInformation = new SummaryInformation(db))
                            {
                                string moduleInstallerVersionString = summaryInformation.GetProperty(14);

                                try
                                {
                                    int moduleInstallerVersion = Convert.ToInt32(moduleInstallerVersionString, CultureInfo.InvariantCulture);
                                    if (moduleInstallerVersion > outputInstallerVersion)
                                    {
                                        Messaging.Instance.OnMessage(WixWarnings.InvalidHigherInstallerVersionInModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, moduleInstallerVersion, outputInstallerVersion));
                                    }
                                }
                                catch (FormatException)
                                {
                                    throw new WixException(WixErrors.MissingOrInvalidModuleInstallerVersion(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.SourceFile, moduleInstallerVersionString));
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        throw new WixException(WixErrors.FileNotFound(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile));
                    }
                    catch (Win32Exception)
                    {
                        throw new WixException(WixErrors.CannotOpenMergeModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.SourceFile));
                    }

                    // if the module has files and creating layout
                    if (containsFiles && !this.SuppressLayout)
                    {
                        bool moduleOpen = false;
                        short mergeLanguage;

                        try
                        {
                            mergeLanguage = Convert.ToInt16(wixMergeRow.Language, CultureInfo.InvariantCulture);
                        }
                        catch (System.FormatException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.Language));
                            continue;
                        }

                        try
                        {
                            merge.OpenModule(wixMergeRow.SourceFile, mergeLanguage);
                            moduleOpen = true;

                            string safeMergeId = wixMergeRow.Number.ToString(CultureInfo.InvariantCulture.NumberFormat);

                            // extract the module cabinet, then explode all of the files to a temp directory
                            string moduleCabPath = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, safeMergeId, ".module.cab");
                            merge.ExtractCAB(moduleCabPath);

                            string mergeIdPath = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, "MergeId.", safeMergeId);
                            Directory.CreateDirectory(mergeIdPath);

                            using (WixExtractCab extractCab = new WixExtractCab())
                            {
                                try
                                {
                                    extractCab.Extract(moduleCabPath, mergeIdPath);
                                }
                                catch (FileNotFoundException)
                                {
                                    throw new WixException(WixErrors.CabFileDoesNotExist(moduleCabPath, wixMergeRow.SourceFile, mergeIdPath));
                                }
                                catch
                                {
                                    throw new WixException(WixErrors.CabExtractionFailed(moduleCabPath, wixMergeRow.SourceFile, mergeIdPath));
                                }
                            }
                        }
                        catch (COMException ce)
                        {
                            throw new WixException(WixErrors.UnableToOpenModule(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile, ce.Message));
                        }
                        finally
                        {
                            if (moduleOpen)
                            {
                                merge.CloseModule();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copy file data between transform substorages and the patch output object
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="allFileRows">True if copying from transform to patch, false the other way.</param>
        private IEnumerable<FileRow> CopyFromTransformData(Output output)
        {
            CopyTransformDataCommand command = new CopyTransformDataCommand();
            command.CopyOutFileRows = true;
            command.FileManagerCore = this.FileManagerCore;
            command.FileManagers = this.FileManagers;
            command.Output = output;
            command.TableDefinitions = this.TableDefinitions;
            command.Execute();

            return command.FileRows;
        }

        /// <summary>
        /// Copy file data between transform substorages and the patch output object
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="allFileRows">True if copying from transform to patch, false the other way.</param>
        private void CopyToTransformData(Output output)
        {
            CopyTransformDataCommand command = new CopyTransformDataCommand();
            command.CopyOutFileRows = false;
            command.FileManagerCore = this.FileManagerCore;
            command.FileManagers = this.FileManagers;
            command.Output = output;
            command.TableDefinitions = this.TableDefinitions;
            command.Execute();
        }

        /// <summary>
        /// Merge data from a row in the WixPatchSymbolsPaths table into an associated FileRow.
        /// </summary>
        /// <param name="row">Row from the WixPatchSymbolsPaths table.</param>
        /// <param name="fileRow">FileRow into which to set symbol information.</param>
        /// <comment>This includes PreviousData as well.</comment>
        private void MergeSymbolPaths(Row row, FileRow fileRow)
        {
            if (null == fileRow.Symbols)
            {
                fileRow.Symbols = (string)row[2];
            }
            else
            {
                fileRow.Symbols = String.Concat(fileRow.Symbols, ";", (string)row[2]);
            }

            Field field = row.Fields[2];
            if (null != field.PreviousData)
            {
                if (null == fileRow.PreviousSymbols)
                {
                    fileRow.PreviousSymbols = field.PreviousData;
                }
                else
                {
                    fileRow.PreviousSymbols = String.Concat(fileRow.PreviousSymbols, ";", field.PreviousData);
                }
            }
        }

        /// <summary>
        /// Takes an id, and demodularizes it (if possible).
        /// </summary>
        /// <remarks>
        /// If the output type is a module, returns a demodularized version of an id. Otherwise, returns the id.
        /// </remarks>
        /// <param name="output">The output to bind.</param>
        /// <param name="modularizationGuid">The modularization GUID.</param>
        /// <param name="id">The id to demodularize.</param>
        /// <returns>The demodularized id.</returns>
        private string Demodularize(Output output, string modularizationGuid, string id)
        {
            if (OutputType.Module == output.Type && id.EndsWith(String.Concat(".", modularizationGuid), StringComparison.Ordinal))
            {
                id = id.Substring(0, id.Length - 37);
            }

            return id;
        }

        /// <summary>
        /// Merges in any modules to the output database.
        /// </summary>
        /// <param name="tempDatabaseFile">The temporary database file.</param>
        /// <param name="output">Output that specifies database and modules to merge.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        /// <param name="suppressedTableNames">The names of tables that are suppressed.</param>
        /// <remarks>Expects that output's database has already been generated.</remarks>
        private void MergeModules(string tempDatabaseFile, Output output, IEnumerable<FileRow> fileRows, HashSet<string> suppressedTableNames)
        {
            Debug.Assert(OutputType.Product == output.Type);

            Table wixMergeTable = output.Tables["WixMerge"];
            Table wixFeatureModulesTable = output.Tables["WixFeatureModules"];

            // check for merge rows to see if there is any work to do
            if (null == wixMergeTable || 0 == wixMergeTable.Rows.Count)
            {
                return;
            }

            IMsmMerge2 merge = null;
            bool commit = true;
            bool logOpen = false;
            bool databaseOpen = false;
            string logPath = null;
            try
            {
                merge = NativeMethods.GetMsmMerge();

                logPath = Path.Combine(this.TempFilesLocation, "merge.log");
                merge.OpenLog(logPath);
                logOpen = true;

                merge.OpenDatabase(tempDatabaseFile);
                databaseOpen = true;

                // process all the merge rows
                foreach (WixMergeRow wixMergeRow in wixMergeTable.Rows)
                {
                    bool moduleOpen = false;

                    try
                    {
                        short mergeLanguage;

                        try
                        {
                            mergeLanguage = Convert.ToInt16(wixMergeRow.Language, CultureInfo.InvariantCulture);
                        }
                        catch (System.FormatException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.Language));
                            continue;
                        }

                        Messaging.Instance.OnMessage(WixVerboses.OpeningMergeModule(wixMergeRow.SourceFile, mergeLanguage));
                        merge.OpenModule(wixMergeRow.SourceFile, mergeLanguage);
                        moduleOpen = true;

                        // If there is merge configuration data, create a callback object to contain it all.
                        ConfigurationCallback callback = null;
                        if (!String.IsNullOrEmpty(wixMergeRow.ConfigurationData))
                        {
                            callback = new ConfigurationCallback(wixMergeRow.ConfigurationData);
                        }

                        // merge the module into the database that's being built
                        Messaging.Instance.OnMessage(WixVerboses.MergingMergeModule(wixMergeRow.SourceFile));
                        merge.MergeEx(wixMergeRow.Feature, wixMergeRow.Directory, callback);

                        // connect any non-primary features
                        if (null != wixFeatureModulesTable)
                        {
                            foreach (Row row in wixFeatureModulesTable.Rows)
                            {
                                if (wixMergeRow.Id == (string)row[1])
                                {
                                    Messaging.Instance.OnMessage(WixVerboses.ConnectingMergeModule(wixMergeRow.SourceFile, (string)row[0]));
                                    merge.Connect((string)row[0]);
                                }
                            }
                        }
                    }
                    catch (COMException)
                    {
                        commit = false;
                    }
                    finally
                    {
                        IMsmErrors mergeErrors = merge.Errors;

                        // display all the errors encountered during the merge operations for this module
                        for (int i = 1; i <= mergeErrors.Count; i++)
                        {
                            IMsmError mergeError = mergeErrors[i];
                            StringBuilder databaseKeys = new StringBuilder();
                            StringBuilder moduleKeys = new StringBuilder();

                            // build a string of the database keys
                            for (int j = 1; j <= mergeError.DatabaseKeys.Count; j++)
                            {
                                if (1 != j)
                                {
                                    databaseKeys.Append(';');
                                }
                                databaseKeys.Append(mergeError.DatabaseKeys[j]);
                            }

                            // build a string of the module keys
                            for (int j = 1; j <= mergeError.ModuleKeys.Count; j++)
                            {
                                if (1 != j)
                                {
                                    moduleKeys.Append(';');
                                }
                                moduleKeys.Append(mergeError.ModuleKeys[j]);
                            }

                            // display the merge error based on the msm error type
                            switch (mergeError.Type)
                            {
                                case MsmErrorType.msmErrorExclusion:
                                    Messaging.Instance.OnMessage(WixErrors.MergeExcludedModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, moduleKeys.ToString()));
                                    break;
                                case MsmErrorType.msmErrorFeatureRequired:
                                    Messaging.Instance.OnMessage(WixErrors.MergeFeatureRequired(wixMergeRow.SourceLineNumbers, mergeError.ModuleTable, moduleKeys.ToString(), wixMergeRow.SourceFile, wixMergeRow.Id));
                                    break;
                                case MsmErrorType.msmErrorLanguageFailed:
                                    Messaging.Instance.OnMessage(WixErrors.MergeLanguageFailed(wixMergeRow.SourceLineNumbers, mergeError.Language, wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorLanguageUnsupported:
                                    Messaging.Instance.OnMessage(WixErrors.MergeLanguageUnsupported(wixMergeRow.SourceLineNumbers, mergeError.Language, wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorResequenceMerge:
                                    Messaging.Instance.OnMessage(WixWarnings.MergeRescheduledAction(wixMergeRow.SourceLineNumbers, mergeError.DatabaseTable, databaseKeys.ToString(), wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorTableMerge:
                                    if ("_Validation" != mergeError.DatabaseTable) // ignore merge errors in the _Validation table
                                    {
                                        Messaging.Instance.OnMessage(WixWarnings.MergeTableFailed(wixMergeRow.SourceLineNumbers, mergeError.DatabaseTable, databaseKeys.ToString(), wixMergeRow.SourceFile));
                                    }
                                    break;
                                case MsmErrorType.msmErrorPlatformMismatch:
                                    Messaging.Instance.OnMessage(WixErrors.MergePlatformMismatch(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile));
                                    break;
                                default:
                                    Messaging.Instance.OnMessage(WixErrors.UnexpectedException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_UnexpectedMergerErrorWithType, Enum.GetName(typeof(MsmErrorType), mergeError.Type), logPath), "InvalidOperationException", Environment.StackTrace));
                                    break;
                            }
                        }

                        if (0 >= mergeErrors.Count && !commit)
                        {
                            Messaging.Instance.OnMessage(WixErrors.UnexpectedException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_UnexpectedMergerErrorInSourceFile, wixMergeRow.SourceFile, logPath), "InvalidOperationException", Environment.StackTrace));
                        }

                        if (moduleOpen)
                        {
                            merge.CloseModule();
                        }
                    }
                }
            }
            finally
            {
                if (databaseOpen)
                {
                    merge.CloseDatabase(commit);
                }

                if (logOpen)
                {
                    merge.CloseLog();
                }
            }

            // stop processing if an error previously occurred
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            using (Database db = new Database(tempDatabaseFile, OpenDatabase.Direct))
            {
                Table suppressActionTable = output.Tables["WixSuppressAction"];

                // suppress individual actions
                if (null != suppressActionTable)
                {
                    foreach (Row row in suppressActionTable.Rows)
                    {
                        if (db.TableExists((string)row[0]))
                        {
                            string query = String.Format(CultureInfo.InvariantCulture, "SELECT * FROM {0} WHERE `Action` = '{1}'", row[0].ToString(), (string)row[1]);

                            using (View view = db.OpenExecuteView(query))
                            {
                                using (Record record = view.Fetch())
                                {
                                    if (null != record)
                                    {
                                        Messaging.Instance.OnMessage(WixWarnings.SuppressMergedAction((string)row[1], row[0].ToString()));
                                        view.Modify(ModifyView.Delete, record);
                                    }
                                }
                            }
                        }
                    }
                }

                // query for merge module actions in suppressed sequences and drop them
                foreach (string tableName in suppressedTableNames)
                {
                    if (!db.TableExists(tableName))
                    {
                        continue;
                    }

                    using (View view = db.OpenExecuteView(String.Concat("SELECT `Action` FROM ", tableName)))
                    {
                        while (true)
                        {
                            using (Record resultRecord = view.Fetch())
                            {
                                if (null == resultRecord)
                                {
                                    break;
                                }

                                Messaging.Instance.OnMessage(WixWarnings.SuppressMergedAction(resultRecord.GetString(1), tableName));
                            }
                        }
                    }

                    // drop suppressed sequences
                    using (View view = db.OpenExecuteView(String.Concat("DROP TABLE ", tableName)))
                    {
                    }

                    // delete the validation rows
                    using (View view = db.OpenView(String.Concat("DELETE FROM _Validation WHERE `Table` = ?")))
                    {
                        using (Record record = new Record(1))
                        {
                            record.SetString(1, tableName);
                            view.Execute(record);
                        }
                    }
                }

                // now update the Attributes column for the files from the Merge Modules
                Messaging.Instance.OnMessage(WixVerboses.ResequencingMergeModuleFiles());
                using (View view = db.OpenView("SELECT `Sequence`, `Attributes` FROM `File` WHERE `File`=?"))
                {
                    foreach (FileRow fileRow in fileRows)
                    {
                        if (!fileRow.FromModule)
                        {
                            continue;
                        }

                        using (Record record = new Record(1))
                        {
                            record.SetString(1, fileRow.File);
                            view.Execute(record);
                        }

                        using (Record recordUpdate = view.Fetch())
                        {
                            if (null == recordUpdate)
                            {
                                throw new InvalidOperationException("Failed to fetch a File row from the database that was merged in from a module.");
                            }

                            recordUpdate.SetInteger(1, fileRow.Sequence);

                            // update the file attributes to match the compression specified
                            // on the Merge element or on the Package element
                            int attributes = 0;

                            // get the current value if its not null
                            if (!recordUpdate.IsNull(2))
                            {
                                attributes = recordUpdate.GetInteger(2);
                            }

                            if (YesNoType.Yes == fileRow.Compressed)
                            {
                                // these are mutually exclusive
                                attributes |= MsiInterop.MsidbFileAttributesCompressed;
                                attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                            }
                            else if (YesNoType.No == fileRow.Compressed)
                            {
                                // these are mutually exclusive
                                attributes |= MsiInterop.MsidbFileAttributesNoncompressed;
                                attributes &= ~MsiInterop.MsidbFileAttributesCompressed;
                            }
                            else // not specified
                            {
                                Debug.Assert(YesNoType.NotSet == fileRow.Compressed);

                                // clear any compression bits
                                attributes &= ~MsiInterop.MsidbFileAttributesCompressed;
                                attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                            }
                            recordUpdate.SetInteger(2, attributes);

                            view.Modify(ModifyView.Update, recordUpdate);
                        }
                    }
                }

                db.Commit();
            }
        }

        private void UpdateMediaSequences(Output output, IEnumerable<FileRow> fileRows, RowDictionary<MediaRow> mediaRows)
        {
            // Index for all the fileId's
            // NOTE: When dealing with patches, there is a file table for each transform. In most cases, the data in these rows will be the same, however users of this index need to be aware of this.
            Table mediaTable = output.Tables["Media"];

            // calculate sequence numbers and media disk id layout for all file media information objects
            if (OutputType.Module == output.Type)
            {
                int lastSequence = 0;
                foreach (FileRow fileRow in fileRows) // TODO: Sort these rows directory path and component id and maybe file size or file extension and other creative ideas to get optimal install speed out of MSI.
                {
                    fileRow.Sequence = ++lastSequence;
                }
            }
            else if (null != mediaTable)
            {
                int lastSequence = 0;
                MediaRow mediaRow = null;
                SortedList patchGroups = new SortedList();

                // sequence the non-patch-added files
                foreach (FileRow fileRow in fileRows) // TODO: Sort these rows directory path and component id and maybe file size or file extension and other creative ideas to get optimal install speed out of MSI.
                {
                    if (null == mediaRow)
                    {
                        mediaRow = mediaRows.Get(fileRow.DiskId);
                        if (OutputType.Patch == output.Type)
                        {
                            // patch Media cannot start at zero
                            lastSequence = mediaRow.LastSequence;
                        }
                    }
                    else if (mediaRow.DiskId != fileRow.DiskId)
                    {
                        mediaRow.LastSequence = lastSequence;
                        mediaRow = mediaRows.Get(fileRow.DiskId);
                    }

                    if (0 < fileRow.PatchGroup)
                    {
                        ArrayList patchGroup = (ArrayList)patchGroups[fileRow.PatchGroup];

                        if (null == patchGroup)
                        {
                            patchGroup = new ArrayList();
                            patchGroups.Add(fileRow.PatchGroup, patchGroup);
                        }

                        patchGroup.Add(fileRow);
                    }
                    else
                    {
                        fileRow.Sequence = ++lastSequence;
                    }
                }

                if (null != mediaRow)
                {
                    mediaRow.LastSequence = lastSequence;
                    mediaRow = null;
                }

                // sequence the patch-added files
                foreach (ArrayList patchGroup in patchGroups.Values)
                {
                    foreach (FileRow fileRow in patchGroup)
                    {
                        if (null == mediaRow)
                        {
                            mediaRow = mediaRows.Get(fileRow.DiskId);
                        }
                        else if (mediaRow.DiskId != fileRow.DiskId)
                        {
                            mediaRow.LastSequence = lastSequence;
                            mediaRow = mediaRows.Get(fileRow.DiskId);
                        }

                        fileRow.Sequence = ++lastSequence;
                    }
                }

                if (null != mediaRow)
                {
                    mediaRow.LastSequence = lastSequence;
                }
            }
        }

        /// <summary>
        /// Update several msi tables with data contained in files references in the File table.
        /// </summary>
        /// <remarks>
        /// For versioned files, update the file version and language in the File table.  For
        /// unversioned files, add a row to the MsiFileHash table for the file.  For assembly
        /// files, add a row to the MsiAssembly table and add AssemblyName information by adding
        /// MsiAssemblyName rows.
        /// </remarks>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        /// <param name="allfileRows">Collection of all the file rows processed to this point.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        /// <param name="mediaRows">The indexed media rows.</param>
        /// <param name="infoCache">A hashtable to populate with the file information (optional).</param>
        /// <param name="modularizationGuid">The modularization guid (used in case of a merge module).</param>
        private void UpdateFileRow(Output output, IDictionary<string, string> infoCache, string modularizationGuid, IEnumerable<FileRow> allfileRows, FileRow fileRow, bool overwriteHash)
        {
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(fileRow.Source);
            }
            catch (ArgumentException)
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                return;
            }
            catch (PathTooLongException)
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                return;
            }
            catch (NotSupportedException)
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                return;
            }

            if (!fileInfo.Exists)
            {
                Messaging.Instance.OnMessage(WixErrors.CannotFindFile(fileRow.SourceLineNumbers, fileRow.File, fileRow.FileName, fileRow.Source));
                return;
            }

            using (FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (Int32.MaxValue < fileStream.Length)
                {
                    throw new WixException(WixErrors.FileTooLarge(fileRow.SourceLineNumbers, fileRow.Source));
                }

                fileRow.FileSize = Convert.ToInt32(fileStream.Length, CultureInfo.InvariantCulture);
            }

            string version = null;
            string language = null;
            try
            {
                Installer.GetFileVersion(fileInfo.FullName, out version, out language);
            }
            catch (Win32Exception e)
            {
                if (0x2 == e.NativeErrorCode) // ERROR_FILE_NOT_FOUND
                {
                    throw new WixException(WixErrors.FileNotFound(fileRow.SourceLineNumbers, fileInfo.FullName));
                }
                else
                {
                    throw new WixException(WixErrors.Win32Exception(e.NativeErrorCode, e.Message));
                }
            }

            // If there is no version, it is assumed there is no language because it won't matter in the versioning of the install.
            if (String.IsNullOrEmpty(version)) // unversioned files have their hashes added to the MsiFileHash table
            {
                if (!overwriteHash)
                {
                    // not overwriting hash, so don't do the rest of these options.
                }
                else if (null != fileRow.Version)
                {
                    // Search all of the file rows available to see if the specified version is actually a companion file. Yes, this looks
                    // very expensive and you're probably thinking it would be better to create an index of some sort to do an O(1) look up.
                    // That's a reasonable thought but companion file usage is usually pretty rare so we'd be doing something expensive (indexing
                    // all the file rows) for a relatively uncommon situation. Let's not do that.
                    //
                    // Also, if we do not find a matching file identifier then the user provided a default version and is providing a version
                    // for unversioned file. That's allowed but generally a dangerous thing to do so let's point that out to the user.
                    if (!allfileRows.Any(r => fileRow.Version.Equals(r.File, StringComparison.Ordinal)))
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DefaultVersionUsedForUnversionedFile(fileRow.SourceLineNumbers, fileRow.Version, fileRow.File));
                    }
                }
                else
                {
                    if (null != fileRow.Language)
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DefaultLanguageUsedForUnversionedFile(fileRow.SourceLineNumbers, fileRow.Language, fileRow.File));
                    }

                    int[] hash;
                    try
                    {
                        Installer.GetFileHash(fileInfo.FullName, 0, out hash);
                    }
                    catch (Win32Exception e)
                    {
                        if (0x2 == e.NativeErrorCode) // ERROR_FILE_NOT_FOUND
                        {
                            throw new WixException(WixErrors.FileNotFound(fileRow.SourceLineNumbers, fileInfo.FullName));
                        }
                        else
                        {
                            throw new WixException(WixErrors.Win32Exception(e.NativeErrorCode, fileInfo.FullName, e.Message));
                        }
                    }

                    if (null == fileRow.HashRow)
                    {
                        Table msiFileHashTable = output.EnsureTable(this.TableDefinitions["MsiFileHash"]);
                        fileRow.HashRow = msiFileHashTable.CreateRow(fileRow.SourceLineNumbers);
                    }

                    fileRow.HashRow[0] = fileRow.File;
                    fileRow.HashRow[1] = 0;
                    fileRow.HashRow[2] = hash[0];
                    fileRow.HashRow[3] = hash[1];
                    fileRow.HashRow[4] = hash[2];
                    fileRow.HashRow[5] = hash[3];
                }
            }
            else // update the file row with the version and language information.
            {
                // If no version was provided by the user, use the version from the file itself.
                // This is the most common case.
                if (String.IsNullOrEmpty(fileRow.Version))
                {
                    fileRow.Version = version;
                }
                else if (!allfileRows.Any(r => fileRow.Version.Equals(r.File, StringComparison.Ordinal))) // this looks expensive, but see explanation below.
                {
                    // The user provided a default version for the file row so we looked for a companion file (a file row with Id matching
                    // the version value). We didn't find it so, we will override the default version they provided with the actual
                    // version from the file itself. Now, I know it looks expensive to search through all the file rows trying to match
                    // on the Id. However, the alternative is to build a big index of all file rows to do look ups. Since this case
                    // where the file version is already present is rare (companion files are pretty uncommon), we'll do the more
                    // CPU intensive search to save on the memory intensive index that wouldn't be used much.
                    //
                    // Also note this case can occur when the file is being updated using the WixBindUpdatedFiles extension mechanism.
                    // That's typically even more rare than companion files so again, no index, just search.
                    fileRow.Version = version;
                }

                if (!String.IsNullOrEmpty(fileRow.Language) && String.IsNullOrEmpty(language))
                {
                    Messaging.Instance.OnMessage(WixWarnings.DefaultLanguageUsedForVersionedFile(fileRow.SourceLineNumbers, fileRow.Language, fileRow.File));
                }
                else // override the default provided by the user (usually nothing) with the actual language from the file itself.
                {
                    fileRow.Language = language;
                }

                // Populate the binder variables for this file information if requested.
                if (null != infoCache)
                {
                    if (!String.IsNullOrEmpty(fileRow.Version))
                    {
                        string key = String.Format(CultureInfo.InvariantCulture, "fileversion.{0}", Demodularize(output, modularizationGuid, fileRow.File));
                        infoCache[key] = fileRow.Version;
                    }

                    if (!String.IsNullOrEmpty(fileRow.Language))
                    {
                        string key = String.Format(CultureInfo.InvariantCulture, "filelanguage.{0}", Demodularize(output, modularizationGuid, fileRow.File));
                        infoCache[key] = fileRow.Language;
                    }
                }
            }

            // If this is a CLR assembly, load the assembly and get the assembly name information
            if (FileAssemblyType.DotNetAssembly == fileRow.AssemblyType)
            {
                bool targetNetfx1 = false;
                StringDictionary assemblyNameValues = new StringDictionary();

                ClrInterop.IReferenceIdentity referenceIdentity = null;
                Guid referenceIdentityGuid = ClrInterop.ReferenceIdentityGuid;
                uint result = ClrInterop.GetAssemblyIdentityFromFile(fileInfo.FullName, ref referenceIdentityGuid, out referenceIdentity);
                if (0 == result && null != referenceIdentity)
                {
                    string imageRuntimeVersion = referenceIdentity.GetAttribute(null, "ImageRuntimeVersion");
                    if (null != imageRuntimeVersion)
                    {
                        targetNetfx1 = imageRuntimeVersion.StartsWith("v1", StringComparison.OrdinalIgnoreCase);
                    }

                    string culture = referenceIdentity.GetAttribute(null, "Culture");
                    if (null != culture)
                    {
                        assemblyNameValues.Add("Culture", culture);
                    }
                    else
                    {
                        assemblyNameValues.Add("Culture", "neutral");
                    }

                    string name = referenceIdentity.GetAttribute(null, "Name");
                    if (null != name)
                    {
                        assemblyNameValues.Add("Name", name);
                    }

                    string processorArchitecture = referenceIdentity.GetAttribute(null, "ProcessorArchitecture");
                    if (null != processorArchitecture)
                    {
                        assemblyNameValues.Add("ProcessorArchitecture", processorArchitecture);
                    }

                    string publicKeyToken = referenceIdentity.GetAttribute(null, "PublicKeyToken");
                    if (null != publicKeyToken)
                    {
                        bool publicKeyIsNeutral = (String.Equals(publicKeyToken, "neutral", StringComparison.OrdinalIgnoreCase));

                        // Managed code expects "null" instead of "neutral", and
                        // this won't be installed to the GAC since it's not signed anyway.
                        assemblyNameValues.Add("publicKeyToken", publicKeyIsNeutral ? "null" : publicKeyToken.ToUpperInvariant());
                        assemblyNameValues.Add("publicKeyTokenPreservedCase", publicKeyIsNeutral ? "null" : publicKeyToken);
                    }
                    else if (fileRow.AssemblyApplication == null)
                    {
                        throw new WixException(WixErrors.GacAssemblyNoStrongName(fileRow.SourceLineNumbers, fileInfo.FullName, fileRow.Component));
                    }

                    string assemblyVersion = referenceIdentity.GetAttribute(null, "Version");
                    if (null != version)
                    {
                        assemblyNameValues.Add("Version", assemblyVersion);
                    }
                }
                else
                {
                    Messaging.Instance.OnMessage(WixErrors.InvalidAssemblyFile(fileRow.SourceLineNumbers, fileInfo.FullName, String.Format(CultureInfo.InvariantCulture, "HRESULT: 0x{0:x8}", result)));
                    return;
                }

                Table assemblyNameTable = output.EnsureTable(this.TableDefinitions["MsiAssemblyName"]);
                if (assemblyNameValues.ContainsKey("name"))
                {
                    this.SetMsiAssemblyName(output, assemblyNameTable, fileRow, "name", assemblyNameValues["name"], infoCache, modularizationGuid);
                }

                if (!String.IsNullOrEmpty(version))
                {
                    this.SetMsiAssemblyName(output, assemblyNameTable, fileRow, "fileVersion", version, infoCache, modularizationGuid);
                }

                if (assemblyNameValues.ContainsKey("version"))
                {
                    string assemblyVersion = assemblyNameValues["version"];

                    if (!targetNetfx1)
                    {
                        // There is a bug in v1 fusion that requires the assembly's "version" attribute
                        // to be equal to or longer than the "fileVersion" in length when its present;
                        // the workaround is to prepend zeroes to the last version number in the assembly
                        // version.
                        if (null != version && version.Length > assemblyVersion.Length)
                        {
                            string padding = new string('0', version.Length - assemblyVersion.Length);
                            string[] assemblyVersionNumbers = assemblyVersion.Split('.');

                            if (assemblyVersionNumbers.Length > 0)
                            {
                                assemblyVersionNumbers[assemblyVersionNumbers.Length - 1] = String.Concat(padding, assemblyVersionNumbers[assemblyVersionNumbers.Length - 1]);
                                assemblyVersion = String.Join(".", assemblyVersionNumbers);
                            }
                        }
                    }

                    this.SetMsiAssemblyName(output, assemblyNameTable, fileRow, "version", assemblyVersion, infoCache, modularizationGuid);
                }

                if (assemblyNameValues.ContainsKey("culture"))
                {
                    this.SetMsiAssemblyName(output, assemblyNameTable, fileRow, "culture", assemblyNameValues["culture"], infoCache, modularizationGuid);
                }

                if (assemblyNameValues.ContainsKey("publicKeyToken"))
                {
                    this.SetMsiAssemblyName(output, assemblyNameTable, fileRow, "publicKeyToken", assemblyNameValues["publicKeyToken"], infoCache, modularizationGuid);
                }

                if (!String.IsNullOrEmpty(fileRow.ProcessorArchitecture))
                {
                    this.SetMsiAssemblyName(output, assemblyNameTable, fileRow, "processorArchitecture", fileRow.ProcessorArchitecture, infoCache, modularizationGuid);
                }

                if (assemblyNameValues.ContainsKey("processorArchitecture"))
                {
                    this.SetMsiAssemblyName(output, assemblyNameTable, fileRow, "processorArchitecture", assemblyNameValues["processorArchitecture"], infoCache, modularizationGuid);
                }

                // add the assembly name to the information cache
                if (null != infoCache)
                {
                    string fileId = Demodularize(output, modularizationGuid, fileRow.File);
                    string key = String.Concat("assemblyfullname.", fileId);
                    string assemblyName = String.Concat(assemblyNameValues["name"], ", version=", assemblyNameValues["version"], ", culture=", assemblyNameValues["culture"], ", publicKeyToken=", String.IsNullOrEmpty(assemblyNameValues["publicKeyToken"]) ? "null" : assemblyNameValues["publicKeyToken"]);
                    if (assemblyNameValues.ContainsKey("processorArchitecture"))
                    {
                        assemblyName = String.Concat(assemblyName, ", processorArchitecture=", assemblyNameValues["processorArchitecture"]);
                    }

                    infoCache[key] = assemblyName;

                    // Add entries with the preserved case publicKeyToken
                    string pcAssemblyNameKey = String.Concat("assemblyfullnamepreservedcase.", fileId);
                    infoCache[pcAssemblyNameKey] = (assemblyNameValues["publicKeyToken"] == assemblyNameValues["publicKeyTokenPreservedCase"]) ? assemblyName : assemblyName.Replace(assemblyNameValues["publicKeyToken"], assemblyNameValues["publicKeyTokenPreservedCase"]);

                    string pcPublicKeyTokenKey = String.Concat("assemblypublickeytokenpreservedcase.", fileId);
                    infoCache[pcPublicKeyTokenKey] = assemblyNameValues["publicKeyTokenPreservedCase"];
                }
            }
            else if (FileAssemblyType.Win32Assembly == fileRow.AssemblyType)
            {
                // TODO: Consider passing in the allFileRows as an indexed collection instead of searching through all files like this. Even those this is a rare case
                // it looks like we might be able to index the file earlier.
                FileRow fileManifestRow = allfileRows.SingleOrDefault(r => r.File.Equals(fileRow.AssemblyManifest, StringComparison.Ordinal));
                if (null == fileManifestRow)
                {
                    Messaging.Instance.OnMessage(WixErrors.MissingManifestForWin32Assembly(fileRow.SourceLineNumbers, fileRow.File, fileRow.AssemblyManifest));
                }

                string win32Type = null;
                string win32Name = null;
                string win32Version = null;
                string win32ProcessorArchitecture = null;
                string win32PublicKeyToken = null;

                // loading the dom is expensive we want more performant APIs than the DOM
                // Navigator is cheaper than dom.  Perhaps there is a cheaper API still.
                try
                {
                    XPathDocument doc = new XPathDocument(fileManifestRow.Source);
                    XPathNavigator nav = doc.CreateNavigator();
                    nav.MoveToRoot();

                    // this assumes a particular schema for a win32 manifest and does not
                    // provide error checking if the file does not conform to schema.
                    // The fallback case here is that nothing is added to the MsiAssemblyName
                    // table for an out of tolerance Win32 manifest.  Perhaps warnings needed.
                    if (nav.MoveToFirstChild())
                    {
                        while (nav.NodeType != XPathNodeType.Element || nav.Name != "assembly")
                        {
                            nav.MoveToNext();
                        }

                        if (nav.MoveToFirstChild())
                        {
                            bool hasNextSibling = true;
                            while (nav.NodeType != XPathNodeType.Element || nav.Name != "assemblyIdentity" && hasNextSibling)
                            {
                                hasNextSibling = nav.MoveToNext();
                            }
                            if (!hasNextSibling)
                            {
                                Messaging.Instance.OnMessage(WixErrors.InvalidManifestContent(fileRow.SourceLineNumbers, fileManifestRow.Source));
                                return;
                            }

                            if (nav.MoveToAttribute("type", String.Empty))
                            {
                                win32Type = nav.Value;
                                nav.MoveToParent();
                            }

                            if (nav.MoveToAttribute("name", String.Empty))
                            {
                                win32Name = nav.Value;
                                nav.MoveToParent();
                            }

                            if (nav.MoveToAttribute("version", String.Empty))
                            {
                                win32Version = nav.Value;
                                nav.MoveToParent();
                            }

                            if (nav.MoveToAttribute("processorArchitecture", String.Empty))
                            {
                                win32ProcessorArchitecture = nav.Value;
                                nav.MoveToParent();
                            }

                            if (nav.MoveToAttribute("publicKeyToken", String.Empty))
                            {
                                win32PublicKeyToken = nav.Value;
                                nav.MoveToParent();
                            }
                        }
                    }
                }
                catch (FileNotFoundException fe)
                {
                    Messaging.Instance.OnMessage(WixErrors.FileNotFound(new SourceLineNumber(fileManifestRow.Source), fe.FileName, "AssemblyManifest"));
                }
                catch (XmlException xe)
                {
                    Messaging.Instance.OnMessage(WixErrors.InvalidXml(new SourceLineNumber(fileManifestRow.Source), "manifest", xe.Message));
                }

                Table assemblyNameTable = output.EnsureTable(this.TableDefinitions["MsiAssemblyName"]);
                if (!String.IsNullOrEmpty(win32Name))
                {
                    SetMsiAssemblyName(output, assemblyNameTable, fileRow, "name", win32Name, infoCache, modularizationGuid);
                }

                if (!String.IsNullOrEmpty(win32Version))
                {
                    SetMsiAssemblyName(output, assemblyNameTable, fileRow, "version", win32Version, infoCache, modularizationGuid);
                }

                if (!String.IsNullOrEmpty(win32Type))
                {
                    SetMsiAssemblyName(output, assemblyNameTable, fileRow, "type", win32Type, infoCache, modularizationGuid);
                }

                if (!String.IsNullOrEmpty(win32ProcessorArchitecture))
                {
                    SetMsiAssemblyName(output, assemblyNameTable, fileRow, "processorArchitecture", win32ProcessorArchitecture, infoCache, modularizationGuid);
                }

                if (!String.IsNullOrEmpty(win32PublicKeyToken))
                {
                    SetMsiAssemblyName(output, assemblyNameTable, fileRow, "publicKeyToken", win32PublicKeyToken, infoCache, modularizationGuid);
                }
            }
        }

        /// <summary>
        /// Set the guids for components with generatable guids.
        /// </summary>
        /// <param name="output">Internal representation of the database to operate on.</param>
        private void SetComponentGuids(Output output)
        {
            Table componentTable = output.Tables["Component"];
            if (null != componentTable)
            {
                Hashtable registryKeyRows = null;
                Hashtable directories = null;
                Hashtable componentIdGenSeeds = null;
                Dictionary<string, List<FileRow>> fileRows = null;

                // find components with generatable guids
                foreach (ComponentRow componentRow in componentTable.Rows)
                {
                    // component guid will be generated
                    if ("*" == componentRow.Guid)
                    {
                        if (null == componentRow.KeyPath || componentRow.IsOdbcDataSourceKeyPath)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalComponentWithAutoGeneratedGuid(componentRow.SourceLineNumbers));
                        }
                        else if (componentRow.IsRegistryKeyPath)
                        {
                            if (null == registryKeyRows)
                            {
                                Table registryTable = output.Tables["Registry"];

                                registryKeyRows = new Hashtable(registryTable.Rows.Count);

                                foreach (Row registryRow in registryTable.Rows)
                                {
                                    registryKeyRows.Add((string)registryRow[0], registryRow);
                                }
                            }

                            Row foundRow = registryKeyRows[componentRow.KeyPath] as Row;

                            string bitness = componentRow.Is64Bit ? "64" : String.Empty;
                            if (null != foundRow)
                            {
                                string regkey = String.Concat(bitness, foundRow[1], "\\", foundRow[2], "\\", foundRow[3]);
                                componentRow.Guid = Uuid.NewUuid(BindDatabaseCommand.WixComponentGuidNamespace, regkey.ToLowerInvariant()).ToString("B").ToUpperInvariant();
                            }
                        }
                        else // must be a File KeyPath
                        {
                            // if the directory table hasn't been loaded into an indexed hash
                            // of directory ids to target names do that now.
                            if (null == directories)
                            {
                                Table directoryTable = output.Tables["Directory"];

                                int numDirectoryTableRows = (null != directoryTable) ? directoryTable.Rows.Count : 0;

                                directories = new Hashtable(numDirectoryTableRows);

                                // get the target paths for all directories
                                if (null != directoryTable)
                                {
                                    foreach (Row row in directoryTable.Rows)
                                    {
                                        // if the directory Id already exists, we will skip it here since
                                        // checking for duplicate primary keys is done later when importing tables
                                        // into database
                                        if (directories.ContainsKey(row[0]))
                                        {
                                            continue;
                                        }

                                        string targetName = Installer.GetName((string)row[2], false, true);
                                        directories.Add(row[0], new ResolvedDirectory((string)row[1], targetName));
                                    }
                                }
                            }

                            // if the component id generation seeds have not been indexed
                            // from the WixDirectory table do that now.
                            if (null == componentIdGenSeeds)
                            {
                                Table wixDirectoryTable = output.Tables["WixDirectory"];

                                int numWixDirectoryRows = (null != wixDirectoryTable) ? wixDirectoryTable.Rows.Count : 0;

                                componentIdGenSeeds = new Hashtable(numWixDirectoryRows);

                                // if there are any WixDirectory rows, build up the Component Guid
                                // generation seeds indexed by Directory/@Id.
                                if (null != wixDirectoryTable)
                                {
                                    foreach (Row row in wixDirectoryTable.Rows)
                                    {
                                        componentIdGenSeeds.Add(row[0], (string)row[1]);
                                    }
                                }
                            }

                            // if the file rows have not been indexed by File.Component yet
                            // then do that now
                            if (null == fileRows)
                            {
                                Table fileTable = output.Tables["File"];

                                int numFileRows = (null != fileTable) ? fileTable.Rows.Count : 0;

                                fileRows = new Dictionary<string, List<FileRow>>(numFileRows);

                                if (null != fileTable)
                                {
                                    foreach (FileRow file in fileTable.Rows)
                                    {
                                        List<FileRow> files;
                                        if (!fileRows.TryGetValue(file.Component, out files))
                                        {
                                            files = new List<FileRow>();
                                            fileRows.Add(file.Component, files);
                                        }

                                        files.Add(file);
                                    }
                                }
                            }

                            // validate component meets all the conditions to have a generated guid
                            List<FileRow> currentComponentFiles = fileRows[componentRow.Component];
                            int numFilesInComponent = currentComponentFiles.Count;
                            string path = null;

                            foreach (FileRow fileRow in currentComponentFiles)
                            {
                                if (fileRow.File == componentRow.KeyPath)
                                {
                                    // calculate the key file's canonical target path
                                    string directoryPath = Binder.GetDirectoryPath(directories, componentIdGenSeeds, componentRow.Directory, true);
                                    string fileName = Installer.GetName(fileRow.FileName, false, true).ToLower(CultureInfo.InvariantCulture);
                                    path = Path.Combine(directoryPath, fileName);

                                    // find paths that are not canonicalized
                                    if (path.StartsWith(@"PersonalFolder\my pictures", StringComparison.Ordinal) ||
                                        path.StartsWith(@"ProgramFilesFolder\common files", StringComparison.Ordinal) ||
                                        path.StartsWith(@"ProgramMenuFolder\startup", StringComparison.Ordinal) ||
                                        path.StartsWith("TARGETDIR", StringComparison.Ordinal) ||
                                        path.StartsWith(@"StartMenuFolder\programs", StringComparison.Ordinal) ||
                                        path.StartsWith(@"WindowsFolder\fonts", StringComparison.Ordinal))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.IllegalPathForGeneratedComponentGuid(componentRow.SourceLineNumbers, fileRow.Component, path));
                                    }

                                    // if component has more than one file, the key path must be versioned
                                    if (1 < numFilesInComponent && String.IsNullOrEmpty(fileRow.Version))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.IllegalGeneratedGuidComponentUnversionedKeypath(componentRow.SourceLineNumbers));
                                    }
                                }
                                else
                                {
                                    // not a key path, so it must be an unversioned file if component has more than one file
                                    if (1 < numFilesInComponent && !String.IsNullOrEmpty(fileRow.Version))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.IllegalGeneratedGuidComponentVersionedNonkeypath(componentRow.SourceLineNumbers));
                                    }
                                }
                            }

                            // if the rules were followed, reward with a generated guid
                            if (!Messaging.Instance.EncounteredError)
                            {
                                componentRow.Guid = Uuid.NewUuid(BindDatabaseCommand.WixComponentGuidNamespace, path).ToString("B").ToUpperInvariant();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates instance transform substorages in the output.
        /// </summary>
        /// <param name="output">Output containing instance transform definitions.</param>
        private void CreateInstanceTransforms(Output output)
        {
            // Create and add substorages for instance transforms.
            Table wixInstanceTransformsTable = output.Tables["WixInstanceTransforms"];
            if (null != wixInstanceTransformsTable && 0 <= wixInstanceTransformsTable.Rows.Count)
            {
                string targetProductCode = null;
                string targetUpgradeCode = null;
                string targetProductVersion = null;

                Table targetSummaryInformationTable = output.Tables["_SummaryInformation"];
                Table targetPropertyTable = output.Tables["Property"];

                // Get the data from target database
                foreach (Row propertyRow in targetPropertyTable.Rows)
                {
                    if ("ProductCode" == (string)propertyRow[0])
                    {
                        targetProductCode = (string)propertyRow[1];
                    }
                    else if ("ProductVersion" == (string)propertyRow[0])
                    {
                        targetProductVersion = (string)propertyRow[1];
                    }
                    else if ("UpgradeCode" == (string)propertyRow[0])
                    {
                        targetUpgradeCode = (string)propertyRow[1];
                    }
                }

                // Index the Instance Component Rows.
                Dictionary<string, ComponentRow> instanceComponentGuids = new Dictionary<string, ComponentRow>();
                Table targetInstanceComponentTable = output.Tables["WixInstanceComponent"];
                if (null != targetInstanceComponentTable && 0 < targetInstanceComponentTable.Rows.Count)
                {
                    foreach (Row row in targetInstanceComponentTable.Rows)
                    {
                        // Build up all the instances, we'll get the Components rows from the real Component table.
                        instanceComponentGuids.Add((string)row[0], null);
                    }

                    Table targetComponentTable = output.Tables["Component"];
                    foreach (ComponentRow componentRow in targetComponentTable.Rows)
                    {
                        string component = (string)componentRow[0];
                        if (instanceComponentGuids.ContainsKey(component))
                        {
                            instanceComponentGuids[component] = componentRow;
                        }
                    }
                }

                // Generate the instance transforms
                foreach (Row instanceRow in wixInstanceTransformsTable.Rows)
                {
                    string instanceId = (string)instanceRow[0];

                    Output instanceTransform = new Output(instanceRow.SourceLineNumbers);
                    instanceTransform.Type = OutputType.Transform;
                    instanceTransform.Codepage = output.Codepage;

                    Table instanceSummaryInformationTable = instanceTransform.EnsureTable(this.TableDefinitions["_SummaryInformation"]);
                    string targetPlatformAndLanguage = null;

                    foreach (Row summaryInformationRow in targetSummaryInformationTable.Rows)
                    {
                        if (7 == (int)summaryInformationRow[0]) // PID_TEMPLATE
                        {
                            targetPlatformAndLanguage = (string)summaryInformationRow[1];
                        }

                        // Copy the row's data to the transform.
                        Row copyOfSummaryRow = instanceSummaryInformationTable.CreateRow(null);
                        copyOfSummaryRow[0] = summaryInformationRow[0];
                        copyOfSummaryRow[1] = summaryInformationRow[1];
                    }

                    // Modify the appropriate properties.
                    Table propertyTable = instanceTransform.EnsureTable(this.TableDefinitions["Property"]);

                    // Change the ProductCode property
                    string productCode = (string)instanceRow[2];
                    if ("*" == productCode)
                    {
                        productCode = Common.GenerateGuid();
                    }

                    Row productCodeRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                    productCodeRow.Operation = RowOperation.Modify;
                    productCodeRow.Fields[1].Modified = true;
                    productCodeRow[0] = "ProductCode";
                    productCodeRow[1] = productCode;

                    // Change the instance property
                    Row instanceIdRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                    instanceIdRow.Operation = RowOperation.Modify;
                    instanceIdRow.Fields[1].Modified = true;
                    instanceIdRow[0] = (string)instanceRow[1];
                    instanceIdRow[1] = instanceId;

                    if (null != instanceRow[3])
                    {
                        // Change the ProductName property
                        Row productNameRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                        productNameRow.Operation = RowOperation.Modify;
                        productNameRow.Fields[1].Modified = true;
                        productNameRow[0] = "ProductName";
                        productNameRow[1] = (string)instanceRow[3];
                    }

                    if (null != instanceRow[4])
                    {
                        // Change the UpgradeCode property
                        Row upgradeCodeRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                        upgradeCodeRow.Operation = RowOperation.Modify;
                        upgradeCodeRow.Fields[1].Modified = true;
                        upgradeCodeRow[0] = "UpgradeCode";
                        upgradeCodeRow[1] = instanceRow[4];

                        // Change the Upgrade table
                        Table targetUpgradeTable = output.Tables["Upgrade"];
                        if (null != targetUpgradeTable && 0 <= targetUpgradeTable.Rows.Count)
                        {
                            string upgradeId = (string)instanceRow[4];
                            Table upgradeTable = instanceTransform.EnsureTable(this.TableDefinitions["Upgrade"]);
                            foreach (Row row in targetUpgradeTable.Rows)
                            {
                                // In case they are upgrading other codes to this new product, leave the ones that don't match the
                                // Product.UpgradeCode intact.
                                if (targetUpgradeCode == (string)row[0])
                                {
                                    Row upgradeRow = upgradeTable.CreateRow(null);
                                    upgradeRow.Operation = RowOperation.Add;
                                    upgradeRow.Fields[0].Modified = true;
                                    // I was hoping to be able to RowOperation.Modify, but that didn't appear to function.
                                    // upgradeRow.Fields[0].PreviousData = (string)row[0];

                                    // Inserting a new Upgrade record with the updated UpgradeCode
                                    upgradeRow[0] = upgradeId;
                                    upgradeRow[1] = row[1];
                                    upgradeRow[2] = row[2];
                                    upgradeRow[3] = row[3];
                                    upgradeRow[4] = row[4];
                                    upgradeRow[5] = row[5];
                                    upgradeRow[6] = row[6];

                                    // Delete the old row
                                    Row upgradeRemoveRow = upgradeTable.CreateRow(null);
                                    upgradeRemoveRow.Operation = RowOperation.Delete;
                                    upgradeRemoveRow[0] = row[0];
                                    upgradeRemoveRow[1] = row[1];
                                    upgradeRemoveRow[2] = row[2];
                                    upgradeRemoveRow[3] = row[3];
                                    upgradeRemoveRow[4] = row[4];
                                    upgradeRemoveRow[5] = row[5];
                                    upgradeRemoveRow[6] = row[6];
                                }
                            }
                        }
                    }

                    // If there are instance Components generate new GUIDs for them.
                    if (0 < instanceComponentGuids.Count)
                    {
                        Table componentTable = instanceTransform.EnsureTable(this.TableDefinitions["Component"]);
                        foreach (ComponentRow targetComponentRow in instanceComponentGuids.Values)
                        {
                            string guid = targetComponentRow.Guid;
                            if (!String.IsNullOrEmpty(guid))
                            {
                                Row instanceComponentRow = componentTable.CreateRow(targetComponentRow.SourceLineNumbers);
                                instanceComponentRow.Operation = RowOperation.Modify;
                                instanceComponentRow.Fields[1].Modified = true;
                                instanceComponentRow[0] = targetComponentRow[0];
                                instanceComponentRow[1] = Uuid.NewUuid(BindDatabaseCommand.WixComponentGuidNamespace, String.Concat(guid, instanceId)).ToString("B").ToUpper(CultureInfo.InvariantCulture);
                                instanceComponentRow[2] = targetComponentRow[2];
                                instanceComponentRow[3] = targetComponentRow[3];
                                instanceComponentRow[4] = targetComponentRow[4];
                                instanceComponentRow[5] = targetComponentRow[5];
                            }
                        }
                    }

                    // Update the summary information
                    Hashtable summaryRows = new Hashtable(instanceSummaryInformationTable.Rows.Count);
                    foreach (Row row in instanceSummaryInformationTable.Rows)
                    {
                        summaryRows[row[0]] = row;

                        if ((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage == (int)row[0])
                        {
                            row[1] = targetPlatformAndLanguage;
                        }
                        else if ((int)SummaryInformation.Transform.ProductCodes == (int)row[0])
                        {
                            row[1] = String.Concat(targetProductCode, targetProductVersion, ';', productCode, targetProductVersion, ';', targetUpgradeCode);
                        }
                        else if ((int)SummaryInformation.Transform.ValidationFlags == (int)row[0])
                        {
                            row[1] = 0;
                        }
                        else if ((int)SummaryInformation.Transform.Security == (int)row[0])
                        {
                            row[1] = "4";
                        }
                    }

                    if (!summaryRows.Contains((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.UpdatedPlatformAndLanguage;
                        summaryRow[1] = targetPlatformAndLanguage;
                    }
                    else if (!summaryRows.Contains((int)SummaryInformation.Transform.ValidationFlags))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.ValidationFlags;
                        summaryRow[1] = "0";
                    }
                    else if (!summaryRows.Contains((int)SummaryInformation.Transform.Security))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.Security;
                        summaryRow[1] = "4";
                    }

                    output.SubStorages.Add(new SubStorage(instanceId, instanceTransform));
                }
            }
        }

        /// <summary>
        /// Validate that there are no duplicate GUIDs in the output.
        /// </summary>
        /// <remarks>
        /// Duplicate GUIDs without conditions are an error condition; with conditions, it's a
        /// warning, as the conditions might be mutually exclusive.
        /// </remarks>
        private void ValidateComponentGuids(Output output)
        {
            Table componentTable = output.Tables["Component"];
            if (null != componentTable)
            {
                Dictionary<string, bool> componentGuidConditions = new Dictionary<string, bool>(componentTable.Rows.Count);

                foreach (ComponentRow row in componentTable.Rows)
                {
                    // we don't care about unmanaged components and if there's a * GUID remaining,
                    // there's already an error that prevented it from being replaced with a real GUID.
                    if (!String.IsNullOrEmpty(row.Guid) && "*" != row.Guid)
                    {
                        bool thisComponentHasCondition = !String.IsNullOrEmpty(row.Condition);
                        bool allComponentsHaveConditions = thisComponentHasCondition;

                        if (componentGuidConditions.ContainsKey(row.Guid))
                        {
                            allComponentsHaveConditions = componentGuidConditions[row.Guid] && thisComponentHasCondition;

                            if (allComponentsHaveConditions)
                            {
                                Messaging.Instance.OnMessage(WixWarnings.DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                            else
                            {
                                Messaging.Instance.OnMessage(WixErrors.DuplicateComponentGuids(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                        }

                        componentGuidConditions[row.Guid] = allComponentsHaveConditions;
                    }
                }
            }
        }

        /// <summary>
        /// Update Control and BBControl text by reading from files when necessary.
        /// </summary>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        private void UpdateControlText(Output output)
        {
            UpdateControlTextCommand command = new UpdateControlTextCommand();
            command.BBControlTable = output.Tables["BBControl"];
            command.WixBBControlTable = output.Tables["WixBBControl"];
            command.ControlTable = output.Tables["Control"];
            command.WixControlTable = output.Tables["WixControl"];
            command.Execute();
        }

        /// <summary>
        /// Set an MsiAssemblyName row.  If it was directly authored, override the value, otherwise
        /// create a new row.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="assemblyNameTable">MsiAssemblyName table.</param>
        /// <param name="fileRow">FileRow containing the assembly read for the MsiAssemblyName row.</param>
        /// <param name="name">MsiAssemblyName name.</param>
        /// <param name="value">MsiAssemblyName value.</param>
        /// <param name="infoCache">Cache to populate with file information (optional).</param>
        /// <param name="modularizationGuid">The modularization GUID (in the case of merge modules).</param>
        private void SetMsiAssemblyName(Output output, Table assemblyNameTable, FileRow fileRow, string name, string value, IDictionary<string, string> infoCache, string modularizationGuid)
        {
            // check for null value (this can occur when grabbing the file version from an assembly without one)
            if (String.IsNullOrEmpty(value))
            {
                Messaging.Instance.OnMessage(WixWarnings.NullMsiAssemblyNameValue(fileRow.SourceLineNumbers, fileRow.Component, name));
            }
            else
            {
                Row assemblyNameRow = null;

                // override directly authored value
                foreach (Row row in assemblyNameTable.Rows)
                {
                    if ((string)row[0] == fileRow.Component && (string)row[1] == name)
                    {
                        assemblyNameRow = row;
                        break;
                    }
                }

                // if the assembly will be GAC'd and the name in the file table doesn't match the name in the MsiAssemblyName table, error because the install will fail.
                if ("name" == name && FileAssemblyType.DotNetAssembly == fileRow.AssemblyType && String.IsNullOrEmpty(fileRow.AssemblyApplication) && !String.Equals(Path.GetFileNameWithoutExtension(fileRow.LongFileName), value, StringComparison.OrdinalIgnoreCase))
                {
                    Messaging.Instance.OnMessage(WixErrors.GACAssemblyIdentityWarning(fileRow.SourceLineNumbers, Path.GetFileNameWithoutExtension(fileRow.LongFileName), value));
                }

                if (null == assemblyNameRow)
                {
                    assemblyNameRow = assemblyNameTable.CreateRow(fileRow.SourceLineNumbers);
                    assemblyNameRow[0] = fileRow.Component;
                    assemblyNameRow[1] = name;
                    assemblyNameRow[2] = value;

                    // put the MsiAssemblyName row in the same section as the related File row
                    assemblyNameRow.SectionId = fileRow.SectionId;

                    if (null == fileRow.AssemblyNameRows)
                    {
                        fileRow.AssemblyNameRows = new List<Row>();
                    }
                    fileRow.AssemblyNameRows.Add(assemblyNameRow);
                }
                else
                {
                    assemblyNameRow[2] = value;
                }

                if (infoCache != null)
                {
                    string key = String.Format(CultureInfo.InvariantCulture, "assembly{0}.{1}", name, this.Demodularize(output, modularizationGuid, fileRow.File)).ToLowerInvariant();
                    infoCache[key] = (string)assemblyNameRow[2];
                }
            }
        }


        /// <summary>
        /// Process uncompressed files.
        /// </summary>
        /// <param name="tempDatabaseFile">The temporary database file.</param>
        /// <param name="fileRows">The collection of files to copy into the image.</param>
        /// <param name="fileTransfers">Array of files to be transfered.</param>
        /// <param name="mediaRows">The indexed media rows.</param>
        /// <param name="layoutDirectory">The directory in which the image should be layed out.</param>
        /// <param name="compressed">Flag if source image should be compressed.</param>
        /// <param name="longNamesInImage">Flag if long names should be used.</param>
        private void ProcessUncompressedFiles(string tempDatabaseFile, IEnumerable<FileRow> fileRows, List<FileTransfer> fileTransfers, RowDictionary<MediaRow> mediaRows, string layoutDirectory, bool compressed, bool longNamesInImage)
        {
            if (Messaging.Instance.EncounteredError || !fileRows.Any())
            {
                return;
            }

            Hashtable directories = new Hashtable();
            using (Database db = new Database(tempDatabaseFile, OpenDatabase.ReadOnly))
            {
                using (View directoryView = db.OpenExecuteView("SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`"))
                {
                    while (true)
                    {
                        using (Record directoryRecord = directoryView.Fetch())
                        {
                            if (null == directoryRecord)
                            {
                                break;
                            }

                            string sourceName = Installer.GetName(directoryRecord.GetString(3), true, longNamesInImage);

                            directories.Add(directoryRecord.GetString(1), new ResolvedDirectory(directoryRecord.GetString(2), sourceName));
                        }
                    }
                }

                using (View fileView = db.OpenView("SELECT `Directory_`, `FileName` FROM `Component`, `File` WHERE `Component`.`Component`=`File`.`Component_` AND `File`.`File`=?"))
                {
                    using (Record fileQueryRecord = new Record(1))
                    {
                        // for each file in the array of uncompressed files
                        foreach (FileRow fileRow in fileRows)
                        {
                            string relativeFileLayoutPath = null;

                            string mediaLayoutDirectory = this.ResolveMedia(mediaRows.Get(fileRow.DiskId), layoutDirectory);

                            // setup up the query record and find the appropriate file in the
                            // previously executed file view
                            fileQueryRecord[1] = fileRow.File;
                            fileView.Execute(fileQueryRecord);

                            using (Record fileRecord = fileView.Fetch())
                            {
                                if (null == fileRecord)
                                {
                                    throw new WixException(WixErrors.FileIdentifierNotFound(fileRow.SourceLineNumbers, fileRow.File));
                                }

                                relativeFileLayoutPath = Binder.GetFileSourcePath(directories, fileRecord[1], fileRecord[2], compressed, longNamesInImage);
                            }

                            // finally put together the base media layout path and the relative file layout path
                            string fileLayoutPath = Path.Combine(mediaLayoutDirectory, relativeFileLayoutPath);
                            FileTransfer transfer;
                            if (FileTransfer.TryCreate(fileRow.Source, fileLayoutPath, false, "File", fileRow.SourceLineNumbers, out transfer))
                            {
                                fileTransfers.Add(transfer);
                            }
                        }
                    }
                }
            }
        }

        private string ResolveMedia(MediaRow mediaRow, string layoutDirectory)
        {
            string layout = null;
            foreach (IBinderFileManager fileManager in this.FileManagers)
            {
                layout = fileManager.ResolveMedia(mediaRow, layoutDirectory);
                if (!String.IsNullOrEmpty(layout))
                {
                    break;
                }
            }

            return layout;
        }

        /// <summary>
        /// Creates the MSI/MSM/PCP database.
        /// </summary>
        /// <param name="output">Output to create database for.</param>
        /// <param name="databaseFile">The database file to create.</param>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        /// <param name="useSubdirectory">Whether to use a subdirectory based on the <paramref name="databaseFile"/> file name for intermediate files.</param>
        private void GenerateDatabase(Output output, string databaseFile, bool keepAddedColumns, bool useSubdirectory)
        {
            GenerateDatabaseCommand command = new GenerateDatabaseCommand();
            command.Extensions = this.Extensions;
            command.FileManagers = this.FileManagers;
            command.Output = output;
            command.OutputPath = databaseFile;
            command.KeepAddedColumns = keepAddedColumns;
            command.UseSubDirectory = useSubdirectory;
            command.SuppressAddingValidationRows = this.SuppressAddingValidationRows;
            command.TableDefinitions = this.TableDefinitions;
            command.TempFilesLocation = this.TempFilesLocation;
            command.Codepage = this.Codepage;
            command.Execute();
        }
    }
}
