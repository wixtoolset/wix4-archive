//-------------------------------------------------------------------------------------------------
// <copyright file="Model.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The model.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.UX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Xml.Linq;
    using WixToolset.Bootstrapper;

    /// <summary>
    /// The model.
    /// </summary>
    public class Model
    {
        private static readonly XNamespace BootstrapperApplicationDataNamespace = "http://wixtoolset.org/schemas/v4/2010/BootstrapperApplicationData";
        private const string BurnBundleInstallDirectoryVariable = "InstallFolder";
        private const string BurnBundleLayoutDirectoryVariable = "WixBundleLayoutDirectory";

        /// <summary>
        /// Creates a new model for the UX.
        /// </summary>
        /// <param name="bootstrapper">Bootstrapper hosting the UX.</param>
        public Model(BootstrapperApplication bootstrapper)
        {
            this.Bootstrapper = bootstrapper;
            this.PackageDisplayNames = new Dictionary<string, string>();
            this.Telemetry = new List<KeyValuePair<string, string>>();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string assemblyLocation = assembly.Location;
            string folder = Path.GetDirectoryName(assemblyLocation);

            try
            {
                XDocument document = XDocument.Load(Path.Combine(folder, "BootstrapperApplicationData.xml"));

                foreach (var packageProperties in document.Root.Descendants(BootstrapperApplicationDataNamespace + "WixPackageProperties"))
                {
                    this.PackageDisplayNames.Add(packageProperties.Attribute("Package").Value, packageProperties.Attribute("DisplayName").Value);
                }
            }
            catch
            {
                // Catching all exceptions is generally poor form but we **really** don't care if the package display names can't be loaded.
            }

            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assemblyLocation);

            this.Version = new Version(fileVersion.FileVersion);
        }

        /// <summary>
        /// Gets the bootstrapper.
        /// </summary>
        public BootstrapperApplication Bootstrapper { get; private set; }

        /// <summary>
        /// Gets the bootstrapper command-line.
        /// </summary>
        public Command Command { get { return this.Bootstrapper.Command; } }

        /// <summary>
        /// Gets the bootstrapper engine.
        /// </summary>
        public Engine Engine { get { return this.Bootstrapper.Engine; } }

        /// <summary>
        /// Gets the key/value pairs used in telemetry.
        /// </summary>
        public List<KeyValuePair<string, string>> Telemetry { get; private set; }

        /// <summary>
        /// Get or set the final result of the installation.
        /// </summary>
        public int Result { get; set; }

        /// <summary>
        /// Get the version of the install.
        /// </summary>
        public Version Version { get; private set; }

        /// <summary>
        /// Get or set the path where the bundle is installed.
        /// </summary>
        public string InstallDirectory
        {
            get
            {
                if (!this.Engine.StringVariables.Contains(BurnBundleInstallDirectoryVariable))
                {
                    return null;
                }

                return this.Engine.StringVariables[BurnBundleInstallDirectoryVariable];
            }

            set
            {
                this.Engine.StringVariables[BurnBundleInstallDirectoryVariable] = value;
            }
        }

        /// <summary>
        /// Get or set the path for the layout to be created.
        /// </summary>
        public string LayoutDirectory
        {
            get
            {
                if (!this.Engine.StringVariables.Contains(BurnBundleLayoutDirectoryVariable))
                {
                    return null;
                }

                return this.Engine.StringVariables[BurnBundleLayoutDirectoryVariable];
            }

            set
            {
                this.Engine.StringVariables[BurnBundleLayoutDirectoryVariable] = value;
            }
        }

        public LaunchAction PlannedAction { get; set; }

        private Dictionary<string, string> PackageDisplayNames { get; set; }

        /// <summary>
        /// Creates a correctly configured HTTP web request.
        /// </summary>
        /// <param name="uri">URI to connect to.</param>
        /// <returns>Correctly configured HTTP web request.</returns>
        public HttpWebRequest CreateWebRequest(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = String.Concat("WixInstall", this.Version.ToString());

            return request;
        }

        /// <summary>
        /// Gets the display name for a pckage if possible.
        /// </summary>
        /// <param name="packageId">Identity of the package to find the display name.</param>
        /// <returns>Display name of the package if found or the package id if not.</returns>
        public string GetPackageName(string packageId)
        {
            string displayName;

            return this.PackageDisplayNames.TryGetValue(packageId, out displayName) ? displayName : packageId;
        }
    }
}
