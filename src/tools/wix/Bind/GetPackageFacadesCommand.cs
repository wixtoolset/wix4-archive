//-------------------------------------------------------------------------------------------------
// <copyright file="GetPackageFacadesCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class GetPackageFacadesCommand : ICommand
    {
        public Table ChainPackageTable { private get; set; }

        public Table ExePackageTable { private get; set; }

        public Table MsiPackageTable { private get; set; }

        public Table MspPackageTable { private get; set; }

        public Table MsuPackageTable { private get; set; }

        public IDictionary<string, ChainPackageFacade> Packages { get; private set; }

        public void Execute()
        {
            RowDictionary<WixBundleExePackageRow> exePackages = new RowDictionary<WixBundleExePackageRow>(this.ExePackageTable);
            RowDictionary<WixBundleMsiPackageRow> msiPackages = new RowDictionary<WixBundleMsiPackageRow>(this.MsiPackageTable);
            RowDictionary<BundleMspPackageRow> mspPackages = new RowDictionary<BundleMspPackageRow>(this.MspPackageTable);
            RowDictionary<WixBundleMsuPackageRow> msuPackages = new RowDictionary<WixBundleMsuPackageRow>(this.MsuPackageTable);

            Dictionary<string, ChainPackageFacade> packages = new Dictionary<string, ChainPackageFacade>(this.ChainPackageTable.Rows.Count);

            foreach (WixBundlePackageRow chainPackage in this.ChainPackageTable.Rows)
            {
                string id = chainPackage.WixChainItemId;
                ChainPackageFacade facade = null;

                switch (chainPackage.Type)
                {
                    case ChainPackageType.Exe:
                        facade = new ChainPackageFacade(chainPackage, exePackages.Get(id));
                        break;

                    case ChainPackageType.Msi:
                        facade = new ChainPackageFacade(chainPackage, msiPackages.Get(id));
                        break;

                    case ChainPackageType.Msp:
                        facade = new ChainPackageFacade(chainPackage, mspPackages.Get(id));
                        break;

                    case ChainPackageType.Msu:
                        facade = new ChainPackageFacade(chainPackage, msuPackages.Get(id));
                        break;
                }

                packages.Add(id, facade);
            }

            this.Packages = packages;
        }
    }
}
