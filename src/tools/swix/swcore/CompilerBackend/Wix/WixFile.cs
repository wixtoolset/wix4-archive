// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using WixToolset.Simplified.Lexicon;

    internal class WixFile : WixItem
    {
        public WixFile(WixBackendCompiler backend, File file) :
            base(backend, file)
        {
        }

        protected override string CalculateMsiId()
        {
            File file = (File)this.Item;
            string id = WixBackendCompilerServices.GenerateId(this.Backend, this.Item, file.Name, file.Path);
            return id;
        }

        public override WixSection GenerateSection()
        {
            if (this.System)
            {
                return null;
            }

            File file = (File)this.Item;
            WixItem folderMsiItem = WixBackendCompilerServices.ResolveParentFolderMsiItem(file.ParentFolder, this.Backend.WixItems);
            if (folderMsiItem == null)
            {
                return null;
            }

            string componentId = this.ComponentMsiId;
            int attributes = this.Backend.Architecture == PackageArchitecture.X64 ? 256 : 0;
            string condition = WixBackendCompilerServices.GenerateMsiCondition(this);
            if (!String.IsNullOrEmpty(condition))
            {
                attributes |= 64; // mark Component transitive when there is a condition.
            }

            WixSection section = new WixSection(this.MsiId, "fragment", this.Item.LineNumber);
            WixBackendCompilerServices.GenerateRow(section, "Component", this.Item.LineNumber,
                componentId,           // Id
                "*",                   // Guid
                folderMsiItem.MsiId,   // Directory
                attributes,            // Attributes
                condition,             // Condition
                this.MsiId);           // KeyPath

            this.GenerateSectionRowsForComponent(section, componentId);

            WixBackendCompilerServices.GenerateSimpleReference(section, "Directory", this.Item.LineNumber, folderMsiItem.MsiId);
            WixBackendCompilerServices.GenerateSimpleReference(section, "Media", this.Item.LineNumber, "1");

            foreach (ITargetFile targetFileItem in file.Items)
            {
                File childsFile = targetFileItem.GetTargetedFile();
                if (childsFile == file)
                {
                    WixItem item = this.Backend.WixItems[(PackageItem)targetFileItem];
                    item.GenerateSectionRowsForComponent(section, componentId);
                }
            }

            return section;
        }

        public override void GenerateSectionRowsForComponent(WixSection section, string componentId)
        {
            File file = (File)this.Item;
            WixItem folderMsiItem = WixBackendCompilerServices.ResolveParentFolderMsiItem(file.ParentFolder, this.Backend.WixItems);

            string fileId = this.MsiId;
            string fileName = WixBackendCompilerServices.GenerateMsiFileName(true, file.Name, "File", fileId);
            bool gac = (folderMsiItem is WixFolderReference && folderMsiItem.System && folderMsiItem.Item.Id.Equals("GacFolder"));

            WixBackendCompilerServices.GenerateRow(section, "File", this.Item.LineNumber,
                fileId,                // Id
                componentId,           // Component
                fileName,              // FileName
                0,                     // Size
                null,                  // Version
                null,                  // Language
                WixBackendCompilerServices.MsidbFileAttributesVital,   // Attributes
                null);                 // Sequence

            WixBackendCompilerServices.GenerateRow(section, "WixFile", this.Item.LineNumber,
                fileId,                // Id
                gac ? "0" : null,      // AssemblyAttributes
                null,                  // AssemblyManifest
                null,                  // AssemblyAssemblyApplication
                folderMsiItem.MsiId,   // Directory
                1,                     // DiskId
                file.Source,           // Source
                null/*file.Architecture*/, // TODO: make this right
                -1,
                String.Equals(file.Name, fileName, StringComparison.OrdinalIgnoreCase) ? 0 : 1,  // GeneratedShortName
                0,
                null,
                null,
                null,
                null);

            if (gac)
            {
                WixBackendCompilerServices.GenerateRow(section, "MsiAssembly", this.Item.LineNumber,
                    componentId,                // ComponentId
                    Guid.Empty.ToString("B"),   // Feature (complex reference)
                    null,                       // AssemblyManifest
                    null,                       // AssemblyApplication
                    0);                         // AssemblyAttributes
            }
        }
    }
}
