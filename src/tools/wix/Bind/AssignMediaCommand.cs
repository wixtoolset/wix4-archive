//-------------------------------------------------------------------------------------------------
// <copyright file="AssignMediaCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Cab;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// AssignMediaCommand assigns files to cabs based on Media or MediaTemplate rows.
    /// </summary>
    public class AssignMediaCommand : ICommand
    {
        public AssignMediaCommand()
        {
            this.CabinetNameTemplate = "Cab{0}.cab";
        }

        public Output Output { private get; set; }

        public bool FilesCompressed { private get; set; }

        public string CabinetNameTemplate { private get; set; }

        public IEnumerable<FileRow> FileRows { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        /// <summary>
        /// Gets cabinets with their file rows.
        /// </summary>
        public Dictionary<MediaRow, IEnumerable<FileRow>> FileRowsByCabinetMedia { get; private set; }

        /// <summary>
        /// Get media rows.
        /// </summary>
        public RowDictionary<MediaRow> MediaRows { get; private set; }

        /// <summary>
        /// Get uncompressed file rows. This will contain file rows of File elements that are marked with compression=no.
        /// This contains all the files when Package element is marked with compression=no
        /// </summary>
        public IEnumerable<FileRow> UncompressedFileRows { get; private set; }

        public void Execute()
        {
            Dictionary<MediaRow, List<FileRow>> filesByCabinetMedia = new Dictionary<MediaRow, List<FileRow>>();

            RowDictionary<MediaRow> mediaRows = new RowDictionary<MediaRow>();

            List<FileRow> uncompressedFiles = new List<FileRow>();

            MediaRow mergeModuleMediaRow = null;
            Table mediaTable = this.Output.Tables["Media"];
            Table mediaTemplateTable = this.Output.Tables["WixMediaTemplate"];

            // If both tables are authored, it is an error.
            if ((mediaTemplateTable != null && mediaTemplateTable.Rows.Count > 0) && (mediaTable != null && mediaTable.Rows.Count > 1))
            {
                throw new WixException(WixErrors.MediaTableCollision(null));
            }

            // When building merge module, all the files go to "#MergeModule.CABinet".
            if (OutputType.Module == this.Output.Type)
            {
                Table mergeModuleMediaTable = new Table(null, this.TableDefinitions["Media"]);
                mergeModuleMediaRow = (MediaRow)mergeModuleMediaTable.CreateRow(null);
                mergeModuleMediaRow.Cabinet = "#MergeModule.CABinet";

                filesByCabinetMedia.Add(mergeModuleMediaRow, new List<FileRow>());
            }

            if (OutputType.Module == this.Output.Type || null == mediaTemplateTable)
            {
                this.ManuallyAssignFiles(mediaTable, mergeModuleMediaRow, this.FileRows, filesByCabinetMedia, mediaRows, uncompressedFiles);
            }
            else
            {
                this.AutoAssignFiles(mediaTable, this.FileRows, filesByCabinetMedia, mediaRows, uncompressedFiles);
            }

            this.FileRowsByCabinetMedia = new Dictionary<MediaRow, IEnumerable<FileRow>>();

            foreach (var mediaRowWithFileRows in filesByCabinetMedia)
            {
                this.FileRowsByCabinetMedia.Add(mediaRowWithFileRows.Key, mediaRowWithFileRows.Value);
            }

            this.MediaRows = mediaRows;

            this.UncompressedFileRows = uncompressedFiles;
        }

        /// <summary>
        /// Assign files to cabinets based on MediaTemplate authoring.
        /// </summary>
        /// <param name="fileRows">FileRowCollection</param>
        private void AutoAssignFiles(Table mediaTable, IEnumerable<FileRow> fileRows, Dictionary<MediaRow, List<FileRow>> filesByCabinetMedia, RowDictionary<MediaRow> mediaRows, List<FileRow> uncompressedFiles)
        {
            const int MaxCabIndex = 999;

            ulong currentPreCabSize = 0;
            ulong maxPreCabSizeInBytes;
            int maxPreCabSizeInMB = 0;
            int currentCabIndex = 0;

            MediaRow currentMediaRow = null;

            Table mediaTemplateTable = this.Output.Tables["WixMediaTemplate"];

            // Auto assign files to cabinets based on maximum uncompressed media size
            mediaTable.Rows.Clear();
            WixMediaTemplateRow mediaTemplateRow = (WixMediaTemplateRow)mediaTemplateTable.Rows[0];

            if (!String.IsNullOrEmpty(mediaTemplateRow.CabinetTemplate))
            {
                this.CabinetNameTemplate = mediaTemplateRow.CabinetTemplate;
            }

            string mumsString = Environment.GetEnvironmentVariable("WIX_MUMS");

            try
            {
                // Override authored mums value if environment variable is authored.
                if (!String.IsNullOrEmpty(mumsString))
                {
                    maxPreCabSizeInMB = Int32.Parse(mumsString);
                }
                else
                {
                    maxPreCabSizeInMB = mediaTemplateRow.MaximumUncompressedMediaSize;
                }

                maxPreCabSizeInBytes = (ulong)maxPreCabSizeInMB * 1024 * 1024;
            }
            catch (FormatException)
            {
                throw new WixException(WixErrors.IllegalEnvironmentVariable("WIX_MUMS", mumsString));
            }
            catch (OverflowException)
            {
                throw new WixException(WixErrors.MaximumUncompressedMediaSizeTooLarge(null, maxPreCabSizeInMB));
            }

            foreach (FileRow fileRow in this.FileRows)
            {
                // When building a product, if the current file is not to be compressed or if 
                // the package set not to be compressed, don't cab it.
                if (OutputType.Product == this.Output.Type &&
                    (YesNoType.No == fileRow.Compressed ||
                    (YesNoType.NotSet == fileRow.Compressed && !this.FilesCompressed)))
                {
                    uncompressedFiles.Add(fileRow);
                    continue;
                }

                FileInfo fileInfo = null;

                // Get the file size
                try
                {
                    fileInfo = new FileInfo(fileRow.Source);
                }
                catch (ArgumentException)
                {
                    Messaging.Instance.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                }
                catch (PathTooLongException)
                {
                    Messaging.Instance.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                }
                catch (NotSupportedException)
                {
                    Messaging.Instance.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                }

                if (fileInfo.Exists)
                {
                    if (fileInfo.Length > Int32.MaxValue)
                    {
                        throw new WixException(WixErrors.FileTooLarge(fileRow.SourceLineNumbers, fileRow.Source));
                    }

                    fileRow.FileSize = Convert.ToInt32(fileInfo.Length, CultureInfo.InvariantCulture);
                }

                if (currentCabIndex == MaxCabIndex)
                {
                    // Associate current file with last cab (irrespective of the size) and cab index is not incremented anymore.
                    List<FileRow> cabinetFileRows = filesByCabinetMedia[currentMediaRow];
                    fileRow.DiskId = currentCabIndex;
                    cabinetFileRows.Add(fileRow);
                    continue;
                }

                // Update current cab size.
                currentPreCabSize += (ulong)fileRow.FileSize;

                if (currentPreCabSize > maxPreCabSizeInBytes)
                {
                    // Overflow due to current file
                    currentMediaRow = this.AddMediaRow(mediaTable, ++currentCabIndex, mediaTemplateRow.CompressionLevel);
                    mediaRows.Add(currentMediaRow);
                    filesByCabinetMedia.Add(currentMediaRow, new List<FileRow>());

                    List<FileRow> cabinetFileRows = filesByCabinetMedia[currentMediaRow];
                    fileRow.DiskId = currentCabIndex;
                    cabinetFileRows.Add(fileRow);
                    // Now files larger than MaxUncompressedMediaSize will be the only file in its cabinet so as to respect MaxUncompressedMediaSize
                    currentPreCabSize = (ulong)fileRow.FileSize;
                }
                else
                {
                    // File fits in the current cab.
                    if (currentMediaRow == null)
                    {
                        // Create new cab and MediaRow
                        currentMediaRow = this.AddMediaRow(mediaTable, ++currentCabIndex, mediaTemplateRow.CompressionLevel);
                        mediaRows.Add(currentMediaRow);
                        filesByCabinetMedia.Add(currentMediaRow, new List<FileRow>());
                    }

                    // Associate current file with current cab.
                    List<FileRow> cabinetFileRows = filesByCabinetMedia[currentMediaRow];
                    fileRow.DiskId = currentCabIndex;
                    cabinetFileRows.Add(fileRow);
                }
            }

            // If there are uncompressed files and no MediaRow, create a default one.
            if (uncompressedFiles.Count > 0 && mediaTable.Rows.Count == 0)
            {
                MediaRow defaultMediaRow = (MediaRow)mediaTable.CreateRow(null);
                defaultMediaRow.DiskId = 1;
                mediaRows.Add(defaultMediaRow);
            }
        }

        /// <summary>
        /// Assign files to cabinets based on Media authoring.
        /// </summary>
        /// <param name="mediaTable"></param>
        /// <param name="mergeModuleMediaRow"></param>
        /// <param name="fileRows"></param>
        private void ManuallyAssignFiles(Table mediaTable, MediaRow mergeModuleMediaRow, IEnumerable<FileRow> fileRows, Dictionary<MediaRow, List<FileRow>> filesByCabinetMedia, RowDictionary<MediaRow> mediaRows, List<FileRow> uncompressedFiles)
        {
            if (OutputType.Module != this.Output.Type)
            {
                if (null != mediaTable)
                {
                    Dictionary<string, MediaRow> cabinetMediaRows = new Dictionary<string, MediaRow>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (MediaRow mediaRow in mediaTable.Rows)
                    {
                        // If the Media row has a cabinet, make sure it is unique across all Media rows.
                        if (!String.IsNullOrEmpty(mediaRow.Cabinet))
                        {
                            MediaRow existingRow;
                            if (cabinetMediaRows.TryGetValue(mediaRow.Cabinet, out existingRow))
                            {
                                Messaging.Instance.OnMessage(WixErrors.DuplicateCabinetName(mediaRow.SourceLineNumbers, mediaRow.Cabinet));
                                Messaging.Instance.OnMessage(WixErrors.DuplicateCabinetName2(existingRow.SourceLineNumbers, existingRow.Cabinet));
                            }
                            else
                            {
                                cabinetMediaRows.Add(mediaRow.Cabinet, mediaRow);
                            }
                        }

                        mediaRows.Add(mediaRow);
                    }
                }

                foreach (MediaRow mediaRow in mediaRows.Values)
                {
                    if (null != mediaRow.Cabinet)
                    {
                        filesByCabinetMedia.Add(mediaRow, new List<FileRow>());
                    }
                }
            }

            foreach (FileRow fileRow in fileRows)
            {
                if (OutputType.Module == this.Output.Type)
                {
                    filesByCabinetMedia[mergeModuleMediaRow].Add(fileRow);
                }
                else
                {
                    MediaRow mediaRow;
                    if (!mediaRows.TryGetValue(fileRow.DiskId.ToString(), out mediaRow))
                    {
                        Messaging.Instance.OnMessage(WixErrors.MissingMedia(fileRow.SourceLineNumbers, fileRow.DiskId));
                        continue;
                    }

                    // When building a product, if the current file is not to be compressed or if
                    // the package set not to be compressed, don't cab it.
                    if (OutputType.Product == this.Output.Type &&
                        (YesNoType.No == fileRow.Compressed ||
                        (YesNoType.NotSet == fileRow.Compressed && !this.FilesCompressed)))
                    {
                        uncompressedFiles.Add(fileRow);
                    }
                    else // file in a Module or marked compressed
                    {
                        List<FileRow> cabinetFileRows;
                        if (filesByCabinetMedia.TryGetValue(mediaRow, out cabinetFileRows))
                        {
                            cabinetFileRows.Add(fileRow);
                        }
                        else
                        {
                            Messaging.Instance.OnMessage(WixErrors.ExpectedMediaCabinet(fileRow.SourceLineNumbers, fileRow.File, fileRow.DiskId));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a row to the media table with cab name template filled in.
        /// </summary>
        /// <param name="mediaTable"></param>
        /// <param name="cabIndex"></param>
        /// <returns></returns>
        private MediaRow AddMediaRow(Table mediaTable, int cabIndex, string compressionLevel)
        {
            MediaRow currentMediaRow = (MediaRow)mediaTable.CreateRow(null);
            currentMediaRow.DiskId = cabIndex;
            currentMediaRow.Cabinet = String.Format(this.CabinetNameTemplate, cabIndex);

            if (!String.IsNullOrEmpty(compressionLevel))
            {
                currentMediaRow.CompressionLevel = WixCreateCab.CompressionLevelFromString(compressionLevel);
            }

            Table wixMediaTable = this.Output.EnsureTable(this.TableDefinitions["WixMedia"]);
            Row row = wixMediaTable.CreateRow(null);
            row[0] = cabIndex;
            row[1] = compressionLevel;

            return currentMediaRow;
        }
    }
}
