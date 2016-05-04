// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Simplified.Lexicon;

    internal class WixClassId : WixItem
    {
        private bool inproc;

        public WixClassId(WixBackendCompiler backend, InprocServer inproc) :
            base(backend, inproc)
        {
            this.inproc = true;
        }

        public WixClassId(WixBackendCompiler backend, OutprocServer outproc) :
            base(backend, outproc)
        {
        }

        protected override string CalculateComponentMsiId()
        {
            InprocServer inprocServer = this.Item as InprocServer;
            OutprocServer outprocServer = this.Item as OutprocServer;

            File file = this.inproc ? inprocServer.File : outprocServer.File;
            if (file == this.Item.Parent)
            {
                return String.Empty;
            }

            return base.CalculateComponentMsiId();
        }

        protected override string CalculateMsiId()
        {
            InprocServer inprocServer = this.Item as InprocServer;
            OutprocServer outprocServer = this.Item as OutprocServer;
            Class firstClass = this.inproc ? inprocServer.Classes[0] : outprocServer.Classes[0];

            string key = this.GetKey(firstClass.Id);
            string id = WixBackendCompilerServices.GenerateIdForRegKey(this.Backend, this.Item, 0, key, null);
            return id;
        }

        public override WixSection GenerateSection()
        {
            if (this.System)
            {
                return null;
            }

            InprocServer inprocServer = this.Item as InprocServer;
            OutprocServer outprocServer = this.Item as OutprocServer;

            File file = this.inproc ? inprocServer.File : outprocServer.File;
            if (file == this.Item.Parent)
            {
                return null;
            }

            WixItem folderMsiItem = WixBackendCompilerServices.ResolveParentFolderMsiItem(file.ParentFolder, this.Backend.WixItems);
            if (folderMsiItem == null)
            {
                return null;
            }

            string componentId = this.ComponentMsiId;
            int attributes = this.Backend.Architecture == PackageArchitecture.X64 ? 260 : 4;
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

            return section;
        }

        public override void GenerateSectionRowsForComponent(WixSection section, string componentId)
        {
            InprocServer inprocServer = this.Item as InprocServer;
            OutprocServer outprocServer = this.Item as OutprocServer;

            IEnumerable<Class> classes = this.inproc ? inprocServer.Classes : outprocServer.Classes;
            foreach (Class classId in classes)
            {
                File fileItem = this.inproc ? inprocServer.File : outprocServer.File;
                WixItem msiFileItem = this.Backend.WixItems[fileItem];

                string guidClassId = new Guid(classId.Id).ToString("B").ToUpperInvariant();
                string key = this.GetKey(guidClassId);

                string value = String.Concat("[#", msiFileItem.MsiId, "]");
                if (!this.inproc && !String.IsNullOrWhiteSpace(outprocServer.Arguments))
                {
                    value = String.Concat("\"", value, "\" ", outprocServer.Arguments);
                }

                string msiId = WixBackendCompilerServices.GenerateIdForRegKey(this.Backend, null, 0, key, null);
                WixBackendCompilerServices.GenerateRow(section, "Registry", this.Item.LineNumber,
                    msiId,          // Id
                    0,              // HKCR
                    key,            // Key
                    null,           // Name
                    value,          // Value
                    componentId);   // Component

                string threadingModel = ConvertThreadingModel(classId.ThreadingModel);

                msiId = WixBackendCompilerServices.GenerateIdForRegKey(this.Backend, null, 0, key, "ThreadingModel");
                WixBackendCompilerServices.GenerateRow(section, "Registry", this.Item.LineNumber,
                    msiId,              // Id
                    0,                  // HKCR
                    key,                // Key
                    "ThreadingModel",   // Name
                    threadingModel,     // Value
                    componentId);       // Component

                if (!String.IsNullOrEmpty(classId.Implementation))
                {
                    key = String.Concat("CLSID\\", guidClassId, "\\ProgID");
                    msiId = WixBackendCompilerServices.GenerateIdForRegKey(this.Backend, null, 0, key, null);
                    WixBackendCompilerServices.GenerateRow(section, "Registry", this.Item.LineNumber,
                        msiId,              // Id
                        0,                  // HKCR
                        key,                // Key
                        null,               // Name
                        classId.Implementation, // Value
                        componentId);       // Component

                    key = String.Concat(classId.Implementation, "\\CLSID");
                    msiId = WixBackendCompilerServices.GenerateIdForRegKey(this.Backend, null, 0, key, null);
                    WixBackendCompilerServices.GenerateRow(section, "Registry", this.Item.LineNumber,
                        msiId,              // Id
                        0,                  // HKCR
                        key,                // Key
                        null,               // Name
                        guidClassId,         // Value
                        componentId);       // Component
                }
            }
        }

        private string GetKey(string classId)
        {
            string key = String.Concat("CLSID\\", classId, "\\", this.inproc ? "InprocServer32" : "LocalServer32");
            return key;
        }

        private static string ConvertThreadingModel(ThreadingModelType threadingModel)
        {
            switch (threadingModel)
            {
                case ThreadingModelType.both:
                    return "Both";
                case ThreadingModelType.mta:
                    return "Free";
                case ThreadingModelType.neutral:
                    return "Neutral";
                case ThreadingModelType.sta:
                    return "Apartment";
            }

            throw new ArgumentException("Unknown threading model type.");
        }
    }
}
