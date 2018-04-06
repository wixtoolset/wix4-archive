// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Vsix
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using IO = System.IO;
    using WixToolset.Simplified.Lexicon;
    using WixToolset.Simplified.Lexicon.Vsix;

    /// <summary>
    /// Vsix manifest object that manages the creation of the extension.vsixmanifest file.
    /// </summary>
    internal class VsixManifest
    {
        public static readonly XNamespace VsixNamespace = "http://schemas.microsoft.com/developer/vsx-schema/2010";

        private XDocument document;
        private VsixBackendCompiler backend;

        public VsixManifest(VsixBackendCompiler backend)
        {
            this.backend = backend;
        }

        /// <summary>
        /// List of files found while processing intermediates that need to be packaged.
        /// </summary>
        public List<PackageFile> Files { get; private set; }

        /// <summary>
        /// Gets the manifest as a stream.
        /// </summary>
        /// <returns>Stream representing the manifest.</returns>
        public IO.Stream GetStream()
        {
            IO.Stream manifestStream = new IO.MemoryStream();

            this.document.Save(manifestStream);
            manifestStream.Seek(0, IO.SeekOrigin.Begin);
            return manifestStream;
        }

        /// <summary>
        /// Creates the manifest document from the provided intermediates. Discards a previous
        /// manifest document if present.
        /// </summary>
        /// <param name="intermediates">Intermediates to process into the AppX manifest.</param>
        public void ProcessIntermediates(IEnumerable<Intermediate> intermediates)
        {
            this.document = null;
            this.Files = new List<PackageFile>();

            Package package = null;
            Version minNetfx = null;
            Version maxNetfx = null;
            SortedSet<string> supportedProducts = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            XElement xContent = new XElement(VsixNamespace + "Content");
            XElement xReferences = new XElement(VsixNamespace + "References");

            foreach (Intermediate intermediate in intermediates)
            {
                foreach (PackageItem item in intermediate.Items)
                {
                    if (item.System)
                    {
                        continue;
                    }

                    // Files are processed differently since we need to go search for them on disk and such.
                    if (item is File)
                    {
                        PackageFile packageFile;
                        if (PackageFile.TryCreate(backend, (File)item, out packageFile))
                        {
                            this.Files.Add(packageFile);
                        }
                    }
                    else if (item is Package)
                    {
                        if (package == null)
                        {
                            package = (Package)item;
                        }
                        else
                        {
                            this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.HighlanderElement("Package"), item));
                        }
                    }
                    else if (item is Prerequisite)
                    {
                        Prerequisite prereq = (Prerequisite)item;
                        switch (prereq.On.ToLowerInvariant())
                        {
                            case "netfx":
                                if (null == minNetfx)
                                {
                                    if (prereq.Version == null)
                                    {
                                        this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Prerequisite", "Version"), prereq));
                                    }
                                    else
                                    {
                                        minNetfx = prereq.Version;
                                    }

                                    if (prereq.MaxVersion != null)
                                    {
                                        maxNetfx = prereq.MaxVersion;
                                    }
                                }
                                else
                                {
                                    this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.HighlanderElementWithAttributeValue("Prerequisite", "On", prereq.On), prereq));
                                }
                                break;

                            case "vs":
                                    if (prereq.Version == null)
                                    {
                                        this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Prerequisite", "Version"), prereq));
                                    }
                                    else
                                    {
                                        string edition = Product.GetEdition(prereq);
                                        if (String.IsNullOrEmpty(edition))
                                        {
                                            this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Vsix", "Product.Edition"), prereq));
                                        }
                                        else
                                        {
                                            // Ensure that if the edition matches one of the well known values that it has the appropriate casing.
                                            string[] editionMatches = new string[] { "IntegratedShell", "Pro", "Premium", "Ultimate", "VWDExpress", "VCSExpress", "VBExpress", "VCExpress", "Express_All" };
                                            foreach (string editionMatch in editionMatches)
                                            {
                                                if (edition.Equals(editionMatch, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    edition = editionMatch;
                                                    break;
                                                }
                                            }

                                            supportedProducts.Add(String.Concat(prereq.Version.ToString(), "\\", edition));
                                        }
                                    }
                                break;

                            default:
                                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.InvalidAttributeValue("Prerequisite", "On", prereq.On), prereq));
                                break;
                        }
                    }
                    else if (item is Dependency)
                    {
                        Dependency dependency = (Dependency)item;
                        XElement reference = new XElement(VsixNamespace + "Reference",
                            new XAttribute("Id", dependency.Name),
                            new XElement(VsixNamespace + "Name", dependency.Publisher)
                            );

                        if (dependency.Version != null)
                        {
                            reference.Add(new XAttribute("MinVersion", dependency.Version.ToString()));
                        }

                        if (dependency.MaxVersion != null)
                        {
                            reference.Add(new XAttribute("MaxVersion", dependency.MaxVersion.ToString()));
                        }

                        xReferences.Add(reference);
                    }
                    else if (item is Vspackage)
                    {
                        Vspackage vspackage = (Vspackage)item;

                        // TODO: verify file specified
                        string pkgdef = VsixManifest.StripRootFolderReference(vspackage.File.Path);

                        // TODO: warn if file extension not .pkgdef.
                        xContent.Add(new XElement(VsixNamespace + "VsPackage", pkgdef));
                    }
                }
            }

            if (package != null)
            {
                // TODO: verify DisplayName <= 50 chars
                // TODO: verify Description <= 1000 chars
                // TODO: verify Manufacturer
                // TODO: verify Version
                // TODO: verify Languages[0] only one.

                XElement xIdentifier = new XElement(VsixNamespace + "Identifier",
                    new XAttribute("Id", package.Name),
                    new XElement(VsixNamespace + "Name", package.DisplayName),
                    new XElement(VsixNamespace + "Author", package.Manufacturer),
                    new XElement(VsixNamespace + "Version", package.Version.ToString()),
                    new XElement(VsixNamespace + "Description", package.Description),
                    new XElement(VsixNamespace + "Locale", this.backend.Languages[0].LCID)
                    );

                string image = (package.Image == null) ? null : package.Image.NonqualifiedName;
                if (!string.IsNullOrEmpty(image))
                {
                    xIdentifier.Add(VsixNamespace + "Icon", image);
                }

                xIdentifier.Add(new XElement(VsixNamespace + "InstalledByMsi", "false"));

                XElement xSupportedProducts = new XElement(VsixNamespace + "SupportedProducts");
                xIdentifier.Add(xSupportedProducts);
                string previonsVersion = null;
                XElement xVisualStudio = null;
                foreach (string supported in supportedProducts)
                {
                    string[] versionEdition = supported.Split(new char[] { '\\' }, 2);
                    if (!versionEdition[0].Equals(previonsVersion))
                    {
                        xVisualStudio = new XElement(VsixNamespace + "VisualStudio", 
                            new XAttribute("Version", versionEdition[0]));
                        xSupportedProducts.Add(xVisualStudio);

                        previonsVersion = versionEdition[0];
                    }

                    xVisualStudio.Add(new XElement(VsixNamespace + "Edition", versionEdition[1]));
                }

                if (null != minNetfx)
                {
                    XElement xSupportedFrameworkRuntime = new XElement(VsixNamespace + "SupportedFrameworkRuntimeEdition",
                        new XAttribute("MinVersion", minNetfx.ToString())
                        );

                    if (null != maxNetfx)
                    {
                        xSupportedFrameworkRuntime.Add(new XAttribute("MaxVersion", maxNetfx.ToString()));
                    }

                    xIdentifier.Add(xSupportedFrameworkRuntime);
                }

                if (package.Framework)
                {
                    xIdentifier.Add(VsixNamespace + "SystemComponent", "true");
                }

                // Now put the manifest together.
                XElement xRoot = new XElement(VsixNamespace + "Vsix", 
                    new XAttribute("Version", "1.0"),
                    xIdentifier
                    );

                if (xReferences.HasElements)
                {
                    xRoot.Add(xReferences);
                }

                if (xContent.HasElements)
                {
                    xRoot.Add(xContent);
                }

                this.document = new XDocument(xRoot);
            }
        }

        /// <summary>
        /// Validate the manifest document against the appropriate AppX manifest.
        /// </summary>
        /// <param name="eventHandler">Optional event handler to post validation errors.</param>
        public void Validate(ValidationEventHandler eventHandler)
        {
            string resource = "WixToolset.Simplified.CompilerBackend.Vsix.schema.VsixManifestSchema.xsd";
            XmlSchema schema = XmlSchema.Read(Assembly.GetExecutingAssembly().GetManifestResourceStream(resource), eventHandler);
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(schema);

            this.document.Validate(schemas, eventHandler);
        }

        internal static string StripRootFolderReference(string path)
        {
            string[] idPath = path.Split(new char[] { ':' }, 2);
            if (!idPath[0].Equals("ApplicationFolder"))
            {
                // TOOD: send warning that we are ignoring all other roots and we always put files in ApplicationFolder
            }

            return idPath[1].Substring(1); // skip the backslash.
        }
    }
}
