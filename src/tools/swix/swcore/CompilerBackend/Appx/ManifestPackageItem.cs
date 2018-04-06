// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System.Xml.Linq;
    using WixToolset.Simplified.Lexicon;

    internal class ManifestPackageItem : ManifestItem
    {
        private XElement applications;
        private XElement capabilities;
        private XElement dependencies;
        private XElement extensions;

        public ManifestPackageItem(string key, PackageItem item, XElement xml) :
            base(key, item, xml)
        {
        }

        public XElement Applications
        {
            get
            {
                if (this.applications == null)
                {
                    this.applications = new XElement(AppxManifest.AppxNamespace + "Applications");
                }

                return this.applications;
            }
        }

        public XElement Capabilities
        {
            get
            {
                if (this.capabilities == null)
                {
                    this.capabilities = new XElement(AppxManifest.AppxNamespace + "Capabilities");
                }

                return this.capabilities;
            }
        }

        public XElement Dependencies
        {
            get
            {
                if (this.dependencies == null)
                {
                    this.dependencies = new XElement(AppxManifest.AppxNamespace + "Dependencies");
                }

                return this.dependencies;
            }
        }

        public XElement Prereqs { get; set;  }

        public XElement Extensions
        {
            get
            {
                if (this.extensions == null)
                {
                    this.extensions = new XElement(AppxManifest.AppxNamespace + "Extensions");
                }

                return this.extensions;
            }
        }

        internal void Finish(BackendCompiler backend)
        {
            if (this.Prereqs != null && this.Prereqs.HasElements)
            {
                this.Xml.Add(this.Prereqs);
            }
            else // single prerequisite is required.
            {
                this.Xml.Add(new XElement(AppxManifest.AppxNamespace + "Prerequisites",
                                    new XElement(AppxManifest.AppxNamespace + "OSMinVersion", "6.2.1"),
                                    new XElement(AppxManifest.AppxNamespace + "OSMaxVersionTested", "6.2.1")
                                    )
                            );
            }

            if (this.dependencies != null && this.dependencies.HasElements)
            {
                this.Xml.Add(this.dependencies);
            }

            if (this.capabilities != null && this.capabilities.HasElements)
            {
                this.Xml.Add(this.capabilities);
            }

            if (this.extensions != null && this.extensions.HasElements)
            {
                this.Xml.Add(this.extensions);
            }

            if (this.applications != null && this.applications.HasElements)
            {
                this.Xml.Add(this.applications);
            }
        }
    }
}
