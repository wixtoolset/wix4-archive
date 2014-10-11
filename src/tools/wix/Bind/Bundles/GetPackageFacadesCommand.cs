//-------------------------------------------------------------------------------------------------
// <copyright file="GetPackageFacadesCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind.Bundles
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class GetPackageFacadesCommand : ICommand
    {
        public Table PackageTable { private get; set; }

        public Table ExePackageTable { private get; set; }

        public Table MsiPackageTable { private get; set; }

        public Table MspPackageTable { private get; set; }

        public Table MsuPackageTable { private get; set; }

        public IDictionary<string, PackageFacade> PackageFacades { get; private set; }

        public void Execute()
        {
            RowDictionary<WixBundleExePackageRow> exePackages = new RowDictionary<WixBundleExePackageRow>(this.ExePackageTable);
            RowDictionary<WixBundleMsiPackageRow> msiPackages = new RowDictionary<WixBundleMsiPackageRow>(this.MsiPackageTable);
            RowDictionary<BundleMspPackageRow> mspPackages = new RowDictionary<BundleMspPackageRow>(this.MspPackageTable);
            RowDictionary<WixBundleMsuPackageRow> msuPackages = new RowDictionary<WixBundleMsuPackageRow>(this.MsuPackageTable);

            Dictionary<string, PackageFacade> facades = new Dictionary<string, PackageFacade>(this.PackageTable.Rows.Count);

            foreach (WixBundlePackageRow package in this.PackageTable.Rows)
            {
                string id = package.WixChainItemId;
                PackageFacade facade = null;

                switch (package.Type)
                {
                    case WixBundlePackageType.Exe:
                        facade = new PackageFacade(package, exePackages.Get(id));
                        break;

                    case WixBundlePackageType.Msi:
                        facade = new PackageFacade(package, msiPackages.Get(id));
                        break;

                    case WixBundlePackageType.Msp:
                        facade = new PackageFacade(package, mspPackages.Get(id));
                        break;

                    case WixBundlePackageType.Msu:
                        facade = new PackageFacade(package, msuPackages.Get(id));
                        break;
                }

                facades.Add(id, facade);
            }

            this.PackageFacades = facades;
        }
    }
}
