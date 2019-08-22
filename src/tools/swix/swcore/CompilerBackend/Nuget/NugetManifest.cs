// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Nuget
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using WixToolset.Simplified.Lexicon;
    using WixToolset.Simplified.Lexicon.Nuget;
    using IO = System.IO;

    /// <summary>
    /// Nuget manifest object that manages the creation of the .nuspec file.
    /// </summary>
    internal class NugetManifest
    {
        private static readonly CultureInfo DefaultLanguage = new CultureInfo("en-US");

        public static readonly XNamespace NugetNamespace = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";

        private XDocument document;
        private NugetBackendCompiler backend;

        public NugetManifest(NugetBackendCompiler backend)
        {
            this.backend = backend;
        }

        /// <summary>
        /// List of files found while processing intermediates that need to be packaged.
        /// </summary>
        public List<PackageFile> Files { get; private set; }

        public string DisplayName { get; private set; }

        public string Description { get; private set; }

        public string Manufacturer { get; private set; }

        public string Name { get; private set; }

        public string Language { get; private set; }

        public string Tags { get; private set; }

        public string Version { get; private set; }

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
        /// <param name="intermediates">Intermediates to process into the .nuspec file.</param>
        public void ProcessIntermediates(IEnumerable<Intermediate> intermediates)
        {
            this.document = null;
            this.Files = new List<PackageFile>();

            Package package = null;
            Metadata metadata = null;
            Version minNuget = null;
            Dictionary<string, List<XElement>> groupedDependencies = new Dictionary<string, List<XElement>>();
            Dictionary<string, List<XElement>> groupedFrameworks = new Dictionary<string, List<XElement>>();
            Dictionary<string, List<XElement>> groupedReferences = new Dictionary<string, List<XElement>>();

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
                            case "nuget":
                                if (null == minNuget)
                                {
                                    if (prereq.Version == null)
                                    {
                                        this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Prerequisite", "Version"), prereq));
                                    }
                                    else
                                    {
                                        minNuget = prereq.Version;
                                    }

                                    if (prereq.MaxVersion != null)
                                    {
                                        this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.InvalidAttributeValue("Prerequisite", "MaxVersion", prereq.MaxVersion.ToString()), prereq));
                                    }
                                }
                                else
                                {
                                    this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.HighlanderElementWithAttributeValue("Prerequisite", "On", prereq.On), prereq));
                                }
                                break;

                            default:
                                // TODO: handle this correctly
                                //xFrameworkAssemblies.Add(new XElement(NugetNamespace + "frameworkAssembly",
                                //                            new XAttribute("assemblyName", prereq.On)));
                                break;
                        }
                    }
                    // TODO: handle dependencies and references correctly.
                    //else if (item is Dependency)
                    //{
                    //    Dependency dependency = (Dependency)item;
                    //    XElement reference = new XElement(NugetNamespace + "reference",
                    //        new XAttribute("Id", dependency.Name),
                    //        new XElement(NugetNamespace + "Name", dependency.Publisher)
                    //        );

                    //    string targetFramework = String.Empty;

                    //    if (dependency.Version != null)
                    //    {
                    //        reference.Add(new XAttribute("MinVersion", dependency.Version.ToString()));
                    //    }

                    //    if (dependency.MaxVersion != null)
                    //    {
                    //        reference.Add(new XAttribute("MaxVersion", dependency.MaxVersion.ToString()));
                    //    }

                    //    List<XElement> dependencies;
                    //    if (!groupedDependencies.TryGetValue(targetFramework, out dependencies))
                    //    {
                    //        dependencies = new List<XElement>();
                    //        groupedDependencies.Add(targetFramework, dependencies);
                    //    }

                    //    dependencies.Add(reference);
                    //}
                    //else if (item is Reference)
                    //{
                    //    Reference reference = (Reference)item;
                    //    string targetFramework = reference.TargetFramework ?? String.Empty;

                    //    List<XElement> references;
                    //    if (!groupedReferences.TryGetValue(targetFramework, out references))
                    //    {
                    //        references = new List<XElement>();
                    //        groupedReferences.Add(targetFramework, references);
                    //    }

                    //    references.Add(new XElement(NugetNamespace + "reference",
                    //        new XAttribute("file", IO.Path.GetFileName(reference.File))));

                    //    references.Add(reference);
                    //}
                    else if (item is Metadata)
                    {
                        if (metadata == null)
                        {
                            metadata = (Metadata)item;
                        }
                        else
                        {
                            this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.HighlanderElement("Metadata"), item));
                        }
                    }
                }
            }

            if (package != null)
            {
                this.Description = package.Description;

                this.Name = package.Name;

                this.Language = (null == backend.Languages || backend.Languages.Length == 0 || backend.Languages[0] == DefaultLanguage) ? null : backend.Languages[0].ToString();

                this.Tags = metadata.Tags;

                this.DisplayName = package.DisplayName;

                this.Version = package.Version.ToString();

                XElement xMetadata = new XElement(NugetNamespace + "metadata");
                if (null != minNuget)
                {
                    xMetadata.Add(new XAttribute("minClientVersion", minNuget.ToString()));
                }

                xMetadata.Add(
                    new XElement(NugetNamespace + "id", package.Name),
                    new XElement(NugetNamespace + "version", this.Version),
                    String.IsNullOrEmpty(package.DisplayName) ? null : new XElement(NugetNamespace + "title", package.DisplayName),
                    new XElement(NugetNamespace + "authors", package.Manufacturer),
                    // TODO: Consider whether owner is useful enough to bring back. NuGet Gallery ignores it so no one will ever see this optional data.
                    //String.IsNullOrEmpty(metadata.Owner) ? null : new XElement(NugetNamespace + "owners", metadata.Owner),
                    String.IsNullOrEmpty(package.License) ? null : new XElement(NugetNamespace + "licenseUrl", package.License),
                    String.IsNullOrEmpty(package.About) ? null : new XElement(NugetNamespace + "projectUrl", package.About),
                    null == package.Image ? null : new XElement(NugetNamespace + "iconUrl", package.Image),
                    metadata.DevelopmentDependency ? null : new XElement(NugetNamespace + "developmentDependency", "true"),
                    new XElement(NugetNamespace + "requireLicenseAcceptance", metadata.RequireLicenseAcceptance ? "true" : "false"),
                    new XElement(NugetNamespace + "description", package.Description),
                    String.IsNullOrEmpty(metadata.Summary) ? null : new XElement(NugetNamespace + "summary", metadata.Summary),
                    String.IsNullOrEmpty(metadata.ReleaseNotes) ? null : new XElement(NugetNamespace + "releaseNotes", metadata.ReleaseNotes),
                    String.IsNullOrEmpty(package.Copyright) ? null : new XElement(NugetNamespace + "copyright", package.Copyright),
                    this.Language == null ? null : new XElement(NugetNamespace + "language", this.Language),
                    String.IsNullOrEmpty(metadata.Tags) ? null : new XElement(NugetNamespace + "tags", metadata.Tags)
                    );

                // Now put the manifest together.
                if (groupedDependencies != null)
                {
                    XElement xDependencies = new XElement(NugetNamespace + "dependencies");
                    this.AddGroupedElements(xDependencies, groupedDependencies);
                    xMetadata.Add(xDependencies);
                }

                if (groupedFrameworks != null)
                {
                    XElement xFrameworkAssemblies = new XElement(NugetNamespace + "frameworkAssemblies");
                    this.AddGroupedElements(xFrameworkAssemblies, groupedFrameworks);
                    xMetadata.Add(xFrameworkAssemblies);
                }

                if (groupedReferences != null)
                {
                    XElement xReferences = new XElement(NugetNamespace + "references");
                    this.AddGroupedElements(xReferences, groupedReferences);
                    xMetadata.Add(xReferences);
                }

                this.document = new XDocument(new XElement(NugetNamespace + "package", xMetadata));
            }
        }

        private void AddGroupedElements(XElement xParent, Dictionary<string, List<XElement>> groupedElements)
        {
            List<XElement> xElements = null;
            if (groupedElements.Count == 1 && groupedElements.TryGetValue(String.Empty, out xElements))
            {
                xParent.Add(xElements);
            }
            else
            {
                foreach (string group in groupedElements.Keys.OrderBy(s => s))
                {
                    xElements = groupedElements[group];

                    XElement xGroup = new XElement(NugetNamespace + "group",
                        String.IsNullOrEmpty(group) ? null : new XAttribute("targetFramework", group),
                        xElements);

                    xParent.Add(group);
                }
            }
        }

        /// <summary>
        /// Validate the manifest document against the appropriate NuGet manifest.
        /// </summary>
        /// <param name="eventHandler">Optional event handler to post validation errors.</param>
        public void Validate(ValidationEventHandler eventHandler)
        {
            string resource = "WixToolset.Simplified.CompilerBackend.Nuget.schema.nuget.xsd";
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
                // TODO: send warning that we are ignoring all other roots and we always put files in ApplicationFolder
            }

            return idPath[1].Substring(1); // skip the backslash.
        }
    }
}
