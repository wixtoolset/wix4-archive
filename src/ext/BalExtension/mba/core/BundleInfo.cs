//-------------------------------------------------------------------------------------------------
// <copyright file="BundleInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bootstrapper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;

    public class BundleInfo
    {
        public bool PerMachine { get; internal set; }
        public string Name { get; internal set; }
        public string LogVariable { get; internal set; }
        public IDictionary<string, PackageInfo> Packages { get; internal set; }

        internal BundleInfo()
        {
            this.Packages = new Dictionary<string, PackageInfo>();
        }

        public void AddRelatedBundleAsPackage(DetectRelatedBundleEventArgs e)
        {
            PackageInfo package = PackageInfo.GetRelatedBundleAsPackage(e.ProductCode, e.RelationType, e.PerMachine);
            this.Packages.Add(package.Id, package);
        }

        public static BundleInfo ParseBundleFromStream(Stream stream)
        {
            XPathDocument manifest = new XPathDocument(stream);
            XPathNavigator root = manifest.CreateNavigator();
            return ParseBundleFromXml(root);
        }

        public static BundleInfo ParseBundleFromXml(XPathNavigator root)
        {
            BundleInfo bundle = new BundleInfo();

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(root.NameTable);
            namespaceManager.AddNamespace("p", BootstrapperApplicationData.XMLNamespace);
            XPathNavigator bundleNode = root.SelectSingleNode("/p:BootstrapperApplicationData/p:WixBundleProperties", namespaceManager);

            if (bundleNode == null)
            {
                throw new Exception("Failed to select bundle information.");
            }

            bool? perMachine = BootstrapperApplicationData.GetYesNoAttribute(bundleNode, "PerMachine");
            if (perMachine.HasValue)
            {
                bundle.PerMachine = perMachine.Value;
            }

            bundle.Name = BootstrapperApplicationData.GetAttribute(bundleNode, "DisplayName");

            bundle.LogVariable = BootstrapperApplicationData.GetAttribute(bundleNode, "LogPathVariable");

            foreach (PackageInfo package in PackageInfo.ParsePackagesFromXml(root))
            {
                bundle.Packages.Add(package.Id, package);
            }

            return bundle;
        }
    }
}
