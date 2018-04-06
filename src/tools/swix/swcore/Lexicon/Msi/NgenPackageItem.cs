// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Msi
{
    using System;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Internal package item for NGEN.
    /// </summary>
    internal class NgenPackageItem : PackageItemTargetsFile
    {
        private IFileReference application;
        private Folder folder;

        /// <summary>
        /// Optional application file. Default is parent folder.
        /// </summary>
        public IFileReference Application
        {
            get
            {
                if (this.application == null)
                {
                    DelayedItemLookup delayedItem;
                    if (this.File.DelayedLookup.TryGetValue("WixToolset.Simplified.Lexicon.Msi.Ngen.SetApplication()", out delayedItem))
                    {
                        this.application = (IFileReference)delayedItem.ResolvedItem;
                    }
                }

                return this.application;
            }

            set
            {
                this.application = value;
            }
        }

        /// <summary>
        /// Optional application folder for DLLs. Default is parent folder.
        /// </summary>
        public Folder Folder
        {
            get
            {
                if (this.folder == null)
                {
                    DelayedItemLookup delayedItem;
                    if (this.File.DelayedLookup.TryGetValue("WixToolset.Simplified.Lexicon.Msi.Ngen.SetFolder()", out delayedItem))
                    {
                        this.folder = (Folder)delayedItem.ResolvedItem;
                    }
                }

                return this.folder;
            }

            set
            {
                this.folder = value;
            }
        }

        /// <summary>
        /// Gets and sets the execute type for this item.
        /// </summary>
        public NgenExecuteType Execute { get; set; }
    }
}
