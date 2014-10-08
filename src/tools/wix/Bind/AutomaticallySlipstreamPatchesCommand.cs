//-------------------------------------------------------------------------------------------------
// <copyright file="AutomaticallySlipstreamPatchesCommand.cs" company="Outercurve Foundation">
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
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class AutomaticallySlipstreamPatchesCommand : ICommand
    {
        public IEnumerable<ChainPackageFacade> Packages { private get; set;}

        public Table WixBundlePatchTargetCodeTable {private get; set;}

        public Table SlipstreamMspTable { private get; set; }

        public void Execute()
        {
            List<ChainMsiPackageRow> msiPackages = new List<ChainMsiPackageRow>();
            Dictionary<string, List<WixBundlePatchTargetCodeRow>> targetsProductCode = new Dictionary<string, List<WixBundlePatchTargetCodeRow>>();
            Dictionary<string, List<WixBundlePatchTargetCodeRow>> targetsUpgradeCode = new Dictionary<string, List<WixBundlePatchTargetCodeRow>>();

            foreach (ChainPackageFacade package in Packages)
            {
                if (ChainPackageType.Msi == package.Package.Type)
                {
                    // Keep track of all MSI packages.
                    msiPackages.Add(package.MsiPackage);
                }
                else if (ChainPackageType.Msp == package.Package.Type && package.MspPackage.Slipstream)
                {
                    IEnumerable<WixBundlePatchTargetCodeRow> patchTargetCodeRows = WixBundlePatchTargetCodeTable.RowsAs<WixBundlePatchTargetCodeRow>().Where(r => r.MspPackageId == package.Package.WixChainItemId);

                    // Index target ProductCodes and UpgradeCodes for slipstreamed MSPs.
                    foreach (WixBundlePatchTargetCodeRow row in patchTargetCodeRows)
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

            RowIndexedList<Row> slipstreamMspRows = new RowIndexedList<Row>(SlipstreamMspTable);

            // Loop through the MSI and slipstream patches targeting it.
            foreach (ChainMsiPackageRow msi in msiPackages)
            {
                List<WixBundlePatchTargetCodeRow> rows;
                if (targetsProductCode.TryGetValue(msi.ProductCode, out rows))
                {
                    foreach (WixBundlePatchTargetCodeRow row in rows)
                    {
                        Debug.Assert(row.TargetsProductCode);
                        Debug.Assert(!row.TargetsUpgradeCode);

                        Row slipstreamMspRow = SlipstreamMspTable.CreateRow(row.SourceLineNumbers, false);
                        slipstreamMspRow[0] = msi.ChainPackageId;
                        slipstreamMspRow[1] = row.MspPackageId;

                        if (slipstreamMspRows.TryAdd(slipstreamMspRow))
                        {
                            SlipstreamMspTable.Rows.Add(slipstreamMspRow);
                        }
                    }

                    rows = null;
                }

                if (!String.IsNullOrEmpty(msi.UpgradeCode) && targetsUpgradeCode.TryGetValue(msi.UpgradeCode, out rows))
                {
                    foreach (WixBundlePatchTargetCodeRow row in rows)
                    {
                        Debug.Assert(!row.TargetsProductCode);
                        Debug.Assert(row.TargetsUpgradeCode);

                        Row slipstreamMspRow = SlipstreamMspTable.CreateRow(row.SourceLineNumbers, false);
                        slipstreamMspRow[0] = msi.ChainPackageId;
                        slipstreamMspRow[1] = row.MspPackageId;

                        if (slipstreamMspRows.TryAdd(slipstreamMspRow))
                        {
                            SlipstreamMspTable.Rows.Add(slipstreamMspRow);
                        }
                    }

                    rows = null;
                }
            }
        }
    }
}
