// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Simplified.Lexicon;

    internal class WixGroup : WixItem
    {
        public WixGroup(WixBackendCompiler backend, Group group) :
            base(backend, group)
        {
            this.ContainedWixItems = new List<WixItem>();
        }

        public List<WixItem> ContainedWixItems { get; private set; }

        public override WixSection GenerateSection()
        {
            if (this.System)
            {
                return null;
            }

            WixSection section = new WixSection(this.MsiId, "fragment", this.Item.LineNumber);
            this.GenerateComponentGroup(section);

            foreach (WixItem msiItem in this.ContainedWixItems)
            {
                if (msiItem.Item is Group) // nested Groups become ComponentGroupRefs
                {
                    this.CreateComponentGroupRef(section, msiItem.MsiId);
                }
                else if (!String.IsNullOrEmpty(msiItem.ComponentMsiId)) // must be an item that creates a Component.
                {
                    this.CreateComponentRef(section, msiItem.ComponentMsiId);
                }
            }

            return section;
        }

        private void GenerateComponentGroup(WixSection section)
        {
            WixBackendCompilerServices.GenerateRow(section, "WixComponentGroup", this.Item.LineNumber,
                this.MsiId);
        }

        private void CreateComponentGroupRef(WixSection section, string refComponentMsiId)
        {
            WixBackendCompilerServices.GenerateSimpleReference(section, "WixComponentGroup", this.Item.LineNumber, refComponentMsiId);

            WixBackendCompilerServices.GenerateRow(section, "WixComplexReference", this.Item.LineNumber,
                this.MsiId,
                WixBackendCompilerServices.ComplexReferenceParentTypeComponentGroup,
                null,
                refComponentMsiId,
                WixBackendCompilerServices.ComplexReferenceChildTypeComponentGroup,
                0);

            WixBackendCompilerServices.GenerateRow(section, "WixGroup", this.Item.LineNumber,
                this.MsiId,
                "ComponentGroup",
                refComponentMsiId,
                "ComponentGroup");
        }

        private void CreateComponentRef(WixSection section, string refMsiId)
        {
            WixBackendCompilerServices.GenerateSimpleReference(section, "Component", this.Item.LineNumber, refMsiId);

            WixBackendCompilerServices.GenerateRow(section, "WixComplexReference", this.Item.LineNumber,
                this.MsiId,
                WixBackendCompilerServices.ComplexReferenceParentTypeComponentGroup,
                null,
                refMsiId,
                WixBackendCompilerServices.ComplexReferenceChildTypeComponent,
                0);

            WixBackendCompilerServices.GenerateRow(section, "WixGroup", this.Item.LineNumber,
                this.MsiId,
                "ComponentGroup",
                refMsiId,
                "Component");
        }
    }
}
