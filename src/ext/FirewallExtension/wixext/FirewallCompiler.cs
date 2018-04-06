// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Firewall Extension.
    /// </summary>
    public sealed class FirewallCompiler : CompilerExtension
    {
        /// <summary>
        /// Instantiate a new FirewallCompiler.
        /// </summary>
        public FirewallCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/firewall";
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
                case "File":
                    string fileId = context["FileId"];
                    string fileComponentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "FirewallException":
                            this.ParseFirewallExceptionElement(element, fileComponentId, fileId);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Component":
                    string componentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "FirewallException":
                            this.ParseFirewallExceptionElement(element, componentId, null);
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
        /// Parses a FirewallException element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="componentId">Identifier of the component that owns this firewall exception.</param>
        /// <param name="fileId">The file identifier of the parent element (null if nested under Component).</param>
        private void ParseFirewallExceptionElement(XElement node, string componentId, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string name = null;
            int attributes = 0;
            string file = null;
            string program = null;
            string port = null;
            string protocolValue = null;
            int protocol = CompilerConstants.IntegerNotSet;
            string profileValue = null;
            int profile = CompilerConstants.IntegerNotSet;
            string scope = null;
            string remoteAddresses = null;
            string description = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            if (null != fileId)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "File", "File"));
                            }
                            else
                            {
                                file = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "IgnoreFailure":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1; // feaIgnoreFailures
                            }
                            break;
                        case "Program":
                            if (null != fileId)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "Program", "File"));
                            }
                            else
                            {
                                program = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "Port":
                            port = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Protocol":
                            protocolValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (protocolValue)
                            {
                                case "tcp":
                                    protocol = FirewallConstants.NET_FW_IP_PROTOCOL_TCP;
                                    break;
                                case "udp":
                                    protocol = FirewallConstants.NET_FW_IP_PROTOCOL_UDP;
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Protocol", protocolValue, "tcp", "udp"));
                                    break;
                            }
                            break;
                        case "Scope":
                            scope = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (scope)
                            {
                                case "any":
                                    remoteAddresses = "*";
                                    break;
                                case "localSubnet":
                                    remoteAddresses = "LocalSubnet";
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Scope", scope, "any", "localSubnet"));
                                    break;
                            }
                            break;
                        case "Profile":
                            profileValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (profileValue)
                            {
                                case "domain":
                                    profile = FirewallConstants.NET_FW_PROFILE2_DOMAIN;
                                    break;
                                case "private":
                                    profile = FirewallConstants.NET_FW_PROFILE2_PRIVATE;
                                    break;
                                case "public":
                                    profile = FirewallConstants.NET_FW_PROFILE2_PUBLIC;
                                    break;
                                case "all":
                                    profile = FirewallConstants.NET_FW_PROFILE2_ALL;
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Profile", profileValue, "domain", "private", "public", "all"));
                                    break;
                            }
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            // parse RemoteAddress children
            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "RemoteAddress":
                            if (null != scope)
                            {
                                this.Core.OnMessage(FirewallErrors.IllegalRemoteAddressWithScopeAttribute(sourceLineNumbers));
                            }
                            else
                            {
                                this.ParseRemoteAddressElement(child, ref remoteAddresses);
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            // Id and Name are required
            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            // Scope or child RemoteAddress(es) are required
            if (null == remoteAddresses)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributeOrElement(sourceLineNumbers, node.Name.LocalName, "Scope", "RemoteAddress"));
            }

            // can't have both Program and File
            if (null != program && null != file)
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "File", "Program"));
            }

            // must be nested under File, have File or Program attributes, or have Port attribute
            if (String.IsNullOrEmpty(fileId) && String.IsNullOrEmpty(file) && String.IsNullOrEmpty(program) && String.IsNullOrEmpty(port))
            {
                this.Core.OnMessage(FirewallErrors.NoExceptionSpecified(sourceLineNumbers));
            }

            if (!this.Core.EncounteredError)
            {
                // at this point, File attribute and File parent element are treated the same
                if (null != file)
                {
                    fileId = file;
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixFirewallException");
                row[0] = id;
                row[1] = name;
                row[2] = remoteAddresses;

                if (!String.IsNullOrEmpty(port))
                {
                    row[3] = port;

                    if (CompilerConstants.IntegerNotSet == protocol)
                    {
                        // default protocol is "TCP"
                        protocol = FirewallConstants.NET_FW_IP_PROTOCOL_TCP;
                    }
                }

                if (CompilerConstants.IntegerNotSet != protocol)
                {
                    row[4] = protocol;
                }

                if (!String.IsNullOrEmpty(fileId))
                {
                    row[5] = String.Format(CultureInfo.InvariantCulture, "[#{0}]", fileId);
                    this.Core.CreateSimpleReference(sourceLineNumbers, "File", fileId);
                }
                else if (!String.IsNullOrEmpty(program))
                {
                    row[5] = program;
                }

                if (CompilerConstants.IntegerNotSet != attributes)
                {
                    row[6] = attributes;
                }

                // Default is "all"
                row[7] = CompilerConstants.IntegerNotSet == profile ? FirewallConstants.NET_FW_PROFILE2_ALL : profile;

                row[8] = componentId;

                row[9] = description;

                if (this.Core.CurrentPlatform == Platform.ARM)
                {
                    // Ensure ARM version of the CA is referenced
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixSchedFirewallExceptionsInstall_ARM");
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixSchedFirewallExceptionsUninstall_ARM");
                }
                else
                {
                    // All other supported platforms use x86
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixSchedFirewallExceptionsInstall");
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixSchedFirewallExceptionsUninstall");
                }
            }
        }

        /// <summary>
        /// Parses a RemoteAddress element
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseRemoteAddressElement(XElement node, ref string remoteAddresses)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            // no attributes
            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    this.Core.UnexpectedAttribute(node, attrib);
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            string address = this.Core.GetTrimmedInnerText(node);
            if (String.IsNullOrEmpty(address))
            {
                this.Core.OnMessage(FirewallErrors.IllegalEmptyRemoteAddress(sourceLineNumbers));
            }
            else
            {
                if (String.IsNullOrEmpty(remoteAddresses))
                {
                    remoteAddresses = address;
                }
                else
                {
                    remoteAddresses = String.Concat(remoteAddresses, ",", address);
                }
            }
        }
    }
}
