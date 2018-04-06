// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using WixToolset.Simplified.Lexicon;
    using AppxLexicon = WixToolset.Simplified.Lexicon.Appx;
    using IO = System.IO;

    /// <summary>
    /// AppX manifest object that always represents the latest version of the spec.
    /// </summary>
    internal class AppxManifest : IAppxManifest
    {
        public static readonly XNamespace AppxNamespace = "http://schemas.microsoft.com/appx/2010/manifest";

        private AppxBackendCompiler backend;

        private XDocument document;

        /// <summary>
        /// Creates an AppX manifest targeting a particular milestone.
        /// </summary>
        /// <param name="backend">Backend for the manifest.</param>
        public AppxManifest(AppxBackendCompiler backend)
        {
            this.backend = backend;
        }

        /// <summary>
        /// List of files while processing intermediates that need to be packaged.
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

            ManifestPackageItem packageItem = null;
            Dictionary<Application, ManifestApplicationItem> applications = new Dictionary<Application, ManifestApplicationItem>();
            List<ManifestItem> manifestItems = new List<ManifestItem>();

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
                    else // probably just an item representing some piece of the manifest.
                    {
                        ManifestItem manifestItem;
                        if (this.TryCreateManifestItem(item, out manifestItem))
                        {
                            // If we just processed a package item, ensure there is only one and remember it since
                            // we handle the package item special.
                            ManifestPackageItem package = manifestItem as ManifestPackageItem;
                            if (package != null)
                            {
                                if (packageItem != null)
                                {
                                    this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.HighlanderElement("Package"), packageItem.Item));
                                }

                                packageItem = package;
                            }
                            else // all other items get tossed in the list to be processed below.
                            {
                                manifestItems.Add(manifestItem);

                                // Application manifest items are indexed separately so that the extension manifest
                                // items can find their parent application.
                                Application application = item as Application;
                                if (application != null)
                                {
                                    applications.Add(application, (ManifestApplicationItem)manifestItem);
                                }
                            }
                        }
                    }
                }
            }

            if (packageItem == null)
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredElement("Package"), null, 0, 0));
            }

            // It is very important to sort the manifest items to ensure that they are processed
            // in the right order. In particular, most resources depend on their parent application
            // being processed first. The ManifestItem.CompareTo() method defines the order.
            manifestItems.Sort();

            foreach (ManifestItem item in manifestItems)
            {
                if (item.ParentApplication == null)
                {
                    if (item.Item is AppxLexicon.Capability)
                    {
                        packageItem.Capabilities.Add(item.Xml);
                    }
                    else if (item.Item is AppxLexicon.DeviceCapability)
                    {
                        packageItem.Capabilities.Add(item.Xml);
                    }
                    else if (item.Item is Dependency)
                    {
                        packageItem.Dependencies.Add(item.Xml);
                    }
                    else if (item.Item is Application)
                    {
                        packageItem.Applications.Add(item.Xml);
                    }
                    else if (item.Item is Prerequisite)
                    {
                        if (packageItem.Prereqs == null)
                        {
                            packageItem.Prereqs = item.Xml;
                        }
                        else // there can only be one prereqs
                        {
                            this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.HighlanderElement("Package", "Prerequisite"), packageItem.Item));
                        }
                    }
                    else
                    {
                        packageItem.Extensions.Add(item.Xml);
                    }
                }
                else
                {
                    ManifestApplicationItem applicationItem = null;
                    if (applications.TryGetValue(item.ParentApplication, out applicationItem))
                    {
                        if (item.Item is AppxLexicon.ContentUri)
                        {
                            applicationItem.ContentUriRules.Add(item.Xml);
                        }
                        else if (item.Item is AppxLexicon.LockScreen)
                        {
                            applicationItem.LockScreen = item;
                        }
                        else if (item.Item is AppxLexicon.InitialRotation)
                        {
                            applicationItem.InitialRotationPreferences.Add(item.Xml);
                        }
                        else if (item.Item is AppxLexicon.SplashScreen)
                        {
                            applicationItem.SplashScreen = item;
                        }
                        else if (item.Item is AppxLexicon.Tile)
                        {
                            applicationItem.Tile = item;
                        }
                        else
                        {
                            applicationItem.Extensions.Add(item.Xml);
                        }
                    }
                    else
                    {
                        // TODO: display error that we could not find a manifest application for the parent application item.
                    }
                }
            }

            // Finalize the XML for each application manifest item.
            foreach (ManifestApplicationItem applicationItem in applications.Values)
            {
                applicationItem.Finish(this.backend);
            }

            // Finalize the XML for the package manifest item.
            packageItem.Finish(this.backend);

            this.document = new XDocument(packageItem.Xml);
        }

        /// <summary>
        /// Validate the manifest document against the appropriate AppX manifest.
        /// </summary>
        /// <param name="eventHandler">Optional event handler to post validation errors.</param>
        public void Validate(ValidationEventHandler eventHandler)
        {
            string resource = "WixToolset.Simplified.CompilerBackend.Appx.schema.AppxManifestSchema.xsd";
            XmlSchema schema = XmlSchema.Read(Assembly.GetExecutingAssembly().GetManifestResourceStream(resource), eventHandler);
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(schema);

            this.document.Validate(schemas, eventHandler);
        }

        private bool TryCreateManifestItem(PackageItem item, out ManifestItem manifestItem)
        {
            manifestItem = null;

            if (item is Package)
            {
                manifestItem = this.Add((Package)item, this.backend.Architecture);
            }
            else if (item is Application)
            {
                manifestItem = this.Add((Application)item);
            }
            else if (item is AppxLexicon.BackgroundTask)
            {
                manifestItem = this.Add((AppxLexicon.BackgroundTask)item);
            }
            else if (item is AppxLexicon.CameraSettings)
            {
                manifestItem = this.Add((AppxLexicon.CameraSettings)item);
            }
            else if (item is AppxLexicon.Capability)
            {
                manifestItem = this.Add((AppxLexicon.Capability)item);
            }
            else if (item is AppxLexicon.ContactPicker)
            {
                manifestItem = this.Add((AppxLexicon.ContactPicker)item);
            }
            else if (item is AppxLexicon.ContentUri)
            {
                manifestItem = this.Add((AppxLexicon.ContentUri)item);
            }
            else if (item is Dependency)
            {
                manifestItem = this.Add((Dependency)item);
            }
            else if (item is AppxLexicon.DeviceCapability)
            {
                manifestItem = this.Add((AppxLexicon.DeviceCapability)item);
            }
            else if (item is AppxLexicon.FileOpenPicker)
            {
                manifestItem = this.Add((AppxLexicon.FileOpenPicker)item);
            }
            else if (item is AppxLexicon.FileSavePicker)
            {
                manifestItem = this.Add((AppxLexicon.FileSavePicker)item);
            }
            else if (item is FileAssociation)
            {
                manifestItem = this.Add((FileAssociation)item);
            }
            else if (item is GameExplorer)
            {
                manifestItem = this.Add((GameExplorer)item);
            }
            else if (item is AppxLexicon.InitialRotation)
            {
                manifestItem = this.Add((AppxLexicon.InitialRotation)item);
            }
            else if (item is InprocServer)
            {
                manifestItem = this.Add((InprocServer)item);
            }
            else if (item is AppxLexicon.LockScreen)
            {
                manifestItem = this.Add((AppxLexicon.LockScreen)item);
            }
            else if (item is OutprocServer)
            {
                manifestItem = this.Add((OutprocServer)item);
            }
            else if (item is AppxLexicon.PrinterSettings)
            {
                manifestItem = this.Add((AppxLexicon.PrinterSettings)item);
            }
            else if (item is Prerequisite)
            {
                manifestItem = this.Add((Prerequisite)item);
            }
            else if (item is Protocol)
            {
                manifestItem = this.Add((Protocol)item);
            }
            else if (item is ProxyStub)
            {
                manifestItem = this.Add((ProxyStub)item);
            }
            else if (item is AppxLexicon.Search)
            {
                manifestItem = this.Add((AppxLexicon.Search)item);
            }
            else if (item is AppxLexicon.ShareTarget)
            {
                manifestItem = this.Add((AppxLexicon.ShareTarget)item);
            }
            else if (item is AppxLexicon.SplashScreen)
            {
                manifestItem = this.Add((AppxLexicon.SplashScreen)item);
            }
            else if (item is AppxLexicon.Tile)
            {
                manifestItem = this.Add((AppxLexicon.Tile)item);
            }
            else if (item is AppxLexicon.AccountPictureProvider)
            {
                manifestItem = this.Add((AppxLexicon.AccountPictureProvider)item);
            }

            return (manifestItem != null);
        }

        private ManifestItem Add(Package package, PackageArchitecture architecture)
        {
            Version version = new Version(
                package.Version.Major < 0 ? 0 : package.Version.Major,
                package.Version.Minor < 0 ? 0 : package.Version.Minor,
                package.Version.Build < 0 ? 0 : package.Version.Build,
                package.Version.Revision < 0 ? 0 : package.Version.Revision
                );

            // TODO: verify Name is provided and "-.[A-Za-z0-9]{1-50}"
            // TODO: verify Publisher is provided and {1-8192}
            // TODO: verify this.Languages.Count is > 0 and < 200
            // TODO: verify DisplayName is {1-512}
            // TODO: verify Description is {0-2048}
            // TODO: verify logo path ends with "jpg", "jpeg" or "png"

            if (String.IsNullOrEmpty(package.Manufacturer))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Package", "Manufacturer"), package));
            }

            XElement xPackage = new XElement(AppxManifest.AppxNamespace + "Package");

            // Identity
            XElement xIdentity = new XElement(AppxManifest.AppxNamespace + "Identity",
                new XAttribute("Name", package.Name));
            xPackage.Add(xIdentity);

            if (architecture != PackageArchitecture.Neutral)
            {
                xIdentity.Add(new XAttribute("ProcessorArchitecture", architecture.ToString().ToLowerInvariant()));
            }

            xIdentity.Add(new XAttribute("Publisher", package.Publisher));

            xIdentity.Add(new XAttribute("Version", version.ToString()));

            // Properties
            XElement xProperties = new XElement(AppxManifest.AppxNamespace + "Properties");
            xPackage.Add(xProperties);

            if (package.Framework)
            {
                xProperties.Add(new XElement(AppxManifest.AppxNamespace + "Framework", "true"));
            }

            xProperties.Add(
                new XElement(AppxManifest.AppxNamespace + "DisplayName", package.DisplayName),
                new XElement(AppxManifest.AppxNamespace + "PublisherDisplayName", package.Manufacturer)
                );

            if (!String.IsNullOrWhiteSpace(package.Description))
            {
                xProperties.Add(new XElement(AppxManifest.AppxNamespace + "Description", package.Description));
            }

            // Logo is required.
            string logo = package.Image == null ? null : package.Image.NonqualifiedName;
            if (!String.IsNullOrEmpty(logo))
            {
                xProperties.Add(new XElement(AppxManifest.AppxNamespace + "Logo", logo));
            }
            else
            {
                backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Package", "Image"), package));
            }


            // Resources
            XElement xResources = new XElement(AppxManifest.AppxNamespace + "Resources");
            xPackage.Add(xResources);

            foreach (CultureInfo language in this.backend.Languages)
            {
                xResources.Add(new XElement(AppxManifest.AppxNamespace + "Resource",
                    new XAttribute("Language", language.Name)));
            }

            return new ManifestPackageItem("Package", package, xPackage);
        }

        private ManifestItem Add(Application application)
        {
            if (application.File == null)
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Application", "File"), application));
                return null;
            }

            XElement xApplication = new XElement(AppxManifest.AppxNamespace + "Application",
                                                new XAttribute("Id", application.Name));

            string path = (application.File == null) ? null : application.File.Path;
            this.SetEntryPoint(application, xApplication, path, application.Implementation, null);

            string key = String.Concat("Application:", application.Name);
            return new ManifestApplicationItem(key, application, xApplication);
        }

        private ManifestItem Add(AppxLexicon.BackgroundTask backgroundTask)
        {
            XElement xBackgroundTasks = new XElement(AppxManifest.AppxNamespace + "BackgroundTasks",
                                            new XElement(AppxManifest.AppxNamespace + "Task",
                                                new XAttribute("Type", ParseBackgroundTaskName(backgroundTask.Name))));

            if (!String.IsNullOrEmpty(backgroundTask.ServerName))
            {
                xBackgroundTasks.Add(new XAttribute("ServerName", backgroundTask.ServerName));
            }

            HashSet<AppxLexicon.BackgroundTaskType> set = new HashSet<AppxLexicon.BackgroundTaskType>();
            set.Add(backgroundTask.Name);

            foreach (AppxLexicon.AdditionalTask additionalTask in backgroundTask.AdditionalTasks)
            {
                string taskName = ParseBackgroundTaskName(additionalTask.Name);
                if (set.Contains(additionalTask.Name)) // ensure the task name is unique.
                {
                    this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.AttributeIgnoringDuplicateValue("AdditionalTask", "Name", taskName), backgroundTask));
                }
                else
                {
                    xBackgroundTasks.Add(new XElement(AppxManifest.AppxNamespace + "Task",
                        new XAttribute("Type", taskName)));

                    set.Add(additionalTask.Name);
                }
            }

            return this.CreateApplicationManifestItem(backgroundTask, "windows.backgroundTasks", backgroundTask.Id, xBackgroundTasks, backgroundTask);
        }

        private string ParseBackgroundTaskName(AppxLexicon.BackgroundTaskType type)
        {
            string taskName = null;
            switch (type)
            {
                case AppxLexicon.BackgroundTaskType.audio:
                    taskName = "audio";
                    break;

                case AppxLexicon.BackgroundTaskType.controlChannel:
                    taskName = "controlChannel";
                    break;

                case AppxLexicon.BackgroundTaskType.pushNotification:
                    taskName = "pushNotification";
                    break;

                case AppxLexicon.BackgroundTaskType.systemEvent:
                    taskName = "systemEvent";
                    break;

                case AppxLexicon.BackgroundTaskType.timer:
                    taskName = "timer";
                    break;
            }

            return taskName;
        }

        private ManifestItem Add(AppxLexicon.Capability capability)
        {
            string capabilityName = null;
            switch (capability.Name)
            {
                case AppxLexicon.CapabilityName.musicLibrary:
                    capabilityName = "musicLibrary";
                    break;

                case AppxLexicon.CapabilityName.picturesLibrary:
                    capabilityName = "picturesLibrary";
                    break;

                case AppxLexicon.CapabilityName.videosLibrary:
                    capabilityName = "videosLibrary";
                    break;

                case AppxLexicon.CapabilityName.internetClient:
                    capabilityName = "internetClient";
                    break;

                case AppxLexicon.CapabilityName.internetClientServer:
                    capabilityName = "internetClientServer";
                    break;

                case AppxLexicon.CapabilityName.privateNetworkClientServer:
                    capabilityName = "privateNetworkClientServer";
                    break;

                case AppxLexicon.CapabilityName.documentsLibrary:
                    capabilityName = "documentsLibrary";
                    break;

                case AppxLexicon.CapabilityName.enterpriseAuthentication:
                    capabilityName = "enterpriseAuthentication";
                    break;

                case AppxLexicon.CapabilityName.sharedUserCertificates:
                    capabilityName = "sharedUserCertificates";
                    break;

                case AppxLexicon.CapabilityName.removableStorage:
                    capabilityName = "removableStorage";
                    break;
            }

            if (!String.IsNullOrEmpty(capabilityName))
            {
                XElement xCapability = new XElement(AppxManifest.AppxNamespace + "Capability",
                    new XAttribute("Name", capabilityName));

                string key = String.Concat("capability:", capabilityName);
                return new ManifestItem(key, capability, xCapability);
            }
            else
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.DeprecatedAttributeValue("Capability", "Name", capability.Name), capability));
                return null;
            }
        }

        /// <summary>
        /// Camera settings processing.
        /// </summary>
        /// <param name="cameraSettings">Item to process.</param>
        /// <returns>Manifest item.</returns>
        private ManifestItem Add(AppxLexicon.CameraSettings cameraSettings)
        {
            return this.CreateApplicationManifestItem(cameraSettings, "windows.cameraSettings", null, null, cameraSettings);
        }

        /// <summary>
        /// Contact picker processing.
        /// </summary>
        /// <param name="contactPicker">Item to process.</param>
        /// <returns>Manifest item.</returns>
        private ManifestItem Add(AppxLexicon.ContactPicker contactPicker)
        {
            return this.CreateApplicationManifestItem(contactPicker, "windows.contactPicker", null, null, contactPicker);
        }

        private ManifestItem Add(AppxLexicon.ContentUri contentUri)
        {
            XElement xContentUriRule = new XElement(AppxManifest.AppxNamespace + "Rule",
                new XAttribute("Type", contentUri.Rule.ToString().ToLowerInvariant()),
                new XAttribute("Match", contentUri.Match)
                );

            string key = String.Concat("windows.contentUriRule:", String.Concat(contentUri.Rule, "/", contentUri.Match));
            return this.CreateApplicationManifestItem(contentUri, key, xContentUriRule, false);
        }

        private ManifestItem Add(Dependency dependency)
        {
            if (String.IsNullOrEmpty(dependency.Name))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Dependency", "Name"), dependency));
                return null;
            }

            XElement xDependency = new XElement(AppxManifest.AppxNamespace + "PackageDependency",
                new XAttribute("Name", dependency.Name));

            if (!String.IsNullOrEmpty(dependency.Publisher))
            {
                xDependency.Add(new XAttribute("Publisher", dependency.Publisher));
            }

            if (dependency.Version != null)
            {
                xDependency.Add(new XAttribute("MinVersion", dependency.Version.ToString()));
            }

            string key = String.Concat("dependency:", dependency.Name);
            return new ManifestItem(key, dependency, xDependency);
        }

        private ManifestItem Add(AppxLexicon.DeviceCapability deviceCapability)
        {
            if (String.IsNullOrWhiteSpace(deviceCapability.Name))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("DeviceCapability", "Name"), deviceCapability));

                return null;
            }
            else
            {
                XElement xDeviceCapability = new XElement(AppxManifest.AppxNamespace + "DeviceCapability",
                    new XAttribute("Name", deviceCapability.Name));

                string key = String.Concat("DeviceCapability:", deviceCapability.Name);
                return new ManifestItem(key, deviceCapability, xDeviceCapability);
            }
        }

        private ManifestItem Add(AppxLexicon.DigitalRightsManagement drm)
        {
            string file = (drm.File == null) ? null : AppxManifest.StripRootFolderReference(drm.File.Path);
            if (String.IsNullOrEmpty(file))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("DigitalRightsManagement", "File"), drm));
                return null;
            }

            XElement xDigitalRightsManagement = new XElement(AppxManifest.AppxNamespace + "DigitalRightsManagement",
                    new XAttribute("ConfigFile", file));

            string key = String.Concat("windows.digitalRightsManagement:", file);
            XElement xExtension = AppxManifest.CreateExtensionElement(xDigitalRightsManagement, "windows.digitalRightsManagement");
            return new ManifestItem(key, drm, xExtension);
        }

        private ManifestItem Add(FileAssociation fileAssociation)
        {
            XElement xFileAssociation = new XElement(AppxManifest.AppxNamespace + "FileTypeAssociation",
                new XAttribute("Name", fileAssociation.Name));

            if (!String.IsNullOrEmpty(fileAssociation.DisplayName))
            {
                xFileAssociation.Add(new XElement(AppxManifest.AppxNamespace + "DisplayName", fileAssociation.DisplayName));
            }

            string logo = fileAssociation.Image == null ? null : AppxManifest.StripRootFolderReference(fileAssociation.Image.Path);
            if (!String.IsNullOrEmpty(logo))
            {
                xFileAssociation.Add(new XElement(AppxManifest.AppxNamespace + "Logo", logo));
            }

            if (!String.IsNullOrEmpty(fileAssociation.Description))
            {
                xFileAssociation.Add(new XElement(AppxManifest.AppxNamespace + "InfoTip", fileAssociation.Description));
            }

            if (fileAssociation.OpenIsSafe || fileAssociation.AlwaysUnsafe)
            {
                XElement xEditFlags = new XElement(AppxManifest.AppxNamespace + "EditFlags");
                xFileAssociation.Add(xEditFlags);

                if (fileAssociation.OpenIsSafe)
                {
                    xEditFlags.Add(new XAttribute("OpenIsSafe", "true"));
                }

                if (fileAssociation.AlwaysUnsafe)
                {
                    xEditFlags.Add(new XAttribute("AlwaysUnsafe", "true"));
                }
            }

            XElement xSupportedFileTypes = new XElement(AppxManifest.AppxNamespace + "SupportedFileTypes");
            foreach (FileType fileType in fileAssociation.SupportedFileTypes)
            {
                XElement xFileType = new XElement(AppxManifest.AppxNamespace + "FileType", fileType.Extension);
                xSupportedFileTypes.Add(xFileType);

                if (!String.IsNullOrEmpty(fileType.ContentType))
                {
                    xFileType.Add(new XAttribute("ContentType", fileType.ContentType));
                }
            }

            if (xSupportedFileTypes.HasElements)
            {
                xFileAssociation.Add(xSupportedFileTypes);
            }

            string key = String.Concat("windows.fileTypeAssociation:", fileAssociation.Name);
            string path = (fileAssociation.File == null) ? null : fileAssociation.File.Path;
            return this.CreateApplicationManifestItem(fileAssociation, "windows.fileTypeAssociation", key, xFileAssociation, false, fileAssociation.Implementation, path, null);
        }

        private ManifestItem Add(GameExplorer gameExplorer)
        {
            string file = (gameExplorer.File == null) ? null : AppxManifest.StripRootFolderReference(gameExplorer.File.Path);
            if (String.IsNullOrEmpty(file))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("GameExplorer", "File"), gameExplorer));
                return null;
            }

            XElement xGameExplorer = new XElement(AppxManifest.AppxNamespace + "GameExplorer",
                    new XAttribute("GameDefinitionContainer", file));

            string key = String.Concat("windows.gameExplorer:", file);
            XElement xExtension = AppxManifest.CreateExtensionElement(xGameExplorer, "windows.gameExplorer");
            return new ManifestItem(key, gameExplorer, xExtension);
        }

        private ManifestItem Add(InprocServer inprocServer)
        {
            string path = (inprocServer.File == null) ? null : AppxManifest.StripRootFolderReference(inprocServer.File.Path);
            if (String.IsNullOrEmpty(path))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("InprocServer", "File"), inprocServer));
                return null;
            }

            XElement xInprocServer = new XElement(AppxManifest.AppxNamespace + "InProcessServer",
                new XElement(AppxManifest.AppxNamespace + "Path", path));

            foreach (Class cls in inprocServer.Classes)
            {
                XElement xActivatableClass;
                if (this.TryParseClassElement(inprocServer, cls, true, out xActivatableClass))
                {
                    xInprocServer.Add(xActivatableClass);
                }
            }

            string key = String.Concat("windows.activatableClass.inProcessServer:", path);
            XElement xExtension = AppxManifest.CreateExtensionElement(xInprocServer, "windows.activatableClass.inProcessServer");
            return new ManifestItem(key, inprocServer, xExtension);
        }

        private ManifestItem Add(AppxLexicon.LockScreen lockScreen)
        {
            string notification = null;

            switch (lockScreen.Notification)
            {
                case AppxLexicon.LockScreenNotification.image:
                    notification = "badge";
                    break;

                case AppxLexicon.LockScreenNotification.imageAndTileText:
                    notification = "badgeAndTileText";
                    break;
            }

            XElement xLockScreen = new XElement(AppxManifest.AppxNamespace + "LockScreen",
                new XAttribute("Notification", notification)
                );

            string image = (lockScreen.Image == null) ? null : lockScreen.Image.NonqualifiedName;
            if (String.IsNullOrEmpty(image))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("LockScreen", "Image"), lockScreen));
            }
            else
            {
                xLockScreen.Add(new XAttribute("BadgeLogo", image));
            }

            return this.CreateApplicationManifestItem(lockScreen, "lockScreen", xLockScreen);
        }

        private ManifestItem Add(AppxLexicon.InitialRotation initialRotation)
        {
            XElement xInitialRotation = new XElement(AppxManifest.AppxNamespace + "Rotation",
                new XAttribute("Preference", initialRotation.Preference)
                );

            string key = String.Concat("windows.initialRotation:", initialRotation.Preference);
            return this.CreateApplicationManifestItem(initialRotation, key, xInitialRotation, false);
        }

        private ManifestItem Add(OutprocServer outprocServer)
        {
            string path = (outprocServer.File == null) ? null : AppxManifest.StripRootFolderReference(outprocServer.File.Path);
            if (String.IsNullOrEmpty(path))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("OutprocServer", "File"), outprocServer));
                return null;
            }
            else if (String.IsNullOrEmpty(outprocServer.Name))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("OutprocServer", "Name"), outprocServer));
                return null;
            }


            XElement xOutprocServer = new XElement(AppxManifest.AppxNamespace + "OutOfProcessServer",
                new XAttribute("ServerName", outprocServer.Name),
                new XElement(AppxManifest.AppxNamespace + "Path", path));

            if (!String.IsNullOrWhiteSpace(outprocServer.Arguments))
            {
                xOutprocServer.Add(new XElement(AppxManifest.AppxNamespace + "Arguments", outprocServer.Arguments));
            }

            string instancing = null;
            switch (outprocServer.Instance)
            {
                case InstanceType.Single:
                    instancing = "singleInstance";
                    break;

                case InstanceType.Multiple:
                    instancing = "multipleInstances";
                    break;
            }

            xOutprocServer.Add(new XElement(AppxManifest.AppxNamespace + "Instancing", instancing));

            foreach (Class cls in outprocServer.Classes)
            {
                XElement xActivatableClass;
                if (this.TryParseClassElement(outprocServer, cls, false, out xActivatableClass))
                {
                    xOutprocServer.Add(xActivatableClass);
                }
            }

            string key = String.Concat("windows.activatableClass.outOfProcessServer:", path);
            XElement xExtension = AppxManifest.CreateExtensionElement(xOutprocServer, "windows.activatableClass.outOfProcessServer");
            return new ManifestItem(key, outprocServer, xExtension);
        }

        private ManifestItem Add(Prerequisite prerequisite)
        {
            XElement xPrerequisite = new XElement(AppxManifest.AppxNamespace + "Prerequisites");

            Version minVersion = null;
            if (prerequisite.Version != null && prerequisite.Version.Major > 0)
            {
                minVersion = (prerequisite.Version.Build > -1) ? new Version(prerequisite.Version.Major, prerequisite.Version.Minor, prerequisite.Version.Build) : new Version(prerequisite.Version.Major, prerequisite.Version.Minor);
                xPrerequisite.Add(new XElement(AppxManifest.AppxNamespace + "OSMinVersion", minVersion));
            }
            else
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Prerequisite", "MinOsVersion"), prerequisite));
            }

            if (prerequisite.MaxVersion != null && prerequisite.MaxVersion.Major > 0)
            {
                Version version = (prerequisite.MaxVersion.Build > -1) ? new Version(prerequisite.MaxVersion.Major, prerequisite.MaxVersion.Minor, prerequisite.MaxVersion.Build) : new Version(prerequisite.MaxVersion.Major, prerequisite.MaxVersion.Minor);
                xPrerequisite.Add(new XElement(AppxManifest.AppxNamespace + "OSMaxVersionTested", version));
            }
            else if (minVersion != null)
            {
                xPrerequisite.Add(new XElement(AppxManifest.AppxNamespace + "OSMaxVersionTested", minVersion));
            }

            string key = "Prequisite";
            return new ManifestItem(key, prerequisite, xPrerequisite);
        }

        /// <summary>
        /// Printer settings processing.
        /// </summary>
        /// <param name="printerSettings">Item to process.</param>
        /// <returns>Manifest item.</returns>
        private ManifestItem Add(AppxLexicon.PrinterSettings printerSettings)
        {
            return this.CreateApplicationManifestItem(printerSettings, "windows.printTaskSettings", null, null, printerSettings);
        }

        private ManifestItem Add(Protocol protocol)
        {
            string logo = protocol.Image == null ? null : AppxManifest.StripRootFolderReference(protocol.Image.Path);

            XElement xProtocol = new XElement(AppxManifest.AppxNamespace + "Protocol",
                new XAttribute("Name", protocol.Name)
            );

            if (!String.IsNullOrEmpty(logo))
            {
                xProtocol.Add(new XElement(AppxManifest.AppxNamespace + "Logo", logo));
            }

            if (!String.IsNullOrEmpty(protocol.DisplayName))
            {
                xProtocol.Add(new XElement(AppxManifest.AppxNamespace + "DisplayName", protocol.DisplayName));
            }

            string key = String.Concat("windows.protocol:", protocol.Name);
            string path = (protocol.File == null) ? null : protocol.File.Path;
            return this.CreateApplicationManifestItem(protocol, "windows.protocol", key, xProtocol, false, protocol.Implementation, path, null);
        }

        private ManifestItem Add(ProxyStub proxyStub)
        {
            string path = (proxyStub.File == null) ? null : AppxManifest.StripRootFolderReference(proxyStub.File.Path);
            if (String.IsNullOrEmpty(path))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("ProxyStub", "File"), proxyStub));
                return null;
            }

            XElement xProxyStub = new XElement(AppxManifest.AppxNamespace + "ProxyStub",
                new XElement(AppxManifest.AppxNamespace + "Path", path),
                new XAttribute("ClassId", proxyStub.Guid));

            foreach (Interface iface in proxyStub.Interfaces)
            {
                xProxyStub.Add(new XElement(AppxManifest.AppxNamespace + "Interface",
                    new XAttribute("Name", iface.Name),
                    new XAttribute("InterfaceId", iface.Guid))
                    );
            }

            string key = String.Concat("windows.activatableClass.inProcessServer:", path);
            XElement xExtension = AppxManifest.CreateExtensionElement(xProxyStub, "windows.activatableClass.proxyStub");
            return new ManifestItem(key, proxyStub, xExtension);
        }

        /// <summary>
        /// File open picker processing.
        /// </summary>
        /// <param name="picker">Item to process.</param>
        /// <returns>Manifest item.</returns>
        private ManifestItem Add(AppxLexicon.FileOpenPicker picker)
        {
            string elementName = "FileOpenPicker";
            XElement xPicker = new XElement(AppxManifest.AppxNamespace + elementName);

            XElement xSupportedFileExtensions = new XElement(AppxManifest.AppxNamespace + "SupportedFileTypes");
            xPicker.Add(xSupportedFileExtensions);

            bool supportsAllFileExtensions = false;
            foreach (AppxLexicon.FileExtension fe in picker.SupportedFileExtensions)
            {
                if (fe.Name.Equals("*", StringComparison.Ordinal) || fe.Name.Equals("*.*", StringComparison.Ordinal))
                {
                    supportsAllFileExtensions = true;
                }
                else
                {
                    xSupportedFileExtensions.Add(new XElement(AppxManifest.AppxNamespace + "FileType", fe.Name));
                }
            }

            if (supportsAllFileExtensions)
            {
                xSupportedFileExtensions.Add(new XElement(AppxManifest.AppxNamespace + "SupportsAnyFileType"));
            }

            return this.CreateApplicationManifestItem(picker, "windows.fileOpenPicker", picker.Id, xPicker, picker);
        }

        /// <summary>
        /// File save picker processing.
        /// </summary>
        /// <param name="picker">Item to process.</param>
        /// <returns>Manifest item.</returns>
        private ManifestItem Add(AppxLexicon.FileSavePicker picker)
        {
            string elementName = "FileSavePicker";
            XElement xPicker = new XElement(AppxManifest.AppxNamespace + elementName);

            XElement xSupportedFileExtensions = new XElement(AppxManifest.AppxNamespace + "SupportedFileTypes");
            xPicker.Add(xSupportedFileExtensions);

            bool supportsAllFileExtensions = false;
            foreach (AppxLexicon.FileExtension fe in picker.SupportedFileExtensions)
            {
                if (fe.Name.Equals("*", StringComparison.Ordinal) || fe.Name.Equals("*.*", StringComparison.Ordinal))
                {
                    supportsAllFileExtensions = true;
                }
                else
                {
                    xSupportedFileExtensions.Add(new XElement(AppxManifest.AppxNamespace + "FileType", fe.Name));
                }
            }

            if (supportsAllFileExtensions)
            {
                xSupportedFileExtensions.Add(new XElement(AppxManifest.AppxNamespace + "SupportsAnyFileType"));
            }

            return this.CreateApplicationManifestItem(picker, "windows.fileSavePicker", picker.Id, xPicker, picker);
        }

        /// <summary>
        /// Search target processing.
        /// </summary>
        /// <param name="search">Item to process.</param>
        /// <returns>Manifest item.</returns>
        private ManifestItem Add(AppxLexicon.Search search)
        {
            return this.CreateApplicationManifestItem(search, "windows.search", null, null, search);
        }

        /// <summary>
        /// Share target processing.
        /// </summary>
        /// <param name="shareTarget">Item to process.</param>
        /// <returns>Manifest item.</returns>
        private ManifestItem Add(AppxLexicon.ShareTarget shareTarget)
        {
            XElement xShareTarget = new XElement(AppxManifest.AppxNamespace + "ShareTarget");

            bool supportsAllFileExtensions = false;
            XElement xSupportedFileExtensions = new XElement(AppxManifest.AppxNamespace + "SupportedFileTypes");
            foreach (AppxLexicon.SupportedDataFormat data in shareTarget.SupportedDataFormats)
            {
                if (data is AppxLexicon.FileExtension)
                {
                    AppxLexicon.FileExtension fe = (AppxLexicon.FileExtension)data;
                    if (fe.Name.Equals("*", StringComparison.Ordinal) || fe.Name.Equals("*.*", StringComparison.Ordinal))
                    {
                        supportsAllFileExtensions = true;
                    }
                    else
                    {
                        xSupportedFileExtensions.Add(new XElement(AppxManifest.AppxNamespace + "FileType", fe.Name));
                    }
                }
            }

            if (supportsAllFileExtensions)
            {
                xSupportedFileExtensions.Add(new XAttribute("SupportsAnyFileType", true));
            }

            if (xSupportedFileExtensions.HasAttributes || xSupportedFileExtensions.HasElements)
            {
                xShareTarget.Add(xSupportedFileExtensions);
            }

            foreach (AppxLexicon.SupportedDataFormat data in shareTarget.SupportedDataFormats)
            {
                string elementName = "DataFormat";
                if (data is AppxLexicon.DataFormat)
                {
                    xShareTarget.Add(new XElement(AppxManifest.AppxNamespace + elementName, data.Name));
                }
                else
                {
                    // error?
                }
            }

            // TODO: error if there are no child elements.

            return this.CreateApplicationManifestItem(shareTarget, "windows.shareTarget", shareTarget.Id, xShareTarget, shareTarget);
        }

        private ManifestItem Add(AppxLexicon.SplashScreen splashScreen)
        {
            string image = (splashScreen.Image == null) ? null : splashScreen.Image.NonqualifiedName;
            if (String.IsNullOrEmpty(image))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("SplashScreen", "Image"), splashScreen));
            }

            XElement xSplashScreen = new XElement(AppxManifest.AppxNamespace + "SplashScreen",
                new XAttribute("Image", image ?? String.Empty)
                );

            if (!String.IsNullOrEmpty(splashScreen.Background))
            {
                string color = AppxManifest.GetColor(splashScreen.Background);
                if (String.IsNullOrEmpty(color))
                {
                    this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.InvalidAttributeValue("SplashScreen", "Background", splashScreen.Background), splashScreen));
                }
                else
                {
                    xSplashScreen.Add(new XAttribute("BackgroundColor", color));
                }
            }

            return this.CreateApplicationManifestItem(splashScreen, "splashScreen", xSplashScreen);
        }

        private ManifestItem Add(AppxLexicon.Tile tile)
        {
            // Tile is all processed when the ManifestApplicationItem.Finish() is called.
            return this.CreateApplicationManifestItem(tile, "tile", null);
        }

        /// <summary>
        /// Account picture provider processing.
        /// </summary>
        /// <param name="accountPictureProvider">Item to process.</param>
        /// <returns>Manifest item.</returns>
        private ManifestItem Add(AppxLexicon.AccountPictureProvider accountPictureProvider)
        {
            return this.CreateApplicationManifestItem(accountPictureProvider, "windows.accountPictureProvider", null, null, accountPictureProvider);
        }

        private static XElement CreateExtensionElement(XElement child, string category)
        {
            XElement xExtension = new XElement(AppxManifest.AppxNamespace + "Extension",
                new XAttribute("Category", category)
                );

            if (child != null)
            {
                xExtension.Add(child);
            }

            return xExtension;
        }

        private ManifestItem CreateApplicationManifestItem(PackageItem item, string key, XElement data, bool singleton = true)
        {
            return this.CreateApplicationManifestItem(item, null, key, data, singleton, null, null, null);
        }

        private ManifestItem CreateApplicationManifestItem(PackageItem item, string extensionCategory, string key, XElement data, AppxLexicon.ApplicationExtensionItem extensionItem)
        {
            key = String.Concat(extensionCategory, ":", key ?? String.Empty);
            string path = (extensionItem.File == null) ? null : extensionItem.File.Path;
            return this.CreateApplicationManifestItem(item, extensionCategory, key, data, false, extensionItem.Implementation, path, extensionItem.RuntimeType);
        }

        private ManifestItem CreateApplicationManifestItem(PackageItem item, string extensionCategory, string key, XElement data, bool singleton, string extensionImplementation, string extensionFilePath, string runtimeType)
        {
            Application parentApplication;
            if (this.TryGetParentApplication(item, out parentApplication))
            {
                // If this is a singleton item in the application, append the application name.
                if (singleton)
                {
                    key = String.Concat(key, parentApplication.Name);
                }

                // If there is an extension category, wrap the data with an extension element and use that as the data for this item.
                if (!String.IsNullOrEmpty(extensionCategory))
                {
                    XElement xExtension = AppxManifest.CreateExtensionElement(data, extensionCategory);
                    this.SetEntryPoint(item, xExtension, extensionFilePath, extensionImplementation, runtimeType);
                    data = xExtension;
                }

                return new ManifestItem(key, item, data) { ParentApplication = parentApplication };
            }
            else
            {
                // TODO: display error that package item must be parented to an application.
                return null;
            }
        }

        private bool TryGetParentApplication(PackageItem item, out Application parentApplication)
        {
            parentApplication = null;

            Group group = item.Group;
            while (group != null && parentApplication == null)
            {
                parentApplication = group as Application;

                group = group.Group; // walk up the groups until we find a parent application or run out of parents.
            }

            return (parentApplication != null);
        }

        private void SetEntryPoint(PackageItem item, XElement xElement, string path, string implementation, string runtimeType)
        {
            string file = null;
            string fileExtension = null;

            // If the file was specified 
            if (!String.IsNullOrEmpty(path))
            {
                file = AppxManifest.StripRootFolderReference(path);
                fileExtension = IO.Path.GetExtension(path);

                if (fileExtension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    xElement.Add(new XAttribute("Executable", file));

                    if (String.IsNullOrEmpty(implementation))
                    {
                        this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.AttributeRequiresAttribute("File", "Implementation"), item));
                    }
                    else
                    {
                        xElement.Add(new XAttribute("EntryPoint", implementation));
                    }

                    if (!String.IsNullOrEmpty(runtimeType))
                    {
                        xElement.Add(new XAttribute("RuntimeType", runtimeType));
                    }
                }
                else // not an executable so we're going to assume it is a web application.
                {
                    xElement.Add(new XAttribute("StartPage", file));

                    if (!String.IsNullOrEmpty(implementation))
                    {
                        this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.WebAppDoesNotAllowAttribute("Implementation"), item));
                    }

                    if (!String.IsNullOrEmpty(runtimeType))
                    {
                        this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.WebAppDoesNotAllowAttribute("Implementation"), item));
                    }
                }
            }
            else if (!String.IsNullOrEmpty(implementation)) // implementation provided without a file.
            {
                xElement.Add(new XAttribute("EntryPoint", implementation));

                // Runtime can be specified with implementation
                if (!String.IsNullOrEmpty(runtimeType))
                {
                    xElement.Add(new XAttribute("RuntimeType", runtimeType));
                }
            }
            else if (!String.IsNullOrEmpty(runtimeType))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.AttributeRequiresAttribute("Implementation", "RuntimeType"), item));
            }
        }

        private bool TryParseClassElement(PackageItem item, Class cls, bool allowThreadingModel, out XElement xActivatableClass)
        {
            bool success = true;
            xActivatableClass = null;

            if (String.IsNullOrEmpty(cls.Id))
            {
                this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Class", "Id"), item));
                success = false;
            }

            xActivatableClass = new XElement(AppxManifest.AppxNamespace + "ActivatableClass",
                    new XAttribute("ActivatableClassId", cls.Id));

            if (allowThreadingModel)
            {
                string threadingModel = null;
                switch (cls.ThreadingModel)
                {
                    case ThreadingModelType.both:
                    case ThreadingModelType.neutral:
                        threadingModel = "both";
                        break;

                    case ThreadingModelType.mta:
                        threadingModel = "MTA";
                        break;

                    case ThreadingModelType.sta:
                        threadingModel = "STA";
                        break;
                }

                xActivatableClass.Add(new XAttribute("ThreadingModel", threadingModel));
            }

            foreach (ClassAttribute ca in cls.Attributes)
            {
                if (String.IsNullOrEmpty(ca.Name))
                {
                    this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("ClassAttribute", "Name"), item));
                    success = false;
                    continue;
                }

                string type = "string"; // assume we have a string value.
                int ignored;
                bool parsed = Int32.TryParse(ca.Value, out ignored);
                if (ca.Type == ClassAttributeType.Automatic && parsed)
                {
                    type = "integer";
                }
                else if (ca.Type == ClassAttributeType.Integer && !parsed)
                {
                    this.backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("ClassAttribute", "Type"), item));
                    success = false;
                    continue;
                }

                XElement xClassAttribute = new XElement(AppxManifest.AppxNamespace + "ActivatableClassAttribute",
                    new XAttribute("Name", ca.Name),
                    new XAttribute("Type", type),
                    new XAttribute("Value", ca.Value)
                    );
                xActivatableClass.Add(xClassAttribute);
            }

            return success;
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

        internal static string GetColor(string color)
        {
            string colorName = null;
            if (color.StartsWith("#", StringComparison.Ordinal))
            {
                color = color.Substring(1);
            }

            switch (color.ToLowerInvariant())
            {
                case "black":
                case "silver":
                case "gray":
                case "white":
                case "maroon":
                case "red":
                case "purple":
                case "fuchsia":
                case "green":
                case "lime":
                case "olive":
                case "yellow":
                case "navy":
                case "blue":
                case "teal":
                case "aqua":
                    colorName = color.ToLowerInvariant();
                    break;

                default:
                    int ignored;
                    if (Int32.TryParse(color, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ignored))
                    {
                        colorName = String.Concat("#", color);
                    }
                    break;
            }

            return colorName;
        }
    }
}
