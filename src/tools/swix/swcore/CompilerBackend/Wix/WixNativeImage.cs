//-------------------------------------------------------------------------------------------------
// <copyright file="WixNativeImage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using System.IO;
    using WixToolset.Simplified.Lexicon.Msi;

    internal class WixNativeImage : WixItem
    {
        public WixNativeImage(WixBackendCompiler backend, NgenPackageItem ngen) :
            base(backend, ngen)
        {
        }

        public override void GenerateSectionRowsForComponent(WixSection section, string componentId)
        {
            NgenPackageItem ngen = (NgenPackageItem)this.Item;

            int priority = 3;
            switch (ngen.Execute)
            {
                case NgenExecuteType.asynchronous:
                    priority = 1;
                    break;

                case NgenExecuteType.idle:
                    priority = 3;
                    break;

                case NgenExecuteType.immediate:
                    priority = 0;
                    break;
            }

            int attributes = 0x2; // dependencies are not processed automatically.
            switch (this.Backend.Architecture)
            {
                case PackageArchitecture.Arm:
                case PackageArchitecture.X86:
                    attributes |= 0x8; // 32-bit
                    break;

                case PackageArchitecture.X64:
                    attributes |= 0x10; // 64-bit
                    break;

                case PackageArchitecture.Unknown:
                case PackageArchitecture.Neutral:
                    attributes |= 0x18; // both
                    break;
            }

            WixItem msiFileItem = this.Backend.WixItems[ngen.File];
            string refTable = null;
            string refId = null;

            string applicationFile = null;
            if (ngen.Application != null)
            {
                WixItem item = this.Backend.WixItems[ngen.Application.GetPackageItem()];
                if (item.Item is Lexicon.File)
                {
                    applicationFile = item.MsiId;
                    refTable = "File";
                    refId = item.MsiId;
                }
                else if (item.Item is Lexicon.Msi.Property || item.Item is Lexicon.Msi.FileSearch)
                {
                    applicationFile = String.Concat("[", item.MsiId, "]");
                    refTable = "Property";
                    refId = item.MsiId;
                }
                else
                {
                    // TODO: display error.
                }
            }

            string applicationFolder = null;
            if (ngen.Folder != null)
            {
                WixItem item = this.Backend.WixItems[ngen.Folder];
                applicationFolder = item.MsiId;
                refTable = "Directory";
                refId = item.MsiId;
            }

            if (!String.IsNullOrEmpty(refTable))
            {
                WixBackendCompilerServices.GenerateSimpleReference(section, refTable, this.Item.LineNumber, refId);
            }

            WixBackendCompilerServices.GenerateRow(section, "NetFxNativeImage", this.Item.LineNumber,
                msiFileItem.MsiId,  // Id
                msiFileItem.MsiId,  // File_
                priority,           // Priority
                attributes,         // Attributes
                applicationFile,    // File_Application
                applicationFolder); // Directory_ApplicationBase

            WixBackendCompilerServices.GenerateSimpleReference(section, "CustomAction", this.Item.LineNumber, "NetFxScheduleNativeImage");
        }
    }
}
