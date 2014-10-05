//-------------------------------------------------------------------------------------------------
// <copyright file="ChainPackageFacade.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using WixToolset.Data.Rows;

    internal class ChainPackageFacade
    {
        private ChainPackageFacade(ChainPackageRow chainPackage)
        {
            this.ChainPackage = chainPackage;
            this.Provides = new ProvidesDependencyCollection();
        }

        public ChainPackageFacade(ChainPackageRow chainPackage, ChainExePackageRow exePackage)
            : this(chainPackage)
        {
            this.ExePackage = exePackage;
        }

        public ChainPackageFacade(ChainPackageRow chainPackage, ChainMsiPackageRow msiPackage)
            : this(chainPackage)
        {
            this.MsiPackage = msiPackage;
        }

        public ChainPackageFacade(ChainPackageRow chainPackage, ChainMspPackageRow mspPackage)
            : this(chainPackage)
        {
            this.MspPackage = mspPackage;
        }

        public ChainPackageFacade(ChainPackageRow chainPackage, ChainMsuPackageRow msuPackage)
            : this(chainPackage)
        {
            this.MsuPackage = msuPackage;
        }

        public ChainPackageRow ChainPackage { get; private set; }

        public ChainExePackageRow ExePackage { get; private set; }

        public ChainMsiPackageRow MsiPackage { get; private set; }

        public ChainMspPackageRow MspPackage { get; private set; }

        public ChainMsuPackageRow MsuPackage { get; private set; }

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
