// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using WixToolset.Simplified.Lexicon.Msi;

    internal class WixFileSearch : WixItem
    {
        public WixFileSearch(WixBackendCompiler backend, FileSearch fileSearch) :
            base(backend, fileSearch)
        {
        }

        protected override string CalculateMsiId()
        {
            return this.Item.Id.ToUpperInvariant();
        }

        public override WixSection GenerateSection()
        {
            if (this.System)
            {
                return null;
            }

            FileSearch fs = (FileSearch)this.Item;
            if (String.IsNullOrEmpty(fs.Id))
            {
                // TODO: display error that id is required.
                return null;
            }

            WixSection section = new WixSection(fs.Id, "fragment", this.Item.LineNumber);
            WixBackendCompilerServices.GenerateRow(section, "AppSearch", this.Item.LineNumber,
                this.MsiId, // Property
                fs.Id);     // Signature_

            WixBackendCompilerServices.GenerateRow(section, "Property", this.Item.LineNumber,
                this.MsiId, // Property
                null);      // Value

            if (!String.IsNullOrEmpty(fs.Registry))
            {
                int root = ConvertRegistryRoot(fs.RegistryRoot);

                WixBackendCompilerServices.GenerateRow(section, "RegLocator", this.Item.LineNumber,
                    fs.Id,              // Signature_
                    root,               // Root
                    fs.RegistryKey,     // Key
                    fs.RegistryName,    // Name
                    2);                 // Type (raw)
            }
            else
            {
                WixBackendCompilerServices.GenerateRow(section, "CompLocator", this.Item.LineNumber,
                    fs.Id,                                          // Signature_
                    fs.Component.ToString("B").ToUpperInvariant(),  // ComponentId
                    1);                                             // Type (file)
            }

            return section;
        }

        private int ConvertRegistryRoot(string root)
        {
            int number = -1;
            switch (root.ToUpperInvariant())
            {
                case "HKCR":
                    number = 0;
                    break;
                case "HKCU":
                    number = 1;
                    break;
                case "HKLM":
                    number = 2;
                    break;
                case "HKU":
                    number = 3;
                    break;
            }

            return number;
        }
    }
}
