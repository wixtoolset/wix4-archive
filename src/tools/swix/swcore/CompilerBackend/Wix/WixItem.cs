// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using WixToolset.Simplified.Lexicon;

    internal abstract class WixItem
    {
        private string componentMsiId;
        private string msiId;

        public WixItem(WixBackendCompiler backend, PackageItem item)
        {
            this.Backend = backend;
            this.Item = item;
        }

        public WixBackendCompiler Backend { get; private set; }

        public bool System { get { return this.Item.System; } }

        public WixGroup Group { get; set; }

        public WixItem Parent { get; set; }

        public PackageItem Item { get; private set; }

        public string ComponentMsiId
        {
            get
            {
                if (this.componentMsiId == null)
                {
                    this.componentMsiId = this.CalculateComponentMsiId();
                }

                return this.componentMsiId;
            }
        }

        public string MsiId
        {
            get
            {
                if (this.msiId == null)
                {
                    this.msiId = this.CalculateMsiId();
                }

                return this.msiId;
            }
        }

        public virtual WixSection GenerateSection()
        {
            return null;
        }

        public virtual void GenerateSectionRowsForComponent(WixSection section, string componentId)
        {
        }

        protected virtual string CalculateComponentMsiId()
        {
            return CalculateMsiId();
        }

        protected virtual string CalculateMsiId()
        {
            return WixBackendCompilerServices.GenerateMsiId(this.Backend, this.Item, null);
        }
    }
}
