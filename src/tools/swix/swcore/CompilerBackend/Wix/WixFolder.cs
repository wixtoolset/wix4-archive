// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using WixToolset.Simplified.Lexicon;

    internal class WixFolder : WixItem
    {
        public WixFolder(WixBackendCompiler backend, Folder folder) :
            base(backend, folder)
        {
        }

        /// <summary>
        /// Folders never create Components
        /// </summary>
        /// <returns>String.Empty so this only gets called once but doesn't not create an actual Component Id.</returns>
        protected override string CalculateComponentMsiId()
        {
            return String.Empty;
        }

        protected override string CalculateMsiId()
        {
            Folder folder = (Folder)this.Item;
            string safePath = WixBackendCompilerServices.GenerateSafeMsiIdFromPath(folder.Path);
            string msiId = WixBackendCompilerServices.GenerateId(this.Backend, this.Item, safePath, folder.Path);

            return msiId;
        }

        public override WixSection GenerateSection()
        {
            if (this.System || String.IsNullOrEmpty(this.MsiId))
            {
                return null;
            }

            Folder folder = (Folder)this.Item;
            WixItem parentMsiItem = WixBackendCompilerServices.ResolveParentFolderMsiItem(folder.ParentFolder, this.Backend.WixItems);
            if (parentMsiItem == null)
            {
                return null;
            }

            WixSection section = new WixSection(this.MsiId, "fragment", this.Item.LineNumber);

            string defaultDir = String.IsNullOrEmpty(folder.Name) ? "." : WixBackendCompilerServices.GenerateMsiFileName(false, folder.Name.TrimEnd(new char[] { '\\' }), "Directory", parentMsiItem.MsiId);

            WixBackendCompilerServices.GenerateRow(section, "Directory", this.Item.LineNumber,
                this.MsiId,            // Id
                parentMsiItem.MsiId,   // Directory_Parent
                defaultDir);           // DefaultDir

            WixBackendCompilerServices.GenerateSimpleReference(section, "Directory", this.Item.LineNumber, parentMsiItem.MsiId);

            return section;
        }
    }
}
