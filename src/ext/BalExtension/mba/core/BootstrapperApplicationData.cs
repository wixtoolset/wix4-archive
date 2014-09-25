//-------------------------------------------------------------------------------------------------
// <copyright file="BootstrapperApplicationData.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------

namespace WixToolset.Bootstrapper
{
    using System;
    using System.IO;
    using System.Xml.XPath;

    public class BootstrapperApplicationData
    {
        public const string DefaultFileName = "BootstrapperApplicationData.xml";
        public const string XMLNamespace = "http://wixtoolset.org/schemas/v4/2010/BootstrapperApplicationData";

        public static readonly DirectoryInfo DefaultFolder;
        public static readonly FileInfo DefaultFile;

        static BootstrapperApplicationData()
        {
            DefaultFolder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            DefaultFile = new FileInfo(Path.Combine(DefaultFolder.FullName, DefaultFileName));
        }

        public FileInfo BADataFile { get; private set; }

        public BundleInfo Bundle { get; private set; }

        public BootstrapperApplicationData() : this(DefaultFile) { }

        public BootstrapperApplicationData(FileInfo baDataFile)
        {
            this.BADataFile = baDataFile;

            using (FileStream fs = this.BADataFile.OpenRead())
            {
                this.Bundle = BundleInfo.ParseBundleFromStream(fs);
            }
        }

        public static string GetAttribute(XPathNavigator node, string attributeName)
        {
            XPathNavigator attribute = node.SelectSingleNode("@" + attributeName);

            if (attribute == null)
            {
                return null;
            }

            return attribute.Value;
        }

        public static bool? GetYesNoAttribute(XPathNavigator node, string attributeName)
        {
            string attributeValue = GetAttribute(node, attributeName);

            if (attributeValue == null)
            {
                return null;
            }

            return attributeValue.Equals("yes", StringComparison.InvariantCulture);
        }
    }
}
