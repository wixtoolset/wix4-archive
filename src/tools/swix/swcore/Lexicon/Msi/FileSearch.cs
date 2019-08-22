// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Msi
{
    using System;

    public class FileSearch : PackageItem, IFileReference
    {
        /// <summary>
        /// Searches for an MSI Component by it's GUID.
        /// </summary>
        public Guid Component { get; set; }

        /// <summary>
        /// Searches the registry key for a file path.
        /// </summary>
        /// <remarks>
        /// To get the default value of a registry key the path should end in a backslash, e.g. "HKLM\Path\To\Default\Value\". Otherwise,
        /// "HKLM\Path\To\Default\Named\Value"
        /// </remarks>
        public string Registry { get; set; }

        /// <summary>
        /// Returns this object as a package item.
        /// </summary>
        /// <returns>this</returns>
        public PackageItem GetPackageItem()
        {
            return this;
        }

        internal string RegistryRoot { get; private set; }

        internal string RegistryKey { get; private set; }

        internal string RegistryName { get; private set; }

        protected override void OnResolveBegin(CompilerFrontend.FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (!String.IsNullOrEmpty(this.Registry))
            {
                if (Guid.Empty == this.Component)
                {
                    int slash = this.Registry.IndexOf('\\');
                    this.RegistryRoot = this.Registry.Substring(0, slash);
                    this.RegistryKey = this.Registry.Substring(slash + 1);

                    if (this.RegistryKey.EndsWith("\\"))
                    {
                        this.RegistryKey = this.RegistryKey.Substring(0, this.RegistryKey.Length - 1);
                    }
                    else
                    {
                        slash = this.RegistryKey.LastIndexOf('\\');
                        this.RegistryName = this.RegistryKey.Substring(slash + 1);
                        this.RegistryKey = this.RegistryKey.Substring(0, slash);
                    }
                }
                else
                {
                    // TODO: show error that registry and component are mutually exclusive.
                }
            }
            else if (Guid.Empty == this.Component)
            {
                // TODO: show error that one of registry or component are required.
            }
        }
    }
}
