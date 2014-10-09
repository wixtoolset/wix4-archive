//-------------------------------------------------------------------------------------------------
// <copyright file="PackageFacade.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using WixToolset.Data.Rows;

    internal class PackageFacade
    {
        private PackageFacade(WixBundlePackageRow package)
        {
            this.Package = package;
            this.Provides = new ProvidesDependencyCollection();
        }

        public PackageFacade(WixBundlePackageRow package, WixBundleExePackageRow exePackage)
            : this(package)
        {
            this.ExePackage = exePackage;
        }

        public PackageFacade(WixBundlePackageRow package, WixBundleMsiPackageRow msiPackage)
            : this(package)
        {
            this.MsiPackage = msiPackage;
        }

        public PackageFacade(WixBundlePackageRow package, BundleMspPackageRow mspPackage)
            : this(package)
        {
            this.MspPackage = mspPackage;
        }

        public PackageFacade(WixBundlePackageRow package, WixBundleMsuPackageRow msuPackage)
            : this(package)
        {
            this.MsuPackage = msuPackage;
        }

        public WixBundlePackageRow Package { get; private set; }

        public WixBundleExePackageRow ExePackage { get; private set; }

        public WixBundleMsiPackageRow MsiPackage { get; private set; }

        public BundleMspPackageRow MspPackage { get; private set; }

        public WixBundleMsuPackageRow MsuPackage { get; private set; }

        /// <summary>
        /// The provides dependencies authored and imported for this package.
        /// </summary>
        /// <remarks>
        /// TODO: Eventually this collection should turn into Rows so they are tracked in the PDB but
        /// the relationship with the extension makes it much trickier to pull off.
        /// </remarks>
        public ProvidesDependencyCollection Provides { get; private set; }
    }
}
