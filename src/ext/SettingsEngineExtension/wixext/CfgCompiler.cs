// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the Windows Installer XML Toolset Cfg Extension.
    /// </summary>
    public sealed class CfgCompiler : CompilerExtension
    {
        internal const int MsidbRegistryRootLocalMachine = 2;
        internal const int MsidbComponentAttributesRegistryKeyPath = 4;

        /// <summary>
        /// Instantiate a new CfgCompiler.
        /// </summary>
        public CfgCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/settingsengine";
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Module":
                case "Product":
                case "Fragment":
                    switch (element.Name.LocalName)
                    {
                        case "Product":
                            this.ParseCfgProductElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        /// <summary>
        /// Parses a CfgException element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseCfgProductElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string version = null;
            string publickey = null;
            string feature = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PublicKey":
                            publickey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Feature":
                            feature = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            // Id and Name are required
            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == version)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            if (null == publickey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PublicKey"));
            }

            if (null == feature)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Feature"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                string componentGuid = this.Core.CreateGuid(CfgConstants.wixCfgGuidNamespace, name + "_" + version + "_" + publickey);
                string componentId = "Cfg_" + componentGuid.Substring(1, componentGuid.Length - 2).Replace("-", "_"); // Cutoff the curly braces and switch dashes to underscrores to get the componentID
                string regId = "Reg_" + componentId;

                Row componentRow = this.Core.CreateRow(sourceLineNumbers, "Component");
                componentRow[0] = componentId;
                componentRow[1] = componentGuid;
                componentRow[2] = "TARGETDIR";
                componentRow[3] = MsidbComponentAttributesRegistryKeyPath;
                componentRow[4] = "";
                componentRow[5] = regId;

                Row featureComponentRow = this.Core.CreateRow(sourceLineNumbers, "FeatureComponents");
                featureComponentRow[0] = feature;
                featureComponentRow[1] = componentId;

                Row cfgRow = this.Core.CreateRow(sourceLineNumbers, "WixCfgProducts");
                cfgRow[0] = name;
                cfgRow[1] = version;
                cfgRow[2] = publickey;
                cfgRow[3] = componentId;

                Row regRow = this.Core.CreateRow(sourceLineNumbers, "Registry");
                regRow[0] = regId;
                regRow[1] = MsidbRegistryRootLocalMachine;
                regRow[2] = "SOFTWARE\\Wix\\SettingsStore\\Products";
                regRow[3] = name + ", " + version + ", " + publickey;
                regRow[4] = "#1";
                regRow[5] = componentId;

                this.Core.CreateSimpleReference(sourceLineNumbers, "Feature", feature);

                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedCfgProductsInstall");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedCfgProductsUninstall");
            }
        }
    }
}
