//-------------------------------------------------------------------------------------------------
// <copyright file="UtilCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The compiler for the WiX Toolset Utility Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Util = WixToolset.Extensions.Serialize.Util;

    /// <summary>
    /// The compiler for the WiX Toolset Utility Extension.
    /// </summary>
    public sealed class UtilCompiler : CompilerExtension
    {
        // user creation attributes definitions (from sca.h)
        internal const int UserDontExpirePasswrd = 0x00000001;
        internal const int UserPasswdCantChange = 0x00000002;
        internal const int UserPasswdChangeReqdOnLogin = 0x00000004;
        internal const int UserDisableAccount = 0x00000008;
        internal const int UserFailIfExists = 0x00000010;
        internal const int UserUpdateIfExists = 0x00000020;
        internal const int UserLogonAsService = 0x00000040;
        internal const int UserLogonAsBatchJob = 0x00000080;

        internal const int UserDontRemoveOnUninstall = 0x00000100;
        internal const int UserDontCreateUser = 0x00000200;
        internal const int UserNonVital = 0x00000400;

        [Flags]
        internal enum WixFileSearchAttributes
        {
            Default = 0x001,
            MinVersionInclusive = 0x002,
            MaxVersionInclusive = 0x004,
            MinSizeInclusive = 0x008,
            MaxSizeInclusive = 0x010,
            MinDateInclusive = 0x020,
            MaxDateInclusive = 0x040,
            WantVersion = 0x080,
            WantExists = 0x100,
            IsDirectory = 0x200,
        }

        internal enum WixRegistrySearchFormat
        {
            Raw,
            Compatible,
        }

        [Flags]
        internal enum WixRegistrySearchAttributes
        {
            Raw = 0x01,
            Compatible = 0x02,
            ExpandEnvironmentVariables = 0x04,
            WantValue = 0x08,
            WantExists = 0x10,
            Win64 = 0x20,
        }

        internal enum WixComponentSearchAttributes
        {
            KeyPath = 0x1,
            State = 0x2,
            WantDirectory = 0x4,
        }

        [Flags]
        internal enum WixProductSearchAttributes
        {
            Version = 0x01,
            Language = 0x02,
            State = 0x04,
            Assignment = 0x08,
            UpgradeCode = 0x10,
        }

        internal enum WixRestartResourceAttributes
        {
            Filename = 1,
            ProcessName,
            ServiceName,
            TypeMask = 0xf,
        }

        internal enum WixRemoveFolderExOn
        {
            Install = 1,
            Uninstall = 2,
            Both = 3,
        }

        private static readonly Regex FindPropertyBrackets = new Regex(@"\[(?!\\|\])|(?<!\[\\\]|\[\\|\\\[)\]", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        /// <summary>
        /// Instantiate a new UtilCompiler.
        /// </summary>
        public UtilCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/util";
        }

        /// <summary>
        /// Types of Internet shortcuts.
        /// </summary>
        public enum InternetShortcutType
        {
            /// <summary>Create a .lnk file.</summary>
            Link = 0,

            /// <summary>Create a .url file.</summary>
            Url,
        }

        /// <summary>
        /// Types of permission setting methods.
        /// </summary>
        private enum PermissionType
        {
            /// <summary>LockPermissions (normal) type permission setting.</summary>
            LockPermissions,

            /// <summary>FileSharePermissions type permission setting.</summary>
            FileSharePermissions,

            /// <summary>SecureObjects type permission setting.</summary>
            SecureObjects,
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            this.ParsePossibleKeyPathElement(parentElement, element, context);
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override ComponentKeyPath ParsePossibleKeyPathElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            ComponentKeyPath possibleKeyPath = null;

            switch (parentElement.Name.LocalName)
            {
                case "CreateFolder":
                    string createFolderId = context["DirectoryId"];
                    string createFolderComponentId = context["ComponentId"];

                    // If this doesn't parse successfully, something really odd is going on, so let the exception get thrown
                    bool createFolderWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "PermissionEx":
                            this.ParsePermissionExElement(element, createFolderId, createFolderComponentId, createFolderWin64, "CreateFolder");
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Component":
                    string componentId = context["ComponentId"];
                    string directoryId = context["DirectoryId"];
                    bool componentWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "EventSource":
                            possibleKeyPath = this.ParseEventSourceElement(element, componentId);
                            break;
                        case "FileShare":
                            this.ParseFileShareElement(element, componentId, directoryId);
                            break;
                        case "InternetShortcut":
                            this.ParseInternetShortcutElement(element, componentId, directoryId);
                            break;
                        case "PerformanceCategory":
                            this.ParsePerformanceCategoryElement(element, componentId);
                            break;
                        case "RemoveFolderEx":
                            this.ParseRemoveFolderExElement(element, componentId);
                            break;
                        case "RestartResource":
                            this.ParseRestartResourceElement(element, componentId);
                            break;
                        case "ServiceConfig":
                            this.ParseServiceConfigElement(element, componentId, "Component", null);
                            break;
                        case "TouchFile":
                            this.ParseTouchFileElement(element, componentId, componentWin64);
                            break;
                        case "User":
                            this.ParseUserElement(element, componentId);
                            break;
                        case "XmlFile":
                            this.ParseXmlFileElement(element, componentId);
                            break;
                        case "XmlConfig":
                            this.ParseXmlConfigElement(element, componentId, false);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "File":
                    string fileId = context["FileId"];
                    string fileComponentId = context["ComponentId"];

                    // If this doesn't parse successfully, something really odd is going on, so let the exception get thrown
                    bool fileWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "PerfCounter":
                            this.ParsePerfCounterElement(element, fileComponentId, fileId);
                            break;
                        case "PermissionEx":
                            this.ParsePermissionExElement(element, fileId, fileComponentId, fileWin64, "File");
                            break;
                        case "PerfCounterManifest":
                            this.ParsePerfCounterManifestElement(element, fileComponentId, fileId);
                            break;
                        case "EventManifest":
                            this.ParseEventManifestElement(element, fileComponentId, fileId);
                            break;
                        case "FormatFile":
                            this.ParseFormatFileElement(element, fileId, fileWin64);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Bundle":
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "CloseApplication":
                            this.ParseCloseApplicationElement(element);
                            break;
                        case "Group":
                            this.ParseGroupElement(element, null);
                            break;
                        case "RestartResource":
                            // Currently not supported for Bundles.
                            if (parentElement.Name.LocalName != "Bundle")
                            {
                                this.ParseRestartResourceElement(element, null);
                            }
                            else
                            {
                                this.Core.UnexpectedElement(parentElement, element);
                            }
                            break;
                        case "User":
                            this.ParseUserElement(element, null);
                            break;
                        case "ComponentSearch":
                        case "ComponentSearchRef":
                        case "DirectorySearch":
                        case "DirectorySearchRef":
                        case "FileSearch":
                        case "FileSearchRef":
                        case "ProductSearch":
                        case "ProductSearchRef":
                        case "RegistrySearch":
                        case "RegistrySearchRef":
                            // These will eventually be supported under Module/Product, but are not yet.
                            if (parentElement.Name.LocalName == "Bundle" || parentElement.Name.LocalName == "Fragment")
                            {
                                // TODO: When these are supported by all section types, move
                                // these out of the nested switch and back into the surrounding one.
                                switch (element.Name.LocalName)
                                {
                                    case "ComponentSearch":
                                        this.ParseComponentSearchElement(element);
                                        break;
                                    case "ComponentSearchRef":
                                        this.ParseComponentSearchRefElement(element);
                                        break;
                                    case "DirectorySearch":
                                        this.ParseDirectorySearchElement(element);
                                        break;
                                    case "DirectorySearchRef":
                                        this.ParseWixSearchRefElement(element);
                                        break;
                                    case "FileSearch":
                                        this.ParseFileSearchElement(element);
                                        break;
                                    case "FileSearchRef":
                                        this.ParseWixSearchRefElement(element);
                                        break;
                                    case "ProductSearch":
                                        this.ParseProductSearchElement(element);
                                        break;
                                    case "ProductSearchRef":
                                        this.ParseWixSearchRefElement(element);
                                        break;
                                    case "RegistrySearch":
                                        this.ParseRegistrySearchElement(element);
                                        break;
                                    case "RegistrySearchRef":
                                        this.ParseWixSearchRefElement(element);
                                        break;
                                }
                            }
                            else
                            {
                                this.Core.UnexpectedElement(parentElement, element);
                            }
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Registry":
                case "RegistryKey":
                case "RegistryValue":
                    string registryId = context["RegistryId"];
                    string registryComponentId = context["ComponentId"];

                    // If this doesn't parse successfully, something really odd is going on, so let the exception get thrown
                    bool registryWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "PermissionEx":
                            this.ParsePermissionExElement(element, registryId, registryComponentId, registryWin64, "Registry");
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "ServiceInstall":
                    string serviceInstallId = context["ServiceInstallId"];
                    string serviceInstallName = context["ServiceInstallName"];
                    string serviceInstallComponentId = context["ServiceInstallComponentId"];

                    // If this doesn't parse successfully, something really odd is going on, so let the exception get thrown
                    bool serviceInstallWin64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "PermissionEx":
                            this.ParsePermissionExElement(element, serviceInstallId, serviceInstallComponentId, serviceInstallWin64, "ServiceInstall");
                            break;
                        case "ServiceConfig":
                            this.ParseServiceConfigElement(element, serviceInstallComponentId, "ServiceInstall", serviceInstallName);
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

            return possibleKeyPath;
        }

        /// <summary>
        /// Parses the common search attributes shared across all searches.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="attrib">Attribute to parse.</param>
        /// <param name="id">Value of the Id attribute.</param>
        /// <param name="variable">Value of the Variable attribute.</param>
        /// <param name="condition">Value of the Condition attribute.</param>
        /// <param name="after">Value of the After attribute.</param>
        private void ParseCommonSearchAttributes(SourceLineNumber sourceLineNumbers, XAttribute attrib, ref Identifier id, ref string variable, ref string condition, ref string after)
        {
            switch (attrib.Name.LocalName)
            {
                case "Id":
                    id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                    break;
                case "Variable":
                    variable = this.Core.GetAttributeBundleVariableValue(sourceLineNumbers, attrib);
                    break;
                case "Condition":
                    condition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                    break;
                case "After":
                    after = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }

        /// <summary>
        /// Parses a ComponentSearch element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseComponentSearchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string guid = null;
            string productCode = null;
            Util.ComponentSearch.ResultType result = Util.ComponentSearch.ResultType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Guid":
                            guid = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib);
                            break;
                        case "ProductCode":
                            productCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib);
                            break;
                        case "Result":
                            string resultValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Util.ComponentSearch.TryParseResultType(resultValue, out result))
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName,
                                    resultValue,
                                    Util.ComponentSearch.ResultType.directory.ToString(),
                                    Util.ComponentSearch.ResultType.state.ToString(),
                                    Util.ComponentSearch.ResultType.keyPath.ToString()));
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

            if (null == variable)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Variable"));
            }

            if (null == guid)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Guid"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("wcs", variable, condition, after, guid, productCode, result.ToString());
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.CreateWixSearchRow(sourceLineNumbers, id, variable, condition);
                if (after != null)
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "WixSearch", after);
                    // TODO: We're currently defaulting to "always run after", which we will need to change...
                    this.CreateWixSearchRelationRow(sourceLineNumbers, id, after, 2);
                }

                WixComponentSearchAttributes attributes = WixComponentSearchAttributes.KeyPath;
                switch (result)
                {
                    case Util.ComponentSearch.ResultType.directory:
                        attributes = WixComponentSearchAttributes.WantDirectory;
                        break;
                    case Util.ComponentSearch.ResultType.keyPath:
                        attributes = WixComponentSearchAttributes.KeyPath;
                        break;
                    case Util.ComponentSearch.ResultType.state:
                        attributes = WixComponentSearchAttributes.State;
                        break;
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixComponentSearch", id);
                row[1] = guid;
                row[2] = productCode;
                row[3] = (int)attributes;
            }
        }

        /// <summary>
        /// Parses a ComponentSearchRef element
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseComponentSearchRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string refId = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            refId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixComponentSearch", refId);
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
        }

        /// <summary>
        /// Parses an event source element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private ComponentKeyPath ParseEventSourceElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string sourceName = null;
            string logName = null;
            string categoryMessageFile = null;
            int categoryCount = CompilerConstants.IntegerNotSet;
            string eventMessageFile = null;
            string parameterMessageFile = null;
            int typesSupported = 0;
            bool isKeyPath = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "CategoryCount":
                            categoryCount = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "CategoryMessageFile":
                            categoryMessageFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EventMessageFile":
                            eventMessageFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "KeyPath":
                            isKeyPath = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Log":
                            logName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("Security" == logName)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, logName, "Application", "System", "<customEventLog>"));
                            }
                            break;
                        case "Name":
                            sourceName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParameterMessageFile":
                            parameterMessageFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SupportsErrors":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x01; // EVENTLOG_ERROR_TYPE
                            }
                            break;
                        case "SupportsFailureAudits":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x10; // EVENTLOG_AUDIT_FAILURE
                            }
                            break;
                        case "SupportsInformationals":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x04; // EVENTLOG_INFORMATION_TYPE
                            }
                            break;
                        case "SupportsSuccessAudits":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x08; // EVENTLOG_AUDIT_SUCCESS
                            }
                            break;
                        case "SupportsWarnings":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                typesSupported |= 0x02; // EVENTLOG_WARNING_TYPE
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

            if (null == sourceName)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == logName)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "EventLog"));
            }

            if (null == eventMessageFile)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "EventMessageFile"));
            }

            if (null == categoryMessageFile && 0 < categoryCount)
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "CategoryCount", "CategoryMessageFile"));
            }

            if (null != categoryMessageFile && CompilerConstants.IntegerNotSet == categoryCount)
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "CategoryMessageFile", "CategoryCount"));
            }

            this.Core.ParseForExtensionElements(node);

            int registryRoot = 2; // MsiInterop.MsidbRegistryRootLocalMachine 
            string eventSourceKey = String.Format(@"SYSTEM\CurrentControlSet\Services\EventLog\{0}\{1}", logName, sourceName);
            Identifier id = this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, eventSourceKey, "EventMessageFile", String.Concat("#%", eventMessageFile), componentId);

            if (null != categoryMessageFile)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, eventSourceKey, "CategoryMessageFile", String.Concat("#%", categoryMessageFile), componentId);
            }

            if (CompilerConstants.IntegerNotSet != categoryCount)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, eventSourceKey, "CategoryCount", String.Concat("#", categoryCount), componentId);
            }

            if (null != parameterMessageFile)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, eventSourceKey, "ParameterMessageFile", String.Concat("#%", parameterMessageFile), componentId);
            }

            if (0 != typesSupported)
            {
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, eventSourceKey, "TypesSupported", String.Concat("#", typesSupported), componentId);
            }

            return new ComponentKeyPath() { Id = id.Id, Explicit = isKeyPath, Type = ComponentKeyPathType.Registry };
        }

        /// <summary>
        /// Parses a close application element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseCloseApplicationElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string condition = null;
            string description = null;
            string target = null;
            string property = null;
            Identifier id = null;
            int attributes = 2; // default to CLOSEAPP_ATTRIBUTE_REBOOTPROMPT enabled
            int sequence = CompilerConstants.IntegerNotSet;
            int terminateExitCode = CompilerConstants.IntegerNotSet;
            int timeout = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Property":
                            property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Sequence":
                            sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Timeout":
                            timeout = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Target":
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CloseMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 1; // CLOSEAPP_ATTRIBUTE_CLOSEMESSAGE
                            }
                            else
                            {
                                attributes &= ~1; // CLOSEAPP_ATTRIBUTE_CLOSEMESSAGE
                            }
                            break;
                        case "EndSessionMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 8; // CLOSEAPP_ATTRIBUTE_ENDSESSIONMESSAGE
                            }
                            else
                            {
                                attributes &= ~8; // CLOSEAPP_ATTRIBUTE_ENDSESSIONMESSAGE
                            }
                            break;
                        case "PromptToContinue":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x40; // CLOSEAPP_ATTRIBUTE_PROMPTTOCONTINUE
                            }
                            else
                            {
                                attributes &= ~0x40; // CLOSEAPP_ATTRIBUTE_PROMPTTOCONTINUE
                            }
                            break;
                        case "RebootPrompt":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 2; // CLOSEAPP_ATTRIBUTE_REBOOTPROMPT
                            }
                            else
                            {
                                attributes &= ~2; // CLOSEAPP_ATTRIBUTE_REBOOTPROMPT
                            }
                            break;
                        case "ElevatedCloseMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 4; // CLOSEAPP_ATTRIBUTE_ELEVATEDCLOSEMESSAGE
                            }
                            else
                            {
                                attributes &= ~4; // CLOSEAPP_ATTRIBUTE_ELEVATEDCLOSEMESSAGE
                            }
                            break;
                        case "ElevatedEndSessionMessage":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x10; // CLOSEAPP_ATTRIBUTE_ELEVATEDENDSESSIONMESSAGE
                            }
                            else
                            {
                                attributes &= ~0x10; // CLOSEAPP_ATTRIBUTE_ELEVATEDENDSESSIONMESSAGE
                            }
                            break;
                        case "TerminateProcess":
                            terminateExitCode = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            attributes |= 0x20; // CLOSEAPP_ATTRIBUTE_TERMINATEPROCESS
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

            if (null == target)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Target"));
            }
            else if (null == id)
            {
                id = this.Core.CreateIdentifier("ca", target);
            }

            if (String.IsNullOrEmpty(description) && 0x40 == (attributes & 0x40))
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name.LocalName, "PromptToContinue", "yes", "Description"));
            }

            if (0x22 == (attributes & 0x22))
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "TerminateProcess", "RebootPrompt", "yes"));
            }

            // get the condition from the inner text of the element
            condition = this.Core.GetConditionInnerText(node);

            this.Core.ParseForExtensionElements(node);

            // Reference CustomAction since nothing will happen without it
            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CA is referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixCloseApplications_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixCloseApplications");
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixCloseApplication", id);
                row[1] = target;
                row[2] = description;
                row[3] = condition;
                row[4] = attributes;
                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    row[5] = sequence;
                }
                row[6] = property;
                if (CompilerConstants.IntegerNotSet != terminateExitCode)
                {
                    row[7] = terminateExitCode;
                }
                if (CompilerConstants.IntegerNotSet != timeout)
                {
                    row[8] = timeout * 1000; // make the timeout milliseconds in the table.
                }
            }
        }

        /// <summary>
        /// Parses a DirectorySearch element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseDirectorySearchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string path = null;
            Util.DirectorySearch.ResultType result = Util.DirectorySearch.ResultType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Path":
                            path = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                            break;
                        case "Result":
                            string resultValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Util.DirectorySearch.TryParseResultType(resultValue, out result))
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName,
                                    resultValue, Util.DirectorySearch.ResultType.exists.ToString()));
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

            if (null == variable)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Variable"));
            }

            if (null == path)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Path"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("wds", variable, condition, after, path, result.ToString());
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.CreateWixSearchRow(sourceLineNumbers, id, variable, condition);
                if (after != null)
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "WixSearch", after);
                    // TODO: We're currently defaulting to "always run after", which we will need to change...
                    this.CreateWixSearchRelationRow(sourceLineNumbers, id, after, 2);
                }

                WixFileSearchAttributes attributes = WixFileSearchAttributes.IsDirectory;
                switch (result)
                {
                    case Util.DirectorySearch.ResultType.exists:
                        attributes |= WixFileSearchAttributes.WantExists;
                        break;
                }

                this.CreateWixFileSearchRow(sourceLineNumbers, id, path, attributes);
            }
        }

        /// <summary>
        /// Parses a DirectorySearchRef, FileSearchRef, ProductSearchRef, and RegistrySearchRef elements
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseWixSearchRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string refId = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            refId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "WixSearch", refId);
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
        }

        /// <summary>
        /// Parses a FileSearch element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseFileSearchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string path = null;
            Util.FileSearch.ResultType result = Util.FileSearch.ResultType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Path":
                            path = this.Core.GetAttributeLongFilename(sourceLineNumbers, attrib, false, true);
                            break;
                        case "Result":
                            string resultValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Util.FileSearch.TryParseResultType(resultValue, out result))
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName,
                                    resultValue,
                                    Util.FileSearch.ResultType.exists.ToString(),
                                    Util.FileSearch.ResultType.version.ToString()));
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

            if (null == variable)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Variable"));
            }

            if (null == path)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Path"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("wfs", variable, condition, after, path, result.ToString());
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.CreateWixSearchRow(sourceLineNumbers, id, variable, condition);
                if (after != null)
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "WixSearch", after);
                    // TODO: We're currently defaulting to "always run after", which we will need to change...
                    this.CreateWixSearchRelationRow(sourceLineNumbers, id, after, 2);
                }

                WixFileSearchAttributes attributes = WixFileSearchAttributes.Default;
                switch (result)
                {
                    case Util.FileSearch.ResultType.exists:
                        attributes |= WixFileSearchAttributes.WantExists;
                        break;
                    case Util.FileSearch.ResultType.version:
                        attributes |= WixFileSearchAttributes.WantVersion;
                        break;
                }

                this.CreateWixFileSearchRow(sourceLineNumbers, id, path, attributes);
            }
        }

        /// <summary>
        /// Creates a row in the WixFileSearch table.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="id">Identifier of the search (key into the WixSearch table)</param>
        /// <param name="path">File/directory path to search for.</param>
        /// <param name="attributes"></param>
        private void CreateWixFileSearchRow(SourceLineNumber sourceLineNumbers, Identifier id, string path, WixFileSearchAttributes attributes)
        {
            Row row = this.Core.CreateRow(sourceLineNumbers, "WixFileSearch", id);
            row[1] = path;
            //row[2] = minVersion;
            //row[3] = maxVersion;
            //row[4] = minSize;
            //row[5] = maxSize;
            //row[6] = minDate;
            //row[7] = maxDate;
            //row[8] = languages;
            row[9] = (int)attributes;
        }

        /// <summary>
        /// Creates a row in the WixSearch table.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="id">Identifier of the search.</param>
        /// <param name="variable">The Burn variable to store the result into.</param>
        /// <param name="condition">A condition to test before evaluating the search.</param>
        private void CreateWixSearchRow(SourceLineNumber sourceLineNumbers, Identifier id, string variable, string condition)
        {
            Row row = this.Core.CreateRow(sourceLineNumbers, "WixSearch", id);
            row[1] = variable;
            row[2] = condition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="id">Identifier of the search (key into the WixSearch table)</param>
        /// <param name="parentId">Identifier of the search that comes before (key into the WixSearch table)</param>
        /// <param name="attributes">Further details about the relation between id and parentId.</param>
        private void CreateWixSearchRelationRow(SourceLineNumber sourceLineNumbers, Identifier id, string parentId, int attributes)
        {
            Row row = this.Core.CreateRow(sourceLineNumbers, "WixSearchRelation", id);
            row[1] = parentId;
            row[2] = attributes;
        }

        /// <summary>
        /// Parses a file share element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="directoryId">Identifier of referred to directory.</param>
        private void ParseFileShareElement(XElement node, string componentId, string directoryId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string description = null;
            string name = null;
            string id = null;

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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (!node.Elements().Any())
            {
                this.Core.OnMessage(WixErrors.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "FileSharePermission"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "FileSharePermission":
                            this.ParseFileSharePermissionElement(child, id);
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

            // Reference ConfigureSmbInstall and ConfigureSmbUninstall since nothing will happen without it
            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CA is referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureSmbInstall_ARM");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureSmbUninstall_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureSmbInstall");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureSmbUninstall");
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "FileShare");
                row[0] = id;
                row[1] = name;
                row[2] = componentId;
                row[3] = description;
                row[4] = directoryId;
            }
        }

        /// <summary>
        /// Parses a FileSharePermission element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="fileShareId">The identifier of the parent FileShare element.</param>
        private void ParseFileSharePermissionElement(XElement node, string fileShareId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            BitArray bits = new BitArray(32);
            int permission = 0;
            string user = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "User":
                            user = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "User", user);
                            break;
                        default:
                            YesNoType attribValue = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (!this.Core.TrySetBitFromName(UtilConstants.StandardPermissions, attrib.Name.LocalName, attribValue, bits, 16))
                            {
                                if (!this.Core.TrySetBitFromName(UtilConstants.GenericPermissions, attrib.Name.LocalName, attribValue, bits, 28))
                                {
                                    if (!this.Core.TrySetBitFromName(UtilConstants.FolderPermissions, attrib.Name.LocalName, attribValue, bits, 0))
                                    {
                                        this.Core.UnexpectedAttribute(node, attrib);
                                        break;
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            permission = this.Core.CreateIntegerFromBitArray(bits);

            if (null == user)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "User"));
            }

            if (int.MinValue == permission) // just GENERIC_READ, which is MSI_NULL
            {
                this.Core.OnMessage(WixErrors.GenericReadNotAllowed(sourceLineNumbers));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "FileSharePermissions");
                row[0] = fileShareId;
                row[1] = user;
                row[2] = permission;
            }
        }

        /// <summary>
        /// Parses a group element.
        /// </summary>
        /// <param name="node">Node to be parsed.</param>
        /// <param name="componentId">Component Id of the parent component of this element.</param>
        private void ParseGroupElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string domain = null;
            string name = null;

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
                        case "Domain":
                            domain = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "Group");
                row[0] = id;
                row[1] = componentId;
                row[2] = name;
                row[3] = domain;
            }
        }

        /// <summary>
        /// Parses a GroupRef element
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="userId">Required user id to be joined to the group.</param>
        private void ParseGroupRefElement(XElement node, string userId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string groupId = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            groupId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Group", groupId);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "UserGroup");
                row[0] = userId;
                row[1] = groupId;
            }
        }

        /// <summary>
        /// Parses an InternetShortcut element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="defaultTarget">Default directory if none is specified on the InternetShortcut element.</param>
        private void ParseInternetShortcutElement(XElement node, string componentId, string defaultTarget)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string name = null;
            string target = null;
            string directoryId = null;
            string type = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Directory":
                            directoryId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Target":
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            type = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            // If there was no directoryId specified on the InternetShortcut element, default to the one on
            // the parent component.
            if (null == directoryId)
            {
                directoryId = defaultTarget;
            }

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            // In theory this can never be the case, since InternetShortcut can only be under
            // a component element, and if the Directory wasn't specified the default will come
            // from the component. However, better safe than sorry, so here's a check to make sure
            // it didn't wind up being null after setting it to the defaultTarget.
            if (null == directoryId)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Directory"));
            }

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == target)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Target"));
            }

            this.Core.ParseForExtensionElements(node);

            InternetShortcutType shortcutType = InternetShortcutType.Link;
            if (0 == String.Compare(type, "url", StringComparison.OrdinalIgnoreCase))
            {
                shortcutType = InternetShortcutType.Url;
            }

            if (!this.Core.EncounteredError)
            {
                CreateWixInternetShortcut(this.Core, sourceLineNumbers, componentId, directoryId, id, name, target, shortcutType);
            }
        }

        /// <summary>
        /// Creates the rows needed for WixInternetShortcut to work.
        /// </summary>
        /// <param name="core">The CompilerCore object used to create rows.</param>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="directoryId">Identifier of directory containing shortcut.</param>
        /// <param name="id">Identifier of shortcut.</param>
        /// <param name="name">Name of shortcut without extension.</param>
        /// <param name="target">Target URL of shortcut.</param>
        public static void CreateWixInternetShortcut(ICompilerCore core, SourceLineNumber sourceLineNumbers, string componentId, string directoryId, string shortcutId, string name, string target, InternetShortcutType type)
        {
            // add the appropriate extension based on type of shortcut
            name = String.Concat(name, InternetShortcutType.Url == type ? ".url" : ".lnk");

            Row row = core.CreateRow(sourceLineNumbers, "WixInternetShortcut");
            row[0] = shortcutId;
            row[1] = componentId;
            row[2] = directoryId;
            row[3] = name;
            row[4] = target;
            row[5] = (int)type;

            // Reference custom action because nothing will happen without it
            if (core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CA is referenced
                core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixSchedInternetShortcuts_ARM");
            }
            else
            {
                // All other supported platforms use x86
                core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixSchedInternetShortcuts");
            }

            // make sure we have a CreateFolder table so that the immediate CA can add temporary rows to handle installation and uninstallation
            core.EnsureTable(sourceLineNumbers, "CreateFolder");

            // use built-in MSI functionality to remove the shortcuts rather than doing so via CA
            row = core.CreateRow(sourceLineNumbers, "RemoveFile");
            row[0] = shortcutId;
            row[1] = componentId;
            row[2] = core.IsValidShortFilename(name, false) ? name : String.Concat(core.CreateShortName(name, true, false, directoryId, name), "|", name);
            row[3] = directoryId;
            row[4] = 2; // msidbRemoveFileInstallModeOnRemove
        }

        /// <summary>
        /// Parses a performance category element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParsePerformanceCategoryElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string name = null;
            string help = null;
            YesNoType multiInstance = YesNoType.No;
            int defaultLanguage = 0x09; // default to "english"

            ArrayList parsedPerformanceCounters = new ArrayList();

            // default to managed performance counter
            string library = "netfxperf.dll";
            string openEntryPoint = "OpenPerformanceData";
            string collectEntryPoint = "CollectPerformanceData";
            string closeEntryPoint = "ClosePerformanceData";

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Close":
                            closeEntryPoint = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Collect":
                            collectEntryPoint = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultLanguage":
                            defaultLanguage = this.GetPerformanceCounterLanguage(sourceLineNumbers, attrib);
                            break;
                        case "Help":
                            help = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Library":
                            library = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MultiInstance":
                            multiInstance = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Open":
                            openEntryPoint = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                name = id;
            }

            // Process the child counter elements.
            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "PerformanceCounter":
                            ParsedPerformanceCounter counter = this.ParsePerformanceCounterElement(child, defaultLanguage);
                            if (null != counter)
                            {
                                parsedPerformanceCounters.Add(counter);
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


            if (!this.Core.EncounteredError)
            {
                // Calculate the ini and h file content.
                string objectName = "OBJECT_1";
                string objectLanguage = defaultLanguage.ToString("D3", CultureInfo.InvariantCulture);

                StringBuilder sbIniData = new StringBuilder();
                sbIniData.AppendFormat("[info]\r\ndrivername={0}\r\nsymbolfile=wixperf.h\r\n\r\n[objects]\r\n{1}_{2}_NAME=\r\n\r\n[languages]\r\n{2}=LANG{2}\r\n\r\n", name, objectName, objectLanguage);
                sbIniData.AppendFormat("[text]\r\n{0}_{1}_NAME={2}\r\n", objectName, objectLanguage, name);
                if (null != help)
                {
                    sbIniData.AppendFormat("{0}_{1}_HELP={2}\r\n", objectName, objectLanguage, help);
                }

                int symbolConstantsCounter = 0;
                StringBuilder sbSymbolicConstants = new StringBuilder();
                sbSymbolicConstants.AppendFormat("#define {0}    {1}\r\n", objectName, symbolConstantsCounter);

                StringBuilder sbCounterNames = new StringBuilder("[~]");
                StringBuilder sbCounterTypes = new StringBuilder("[~]");
                for (int i = 0; i < parsedPerformanceCounters.Count; ++i)
                {
                    ParsedPerformanceCounter counter = (ParsedPerformanceCounter)parsedPerformanceCounters[i];
                    string counterName = String.Concat("DEVICE_COUNTER_", i + 1);

                    sbIniData.AppendFormat("{0}_{1}_NAME={2}\r\n", counterName, counter.Language, counter.Name);
                    if (null != counter.Help)
                    {
                        sbIniData.AppendFormat("{0}_{1}_HELP={2}\r\n", counterName, counter.Language, counter.Help);
                    }

                    symbolConstantsCounter += 2;
                    sbSymbolicConstants.AppendFormat("#define {0}    {1}\r\n", counterName, symbolConstantsCounter);

                    sbCounterNames.Append(UtilCompiler.FindPropertyBrackets.Replace(counter.Name, this.EscapeProperties));
                    sbCounterNames.Append("[~]");
                    sbCounterTypes.Append(counter.Type);
                    sbCounterTypes.Append("[~]");
                }

                sbSymbolicConstants.AppendFormat("#define LAST_{0}_COUNTER_OFFSET    {1}\r\n", objectName, symbolConstantsCounter);

                // Add the calculated INI and H strings to the PerformanceCategory table.
                Row row = this.Core.CreateRow(sourceLineNumbers, "PerformanceCategory");
                row[0] = id;
                row[1] = componentId;
                row[2] = name;
                row[3] = sbIniData.ToString();
                row[4] = sbSymbolicConstants.ToString();

                // Set up the application's performance key.
                int registryRoot = 2; // HKLM
                string escapedName = UtilCompiler.FindPropertyBrackets.Replace(name, this.EscapeProperties);
                string linkageKey = String.Format(@"SYSTEM\CurrentControlSet\Services\{0}\Linkage", escapedName);
                string performanceKey = String.Format(@"SYSTEM\CurrentControlSet\Services\{0}\Performance", escapedName);

                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, linkageKey, "Export", escapedName, componentId, false);
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, performanceKey, "-", null, componentId, false);
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, performanceKey, "Library", library, componentId, false);
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, performanceKey, "Open", openEntryPoint, componentId, false);
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, performanceKey, "Collect", collectEntryPoint, componentId, false);
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, performanceKey, "Close", closeEntryPoint, componentId, false);
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, performanceKey, "IsMultiInstance", YesNoType.Yes == multiInstance ? "#1" : "#0", componentId, false);
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, performanceKey, "Counter Names", sbCounterNames.ToString(), componentId, false);
                this.Core.CreateRegistryRow(sourceLineNumbers, registryRoot, performanceKey, "Counter Types", sbCounterTypes.ToString(), componentId, false);
            }

            // Reference InstallPerfCounterData and UninstallPerfCounterData since nothing will happen without them
            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CAs are referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "InstallPerfCounterData_ARM");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "UninstallPerfCounterData_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "InstallPerfCounterData");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "UninstallPerfCounterData");
            }
        }

        /// <summary>
        /// Gets the performance counter language as a decimal number.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>Numeric representation of the language as per WinNT.h.</returns>
        private int GetPerformanceCounterLanguage(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            int language = 0;
            if (String.Empty == attribute.Value)
            {
                this.Core.OnMessage(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }
            else
            {
                switch (attribute.Value)
                {
                    case "afrikaans":
                        language = 0x36;
                        break;
                    case "albanian":
                        language = 0x1c;
                        break;
                    case "arabic":
                        language = 0x01;
                        break;
                    case "armenian":
                        language = 0x2b;
                        break;
                    case "assamese":
                        language = 0x4d;
                        break;
                    case "azeri":
                        language = 0x2c;
                        break;
                    case "basque":
                        language = 0x2d;
                        break;
                    case "belarusian":
                        language = 0x23;
                        break;
                    case "bengali":
                        language = 0x45;
                        break;
                    case "bulgarian":
                        language = 0x02;
                        break;
                    case "catalan":
                        language = 0x03;
                        break;
                    case "chinese":
                        language = 0x04;
                        break;
                    case "croatian":
                        language = 0x1a;
                        break;
                    case "czech":
                        language = 0x05;
                        break;
                    case "danish":
                        language = 0x06;
                        break;
                    case "divehi":
                        language = 0x65;
                        break;
                    case "dutch":
                        language = 0x13;
                        break;
                    case "piglatin":
                    case "english":
                        language = 0x09;
                        break;
                    case "estonian":
                        language = 0x25;
                        break;
                    case "faeroese":
                        language = 0x38;
                        break;
                    case "farsi":
                        language = 0x29;
                        break;
                    case "finnish":
                        language = 0x0b;
                        break;
                    case "french":
                        language = 0x0c;
                        break;
                    case "galician":
                        language = 0x56;
                        break;
                    case "georgian":
                        language = 0x37;
                        break;
                    case "german":
                        language = 0x07;
                        break;
                    case "greek":
                        language = 0x08;
                        break;
                    case "gujarati":
                        language = 0x47;
                        break;
                    case "hebrew":
                        language = 0x0d;
                        break;
                    case "hindi":
                        language = 0x39;
                        break;
                    case "hungarian":
                        language = 0x0e;
                        break;
                    case "icelandic":
                        language = 0x0f;
                        break;
                    case "indonesian":
                        language = 0x21;
                        break;
                    case "italian":
                        language = 0x10;
                        break;
                    case "japanese":
                        language = 0x11;
                        break;
                    case "kannada":
                        language = 0x4b;
                        break;
                    case "kashmiri":
                        language = 0x60;
                        break;
                    case "kazak":
                        language = 0x3f;
                        break;
                    case "konkani":
                        language = 0x57;
                        break;
                    case "korean":
                        language = 0x12;
                        break;
                    case "kyrgyz":
                        language = 0x40;
                        break;
                    case "latvian":
                        language = 0x26;
                        break;
                    case "lithuanian":
                        language = 0x27;
                        break;
                    case "macedonian":
                        language = 0x2f;
                        break;
                    case "malay":
                        language = 0x3e;
                        break;
                    case "malayalam":
                        language = 0x4c;
                        break;
                    case "manipuri":
                        language = 0x58;
                        break;
                    case "marathi":
                        language = 0x4e;
                        break;
                    case "mongolian":
                        language = 0x50;
                        break;
                    case "nepali":
                        language = 0x61;
                        break;
                    case "norwegian":
                        language = 0x14;
                        break;
                    case "oriya":
                        language = 0x48;
                        break;
                    case "polish":
                        language = 0x15;
                        break;
                    case "portuguese":
                        language = 0x16;
                        break;
                    case "punjabi":
                        language = 0x46;
                        break;
                    case "romanian":
                        language = 0x18;
                        break;
                    case "russian":
                        language = 0x19;
                        break;
                    case "sanskrit":
                        language = 0x4f;
                        break;
                    case "serbian":
                        language = 0x1a;
                        break;
                    case "sindhi":
                        language = 0x59;
                        break;
                    case "slovak":
                        language = 0x1b;
                        break;
                    case "slovenian":
                        language = 0x24;
                        break;
                    case "spanish":
                        language = 0x0a;
                        break;
                    case "swahili":
                        language = 0x41;
                        break;
                    case "swedish":
                        language = 0x1d;
                        break;
                    case "syriac":
                        language = 0x5a;
                        break;
                    case "tamil":
                        language = 0x49;
                        break;
                    case "tatar":
                        language = 0x44;
                        break;
                    case "telugu":
                        language = 0x4a;
                        break;
                    case "thai":
                        language = 0x1e;
                        break;
                    case "turkish":
                        language = 0x1f;
                        break;
                    case "ukrainian":
                        language = 0x22;
                        break;
                    case "urdu":
                        language = 0x20;
                        break;
                    case "uzbek":
                        language = 0x43;
                        break;
                    case "vietnamese":
                        language = 0x2a;
                        break;
                    default:
                        this.Core.OnMessage(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
                        break;
                }
            }

            return language;
        }

        /// <summary>
        /// Parses a performance counter element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="defaultLanguage">Default language for the performance counter.</param>
        private ParsedPerformanceCounter ParsePerformanceCounterElement(XElement node, int defaultLanguage)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            ParsedPerformanceCounter parsedPerformanceCounter = null;
            string name = null;
            string help = null;
            System.Diagnostics.PerformanceCounterType type = System.Diagnostics.PerformanceCounterType.NumberOfItems32;
            int language = defaultLanguage;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Help":
                            help = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            type = this.GetPerformanceCounterType(sourceLineNumbers, attrib);
                            break;
                        case "Language":
                            language = this.GetPerformanceCounterLanguage(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (null == help)
            {
                this.Core.OnMessage(UtilWarnings.RequiredAttributeForWindowsXP(sourceLineNumbers, node.Name.LocalName, "Help"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                parsedPerformanceCounter = new ParsedPerformanceCounter(name, help, type, language);
            }

            return parsedPerformanceCounter;
        }

        /// <summary>
        /// Gets the performance counter type.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>Numeric representation of the language as per WinNT.h.</returns>
        private System.Diagnostics.PerformanceCounterType GetPerformanceCounterType(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            System.Diagnostics.PerformanceCounterType type = System.Diagnostics.PerformanceCounterType.NumberOfItems32;
            if (String.Empty == attribute.Value)
            {
                this.Core.OnMessage(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
            }
            else
            {
                switch (attribute.Value)
                {
                    case "averageBase":
                        type = System.Diagnostics.PerformanceCounterType.AverageBase;
                        break;
                    case "averageCount64":
                        type = System.Diagnostics.PerformanceCounterType.AverageCount64;
                        break;
                    case "averageTimer32":
                        type = System.Diagnostics.PerformanceCounterType.AverageTimer32;
                        break;
                    case "counterDelta32":
                        type = System.Diagnostics.PerformanceCounterType.CounterDelta32;
                        break;
                    case "counterTimerInverse":
                        type = System.Diagnostics.PerformanceCounterType.CounterTimerInverse;
                        break;
                    case "sampleFraction":
                        type = System.Diagnostics.PerformanceCounterType.SampleFraction;
                        break;
                    case "timer100Ns":
                        type = System.Diagnostics.PerformanceCounterType.Timer100Ns;
                        break;
                    case "counterTimer":
                        type = System.Diagnostics.PerformanceCounterType.CounterTimer;
                        break;
                    case "rawFraction":
                        type = System.Diagnostics.PerformanceCounterType.RawFraction;
                        break;
                    case "timer100NsInverse":
                        type = System.Diagnostics.PerformanceCounterType.Timer100NsInverse;
                        break;
                    case "counterMultiTimer":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiTimer;
                        break;
                    case "counterMultiTimer100Ns":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiTimer100Ns;
                        break;
                    case "counterMultiTimerInverse":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiTimerInverse;
                        break;
                    case "counterMultiTimer100NsInverse":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiTimer100NsInverse;
                        break;
                    case "elapsedTime":
                        type = System.Diagnostics.PerformanceCounterType.ElapsedTime;
                        break;
                    case "sampleBase":
                        type = System.Diagnostics.PerformanceCounterType.SampleBase;
                        break;
                    case "rawBase":
                        type = System.Diagnostics.PerformanceCounterType.RawBase;
                        break;
                    case "counterMultiBase":
                        type = System.Diagnostics.PerformanceCounterType.CounterMultiBase;
                        break;
                    case "rateOfCountsPerSecond64":
                        type = System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64;
                        break;
                    case "rateOfCountsPerSecond32":
                        type = System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond32;
                        break;
                    case "countPerTimeInterval64":
                        type = System.Diagnostics.PerformanceCounterType.CountPerTimeInterval64;
                        break;
                    case "countPerTimeInterval32":
                        type = System.Diagnostics.PerformanceCounterType.CountPerTimeInterval32;
                        break;
                    case "sampleCounter":
                        type = System.Diagnostics.PerformanceCounterType.SampleCounter;
                        break;
                    case "counterDelta64":
                        type = System.Diagnostics.PerformanceCounterType.CounterDelta64;
                        break;
                    case "numberOfItems64":
                        type = System.Diagnostics.PerformanceCounterType.NumberOfItems64;
                        break;
                    case "numberOfItems32":
                        type = System.Diagnostics.PerformanceCounterType.NumberOfItems32;
                        break;
                    case "numberOfItemsHEX64":
                        type = System.Diagnostics.PerformanceCounterType.NumberOfItemsHEX64;
                        break;
                    case "numberOfItemsHEX32":
                        type = System.Diagnostics.PerformanceCounterType.NumberOfItemsHEX32;
                        break;
                    default:
                        this.Core.OnMessage(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName));
                        break;
                }
            }

            return type;
        }

        /// <summary>
        /// Parses a perf counter element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Identifier of referenced file.</param>
        private void ParsePerfCounterElement(XElement node, string componentId, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string name = null;

            this.Core.OnMessage(UtilWarnings.DeprecatedPerfCounterElement(sourceLineNumbers));

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "Perfmon");
                row[0] = componentId;
                row[1] = String.Concat("[#", fileId, "]");
                row[2] = name;
            }

            // Reference ConfigurePerfmonInstall and ConfigurePerfmonUninstall since nothing will happen without them
            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CAs are referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigurePerfmonInstall_ARM");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigurePerfmonUninstall_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigurePerfmonInstall");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigurePerfmonUninstall");
            }
        }


        /// <summary>
        /// Parses a perf manifest element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Identifier of referenced file.</param>
        private void ParsePerfCounterManifestElement(XElement node, string componentId, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string resourceFileDirectory = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "ResourceFileDirectory":
                            resourceFileDirectory = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "PerfmonManifest");
                row[0] = componentId;
                row[1] = String.Concat("[#", fileId, "]");
                row[2] = resourceFileDirectory;
            }

            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CAs are referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigurePerfmonManifestRegister_ARM");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigurePerfmonManifestUnregister_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigurePerfmonManifestRegister");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigurePerfmonManifestUnregister");
            }
        }

        /// <summary>
        /// Parses a format files element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="fileId">Identifier of referenced file.</param>
        /// <param name="win64">Flag to determine whether the component is 64-bit.</param>
        private void ParseFormatFileElement(XElement node, string fileId, bool win64)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string binaryId = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "BinaryKey":
                            binaryId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == binaryId)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "BinaryKey"));
            }

            if (!this.Core.EncounteredError)
            {
                switch (this.Core.CurrentPlatform)
                {
                    case Platform.X86:
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixSchedFormatFiles");
                        break;
                    case Platform.X64:
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixSchedFormatFiles_x64");
                        break;
                    case Platform.IA64:
                    case Platform.ARM:
                        this.Core.OnMessage(WixErrors.UnsupportedPlatformForElement(sourceLineNumbers, this.Core.CurrentPlatform.ToString(), node.Name.LocalName));
                        break;
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixFormatFiles");
                row[0] = binaryId;
                row[1] = fileId;

                this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", binaryId);
            }
        }

        /// <summary>
        /// Parses a event manifest element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="fileId">Identifier of referenced file.</param>
        private void ParseEventManifestElement(XElement node, string componentId, string fileId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string messageFile = null;
            string resourceFile = null;
            string parameterFile = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "MessageFile":
                            messageFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ResourceFile":
                            resourceFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParameterFile":
                            parameterFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "EventManifest");
                row[0] = componentId;
                row[1] = String.Concat("[#", fileId, "]");

                if (null != messageFile)
                {
                    Row messageRow = this.Core.CreateRow(sourceLineNumbers, "XmlFile");
                    messageRow[0] = String.Concat("Config_", fileId, "MessageFile");
                    messageRow[1] = String.Concat("[#", fileId, "]");
                    messageRow[2] = "/*/*/*/*[\\[]@messageFileName[\\]]";
                    messageRow[3] = "messageFileName";
                    messageRow[4] = messageFile;
                    messageRow[5] = 4 | 0x00001000;  //bulk write | preserve modified date
                    messageRow[6] = componentId;
                }
                if (null != parameterFile)
                {
                    Row resourceRow = this.Core.CreateRow(sourceLineNumbers, "XmlFile");
                    resourceRow[0] = String.Concat("Config_", fileId, "ParameterFile");
                    resourceRow[1] = String.Concat("[#", fileId, "]");
                    resourceRow[2] = "/*/*/*/*[\\[]@parameterFileName[\\]]";
                    resourceRow[3] = "parameterFileName";
                    resourceRow[4] = parameterFile;
                    resourceRow[5] = 4 | 0x00001000;  //bulk write | preserve modified date
                    resourceRow[6] = componentId;
                }
                if (null != resourceFile)
                {
                    Row resourceRow = this.Core.CreateRow(sourceLineNumbers, "XmlFile");
                    resourceRow[0] = String.Concat("Config_", fileId, "ResourceFile");
                    resourceRow[1] = String.Concat("[#", fileId, "]");
                    resourceRow[2] = "/*/*/*/*[\\[]@resourceFileName[\\]]";
                    resourceRow[3] = "resourceFileName";
                    resourceRow[4] = resourceFile;
                    resourceRow[5] = 4 | 0x00001000;  //bulk write | preserve modified date
                    resourceRow[6] = componentId;
                }

            }

            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CA is referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureEventManifestRegister_ARM");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureEventManifestUnregister_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureEventManifestRegister");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureEventManifestUnregister");
            }

            if (null != messageFile || null != parameterFile || null != resourceFile)
            {
                if (this.Core.CurrentPlatform == Platform.ARM)
                {
                    // Ensure ARM version of the CA is referenced
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedXmlFile_ARM");
                }
                else
                {
                    // All other supported platforms use x86
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedXmlFile");
                }
            }
        }

        /// <summary>
        /// Parses a PermissionEx element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="objectId">Identifier of object to be secured.</param>
        /// <param name="componentId">Identifier of component, used to determine install state.</param>
        /// <param name="win64">Flag to determine whether the component is 64-bit.</param>
        /// <param name="tableName">Name of table that contains objectId.</param>
        private void ParsePermissionExElement(XElement node, string objectId, string componentId, bool win64, string tableName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            BitArray bits = new BitArray(32);
            string domain = null;
            int permission = 0;
            string[] specialPermissions = null;
            string user = null;

            PermissionType permissionType = PermissionType.SecureObjects;

            switch (tableName)
            {
                case "CreateFolder":
                    specialPermissions = UtilConstants.FolderPermissions;
                    break;
                case "File":
                    specialPermissions = UtilConstants.FilePermissions;
                    break;
                case "Registry":
                    specialPermissions = UtilConstants.RegistryPermissions;
                    if (String.IsNullOrEmpty(objectId))
                    {
                        this.Core.OnMessage(UtilErrors.InvalidRegistryObject(sourceLineNumbers, node.Parent.Name.LocalName));
                    }
                    break;
                case "ServiceInstall":
                    specialPermissions = UtilConstants.ServicePermissions;
                    permissionType = PermissionType.SecureObjects;
                    break;
                default:
                    this.Core.UnexpectedElement(node.Parent, node);
                    break;
            }

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Domain":
                            if (PermissionType.FileSharePermissions == permissionType)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            domain = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "User":
                            user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            YesNoType attribValue = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            if (!this.Core.TrySetBitFromName(UtilConstants.StandardPermissions, attrib.Name.LocalName, attribValue, bits, 16))
                            {
                                if (!this.Core.TrySetBitFromName(UtilConstants.GenericPermissions, attrib.Name.LocalName, attribValue, bits, 28))
                                {
                                    if (!this.Core.TrySetBitFromName(specialPermissions, attrib.Name.LocalName, attribValue, bits, 0))
                                    {
                                        this.Core.UnexpectedAttribute(node, attrib);
                                        break;
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            permission = this.Core.CreateIntegerFromBitArray(bits);

            if (null == user)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "User"));
            }

            if (int.MinValue == permission) // just GENERIC_READ, which is MSI_NULL
            {
                this.Core.OnMessage(WixErrors.GenericReadNotAllowed(sourceLineNumbers));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                if (win64)
                {
                    if (this.Core.CurrentPlatform == Platform.IA64)
                    {
                        this.Core.OnMessage(WixErrors.UnsupportedPlatformForElement(sourceLineNumbers, "ia64", node.Name.LocalName));
                    }
                    else
                    {
                        // Ensure SchedSecureObjects (x64) is referenced
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedSecureObjects_x64");
                    }
                }
                else if (this.Core.CurrentPlatform == Platform.ARM)
                {
                    // Ensure SchedSecureObjects (arm) is referenced
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedSecureObjects_ARM");
                }
                else
                {
                    // Ensure SchedSecureObjects (x86) is referenced, to handle this x86 component member
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedSecureObjects");
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "SecureObjects");
                row[0] = objectId;
                row[1] = tableName;
                row[2] = domain;
                row[3] = user;
                row[4] = permission;
                row[5] = componentId;
            }
        }

        /// <summary>
        /// Parses a ProductSearch element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseProductSearchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            string productCode = null;
            string upgradeCode = null;

            Util.ProductSearch.ResultType result = Util.ProductSearch.ResultType.NotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "ProductCode":
                            productCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "UpgradeCode":
                            upgradeCode = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Result":
                            string resultValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Util.ProductSearch.TryParseResultType(resultValue, out result))
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName,
                                    resultValue,
                                    Util.ProductSearch.ResultType.version.ToString(),
                                    Util.ProductSearch.ResultType.language.ToString(),
                                    Util.ProductSearch.ResultType.state.ToString(),
                                    Util.ProductSearch.ResultType.assignment.ToString()));
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

            if (null == variable)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Variable"));
            }

            if (null == upgradeCode && null == productCode)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProductCode", "UpgradeCode", true));
            }

            if (null != upgradeCode && null != productCode)
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "UpgradeCode", "ProductCode"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("wps", variable, condition, after, (productCode == null ? upgradeCode : productCode), result.ToString());
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.CreateWixSearchRow(sourceLineNumbers, id, variable, condition);
                if (after != null)
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "WixSearch", after);
                    // TODO: We're currently defaulting to "always run after", which we will need to change...
                    this.CreateWixSearchRelationRow(sourceLineNumbers, id, after, 2);
                }

                WixProductSearchAttributes attributes = WixProductSearchAttributes.Version;
                switch (result)
                {
                    case Util.ProductSearch.ResultType.version:
                        attributes = WixProductSearchAttributes.Version;
                        break;
                    case Util.ProductSearch.ResultType.language:
                        attributes = WixProductSearchAttributes.Language;
                        break;
                    case Util.ProductSearch.ResultType.state:
                        attributes = WixProductSearchAttributes.State;
                        break;
                    case Util.ProductSearch.ResultType.assignment:
                        attributes = WixProductSearchAttributes.Assignment;
                        break;
                }

                // set an additional flag if this is an upgrade code
                if (null != upgradeCode)
                {
                    attributes |= WixProductSearchAttributes.UpgradeCode;
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixProductSearch");
                row[0] = id;
                row[1] = (productCode == null ? upgradeCode : productCode);
                row[2] = (int)attributes;
            }
        }

        /// <summary>
        /// Parses a RegistrySearch element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseRegistrySearchElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            int root = CompilerConstants.IntegerNotSet;
            string key = null;
            string value = null;
            YesNoType expand = YesNoType.NotSet;
            YesNoType win64 = YesNoType.NotSet;
            Util.RegistrySearch.ResultType result = Util.RegistrySearch.ResultType.NotSet;
            Util.RegistrySearch.FormatType format = Util.RegistrySearch.FormatType.raw;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                        case "Variable":
                        case "Condition":
                        case "After":
                            this.ParseCommonSearchAttributes(sourceLineNumbers, attrib, ref id, ref variable, ref condition, ref after);
                            break;
                        case "Root":
                            root = this.Core.GetAttributeMsidbRegistryRootValue(sourceLineNumbers, attrib, false);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ExpandEnvironmentVariables":
                            expand = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Format":
                            string formatValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!String.IsNullOrEmpty(formatValue))
                            {
                                if (!Util.RegistrySearch.TryParseFormatType(formatValue, out format))
                                {
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName,
                                        formatValue, Util.RegistrySearch.FormatType.raw.ToString(), Util.RegistrySearch.FormatType.compatible.ToString()));
                                }
                            }
                            break;
                        case "Result":
                            string resultValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!Util.RegistrySearch.TryParseResultType(resultValue, out result))
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attrib.Parent.Name.LocalName, attrib.Name.LocalName,
                                    resultValue, Util.RegistrySearch.ResultType.exists.ToString(), Util.RegistrySearch.ResultType.value.ToString()));
                            }
                            break;
                        case "Win64":
                            win64 = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == variable)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Variable"));
            }

            if (CompilerConstants.IntegerNotSet == root)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Root"));
            }

            if (null == key)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            if (Util.RegistrySearch.ResultType.NotSet == result)
            {
                result = Util.RegistrySearch.ResultType.value;
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("wrs", variable, condition, after, root.ToString(), key, value, result.ToString());
            }

            WixRegistrySearchAttributes attributes = WixRegistrySearchAttributes.Raw;
            switch (format)
            {
                case Util.RegistrySearch.FormatType.raw:
                    attributes = WixRegistrySearchAttributes.Raw;
                    break;
                case Util.RegistrySearch.FormatType.compatible:
                    attributes = WixRegistrySearchAttributes.Compatible;
                    break;
            }

            switch (result)
            {
                case Util.RegistrySearch.ResultType.exists:
                    attributes |= WixRegistrySearchAttributes.WantExists;
                    break;
                case Util.RegistrySearch.ResultType.value:
                    attributes |= WixRegistrySearchAttributes.WantValue;
                    break;
            }

            if (expand == YesNoType.Yes)
            {
                if (0 != (attributes & WixRegistrySearchAttributes.WantExists))
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName,
                        "ExpandEnvironmentVariables", expand.ToString(), "Result", result.ToString()));
                }

                attributes |= WixRegistrySearchAttributes.ExpandEnvironmentVariables;
            }

            if (win64 == YesNoType.Yes)
            {
                attributes |= WixRegistrySearchAttributes.Win64;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.CreateWixSearchRow(sourceLineNumbers, id, variable, condition);
                if (after != null)
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "WixSearch", after);
                    // TODO: We're currently defaulting to "always run after", which we will need to change...
                    this.CreateWixSearchRelationRow(sourceLineNumbers, id, after, 2);
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixRegistrySearch", id);
                row[1] = root;
                row[2] = key;
                row[3] = value;
                row[4] = (int)attributes;
            }
        }

        /// <summary>
        /// Parses a RemoveFolderEx element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseRemoveFolderExElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int on = (int)WixRemoveFolderExOn.Uninstall;
            string property = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "On":
                            string onValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (onValue.Length == 0)
                            {
                                on = CompilerConstants.IllegalInteger;
                            }
                            else
                            {
                                switch (onValue)
                                {
                                    case "install":
                                        on = (int)WixRemoveFolderExOn.Install;
                                        break;
                                    case "uninstall":
                                        on = (int)WixRemoveFolderExOn.Uninstall;
                                        break;
                                    case "both":
                                        on = (int)WixRemoveFolderExOn.Both;
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "On", onValue, "install", "uninstall", "both"));
                                        on = CompilerConstants.IllegalInteger;
                                        break;
                                }
                            }
                            break;
                        case "Property":
                            property = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(property))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("wrf", componentId, property, on.ToString(CultureInfo.InvariantCulture.NumberFormat));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixRemoveFolderEx", id);
                row[1] = componentId;
                row[2] = property;
                row[3] = on;

                this.Core.EnsureTable(sourceLineNumbers, "RemoveFile");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixRemoveFoldersEx");
            }
        }

        /// <summary>
        /// Parses a RestartResource element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="componentId">The identity of the parent component.</param>
        private void ParseRestartResourceElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string resource = null;
            int attributes = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;

                        case "Path":
                            resource = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes = (int)WixRestartResourceAttributes.Filename;
                            break;

                        case "ProcessName":
                            resource = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes = (int)WixRestartResourceAttributes.ProcessName;
                            break;

                        case "ServiceName":
                            resource = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes = (int)WixRestartResourceAttributes.ServiceName;
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

            // Validate the attribute.
            if (null == id)
            {
                id = this.Core.CreateIdentifier("wrr", componentId, resource, attributes.ToString());
            }

            if (String.IsNullOrEmpty(resource) || CompilerConstants.IntegerNotSet == attributes)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Path", "ServiceName"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                // Add a reference to the WixRegisterRestartResources custom action since nothing will happen without it.
                if (this.Core.CurrentPlatform == Platform.ARM)
                {
                    // Ensure ARM version of the CA is referenced
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixRegisterRestartResources_ARM");
                }
                else
                {
                    // All other supported platforms use x86
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixRegisterRestartResources");
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixRestartResource", id);
                row[1] = componentId;
                row[2] = resource;
                row[3] = attributes;
            }
        }

        /// <summary>
        /// Parses a service configuration element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="parentTableName">Name of parent element.</param>
        /// <param name="parentTableServiceName">Optional name of service </param>
        private void ParseServiceConfigElement(XElement node, string componentId, string parentTableName, string parentTableServiceName)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string firstFailureActionType = null;
            bool newService = false;
            string programCommandLine = null;
            string rebootMessage = null;
            int resetPeriod = CompilerConstants.IntegerNotSet;
            int restartServiceDelay = CompilerConstants.IntegerNotSet;
            string secondFailureActionType = null;
            string serviceName = null;
            string thirdFailureActionType = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "FirstFailureActionType":
                            firstFailureActionType = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ProgramCommandLine":
                            programCommandLine = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RebootMessage":
                            rebootMessage = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ResetPeriodInDays":
                            resetPeriod = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "RestartServiceDelayInSeconds":
                            restartServiceDelay = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "SecondFailureActionType":
                            secondFailureActionType = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ServiceName":
                            serviceName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThirdFailureActionType":
                            thirdFailureActionType = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            // if this element is a child of ServiceInstall then ignore the service name provided.
            if ("ServiceInstall" == parentTableName)
            {
                // TODO: the ServiceName attribute should not be allowed in this case (the overwriting behavior may confuse users)
                serviceName = parentTableServiceName;
                newService = true;
            }
            else
            {
                // not a child of ServiceInstall, so ServiceName must have been provided
                if (null == serviceName)
                {
                    this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ServiceName"));
                }
            }

            this.Core.ParseForExtensionElements(node);

            // Reference SchedServiceConfig since nothing will happen without it
            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CA is referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedServiceConfig_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedServiceConfig");
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "ServiceConfig");
                row[0] = serviceName;
                row[1] = componentId;
                row[2] = (newService ? 1 : 0);
                row[3] = firstFailureActionType;
                row[4] = secondFailureActionType;
                row[5] = thirdFailureActionType;
                if (CompilerConstants.IntegerNotSet != resetPeriod)
                {
                    row[6] = resetPeriod;
                }

                if (CompilerConstants.IntegerNotSet != restartServiceDelay)
                {
                    row[7] = restartServiceDelay;
                }
                row[8] = programCommandLine;
                row[9] = rebootMessage;
            }
        }

        /// <summary>
        /// Parses a touch file element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="win64">Indicates whether the path is a 64-bit path.</param>
        private void ParseTouchFileElement(XElement node, string componentId, bool win64)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string path = null;
            YesNoType onInstall = YesNoType.NotSet;
            YesNoType onReinstall = YesNoType.NotSet;
            YesNoType onUninstall = YesNoType.NotSet;
            YesNoType nonvital = YesNoType.NotSet;
            int attributes = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Path":
                            path = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "OnInstall":
                            onInstall = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "OnReinstall":
                            onReinstall = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "OnUninstall":
                            onUninstall = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Nonvital":
                            nonvital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == path)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Path"));
            }

            // If none of the scheduling actions are set, default to touching on install and reinstall.
            if (YesNoType.NotSet == onInstall && YesNoType.NotSet == onReinstall && YesNoType.NotSet == onUninstall)
            {
                onInstall = YesNoType.Yes;
                onReinstall = YesNoType.Yes;
            }

            attributes |= YesNoType.Yes == onInstall ? 0x1 : 0;
            attributes |= YesNoType.Yes == onReinstall ? 0x2 : 0;
            attributes |= YesNoType.Yes == onUninstall ? 0x4 : 0;
            attributes |= win64 ? 0x10 : 0;
            attributes |= YesNoType.Yes == nonvital ? 0 : 0x20;

            if (null == id)
            {
                id = this.Core.CreateIdentifier("tf", path, attributes.ToString());
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixTouchFile", id);
                row[1] = componentId;
                row[2] = path;
                row[3] = attributes;

                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixTouchFileDuringInstall");
            }
        }

        /// <summary>
        /// Parses an user element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Optional identifier of parent component.</param>
        private void ParseUserElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int attributes = 0;
            string domain = null;
            string name = null;
            string password = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "CanNotChangePassword":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserPasswdCantChange;
                            }
                            break;
                        case "CreateUser":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserDontCreateUser;
                            }
                            break;
                        case "Disabled":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserDisableAccount;
                            }
                            break;
                        case "Domain":
                            domain = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FailIfExists":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserFailIfExists;
                            }
                            break;
                        case "LogonAsService":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserLogonAsService;
                            }
                            break;
                        case "LogonAsBatchJob":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserLogonAsBatchJob;
                            }
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Password":
                            password = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PasswordExpired":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserPasswdChangeReqdOnLogin;
                            }
                            break;
                        case "PasswordNeverExpires":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserDontExpirePasswrd;
                            }
                            break;
                        case "RemoveOnUninstall":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserDontRemoveOnUninstall;
                            }
                            break;
                        case "UpdateIfExists":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserUpdateIfExists;
                            }
                            break;
                        case "Vital":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= UserNonVital;
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

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "GroupRef":
                            if (null == componentId)
                            {
                                SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                this.Core.OnMessage(UtilErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseGroupRefElement(child, id);
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

            if (null != componentId)
            {
                // Reference ConfigureIIs since nothing will happen without it
                if (this.Core.CurrentPlatform == Platform.ARM)
                {
                    // Ensure ARM version of the CA is referenced
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureUsers_ARM");
                }
                else
                {
                    // All other supported platforms use x86
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureUsers");
                }
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "User");
                row[0] = id;
                row[1] = componentId;
                row[2] = name;
                row[3] = domain;
                row[4] = password;
                row[5] = attributes;
            }
        }

        /// <summary>
        /// Parses a XmlFile element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        private void ParseXmlFileElement(XElement node, string componentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string file = null;
            string elementPath = null;
            string name = null;
            string value = null;
            int sequence = -1;
            int flags = 0;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Action":
                            string actionValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (actionValue)
                            {
                                case "createElement":
                                    flags |= 0x00000001; // XMLFILE_CREATE_ELEMENT
                                    break;
                                case "deleteValue":
                                    flags |= 0x00000002; // XMLFILE_DELETE_VALUE
                                    break;
                                case "bulkSetValue":
                                    flags |= 0x00000004; // XMLFILE_BULKWRITE_VALUE
                                    break;
                                case "setValue":
                                    // no flag for set value since it's the default
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Action", actionValue, "createElement", "deleteValue", "setValue", "bulkSetValue"));
                                    break;
                            }
                            break;
                        case "SelectionLanguage":
                            string selectionLanguage = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (selectionLanguage)
                            {
                                case "XPath":
                                    flags |= 0x00000100; // XMLFILE_USE_XPATH
                                    break;
                                case "XSLPattern":
                                    // no flag for since it's the default
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "SelectionLanguage", selectionLanguage, "XPath", "XSLPattern"));
                                    break;
                            }
                            break;
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ElementPath":
                            elementPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Permanent":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 0x00010000; // XMLFILE_DONT_UNINSTALL
                            }
                            break;
                        case "Sequence":
                            sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PreserveModifiedDate":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 0x00001000; // XMLFILE_PRESERVE_MODIFIED
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

            if (null == file)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            if (null == elementPath)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ElementPath"));
            }

            if ((0x00000001 /*XMLFILE_CREATE_ELEMENT*/ & flags) != 0 && null == name)
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Action", "Name"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "XmlFile");
                row[0] = id;
                row[1] = file;
                row[2] = elementPath;
                row[3] = name;
                row[4] = value;
                row[5] = flags;
                row[6] = componentId;
                if (-1 != sequence)
                {
                    row[7] = sequence;
                }
            }

            // Reference SchedXmlFile since nothing will happen without it
            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CA is referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedXmlFile_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedXmlFile");
            }
        }

        /// <summary>
        /// Parses a XmlConfig element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="nested">Whether or not the element is nested.</param>
        private void ParseXmlConfigElement(XElement node, string componentId, bool nested)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string elementId = null;
            string elementPath = null;
            int flags = 0;
            string file = null;
            string name = null;
            int sequence = CompilerConstants.IntegerNotSet;
            string value = null;
            string verifyPath = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Action":
                            if (nested)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            else
                            {
                                string actionValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                                switch (actionValue)
                                {
                                    case "create":
                                        flags |= 0x10; // XMLCONFIG_CREATE
                                        break;
                                    case "delete":
                                        flags |= 0x20; // XMLCONFIG_DELETE
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, actionValue, "create", "delete"));
                                        break;
                                }
                            }
                            break;
                        case "ElementId":
                            elementId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ElementPath":
                            elementPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Node":
                            if (nested)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            else
                            {
                                string nodeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                                switch (nodeValue)
                                {
                                    case "element":
                                        flags |= 0x1; // XMLCONFIG_ELEMENT
                                        break;
                                    case "value":
                                        flags |= 0x2; // XMLCONFIG_VALUE
                                        break;
                                    case "document":
                                        flags |= 0x4; // XMLCONFIG_DOCUMENT
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, nodeValue, "element", "value", "document"));
                                        break;
                                }
                            }
                            break;
                        case "On":
                            if (nested)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            else
                            {
                                string onValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                                switch (onValue)
                                {
                                    case "install":
                                        flags |= 0x100; // XMLCONFIG_INSTALL
                                        break;
                                    case "uninstall":
                                        flags |= 0x200; // XMLCONFIG_UNINSTALL
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, onValue, "install", "uninstall"));
                                        break;
                                }
                            }
                            break;
                        case "PreserveModifiedDate":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                flags |= 0x00001000; // XMLCONFIG_PRESERVE_MODIFIED
                            }
                            break;
                        case "Sequence":
                            sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "VerifyPath":
                            verifyPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == file)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "File"));
            }

            if (null == elementId && null == elementPath)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "ElementId", "ElementPath"));
            }
            else if (null != elementId)
            {
                if (null != elementPath)
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ElementId", "ElementPath"));
                }

                if (0 != flags)
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, "ElementId", "Action", "Node", "On"));
                }

                this.Core.CreateSimpleReference(sourceLineNumbers, "XmlConfig", elementId);
            }

            string innerText = this.Core.GetTrimmedInnerText(node);
            if (null != value)
            {
                // cannot specify both the value attribute and inner text
                if (0 != innerText.Length)
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithInnerText(sourceLineNumbers, node.Name.LocalName, "Value"));
                }
            }
            else // value attribute not specified
            {
                if (0 < innerText.Length)
                {
                    value = innerText;
                }
            }

            // find unexpected child elements
            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "XmlConfig":
                            if (nested)
                            {
                                this.Core.OnMessage(WixErrors.UnexpectedElement(sourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                            }
                            else
                            {
                                this.ParseXmlConfigElement(child, componentId, true);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "XmlConfig");
                row[0] = id;
                row[1] = file;
                row[2] = null == elementId ? elementPath : elementId;
                row[3] = verifyPath;
                row[4] = name;
                row[5] = value;
                row[6] = flags;
                row[7] = componentId;
                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    row[8] = sequence;
                }
            }

            // Reference SchedXmlConfig since nothing will happen without it
            if (this.Core.CurrentPlatform == Platform.ARM)
            {
                // Ensure ARM version of the CA is referenced
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedXmlConfig_ARM");
            }
            else
            {
                // All other supported platforms use x86
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "SchedXmlConfig");
            }
        }

        /// <summary>
        /// Match evaluator to escape properties in a string.
        /// </summary>
        private string EscapeProperties(Match match)
        {
            string escape = null;
            switch (match.Value)
            {
                case "[":
                    escape = @"[\[]";
                    break;
                case "]":
                    escape = @"[\]]";
                    break;
            }

            return escape;
        }

        /// <summary>
        /// Private class that stores the data from a parsed PerformanceCounter element.
        /// </summary>
        private class ParsedPerformanceCounter
        {
            string name;
            string help;
            int type;
            string language;

            internal ParsedPerformanceCounter(string name, string help, System.Diagnostics.PerformanceCounterType type, int language)
            {
                this.name = name;
                this.help = help;
                this.type = (int)type;
                this.language = language.ToString("D3", CultureInfo.InvariantCulture);
            }

            internal string Name
            {
                get { return this.name; }
            }

            internal string Help
            {
                get { return this.help; }
            }

            internal int Type
            {
                get { return this.type; }
            }

            internal string Language
            {
                get { return this.language; }
            }
        }
    }
}
