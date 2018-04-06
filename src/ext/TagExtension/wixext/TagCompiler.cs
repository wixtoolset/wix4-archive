// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Software Id Tag Extension.
    /// </summary>
    public sealed class TagCompiler : CompilerExtension
    {
        /// <summary>
        /// Instantiate a new GamingCompiler.
        /// </summary>
        public TagCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/tag";
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                    switch (element.Name.LocalName)
                    {
                        case "Tag":
                            this.ParseBundleTagElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "Tag":
                            this.ParseProductTagElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "PatchFamily":
                    switch (element.Name.LocalName)
                    {
                        case "TagRef":
                            this.ParseTagRefElement(element);
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
        /// Parses a Tag element for Software Id Tag registration under a Bundle element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseBundleTagElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string regid = null;
            YesNoType licensed = YesNoType.NotSet;
            string type = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Licensed":
                            licensed = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            type = this.ParseTagTypeAttribute(sourceLineNumbers, node, attrib);
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

            this.Core.ParseForExtensionElements(node);

            if (String.IsNullOrEmpty(name))
            {
                XAttribute productNameAttribute = node.Parent.Attribute("Name");
                if (null != productNameAttribute)
                {
                    name = productNameAttribute.Value;
                }
                else
                {
                    this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
            }

            if (!String.IsNullOrEmpty(name) && !this.Core.IsValidLongFilename(name, false))
            {
                this.Core.OnMessage(TagErrors.IllegalName(sourceLineNumbers, node.Parent.Name.LocalName, name));
            }

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }

            if (!this.Core.EncounteredError)
            {
                string fileName = String.Concat(regid, " ", name, ".swidtag");

                Row tagRow = this.Core.CreateRow(sourceLineNumbers, "WixBundleTag");
                tagRow[0] = fileName;
                tagRow[1] = regid;
                tagRow[2] = name;
                if (YesNoType.Yes == licensed)
                {
                    tagRow[3] = 1;
                }
                // field 4 is the TagXml set by the binder.
                tagRow[5] = type;
            }
        }

        /// <summary>
        /// Parses a Tag element for Software Id Tag registration under a Product element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseProductTagElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;
            string regid = null;
            string feature = "WixSwidTag";
            YesNoType licensed = YesNoType.NotSet;
            string type = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false);
                            break;
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Feature":
                            feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Licensed":
                            licensed = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            type = this.ParseTagTypeAttribute(sourceLineNumbers, node, attrib);
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

            this.Core.ParseForExtensionElements(node);

            if (String.IsNullOrEmpty(name))
            {
                XAttribute productNameAttribute = node.Parent.Attribute("Name");
                if (null != productNameAttribute)
                {
                    name = productNameAttribute.Value;
                }
                else
                {
                    this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
                }
            }

            if (!String.IsNullOrEmpty(name) && !this.Core.IsValidLongFilename(name, false))
            {
                this.Core.OnMessage(TagErrors.IllegalName(sourceLineNumbers, node.Parent.Name.LocalName, name));
            }

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }

            if (!this.Core.EncounteredError)
            {
                string directoryId = "WixTagRegidFolder";
                Identifier fileId = this.Core.CreateIdentifier("tag", regid, ".product.tag");
                string fileName = String.Concat(regid, " ", name, ".swidtag");
                string shortName = this.Core.CreateShortName(fileName, false, false);

                this.Core.CreateSimpleReference(sourceLineNumbers, "Directory", directoryId);

                ComponentRow componentRow = (ComponentRow)this.Core.CreateRow(sourceLineNumbers, "Component", fileId);
                componentRow.Guid = "*";
                componentRow[3] = 0;
                componentRow.Directory = directoryId;
                componentRow.IsLocalOnly = true;
                componentRow.KeyPath = fileId.Id;

                this.Core.CreateSimpleReference(sourceLineNumbers, "Feature", feature);
                this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, null, ComplexReferenceChildType.Component, fileId.Id, true);

                FileRow fileRow = (FileRow)this.Core.CreateRow(sourceLineNumbers, "File", fileId);
                fileRow.Component = fileId.Id;
                fileRow.FileName = String.Concat(shortName, "|", fileName);

                WixFileRow wixFileRow = (WixFileRow)this.Core.CreateRow(sourceLineNumbers, "WixFile");
                wixFileRow.Directory = directoryId;
                wixFileRow.File = fileId.Id;
                wixFileRow.DiskId = 1;
                wixFileRow.Attributes = 1;
                wixFileRow.Source = String.Concat("%TEMP%\\", fileName);

                this.Core.EnsureTable(sourceLineNumbers, "SoftwareIdentificationTag");
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixProductTag");
                row[0] = fileId.Id;
                row[1] = regid;
                row[2] = name;
                if (YesNoType.Yes == licensed)
                {
                    row[3] = 1;
                }
                row[4] = type;

                this.Core.CreateSimpleReference(sourceLineNumbers, "File", fileId.Id);
            }
        }

        /// <summary>
        /// Parses a TagRef element for Software Id Tag registration under a PatchFamily element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseTagRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string regid = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Regid":
                            regid = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            this.Core.ParseForExtensionElements(node);

            if (String.IsNullOrEmpty(regid))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Regid"));
            }

            if (!this.Core.EncounteredError)
            {
                Identifier id = this.Core.CreateIdentifier("tag", regid, ".product.tag");
                this.Core.CreatePatchFamilyChildReference(sourceLineNumbers, "Component", id.Id);
            }
        }

        private string ParseTagTypeAttribute(SourceLineNumber sourceLineNumbers, XElement node, XAttribute attrib)
        {
            string typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
            switch (typeValue)
            {
                case "application":
                    typeValue = "Application";
                    break;
                case "component":
                    typeValue = "Component";
                    break;
                case "feature":
                    typeValue = "Feature";
                    break;
                case "group":
                    typeValue = "Group";
                    break;
                case "patch":
                    typeValue = "Patch";
                    break;
                default:
                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, typeValue, "application", "component", "feature", "group", "patch"));
                    break;
            }

            return typeValue;
        }
    }
}
