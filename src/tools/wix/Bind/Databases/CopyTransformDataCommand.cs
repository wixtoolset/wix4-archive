﻿//-------------------------------------------------------------------------------------------------
// <copyright file="CopyTransformDataCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind.Databases
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;
    using WixToolset.Msi.Interop;

    internal class CopyTransformDataCommand : ICommand
    {
        public bool CopyOutFileRows { private get; set; }

        public BinderFileManagerCore FileManagerCore { private get; set; }

        public IEnumerable<IBinderFileManager> FileManagers { private get; set; }

        public Output Output { private get; set; }

        public TableDefinitionCollection TableDefinitions { private get; set; }

        public IEnumerable<FileFacade> FileFacades { get; private set; }

        public void Execute()
        {
            Debug.Assert(OutputType.Patch != this.Output.Type);

            List<FileFacade> allFileRows = this.CopyOutFileRows ? new List<FileFacade>() : null;

#if false // TODO: Fix this patching related code to work correctly with FileFacades.
            bool copyToPatch = (allFileRows != null);
            bool copyFromPatch = !copyToPatch;

            RowDictionary<MediaRow> patchMediaRows = new RowDictionary<MediaRow>();

            Dictionary<int, RowDictionary<WixFileRow>> patchMediaFileRows = new Dictionary<int, RowDictionary<WixFileRow>>();

            Table patchActualFileTable = this.Output.EnsureTable(this.TableDefinitions["File"]);
            Table patchFileTable = this.Output.EnsureTable(this.TableDefinitions["WixFile"]);

            if (copyFromPatch)
            {
                // index patch files by diskId+fileId
                foreach (WixFileRow patchFileRow in patchFileTable.Rows)
                {
                    int diskId = patchFileRow.DiskId;
                    RowDictionary<WixFileRow> mediaFileRows;
                    if (!patchMediaFileRows.TryGetValue(diskId, out mediaFileRows))
                    {
                        mediaFileRows = new RowDictionary<WixFileRow>();
                        patchMediaFileRows.Add(diskId, mediaFileRows);
                    }

                    mediaFileRows.Add(patchFileRow);
                }

                Table patchMediaTable = this.Output.EnsureTable(this.TableDefinitions["Media"]);
                patchMediaRows = new RowDictionary<MediaRow>(patchMediaTable);
            }

            // index paired transforms
            Dictionary<string, Output> pairedTransforms = new Dictionary<string, Output>();
            foreach (SubStorage substorage in this.Output.SubStorages)
            {
                if (substorage.Name.StartsWith("#"))
                {
                    pairedTransforms.Add(substorage.Name.Substring(1), substorage.Data);
                }
            }

            try
            {
                // copy File bind data into substorages
                foreach (SubStorage substorage in this.Output.SubStorages)
                {
                    if (substorage.Name.StartsWith("#"))
                    {
                        // no changes necessary for paired transforms
                        continue;
                    }

                    Output mainTransform = substorage.Data;
                    Table mainWixFileTable = mainTransform.Tables["WixFile"];
                    Table mainMsiFileHashTable = mainTransform.Tables["MsiFileHash"];

                    this.FileManagerCore.ActiveSubStorage = substorage;

                    RowDictionary<WixFileRow> mainWixFiles = new RowDictionary<WixFileRow>(mainWixFileTable);
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
                            if (RowOperation.Delete == mainFileRow.Operation)
                            {
                                continue;
                            }
                            else if (RowOperation.None == mainFileRow.Operation && !copyToPatch)
                            {
                                continue;
                            }

                            WixFileRow mainWixFileRow = mainWixFiles.Get(mainFileRow.File);

                            if (copyToPatch) // when copying to the patch, we need compare the underlying files and include all file changes.
                            {
                                ObjectField objectField = (ObjectField)mainWixFileRow.Fields[6];
                                FileRow pairedFileRow = pairedFileRows.Get(mainFileRow.File);

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
                                        if ((0 == (PatchAttributeType.Ignore & mainWixFileRow.PatchAttributes)) &&
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
                            int diskId = mainWixFileRow.DiskId;

                            RowDictionary<WixFileRow> mediaFileRows;
                            if (!patchMediaFileRows.TryGetValue(diskId, out mediaFileRows))
                            {
                                mediaFileRows = new RowDictionary<WixFileRow>();
                                patchMediaFileRows.Add(diskId, mediaFileRows);
                            }

                            string fileId = mainFileRow.File;
                            WixFileRow patchFileRow = mediaFileRows.Get(fileId);
                            if (copyToPatch)
                            {
                                if (null == patchFileRow)
                                {
                                    FileRow patchActualFileRow = (FileRow)patchFileTable.CreateRow(mainFileRow.SourceLineNumbers);
                                    patchActualFileRow.CopyFrom(mainFileRow);

                                    patchFileRow = (WixFileRow)patchFileTable.CreateRow(mainFileRow.SourceLineNumbers);
                                    patchFileRow.CopyFrom(mainWixFileRow);

                                    mediaFileRows.Add(patchFileRow);

                                    allFileRows.Add(new FileFacade(patchActualFileRow, patchFileRow, null)); // TODO: should we be passing along delta information? Probably, right?
                                }
                                else
                                {
                                    // TODO: confirm the rest of data is identical?

                                    // make sure Source is same. Otherwise we are silently ignoring a file.
                                    if (0 != String.Compare(patchFileRow.Source, mainWixFileRow.Source, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Messaging.Instance.OnMessage(WixErrors.SameFileIdDifferentSource(mainFileRow.SourceLineNumbers, fileId, patchFileRow.Source, mainWixFileRow.Source));
                                    }

                                    // capture the previous file versions (and associated data) from this targeted instance of the baseline into the current filerow.
                                    patchFileRow.AppendPreviousDataFrom(mainWixFileRow);
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
                                        patchHashRow = patchFileRow.Hash;
                                    }

                                    if (null != patchHashRow)
                                    {
                                        Table mainHashTable = mainTransform.EnsureTable(this.TableDefinitions["MsiFileHash"]);
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
                                    List<Row> patchAssemblyNameRows = patchFileRow.AssemblyNames;
                                    if (null != patchAssemblyNameRows)
                                    {
                                        Table mainAssemblyNameTable = mainTransform.EnsureTable(this.TableDefinitions["MsiAssemblyName"]);
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
                                        Table patchTable = pairedTransform.EnsureTable(this.TableDefinitions["Patch"]);
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
                                            streamName = "_" + Guid.NewGuid().ToString("D").ToUpperInvariant().Replace('-', '_');
                                            Table patchHeadersTable = pairedTransform.EnsureTable(this.TableDefinitions["MsiPatchHeaders"]);
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
                        this.Output.Tables.Remove("Media");
                        this.Output.Tables.Remove("File");
                        this.Output.Tables.Remove("MsiFileHash");
                        this.Output.Tables.Remove("MsiAssemblyName");
                    }
                }
            }
            finally
            {
                this.FileManagerCore.ActiveSubStorage = null;
            }
#endif
            this.FileFacades = allFileRows;
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
                Table iesTable = pairedTransform.EnsureTable(this.TableDefinitions[tableName]);
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
                    Messaging.Instance.OnMessage(WixWarnings.UpdateOfNonKeyPathFile((string)componentFile.Value, (string)componentFile.Key, (string)componentKeyPath[componentFile.Key]));
                }
            }
        }

        private bool CompareFiles(string targetFile, string updatedFile)
        {
            bool? compared = null;
            foreach (IBinderFileManager fileManager in this.FileManagers)
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
    }
}
