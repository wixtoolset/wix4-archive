//-------------------------------------------------------------------------------------------------
// <copyright file="Binder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Binder core of the WiX toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using WixToolset.Bind;
    using WixToolset.Cab;
    using WixToolset.CLR.Interop;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;
    using WixToolset.MergeMod;
    using WixToolset.Msi;
    using WixToolset.Msi.Interop;

    // TODO: (4.0) Refactor so that these don't need to be copied.
    // Copied verbatim from ext\UtilExtension\wixext\UtilCompiler.cs
    [Flags]
    internal enum WixFileSearchAttributes
    {
        Default = 0x001,
        MinVersionInclusive = 0x002,
        MaxVersionInclusive = 0x004,
        MinSizeInclusive = 0x008,
        MaxSizeInclusive = 0x010,
        MinDateInclusive = 0x020,
        MaxDateInclusive = 0x040,
        WantVersion = 0x080,
        WantExists = 0x100,
        IsDirectory = 0x200,
    }

    [Flags]
    internal enum WixRegistrySearchAttributes
    {
        Raw = 0x01,
        Compatible = 0x02,
        ExpandEnvironmentVariables = 0x04,
        WantValue = 0x08,
        WantExists = 0x10,
        Win64 = 0x20,
    }

    internal enum WixComponentSearchAttributes
    {
        KeyPath = 0x1,
        State = 0x2,
        WantDirectory = 0x4,
    }

    [Flags]
    internal enum WixProductSearchAttributes
    {
        Version = 0x1,
        Language = 0x2,
        State = 0x4,
        Assignment = 0x8,
        UpgradeCode = 0x10,
    }

    /// <summary>
    /// Binder core of the WiX toolset.
    /// </summary>
    public sealed class Binder
    {
        // as outlined in RFC 4122, this is our namespace for generating name-based (version 3) UUIDs
        private static readonly Guid WixComponentGuidNamespace = new Guid("{3064E5C6-FB63-4FE9-AC49-E446A792EFA5}");

        // The following constants must stay in sync with src\burn\engine\core.h
        private const string BURN_BUNDLE_NAME = "WixBundleName";
        private const string BURN_BUNDLE_ORIGINAL_SOURCE = "WixBundleOriginalSource";
        private const string BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER = "WixBundleOriginalSourceFolder";
        private const string BURN_BUNDLE_LAST_USED_SOURCE = "WixBundleLastUsedSource";

        private string emptyFile;

        private BinderCore core;
        private BinderFileManagerCore fileManagerCore;
        private List<IBinderExtension> extensions;
        private List<IBinderFileManager> fileManagers;
        private List<InspectorExtension> inspectorExtensions;

        private Validator validator;

        // Following object handles are needed by the NewCabNamesCallBack callback
        private List<FileTransfer> fileTransfers; // File Transfers for BindDatabase Only and not the one for BindBundle
        private Output output;

        /// <summary>
        /// Creates an MSI binder.
        /// </summary>
        public Binder()
        {
            this.DefaultCompressionLevel = CompressionLevel.Mszip;

            this.BindPaths = new List<BindPath>();
            this.TargetBindPaths = new List<BindPath>();
            this.UpdatedBindPaths = new List<BindPath>();

            this.extensions = new List<IBinderExtension>();
            this.fileManagers = new List<IBinderFileManager>();
            this.inspectorExtensions = new List<InspectorExtension>();

            this.Ices = new List<string>();
            this.SuppressIces = new List<string>();

            this.validator = new Validator();

            // Need fileTransfers handle for NewCabNamesCallBack callback
            this.fileTransfers = new List<FileTransfer>();
        }

        public string ContentsFile { get; set; }
        public string OutputsFile { get; set; }
        public string BuiltOutputsFile { get; set; }
        public string WixprojectFile { get; set; }

        /// <summary>
        /// Gets the list of bindpaths.
        /// </summary>
        public List<BindPath> BindPaths { get; set; }

        /// <summary>
        /// Gets the list of target bindpaths.
        /// </summary>
        public List<BindPath> TargetBindPaths { get; set; }

        /// <summary>
        /// Gets the list of updated bindpaths.
        /// </summary>
        public List<BindPath> UpdatedBindPaths { get; set; }

        /// <summary>
        /// Gets or sets the cabinet cache location.
        /// </summary>
        public string CabCachePath { get; set; }

        /// <summary>
        /// Gets or sets whether to attempt to use cabinets from the cabinet cache location.
        /// </summary>
        public bool ReuseCabinets { get; set; }

        /// <summary>
        /// Gets or sets the number of threads to use for cabinet creation.
        /// </summary>
        /// <value>The number of threads to use for cabinet creation.</value>
        public int CabbingThreadCount { get; set; }

        /// <summary>
        /// Gets or sets the default compression level to use for cabinets
        /// that don't have their compression level explicitly set.
        /// </summary>
        public CompressionLevel DefaultCompressionLevel { get; set; }

        /// <summary>
        /// Gets or sets the exact assembly versions flag (see docs).
        /// </summary>
        public bool ExactAssemblyVersions { get; set; }

        /// <summary>
        /// Gets and sets the location to save the WixPdb.
        /// </summary>
        /// <value>The location in which to save the WixPdb. Null if the the WixPdb should not be output.</value>
        public string PdbFile { get; set; }

        public List<string> Ices { get; set; }

        public List<string> SuppressIces { get; set; }

        /// <summary>
        /// Gets and sets the option to suppress resetting ACLs by the binder.
        /// </summary>
        /// <value>The option to suppress resetting ACLs by the binder.</value>
        public bool SuppressAclReset { get; set; }

        /// <summary>
        /// Gets and sets the option to suppress creating an image for MSI/MSM.
        /// </summary>
        /// <value>The option to suppress creating an image for MSI/MSM.</value>
        public bool SuppressLayout { get; set; }

        /// <summary>
        /// Gets and sets the option to suppress MSI/MSM validation.
        /// </summary>
        /// <value>The option to suppress MSI/MSM validation.</value>
        /// <remarks>This must be set before calling Bind.</remarks>
        public bool SuppressValidation { get; set; }

        /// <summary>
        /// Gets and sets the option to suppress adding _Validation table rows.
        /// </summary>
        public bool SuppressAddingValidationRows { get; set; }

        /// <summary>
        /// Gets or sets the localizer.
        /// </summary>
        /// <value>The localizer.</value>
        public Localizer Localizer { get; set; }

        /// <summary>
        /// Gets or sets the temporary path for the Binder.  If left null, the binder
        /// will use %TEMP% environment variable.
        /// </summary>
        /// <value>Path to temp files.</value>
        public string TempFilesLocation
        {
            get;
            //{
            //    // if we don't have the temporary files object yet, get one
            //    if (null == this.tempFiles)
            //    {
            //        this.tempFiles = new TempFileCollection();

            //        // ensure the base path exists
            //        Directory.CreateDirectory(this.tempFiles.BasePath);
            //        this.fileManager.TempFilesLocation = this.tempFiles.BasePath;
            //    }

            //    return this.tempFiles.BasePath;
            //}

            set;
            //{
            //    this.DeleteTempFiles();

            //    if (null == value)
            //    {
            //        this.tempFiles = new TempFileCollection();
            //    }
            //    else
            //    {
            //        this.tempFiles = new TempFileCollection(value);
            //    }

            //    // ensure the base path exists
            //    Directory.CreateDirectory(this.tempFiles.BasePath);
            //    this.fileManager.TempFilesLocation = this.tempFiles.BasePath;
            //}
        }

        /// <summary>
        /// Gets or sets the Wix variable resolver.
        /// </summary>
        /// <value>The Wix variable resolver.</value>
        public WixVariableResolver WixVariableResolver { get; set; }

        /// <summary>
        /// Add a binder extension.
        /// </summary>
        /// <param name="extension">New extension.</param>
        public void AddExtension(IBinderExtension extension)
        {
            this.extensions.Add(extension);
        }

        /// <summary>
        /// Add a file manager extension.
        /// </summary>
        /// <param name="extension">New file manager.</param>
        public void AddExtension(IBinderFileManager extension)
        {
            this.fileManagers.Add(extension);
        }

        /// <summary>
        /// Binds an output.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="file">The Windows Installer file to create.</param>
        /// <remarks>The Binder.DeleteTempFiles method should be called after calling this method.</remarks>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        public bool Bind(Output output, string file)
        {
            // Need output object handle for NewCabNamesCallBack callback
            this.output = output;

            // Ensure the cabinet cache path exists if we are going to use it.
            if (!String.IsNullOrEmpty(this.CabCachePath))
            {
                Directory.CreateDirectory(this.CabCachePath);
            }

            // tell the binder about the validator if validation isn't suppressed
            if (!this.SuppressValidation && (OutputType.Module == output.Type || OutputType.Product == output.Type))
            {
                if (String.IsNullOrEmpty(this.validator.TempFilesLocation))
                {
                    this.validator.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");
                }

                // set the default cube file
                string lightDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string cubePath = (OutputType.Module == output.Type) ? Path.Combine(lightDirectory, "mergemod.cub") : Path.Combine(lightDirectory, "darice.cub");
                this.validator.AddCubeFile(cubePath);

                // by default, disable ICEs that have equivalent-or-better checks in WiX
                this.SuppressIces.Add("ICE08");
                this.SuppressIces.Add("ICE33");
                this.SuppressIces.Add("ICE47");
                this.SuppressIces.Add("ICE66");

                // set the ICEs
                string[] iceArray = new string[this.Ices.Count];
                this.Ices.CopyTo(iceArray, 0);
                this.validator.ICEs = iceArray;

                // set the suppressed ICEs
                string[] suppressICEArray = this.SuppressIces.ToArray();
                this.validator.SuppressedICEs = suppressICEArray;
            }
            else
            {
                this.validator = null;
            }

            this.fileManagerCore = new BinderFileManagerCore();
            this.fileManagerCore.CabCachePath = this.CabCachePath;
            this.fileManagerCore.Output = output;
            this.fileManagerCore.TempFilesLocation = this.TempFilesLocation;
            this.fileManagerCore.AddBindPaths(this.BindPaths, BindStage.Normal);
            this.fileManagerCore.AddBindPaths(this.TargetBindPaths, BindStage.Target);
            this.fileManagerCore.AddBindPaths(this.UpdatedBindPaths, BindStage.Updated);
            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                fileManager.Core = this.fileManagerCore;
            }

            this.core = new BinderCore();
            this.core.FileManagerCore = this.fileManagerCore;

            foreach (IBinderExtension extension in this.extensions)
            {
                extension.Core = this.core;
            }

            switch (output.Type)
            {
                case OutputType.Bundle:
                    this.BindBundle(output, file);
                    break;

                case OutputType.Transform:
                    this.BindTransform(output, file);
                    break;

                default:
                    this.BindDatabase(output, file);
                    break;
            }

            this.core = null;

            return Messaging.Instance.EncounteredError;
        }

        /// <summary>
        /// Does any housekeeping after Bind.
        /// </summary>
        /// <param name="tidy">Whether or not any actual tidying should be done.</param>
        public void Cleanup(bool tidy)
        {
            // If Bind hasn't been called yet, core will be null. There will be
            // nothing to cleanup.
            if (this.core == null)
            {
                return;
            }

            if (tidy)
            {
                if (!this.DeleteTempFiles())
                {
                    this.core.OnMessage(WixWarnings.FailedToDeleteTempDir(this.TempFilesLocation));
                }
            }
            else
            {
                this.core.OnMessage(WixVerboses.BinderTempDirLocatedAt(this.TempFilesLocation));
            }

            if (null != this.validator && !String.IsNullOrEmpty(this.validator.TempFilesLocation))
            {
                if (tidy)
                {
                    if (!this.validator.DeleteTempFiles())
                    {
                        this.core.OnMessage(WixWarnings.FailedToDeleteTempDir(this.validator.TempFilesLocation));
                    }
                }
                else
                {
                    this.core.OnMessage(WixVerboses.ValidatorTempDirLocatedAt(this.validator.TempFilesLocation));
                }
            }
        }

        /// <summary>
        /// Cleans up the temp files used by the Binder.
        /// </summary>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        public bool DeleteTempFiles()
        {
            bool deleted = Common.DeleteTempFiles(this.TempFilesLocation, this.core);
            if (deleted)
            {
                this.emptyFile = null;
            }

            return deleted;
        }

        /// <summary>
        /// Creates the MSI/MSM/PCP database.
        /// </summary>
        /// <param name="output">Output to create database for.</param>
        /// <param name="databaseFile">The database file to create.</param>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        /// <param name="useSubdirectory">Whether to use a subdirectory based on the <paramref name="databaseFile"/> file name for intermediate files.</param>
        internal void GenerateDatabase(Output output, string databaseFile, bool keepAddedColumns, bool useSubdirectory)
        {
            // add the _Validation rows
            if (!this.SuppressAddingValidationRows)
            {
                Table validationTable = output.EnsureTable(this.core.TableDefinitions["_Validation"]);

                foreach (Table table in output.Tables)
                {
                    if (!table.Definition.Unreal)
                    {
                        // add the validation rows for this table
                        table.Definition.AddValidationRows(validationTable);
                    }
                }
            }

            // set the base directory
            string baseDirectory = this.TempFilesLocation;
            if (useSubdirectory)
            {
                string filename = Path.GetFileNameWithoutExtension(databaseFile);
                baseDirectory = Path.Combine(baseDirectory, filename);

                // make sure the directory exists
                Directory.CreateDirectory(baseDirectory);
            }

            try
            {
                OpenDatabase type = OpenDatabase.CreateDirect;

                // set special flag for patch files
                if (OutputType.Patch == output.Type)
                {
                    type |= OpenDatabase.OpenPatchFile;
                }

                // try to create the database
                using (Database db = new Database(databaseFile, type))
                {
                    // localize the codepage if a value was specified by the localizer
                    if (null != this.Localizer && -1 != this.Localizer.Codepage)
                    {
                        output.Codepage = this.Localizer.Codepage;
                    }

                    // if we're not using the default codepage, import a new one into our
                    // database before we add any tables (or the tables would be added
                    // with the wrong codepage)
                    if (0 != output.Codepage)
                    {
                        this.SetDatabaseCodepage(db, output);
                    }

                    foreach (Table table in output.Tables)
                    {
                        Table importTable = table;
                        bool hasBinaryColumn = false;

                        // skip all unreal tables other than _Streams
                        if (table.Definition.Unreal && "_Streams" != table.Name)
                        {
                            continue;
                        }

                        // Do not put the _Validation table in patches, it is not needed
                        if (OutputType.Patch == output.Type && "_Validation" == table.Name)
                        {
                            continue;
                        }

                        // The only way to import binary data is to copy it to a local subdirectory first.
                        // To avoid this extra copying and perf hit, import an empty table with the same
                        // definition and later import the binary data from source using records.
                        foreach (ColumnDefinition columnDefinition in table.Definition.Columns)
                        {
                            if (ColumnType.Object == columnDefinition.Type)
                            {
                                importTable = new Table(table.Section, table.Definition);
                                hasBinaryColumn = true;
                                break;
                            }
                        }

                        // create the table via IDT import
                        if ("_Streams" != importTable.Name)
                        {
                            try
                            {
                                db.ImportTable(output.Codepage, importTable, baseDirectory, keepAddedColumns);
                            }
                            catch (WixInvalidIdtException)
                            {
                                // If ValidateRows finds anything it doesn't like, it throws
                                importTable.ValidateRows();

                                // Otherwise we rethrow the InvalidIdt
                                throw;
                            }
                        }

                        // insert the rows via SQL query if this table contains object fields
                        if (hasBinaryColumn)
                        {
                            StringBuilder query = new StringBuilder("SELECT ");

                            // build the query for the view
                            bool firstColumn = true;
                            foreach (ColumnDefinition columnDefinition in table.Definition.Columns)
                            {
                                if (!firstColumn)
                                {
                                    query.Append(",");
                                }
                                query.AppendFormat(" `{0}`", columnDefinition.Name);
                                firstColumn = false;
                            }
                            query.AppendFormat(" FROM `{0}`", table.Name);

                            using (View tableView = db.OpenExecuteView(query.ToString()))
                            {
                                // import each row containing a stream
                                foreach (Row row in table.Rows)
                                {
                                    using (Record record = new Record(table.Definition.Columns.Count))
                                    {
                                        StringBuilder streamName = new StringBuilder();
                                        bool needStream = false;

                                        // the _Streams table doesn't prepend the table name (or a period)
                                        if ("_Streams" != table.Name)
                                        {
                                            streamName.Append(table.Name);
                                        }

                                        for (int i = 0; i < table.Definition.Columns.Count; i++)
                                        {
                                            ColumnDefinition columnDefinition = table.Definition.Columns[i];

                                            switch (columnDefinition.Type)
                                            {
                                                case ColumnType.Localized:
                                                case ColumnType.Preserved:
                                                case ColumnType.String:
                                                    if (columnDefinition.PrimaryKey)
                                                    {
                                                        if (0 < streamName.Length)
                                                        {
                                                            streamName.Append(".");
                                                        }
                                                        streamName.Append((string)row[i]);
                                                    }

                                                    record.SetString(i + 1, (string)row[i]);
                                                    break;
                                                case ColumnType.Number:
                                                    record.SetInteger(i + 1, Convert.ToInt32(row[i], CultureInfo.InvariantCulture));
                                                    break;
                                                case ColumnType.Object:
                                                    if (null != row[i])
                                                    {
                                                        needStream = true;
                                                        try
                                                        {
                                                            record.SetStream(i + 1, (string)row[i]);
                                                        }
                                                        catch (Win32Exception e)
                                                        {
                                                            if (0xA1 == e.NativeErrorCode) // ERROR_BAD_PATHNAME
                                                            {
                                                                throw new WixException(WixErrors.FileNotFound(row.SourceLineNumbers, (string)row[i]));
                                                            }
                                                            else
                                                            {
                                                                throw new WixException(WixErrors.Win32Exception(e.NativeErrorCode, e.Message));
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }

                                        // stream names are created by concatenating the name of the table with the values
                                        // of the primary key (delimited by periods)
                                        // check for a stream name that is more than 62 characters long (the maximum allowed length)
                                        if (needStream && MsiInterop.MsiMaxStreamNameLength < streamName.Length)
                                        {
                                            this.core.OnMessage(WixErrors.StreamNameTooLong(row.SourceLineNumbers, table.Name, streamName.ToString(), streamName.Length));
                                        }
                                        else // add the row to the database
                                        {
                                            tableView.Modify(ModifyView.Assign, record);
                                        }
                                    }
                                }
                            }

                            // Remove rows from the _Streams table for wixpdbs.
                            if ("_Streams" == table.Name)
                            {
                                table.Rows.Clear();
                            }
                        }
                    }

                    // insert substorages (like transforms inside a patch)
                    if (0 < output.SubStorages.Count)
                    {
                        using (View storagesView = new View(db, "SELECT `Name`, `Data` FROM `_Storages`"))
                        {
                            foreach (SubStorage subStorage in output.SubStorages)
                            {
                                string transformFile = Path.Combine(this.TempFilesLocation, String.Concat(subStorage.Name, ".mst"));

                                // Bind the transform.
                                this.BindTransform(subStorage.Data, transformFile);
                                if (!Messaging.Instance.EncounteredError)
                                {
                                    // add the storage
                                    using (Record record = new Record(2))
                                    {
                                        record.SetString(1, subStorage.Name);
                                        record.SetStream(2, transformFile);
                                        storagesView.Modify(ModifyView.Assign, record);
                                    }
                                }
                            }
                        }
                    }

                    // we're good, commit the changes to the new MSI
                    db.Commit();
                }
            }
            catch (IOException)
            {
                // TODO: this error message doesn't seem specific enough
                throw new WixFileNotFoundException(new SourceLineNumber(databaseFile), databaseFile);
            }
        }

        /// <summary>
        /// Final step in binding that transfers (moves/copies) all files generated into the appropriate
        /// location in the source image
        /// </summary>
        /// <param name="fileTransfers">List of files to transfer.</param>
        private void LayoutMedia(List<FileTransfer> fileTransfers)
        {
            if (this.core.EncounteredError)
            {
                return;
            }

            List<string> destinationFiles = new List<string>();

            for (int i = 0; i < fileTransfers.Count; ++i)
            {
                FileTransfer fileTransfer = fileTransfers[i];
                string fileSource = this.ResolveFile(fileTransfer.Source, fileTransfer.Type, fileTransfer.SourceLineNumbers, BindStage.Normal);

                // If the source and destination are identical, then there's nothing to do here
                if (0 == String.Compare(fileSource, fileTransfer.Destination, StringComparison.OrdinalIgnoreCase))
                {
                    fileTransfer.Redundant = true;
                    continue;
                }

                bool retry = false;
                do
                {
                    try
                    {
                        if (fileTransfer.Move)
                        {
                            this.core.OnMessage(WixVerboses.MoveFile(fileSource, fileTransfer.Destination));
                            this.TransferFile(true, fileSource, fileTransfer.Destination);
                        }
                        else
                        {
                            this.core.OnMessage(WixVerboses.CopyFile(fileSource, fileTransfer.Destination));
                            this.TransferFile(false, fileSource, fileTransfer.Destination);
                        }

                        retry = false;
                        destinationFiles.Add(fileTransfer.Destination);
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new WixFileNotFoundException(e.FileName);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        string directory = Path.GetDirectoryName(fileTransfer.Destination);
                        this.core.OnMessage(WixVerboses.CreateDirectory(directory));
                        Directory.CreateDirectory(directory);
                        retry = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        if (File.Exists(fileTransfer.Destination))
                        {
                            this.core.OnMessage(WixVerboses.RemoveDestinationFile(fileTransfer.Destination));

                            // try to ensure the file is not read-only
                            FileAttributes attributes = File.GetAttributes(fileTransfer.Destination);
                            try
                            {
                                File.SetAttributes(fileTransfer.Destination, attributes & ~FileAttributes.ReadOnly);
                            }
                            catch (ArgumentException) // thrown for unauthorized access errors
                            {
                                throw new WixException(WixErrors.UnauthorizedAccess(fileTransfer.Destination));
                            }

                            // try to delete the file
                            try
                            {
                                File.Delete(fileTransfer.Destination);
                            }
                            catch (IOException)
                            {
                                throw new WixException(WixErrors.FileInUse(null, fileTransfer.Destination));
                            }

                            retry = true;
                        }
                        else // no idea what just happened, bail
                        {
                            throw;
                        }
                    }
                    catch (IOException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        if (File.Exists(fileTransfer.Destination))
                        {
                            this.core.OnMessage(WixVerboses.RemoveDestinationFile(fileTransfer.Destination));

                            // ensure the file is not read-only, then delete it
                            FileAttributes attributes = File.GetAttributes(fileTransfer.Destination);
                            File.SetAttributes(fileTransfer.Destination, attributes & ~FileAttributes.ReadOnly);
                            try
                            {
                                File.Delete(fileTransfer.Destination);
                            }
                            catch (IOException)
                            {
                                throw new WixException(WixErrors.FileInUse(null, fileTransfer.Destination));
                            }

                            retry = true;
                        }
                        else // no idea what just happened, bail
                        {
                            throw;
                        }
                    }
                } while (retry);
            }

            // Finally, if there were any files remove the ACL that may have been added to
            // during the file transfer process.
            if (0 < destinationFiles.Count && !this.SuppressAclReset)
            {
                try
                {
                    WixToolset.Cab.Interop.NativeMethods.ResetAcls(destinationFiles.ToArray(), (uint)destinationFiles.Count);
                }
                catch
                {
                    this.core.OnMessage(WixWarnings.UnableToResetAcls());
                }
            }
        }

        /// <summary>
        /// Get the source path of a directory.
        /// </summary>
        /// <param name="directories">All cached directories.</param>
        /// <param name="componentIdGenSeeds">Hash table of Component GUID generation seeds indexed by directory id.</param>
        /// <param name="directory">Directory identifier.</param>
        /// <param name="canonicalize">Canonicalize the path for standard directories.</param>
        /// <returns>Source path of a directory.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way autogenerated GUIDs are generated. Furthermore, there is no security hole here, as the strings won't need to " +
                         "make a round trip")]
        private static string GetDirectoryPath(Hashtable directories, Hashtable componentIdGenSeeds, string directory, bool canonicalize)
        {
            if (!directories.Contains(directory))
            {
                throw new WixException(WixErrors.ExpectedDirectory(directory));
            }
            ResolvedDirectory resolvedDirectory = (ResolvedDirectory)directories[directory];

            if (null == resolvedDirectory.Path)
            {
                if (null != componentIdGenSeeds && componentIdGenSeeds.Contains(directory))
                {
                    resolvedDirectory.Path = (string)componentIdGenSeeds[directory];
                }
                else if (canonicalize && WindowsInstallerStandard.IsStandardDirectory(directory))
                {
                    // when canonicalization is on, standard directories are treated equally
                    resolvedDirectory.Path = directory;
                }
                else
                {
                    string name = resolvedDirectory.Name;

                    if (canonicalize && null != name)
                    {
                        name = name.ToLower(CultureInfo.InvariantCulture);
                    }

                    if (String.IsNullOrEmpty(resolvedDirectory.DirectoryParent))
                    {
                        resolvedDirectory.Path = name;
                    }
                    else
                    {
                        string parentPath = GetDirectoryPath(directories, componentIdGenSeeds, resolvedDirectory.DirectoryParent, canonicalize);

                        if (null != resolvedDirectory.Name)
                        {
                            resolvedDirectory.Path = Path.Combine(parentPath, name);
                        }
                        else
                        {
                            resolvedDirectory.Path = parentPath;
                        }
                    }
                }
            }

            return resolvedDirectory.Path;
        }

        /// <summary>
        /// Gets the source path of a file.
        /// </summary>
        /// <param name="directories">All cached directories in <see cref="ResolvedDirectory"/>.</param>
        /// <param name="directoryId">Parent directory identifier.</param>
        /// <param name="fileName">File name (in long|source format).</param>
        /// <param name="compressed">Specifies the package is compressed.</param>
        /// <param name="useLongName">Specifies the package uses long file names.</param>
        /// <returns>Source path of file relative to package directory.</returns>
        internal static string GetFileSourcePath(Hashtable directories, string directoryId, string fileName, bool compressed, bool useLongName)
        {
            string fileSourcePath = Installer.GetName(fileName, true, useLongName);

            if (compressed)
            {
                // Use just the file name of the file since all uncompressed files must appear
                // in the root of the image in a compressed package.
            }
            else
            {
                // Get the relative path of where we want the file to be layed out as specified
                // in the Directory table.
                string directoryPath = Binder.GetDirectoryPath(directories, null, directoryId, false);
                fileSourcePath = Path.Combine(directoryPath, fileSourcePath);
            }

            // Strip off "SourceDir" if it's still on there.
            if (fileSourcePath.StartsWith("SourceDir\\", StringComparison.Ordinal))
            {
                fileSourcePath = fileSourcePath.Substring(10);
            }

            return fileSourcePath;
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
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "This string is not round tripped, and not used for any security decisions")]
        private void SetMsiAssemblyName(Output output, Table assemblyNameTable, FileRow fileRow, string name, string value, IDictionary<string, string> infoCache, string modularizationGuid)
        {
            // check for null value (this can occur when grabbing the file version from an assembly without one)
            if (String.IsNullOrEmpty(value))
            {
                this.core.OnMessage(WixWarnings.NullMsiAssemblyNameValue(fileRow.SourceLineNumbers, fileRow.Component, name));
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
                    this.core.OnMessage(WixErrors.GACAssemblyIdentityWarning(fileRow.SourceLineNumbers, Path.GetFileNameWithoutExtension(fileRow.LongFileName), value));
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
                    string key = String.Format(CultureInfo.InvariantCulture, "assembly{0}.{1}", name, Demodularize(output, modularizationGuid, fileRow.File)).ToLowerInvariant();
                    infoCache[key] = (string)assemblyNameRow[2];
                }
            }
        }

        /// <summary>
        /// Merge data from a row in the WixPatchSymbolsPaths table into an associated FileRow.
        /// </summary>
        /// <param name="row">Row from the WixPatchSymbolsPaths table.</param>
        /// <param name="fileRow">FileRow into which to set symbol information.</param>
        /// <comment>This includes PreviousData as well.</comment>
        private static void MergeSymbolPaths(Row row, FileRow fileRow)
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
        /// Merge data from the unreal tables into the real tables.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void MergeUnrealTables(TableIndexedCollection tables)
        {
            // Merge data from the WixBBControl rows into the BBControl rows.
            Table wixBBControlTable = tables["WixBBControl"];
            if (null != wixBBControlTable)
            {
                RowDictionary<BBControlRow> indexedBBControlRows = new RowDictionary<BBControlRow>(tables["BBControl"]);
                foreach (Row row in wixBBControlTable.Rows)
                {
                    BBControlRow bbControlRow = indexedBBControlRows.Get(row.GetPrimaryKey());
                    bbControlRow.SourceFile = (string)row[2];
                }
            }

            // Merge data from the WixControl rows into the Control rows.
            Table wixControlTable = tables["WixControl"];
            if (null != wixControlTable)
            {
                RowDictionary<ControlRow> indexedControlRows = new RowDictionary<ControlRow>(tables["Control"]);
                foreach (Row row in wixControlTable.Rows)
                {
                    ControlRow controlRow = indexedControlRows.Get(row.GetPrimaryKey());
                    controlRow.SourceFile = (string)row[2];
                }
            }

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
                        this.core.OnMessage(WixErrors.DuplicateFileId(fileRow.File));
                    }
                }

                if (this.core.EncounteredError)
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
                            MergeSymbolPaths(row, (FileRow)fileRowsByFile[row[1]]);
                            break;

                        case "Product":
                            foreach (FileRow fileRow in fileRowsByFile)
                            {
                                MergeSymbolPaths(row, fileRow);
                            }
                            break;

                        case "Component":
                            ArrayList fileRowsByThisComponent = (ArrayList)(fileRowsByComponent[row[1]]);
                            if (null != fileRowsByThisComponent)
                            {
                                foreach (FileRow fileRow in fileRowsByThisComponent)
                                {
                                    MergeSymbolPaths(row, fileRow);
                                }
                            }

                            break;

                        case "Directory":
                            ArrayList fileRowsByThisDirectory = (ArrayList)(fileRowsByDirectory[row[1]]);
                            if (null != fileRowsByThisDirectory)
                            {
                                foreach (FileRow fileRow in fileRowsByThisDirectory)
                                {
                                    MergeSymbolPaths(row, fileRow);
                                }
                            }
                            break;

                        case "Media":
                            ArrayList fileRowsByThisDiskId = (ArrayList)(fileRowsByDiskId[row[1]]);
                            if (null != fileRowsByThisDiskId)
                            {
                                foreach (FileRow fileRow in fileRowsByThisDiskId)
                                {
                                    MergeSymbolPaths(row, fileRow);
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
        /// Signal a warning if a non-keypath file was changed in a patch without also changing the keypath file of the component.
        /// </summary>
        /// <param name="output">The output to validate.</param>
        private void ValidateFileRowChanges(Output transform)
        {
            Table componentTable = transform.Tables["Component"];
            Table fileTable = transform.Tables["File"];

            // There's no sense validating keypaths if the transform has no component or file table
            if (componentTable == null || fileTable == null)
            {
                return;
            }

            Dictionary<string, string> componentKeyPath = new Dictionary<string, string>(componentTable.Rows.Count);

            // Index the Component table for non-directory & non-registry key paths.
            foreach (Row row in componentTable.Rows)
            {
                if (null != row.Fields[5].Data &&
                    0 != ((int)row.Fields[3].Data & MsiInterop.MsidbComponentAttributesRegistryKeyPath))
                {
                    componentKeyPath.Add(row.Fields[0].Data.ToString(), row.Fields[5].Data.ToString());
                }
            }

            Dictionary<string, string> componentWithChangedKeyPath = new Dictionary<string, string>();
            Dictionary<string, string> componentWithNonKeyPathChanged = new Dictionary<string, string>();
            // Verify changes in the file table, now that file diffing has occurred
            foreach (FileRow row in fileTable.Rows)
            {
                string fileId = row.Fields[0].Data.ToString();
                string componentId = row.Fields[1].Data.ToString();

                if (RowOperation.Modify != row.Operation)
                {
                    continue;
                }

                // If this file is the keypath of a component
                if (componentKeyPath.ContainsValue(fileId))
                {
                    if (!componentWithChangedKeyPath.ContainsKey(componentId))
                    {
                        componentWithChangedKeyPath.Add(componentId, fileId);
                    }
                }
                else
                {
                    if (!componentWithNonKeyPathChanged.ContainsKey(componentId))
                    {
                        componentWithNonKeyPathChanged.Add(componentId, fileId);
                    }
                }
            }

            foreach (KeyValuePair<string, string> componentFile in componentWithNonKeyPathChanged)
            {
                // Make sure all changes to non keypath files also had a change in the keypath.
                if (!componentWithChangedKeyPath.ContainsKey(componentFile.Key) && componentKeyPath.ContainsKey(componentFile.Key))
                {
                    this.core.OnMessage(WixWarnings.UpdateOfNonKeyPathFile((string)componentFile.Value, (string)componentFile.Key, (string)componentKeyPath[componentFile.Key]));
                }
            }
        }

        /// <summary>
        /// Binds the summary information table of a database.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="longNames">Returns a flag indicating if uncompressed files use long filenames.</param>
        /// <param name="compressed">Returns a flag indicating if files are compressed by default.</param>
        /// <returns>Modularization guid, or null if the output is not a module.</returns>
        private string BindDatabaseSummaryInfo(Output output, out bool longNames, out bool compressed)
        {
            longNames = false;
            compressed = false;
            string modularizationGuid = null;
            Table summaryInformationTable = output.Tables["_SummaryInformation"];
            if (null != summaryInformationTable)
            {
                bool foundCreateDataTime = false;
                bool foundLastSaveDataTime = false;
                bool foundCreatingApplication = false;
                string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

                foreach (Row summaryInformationRow in summaryInformationTable.Rows)
                {
                    switch ((int)summaryInformationRow[0])
                    {
                        case 1: // PID_CODEPAGE
                            // make sure the code page is an int and not a web name or null
                            string codepage = (string)summaryInformationRow[1];

                            if (null == codepage)
                            {
                                codepage = "0";
                            }
                            else
                            {
                                summaryInformationRow[1] = Common.GetValidCodePage(codepage, false, true, summaryInformationRow.SourceLineNumbers).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case 9: // PID_REVNUMBER
                            string packageCode = (string)summaryInformationRow[1];

                            if (OutputType.Module == output.Type)
                            {
                                modularizationGuid = packageCode.Substring(1, 36).Replace('-', '_');
                            }
                            else if ("*" == packageCode)
                            {
                                // set the revision number (package/patch code) if it should be automatically generated
                                summaryInformationRow[1] = Common.GenerateGuid();
                            }
                            break;
                        case 12: // PID_CREATE_DTM
                            foundCreateDataTime = true;
                            break;
                        case 13: // PID_LASTSAVE_DTM
                            foundLastSaveDataTime = true;
                            break;
                        case 15: // PID_WORDCOUNT
                            if (OutputType.Patch == output.Type)
                            {
                                longNames = true;
                                compressed = true;
                            }
                            else
                            {
                                longNames = (0 == (Convert.ToInt32(summaryInformationRow[1], CultureInfo.InvariantCulture) & 1));
                                compressed = (2 == (Convert.ToInt32(summaryInformationRow[1], CultureInfo.InvariantCulture) & 2));
                            }
                            break;
                        case 18: // PID_APPNAME
                            foundCreatingApplication = true;
                            break;
                    }
                }

                // add a summary information row for the create time/date property if its not already set
                if (!foundCreateDataTime)
                {
                    Row createTimeDateRow = summaryInformationTable.CreateRow(null);
                    createTimeDateRow[0] = 12;
                    createTimeDateRow[1] = now;
                }

                // add a summary information row for the last save time/date property if its not already set
                if (!foundLastSaveDataTime)
                {
                    Row lastSaveTimeDateRow = summaryInformationTable.CreateRow(null);
                    lastSaveTimeDateRow[0] = 13;
                    lastSaveTimeDateRow[1] = now;
                }

                // add a summary information row for the creating application property if its not already set
                if (!foundCreatingApplication)
                {
                    Row creatingApplicationRow = summaryInformationTable.CreateRow(null);
                    creatingApplicationRow[0] = 18;
                    creatingApplicationRow[1] = String.Format(CultureInfo.InvariantCulture, AppCommon.GetCreatingApplicationString());
                }
            }

            return modularizationGuid;
        }

        /// <summary>
        /// Populates the WixBuildInfo table in an output.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="databaseFile">The output file if OutputFile not set.</param>
        private void WriteBuildInfoTable(Output output, string outputFile)
        {
            Table buildInfoTable = output.EnsureTable(this.core.TableDefinitions["WixBuildInfo"]);
            Row buildInfoRow = buildInfoTable.CreateRow(null);

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(executingAssembly.Location);
            buildInfoRow[0] = fileVersion.FileVersion;
            buildInfoRow[1] = outputFile;

            if (!String.IsNullOrEmpty(this.WixprojectFile))
            {
                buildInfoRow[2] = this.WixprojectFile;
            }

            if (!String.IsNullOrEmpty(this.PdbFile))
            {
                buildInfoRow[3] = this.PdbFile;
            }
        }

        /// <summary>
        /// Binds a databse.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="databaseFile">The database file to create.</param>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "This string is not round tripped, and not used for any security decisions")]
        [SuppressMessage("Microsoft.Globalization", "CA1309:UseOrdinalStringComparison", Justification = "These strings need to be culture insensitive rather than ordinal because they are used for sorting")]
        private bool BindDatabase(Output output, string databaseFile)
        {
            foreach (BinderExtension extension in this.extensions)
            {
                extension.Initialize(output);
            }

            bool compressed = false;
            //FileRowCollection fileRows = new FileRowCollection(OutputType.Patch == output.Type);
            bool longNames = false;
            HashSet<string> suppressedTableNames = new HashSet<string>();
            Table propertyTable = output.Tables["Property"];

            this.WriteBuildInfoTable(output, databaseFile);

            // gather all the wix variables
            Table wixVariableTable = output.Tables["WixVariable"];
            if (null != wixVariableTable)
            {
                foreach (WixVariableRow wixVariableRow in wixVariableTable.Rows)
                {
                    this.WixVariableResolver.AddVariable(wixVariableRow);
                }
            }

            // localize fields, resolve wix variables, and resolve file paths
            ExtractEmbeddedFiles filesWithEmbeddedFiles = new ExtractEmbeddedFiles();
            List<DelayedField> delayedFields = new List<DelayedField>();
            this.ResolveFields(output.Tables, filesWithEmbeddedFiles, delayedFields);

            // if there are any fields to resolve later, create the cache to populate during bind
            IDictionary<string, string> variableCache = null;
            if (0 < delayedFields.Count)
            {
                variableCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }

            this.LocalizeUI(output.Tables);

            // process the summary information table before the other tables
            string modularizationGuid = this.BindDatabaseSummaryInfo(output, out longNames, out compressed);

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // modularize identifiers and add tables with real streams to the import tables
            if (OutputType.Module == output.Type)
            {
                // Gather all the suppress modularization identifiers
                HashSet<string> suppressModularizationIdentifiers = null;
                Table wixSuppressModularizationTable = output.Tables["WixSuppressModularization"];
                if (null != wixSuppressModularizationTable)
                {
                    suppressModularizationIdentifiers = new HashSet<string>(wixSuppressModularizationTable.Rows.Select(row => (string)row[0]));
                }

                foreach (Table table in output.Tables)
                {
                    table.Modularize(modularizationGuid, suppressModularizationIdentifiers);
                }
            }

            // Merge unreal table data into the real tables
            // This must occur after all variables and source paths have been resolved and after modularization.
            this.MergeUnrealTables(output.Tables);

            if (this.core.EncounteredError)
            {
                return false;
            }

            if (OutputType.Patch == output.Type)
            {
                foreach (SubStorage substorage in output.SubStorages)
                {
                    Output transform = (Output)substorage.Data;
                    this.ResolveFields(transform.Tables, filesWithEmbeddedFiles, null);
                    this.MergeUnrealTables(transform.Tables);
                }
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // add binder variables for all properties
            propertyTable = output.Tables["Property"];
            if (null != propertyTable)
            {
                foreach (PropertyRow propertyRow in propertyTable.Rows)
                {
                    // Set the ProductCode if it is to be generated.
                    if (OutputType.Product == output.Type && "ProductCode".Equals(propertyRow.Property, StringComparison.Ordinal) && "*".Equals(propertyRow.Value, StringComparison.Ordinal))
                    {
                        propertyRow.Value = Common.GenerateGuid();

                        // Update the target ProductCode in any instance transforms.
                        foreach (SubStorage subStorage in output.SubStorages)
                        {
                            Output subStorageOutput = subStorage.Data;
                            if (OutputType.Transform != subStorageOutput.Type)
                            {
                                continue;
                            }

                            Table instanceSummaryInformationTable = subStorageOutput.Tables["_SummaryInformation"];
                            foreach (Row row in instanceSummaryInformationTable.Rows)
                            {
                                if ((int)SummaryInformation.Transform.ProductCodes == (int)row[0])
                                {
                                    row[1] = ((string)row[1]).Replace("*", propertyRow.Value);
                                    break;
                                }
                            }
                        }
                    }

                    // Add the property name and value to the variableCache.
                    if (null != variableCache)
                    {
                        string key = String.Concat("property.", Demodularize(output, modularizationGuid, propertyRow.Property));
                        variableCache[key] = propertyRow.Value;
                    }
                }
            }

            // extract files that come from cabinet files (this does not extract files from merge modules)
            this.ExtractEmbeddedFiles(filesWithEmbeddedFiles);

            // Start with a collection of all files from the File table. The list of all files that need
            // to be processed may grow if we add things like Merge Modules or patches with multiple
            // transforms.
            // Collecting these files must occur AFTER the unreal data has been merged in.
            Table fileTable = output.Tables["File"];
            List<FileRow> fileRows = fileTable.Rows.Cast<FileRow>().ToList();

            if (OutputType.Product == output.Type)
            {
                // Retrieve files and their information from merge modules.
                this.ProcessMergeModules(output, fileRows);
            }
            else if (OutputType.Patch == output.Type)
            {
                // Merge transform data into the output object.
                this.CopyTransformData(output, fileRows);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // assign files to media
            AutoMediaAssigner autoMediaAssigner = new AutoMediaAssigner(output, this.core, compressed);
            autoMediaAssigner.AssignFiles(fileRows);

            // Update file sequence.
            this.core.OnMessage(WixVerboses.UpdatingFileInformation());
            this.UpdateMediaSequences(output, fileRows, autoMediaAssigner.MediaRows);

            // Gather information about files that did not come from merge modules (i.e. rows with a reference to the File table).
            foreach (FileRow row in fileRows.Where(r => null != r.Table))
            {
                this.UpdateFileRow(output, variableCache, modularizationGuid, fileRows, row, false);
            }

            // set generated component guids
            this.SetComponentGuids(output);

            // With the Component Guids set now we can create instance transforms.
            this.CreateInstanceTransforms(output);

            this.ValidateComponentGuids(output);

            this.UpdateControlText(output);

            if (0 < delayedFields.Count)
            {
                this.ResolveDelayedFields(output, delayedFields, variableCache, modularizationGuid);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // Extended binder extensions can be called now that fields are resolved.
            foreach (BinderExtension extension in this.extensions)
            {
                output.EnsureTable(this.core.TableDefinitions["WixBindUpdatedFiles"]);
                extension.AfterResolvedFields(output);
            }

            Table updatedFiles = output.Tables["WixBindUpdatedFiles"];
            if (null != updatedFiles)
            {
                foreach (Row updatedFile in updatedFiles.Rows)
                {
                    // TODO: Is this too slow? It's basically NxM. Okay'ish, if updatedFiles is small count.
                    FileRow updatedFileRow = fileRows.Single(r => r.File.Equals((string)updatedFile[0], StringComparison.Ordinal));
                    this.UpdateFileRow(output, null, modularizationGuid, fileRows, updatedFileRow, true);
                }
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // create cabinet files and process uncompressed files
            string layoutDirectory = Path.GetDirectoryName(databaseFile);
            RowDictionary<FileRow> uncompressedFileRows = null;
            if (!this.SuppressLayout || OutputType.Module == output.Type)
            {
                this.core.OnMessage(WixVerboses.CreatingCabinetFiles());

                CreateCabinetsCommand command = new CreateCabinetsCommand();
                command.CabbingThreadCount = this.CabbingThreadCount;
                command.DefaultCompressionLevel = this.DefaultCompressionLevel;
                command.Output = output;
                command.FileManagers = this.fileManagers;
                command.FileTransfers = this.fileTransfers;
                command.LayoutDirectory = layoutDirectory;
                command.Compressed = compressed;
                command.AutoMediaAssigner = autoMediaAssigner;
                command.TableDefinitions = this.core.TableDefinitions;
                command.TempFilesLocation = this.TempFilesLocation;
                command.Execute();

                uncompressedFileRows = command.UncompressedFileRows;
            }

            if (OutputType.Patch == output.Type)
            {
                // copy output data back into the transforms
                this.CopyTransformData(output, null);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // add back suppressed tables which must be present prior to merging in modules
            if (OutputType.Product == output.Type)
            {
                Table wixMergeTable = output.Tables["WixMerge"];

                if (null != wixMergeTable && 0 < wixMergeTable.Rows.Count)
                {
                    foreach (SequenceTable sequence in Enum.GetValues(typeof(SequenceTable)))
                    {
                        string sequenceTableName = sequence.ToString();
                        Table sequenceTable = output.Tables[sequenceTableName];

                        if (null == sequenceTable)
                        {
                            sequenceTable = output.EnsureTable(this.core.TableDefinitions[sequenceTableName]);
                        }

                        if (0 == sequenceTable.Rows.Count)
                        {
                            suppressedTableNames.Add(sequenceTableName);
                        }
                    }
                }
            }

            foreach (BinderExtension extension in this.extensions)
            {
                extension.Finish(output);
            }

            // generate database file
            this.core.OnMessage(WixVerboses.GeneratingDatabase());
            string tempDatabaseFile = Path.Combine(this.TempFilesLocation, Path.GetFileName(databaseFile));
            this.GenerateDatabase(output, tempDatabaseFile, false, false);

            FileTransfer transfer;
            if (FileTransfer.TryCreate(tempDatabaseFile, databaseFile, true, output.Type.ToString(), null, out transfer)) // note where this database needs to move in the future
            {
                transfer.Built = true;
                this.fileTransfers.Add(transfer);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // Output the output to a file
            Pdb pdb = new Pdb();
            pdb.Output = output;
            if (!String.IsNullOrEmpty(this.PdbFile))
            {
                pdb.Save(this.PdbFile);
            }

            // merge modules
            if (OutputType.Product == output.Type)
            {
                this.core.OnMessage(WixVerboses.MergingModules());
                this.MergeModules(tempDatabaseFile, output, fileRows, suppressedTableNames);

                // stop processing if an error previously occurred
                if (this.core.EncounteredError)
                {
                    return false;
                }
            }

            // inspect the MSI prior to running ICEs
            InspectorCore inspectorCore = new InspectorCore();
            foreach (InspectorExtension inspectorExtension in this.inspectorExtensions)
            {
                inspectorExtension.Core = inspectorCore;
                inspectorExtension.InspectDatabase(tempDatabaseFile, pdb);

                // reset
                inspectorExtension.Core = null;
            }

            if (inspectorCore.EncounteredError)
            {
                return false;
            }

            // validate the output if there is an MSI validator
            if (null != this.validator)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                // set the output file for source line information
                this.validator.Output = output;

                this.core.OnMessage(WixVerboses.ValidatingDatabase());

                this.validator.Validate(tempDatabaseFile);

                stopwatch.Stop();
                this.core.OnMessage(WixVerboses.ValidatedDatabase(stopwatch.ElapsedMilliseconds));

                // Stop processing if an error occurred.
                if (Messaging.Instance.EncounteredError)
                {
                    return false;
                }
            }

            // process uncompressed files
            if (!this.SuppressLayout)
            {
                this.ProcessUncompressedFiles(tempDatabaseFile, uncompressedFileRows.Values, this.fileTransfers, autoMediaAssigner.MediaRows, layoutDirectory, compressed, longNames);
            }

            // layout media
            try
            {
                this.core.OnMessage(WixVerboses.LayingOutMedia());
                this.LayoutMedia(this.fileTransfers);
            }
            finally
            {
                if (!String.IsNullOrEmpty(this.ContentsFile))
                {
                    this.CreateContentsFile(this.ContentsFile, fileRows);
                }

                if (!String.IsNullOrEmpty(this.OutputsFile))
                {
                    this.CreateOutputsFile(this.OutputsFile, this.fileTransfers, this.PdbFile);
                }

                if (!String.IsNullOrEmpty(this.BuiltOutputsFile))
                {
                    this.CreateBuiltOutputsFile(this.BuiltOutputsFile, this.fileTransfers, this.PdbFile);
                }
            }

            return !this.core.EncounteredError;
        }

        /// <summary>
        /// Get a sorted property list as a semicolon-delimited string.
        /// </summary>
        /// <param name="properties">SortedList of the properties.</param>
        /// <returns>Semicolon-delimited string representing the property list.</returns>
        private static string GetPropertyListString(SortedList properties)
        {
            bool first = true;
            StringBuilder propertiesString = new StringBuilder();

            foreach (string propertyName in properties.Keys)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    propertiesString.Append(';');
                }
                propertiesString.Append(propertyName);
            }

            return propertiesString.ToString();
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
        /// Resolve source fields in the tables included in the output
        /// </summary>
        /// <param name="tables">The tables to resolve.</param>
        /// <param name="filesWithEmbeddedFiles">Cabinets containing files that need to be patched.</param>
        /// <param name="delayedFields">The collection of delayed fields. Null if resolution of delayed fields is not allowed</param>
        private void ResolveFields(TableIndexedCollection tables, ExtractEmbeddedFiles filesWithEmbeddedFiles, IList<DelayedField> delayedFields)
        {
            foreach (Table table in tables)
            {
                foreach (Row row in table.Rows)
                {
                    foreach (Field field in row.Fields)
                    {
                        bool isDefault = true;
                        bool delayedResolve = false;

                        // Check to make sure we're in a scenario where we can handle variable resolution.
                        if (null != delayedFields)
                        {
                            // resolve localization and wix variables
                            if (field.Data is string)
                            {
                                field.Data = this.WixVariableResolver.ResolveVariables(row.SourceLineNumbers, (string)field.Data, false, ref isDefault, ref delayedResolve);
                                if (delayedResolve)
                                {
                                    delayedFields.Add(new DelayedField(row, field));
                                }
                            }
                        }

                        // Move to next row if we've hit an error resolving variables.
                        if (Messaging.Instance.EncounteredError) // TODO: make this error handling more specific to just the failure to resolve variables in this field.
                        {
                            continue;
                        }

                        // Resolve file paths
                        if (ColumnType.Object == field.Column.Type)
                        {
                            ObjectField objectField = (ObjectField)field;

                            // skip file resolution if the file is to be deleted
                            if (RowOperation.Delete == row.Operation)
                            {
                                continue;
                            }

                            // File is embedded and path to it was not modified above.
                            if (objectField.EmbeddedFileIndex.HasValue && isDefault)
                            {
                                string extractPath = filesWithEmbeddedFiles.AddEmbeddedFileIndex(objectField.BaseUri, objectField.EmbeddedFileIndex.Value, this.TempFilesLocation);

                                // Set the path to the embedded file once where it will be extracted.
                                objectField.Data = extractPath;
                            }
                            else if (null != objectField.Data) // non-compressed file (or localized value)
                            {
                                try
                                {
                                    if (OutputType.Patch != this.fileManagerCore.Output.Type) // Normal binding for non-Patch scenario such as link (light.exe)
                                    {
                                        // keep a copy of the un-resolved data for future replay. This will be saved into wixpdb file
                                        if (null == objectField.UnresolvedData)
                                        {
                                            objectField.UnresolvedData = (string)objectField.Data;
                                        }

                                        // resolve the path to the file
                                        objectField.Data = this.ResolveFile((string)objectField.Data, table.Name, row.SourceLineNumbers, BindStage.Normal);
                                    }
                                    else if (!(this.fileManagerCore.RebaseTarget || this.fileManagerCore.RebaseUpdated)) // Normal binding for Patch Scenario (normal patch, no re-basing logic)
                                    {
                                        // resolve the path to the file
                                        objectField.Data = this.ResolveFile((string)objectField.Data, table.Name, row.SourceLineNumbers, BindStage.Normal);
                                    }
                                    else // Re-base binding path scenario caused by pyro.exe -bt -bu
                                    {
                                        // by default, use the resolved Data for file lookup
                                        string filePathToResolve = (string)objectField.Data;

                                        // if -bu is used in pyro command, this condition holds true and the tool
                                        // will use pre-resolved source for new wixpdb file
                                        if (this.fileManagerCore.RebaseUpdated)
                                        {
                                            // try to use the unResolved Source if it exists.
                                            // New version of wixpdb file keeps a copy of pre-resolved Source. i.e. !(bindpath.test)\foo.dll
                                            // Old version of winpdb file does not contain this attribute and the value is null.
                                            if (null != objectField.UnresolvedData)
                                            {
                                                filePathToResolve = objectField.UnresolvedData;
                                            }
                                        }

                                        objectField.Data = this.ResolveFile(filePathToResolve, table.Name, row.SourceLineNumbers, BindStage.Updated);
                                    }
                                }
                                catch (WixFileNotFoundException)
                                {
                                    // display the error with source line information
                                    this.core.OnMessage(WixErrors.FileNotFound(row.SourceLineNumbers, (string)objectField.Data));
                                }
                            }

                            isDefault = true;
                            if (null != objectField.PreviousData)
                            {
                                objectField.PreviousData = this.WixVariableResolver.ResolveVariables(row.SourceLineNumbers, objectField.PreviousData, false, ref isDefault);
                                if (!Messaging.Instance.EncounteredError) // TODO: make this error handling more specific to just the failure to resolve variables in this field.
                                {
                                    // file is compressed in a cabinet (and not modified above)
                                    if (objectField.PreviousEmbeddedFileIndex.HasValue && isDefault)
                                    {
                                        // when loading transforms from disk, PreviousBaseUri may not have been set
                                        if (null == objectField.PreviousBaseUri)
                                        {
                                            objectField.PreviousBaseUri = objectField.BaseUri;
                                        }

                                        string extractPath = filesWithEmbeddedFiles.AddEmbeddedFileIndex(objectField.PreviousBaseUri, objectField.PreviousEmbeddedFileIndex.Value, this.TempFilesLocation);

                                        // set the path to the file once its extracted from the cabinet
                                        objectField.PreviousData = extractPath;
                                    }
                                    else if (null != objectField.PreviousData) // non-compressed file (or localized value)
                                    {
                                        try
                                        {
                                            if (!this.fileManagerCore.RebaseTarget && !this.fileManagerCore.RebaseUpdated)
                                            {
                                                // resolve the path to the file
                                                objectField.PreviousData = this.ResolveFile((string)objectField.PreviousData, table.Name, row.SourceLineNumbers, BindStage.Normal);
                                            }
                                            else
                                            {
                                                if (this.fileManagerCore.RebaseTarget)
                                                {
                                                    // if -bt is used, it come here
                                                    // Try to use the original unresolved source from either target build or update build
                                                    // If both target and updated are of old wixpdb, it behaves the same as today, no re-base logic here
                                                    // If target is old version and updated is new version, it uses unresolved path from updated build
                                                    // If both target and updated are of new versions, it uses unresolved path from target build
                                                    if (null != objectField.UnresolvedPreviousData || null != objectField.UnresolvedData)
                                                    {
                                                        objectField.PreviousData = objectField.UnresolvedPreviousData ?? objectField.UnresolvedData;
                                                    }
                                                }

                                                // resolve the path to the file
                                                objectField.PreviousData = this.ResolveFile((string)objectField.PreviousData, table.Name, row.SourceLineNumbers, BindStage.Target);

                                            }
                                        }
                                        catch (WixFileNotFoundException)
                                        {
                                            // display the error with source line information
                                            this.core.OnMessage(WixErrors.FileNotFound(row.SourceLineNumbers, (string)objectField.PreviousData));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extract embedded files for resolved data.
        /// </summary>
        /// <param name="filesWithEmbeddedFiles">Collection of files containing embedded files to be extracted.</param>
        private void ExtractEmbeddedFiles(ExtractEmbeddedFiles filesWithEmbeddedFiles)
        {
            foreach (var baseUri in filesWithEmbeddedFiles.Uris)
            {
                Stream stream = null;
                try
                {
                    // If the embedded files are stored in an assembly resource stream (usually
                    // a .wixlib embedded in a WixExtension).
                    if ("embeddedresource" == baseUri.Scheme)
                    {
                        string assemblyPath = Path.GetFullPath(baseUri.LocalPath);
                        string resourceName = baseUri.Fragment.TrimStart('#');

                        Assembly assembly = Assembly.LoadFile(assemblyPath);
                        stream = assembly.GetManifestResourceStream(resourceName);
                    }
                    else // normal file (usually a binary .wixlib on disk).
                    {
                        stream = File.OpenRead(baseUri.LocalPath);
                    }

                    using (FileStructure fs = FileStructure.Read(stream))
                    {
                        foreach (var embeddedFile in filesWithEmbeddedFiles.GetExtractFilesForUri(baseUri))
                        {
                            fs.ExtractEmbeddedFile(embeddedFile.EmbeddedFileIndex, embeddedFile.OutputPath);
                        }
                    }
                }
                finally
                {
                    if (null != stream)
                    {
                        stream.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Resolves the fields which had variables that needed to be resolved after the file information
        /// was loaded.
        /// </summary>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        /// <param name="delayedFields">The fields which had resolution delayed.</param>
        /// <param name="variableCache">The file information to use when resolving variables.</param>
        /// <param name="modularizationGuid">The modularization guid (used in case of a merge module).</param>
        private void ResolveDelayedFields(Output output, List<DelayedField> delayedFields, IDictionary<string, string> variableCache, string modularizationGuid)
        {
            List<DelayedField> deferredFields = new List<DelayedField>();

            foreach (DelayedField delayedField in delayedFields)
            {
                try
                {
                    Row propertyRow = delayedField.Row;

                    // process properties first in case they refer to other binder variables
                    if ("Property" == propertyRow.Table.Name)
                    {
                        string value = WixVariableResolver.ResolveDelayedVariables(propertyRow.SourceLineNumbers, (string)delayedField.Field.Data, variableCache);

                        // update the variable cache with the new value
                        string key = String.Concat("property.", Demodularize(output, modularizationGuid, (string)propertyRow[0]));
                        variableCache[key] = value;

                        // update the field data
                        delayedField.Field.Data = value;
                    }
                    else
                    {
                        deferredFields.Add(delayedField);
                    }
                }
                catch (WixException we)
                {
                    this.core.OnMessage(we.Error);
                    continue;
                }
            }

            // add specialization for ProductVersion fields
            string keyProductVersion = "property.ProductVersion";
            if (variableCache.ContainsKey(keyProductVersion))
            {
                string value = variableCache[keyProductVersion];
                Version productVersion = null;

                try
                {
                    productVersion = new Version(value);

                    // Don't add the variable if it already exists (developer defined a property with the same name).
                    string fieldKey = String.Concat(keyProductVersion, ".Major");
                    if (!variableCache.ContainsKey(fieldKey))
                    {
                        variableCache[fieldKey] = productVersion.Major.ToString(CultureInfo.InvariantCulture);
                    }

                    fieldKey = String.Concat(keyProductVersion, ".Minor");
                    if (!variableCache.ContainsKey(fieldKey))
                    {
                        variableCache[fieldKey] = productVersion.Minor.ToString(CultureInfo.InvariantCulture);
                    }

                    fieldKey = String.Concat(keyProductVersion, ".Build");
                    if (!variableCache.ContainsKey(fieldKey))
                    {
                        variableCache[fieldKey] = productVersion.Build.ToString(CultureInfo.InvariantCulture);
                    }

                    fieldKey = String.Concat(keyProductVersion, ".Revision");
                    if (!variableCache.ContainsKey(fieldKey))
                    {
                        variableCache[fieldKey] = productVersion.Revision.ToString(CultureInfo.InvariantCulture);
                    }
                }
                catch
                {
                    // Ignore the error introduced by new behavior.
                }
            }

            // process the remaining fields in case they refer to property binder variables
            foreach (DelayedField delayedField in deferredFields)
            {
                try
                {
                    delayedField.Field.Data = WixVariableResolver.ResolveDelayedVariables(delayedField.Row.SourceLineNumbers, (string)delayedField.Field.Data, variableCache);
                }
                catch (WixException we)
                {
                    this.core.OnMessage(we.Error);
                    continue;
                }
            }
        }

        private bool CompareFiles(string targetFile, string updatedFile)
        {
            bool? compared = null;
            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                compared = fileManager.CompareFiles(targetFile, updatedFile);
                if (compared.HasValue)
                {
                    break;
                }
            }

            if (!compared.HasValue)
            {
                throw new InvalidOperationException(); // TODO: something needs to be said here that none of the binder file managers returned a result.
            }

            return compared.Value;
        }

        private void TransferFile(bool move, string source, string destination)
        {
            bool complete = false;
            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                if (move)
                {
                    complete = fileManager.MoveFile(source, destination, true);
                }
                else
                {
                    complete = fileManager.CopyFile(source, destination, true);
                }

                if (complete)
                {
                    break;
                }
            }

            if (!complete)
            {
                throw new InvalidOperationException(); // TODO: something needs to be said here that none of the binder file managers returned a result.
            }
        }

        private string ResolveFile(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage = BindStage.Normal)
        {
            string path = null;
            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                path = fileManager.ResolveFile(source, type, sourceLineNumbers, bindStage);
                if (null != path)
                {
                    break;
                }
            }

            if (null == path)
            {
                throw new WixFileNotFoundException(sourceLineNumbers, source, type);
            }

            return path;
        }

        private string ResolveMedia(MediaRow mediaRow, string layoutDirectory)
        {
            string layout = null;
            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                layout = fileManager.ResolveMedia(mediaRow, layoutDirectory);
                if (!String.IsNullOrEmpty(layout))
                {
                    break;
                }
            }

            return layout;
        }

        private string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            string resolved = null;
            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                resolved = fileManager.ResolveUrl(url, fallbackUrl, packageId, payloadId, fileName);
                if (!String.IsNullOrEmpty(resolved))
                {
                    break;
                }
            }

            return resolved;
        }

        /// <summary>
        /// Tests sequence table for PatchFiles and associated actions
        /// </summary>
        /// <param name="iesTable">The table to test.</param>
        /// <param name="hasPatchFilesAction">Set to true if PatchFiles action is found. Left unchanged otherwise.</param>
        /// <param name="seqInstallFiles">Set to sequence value of InstallFiles action if found. Left unchanged otherwise.</param>
        /// <param name="seqDuplicateFiles">Set to sequence value of DuplicateFiles action if found. Left unchanged otherwise.</param>
        private static void TestSequenceTableForPatchFilesAction(Table iesTable, ref bool hasPatchFilesAction, ref int seqInstallFiles, ref int seqDuplicateFiles)
        {
            if (null != iesTable)
            {
                foreach (Row iesRow in iesTable.Rows)
                {
                    if (String.Equals("PatchFiles", (string)iesRow[0], StringComparison.Ordinal))
                    {
                        hasPatchFilesAction = true;
                    }
                    if (String.Equals("InstallFiles", (string)iesRow[0], StringComparison.Ordinal))
                    {
                        seqInstallFiles = (int)iesRow.Fields[2].Data;
                    }
                    if (String.Equals("DuplicateFiles", (string)iesRow[0], StringComparison.Ordinal))
                    {
                        seqDuplicateFiles = (int)iesRow.Fields[2].Data;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the PatchFiles action to the sequence table if it does not already exist.
        /// </summary>
        /// <param name="table">The sequence table to check or modify.</param>
        /// <param name="mainTransform">The primary authoring transform.</param>
        /// <param name="pairedTransform">The secondary patch transform.</param>
        /// <param name="mainFileRow">The file row that contains information about the patched file.</param>
        private void AddPatchFilesActionToSequenceTable(SequenceTable table, Output mainTransform, Output pairedTransform, Row mainFileRow)
        {
            // Find/add PatchFiles action (also determine sequence for it).
            // Search mainTransform first, then pairedTransform (pairedTransform overrides).
            bool hasPatchFilesAction = false;
            int seqInstallFiles = 0;
            int seqDuplicateFiles = 0;
            string tableName = table.ToString();

            TestSequenceTableForPatchFilesAction(
                    mainTransform.Tables[tableName],
                    ref hasPatchFilesAction,
                    ref seqInstallFiles,
                    ref seqDuplicateFiles);
            TestSequenceTableForPatchFilesAction(
                    pairedTransform.Tables[tableName],
                    ref hasPatchFilesAction,
                    ref seqInstallFiles,
                    ref seqDuplicateFiles);
            if (!hasPatchFilesAction)
            {
                Table iesTable = pairedTransform.EnsureTable(this.core.TableDefinitions[tableName]);
                if (0 == iesTable.Rows.Count)
                {
                    iesTable.Operation = TableOperation.Add;
                }
                Row patchAction = iesTable.CreateRow(null);
                WixActionRow wixPatchAction = WindowsInstallerStandard.GetStandardActions()[table, "PatchFiles"];
                int sequence = wixPatchAction.Sequence;
                // Test for default sequence value's appropriateness
                if (seqInstallFiles >= sequence || (0 != seqDuplicateFiles && seqDuplicateFiles <= sequence))
                {
                    if (0 != seqDuplicateFiles)
                    {
                        if (seqDuplicateFiles < seqInstallFiles)
                        {
                            throw new WixException(WixErrors.InsertInvalidSequenceActionOrder(mainFileRow.SourceLineNumbers, iesTable.Name, "InstallFiles", "DuplicateFiles", wixPatchAction.Action));
                        }
                        else
                        {
                            sequence = (seqDuplicateFiles + seqInstallFiles) / 2;
                            if (seqInstallFiles == sequence || seqDuplicateFiles == sequence)
                            {
                                throw new WixException(WixErrors.InsertSequenceNoSpace(mainFileRow.SourceLineNumbers, iesTable.Name, "InstallFiles", "DuplicateFiles", wixPatchAction.Action));
                            }
                        }
                    }
                    else
                    {
                        sequence = seqInstallFiles + 1;
                    }
                }
                patchAction[0] = wixPatchAction.Action;
                patchAction[1] = wixPatchAction.Condition;
                patchAction[2] = sequence;
                patchAction.Operation = RowOperation.Add;
            }
        }

        /// <summary>
        /// Copy file data between transform substorages and the patch output object
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="allFileRows">True if copying from transform to patch, false the other way.</param>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        private bool CopyTransformData(Output output, ICollection<FileRow> allFileRows)
        {
            if (OutputType.Patch != output.Type)
            {
                return true;
            }

            bool copyToPatch = (allFileRows != null);
            bool copyFromPatch = !copyToPatch;

            RowDictionary<MediaRow> patchMediaRows = new RowDictionary<MediaRow>();
            Dictionary<int, RowDictionary<FileRow>> patchMediaFileRows = new Dictionary<int, RowDictionary<FileRow>>();
            Table patchFileTable = output.EnsureTable(this.core.TableDefinitions["File"]);
            if (copyFromPatch)
            {
                // index patch files by diskId+fileId
                foreach (FileRow patchFileRow in patchFileTable.Rows)
                {
                    int diskId = patchFileRow.DiskId;
                    RowDictionary<FileRow> mediaFileRows;
                    if (!patchMediaFileRows.TryGetValue(diskId, out mediaFileRows))
                    {
                        mediaFileRows = new RowDictionary<FileRow>();
                        patchMediaFileRows.Add(diskId, mediaFileRows);
                    }

                    mediaFileRows.Add(patchFileRow);
                }

                Table patchMediaTable = output.EnsureTable(this.core.TableDefinitions["Media"]);
                patchMediaRows = new RowDictionary<MediaRow>(patchMediaTable);
            }

            // index paired transforms
            Dictionary<string, Output> pairedTransforms = new Dictionary<string, Output>();
            foreach (SubStorage substorage in output.SubStorages)
            {
                if (substorage.Name.StartsWith("#"))
                {
                    pairedTransforms.Add(substorage.Name.Substring(1), substorage.Data);
                }
            }

            try
            {
                // copy File bind data into substorages
                foreach (SubStorage substorage in output.SubStorages)
                {
                    if (substorage.Name.StartsWith("#"))
                    {
                        // no changes necessary for paired transforms
                        continue;
                    }

                    Output mainTransform = (Output)substorage.Data;
                    Table mainWixFileTable = mainTransform.Tables["WixFile"];
                    Table mainMsiFileHashTable = mainTransform.Tables["MsiFileHash"];

                    this.fileManagerCore.ActiveSubStorage = substorage;
                    RowDictionary<WixFileRow> wixFiles = new RowDictionary<WixFileRow>(mainWixFileTable);
                    RowDictionary<Row> mainMsiFileHashIndex = new RowDictionary<Row>();

                    Table mainFileTable = mainTransform.Tables["File"];
                    Output pairedTransform = (Output)pairedTransforms[substorage.Name];

                    // copy Media.LastSequence and index the MsiFileHash table if it exists.
                    if (copyFromPatch)
                    {
                        Table pairedMediaTable = pairedTransform.Tables["Media"];
                        foreach (MediaRow pairedMediaRow in pairedMediaTable.Rows)
                        {
                            MediaRow patchMediaRow = patchMediaRows.Get(pairedMediaRow.DiskId);
                            pairedMediaRow.Fields[1] = patchMediaRow.Fields[1];
                        }

                        if (null != mainMsiFileHashTable)
                        {
                            mainMsiFileHashIndex = new RowDictionary<Row>(mainMsiFileHashTable);
                        }

                        // Validate file row changes for keypath-related issues
                        this.ValidateFileRowChanges(mainTransform);
                    }

                    // Index File table of pairedTransform
                    Table pairedFileTable = pairedTransform.Tables["File"];
                    RowDictionary<FileRow> pairedFileRows = new RowDictionary<FileRow>(pairedFileTable);

                    if (null != mainFileTable)
                    {
                        if (copyFromPatch)
                        {
                            // Remove the MsiFileHash table because it will be updated later with the final file hash for each file
                            mainTransform.Tables.Remove("MsiFileHash");
                        }

                        foreach (FileRow mainFileRow in mainFileTable.Rows)
                        {
                            if (mainFileRow.Operation == RowOperation.Delete)
                            {
                                continue;
                            }

                            if (!copyToPatch && mainFileRow.Operation == RowOperation.None)
                            {
                                continue;
                            }
                            else if (copyToPatch) // when copying to the patch, we need compare the underlying files and include all file changes.
                            {
                                WixFileRow wixFileRow = wixFiles.Get((string)mainFileRow[0]);
                                ObjectField objectField = (ObjectField)wixFileRow.Fields[6];
                                FileRow pairedFileRow = pairedFileRows.Get((string)mainFileRow[0]);

                                // If the file is new, we always need to add it to the patch.
                                if (mainFileRow.Operation != RowOperation.Add)
                                {
                                    // If PreviousData doesn't exist, target and upgrade layout point to the same location. No need to compare.
                                    if (null == objectField.PreviousData)
                                    {
                                        if (mainFileRow.Operation == RowOperation.None)
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // TODO: should this entire condition be placed in the binder file manager?
                                        if ((0 == (PatchAttributeType.Ignore & wixFileRow.PatchAttributes)) &&
                                            !this.CompareFiles(objectField.PreviousData.ToString(), objectField.Data.ToString()))
                                        {
                                            // If the file is different, we need to mark the mainFileRow and pairedFileRow as modified.
                                            mainFileRow.Operation = RowOperation.Modify;
                                            if (null != pairedFileRow)
                                            {
                                                // Always patch-added, but never non-compressed.
                                                pairedFileRow.Attributes |= MsiInterop.MsidbFileAttributesPatchAdded;
                                                pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                                                pairedFileRow.Fields[6].Modified = true;
                                                pairedFileRow.Operation = RowOperation.Modify;
                                            }
                                        }
                                        else
                                        {
                                            // The File is same. We need mark all the attributes as unchanged.
                                            mainFileRow.Operation = RowOperation.None;
                                            foreach (Field field in mainFileRow.Fields)
                                            {
                                                field.Modified = false;
                                            }

                                            if (null != pairedFileRow)
                                            {
                                                pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesPatchAdded;
                                                pairedFileRow.Fields[6].Modified = false;
                                                pairedFileRow.Operation = RowOperation.None;
                                            }
                                            continue;
                                        }
                                    }
                                }
                                else if (null != pairedFileRow) // RowOperation.Add
                                {
                                    // Always patch-added, but never non-compressed.
                                    pairedFileRow.Attributes |= MsiInterop.MsidbFileAttributesPatchAdded;
                                    pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                                    pairedFileRow.Fields[6].Modified = true;
                                    pairedFileRow.Operation = RowOperation.Add;
                                }
                            }

                            // index patch files by diskId+fileId
                            int diskId = mainFileRow.DiskId;
                            RowDictionary<FileRow> mediaFileRows;
                            if (!patchMediaFileRows.TryGetValue(diskId, out mediaFileRows))
                            {
                                mediaFileRows = new RowDictionary<FileRow>();
                                patchMediaFileRows.Add(diskId, mediaFileRows);
                            }

                            string fileId = mainFileRow.File;
                            FileRow patchFileRow = mediaFileRows.Get(fileId);
                            if (copyToPatch)
                            {
                                if (null == patchFileRow)
                                {
                                    patchFileRow = (FileRow)patchFileTable.CreateRow(null);
                                    patchFileRow.CopyFrom(mainFileRow);
                                    mediaFileRows.Add(patchFileRow);
                                    allFileRows.Add(patchFileRow);
                                }
                                else
                                {
                                    // TODO: confirm the rest of data is identical?

                                    // make sure Source is same. Otherwise we are silently ignoring a file.
                                    if (0 != String.Compare(patchFileRow.Source, mainFileRow.Source, StringComparison.OrdinalIgnoreCase))
                                    {
                                        this.core.OnMessage(WixErrors.SameFileIdDifferentSource(mainFileRow.SourceLineNumbers, fileId, patchFileRow.Source, mainFileRow.Source));
                                    }
                                    // capture the previous file versions (and associated data) from this targeted instance of the baseline into the current filerow.
                                    patchFileRow.AppendPreviousDataFrom(mainFileRow);
                                }
                            }
                            else
                            {
                                // copy data from the patch back to the transform
                                if (null != patchFileRow)
                                {
                                    FileRow pairedFileRow = (FileRow)pairedFileRows.Get(fileId);
                                    for (int i = 0; i < patchFileRow.Fields.Length; i++)
                                    {
                                        string patchValue = patchFileRow[i] == null ? "" : patchFileRow[i].ToString();
                                        string mainValue = mainFileRow[i] == null ? "" : mainFileRow[i].ToString();

                                        if (1 == i)
                                        {
                                            // File.Component_ changes should not come from the shared file rows
                                            // that contain the file information as each individual transform might
                                            // have different changes (or no changes at all).
                                        }
                                        // File.Attributes should not changed for binary deltas
                                        else if (6 == i)
                                        {
                                            if (null != patchFileRow.Patch)
                                            {
                                                // File.Attribute should not change for binary deltas
                                                pairedFileRow.Attributes = mainFileRow.Attributes;
                                                mainFileRow.Fields[i].Modified = false;
                                            }
                                        }
                                        // File.Sequence is updated in pairedTransform, not mainTransform
                                        else if (7 == i)
                                        {
                                            // file sequence is updated in Patch table instead of File table for delta patches
                                            if (null != patchFileRow.Patch)
                                            {
                                                pairedFileRow.Fields[i].Modified = false;
                                            }
                                            else
                                            {
                                                pairedFileRow[i] = patchFileRow[i];
                                                pairedFileRow.Fields[i].Modified = true;
                                            }
                                            mainFileRow.Fields[i].Modified = false;
                                        }
                                        else if (patchValue != mainValue)
                                        {
                                            mainFileRow[i] = patchFileRow[i];
                                            mainFileRow.Fields[i].Modified = true;
                                            if (mainFileRow.Operation == RowOperation.None)
                                            {
                                                mainFileRow.Operation = RowOperation.Modify;
                                            }
                                        }
                                    }

                                    // copy MsiFileHash row for this File
                                    Row patchHashRow;
                                    if (!mainMsiFileHashIndex.TryGetValue(patchFileRow.File, out patchHashRow))
                                    {
                                        patchHashRow = patchFileRow.HashRow;
                                    }

                                    if (null != patchHashRow)
                                    {
                                        Table mainHashTable = mainTransform.EnsureTable(this.core.TableDefinitions["MsiFileHash"]);
                                        Row mainHashRow = mainHashTable.CreateRow(mainFileRow.SourceLineNumbers);
                                        for (int i = 0; i < patchHashRow.Fields.Length; i++)
                                        {
                                            mainHashRow[i] = patchHashRow[i];
                                            if (i > 1)
                                            {
                                                // assume all hash fields have been modified
                                                mainHashRow.Fields[i].Modified = true;
                                            }
                                        }

                                        // assume the MsiFileHash operation follows the File one
                                        mainHashRow.Operation = mainFileRow.Operation;
                                    }

                                    // copy MsiAssemblyName rows for this File
                                    List<Row> patchAssemblyNameRows = patchFileRow.AssemblyNameRows;
                                    if (null != patchAssemblyNameRows)
                                    {
                                        Table mainAssemblyNameTable = mainTransform.EnsureTable(this.core.TableDefinitions["MsiAssemblyName"]);
                                        foreach (Row patchAssemblyNameRow in patchAssemblyNameRows)
                                        {
                                            // Copy if there isn't an identical modified/added row already in the transform.
                                            bool foundMatchingModifiedRow = false;
                                            foreach (Row mainAssemblyNameRow in mainAssemblyNameTable.Rows)
                                            {
                                                if (RowOperation.None != mainAssemblyNameRow.Operation && mainAssemblyNameRow.GetPrimaryKey('/').Equals(patchAssemblyNameRow.GetPrimaryKey('/')))
                                                {
                                                    foundMatchingModifiedRow = true;
                                                    break;
                                                }
                                            }

                                            if (!foundMatchingModifiedRow)
                                            {
                                                Row mainAssemblyNameRow = mainAssemblyNameTable.CreateRow(mainFileRow.SourceLineNumbers);
                                                for (int i = 0; i < patchAssemblyNameRow.Fields.Length; i++)
                                                {
                                                    mainAssemblyNameRow[i] = patchAssemblyNameRow[i];
                                                }

                                                // assume value field has been modified
                                                mainAssemblyNameRow.Fields[2].Modified = true;
                                                mainAssemblyNameRow.Operation = mainFileRow.Operation;
                                            }
                                        }
                                    }

                                    // Add patch header for this file
                                    if (null != patchFileRow.Patch)
                                    {
                                        // Add the PatchFiles action automatically to the AdminExecuteSequence and InstallExecuteSequence tables.
                                        AddPatchFilesActionToSequenceTable(SequenceTable.AdminExecuteSequence, mainTransform, pairedTransform, mainFileRow);
                                        AddPatchFilesActionToSequenceTable(SequenceTable.InstallExecuteSequence, mainTransform, pairedTransform, mainFileRow);

                                        // Add to Patch table
                                        Table patchTable = pairedTransform.EnsureTable(this.core.TableDefinitions["Patch"]);
                                        if (0 == patchTable.Rows.Count)
                                        {
                                            patchTable.Operation = TableOperation.Add;
                                        }

                                        Row patchRow = patchTable.CreateRow(mainFileRow.SourceLineNumbers);
                                        patchRow[0] = patchFileRow.File;
                                        patchRow[1] = patchFileRow.Sequence;

                                        FileInfo patchFile = new FileInfo(patchFileRow.Source);
                                        patchRow[2] = (int)patchFile.Length;
                                        patchRow[3] = 0 == (PatchAttributeType.AllowIgnoreOnError & patchFileRow.PatchAttributes) ? 0 : 1;

                                        string streamName = patchTable.Name + "." + patchRow[0] + "." + patchRow[1];
                                        if (MsiInterop.MsiMaxStreamNameLength < streamName.Length)
                                        {
                                            streamName = "_" + Guid.NewGuid().ToString("D").ToUpper(CultureInfo.InvariantCulture).Replace('-', '_');
                                            Table patchHeadersTable = pairedTransform.EnsureTable(this.core.TableDefinitions["MsiPatchHeaders"]);
                                            if (0 == patchHeadersTable.Rows.Count)
                                            {
                                                patchHeadersTable.Operation = TableOperation.Add;
                                            }
                                            Row patchHeadersRow = patchHeadersTable.CreateRow(mainFileRow.SourceLineNumbers);
                                            patchHeadersRow[0] = streamName;
                                            patchHeadersRow[1] = patchFileRow.Patch;
                                            patchRow[5] = streamName;
                                            patchHeadersRow.Operation = RowOperation.Add;
                                        }
                                        else
                                        {
                                            patchRow[4] = patchFileRow.Patch;
                                        }
                                        patchRow.Operation = RowOperation.Add;
                                    }
                                }
                                else
                                {
                                    // TODO: throw because all transform rows should have made it into the patch
                                }
                            }
                        }
                    }

                    if (copyFromPatch)
                    {
                        output.Tables.Remove("Media");
                        output.Tables.Remove("File");
                        output.Tables.Remove("MsiFileHash");
                        output.Tables.Remove("MsiAssemblyName");
                    }
                }
            }
            finally
            {
                this.fileManagerCore.ActiveSubStorage = null;
            }

            return true;
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
        private static string Demodularize(Output output, string modularizationGuid, string id)
        {
            if (OutputType.Module == output.Type && id.EndsWith(String.Concat(".", modularizationGuid), StringComparison.Ordinal))
            {
                id = id.Substring(0, id.Length - 37);
            }

            return id;
        }

        /// <summary>
        /// Populates the variable cache with specific package properties.
        /// </summary>
        /// <param name="package">The package with properties to cache.</param>
        /// <param name="variableCache">The property cache.</param>
        private static void PopulatePackageVariableCache(ChainPackageInfo package, IDictionary<string, string> variableCache)
        {
            string id = package.Id;

            variableCache.Add(String.Concat("packageDescription.", id), package.Description);
            variableCache.Add(String.Concat("packageLanguage.", id), package.Language);
            variableCache.Add(String.Concat("packageManufacturer.", id), package.Manufacturer);
            variableCache.Add(String.Concat("packageName.", id), package.DisplayName);
            variableCache.Add(String.Concat("packageVersion.", id), package.Version);
        }

        /// <summary>
        /// Binds a bundle.
        /// </summary>
        /// <param name="bundle">The bundle to bind.</param>
        /// <param name="bundleFile">The bundle to create.</param>
        private void BindBundle(Output bundle, string bundleFile)
        {
            // First look for data we expect to find... Chain, WixGroups, etc.
            Table chainPackageTable = bundle.Tables["ChainPackage"];
            if (null == chainPackageTable || 0 == chainPackageTable.Rows.Count)
            {
                // We shouldn't really get past the linker phase if there are
                // no group items... that means that there's no UX, no Chain,
                // *and* no Containers!
                throw new WixException(WixErrors.MissingBundleInformation("ChainPackage"));
            }

            Table wixGroupTable = bundle.Tables["WixGroup"];
            if (null == wixGroupTable || 0 == wixGroupTable.Rows.Count)
            {
                // We shouldn't really get past the linker phase if there are
                // no group items... that means that there's no UX, no Chain,
                // *and* no Containers!
                throw new WixException(WixErrors.MissingBundleInformation("WixGroup"));
            }

            // Ensure there is one and only one row in the WixBundle table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            Table bundleTable = bundle.Tables["WixBundle"];
            if (null == bundleTable || 1 != bundleTable.Rows.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation("WixBundle"));
            }

            // Ensure there is one and only one row in the WixBootstrapperApplication table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            Table baTable = bundle.Tables["WixBootstrapperApplication"];
            if (null == baTable || 1 != baTable.Rows.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation("WixBootstrapperApplication"));
            }

            // Ensure there is one and only one row in the WixChain table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            Table chainTable = bundle.Tables["WixChain"];
            if (null == chainTable || 1 != chainTable.Rows.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation("WixChain"));
            }

            foreach (BinderExtension extension in this.extensions)
            {
                extension.Initialize(bundle);
            }

            if (this.core.EncounteredError)
            {
                return;
            }

            this.WriteBuildInfoTable(bundle, bundleFile);

            // gather all the wix variables
            Table wixVariableTable = bundle.Tables["WixVariable"];
            if (null != wixVariableTable)
            {
                foreach (WixVariableRow wixVariableRow in wixVariableTable.Rows)
                {
                    this.WixVariableResolver.AddVariable(wixVariableRow);
                }
            }

            ExtractEmbeddedFiles filesWithEmbeddedFiles = new ExtractEmbeddedFiles();
            List<DelayedField> delayedFields = new List<DelayedField>();

            // localize fields, resolve wix variables, and resolve file paths
            this.ResolveFields(bundle.Tables, filesWithEmbeddedFiles, delayedFields);

            // if there are any fields to resolve later, create the cache to populate during bind
            IDictionary<string, string> variableCache = null;
            if (0 < delayedFields.Count)
            {
                variableCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }

            if (this.core.EncounteredError)
            {
                return;
            }

            Table relatedBundleTable = bundle.Tables["RelatedBundle"];
            List<RelatedBundleInfo> allRelatedBundles = new List<RelatedBundleInfo>();
            if (null != relatedBundleTable && 0 < relatedBundleTable.Rows.Count)
            {
                Dictionary<string, bool> deduplicatedRelatedBundles = new Dictionary<string, bool>();
                foreach (Row row in relatedBundleTable.Rows)
                {
                    string id = (string)row[0];
                    if (!deduplicatedRelatedBundles.ContainsKey(id))
                    {
                        deduplicatedRelatedBundles[id] = true;
                        allRelatedBundles.Add(new RelatedBundleInfo(row));
                    }
                }
            }

            // Ensure that the bundle has our well-known persisted values.
            Table variableTable = bundle.EnsureTable(this.core.TableDefinitions["Variable"]);
            VariableRow bundleNameWellKnownVariable = (VariableRow)variableTable.CreateRow(null);
            bundleNameWellKnownVariable.Id = Binder.BURN_BUNDLE_NAME;
            bundleNameWellKnownVariable.Hidden = false;
            bundleNameWellKnownVariable.Persisted = true;

            VariableRow bundleOriginalSourceWellKnownVariable = (VariableRow)variableTable.CreateRow(null);
            bundleOriginalSourceWellKnownVariable.Id = Binder.BURN_BUNDLE_ORIGINAL_SOURCE;
            bundleOriginalSourceWellKnownVariable.Hidden = false;
            bundleOriginalSourceWellKnownVariable.Persisted = true;

            VariableRow bundleOriginalSourceFolderWellKnownVariable = (VariableRow)variableTable.CreateRow(null);
            bundleOriginalSourceFolderWellKnownVariable.Id = Binder.BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER;
            bundleOriginalSourceFolderWellKnownVariable.Hidden = false;
            bundleOriginalSourceFolderWellKnownVariable.Persisted = true;

            VariableRow bundleLastUsedSourceWellKnownVariable = (VariableRow)variableTable.CreateRow(null);
            bundleLastUsedSourceWellKnownVariable.Id = Binder.BURN_BUNDLE_LAST_USED_SOURCE;
            bundleLastUsedSourceWellKnownVariable.Hidden = false;
            bundleLastUsedSourceWellKnownVariable.Persisted = true;

            // To make lookups easier, we load the variable table bottom-up, so
            // that we can index by ID.
            List<VariableInfo> allVariables = new List<VariableInfo>(variableTable.Rows.Count);
            foreach (VariableRow variableRow in variableTable.Rows)
            {
                allVariables.Add(new VariableInfo(variableRow));
            }

            // TODO: Although the WixSearch tables are defined in the Util extension,
            // the Bundle Binder has to know all about them. We hope to revisit all
            // of this in the 4.0 timeframe.
            Dictionary<string, WixSearchInfo> allSearches = new Dictionary<string, WixSearchInfo>();
            Table wixFileSearchTable = bundle.Tables["WixFileSearch"];
            if (null != wixFileSearchTable && 0 < wixFileSearchTable.Rows.Count)
            {
                foreach (Row row in wixFileSearchTable.Rows)
                {
                    WixFileSearchInfo fileSearchInfo = new WixFileSearchInfo(row);
                    allSearches.Add(fileSearchInfo.Id, fileSearchInfo);
                }
            }

            Table wixRegistrySearchTable = bundle.Tables["WixRegistrySearch"];
            if (null != wixRegistrySearchTable && 0 < wixRegistrySearchTable.Rows.Count)
            {
                foreach (Row row in wixRegistrySearchTable.Rows)
                {
                    WixRegistrySearchInfo registrySearchInfo = new WixRegistrySearchInfo(row);
                    allSearches.Add(registrySearchInfo.Id, registrySearchInfo);
                }
            }

            Table wixComponentSearchTable = bundle.Tables["WixComponentSearch"];
            if (null != wixComponentSearchTable && 0 < wixComponentSearchTable.Rows.Count)
            {
                foreach (Row row in wixComponentSearchTable.Rows)
                {
                    WixComponentSearchInfo componentSearchInfo = new WixComponentSearchInfo(row);
                    allSearches.Add(componentSearchInfo.Id, componentSearchInfo);
                }
            }

            Table wixProductSearchTable = bundle.Tables["WixProductSearch"];
            if (null != wixProductSearchTable && 0 < wixProductSearchTable.Rows.Count)
            {
                foreach (Row row in wixProductSearchTable.Rows)
                {
                    WixProductSearchInfo productSearchInfo = new WixProductSearchInfo(row);
                    allSearches.Add(productSearchInfo.Id, productSearchInfo);
                }
            }

            // Merge in the variable/condition info and get the canonical ordering for
            // the searches.
            List<WixSearchInfo> orderedSearches = new List<WixSearchInfo>();
            Table wixSearchTable = bundle.Tables["WixSearch"];
            if (null != wixSearchTable && 0 < wixSearchTable.Rows.Count)
            {
                orderedSearches.Capacity = wixSearchTable.Rows.Count;
                foreach (Row row in wixSearchTable.Rows)
                {
                    WixSearchInfo searchInfo = allSearches[(string)row[0]];
                    searchInfo.AddWixSearchRowInfo(row);
                    orderedSearches.Add(searchInfo);
                }
            }

            // extract files that come from cabinet files (this does not extract files from merge modules)
            this.ExtractEmbeddedFiles(filesWithEmbeddedFiles);

            WixBundleRow bundleInfo = (WixBundleRow)bundleTable.Rows[0];
            bundleInfo.PerMachine = true; // default to per-machine but the first-per user package would flip it.

            // Get update if specified.
            Table bundleUpdateTable = bundle.Tables["WixBundleUpdate"];
            WixBundleUpdateRow bundleUpdateRow = null;
            if (null != bundleUpdateTable)
            {
                bundleUpdateRow = (WixBundleUpdateRow)bundleUpdateTable.Rows[0];
            }

            // Get update registration if specified.
            Table updateRegistrationTable = bundle.Tables["WixUpdateRegistration"];
            WixUpdateRegistrationRow updateRegistrationInfo = null;
            if (null != updateRegistrationTable)
            {
                updateRegistrationInfo = (WixUpdateRegistrationRow)updateRegistrationTable.Rows[0];
            }

            // Get the explicit payloads.
            Table payloadTable = bundle.Tables["Payload"];
            Dictionary<string, PayloadInfoRow> allPayloads = new Dictionary<string, PayloadInfoRow>(payloadTable.Rows.Count);

            Table payloadInfoTable = bundle.EnsureTable(core.TableDefinitions["PayloadInfo"]);
            foreach (PayloadInfoRow row in payloadInfoTable.Rows)
            {
                allPayloads.Add(row.Id, row);
            }

            RowDictionary<Row> payloadDisplayInformationRows = new RowDictionary<Row>(bundle.Tables["PayloadDisplayInformation"]);
            foreach (Row row in payloadTable.Rows)
            {
                string id = (string)row[0];

                PayloadInfoRow payloadInfo = null;

                if (allPayloads.ContainsKey(id))
                {
                    payloadInfo = allPayloads[id];
                }
                else
                {
                    allPayloads.Add(id, payloadInfo = (PayloadInfoRow)payloadInfoTable.CreateRow(row.SourceLineNumbers));
                }

                payloadInfo.FillFromPayloadRow(bundle, row);

                // Check if there is an override row for the display name or description.
                Row payloadDisplayInformationRow;
                if (payloadDisplayInformationRows.TryGetValue(id, out payloadDisplayInformationRow))
                {
                    if (!String.IsNullOrEmpty(payloadDisplayInformationRow[1] as string))
                    {
                        payloadInfo.ProductName = (string)payloadDisplayInformationRow[1];
                    }

                    if (!String.IsNullOrEmpty(payloadDisplayInformationRow[2] as string))
                    {
                        payloadInfo.Description = (string)payloadDisplayInformationRow[2];
                    }
                }

                if (payloadInfo.Packaging == PackagingType.Unknown)
                {
                    payloadInfo.Packaging = bundleInfo.DefaultPackagingType;
                }
            }

            Dictionary<string, ContainerInfo> containers = new Dictionary<string, ContainerInfo>();
            Dictionary<string, bool> payloadsAddedToContainers = new Dictionary<string, bool>();

            // Create the list of containers.
            Table containerTable = bundle.Tables["Container"];
            if (null != containerTable)
            {
                foreach (Row row in containerTable.Rows)
                {
                    ContainerInfo container = new ContainerInfo(row, this.fileManagerCore.TempFilesLocation);
                    containers.Add(container.Id, container);
                }
            }

            // Create the default attached container for payloads that need to be attached but don't have an explicit container.
            ContainerInfo defaultAttachedContainer = new ContainerInfo("WixAttachedContainer", "bundle-attached.cab", "attached", null, this.fileManagerCore.TempFilesLocation);
            containers.Add(defaultAttachedContainer.Id, defaultAttachedContainer);

            Row baRow = baTable.Rows[0];
            string baPayloadId = (string)baRow[0];

            // Create lists of which payloads go in each container or are layout only.
            foreach (Row row in wixGroupTable.Rows)
            {
                string rowParentName = (string)row[0];
                string rowParentType = (string)row[1];
                string rowChildName = (string)row[2];
                string rowChildType = (string)row[3];

                if (Enum.GetName(typeof(ComplexReferenceChildType), ComplexReferenceChildType.Payload) == rowChildType)
                {
                    PayloadInfoRow payload = allPayloads[rowChildName];

                    if (Enum.GetName(typeof(ComplexReferenceParentType), ComplexReferenceParentType.Container) == rowParentType)
                    {
                        ContainerInfo container = containers[rowParentName];

                        // Make sure the BA DLL is the first payload.
                        if (payload.Id.Equals(baPayloadId))
                        {
                            container.Payloads.Insert(0, payload);
                        }
                        else
                        {
                            container.Payloads.Add(payload);
                        }

                        payload.Container = container.Id;
                        payloadsAddedToContainers.Add(rowChildName, false);
                    }
                    else if (Enum.GetName(typeof(ComplexReferenceParentType), ComplexReferenceParentType.Layout) == rowParentType)
                    {
                        payload.LayoutOnly = true;
                    }
                }
            }

            ContainerInfo burnUXContainer;
            containers.TryGetValue(Compiler.BurnUXContainerId, out burnUXContainer);
            List<PayloadInfoRow> uxPayloads = null == burnUXContainer ? null : burnUXContainer.Payloads;

            // If we didn't get any UX payloads, it's an error!
            if (null == uxPayloads || 0 == uxPayloads.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation("BootstrapperApplication"));
            }

            // Get the catalog information
            Dictionary<string, CatalogInfo> catalogs = new Dictionary<string, CatalogInfo>();
            Table catalogTable = bundle.Tables["WixCatalog"];
            if (null != catalogTable)
            {
                foreach (WixCatalogRow catalogRow in catalogTable.Rows)
                {
                    // Each catalog is also a payload
                    string payloadId = Common.GenerateIdentifier("pay", catalogRow.SourceFile);
                    string catalogFile = this.ResolveFile(catalogRow.SourceFile, "Catalog", catalogRow.SourceLineNumbers, BindStage.Normal);
                    PayloadInfoRow payloadInfo = PayloadInfoRow.Create(catalogRow.SourceLineNumbers, bundle, payloadId, Path.GetFileName(catalogFile), catalogFile, true, false, null, burnUXContainer.Id, PackagingType.Embedded);

                    // Add the payload to the UX container
                    allPayloads.Add(payloadInfo.Id, payloadInfo);
                    burnUXContainer.Payloads.Add(payloadInfo);
                    payloadsAddedToContainers.Add(payloadInfo.Id, true);

                    // Create the catalog info
                    CatalogInfo catalog = new CatalogInfo(catalogRow, payloadId);
                    catalogs.Add(catalog.Id, catalog);
                }
            }

            // Get the chain packages, this may add more payloads.
            Dictionary<string, ChainPackageInfo> allPackages = new Dictionary<string, ChainPackageInfo>();
            Dictionary<string, RollbackBoundaryInfo> allBoundaries = new Dictionary<string, RollbackBoundaryInfo>();
            foreach (Row row in chainPackageTable.Rows)
            {
                Compiler.ChainPackageType type = (Compiler.ChainPackageType)Enum.Parse(typeof(Compiler.ChainPackageType), row[1].ToString(), true);
                if (Compiler.ChainPackageType.RollbackBoundary == type)
                {
                    RollbackBoundaryInfo rollbackBoundary = new RollbackBoundaryInfo(row);
                    allBoundaries.Add(rollbackBoundary.Id, rollbackBoundary);
                }
                else // package
                {
                    Table chainPackageInfoTable = bundle.EnsureTable(this.core.TableDefinitions["ChainPackageInfo"]);

                    ChainPackageInfo packageInfo = new ChainPackageInfo(row, wixGroupTable, allPayloads, containers, this.fileManagers.First(), this.core, bundle); // TODO: fix these info objects to not take the file managers or any of this and make them just rows.
                    allPackages.Add(packageInfo.Id, packageInfo);

                    chainPackageInfoTable.Rows.Add(packageInfo);

                    // Add package properties to resolve fields later.
                    if (null != variableCache)
                    {
                        Binder.PopulatePackageVariableCache(packageInfo, variableCache);
                    }
                }
            }

            // Determine patches to automatically slipstream.
            this.AutomaticallySlipstreamPatches(bundle, allPackages.Values);

            // NOTE: All payloads should be generated before here with the exception of specific engine and ux data files.

            List<FileTransfer> fileTransfers = new List<FileTransfer>();
            string layoutDirectory = Path.GetDirectoryName(bundleFile);

            // Handle any payloads not explicitly in a container.
            foreach (string payloadName in allPayloads.Keys)
            {
                if (!payloadsAddedToContainers.ContainsKey(payloadName))
                {
                    PayloadInfoRow payload = allPayloads[payloadName];
                    if (PackagingType.Embedded == payload.Packaging)
                    {
                        payload.Container = defaultAttachedContainer.Id;
                        defaultAttachedContainer.Payloads.Add(payload);
                    }
                    else if (!String.IsNullOrEmpty(payload.FullFileName))
                    {
                        FileTransfer transfer;
                        if (FileTransfer.TryCreate(payload.FullFileName, Path.Combine(layoutDirectory, payload.Name), false, "Payload", payload.SourceLineNumbers, out transfer))
                        {
                            fileTransfers.Add(transfer);
                        }
                    }
                }
            }

            // Give the UX payloads their embedded IDs...
            for (int uxPayloadIndex = 0; uxPayloadIndex < uxPayloads.Count; ++uxPayloadIndex)
            {
                PayloadInfoRow payload = uxPayloads[uxPayloadIndex];

                // In theory, UX payloads could be embedded in the UX CAB, external to the
                // bundle EXE, or even downloaded. The current engine requires the UX to be
                // fully present before any downloading starts, so that rules out downloading.
                // Also, the burn engine does not currently copy external UX payloads into
                // the temporary UX directory correctly, so we don't allow external either.
                if (PackagingType.Embedded != payload.Packaging)
                {
                    core.OnMessage(WixWarnings.UxPayloadsOnlySupportEmbedding(payload.SourceLineNumbers, payload.FullFileName));
                    payload.Packaging = PackagingType.Embedded;
                }

                payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, uxPayloadIndex);
            }

            if (this.core.EncounteredError)
            {
                return;
            }

            // If catalog files exist, non-UX payloads should validate with the catalog
            if (catalogs.Count > 0)
            {
                foreach (PayloadInfoRow payloadInfo in allPayloads.Values)
                {
                    if (String.IsNullOrEmpty(payloadInfo.EmbeddedId))
                    {
                        VerifyPayloadWithCatalog(payloadInfo, catalogs);
                    }
                }
            }

            if (this.core.EncounteredError)
            {
                return;
            }

            // Process the chain of packages to add them in the correct order
            // and assign the forward rollback boundaries as appropriate. Remember
            // rollback boundaries are authored as elements in the chain which
            // we re-interpret here to add them as attributes on the next available
            // package in the chain. Essentially we mark some packages as being
            // the start of a rollback boundary when installing and repairing.
            // We handle uninstall (aka: backwards) rollback boundaries after
            // we get these install/repair (aka: forward) rollback boundaries
            // defined.
            ChainInfo chain = new ChainInfo(chainTable.Rows[0]); // WixChain table always has one and only row in it.
            RollbackBoundaryInfo previousRollbackBoundary = new RollbackBoundaryInfo("WixDefaultBoundary"); // ensure there is always a rollback boundary at the beginning of the chain.
            foreach (Row row in wixGroupTable.Rows)
            {
                string rowParentName = (string)row[0];
                string rowParentType = (string)row[1];
                string rowChildName = (string)row[2];
                string rowChildType = (string)row[3];

                if ("PackageGroup" == rowParentType && "WixChain" == rowParentName && "Package" == rowChildType)
                {
                    ChainPackageInfo packageInfo = null;
                    if (allPackages.TryGetValue(rowChildName, out packageInfo))
                    {
                        if (null != previousRollbackBoundary)
                        {
                            chain.RollbackBoundaries.Add(previousRollbackBoundary);

                            packageInfo.RollbackBoundary = previousRollbackBoundary;
                            previousRollbackBoundary = null;
                        }

                        chain.Packages.Add(packageInfo);
                    }
                    else // must be a rollback boundary.
                    {
                        // Discard the next rollback boundary if we have a previously defined boundary. Of course,
                        // a boundary specifically defined will override the default boundary.
                        RollbackBoundaryInfo nextRollbackBoundary = allBoundaries[rowChildName];
                        if (null != previousRollbackBoundary && !previousRollbackBoundary.Default)
                        {
                            this.core.OnMessage(WixWarnings.DiscardedRollbackBoundary(nextRollbackBoundary.SourceLineNumbers, nextRollbackBoundary.Id));
                        }
                        else
                        {
                            previousRollbackBoundary = nextRollbackBoundary;
                        }
                    }
                }
            }

            if (null != previousRollbackBoundary)
            {
                this.core.OnMessage(WixWarnings.DiscardedRollbackBoundary(previousRollbackBoundary.SourceLineNumbers, previousRollbackBoundary.Id));
            }

            // With the forward rollback boundaries assigned, we can now go
            // through the packages with rollback boundaries and assign backward
            // rollback boundaries. Backward rollback boundaries are used when
            // the chain is going "backwards" which (AFAIK) only happens during
            // uninstall.
            //
            // Consider the scenario with three packages: A, B and C. Packages A
            // and C are marked as rollback boundary packages and package B is
            // not. The naive implementation would execute the chain like this
            // (numbers indicate where rollback boundaries would end up):
            //      install:    1 A B 2 C
            //      uninstall:  2 C B 1 A
            //
            // The uninstall chain is wrong, A and B should be grouped together
            // not C and B. The fix is to label packages with a "backwards"
            // rollback boundary used during uninstall. The backwards rollback
            // boundaries are assigned to the package *before* the next rollback
            // boundary. Using our example from above again, I'll mark the
            // backwards rollback boundaries prime (aka: with ').
            //      install:    1 A B 1' 2 C 2'
            //      uninstall:  2' C 2 1' B A 1
            //
            // If the marked boundaries are ignored during install you get the
            // same thing as above (good) and if the non-marked boundaries are
            // ignored during uninstall then A and B are correctly grouped.
            // Here's what it looks like without all the markers:
            //      install:    1 A B 2 C
            //      uninstall:  2 C 1 B A
            // Woot!
            string previousRollbackBoundaryId = null;
            ChainPackageInfo previousPackage = null;
            foreach (ChainPackageInfo package in chain.Packages)
            {
                if (null != package.RollbackBoundary)
                {
                    if (null != previousPackage)
                    {
                        previousPackage.RollbackBoundaryBackwardId = previousRollbackBoundaryId;
                    }

                    previousRollbackBoundaryId = package.RollbackBoundary.Id;
                }

                previousPackage = package;
            }

            if (!String.IsNullOrEmpty(previousRollbackBoundaryId) && null != previousPackage)
            {
                previousPackage.RollbackBoundaryBackwardId = previousRollbackBoundaryId;
            }

            // Give all embedded payloads that don't have an embedded ID yet an embedded ID.
            int payloadIndex = 0;
            foreach (PayloadInfoRow payload in allPayloads.Values)
            {
                Debug.Assert(PackagingType.Unknown != payload.Packaging);

                if (PackagingType.Embedded == payload.Packaging && String.IsNullOrEmpty(payload.EmbeddedId))
                {
                    payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnAttachedContainerEmbeddedIdFormat, payloadIndex);
                    ++payloadIndex;
                }
            }

            // Load the MsiProperty information...
            Table msiPropertyTable = bundle.Tables["MsiProperty"];
            if (null != msiPropertyTable && 0 < msiPropertyTable.Rows.Count)
            {
                foreach (Row row in msiPropertyTable.Rows)
                {
                    MsiPropertyInfo msiProperty = new MsiPropertyInfo(row);

                    ChainPackageInfo package;
                    if (allPackages.TryGetValue(msiProperty.PackageId, out package))
                    {
                        package.MsiProperties.Add(msiProperty);
                    }
                    else
                    {
                        core.OnMessage(WixErrors.IdentifierNotFound("Package", msiProperty.PackageId));
                    }
                }
            }

            // Load the SlipstreamMsp information...
            Table slipstreamMspTable = bundle.Tables["SlipstreamMsp"];
            if (null != slipstreamMspTable && 0 < slipstreamMspTable.Rows.Count)
            {
                foreach (Row row in slipstreamMspTable.Rows)
                {
                    string msiPackageId = (string)row[0];
                    string mspPackageId = (string)row[1];

                    if (!allPackages.ContainsKey(mspPackageId))
                    {
                        core.OnMessage(WixErrors.IdentifierNotFound("Package", mspPackageId));
                        continue;
                    }

                    ChainPackageInfo package;
                    if (!allPackages.TryGetValue(msiPackageId, out package))
                    {
                        core.OnMessage(WixErrors.IdentifierNotFound("Package", msiPackageId));
                        continue;
                    }

                    package.SlipstreamMsps.Add(mspPackageId);
                }
            }

            // Load the ExitCode information...
            Table exitCodeTable = bundle.Tables["ExitCode"];
            if (null != exitCodeTable && 0 < exitCodeTable.Rows.Count)
            {
                foreach (Row row in exitCodeTable.Rows)
                {
                    ExitCodeInfo exitCode = new ExitCodeInfo(row);

                    ChainPackageInfo package;
                    if (allPackages.TryGetValue(exitCode.PackageId, out package))
                    {
                        package.ExitCodes.Add(exitCode);
                    }
                    else
                    {
                        core.OnMessage(WixErrors.IdentifierNotFound("Package", exitCode.PackageId));
                    }
                }
            }

            // Resolve any delayed fields before generating the manifest.
            if (0 < delayedFields.Count)
            {
                this.ResolveDelayedFields(bundle, delayedFields, variableCache, null);
            }

            // Process WixApprovedExeForElevation rows.
            Table wixApprovedExeForElevationTable = bundle.Tables["WixApprovedExeForElevation"];
            List<ApprovedExeForElevation> approvedExesForElevation = new List<ApprovedExeForElevation>();
            if (null != wixApprovedExeForElevationTable && 0 < wixApprovedExeForElevationTable.Rows.Count)
            {
                foreach (WixApprovedExeForElevationRow wixApprovedExeForElevationRow in wixApprovedExeForElevationTable.Rows)
                {
                    ApprovedExeForElevation approvedExeForElevation = new ApprovedExeForElevation(wixApprovedExeForElevationRow);
                    approvedExesForElevation.Add(approvedExeForElevation);
                }
            }

            // Set the overridable bundle provider key.
            this.SetBundleProviderKey(bundle, bundleInfo);

            // Import or generate dependency providers for packages in the manifest.
            this.ProcessDependencyProviders(bundle, allPackages);

            // Generate the core-defined BA manifest tables...
            this.GenerateBAManifestPackageTables(bundle, chain.Packages);

            this.GenerateBAManifestPayloadTables(bundle, chain.Packages, allPayloads);

            foreach (BinderExtension extension in this.extensions)
            {
                extension.Finish(bundle);
            }

            // Start creating the bundle.
            this.PopulateBundleInfoFromChain(bundleInfo, chain.Packages);
            this.PopulateChainInfoTables(bundle, bundleInfo, chain.Packages);
            this.GenerateBAManifestBundleTables(bundle, bundleInfo);

            // Copy the burn.exe to a writable location then mark it to be moved to its
            // final build location.
            string stubPlatform;
            if (Platform.X64 == bundleInfo.Platform) // today, the x64 Burn uses the x86 stub.
            {
                stubPlatform = "x86";
            }
            else
            {
                stubPlatform = bundleInfo.Platform.ToString();
            }
            string wixExeDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), stubPlatform);
            string stubFile = Path.Combine(wixExeDirectory, "burn.exe");
            string bundleTempPath = Path.Combine(this.TempFilesLocation, Path.GetFileName(bundleFile));

            this.core.OnMessage(WixVerboses.GeneratingBundle(bundleTempPath, stubFile));
            File.Copy(stubFile, bundleTempPath, true);
            File.SetAttributes(bundleTempPath, FileAttributes.Normal);

            FileTransfer bundleTransfer;
            if (FileTransfer.TryCreate(bundleTempPath, bundleFile, true, "Bundle", bundleInfo.SourceLineNumbers, out bundleTransfer))
            {
                bundleTransfer.Built = true;
                fileTransfers.Add(bundleTransfer);
            }

            // Create our manifests, CABs and final EXE...
            string baManifestPath = Path.Combine(this.TempFilesLocation, "bundle-BootstrapperApplicationData.xml");
            this.CreateBootstrapperApplicationManifest(bundle, baManifestPath, uxPayloads);

            // Add the bootstrapper application manifest to the set of UX payloads.
            PayloadInfoRow baManifestPayload = PayloadInfoRow.Create(null /*TODO*/, bundle, Common.GenerateIdentifier("ux", "BootstrapperApplicationData.xml"),
                "BootstrapperApplicationData.xml", baManifestPath, false, true, null, burnUXContainer.Id, PackagingType.Embedded);
            baManifestPayload.EmbeddedId = string.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, uxPayloads.Count);
            uxPayloads.Add(baManifestPayload);

            // Create all the containers except the UX container first so the manifest in the UX container can contain all size and hash information.
            foreach (ContainerInfo container in containers.Values)
            {
                if (Compiler.BurnUXContainerId != container.Id && 0 < container.Payloads.Count)
                {
                    this.CreateContainer(container, null);
                }
            }

            string manifestPath = Path.Combine(this.TempFilesLocation, "bundle-manifest.xml");
            this.CreateBurnManifest(bundleFile, bundleInfo, bundleUpdateRow, updateRegistrationInfo, manifestPath, allRelatedBundles, allVariables, orderedSearches, allPayloads, chain, containers, catalogs, bundle.Tables["WixBundleTag"], approvedExesForElevation);

            this.UpdateBurnResources(bundleTempPath, bundleFile, bundleInfo);

            // update the .wixburn section to point to at the UX and attached container(s) then attach the container(s) if they should be attached.
            using (BurnWriter writer = BurnWriter.Open(bundleTempPath, this.core))
            {
                FileInfo burnStubFile = new FileInfo(bundleTempPath);
                writer.InitializeBundleSectionData(burnStubFile.Length, bundleInfo.BundleId);

                // Always create UX container and attach it first
                this.CreateContainer(burnUXContainer, manifestPath);
                writer.AppendContainer(burnUXContainer.TempPath, BurnWriter.Container.UX);

                // Now append all other attached containers
                foreach (ContainerInfo container in containers.Values)
                {
                    if (container.Type == "attached")
                    {
                        // The container was only created if it had payloads.
                        if (Compiler.BurnUXContainerId != container.Id && 0 < container.Payloads.Count)
                        {
                            writer.AppendContainer(container.TempPath, BurnWriter.Container.Attached);
                        }
                    }
                }
            }

            // Output the bundle to a file
            if (null != this.PdbFile)
            {
                Pdb pdb = new Pdb();
                pdb.Output = bundle;
                pdb.Save(this.PdbFile);
            }

            // Add detached containers to the list of file transfers.
            foreach (ContainerInfo container in containers.Values)
            {
                if ("detached" == container.Type)
                {
                    FileTransfer transfer;
                    if (FileTransfer.TryCreate(Path.Combine(this.TempFilesLocation, container.Name), Path.Combine(layoutDirectory, container.Name), true, "Container", container.SourceLineNumbers, out transfer))
                    {
                        transfer.Built = true;
                        fileTransfers.Add(transfer);
                    }
                }
            }

            // layout media
            try
            {
                this.core.OnMessage(WixVerboses.LayingOutMedia());
                this.LayoutMedia(fileTransfers);
            }
            finally
            {
                if (!String.IsNullOrEmpty(this.ContentsFile))
                {
                    this.CreateContentsFile(this.ContentsFile, allPayloads.Values);
                }

                if (!String.IsNullOrEmpty(this.OutputsFile))
                {
                    this.CreateOutputsFile(this.OutputsFile, fileTransfers, this.PdbFile);
                }

                if (!String.IsNullOrEmpty(this.BuiltOutputsFile))
                {
                    this.CreateBuiltOutputsFile(this.BuiltOutputsFile, fileTransfers, this.PdbFile);
                }
            }
        }

        private void GenerateBAManifestPackageTables(Output bundle, List<ChainPackageInfo> chainPackages)
        {
            Table wixPackagePropertiesTable = bundle.EnsureTable(this.core.TableDefinitions["WixPackageProperties"]);

            foreach (ChainPackageInfo package in chainPackages)
            {
                Row row = wixPackagePropertiesTable.CreateRow(package.SourceLineNumbers);
                row[0] = package.Id;
                row[1] = package.Vital ? "yes" : "no";
                row[2] = package.DisplayName;
                row[3] = package.Description;
                row[4] = package.Size.ToString(CultureInfo.InvariantCulture); // TODO: DownloadSize (compressed) (what does this mean when it's embedded?)
                row[5] = package.Size.ToString(CultureInfo.InvariantCulture); // Package.Size (uncompressed)
                row[6] = package.InstallSize.ToString(CultureInfo.InvariantCulture); // InstallSize (required disk space)
                row[7] = package.ChainPackageType.ToString(CultureInfo.InvariantCulture);
                row[8] = package.Permanent ? "yes" : "no";
                row[9] = package.LogPathVariable;
                row[10] = package.RollbackLogPathVariable;
                row[11] = (PackagingType.Embedded == package.PackagePayload.Packaging) ? "yes" : "no";
                row[12] = package.DisplayInternalUI ? "yes" : "no";
                if (!String.IsNullOrEmpty(package.ProductCode))
                {
                    row[13] = package.ProductCode;
                }
                if (!String.IsNullOrEmpty(package.UpgradeCode))
                {
                    row[14] = package.UpgradeCode;
                }
                if (!String.IsNullOrEmpty(package.Version))
                {
                    row[15] = package.Version;
                }
                if (!String.IsNullOrEmpty(package.InstallCondition))
                {
                    row[16] = package.InstallCondition;
                }
                switch (package.Cache)
                {
                    case YesNoAlwaysType.No:
                        row[17] = "no";
                        break;
                    case YesNoAlwaysType.Yes:
                        row[17] = "yes";
                        break;
                    case YesNoAlwaysType.Always:
                        row[17] = "always";
                        break;
                }

                Table wixPackageFeatureInfoTable = bundle.EnsureTable(this.core.TableDefinitions["WixPackageFeatureInfo"]);

                foreach (MsiFeature feature in package.MsiFeatures)
                {
                    Row packageFeatureInfoRow = wixPackageFeatureInfoTable.CreateRow(package.SourceLineNumbers);
                    packageFeatureInfoRow[0] = package.Id;
                    packageFeatureInfoRow[1] = feature.Name;
                    packageFeatureInfoRow[2] = Convert.ToString(feature.Size, CultureInfo.InvariantCulture);
                    packageFeatureInfoRow[3] = feature.Parent;
                    packageFeatureInfoRow[4] = feature.Title;
                    packageFeatureInfoRow[5] = feature.Description;
                    packageFeatureInfoRow[6] = Convert.ToString(feature.Display, CultureInfo.InvariantCulture);
                    packageFeatureInfoRow[7] = Convert.ToString(feature.Level, CultureInfo.InvariantCulture);
                    packageFeatureInfoRow[8] = feature.Directory;
                    packageFeatureInfoRow[9] = Convert.ToString(feature.Attributes, CultureInfo.InvariantCulture);
                }
            }
        }

        private void GenerateBAManifestPayloadTables(Output bundle, List<ChainPackageInfo> chainPackages, Dictionary<string, PayloadInfoRow> payloads)
        {
            Table wixPayloadPropertiesTable = bundle.EnsureTable(this.core.TableDefinitions["WixPayloadProperties"]);

            foreach (ChainPackageInfo package in chainPackages)
            {
                PayloadInfoRow packagePayload = payloads[package.Payload];

                Row payloadRow = wixPayloadPropertiesTable.CreateRow(packagePayload.SourceLineNumbers);
                payloadRow[0] = packagePayload.Id;
                payloadRow[1] = package.Id;
                payloadRow[2] = packagePayload.Container;
                payloadRow[3] = packagePayload.Name;
                payloadRow[4] = packagePayload.FileSize.ToString();
                payloadRow[5] = packagePayload.DownloadUrl;
                payloadRow[6] = packagePayload.LayoutOnly ? "yes" : "no";

                foreach (PayloadInfoRow childPayload in package.Payloads)
                {
                    payloadRow = wixPayloadPropertiesTable.CreateRow(childPayload.SourceLineNumbers);
                    payloadRow[0] = childPayload.Id;
                    payloadRow[1] = package.Id;
                    payloadRow[2] = childPayload.Container;
                    payloadRow[3] = childPayload.Name;
                    payloadRow[4] = childPayload.FileSize.ToString();
                    payloadRow[5] = childPayload.DownloadUrl;
                    payloadRow[6] = childPayload.LayoutOnly ? "yes" : "no";
                }
            }

            foreach (PayloadInfoRow payload in payloads.Values)
            {
                if (payload.LayoutOnly)
                {
                    Row row = wixPayloadPropertiesTable.CreateRow(payload.SourceLineNumbers);
                    row[0] = payload.Id;
                    row[1] = null;
                    row[2] = payload.Container;
                    row[3] = payload.Name;
                    row[4] = payload.FileSize.ToString();
                    row[5] = payload.DownloadUrl;
                    row[6] = payload.LayoutOnly ? "yes" : "no";
                }
            }
        }

        private void AutomaticallySlipstreamPatches(Output bundle, ICollection<ChainPackageInfo> packages)
        {
            List<ChainPackageInfo> msiPackages = new List<ChainPackageInfo>();
            Dictionary<string, List<WixBundlePatchTargetCodeRow>> targetsProductCode = new Dictionary<string, List<WixBundlePatchTargetCodeRow>>();
            Dictionary<string, List<WixBundlePatchTargetCodeRow>> targetsUpgradeCode = new Dictionary<string, List<WixBundlePatchTargetCodeRow>>();

            foreach (ChainPackageInfo package in packages)
            {
                if (Compiler.ChainPackageType.Msi == package.ChainPackageType)
                {
                    // Keep track of all MSI packages.
                    msiPackages.Add(package);
                }
                else if (Compiler.ChainPackageType.Msp == package.ChainPackageType && package.Slipstream)
                {
                    // Index target ProductCodes and UpgradeCodes for slipstreamed MSPs.
                    foreach (WixBundlePatchTargetCodeRow row in package.TargetCodes)
                    {
                        if (row.TargetsProductCode)
                        {
                            List<WixBundlePatchTargetCodeRow> rows;
                            if (!targetsProductCode.TryGetValue(row.TargetCode, out rows))
                            {
                                rows = new List<WixBundlePatchTargetCodeRow>();
                                targetsProductCode.Add(row.TargetCode, rows);
                            }

                            rows.Add(row);
                        }
                        else if (row.TargetsUpgradeCode)
                        {
                            List<WixBundlePatchTargetCodeRow> rows;
                            if (!targetsUpgradeCode.TryGetValue(row.TargetCode, out rows))
                            {
                                rows = new List<WixBundlePatchTargetCodeRow>();
                                targetsUpgradeCode.Add(row.TargetCode, rows);
                            }
                        }
                    }
                }
            }

            Table slipstreamMspTable = bundle.EnsureTable(this.core.TableDefinitions["SlipstreamMsp"]);
            RowIndexedList<Row> slipstreamMspRows = new RowIndexedList<Row>(slipstreamMspTable);

            // Loop through the MSI and slipstream patches targeting it.
            foreach (ChainPackageInfo msi in msiPackages)
            {
                List<WixBundlePatchTargetCodeRow> rows;
                if (targetsProductCode.TryGetValue(msi.ProductCode, out rows))
                {
                    foreach (WixBundlePatchTargetCodeRow row in rows)
                    {
                        Row slipstreamMspRow = slipstreamMspTable.CreateRow(row.SourceLineNumbers, false);
                        slipstreamMspRow[0] = msi.Id;
                        slipstreamMspRow[1] = row.MspPackageId;

                        if (slipstreamMspRows.TryAdd(slipstreamMspRow))
                        {
                            slipstreamMspTable.Rows.Add(slipstreamMspRow);
                        }
                    }

                    rows = null;
                }

                if (!String.IsNullOrEmpty(msi.UpgradeCode) && targetsUpgradeCode.TryGetValue(msi.UpgradeCode, out rows))
                {
                    foreach (WixBundlePatchTargetCodeRow row in rows)
                    {
                        Row slipstreamMspRow = slipstreamMspTable.CreateRow(row.SourceLineNumbers, false);
                        slipstreamMspRow[0] = msi.Id;
                        slipstreamMspRow[1] = row.MspPackageId;

                        if (slipstreamMspRows.TryAdd(slipstreamMspRow))
                        {
                            slipstreamMspTable.Rows.Add(slipstreamMspRow);
                        }
                    }

                    rows = null;
                }
            }
        }

        private void PopulateBundleInfoFromChain(WixBundleRow bundleInfo, List<ChainPackageInfo> chainPackages)
        {
            foreach (ChainPackageInfo package in chainPackages)
            {
                if (bundleInfo.PerMachine && YesNoDefaultType.No == package.PerMachine)
                {
                    this.core.OnMessage(WixVerboses.SwitchingToPerUserPackage(package.PackagePayload.FullFileName));
                    bundleInfo.PerMachine = false;
                }
            }
        }

        private void PopulateChainInfoTables(Output bundle, WixBundleRow bundleInfo, List<ChainPackageInfo> chainPackages)
        {
            bool hasPerMachineNonPermanentPackages = false;

            foreach (ChainPackageInfo package in chainPackages)
            {
                // Update package scope from bundle scope if default.
                if (YesNoDefaultType.Default == package.PerMachine)
                {
                    package.PerMachine = bundleInfo.PerMachine ? YesNoDefaultType.Yes : YesNoDefaultType.No;
                }

                // Keep track if any per-machine non-permanent packages exist.
                if (YesNoDefaultType.Yes == package.PerMachine && 0 < package.Provides.Count && !package.Permanent)
                {
                    hasPerMachineNonPermanentPackages = true;
                }

                switch (package.ChainPackageType)
                {
                    case Compiler.ChainPackageType.Msi:
                        Table chainMsiPackageTable = bundle.EnsureTable(this.core.TableDefinitions["ChainMsiPackage"]);
                        ChainMsiPackageRow row = (ChainMsiPackageRow)chainMsiPackageTable.CreateRow(null);
                        row.ChainPackage = package.Id;
                        row.ProductCode = package.ProductCode;
                        row.ProductLanguage = Convert.ToInt32(package.Language, CultureInfo.InvariantCulture);
                        row.ProductName = package.DisplayName;
                        row.ProductVersion = package.Version;
                        if (!String.IsNullOrEmpty(package.UpgradeCode))
                        {
                            row.UpgradeCode = package.UpgradeCode;
                        }
                        break;
                    default:
                        break;
                }
            }

            // We will only register packages in the same scope as the bundle.
            // Warn if any packages with providers are in a different scope
            // and not permanent (permanents typically don't need a ref-count).
            if (!bundleInfo.PerMachine && hasPerMachineNonPermanentPackages)
            {
                this.core.OnMessage(WixWarnings.NoPerMachineDependencies());
            }
        }

        private void GenerateBAManifestBundleTables(Output bundle, WixBundleRow bundleInfo)
        {
            Table wixBundlePropertiesTable = bundle.EnsureTable(this.core.TableDefinitions["WixBundleProperties"]);
            Row row = wixBundlePropertiesTable.CreateRow(bundleInfo.SourceLineNumbers);
            row[0] = bundleInfo.Name;
            row[1] = bundleInfo.LogPathVariable;
            row[2] = (YesNoDefaultType.Yes == bundleInfo.Compressed) ? "yes" : "no";
            row[3] = bundleInfo.BundleId.ToString("B");
            row[4] = bundleInfo.UpgradeCode;
            row[5] = bundleInfo.PerMachine ? "yes" : "no";
        }

        private void CreateContainer(ContainerInfo container, string manifestFile)
        {
            int payloadCount = container.Payloads.Count; // The number of embedded payloads
            if (!String.IsNullOrEmpty(manifestFile))
            {
                ++payloadCount;
            }

            using (WixCreateCab cab = new WixCreateCab(Path.GetFileName(container.TempPath), Path.GetDirectoryName(container.TempPath), payloadCount, 0, 0, this.DefaultCompressionLevel))
            {
                // If a manifest was provided always add it as "payload 0" to the container.
                if (!String.IsNullOrEmpty(manifestFile))
                {
                    cab.AddFile(manifestFile, "0");
                }

                foreach (PayloadInfoRow payload in container.Payloads)
                {
                    Debug.Assert(PackagingType.Embedded == payload.Packaging);
                    this.core.OnMessage(WixVerboses.LoadingPayload(payload.FullFileName));
                    cab.AddFile(payload.FullFileName, payload.EmbeddedId);
                }

                cab.Complete();
            }
        }

        private void CreateBootstrapperApplicationManifest(Output bundle, string path, List<PayloadInfoRow> uxPayloads)
        {
            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("BootstrapperApplicationData", "http://wixtoolset.org/schemas/v4/2010/BootstrapperApplicationData");

                foreach (Table table in bundle.Tables)
                {
                    if (table.Definition.BootstrapperApplicationData && null != table.Rows && 0 < table.Rows.Count)
                    {
                        // We simply assert that the table (and field) name is valid, because
                        // this is up to the extension developer to get right. An author will
                        // only affect the attribute value, and that will get properly escaped.
#if DEBUG
                        Debug.Assert(Common.IsIdentifier(table.Name));
                        foreach (ColumnDefinition column in table.Definition.Columns)
                        {
                            Debug.Assert(Common.IsIdentifier(column.Name));
                        }
#endif // DEBUG

                        foreach (Row row in table.Rows)
                        {
                            writer.WriteStartElement(table.Name);

                            foreach (Field field in row.Fields)
                            {
                                if (null != field.Data)
                                {
                                    writer.WriteAttributeString(field.Column.Name, field.Data.ToString());
                                }
                            }

                            writer.WriteEndElement();
                        }
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void CreateBurnManifest(string outputPath, WixBundleRow bundleInfo, WixBundleUpdateRow updateRow, WixUpdateRegistrationRow updateRegistrationInfo, string path, List<RelatedBundleInfo> allRelatedBundles, List<VariableInfo> allVariables, List<WixSearchInfo> orderedSearches, Dictionary<string, PayloadInfoRow> allPayloads, ChainInfo chain, Dictionary<string, ContainerInfo> containers, Dictionary<string, CatalogInfo> catalogs, Table wixBundleTagTable, List<ApprovedExeForElevation> approvedExesForElevation)
        {
            string executableName = Path.GetFileName(outputPath);

            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("BurnManifest", BurnCommon.BurnNamespace);

                // Write the condition, if there is one
                if (null != bundleInfo.Condition)
                {
                    writer.WriteElementString("Condition", bundleInfo.Condition);
                }

                // Write the log element if default logging wasn't disabled.
                if (!String.IsNullOrEmpty(bundleInfo.LogPrefix))
                {
                    writer.WriteStartElement("Log");
                    if (!String.IsNullOrEmpty(bundleInfo.LogPathVariable))
                    {
                        writer.WriteAttributeString("PathVariable", bundleInfo.LogPathVariable);
                    }
                    writer.WriteAttributeString("Prefix", bundleInfo.LogPrefix);
                    writer.WriteAttributeString("Extension", bundleInfo.LogExtension);
                    writer.WriteEndElement();
                }

                if (null != updateRow)
                {
                    writer.WriteStartElement("Update");
                    writer.WriteAttributeString("Location", updateRow.Location);
                    writer.WriteEndElement(); // </Update>
                }

                // Write the RelatedBundle elements
                foreach (RelatedBundleInfo relatedBundle in allRelatedBundles)
                {
                    relatedBundle.WriteXml(writer);
                }

                // Write the variables
                foreach (VariableInfo variable in allVariables)
                {
                    variable.WriteXml(writer);
                }

                // Write the searches
                foreach (WixSearchInfo searchinfo in orderedSearches)
                {
                    searchinfo.WriteXml(writer);
                }

                // write the UX element
                writer.WriteStartElement("UX");
                if (!String.IsNullOrEmpty(bundleInfo.SplashScreenBitmapPath))
                {
                    writer.WriteAttributeString("SplashScreen", "yes");
                }

                // write the UX allPayloads...
                List<PayloadInfoRow> uxPayloads = containers[Compiler.BurnUXContainerId].Payloads;
                foreach (PayloadInfoRow payload in uxPayloads)
                {
                    writer.WriteStartElement("Payload");
                    WriteBurnManifestPayloadAttributes(writer, payload, true, allPayloads);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                // write the catalog elements
                if (catalogs.Count > 0)
                {
                    foreach (CatalogInfo catalog in catalogs.Values)
                    {
                        writer.WriteStartElement("Catalog");
                        writer.WriteAttributeString("Id", catalog.Id);
                        writer.WriteAttributeString("Payload", catalog.PayloadId);
                        writer.WriteEndElement();
                    }
                }

                int attachedContainerIndex = 1; // count starts at one because UX container is "0".
                foreach (ContainerInfo container in containers.Values)
                {
                    if (Compiler.BurnUXContainerId != container.Id && 0 < container.Payloads.Count)
                    {
                        writer.WriteStartElement("Container");
                        WriteBurnManifestContainerAttributes(writer, executableName, container, attachedContainerIndex);
                        writer.WriteEndElement();
                        if ("attached" == container.Type)
                        {
                            attachedContainerIndex++;
                        }
                    }
                }

                foreach (PayloadInfoRow payload in allPayloads.Values)
                {
                    if (PackagingType.Embedded == payload.Packaging && Compiler.BurnUXContainerId != payload.Container)
                    {
                        writer.WriteStartElement("Payload");
                        WriteBurnManifestPayloadAttributes(writer, payload, true, allPayloads);
                        writer.WriteEndElement();
                    }
                    else if (PackagingType.External == payload.Packaging)
                    {
                        writer.WriteStartElement("Payload");
                        WriteBurnManifestPayloadAttributes(writer, payload, false, allPayloads);
                        writer.WriteEndElement();
                    }
                }

                foreach (RollbackBoundaryInfo rollbackBoundary in chain.RollbackBoundaries)
                {
                    writer.WriteStartElement("RollbackBoundary");
                    writer.WriteAttributeString("Id", rollbackBoundary.Id);
                    writer.WriteAttributeString("Vital", YesNoType.Yes == rollbackBoundary.Vital ? "yes" : "no");
                    writer.WriteEndElement();
                }

                // Write the registration information...
                writer.WriteStartElement("Registration");

                writer.WriteAttributeString("Id", bundleInfo.BundleId.ToString("B"));
                writer.WriteAttributeString("ExecutableName", executableName);
                writer.WriteAttributeString("PerMachine", bundleInfo.PerMachine ? "yes" : "no");
                writer.WriteAttributeString("Tag", bundleInfo.Tag);
                writer.WriteAttributeString("Version", bundleInfo.Version);
                writer.WriteAttributeString("ProviderKey", bundleInfo.ProviderKey);

                writer.WriteStartElement("Arp");
                writer.WriteAttributeString("Register", (0 < bundleInfo.DisableModify && bundleInfo.DisableRemove) ? "no" : "yes"); // do not register if disabled modify and remove.
                writer.WriteAttributeString("DisplayName", bundleInfo.Name);
                writer.WriteAttributeString("DisplayVersion", bundleInfo.Version);

                if (!String.IsNullOrEmpty(bundleInfo.Publisher))
                {
                    writer.WriteAttributeString("Publisher", bundleInfo.Publisher);
                }

                if (!String.IsNullOrEmpty(bundleInfo.HelpLink))
                {
                    writer.WriteAttributeString("HelpLink", bundleInfo.HelpLink);
                }

                if (!String.IsNullOrEmpty(bundleInfo.HelpTelephone))
                {
                    writer.WriteAttributeString("HelpTelephone", bundleInfo.HelpTelephone);
                }

                if (!String.IsNullOrEmpty(bundleInfo.AboutUrl))
                {
                    writer.WriteAttributeString("AboutUrl", bundleInfo.AboutUrl);
                }

                if (!String.IsNullOrEmpty(bundleInfo.UpdateUrl))
                {
                    writer.WriteAttributeString("UpdateUrl", bundleInfo.UpdateUrl);
                }

                if (!String.IsNullOrEmpty(bundleInfo.ParentName))
                {
                    writer.WriteAttributeString("ParentDisplayName", bundleInfo.ParentName);
                }

                if (1 == bundleInfo.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "yes");
                }
                else if (2 == bundleInfo.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "button");
                }

                if (bundleInfo.DisableRemove)
                {
                    writer.WriteAttributeString("DisableRemove", "yes");
                }
                writer.WriteEndElement(); // </Arp>

                if (null != updateRegistrationInfo)
                {
                    writer.WriteStartElement("Update"); // <Update>
                    writer.WriteAttributeString("Manufacturer", updateRegistrationInfo.Manufacturer);

                    if (!String.IsNullOrEmpty(updateRegistrationInfo.Department))
                    {
                        writer.WriteAttributeString("Department", updateRegistrationInfo.Department);
                    }

                    if (!String.IsNullOrEmpty(updateRegistrationInfo.ProductFamily))
                    {
                        writer.WriteAttributeString("ProductFamily", updateRegistrationInfo.ProductFamily);
                    }

                    writer.WriteAttributeString("Name", updateRegistrationInfo.Name);
                    writer.WriteAttributeString("Classification", updateRegistrationInfo.Classification);
                    writer.WriteEndElement(); // </Update>
                }

                if (null != wixBundleTagTable)
                {
                    foreach (Row row in wixBundleTagTable.Rows)
                    {
                        writer.WriteStartElement("SoftwareTag");
                        writer.WriteAttributeString("Filename", (string)row[0]);
                        writer.WriteAttributeString("Regid", (string)row[1]);
                        writer.WriteCData((string)row[4]);
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement(); // </Register>

                // write the Chain...
                writer.WriteStartElement("Chain");
                if (chain.DisableRollback)
                {
                    writer.WriteAttributeString("DisableRollback", "yes");
                }

                if (chain.DisableSystemRestore)
                {
                    writer.WriteAttributeString("DisableSystemRestore", "yes");
                }

                if (chain.ParallelCache)
                {
                    writer.WriteAttributeString("ParallelCache", "yes");
                }

                // Build up the list of target codes from all the MSPs in the chain.
                List<WixBundlePatchTargetCodeRow> targetCodes = new List<WixBundlePatchTargetCodeRow>();

                foreach (ChainPackageInfo package in chain.Packages)
                {
                    writer.WriteStartElement(String.Format(CultureInfo.InvariantCulture, "{0}Package", package.ChainPackageType));

                    writer.WriteAttributeString("Id", package.Id);

                    switch (package.Cache)
                    {
                        case YesNoAlwaysType.No:
                            writer.WriteAttributeString("Cache", "no");
                            break;
                        case YesNoAlwaysType.Yes:
                            writer.WriteAttributeString("Cache", "yes");
                            break;
                        case YesNoAlwaysType.Always:
                            writer.WriteAttributeString("Cache", "always");
                            break;
                    }

                    writer.WriteAttributeString("CacheId", package.CacheId);
                    writer.WriteAttributeString("InstallSize", Convert.ToString(package.InstallSize));
                    writer.WriteAttributeString("Size", Convert.ToString(package.Size));
                    writer.WriteAttributeString("PerMachine", YesNoDefaultType.Yes == package.PerMachine ? "yes" : "no");
                    writer.WriteAttributeString("Permanent", package.Permanent ? "yes" : "no");
                    writer.WriteAttributeString("Vital", package.Vital ? "yes" : "no");

                    if (null != package.RollbackBoundary)
                    {
                        writer.WriteAttributeString("RollbackBoundaryForward", package.RollbackBoundary.Id);
                    }

                    if (!String.IsNullOrEmpty(package.RollbackBoundaryBackwardId))
                    {
                        writer.WriteAttributeString("RollbackBoundaryBackward", package.RollbackBoundaryBackwardId);
                    }

                    if (!String.IsNullOrEmpty(package.LogPathVariable))
                    {
                        writer.WriteAttributeString("LogPathVariable", package.LogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.RollbackLogPathVariable))
                    {
                        writer.WriteAttributeString("RollbackLogPathVariable", package.RollbackLogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.InstallCondition))
                    {
                        writer.WriteAttributeString("InstallCondition", package.InstallCondition);
                    }

                    if (Compiler.ChainPackageType.Exe == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("DetectCondition", package.DetectCondition);
                        writer.WriteAttributeString("InstallArguments", package.InstallCommand);
                        writer.WriteAttributeString("UninstallArguments", package.UninstallCommand);
                        writer.WriteAttributeString("RepairArguments", package.RepairCommand);
                        writer.WriteAttributeString("Repairable", package.Repairable ? "yes" : "no");
                        if (!String.IsNullOrEmpty(package.Protocol))
                        {
                            writer.WriteAttributeString("Protocol", package.Protocol);
                        }
                    }
                    else if (Compiler.ChainPackageType.Msi == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("ProductCode", package.ProductCode);
                        writer.WriteAttributeString("Language", package.Language);
                        writer.WriteAttributeString("Version", package.Version);
                        writer.WriteAttributeString("DisplayInternalUI", package.DisplayInternalUI ? "yes" : "no");
                        if (!String.IsNullOrEmpty(package.UpgradeCode))
                        {
                            writer.WriteAttributeString("UpgradeCode", package.UpgradeCode);
                        }
                    }
                    else if (Compiler.ChainPackageType.Msp == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("PatchCode", package.PatchCode);
                        writer.WriteAttributeString("PatchXml", package.PatchXml);
                        writer.WriteAttributeString("DisplayInternalUI", package.DisplayInternalUI ? "yes" : "no");

                        // If there is still a chance that all of our patches will target a narrow set of
                        // product codes, add the patch list to the overall list.
                        if (null != targetCodes)
                        {
                            if (!package.TargetUnspecified)
                            {
                                targetCodes.AddRange(package.TargetCodes);
                            }
                            else // we have a patch that targets the world, so throw the whole list away.
                            {
                                targetCodes = null;
                            }
                        }
                    }
                    else if (Compiler.ChainPackageType.Msu == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("DetectCondition", package.DetectCondition);
                        writer.WriteAttributeString("KB", package.MsuKB);
                    }

                    foreach (MsiFeature feature in package.MsiFeatures)
                    {
                        writer.WriteStartElement("MsiFeature");
                        writer.WriteAttributeString("Id", feature.Name);
                        writer.WriteEndElement();
                    }

                    foreach (MsiPropertyInfo msiProperty in package.MsiProperties)
                    {
                        writer.WriteStartElement("MsiProperty");
                        writer.WriteAttributeString("Id", msiProperty.Name);
                        writer.WriteAttributeString("Value", msiProperty.Value);
                        writer.WriteEndElement();
                    }

                    foreach (string slipstreamMsp in package.SlipstreamMsps)
                    {
                        writer.WriteStartElement("SlipstreamMsp");
                        writer.WriteAttributeString("Id", slipstreamMsp);
                        writer.WriteEndElement();
                    }

                    foreach (ExitCodeInfo exitCode in package.ExitCodes)
                    {
                        writer.WriteStartElement("ExitCode");
                        writer.WriteAttributeString("Type", exitCode.Type);
                        writer.WriteAttributeString("Code", exitCode.Code);
                        writer.WriteEndElement();
                    }

                    // Output the dependency information.
                    foreach (ProvidesDependency dependency in package.Provides)
                    {
                        // TODO: Add to wixpdb as an imported table, or link package wixpdbs to bundle wixpdbs.
                        dependency.WriteXml(writer);
                    }

                    foreach (RelatedPackage related in package.RelatedPackages)
                    {
                        writer.WriteStartElement("RelatedPackage");
                        writer.WriteAttributeString("Id", related.Id);
                        if (!String.IsNullOrEmpty(related.MinVersion))
                        {
                            writer.WriteAttributeString("MinVersion", related.MinVersion);
                            writer.WriteAttributeString("MinInclusive", related.MinInclusive ? "yes" : "no");
                        }
                        if (!String.IsNullOrEmpty(related.MaxVersion))
                        {
                            writer.WriteAttributeString("MaxVersion", related.MaxVersion);
                            writer.WriteAttributeString("MaxInclusive", related.MaxInclusive ? "yes" : "no");
                        }
                        writer.WriteAttributeString("OnlyDetect", related.OnlyDetect ? "yes" : "no");
                        if (0 < related.Languages.Count)
                        {
                            writer.WriteAttributeString("LangInclusive", related.LangInclusive ? "yes" : "no");
                            foreach (string language in related.Languages)
                            {
                                writer.WriteStartElement("Language");
                                writer.WriteAttributeString("Id", language);
                                writer.WriteEndElement();
                            }
                        }
                        writer.WriteEndElement();
                    }

                    // Write any contained Payloads with the PackagePayload being first
                    writer.WriteStartElement("PayloadRef");
                    writer.WriteAttributeString("Id", package.PackagePayload.Id);
                    writer.WriteEndElement();

                    foreach (PayloadInfoRow payload in package.Payloads)
                    {
                        if (payload.Id != package.PackagePayload.Id)
                        {
                            writer.WriteStartElement("PayloadRef");
                            writer.WriteAttributeString("Id", payload.Id);
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteEndElement(); // </XxxPackage>
                }
                writer.WriteEndElement(); // </Chain>

                if (null != targetCodes)
                {
                    foreach (WixBundlePatchTargetCodeRow targetCode in targetCodes)
                    {
                        writer.WriteStartElement("PatchTargetCode");
                        writer.WriteAttributeString("TargetCode", targetCode.TargetCode);
                        writer.WriteAttributeString("Product", targetCode.TargetsProductCode ? "yes" : "no");
                        writer.WriteEndElement();
                    }
                }

                // write the ApprovedExeForElevation elements
                if (0 < approvedExesForElevation.Count)
                {
                    foreach (ApprovedExeForElevation approvedExeForElevation in approvedExesForElevation)
                    {
                        writer.WriteStartElement("ApprovedExeForElevation");
                        writer.WriteAttributeString("Id", approvedExeForElevation.Id);
                        writer.WriteAttributeString("Key", approvedExeForElevation.Key);

                        if (!String.IsNullOrEmpty(approvedExeForElevation.ValueName))
                        {
                            writer.WriteAttributeString("ValueName", approvedExeForElevation.ValueName);
                        }

                        if (approvedExeForElevation.Win64)
                        {
                            writer.WriteAttributeString("Win64", "yes");
                        }

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndDocument(); // </BurnManifest>
            }
        }

        private void UpdateBurnResources(string bundleTempPath, string outputPath, WixBundleRow bundleInfo)
        {
            WixToolset.Dtf.Resources.ResourceCollection resources = new WixToolset.Dtf.Resources.ResourceCollection();
            WixToolset.Dtf.Resources.VersionResource version = new WixToolset.Dtf.Resources.VersionResource("#1", 1033);

            version.Load(bundleTempPath);
            resources.Add(version);

            // Ensure the bundle info provides a full four part version.
            Version fourPartVersion = new Version(bundleInfo.Version);
            int major = (fourPartVersion.Major < 0) ? 0 : fourPartVersion.Major;
            int minor = (fourPartVersion.Minor < 0) ? 0 : fourPartVersion.Minor;
            int build = (fourPartVersion.Build < 0) ? 0 : fourPartVersion.Build;
            int revision = (fourPartVersion.Revision < 0) ? 0 : fourPartVersion.Revision;

            if (UInt16.MaxValue < major || UInt16.MaxValue < minor || UInt16.MaxValue < build || UInt16.MaxValue < revision)
            {
                throw new WixException(WixErrors.InvalidModuleOrBundleVersion(bundleInfo.SourceLineNumbers, "Bundle", bundleInfo.Version));
            }

            fourPartVersion = new Version(major, minor, build, revision);
            version.FileVersion = fourPartVersion;
            version.ProductVersion = fourPartVersion;

            WixToolset.Dtf.Resources.VersionStringTable strings = version[1033];
            strings["LegalCopyright"] = bundleInfo.Copyright;
            strings["OriginalFilename"] = Path.GetFileName(outputPath);
            strings["FileVersion"] = bundleInfo.Version;    // string versions do not have to be four parts.
            strings["ProductVersion"] = bundleInfo.Version; // string versions do not have to be four parts.

            if (!String.IsNullOrEmpty(bundleInfo.Name))
            {
                strings["ProductName"] = bundleInfo.Name;
                strings["FileDescription"] = bundleInfo.Name;
            }

            if (!String.IsNullOrEmpty(bundleInfo.Publisher))
            {
                strings["CompanyName"] = bundleInfo.Publisher;
            }
            else
            {
                strings["CompanyName"] = String.Empty;
            }

            if (!String.IsNullOrEmpty(bundleInfo.IconPath))
            {
                Dtf.Resources.GroupIconResource iconGroup = new Dtf.Resources.GroupIconResource("#1", 1033);
                iconGroup.ReadFromFile(bundleInfo.IconPath);
                resources.Add(iconGroup);

                foreach (Dtf.Resources.Resource icon in iconGroup.Icons)
                {
                    resources.Add(icon);
                }
            }

            if (!String.IsNullOrEmpty(bundleInfo.SplashScreenBitmapPath))
            {
                Dtf.Resources.BitmapResource bitmap = new Dtf.Resources.BitmapResource("#1", 1033);
                bitmap.ReadFromFile(bundleInfo.SplashScreenBitmapPath);
                resources.Add(bitmap);
            }

            resources.Save(bundleTempPath);
        }

        private void WriteBurnManifestContainerAttributes(XmlTextWriter writer, string executableName, ContainerInfo container, int containerIndex)
        {
            writer.WriteAttributeString("Id", container.Id);
            writer.WriteAttributeString("FileSize", container.FileInfo.Length.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", Common.GetFileHash(container.FileInfo));
            if (container.Type == "detached")
            {
                string resolvedUrl = this.ResolveUrl(container.DownloadUrl, null, null, container.Id, container.Name);
                if (!String.IsNullOrEmpty(resolvedUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", resolvedUrl);
                }
                else if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", container.DownloadUrl);
                }

                writer.WriteAttributeString("FilePath", container.Name);
            }
            else if (container.Type == "attached")
            {
                if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    this.core.OnMessage(WixWarnings.DownloadUrlNotSupportedForAttachedContainers(container.SourceLineNumbers, container.Id));
                }

                writer.WriteAttributeString("FilePath", executableName); // attached containers use the name of the bundle since they are attached to the executable.
                writer.WriteAttributeString("AttachedIndex", containerIndex.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Attached", "yes");
                writer.WriteAttributeString("Primary", "yes");
            }
        }

        private void WriteBurnManifestPayloadAttributes(XmlTextWriter writer, PayloadInfoRow payload, bool embeddedOnly, Dictionary<string, PayloadInfoRow> allPayloads)
        {
            Debug.Assert(!embeddedOnly || PackagingType.Embedded == payload.Packaging);

            writer.WriteAttributeString("Id", payload.Id);
            writer.WriteAttributeString("FilePath", payload.Name);
            writer.WriteAttributeString("FileSize", payload.FileSize.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", payload.Hash);

            if (payload.LayoutOnly)
            {
                writer.WriteAttributeString("LayoutOnly", "yes");
            }

            if (!String.IsNullOrEmpty(payload.PublicKey))
            {
                writer.WriteAttributeString("CertificateRootPublicKeyIdentifier", payload.PublicKey);
            }

            if (!String.IsNullOrEmpty(payload.Thumbprint))
            {
                writer.WriteAttributeString("CertificateRootThumbprint", payload.Thumbprint);
            }

            switch (payload.Packaging)
            {
                case PackagingType.Embedded: // this means it's in a container.
                    if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        this.core.OnMessage(WixWarnings.DownloadUrlNotSupportedForEmbeddedPayloads(payload.SourceLineNumbers, payload.Id));
                    }

                    writer.WriteAttributeString("Packaging", "embedded");
                    writer.WriteAttributeString("SourcePath", payload.EmbeddedId);

                    if (Compiler.BurnUXContainerId != payload.Container)
                    {
                        writer.WriteAttributeString("Container", payload.Container);
                    }
                    break;

                case PackagingType.External:
                    string packageId = payload.ParentPackagePayload;
                    string parentUrl = payload.ParentPackagePayload == null ? null : allPayloads[payload.ParentPackagePayload].DownloadUrl;
                    string resolvedUrl = this.ResolveUrl(payload.DownloadUrl, parentUrl, packageId, payload.Id, payload.Name);
                    if (!String.IsNullOrEmpty(resolvedUrl))
                    {
                        writer.WriteAttributeString("DownloadUrl", resolvedUrl);
                    }
                    else if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        writer.WriteAttributeString("DownloadUrl", payload.DownloadUrl);
                    }

                    writer.WriteAttributeString("Packaging", "external");
                    writer.WriteAttributeString("SourcePath", payload.Name);
                    break;
            }

            if (!String.IsNullOrEmpty(payload.CatalogId))
            {
                writer.WriteAttributeString("Catalog", payload.CatalogId);
            }
        }

        private void VerifyPayloadWithCatalog(PayloadInfoRow payloadInfo, Dictionary<string, CatalogInfo> catalogs)
        {
            bool validated = false;

            foreach (CatalogInfo catalog in catalogs.Values)
            {
                if (!validated)
                {
                    // Get the file hash
                    uint cryptHashSize = 20;
                    byte[] cryptHashBytes = new byte[cryptHashSize];
                    int error;
                    IntPtr fileHandle = IntPtr.Zero;
                    using (FileStream payloadStream = File.OpenRead(payloadInfo.FullFileName))
                    {
                        // Get the file handle
                        fileHandle = payloadStream.SafeFileHandle.DangerousGetHandle();

                        // 20 bytes is usually the hash size.  Future hashes may be bigger
                        if (!VerifyInterop.CryptCATAdminCalcHashFromFileHandle(
                            fileHandle, ref cryptHashSize, cryptHashBytes, 0))
                        {
                            error = Marshal.GetLastWin32Error();
                            if (VerifyInterop.ErrorInsufficientBuffer == error)
                            {
                                error = 0;
                                cryptHashBytes = new byte[cryptHashSize];
                                if (!VerifyInterop.CryptCATAdminCalcHashFromFileHandle(
                                    fileHandle, ref cryptHashSize, cryptHashBytes, 0))
                                {
                                    error = Marshal.GetLastWin32Error();
                                }
                            }
                            if (0 != error)
                            {
                                this.core.OnMessage(WixErrors.CatalogFileHashFailed(payloadInfo.FullFileName, error));
                            }
                        }
                    }

                    VerifyInterop.WinTrustCatalogInfo catalogData = new VerifyInterop.WinTrustCatalogInfo();
                    VerifyInterop.WinTrustData trustData = new VerifyInterop.WinTrustData();
                    try
                    {
                        // Create WINTRUST_CATALOG_INFO structure
                        catalogData.cbStruct = (uint)Marshal.SizeOf(catalogData);
                        catalogData.cbCalculatedFileHash = cryptHashSize;
                        catalogData.pbCalculatedFileHash = Marshal.AllocCoTaskMem((int)cryptHashSize);
                        Marshal.Copy(cryptHashBytes, 0, catalogData.pbCalculatedFileHash, (int)cryptHashSize);

                        StringBuilder hashString = new StringBuilder();
                        foreach (byte hashByte in cryptHashBytes)
                        {
                            hashString.Append(hashByte.ToString("X2"));
                        }
                        catalogData.pcwszMemberTag = hashString.ToString();

                        // The file names need to be lower case for older OSes
                        catalogData.pcwszMemberFilePath = payloadInfo.FullFileName.ToLowerInvariant();
                        catalogData.pcwszCatalogFilePath = catalog.FileInfo.FullName.ToLowerInvariant();

                        // Create WINTRUST_DATA structure
                        trustData.cbStruct = (uint)Marshal.SizeOf(trustData);
                        trustData.dwUIChoice = VerifyInterop.WTD_UI_NONE;
                        trustData.fdwRevocationChecks = VerifyInterop.WTD_REVOKE_NONE;
                        trustData.dwUnionChoice = VerifyInterop.WTD_CHOICE_CATALOG;
                        trustData.dwStateAction = VerifyInterop.WTD_STATEACTION_VERIFY;
                        trustData.dwProvFlags = VerifyInterop.WTD_REVOCATION_CHECK_NONE;

                        // Create the structure pointers for unmanaged
                        trustData.pCatalog = Marshal.AllocCoTaskMem(Marshal.SizeOf(catalogData));
                        Marshal.StructureToPtr(catalogData, trustData.pCatalog, false);

                        // Call WinTrustVerify to validate the file with the catalog
                        IntPtr noWindow = new IntPtr(-1);
                        Guid verifyGuid = new Guid(VerifyInterop.GenericVerify2);
                        long verifyResult = VerifyInterop.WinVerifyTrust(noWindow, ref verifyGuid, ref trustData);
                        if (0 == verifyResult)
                        {
                            validated = true;
                            payloadInfo.CatalogId = catalog.Id;
                        }
                    }
                    finally
                    {
                        // Free the structure memory
                        if (IntPtr.Zero != trustData.pCatalog)
                        {
                            Marshal.FreeCoTaskMem(trustData.pCatalog);
                        }

                        if (IntPtr.Zero != catalogData.pbCalculatedFileHash)
                        {
                            Marshal.FreeCoTaskMem(catalogData.pbCalculatedFileHash);
                        }
                    }
                }
            }

            // Error message if the file was not validated by one of the catalogs
            if (!validated)
            {
                this.core.OnMessage(WixErrors.CatalogVerificationFailed(payloadInfo.FullFileName));
            }
        }

        /// <summary>
        /// Binds a transform.
        /// </summary>
        /// <param name="transform">The transform to bind.</param>
        /// <param name="transformFile">The transform to create.</param>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        private void BindTransform(Output transform, string transformFile)
        {
            foreach (BinderExtension extension in this.extensions)
            {
                extension.Initialize(transform);
            }

            int transformFlags = 0;

            Output targetOutput = new Output(null);
            Output updatedOutput = new Output(null);

            // TODO: handle added columns

            // to generate a localized transform, both the target and updated
            // databases need to have the same code page. the only reason to
            // set different code pages is to support localized primary key
            // columns, but that would only support deleting rows. if this
            // becomes necessary, define a PreviousCodepage property on the
            // Output class and persist this throughout transform generation.
            targetOutput.Codepage = transform.Codepage;
            updatedOutput.Codepage = transform.Codepage;

            // remove certain Property rows which will be populated from summary information values
            string targetUpgradeCode = null;
            string updatedUpgradeCode = null;

            Table propertyTable = transform.Tables["Property"];
            if (null != propertyTable)
            {
                for (int i = propertyTable.Rows.Count - 1; i >= 0; i--)
                {
                    Row row = propertyTable.Rows[i];

                    if ("ProductCode" == (string)row[0] || "ProductLanguage" == (string)row[0] || "ProductVersion" == (string)row[0] || "UpgradeCode" == (string)row[0])
                    {
                        propertyTable.Rows.RemoveAt(i);

                        if ("UpgradeCode" == (string)row[0])
                        {
                            updatedUpgradeCode = (string)row[1];
                        }
                    }
                }
            }

            Table targetSummaryInfo = targetOutput.EnsureTable(this.core.TableDefinitions["_SummaryInformation"]);
            Table updatedSummaryInfo = updatedOutput.EnsureTable(this.core.TableDefinitions["_SummaryInformation"]);
            Table targetPropertyTable = targetOutput.EnsureTable(this.core.TableDefinitions["Property"]);
            Table updatedPropertyTable = updatedOutput.EnsureTable(this.core.TableDefinitions["Property"]);

            // process special summary information values
            foreach (Row row in transform.Tables["_SummaryInformation"].Rows)
            {
                if ((int)SummaryInformation.Transform.CodePage == (int)row[0])
                {
                    // convert from a web name if provided
                    string codePage = (string)row.Fields[1].Data;
                    if (null == codePage)
                    {
                        codePage = "0";
                    }
                    else
                    {
                        codePage = Common.GetValidCodePage(codePage).ToString(CultureInfo.InvariantCulture);
                    }

                    string previousCodePage = (string)row.Fields[1].PreviousData;
                    if (null == previousCodePage)
                    {
                        previousCodePage = "0";
                    }
                    else
                    {
                        previousCodePage = Common.GetValidCodePage(previousCodePage).ToString(CultureInfo.InvariantCulture);
                    }

                    Row targetCodePageRow = targetSummaryInfo.CreateRow(null);
                    targetCodePageRow[0] = 1; // PID_CODEPAGE
                    targetCodePageRow[1] = previousCodePage;

                    Row updatedCodePageRow = updatedSummaryInfo.CreateRow(null);
                    updatedCodePageRow[0] = 1; // PID_CODEPAGE
                    updatedCodePageRow[1] = codePage;
                }
                else if ((int)SummaryInformation.Transform.TargetPlatformAndLanguage == (int)row[0] ||
                         (int)SummaryInformation.Transform.UpdatedPlatformAndLanguage == (int)row[0])
                {
                    // the target language
                    string[] propertyData = ((string)row[1]).Split(';');
                    string lang = 2 == propertyData.Length ? propertyData[1] : "0";

                    Table tempSummaryInfo = (int)SummaryInformation.Transform.TargetPlatformAndLanguage == (int)row[0] ? targetSummaryInfo : updatedSummaryInfo;
                    Table tempPropertyTable = (int)SummaryInformation.Transform.TargetPlatformAndLanguage == (int)row[0] ? targetPropertyTable : updatedPropertyTable;

                    Row productLanguageRow = tempPropertyTable.CreateRow(null);
                    productLanguageRow[0] = "ProductLanguage";
                    productLanguageRow[1] = lang;

                    // set the platform;language on the MSI to be generated
                    Row templateRow = tempSummaryInfo.CreateRow(null);
                    templateRow[0] = 7; // PID_TEMPLATE
                    templateRow[1] = (string)row[1];
                }
                else if ((int)SummaryInformation.Transform.ProductCodes == (int)row[0])
                {
                    string[] propertyData = ((string)row[1]).Split(';');

                    Row targetProductCodeRow = targetPropertyTable.CreateRow(null);
                    targetProductCodeRow[0] = "ProductCode";
                    targetProductCodeRow[1] = propertyData[0].Substring(0, 38);

                    Row targetProductVersionRow = targetPropertyTable.CreateRow(null);
                    targetProductVersionRow[0] = "ProductVersion";
                    targetProductVersionRow[1] = propertyData[0].Substring(38);

                    Row updatedProductCodeRow = updatedPropertyTable.CreateRow(null);
                    updatedProductCodeRow[0] = "ProductCode";
                    updatedProductCodeRow[1] = propertyData[1].Substring(0, 38);

                    Row updatedProductVersionRow = updatedPropertyTable.CreateRow(null);
                    updatedProductVersionRow[0] = "ProductVersion";
                    updatedProductVersionRow[1] = propertyData[1].Substring(38);

                    // UpgradeCode is optional and may not exists in the target
                    // or upgraded databases, so do not include a null-valued
                    // UpgradeCode property.

                    targetUpgradeCode = propertyData[2];
                    if (!String.IsNullOrEmpty(targetUpgradeCode))
                    {
                        Row targetUpgradeCodeRow = targetPropertyTable.CreateRow(null);
                        targetUpgradeCodeRow[0] = "UpgradeCode";
                        targetUpgradeCodeRow[1] = targetUpgradeCode;

                        // If the target UpgradeCode is specified, an updated
                        // UpgradeCode is required.
                        if (String.IsNullOrEmpty(updatedUpgradeCode))
                        {
                            updatedUpgradeCode = targetUpgradeCode;
                        }
                    }

                    if (!String.IsNullOrEmpty(updatedUpgradeCode))
                    {
                        Row updatedUpgradeCodeRow = updatedPropertyTable.CreateRow(null);
                        updatedUpgradeCodeRow[0] = "UpgradeCode";
                        updatedUpgradeCodeRow[1] = updatedUpgradeCode;
                    }
                }
                else if ((int)SummaryInformation.Transform.ValidationFlags == (int)row[0])
                {
                    transformFlags = Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                }
                else if ((int)SummaryInformation.Transform.Reserved11 == (int)row[0])
                {
                    // PID_LASTPRINTED should be null for transforms
                    row.Operation = RowOperation.None;
                }
                else
                {
                    // add everything else as is
                    Row targetRow = targetSummaryInfo.CreateRow(null);
                    targetRow[0] = row[0];
                    targetRow[1] = row[1];

                    Row updatedRow = updatedSummaryInfo.CreateRow(null);
                    updatedRow[0] = row[0];
                    updatedRow[1] = row[1];
                }
            }

            // Validate that both databases have an UpgradeCode if the
            // authoring transform will validate the UpgradeCode; otherwise,
            // MsiCreateTransformSummaryinfo() will fail with 1620.
            if (((int)TransformFlags.ValidateUpgradeCode & transformFlags) != 0 &&
                (String.IsNullOrEmpty(targetUpgradeCode) || String.IsNullOrEmpty(updatedUpgradeCode)))
            {
                this.core.OnMessage(WixErrors.BothUpgradeCodesRequired());
            }

            foreach (Table table in transform.Tables)
            {
                // Ignore unreal tables when building transforms except the _Stream table.
                // These tables are ignored when generating the database so there is no reason
                // to process them here.
                if (table.Definition.Unreal && "_Streams" != table.Name)
                {
                    continue;
                }

                // process table operations
                switch (table.Operation)
                {
                    case TableOperation.Add:
                        updatedOutput.EnsureTable(table.Definition);
                        break;
                    case TableOperation.Drop:
                        targetOutput.EnsureTable(table.Definition);
                        continue;
                    default:
                        targetOutput.EnsureTable(table.Definition);
                        updatedOutput.EnsureTable(table.Definition);
                        break;
                }

                // process row operations
                foreach (Row row in table.Rows)
                {
                    switch (row.Operation)
                    {
                        case RowOperation.Add:
                            Table updatedTable = updatedOutput.EnsureTable(table.Definition);
                            updatedTable.Rows.Add(row);
                            continue;
                        case RowOperation.Delete:
                            Table targetTable = targetOutput.EnsureTable(table.Definition);
                            targetTable.Rows.Add(row);

                            // fill-in non-primary key values
                            foreach (Field field in row.Fields)
                            {
                                if (!field.Column.PrimaryKey)
                                {
                                    if (ColumnType.Number == field.Column.Type && !field.Column.IsLocalizable)
                                    {
                                        field.Data = field.Column.MinValue;
                                    }
                                    else if (ColumnType.Object == field.Column.Type)
                                    {
                                        if (null == this.emptyFile)
                                        {
                                            this.emptyFile = Path.Combine(this.fileManagerCore.TempFilesLocation, "empty");
                                            using (FileStream fileStream = File.Create(this.emptyFile))
                                            {
                                            }
                                        }

                                        field.Data = emptyFile;
                                    }
                                    else
                                    {
                                        field.Data = "0";
                                    }
                                }
                            }
                            continue;
                    }

                    // Assure that the file table's sequence is populated
                    if ("File" == table.Name)
                    {
                        foreach (Row fileRow in table.Rows)
                        {
                            if (null == fileRow[7])
                            {
                                if (RowOperation.Add == fileRow.Operation)
                                {
                                    this.core.OnMessage(WixErrors.InvalidAddedFileRowWithoutSequence(fileRow.SourceLineNumbers, (string)fileRow[0]));
                                    break;
                                }

                                // Set to 1 to prevent invalid IDT file from being generated
                                fileRow[7] = 1;
                            }
                        }
                    }

                    // process modified and unmodified rows
                    bool modifiedRow = false;
                    Row targetRow = new Row(null, table.Definition);
                    Row updatedRow = row;
                    for (int i = 0; i < row.Fields.Length; i++)
                    {
                        Field updatedField = row.Fields[i];

                        if (updatedField.Modified)
                        {
                            // set a different value in the target row to ensure this value will be modified during transform generation
                            if (ColumnType.Number == updatedField.Column.Type && !updatedField.Column.IsLocalizable)
                            {
                                if (null == updatedField.Data || 1 != (int)updatedField.Data)
                                {
                                    targetRow[i] = 1;
                                }
                                else
                                {
                                    targetRow[i] = 2;
                                }
                            }
                            else if (ColumnType.Object == updatedField.Column.Type)
                            {
                                if (null == this.emptyFile)
                                {
                                    this.emptyFile = Path.Combine(this.fileManagerCore.TempFilesLocation, "empty");
                                    using (FileStream fileStream = File.Create(emptyFile))
                                    {
                                    }
                                }

                                targetRow[i] = this.emptyFile;
                            }
                            else
                            {
                                if ("0" != (string)updatedField.Data)
                                {
                                    targetRow[i] = "0";
                                }
                                else
                                {
                                    targetRow[i] = "1";
                                }
                            }

                            modifiedRow = true;
                        }
                        else if (ColumnType.Object == updatedField.Column.Type)
                        {
                            ObjectField objectField = (ObjectField)updatedField;

                            // create an empty file for comparing against
                            if (null == objectField.PreviousData)
                            {
                                if (null == this.emptyFile)
                                {
                                    this.emptyFile = Path.Combine(this.fileManagerCore.TempFilesLocation, "empty");
                                    using (FileStream fileStream = File.Create(emptyFile))
                                    {
                                    }
                                }

                                targetRow[i] = emptyFile;
                                modifiedRow = true;
                            }
                            else if (!this.CompareFiles(objectField.PreviousData, (string)objectField.Data))
                            {
                                targetRow[i] = objectField.PreviousData;
                                modifiedRow = true;
                            }
                        }
                        else // unmodified
                        {
                            if (null != updatedField.Data)
                            {
                                targetRow[i] = updatedField.Data;
                            }
                        }
                    }

                    // modified rows and certain special rows go in the target and updated msi databases
                    if (modifiedRow ||
                        ("Property" == table.Name &&
                            ("ProductCode" == (string)row[0] ||
                            "ProductLanguage" == (string)row[0] ||
                            "ProductVersion" == (string)row[0] ||
                            "UpgradeCode" == (string)row[0])))
                    {
                        Table targetTable = targetOutput.EnsureTable(table.Definition);
                        targetTable.Rows.Add(targetRow);

                        Table updatedTable = updatedOutput.EnsureTable(table.Definition);
                        updatedTable.Rows.Add(updatedRow);
                    }
                }
            }

            foreach (BinderExtension extension in this.extensions)
            {
                extension.Finish(transform);
            }

            // Any errors encountered up to this point can cause errors during generation.
            if (this.core.EncounteredError)
            {
                return;
            }

            string transformFileName = Path.GetFileNameWithoutExtension(transformFile);
            string targetDatabaseFile = Path.Combine(this.TempFilesLocation, String.Concat(transformFileName, "_target.msi"));
            string updatedDatabaseFile = Path.Combine(this.TempFilesLocation, String.Concat(transformFileName, "_updated.msi"));

            this.SuppressAddingValidationRows = true;
            this.GenerateDatabase(targetOutput, targetDatabaseFile, false, true);
            this.GenerateDatabase(updatedOutput, updatedDatabaseFile, true, true);

            // make sure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(transformFile));

            // create the transform file
            using (Database targetDatabase = new Database(targetDatabaseFile, OpenDatabase.ReadOnly))
            {
                using (Database updatedDatabase = new Database(updatedDatabaseFile, OpenDatabase.ReadOnly))
                {
                    if (!updatedDatabase.GenerateTransform(targetDatabase, transformFile))
                    {
                        throw new WixException(WixErrors.NoDifferencesInTransform(transform.SourceLineNumbers));
                    }

                    updatedDatabase.CreateTransformSummaryInfo(targetDatabase, transformFile, (TransformErrorConditions)(transformFlags & 0xFFFF), (TransformValidations)((transformFlags >> 16) & 0xFFFF));
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
                                            FileRow fileRow = new FileRow(null, this.core.TableDefinitions["File"]);
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
                                                this.core.OnMessage(WixErrors.DuplicateModuleFileIdentifier(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, collidingFileRow.File));
                                            }
                                            else if (uniqueModuleFileIdentifiers.TryGetValue(fileRow.File, out collidingFileRow)) // case-insensitive collision with another file identifier in the same merge module
                                            {
                                                this.core.OnMessage(WixErrors.DuplicateModuleCaseInsensitiveFileIdentifier(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, fileRow.File, collidingFileRow.File));
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
                                        this.core.OnMessage(WixWarnings.InvalidHigherInstallerVersionInModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, moduleInstallerVersion, outputInstallerVersion));
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
                            this.core.OnMessage(WixErrors.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.Language));
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
        /// Set the guids for components with generatable guids.
        /// </summary>
        /// <param name="output">Internal representation of the database to operate on.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way autogenerated GUIDs are generated. Furthermore, there is no security hole here, as the strings won't need to " +
                         "make a round trip")]
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
                            this.core.OnMessage(WixErrors.IllegalComponentWithAutoGeneratedGuid(componentRow.SourceLineNumbers));
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
                                componentRow.Guid = Uuid.NewUuid(Binder.WixComponentGuidNamespace, regkey.ToLower(CultureInfo.InvariantCulture)).ToString("B").ToUpper(CultureInfo.InvariantCulture);
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
                                    string directoryPath = GetDirectoryPath(directories, componentIdGenSeeds, componentRow.Directory, true);
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
                                        this.core.OnMessage(WixErrors.IllegalPathForGeneratedComponentGuid(componentRow.SourceLineNumbers, fileRow.Component, path));
                                    }

                                    // if component has more than one file, the key path must be versioned
                                    if (1 < numFilesInComponent && String.IsNullOrEmpty(fileRow.Version))
                                    {
                                        this.core.OnMessage(WixErrors.IllegalGeneratedGuidComponentUnversionedKeypath(componentRow.SourceLineNumbers));
                                    }
                                }
                                else
                                {
                                    // not a key path, so it must be an unversioned file if component has more than one file
                                    if (1 < numFilesInComponent && !String.IsNullOrEmpty(fileRow.Version))
                                    {
                                        this.core.OnMessage(WixErrors.IllegalGeneratedGuidComponentVersionedNonkeypath(componentRow.SourceLineNumbers));
                                    }
                                }
                            }

                            // if the rules were followed, reward with a generated guid
                            if (!this.core.EncounteredError)
                            {
                                componentRow.Guid = Uuid.NewUuid(Binder.WixComponentGuidNamespace, path).ToString("B").ToUpper(CultureInfo.InvariantCulture);
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

                    Table instanceSummaryInformationTable = instanceTransform.EnsureTable(this.core.TableDefinitions["_SummaryInformation"]);
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
                    Table propertyTable = instanceTransform.EnsureTable(this.core.TableDefinitions["Property"]);

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
                            Table upgradeTable = instanceTransform.EnsureTable(this.core.TableDefinitions["Upgrade"]);
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
                        Table componentTable = instanceTransform.EnsureTable(this.core.TableDefinitions["Component"]);
                        foreach (ComponentRow targetComponentRow in instanceComponentGuids.Values)
                        {
                            string guid = targetComponentRow.Guid;
                            if (!String.IsNullOrEmpty(guid))
                            {
                                Row instanceComponentRow = componentTable.CreateRow(targetComponentRow.SourceLineNumbers);
                                instanceComponentRow.Operation = RowOperation.Modify;
                                instanceComponentRow.Fields[1].Modified = true;
                                instanceComponentRow[0] = targetComponentRow[0];
                                instanceComponentRow[1] = Uuid.NewUuid(Binder.WixComponentGuidNamespace, String.Concat(guid, instanceId)).ToString("B").ToUpper(CultureInfo.InvariantCulture);
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
                                this.core.OnMessage(WixWarnings.DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                            else
                            {
                                this.core.OnMessage(WixErrors.DuplicateComponentGuids(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                        }

                        componentGuidConditions[row.Guid] = allComponentsHaveConditions;
                    }
                }
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
                this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                return;
            }
            catch (PathTooLongException)
            {
                this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                return;
            }
            catch (NotSupportedException)
            {
                this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                return;
            }

            if (!fileInfo.Exists)
            {
                this.core.OnMessage(WixErrors.CannotFindFile(fileRow.SourceLineNumbers, fileRow.File, fileRow.FileName, fileRow.Source));
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
                        this.core.OnMessage(WixWarnings.DefaultVersionUsedForUnversionedFile(fileRow.SourceLineNumbers, fileRow.Version, fileRow.File));
                    }
                }
                else
                {
                    if (null != fileRow.Language)
                    {
                        this.core.OnMessage(WixWarnings.DefaultLanguageUsedForUnversionedFile(fileRow.SourceLineNumbers, fileRow.Language, fileRow.File));
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
                        Table msiFileHashTable = output.EnsureTable(this.core.TableDefinitions["MsiFileHash"]);
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
                    this.core.OnMessage(WixWarnings.DefaultLanguageUsedForVersionedFile(fileRow.SourceLineNumbers, fileRow.Language, fileRow.File));
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
                StringDictionary assemblyNameValues = new StringDictionary();

                CLRInterop.IReferenceIdentity referenceIdentity = null;
                Guid referenceIdentityGuid = CLRInterop.ReferenceIdentityGuid;
                uint result = CLRInterop.GetAssemblyIdentityFromFile(fileInfo.FullName, ref referenceIdentityGuid, out referenceIdentity);
                if (0 == result && null != referenceIdentity)
                {
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
                    this.core.OnMessage(WixErrors.InvalidAssemblyFile(fileRow.SourceLineNumbers, fileInfo.FullName, String.Format(CultureInfo.InvariantCulture, "HRESULT: 0x{0:x8}", result)));
                    return;
                }

                Table assemblyNameTable = output.EnsureTable(this.core.TableDefinitions["MsiAssemblyName"]);
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

                    if (!this.ExactAssemblyVersions)
                    {
                        // There is a bug in fusion that requires the assembly's "version" attribute
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
                    this.core.OnMessage(WixErrors.MissingManifestForWin32Assembly(fileRow.SourceLineNumbers, fileRow.File, fileRow.AssemblyManifest));
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
                                this.core.OnMessage(WixErrors.InvalidManifestContent(fileRow.SourceLineNumbers, fileManifestRow.Source));
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
                    this.core.OnMessage(WixErrors.FileNotFound(new SourceLineNumber(fileManifestRow.Source), fe.FileName, "AssemblyManifest"));
                }
                catch (XmlException xe)
                {
                    this.core.OnMessage(WixErrors.InvalidXml(new SourceLineNumber(fileManifestRow.Source), "manifest", xe.Message));
                }

                Table assemblyNameTable = output.EnsureTable(this.core.TableDefinitions["MsiAssemblyName"]);
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
        /// Update Control and BBControl text by reading from files when necessary.
        /// </summary>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        private void UpdateControlText(Output output)
        {
            // Control table
            Table controlTable = output.Tables["Control"];
            if (null != controlTable)
            {
                foreach (ControlRow controlRow in controlTable.Rows)
                {
                    if (null != controlRow.SourceFile)
                    {
                        controlRow.Text = this.ReadTextFile(controlRow.SourceLineNumbers, controlRow.SourceFile);
                    }
                }
            }

            // BBControl table
            Table bbcontrolTable = output.Tables["BBControl"];
            if (null != bbcontrolTable)
            {
                foreach (BBControlRow bbcontrolRow in bbcontrolTable.Rows)
                {
                    if (null != bbcontrolRow.SourceFile)
                    {
                        bbcontrolRow.Text = this.ReadTextFile(bbcontrolRow.SourceLineNumbers, bbcontrolRow.SourceFile);
                    }
                }
            }
        }

        /// <summary>
        /// Reads a text file and returns the contents.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers for row from source.</param>
        /// <param name="source">Source path to file to read.</param>
        /// <returns>Text string read from file.</returns>
        private string ReadTextFile(SourceLineNumber sourceLineNumbers, string source)
        {
            string text = null;

            try
            {
                using (StreamReader reader = new StreamReader(source))
                {
                    text = reader.ReadToEnd();
                }
            }
            catch (DirectoryNotFoundException e)
            {
                this.core.OnMessage(WixErrors.BinderFileManagerMissingFile(sourceLineNumbers, e.Message));
            }
            catch (FileNotFoundException e)
            {
                this.core.OnMessage(WixErrors.BinderFileManagerMissingFile(sourceLineNumbers, e.Message));
            }
            catch (IOException e)
            {
                this.core.OnMessage(WixErrors.BinderFileManagerMissingFile(sourceLineNumbers, e.Message));
            }
            catch (NotSupportedException)
            {
                this.core.OnMessage(WixErrors.FileNotFound(sourceLineNumbers, source));
            }

            return text;
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
                            this.core.OnMessage(WixErrors.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.Language));
                            continue;
                        }

                        this.core.OnMessage(WixVerboses.OpeningMergeModule(wixMergeRow.SourceFile, mergeLanguage));
                        merge.OpenModule(wixMergeRow.SourceFile, mergeLanguage);
                        moduleOpen = true;

                        // If there is merge configuration data, create a callback object to contain it all.
                        ConfigurationCallback callback = null;
                        if (!String.IsNullOrEmpty(wixMergeRow.ConfigurationData))
                        {
                            callback = new ConfigurationCallback(wixMergeRow.ConfigurationData);
                        }

                        // merge the module into the database that's being built
                        this.core.OnMessage(WixVerboses.MergingMergeModule(wixMergeRow.SourceFile));
                        merge.MergeEx(wixMergeRow.Feature, wixMergeRow.Directory, callback);

                        // connect any non-primary features
                        if (null != wixFeatureModulesTable)
                        {
                            foreach (Row row in wixFeatureModulesTable.Rows)
                            {
                                if (wixMergeRow.Id == (string)row[1])
                                {
                                    this.core.OnMessage(WixVerboses.ConnectingMergeModule(wixMergeRow.SourceFile, (string)row[0]));
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
                                    this.core.OnMessage(WixErrors.MergeExcludedModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, moduleKeys.ToString()));
                                    break;
                                case MsmErrorType.msmErrorFeatureRequired:
                                    this.core.OnMessage(WixErrors.MergeFeatureRequired(wixMergeRow.SourceLineNumbers, mergeError.ModuleTable, moduleKeys.ToString(), wixMergeRow.SourceFile, wixMergeRow.Id));
                                    break;
                                case MsmErrorType.msmErrorLanguageFailed:
                                    this.core.OnMessage(WixErrors.MergeLanguageFailed(wixMergeRow.SourceLineNumbers, mergeError.Language, wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorLanguageUnsupported:
                                    this.core.OnMessage(WixErrors.MergeLanguageUnsupported(wixMergeRow.SourceLineNumbers, mergeError.Language, wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorResequenceMerge:
                                    this.core.OnMessage(WixWarnings.MergeRescheduledAction(wixMergeRow.SourceLineNumbers, mergeError.DatabaseTable, databaseKeys.ToString(), wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorTableMerge:
                                    if ("_Validation" != mergeError.DatabaseTable) // ignore merge errors in the _Validation table
                                    {
                                        this.core.OnMessage(WixWarnings.MergeTableFailed(wixMergeRow.SourceLineNumbers, mergeError.DatabaseTable, databaseKeys.ToString(), wixMergeRow.SourceFile));
                                    }
                                    break;
                                case MsmErrorType.msmErrorPlatformMismatch:
                                    this.core.OnMessage(WixErrors.MergePlatformMismatch(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile));
                                    break;
                                default:
                                    this.core.OnMessage(WixErrors.UnexpectedException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_UnexpectedMergerErrorWithType, Enum.GetName(typeof(MsmErrorType), mergeError.Type), logPath), "InvalidOperationException", Environment.StackTrace));
                                    break;
                            }
                        }

                        if (0 >= mergeErrors.Count && !commit)
                        {
                            this.core.OnMessage(WixErrors.UnexpectedException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_UnexpectedMergerErrorInSourceFile, wixMergeRow.SourceFile, logPath), "InvalidOperationException", Environment.StackTrace));
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
            if (this.core.EncounteredError)
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
                                        this.core.OnMessage(WixWarnings.SuppressMergedAction((string)row[1], row[0].ToString()));
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

                                this.core.OnMessage(WixWarnings.SuppressMergedAction(resultRecord.GetString(1), tableName));
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
                this.core.OnMessage(WixVerboses.ResequencingMergeModuleFiles());
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

        /// <summary>
        /// Sets the codepage of a database.
        /// </summary>
        /// <param name="db">Database to set codepage into.</param>
        /// <param name="output">Output with the codepage for the database.</param>
        private void SetDatabaseCodepage(Database db, Output output)
        {
            // write out the _ForceCodepage IDT file
            string idtPath = Path.Combine(this.TempFilesLocation, "_ForceCodepage.idt");
            using (StreamWriter idtFile = new StreamWriter(idtPath, false, Encoding.ASCII))
            {
                idtFile.WriteLine(); // dummy column name record
                idtFile.WriteLine(); // dummy column definition record
                idtFile.Write(output.Codepage);
                idtFile.WriteLine("\t_ForceCodepage");
            }

            // try to import the table into the MSI
            try
            {
                db.Import(idtPath);
            }
            catch (WixInvalidIdtException)
            {
                // the IDT should be valid, so an invalid code page was given
                throw new WixException(WixErrors.IllegalCodepage(output.Codepage));
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
            if (this.core.EncounteredError || !fileRows.Any())
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

        /// <summary>
        /// Writes the paths to the content files included in the package to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileRows">Collection of file rows whose source will be written to file.</param>
        private void CreateContentsFile(string path, IEnumerable<FileRow> fileRows)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter contents = new StreamWriter(path, false))
            {
                foreach (FileRow fileRow in fileRows)
                {
                    contents.WriteLine(fileRow.Source);
                }
            }
        }

        /// <summary>
        /// Writes the paths to the content files included in the bundle to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="payloads">Collection of payloads whose source will be written to file.</param>
        private void CreateContentsFile(string path, IEnumerable<PayloadInfoRow> payloads)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter contents = new StreamWriter(path, false))
            {
                foreach (PayloadInfoRow payload in payloads)
                {
                    if (payload.ContentFile)
                    {
                        contents.WriteLine(payload.FullFileName);
                    }
                }
            }
        }

        /// <summary>
        /// Writes the paths to the output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        /// <param name="pdbPath">Optional path to created .wixpdb.</param>
        private void CreateOutputsFile(string path, List<FileTransfer> fileTransfers, string pdbPath)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter outputs = new StreamWriter(path, false))
            {
                foreach (FileTransfer fileTransfer in fileTransfers)
                {
                    // Don't list files where the source is the same as the destination since
                    // that might be the only place the file exists. The outputs file is often
                    // used to delete stuff and losing the original source would be bad.
                    if (!fileTransfer.Redundant)
                    {
                        outputs.WriteLine(fileTransfer.Destination);
                    }
                }

                if (!String.IsNullOrEmpty(pdbPath))
                {
                    outputs.WriteLine(Path.GetFullPath(pdbPath));
                }
            }
        }

        /// <summary>
        /// Writes the paths to the built output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        /// <param name="pdbPath">Optional path to created .wixpdb.</param>
        private void CreateBuiltOutputsFile(string path, List<FileTransfer> fileTransfers, string pdbPath)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter outputs = new StreamWriter(path, false))
            {
                foreach (FileTransfer fileTransfer in fileTransfers)
                {
                    // Only write the built file transfers. Also, skip redundant
                    // files for the same reason spelled out in this.CreateOutputsFile().
                    if (fileTransfer.Built && !fileTransfer.Redundant)
                    {
                        outputs.WriteLine(fileTransfer.Destination);
                    }
                }

                if (!String.IsNullOrEmpty(pdbPath))
                {
                    outputs.WriteLine(Path.GetFullPath(pdbPath));
                }
            }
        }

        /// <summary>
        /// Structure used to hold a row and field that contain binder variables, which need to be resolved
        /// later, once the files have been resolved.
        /// </summary>
        private struct DelayedField
        {
            /// <summary>
            /// The row containing the field.
            /// </summary>
            public Row Row;

            /// <summary>
            /// The field needing further resolving.
            /// </summary>
            public Field Field;

            /// <summary>
            /// Basic constructor for struct
            /// </summary>
            /// <param name="row">Row for the field.</param>
            /// <param name="field">Field needing further resolution.</param>
            public DelayedField(Row row, Field field)
            {
                this.Row = row;
                this.Field = field;
            }
        }

        /// <summary>
        /// Callback object for configurable merge modules.
        /// </summary>
        private sealed class ConfigurationCallback : IMsmConfigureModule
        {
            private const int SOk = 0x0;
            private const int SFalse = 0x1;
            private Hashtable configurationData;

            /// <summary>
            /// Creates a ConfigurationCallback object.
            /// </summary>
            /// <param name="configData">String to break up into name/value pairs.</param>
            public ConfigurationCallback(string configData)
            {
                if (String.IsNullOrEmpty(configData))
                {
                    throw new ArgumentNullException("configData");
                }

                string[] pairs = configData.Split(',');
                this.configurationData = new Hashtable(pairs.Length);
                for (int i = 0; i < pairs.Length; ++i)
                {
                    string[] nameVal = pairs[i].Split('=');
                    string name = nameVal[0];
                    string value = nameVal[1];

                    name = name.Replace("%2C", ",");
                    name = name.Replace("%3D", "=");
                    name = name.Replace("%25", "%");

                    value = value.Replace("%2C", ",");
                    value = value.Replace("%3D", "=");
                    value = value.Replace("%25", "%");

                    this.configurationData[name] = value;
                }
            }

            /// <summary>
            /// Returns text data based on name.
            /// </summary>
            /// <param name="name">Name of value to return.</param>
            /// <param name="configData">Out param to put configuration data into.</param>
            /// <returns>S_OK if value provided, S_FALSE if not.</returns>
            public int ProvideTextData(string name, out string configData)
            {
                if (this.configurationData.Contains(name))
                {
                    configData = (string)this.configurationData[name];
                    return SOk;
                }
                else
                {
                    configData = null;
                    return SFalse;
                }
            }

            /// <summary>
            /// Returns integer data based on name.
            /// </summary>
            /// <param name="name">Name of value to return.</param>
            /// <param name="configData">Out param to put configuration data into.</param>
            /// <returns>S_OK if value provided, S_FALSE if not.</returns>
            public int ProvideIntegerData(string name, out int configData)
            {
                if (this.configurationData.Contains(name))
                {
                    string val = (string)this.configurationData[name];
                    configData = Convert.ToInt32(val, CultureInfo.InvariantCulture);
                    return SOk;
                }
                else
                {
                    configData = 0;
                    return SFalse;
                }
            }
        }


        /// <summary>
        /// The types that the WixPatchSymbolPaths table can hold (and that the WixPatchSymbolPathsComparer can sort).
        /// </summary>
        internal enum SymbolPathType
        {
            File,
            Component,
            Directory,
            Media,
            Product
        };

        /// <summary>
        /// Sorts the WixPatchSymbolPaths table for processing.
        /// </summary>
        internal sealed class WixPatchSymbolPathsComparer : IComparer<Row>
        {
            /// <summary>
            /// Compares two rows from the WixPatchSymbolPaths table.
            /// </summary>
            /// <param name="a">First row to compare.</param>
            /// <param name="b">Second row to compare.</param>
            /// <remarks>Only the File, Product, Component, Directory, and Media tables links are allowed by this method.</remarks>
            /// <returns>Less than zero if a is less than b; Zero if they are equal, and Greater than zero if a is greater than b</returns>
            //public int Compare(Object a, Object b)
            //{
            //    Row ra = (Row)a;
            //    Row rb = (Row)b;

            //    SymbolPathType ia = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), ((Field)ra.Fields[0]).Data.ToString());
            //    SymbolPathType ib = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), ((Field)rb.Fields[0]).Data.ToString());
            //    return (int)ib - (int)ia;
            //}

            public int Compare(Row x, Row y)
            {
                SymbolPathType ix = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), x.Fields[0].ToString());
                SymbolPathType iy = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), y.Fields[0].ToString());
                return (int)iy - (int)ix;
            }
        }

        #region DependencyExtension
        /// <summary>
        /// Imports authored dependency providers for each package in the manifest,
        /// and generates dependency providers for certain package types that do not
        /// have a provider defined.
        /// </summary>
        /// <param name="bundle">The <see cref="Output"/> object for the bundle.</param>
        /// <param name="packages">An indexed collection of chained packages.</param>
        private void ProcessDependencyProviders(Output bundle, Dictionary<string, ChainPackageInfo> packages)
        {
            // First import any authored dependencies. These may merge with imported provides from MSI packages.
            Table wixDependencyProviderTable = bundle.Tables["WixDependencyProvider"];
            if (null != wixDependencyProviderTable && 0 < wixDependencyProviderTable.Rows.Count)
            {
                // Add package information for each dependency provider authored into the manifest.
                foreach (Row wixDependencyProviderRow in wixDependencyProviderTable.Rows)
                {
                    string packageId = (string)wixDependencyProviderRow[1];

                    ChainPackageInfo package = null;
                    if (packages.TryGetValue(packageId, out package))
                    {
                        ProvidesDependency dependency = new ProvidesDependency(wixDependencyProviderRow);

                        if (String.IsNullOrEmpty(dependency.Key))
                        {
                            switch (package.ChainPackageType)
                            {
                                // The WixDependencyExtension allows an empty Key for MSIs and MSPs.
                                case Compiler.ChainPackageType.Msi:
                                    dependency.Key = package.ProductCode;
                                    break;
                                case Compiler.ChainPackageType.Msp:
                                    dependency.Key = package.PatchCode;
                                    break;
                            }
                        }

                        if (String.IsNullOrEmpty(dependency.Version))
                        {
                            dependency.Version = package.Version;
                        }

                        // If the version is still missing, a version could not be harvested from the package and was not authored.
                        if (String.IsNullOrEmpty(dependency.Version))
                        {
                            this.core.OnMessage(WixErrors.MissingDependencyVersion(package.Id));
                        }

                        if (String.IsNullOrEmpty(dependency.DisplayName))
                        {
                            dependency.DisplayName = package.DisplayName;
                        }

                        if (!package.Provides.Merge(dependency))
                        {
                            this.core.OnMessage(WixErrors.DuplicateProviderDependencyKey(dependency.Key, package.Id));
                        }
                    }
                }
            }

            // Generate providers for MSI packages that still do not have providers.
            foreach (ChainPackageInfo package in packages.Values)
            {
                if (Compiler.ChainPackageType.Msi == package.ChainPackageType && 0 == package.Provides.Count)
                {
                    ProvidesDependency dependency = new ProvidesDependency(package.ProductCode, package.Version, package.DisplayName, 0);

                    if (!package.Provides.Merge(dependency))
                    {
                        this.core.OnMessage(WixErrors.DuplicateProviderDependencyKey(dependency.Key, package.Id));
                    }
                }
                else if (Compiler.ChainPackageType.Msp == package.ChainPackageType && 0 == package.Provides.Count)
                {
                    ProvidesDependency dependency = new ProvidesDependency(package.PatchCode, package.Version, package.DisplayName, 0);

                    if (!package.Provides.Merge(dependency))
                    {
                        this.core.OnMessage(WixErrors.DuplicateProviderDependencyKey(dependency.Key, package.Id));
                    }
                }
            }
        }

        /// <summary>
        /// Sets the provider key for the bundle.
        /// </summary>
        /// <param name="bundle">The <see cref="Output"/> object for the bundle.</param>
        /// <param name="bundleInfo">The <see cref="BundleInfo"/> containing the provider key and other information for the bundle.</param>
        private void SetBundleProviderKey(Output bundle, WixBundleRow bundleInfo)
        {
            // From DependencyCommon.cs in the WixDependencyExtension.
            const int ProvidesAttributesBundle = 0x10000;

            Table wixDependencyProviderTable = bundle.Tables["WixDependencyProvider"];
            if (null != wixDependencyProviderTable && 0 < wixDependencyProviderTable.Rows.Count)
            {
                // Search the WixDependencyProvider table for the single bundle provider key.
                foreach (Row wixDependencyProviderRow in wixDependencyProviderTable.Rows)
                {
                    object attributes = wixDependencyProviderRow[5];
                    if (null != attributes && 0 != (ProvidesAttributesBundle & (int)attributes))
                    {
                        bundleInfo.ProviderKey = (string)wixDependencyProviderRow[2];
                        break;
                    }
                }
            }

            // Defaults to the bundle ID as the provider key.
        }
        #endregion
    }
}
