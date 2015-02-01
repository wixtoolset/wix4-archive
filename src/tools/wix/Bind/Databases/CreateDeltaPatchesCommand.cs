//-------------------------------------------------------------------------------------------------
// <copyright file="CreateDeltaPatchesCommand.cs" company="Outercurve Foundation">
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
    using System.Globalization;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Creates delta patches and updates the appropriate rows to point to the newly generated patches.
    /// </summary>
    internal class CreateDeltaPatchesCommand : ICommand
    {
        public IEnumerable<FileFacade> FileFacades { private get; set; }

        public ITable WixPatchIdTable { private get; set; }

        public string TempFilesLocation { private get; set; }

        public void Execute()
        {
            bool optimizePatchSizeForLargeFiles = false;
            PatchAPI.PatchInterop.PatchSymbolFlagsType apiPatchingSymbolFlags = 0;

            if (null != this.WixPatchIdTable)
            {
                Row row = this.WixPatchIdTable.Rows[0];
                if (null != row)
                {
                    if (null != row[2])
                    {
                        optimizePatchSizeForLargeFiles = (1 == Convert.ToUInt32(row[2], CultureInfo.InvariantCulture));
                    }

                    if (null != row[3])
                    {
                        apiPatchingSymbolFlags = (PatchAPI.PatchInterop.PatchSymbolFlagsType)Convert.ToUInt32(row[3], CultureInfo.InvariantCulture);
                    }
                }
            }

            foreach (FileFacade facade in this.FileFacades)
            {
                if (RowOperation.Modify == facade.File.Operation &&
                    0 != (facade.WixFile.PatchAttributes & PatchAttributeType.IncludeWholeFile))
                {
                    string deltaBase = String.Concat("delta_", facade.File.File);
                    string deltaFile = Path.Combine(this.TempFilesLocation, String.Concat(deltaBase, ".dpf"));
                    string headerFile = Path.Combine(this.TempFilesLocation, String.Concat(deltaBase, ".phd"));

                    bool retainRangeWarning = false;

                    if (PatchAPI.PatchInterop.CreateDelta(
                            deltaFile,
                            facade.WixFile.Source,
                            facade.DeltaPatchFile.Symbols,
                            facade.DeltaPatchFile.RetainOffsets,
                            new[] { facade.WixFile.PreviousSource },
                            facade.DeltaPatchFile.PreviousSymbols.Split(new[] { ';' }),
                            facade.DeltaPatchFile.PreviousIgnoreLengths.Split(new[] { ';' }),
                            facade.DeltaPatchFile.PreviousIgnoreOffsets.Split(new[] { ';' }),
                            facade.DeltaPatchFile.PreviousRetainLengths.Split(new[] { ';' }),
                            facade.DeltaPatchFile.PreviousRetainOffsets.Split(new[] { ';' }),
                            apiPatchingSymbolFlags,
                            optimizePatchSizeForLargeFiles,
                            out retainRangeWarning))
                    {
                        PatchAPI.PatchInterop.ExtractDeltaHeader(deltaFile, headerFile);

                        facade.WixFile.Source = deltaFile;
                        facade.WixFile.DeltaPatchHeaderSource = headerFile;
                    }

                    if (retainRangeWarning)
                    {
                        // TODO: get patch family to add to warning message for PatchWiz parity.
                        Messaging.Instance.OnMessage(WixWarnings.RetainRangeMismatch(facade.File.SourceLineNumbers, facade.File.File));
                    }
                }
            }
        }
    }
}
