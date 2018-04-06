// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset .NET Framework Extension.
    /// </summary>
    public sealed class NetFxCompiler : CompilerExtension
    {
        /// <summary>
        /// Instantiate a new NetFxCompiler.
        /// </summary>
        public NetFxCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/netfx";
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
                case "File":
                    string fileId = context["FileId"];

                    switch (element.Name.LocalName)
                    {
                        case "NativeImage":
                            this.ParseNativeImageElement(element, fileId);
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
        /// Parses a NativeImage element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="fileId">The file identifier of the parent element.</param>
        private void ParseNativeImageElement(XElement node, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string appBaseDirectory = null;
            string assemblyApplication = null;
            int attributes = 0x8; // 32bit is on by default
            int priority = 3;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "AppBaseDirectory":
                            appBaseDirectory = this.Core.GetAttributeValue(sourceLineNumbers, attrib);

                            // See if a formatted value is specified.
                            if (-1 == appBaseDirectory.IndexOf("[", StringComparison.Ordinal))
                            {
                                this.Core.CreateSimpleReference(sourceLineNumbers, "Directory", appBaseDirectory);
                            }
                            break;
                        case "AssemblyApplication":
                            assemblyApplication = this.Core.GetAttributeValue(sourceLineNumbers, attrib);

                            // See if a formatted value is specified.
                            if (-1 == assemblyApplication.IndexOf("[", StringComparison.Ordinal))
                            {
                                this.Core.CreateSimpleReference(sourceLineNumbers, "File", assemblyApplication);
                            }
                            break;
                        case "Debug":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1;
                            }
                            break;
                        case "Dependencies":
                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x2;
                            }
                            break;
                        case "Platform":
                            string platformValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < platformValue.Length)
                            {
                                switch (platformValue)
                                {
                                    case "32bit":
                                        // 0x8 is already on by default
                                        break;
                                    case "64bit":
                                        attributes &= ~0x8;
                                        attributes |= 0x10;
                                        break;
                                    case "all":
                                        attributes |= 0x10;
                                        break;
                                }
                            }
                            break;
                        case "Priority":
                            priority = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 3);
                            break;
                        case "Profile":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x4;
                            }
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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "NetFxScheduleNativeImage");

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "NetFxNativeImage");
                row[0] = id;
                row[1] = fileId;
                row[2] = priority;
                row[3] = attributes;
                row[4] = assemblyApplication;
                row[5] = appBaseDirectory;
            }
        }
    }
}
