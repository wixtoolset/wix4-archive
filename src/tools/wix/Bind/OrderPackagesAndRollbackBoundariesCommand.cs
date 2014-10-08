//-------------------------------------------------------------------------------------------------
// <copyright file="OrderPackagesAndRollbackBoundariesCommand.cs" company="Outercurve Foundation">
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
    using System.Linq;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    internal class OrderPackagesAndRollbackBoundariesCommand : ICommand
    {
        public Table wixGroupTable { private get; set; }

        public RowDictionary<WixRollbackBoundaryRow> boundaries { private get; set; }

        public IDictionary<string, ChainPackageFacade> packages { private get; set; }

        public IEnumerable<ChainPackageFacade> OrderedPackages { get; private set; }

        public IEnumerable<WixRollbackBoundaryRow> UsedRollbackBoundaries { get; private set; }

        public void Execute()
        {
            List<ChainPackageFacade> orderedPackages = new List<ChainPackageFacade>();
            List<WixRollbackBoundaryRow> usedBoundaries = new List<WixRollbackBoundaryRow>();

            // Process the chain of packages to add them in the correct order
            // and assign the forward rollback boundaries as appropriate. Remember
            // rollback boundaries are authored as elements in the chain which
            // we re-interpret here to add them as attributes on the next available
            // package in the chain. Essentially we mark some packages as being
            // the start of a rollback boundary when installing and repairing.
            // We handle uninstall (aka: backwards) rollback boundaries after
            // we get these install/repair (aka: forward) rollback boundaries
            // defined.
            WixRollbackBoundaryRow previousRollbackBoundary = null;

            foreach (WixGroupRow row in this.wixGroupTable.Rows)
            {
                if (ComplexReferenceChildType.Package == row.ChildType && ComplexReferenceParentType.PackageGroup == row.ParentType && "WixChain" == row.ParentId)
                {
                    ChainPackageFacade package = null;
                    if (packages.TryGetValue(row.ChildId, out package))
                    {
                        if (null != previousRollbackBoundary)
                        {
                            usedBoundaries.Add(previousRollbackBoundary);

                            package.Package.RollbackBoundary = previousRollbackBoundary.ChainPackageId;
                            previousRollbackBoundary = null;
                        }

                        orderedPackages.Add(package);
                    }
                    else // must be a rollback boundary.
                    {
                        // Discard the next rollback boundary if we have a previously defined boundary.
                        WixRollbackBoundaryRow nextRollbackBoundary = boundaries.Get(row.ChildId);
                        if (null != previousRollbackBoundary)
                        {
                            Messaging.Instance.OnMessage(WixWarnings.DiscardedRollbackBoundary(nextRollbackBoundary.SourceLineNumbers, nextRollbackBoundary.ChainPackageId));
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
                Messaging.Instance.OnMessage(WixWarnings.DiscardedRollbackBoundary(previousRollbackBoundary.SourceLineNumbers, previousRollbackBoundary.ChainPackageId));
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
            ChainPackageFacade previousPackage = null;

            foreach (ChainPackageFacade package in orderedPackages)
            {
                if (null != package.Package.RollbackBoundary)
                {
                    if (null != previousPackage)
                    {
                        previousPackage.Package.RollbackBoundaryBackward = previousRollbackBoundaryId;
                    }

                    previousRollbackBoundaryId = package.Package.RollbackBoundary;
                }

                previousPackage = package;
            }

            if (!String.IsNullOrEmpty(previousRollbackBoundaryId) && null != previousPackage)
            {
                previousPackage.Package.RollbackBoundaryBackward = previousRollbackBoundaryId;
            }

            this.OrderedPackages = orderedPackages;
            this.UsedRollbackBoundaries = usedBoundaries;
        }
    }
}
