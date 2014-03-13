//-------------------------------------------------------------------------------------------------
// <copyright file="Inspector.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// WiX source code inspector.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// WiX source code inspector.
    /// </summary>
    public class Inspector
    {
        private const string XmlnsNamespaceURI = "http://www.w3.org/2000/xmlns/";
        private const string WixNamespaceURI = "http://wixtoolset.org/schemas/v4/wxs";
        private const string WixLocalizationNamespaceURI = "http://wixtoolset.org/schemas/v4/wxl";
        private static readonly Regex WixVariableRegex = new Regex(@"(\!|\$)\((?<namespace>loc|wix)\.(?<name>[_A-Za-z][0-9A-Za-z_]+)\)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        private static readonly Dictionary<string, string> OldToNewNamespaceMapping = new Dictionary<string, string>() {
            { "http://schemas.microsoft.com/wix/BalExtension", "http://wixtoolset.org/schemas/v4/wxs/bal" },
            { "http://schemas.microsoft.com/wix/ComPlusExtension", "http://wixtoolset.org/schemas/v4/wxs/complus" },
            { "http://schemas.microsoft.com/wix/DependencyExtension", "http://wixtoolset.org/schemas/v4/wxs/dependency" },
            { "http://schemas.microsoft.com/wix/DifxAppExtension", "http://wixtoolset.org/schemas/v4/wxs/difxapp" },
            { "http://schemas.microsoft.com/wix/FirewallExtension", "http://wixtoolset.org/schemas/v4/wxs/firewall" },
            { "http://schemas.microsoft.com/wix/GamingExtension", "http://wixtoolset.org/schemas/v4/wxs/gaming" },
            { "http://schemas.microsoft.com/wix/IIsExtension", "http://wixtoolset.org/schemas/v4/wxs/iis" },
            { "http://schemas.microsoft.com/wix/MsmqExtension", "http://wixtoolset.org/schemas/v4/wxs/msmq" },
            { "http://schemas.microsoft.com/wix/NetFxExtension", "http://wixtoolset.org/schemas/v4/wxs/netfx" },
            { "http://schemas.microsoft.com/wix/PSExtension", "http://wixtoolset.org/schemas/v4/wxs/powershell" },
            { "http://schemas.microsoft.com/wix/SqlExtension", "http://wixtoolset.org/schemas/v4/wxs/sql" },
            { "http://schemas.microsoft.com/wix/TagExtension", "http://wixtoolset.org/schemas/v4/wxs/tag" },
            { "http://schemas.microsoft.com/wix/UtilExtension", "http://wixtoolset.org/schemas/v4/wxs/util" },
            { "http://schemas.microsoft.com/wix/VSExtension", "http://wixtoolset.org/schemas/v4/wxs/vs" },
            { "http://wixtoolset.org/schemas/thmutil/2010", "http://wixtoolset.org/schemas/v4/thmutil" },
            { "http://schemas.microsoft.com/wix/2009/Lux", "http://wixtoolset.org/schemas/v4/lux" },
            { "http://schemas.microsoft.com/wix/2006/wi", "http://wixtoolset.org/schemas/v4/wxs" },
            { "http://schemas.microsoft.com/wix/2006/localization", "http://wixtoolset.org/schemas/v4/wxl" },
            { "http://schemas.microsoft.com/wix/2006/libraries", "http://wixtoolset.org/schemas/v4/wixlib" },
            { "http://schemas.microsoft.com/wix/2006/objects", "http://wixtoolset.org/schemas/v4/wixobj" },
            { "http://schemas.microsoft.com/wix/2006/outputs", "http://wixtoolset.org/schemas/v4/wixout" },
            { "http://schemas.microsoft.com/wix/2007/pdbs", "http://wixtoolset.org/schemas/v4/wixpdb" },
            { "http://schemas.microsoft.com/wix/2003/04/actions", "http://wixtoolset.org/schemas/v4/wi/actions" },
            { "http://schemas.microsoft.com/wix/2006/tables", "http://wixtoolset.org/schemas/v4/wi/tables" },
            { "http://schemas.microsoft.com/wix/2006/WixUnit", "http://wixtoolset.org/schemas/v4/wixunit" },
        };

        private int errors;
        private Hashtable errorsAsWarnings;
        private Hashtable ignoreErrors;
        private int indentationAmount;
        private string sourceFile;

        /// <summary>
        /// Instantiate a new Inspector class.
        /// </summary>
        /// <param name="errorsAsWarnings">Test errors to display as warnings.</param>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        public Inspector(string[] errorsAsWarnings, string[] ignoreErrors, int indentationAmount)
        {
            this.errorsAsWarnings = new Hashtable();
            this.ignoreErrors = new Hashtable();
            this.indentationAmount = indentationAmount;

            if (null != errorsAsWarnings)
            {
                foreach (string error in errorsAsWarnings)
                {
                    InspectorTestType itt = GetInspectorTestType(error);

                    if (itt != InspectorTestType.Unknown)
                    {
                        this.errorsAsWarnings.Add(itt, null);
                    }
                    else // not a known InspectorTestType
                    {
                        this.OnError(InspectorTestType.InspectorTestTypeUnknown, null, "Unknown error type: '{0}'.", error);
                    }
                }
            }

            if (null != ignoreErrors)
            {
                foreach (string error in ignoreErrors)
                {
                    InspectorTestType itt = GetInspectorTestType(error);

                    if (itt != InspectorTestType.Unknown)
                    {
                        this.ignoreErrors.Add(itt, null);
                    }
                    else // not a known InspectorTestType
                    {
                        this.OnError(InspectorTestType.InspectorTestTypeUnknown, null, "Unknown error type: '{0}'.", error);
                    }
                }
            }
        }

        /// <summary>
        /// Inspector test types.  These are used to condition error messages down to warnings.
        /// </summary>
        private enum InspectorTestType
        {
            /// <summary>
            /// Internal-only: returned when a string cannot be converted to an InspectorTestType.
            /// </summary>
            Unknown,

            /// <summary>
            /// Internal-only: displayed when a string cannot be converted to an InspectorTestType.
            /// </summary>
            InspectorTestTypeUnknown,

            /// <summary>
            /// Displayed when an XML loading exception has occurred.
            /// </summary>
            XmlException,

            /// <summary>
            /// Displayed when a file cannot be accessed; typically when trying to save back a fixed file.
            /// </summary>
            UnauthorizedAccessException,

            /// <summary>
            /// Displayed when the encoding attribute in the XML declaration is not 'UTF-8'.
            /// </summary>
            DeclarationEncodingWrong,

            /// <summary>
            /// Displayed when the XML declaration is missing from the source file.
            /// </summary>
            DeclarationMissing,

            /// <summary>
            /// Displayed when the whitespace preceding a CDATA node is wrong.
            /// </summary>
            WhitespacePrecedingCDATAWrong,

            /// <summary>
            /// Displayed when the whitespace preceding a node is wrong.
            /// </summary>
            WhitespacePrecedingNodeWrong,

            /// <summary>
            /// Displayed when an element is not empty as it should be.
            /// </summary>
            NotEmptyElement,

            /// <summary>
            /// Displayed when the whitespace following a CDATA node is wrong.
            /// </summary>
            WhitespaceFollowingCDATAWrong,

            /// <summary>
            /// Displayed when the whitespace preceding an end element is wrong.
            /// </summary>
            WhitespacePrecedingEndElementWrong,

            /// <summary>
            /// Displayed when the xmlns attribute is missing from the document element.
            /// </summary>
            XmlnsMissing,

            /// <summary>
            /// Displayed when the xmlns attribute on the document element is wrong.
            /// </summary>
            XmlnsValueWrong,

            /// <summary>
            /// Displayed when a Category element has an empty AppData attribute.
            /// </summary>
            CategoryAppDataEmpty,

            /// <summary>
            /// Displayed when a Registry element encounters an error while being converted
            /// to a strongly-typed WiX COM element.
            /// </summary>
            COMRegistrationTyper,

            /// <summary>
            /// Displayed when an UpgradeVersion element has an empty RemoveFeatures attribute.
            /// </summary>
            UpgradeVersionRemoveFeaturesEmpty,

            /// <summary>
            /// Displayed when a Feature element contains the deprecated FollowParent attribute.
            /// </summary>
            FeatureFollowParentDeprecated,

            /// <summary>
            /// Displayed when a RadioButton element is missing the Value attribute.
            /// </summary>
            RadioButtonMissingValue,

            /// <summary>
            /// Displayed when a TypeLib element contains a Description element with an empty
            /// string value.
            /// </summary>
            TypeLibDescriptionEmpty,

            /// <summary>
            /// Displayed when a RelativePath attribute occurs on an unadvertised Class element.
            /// </summary>
            ClassRelativePathMustBeAdvertised,

            /// <summary>
            /// Displayed when a Class element has an empty Description attribute.
            /// </summary>
            ClassDescriptionEmpty,

            /// <summary>
            /// Displayed when a ServiceInstall element has an empty LocalGroup attribute.
            /// </summary>
            ServiceInstallLocalGroupEmpty,

            /// <summary>
            /// Displayed when a ServiceInstall element has an empty Password attribute.
            /// </summary>
            ServiceInstallPasswordEmpty,

            /// <summary>
            /// Displayed when a Shortcut element has an empty WorkingDirectory attribute.
            /// </summary>
            ShortcutWorkingDirectoryEmpty,

            /// <summary>
            /// Displayed when a IniFile element has an empty Value attribute.
            /// </summary>
            IniFileValueEmpty,

            /// <summary>
            /// Displayed when a FileSearch element has a Name attribute that contains both the short
            /// and long versions of the file name.
            /// </summary>
            FileSearchNamesCombined,

            /// <summary>
            /// Displayed when a WebApplicationExtension element has a deprecated Id attribute.
            /// </summary>
            WebApplicationExtensionIdDeprecated,

            /// <summary>
            /// Displayed when a WebApplicationExtension element has an empty Id attribute.
            /// </summary>
            WebApplicationExtensionIdEmpty,

            /// <summary>
            /// Displayed when a Property element has an empty Value attribute.
            /// </summary>
            PropertyValueEmpty,

            /// <summary>
            /// Displayed when a Control element has an empty CheckBoxValue attribute.
            /// </summary>
            ControlCheckBoxValueEmpty,

            /// <summary>
            /// Displayed when a deprecated RadioGroup element is found.
            /// </summary>
            RadioGroupDeprecated,

            /// <summary>
            /// Displayed when a Progress element has an empty TextTemplate attribute.
            /// </summary>
            ProgressTextTemplateEmpty,

            /// <summary>
            /// Displayed when a RegistrySearch element has a Type attribute set to 'registry'.
            /// </summary>
            RegistrySearchTypeRegistryDeprecated,

            /// <summary>
            /// Displayed when a WebFilter/@LoadOrder attribute has a value that is not more stongly typed.
            /// </summary>
            WebFilterLoadOrderIncorrect,

            /// <summary>
            /// Displayed when an element contains a deprecated src attribute.
            /// </summary>
            SrcIsDeprecated,

            /// <summary>
            /// Displayed when a Component element is missing the required Guid attribute.
            /// </summary>
            RequireComponentGuid,

            /// <summary>
            /// Displayed when an element has a LongName attribute.
            /// </summary>
            LongNameDeprecated,

            /// <summary>
            /// Displayed when a RemoveFile element has no Name or LongName attribute.
            /// </summary>
            RemoveFileNameRequired,

            /// <summary>
            /// Displayed when a localization variable begins with the deprecated '$' character.
            /// </summary>
            DeprecatedLocalizationVariablePrefix,

            /// <summary>
            /// Displayed when the namespace of an element has changed.
            /// </summary>
            NamespaceChanged,

            /// <summary>
            /// Displayed when an UpgradeVersion element is missing the required Property attribute.
            /// </summary>
            UpgradeVersionPropertyAttributeRequired,

            /// <summary>
            /// Displayed when an Upgrade element contains a deprecated Property child element.
            /// </summary>
            UpgradePropertyChild,

            /// <summary>
            /// Displayed when a deprecated Registry element is found.
            /// </summary>
            RegistryElementDeprecated,

            /// <summary>
            /// Displayed when a PatchSequence/@Supersede attribute contains a deprecated integer value.
            /// </summary>
            PatchSequenceSupersedeTypeChanged,

            /// <summary>
            /// Displayed when a deprecated PatchSequence/@Target attribute is found.
            /// </summary>
            PatchSequenceTargetDeprecated,

            /// <summary>
            /// Displayed when a deprecated Verb/@Target attribute is found.
            /// </summary>
            VerbTargetDeprecated,

            /// <summary>
            /// Displayed when a ProgId/@Icon attribute value contains a formatted string.
            /// </summary>
            ProgIdIconFormatted,

            /// <summary>
            /// Displayed when a deprecated IgnoreModularization element is found.
            /// </summary>
            IgnoreModularizationDeprecated,

            /// <summary>
            /// Displayed when a Package/@Compressed attribute is found under a Module element.
            /// </summary>
            PackageCompressedIllegal,

            /// <summary>
            /// Displayed when a Package/@Platforms attribute is found.
            /// </summary>
            PackagePlatformsDeprecated,

            /// <summary>
            /// Displayed when a Package/@Platform attribute has the deprecated value "intel"
            /// </summary>
            PackagePlatformIntel,

            /// <summary>
            /// Displayed when a Package/@Platform attribute has the deprecated value "intel64"
            /// </summary>
            PackagePlatformIntel64,

            /// <summary>
            /// Displayed when a deprecated Module/@Guid attribute is found.
            /// </summary>
            ModuleGuidDeprecated,

            /// <summary>
            /// Displayed when a deprecated guid wildcard value is found.
            /// </summary>
            GuidWildcardDeprecated,

            /// <summary>
            /// Displayed when a FragmentRef Element is found.
            /// </summary>
            FragmentRefIllegal,

            /// <summary>
            /// Displayed when a File/@Name matches a File/@ShortName.
            /// </summary>
            FileRedundantNames,

            /// <summary>
            /// Displayed when a FileSearch(Ref) element has an invalid parent.
            /// </summary>
            FileSearchParentInvalid,

            /// <summary>
            /// Displayed when an optional attribute is specified with its default value.
            /// </summary>
            DefaultOptionalAttribute,

            /// <summary>
            /// Displayed when an attribute that WiX generates is specified with an explicit value.
            /// (Rarely an error but indicates authoring that likely can be simplified.)
            /// </summary>
            ExplicitGeneratedAttribute,

            /// <summary>
            /// Displayed when an identifier for a ComponentGroup or ComponentGroupRef contains invalid characters..
            /// </summary>
            InvalidIdentifier
        }

        /// <summary>
        /// Inspect a file.
        /// </summary>
        /// <param name="inspectSourceFile">The file to inspect.</param>
        /// <param name="fixErrors">Option to fix errors that are found.</param>
        /// <returns>The number of errors found.</returns>
        public int InspectFile(string inspectSourceFile, bool fixErrors)
        {
            XmlTextReader reader = null;
            XmlWriter writer = null;
            LineInfoDocument doc = null;

            try
            {
                // set the instance info
                this.errors = 0;
                this.sourceFile = inspectSourceFile;

                // load the xml
                reader = new XmlTextReader(this.sourceFile);
                doc = new LineInfoDocument();
                doc.PreserveWhitespace = true;
                doc.Load(reader);
            }
            catch (XmlException xe)
            {
                this.OnError(InspectorTestType.XmlException, null, "The xml is invalid.  Detail: '{0}'", xe.Message);
                return this.errors;
            }
            finally
            {
                if (null != reader)
                {
                    reader.Close();
                }
            }

            // inspect the document
            this.InspectDocument(doc);

            // fix errors if necessary
            if (fixErrors && 0 < this.errors)
            {
                try
                {
                    using (StreamWriter sw = File.CreateText(inspectSourceFile))
                    {
                        writer = new XmlTextWriter(sw);
                        doc.WriteTo(writer);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    this.OnError(InspectorTestType.UnauthorizedAccessException, null, "Could not write to file.");
                }
                finally
                {
                    if (null != writer)
                    {
                        writer.Close();
                    }
                }
            }

            return this.errors;
        }

        /// <summary>
        /// Get the strongly-typed InspectorTestType for a string representation of the same.
        /// </summary>
        /// <param name="inspectorTestType">The InspectorTestType represented by the string.</param>
        /// <returns>The InspectorTestType value if found; otherwise InspectorTestType.Unknown.</returns>
        private static InspectorTestType GetInspectorTestType(string inspectorTestType)
        {
            foreach (InspectorTestType itt in Enum.GetValues(typeof(InspectorTestType)))
            {
                if (itt.ToString() == inspectorTestType)
                {
                    return itt;
                }
            }

            return InspectorTestType.Unknown;
        }

        /// <summary>
        /// Fix the whitespace in a Whitespace node.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level of the desired whitespace.</param>
        /// <param name="whitespace">The whitespace node to fix.</param>
        private static void FixWhitespace(int indentationAmount, int level, XmlNode whitespace)
        {
            int newLineCount = 0;

            for (int i = 0; i + 1 < whitespace.Value.Length; i++)
            {
                if (Environment.NewLine == whitespace.Value.Substring(i, 2))
                {
                    i++; // skip an extra character
                    newLineCount++;
                }
            }

            if (0 == newLineCount)
            {
                newLineCount = 1;
            }

            // reset the whitespace value
            whitespace.Value = string.Empty;

            // add the correct number of newlines
            for (int i = 0; i < newLineCount; i++)
            {
                whitespace.Value = string.Concat(whitespace.Value, Environment.NewLine);
            }

            // add the correct number of spaces based on configured indentation amount
            whitespace.Value = string.Concat(whitespace.Value, new string(' ', level * indentationAmount));
        }

        /// <summary>
        /// Replace one element with another, copying all the attributes and child nodes.
        /// </summary>
        /// <param name="sourceElement">The source element.</param>
        /// <param name="destinationElement">The destination element.</param>
        private static void ReplaceElement(XmlElement sourceElement, XmlElement destinationElement)
        {
            if (sourceElement == sourceElement.OwnerDocument.DocumentElement)
            {
                sourceElement.OwnerDocument.ReplaceChild(destinationElement, sourceElement);
            }
            else
            {
                sourceElement.ParentNode.ReplaceChild(destinationElement, sourceElement);
            }

            // move all the attributes from the old element to the new element
            SortedList xmlnsAttributes = new SortedList();
            while (sourceElement.Attributes.Count > 0)
            {
                XmlAttribute attribute = sourceElement.Attributes[0];

                sourceElement.Attributes.Remove(attribute);

                // migrate any attribute other than xmlns
                if (attribute.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                {
                    // migrate prefix xmlns attribute after all normal attributes
                    if ("xmlns" == attribute.LocalName)
                    {
                        attribute.Value = destinationElement.NamespaceURI;
                        xmlnsAttributes.Add(String.Empty, attribute);
                    }
                    else
                    {
                        xmlnsAttributes.Add(attribute.LocalName, attribute);
                    }
                }
                else
                {
                    destinationElement.Attributes.Append(attribute);
                }
            }

            // add the xmlns attributes back in alphabetical order
            foreach (XmlAttribute attribute in xmlnsAttributes.Values)
            {
                destinationElement.Attributes.Append(attribute);
            }

            // move all the child nodes from the old element to the new element
            while (sourceElement.ChildNodes.Count > 0)
            {
                XmlNode node = sourceElement.ChildNodes[0];

                sourceElement.RemoveChild(node);
                destinationElement.AppendChild(node);
            }
        }

        /// <summary>
        /// Set the namespace URI for an element and all its children.
        /// </summary>
        /// <param name="element">The element which will get its namespace URI set.</param>
        /// <param name="namespaceURI">The namespace URI to set.</param>
        /// <returns>The modified element.</returns>
        private static XmlElement SetNamespaceURI(XmlElement element, string namespaceURI)
        {
            XmlElement newElement = element.OwnerDocument.CreateElement(element.LocalName, namespaceURI);

            ReplaceElement(element, newElement);

            for (int i = 0; i < newElement.ChildNodes.Count; i++)
            {
                XmlNode childNode = newElement.ChildNodes[i];

                if (XmlNodeType.Element == childNode.NodeType && childNode.NamespaceURI == element.NamespaceURI)
                {
                    SetNamespaceURI((XmlElement)childNode, namespaceURI);
                }
            }

            return newElement;
        }

        /// <summary>
        /// Determine if the whitespace preceding a node is appropriate for its depth level.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level that should match this whitespace.</param>
        /// <param name="whitespace">The whitespace to validate.</param>
        /// <returns>true if the whitespace is legal; false otherwise.</returns>
        private static bool IsLegalWhitespace(int indentationAmount, int level, string whitespace)
        {
            // strip off leading newlines; there can be an arbitrary number of these
            while (whitespace.StartsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                whitespace = whitespace.Substring(Environment.NewLine.Length);
            }

            // check the length
            if (whitespace.Length != level * indentationAmount)
            {
                return false;
            }

            // check the spaces
            foreach (char character in whitespace)
            {
                if (' ' != character)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Inspect an XML document.
        /// </summary>
        /// <param name="doc">The XML document to inspect.</param>
        private void InspectDocument(XmlDocument doc)
        {
            // inspect the declaration
            if (XmlNodeType.XmlDeclaration == doc.FirstChild.NodeType)
            {
                XmlDeclaration declaration = (XmlDeclaration)doc.FirstChild;

                if (!String.Equals("utf-8", declaration.Encoding, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.OnError(InspectorTestType.DeclarationEncodingWrong, declaration, "The XML declaration encoding is not properly set to 'utf-8'."))
                    {
                        declaration.Encoding = "utf-8";
                    }
                }
            }
            else // missing declaration
            {
                if (this.OnError(InspectorTestType.DeclarationMissing, null, "This file is missing an XML declaration on the first line."))
                {
                    XmlNode xmlDecl = doc.PrependChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
                    doc.InsertAfter(doc.CreateWhitespace("\r\n"), xmlDecl);
                }
            }

            // start inspecting the nodes at the document element
            this.InspectNode(doc.DocumentElement, 0);
        }

        /// <summary>
        /// Inspect a single xml node.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="level">The depth level of the node.</param>
        /// <returns>The inspected node.</returns>
        private XmlNode InspectNode(XmlNode node, int level)
        {
            // inspect this node's whitespace
            if ((XmlNodeType.Comment == node.NodeType && 0 > node.Value.IndexOf(Environment.NewLine, StringComparison.Ordinal)) ||
                XmlNodeType.CDATA == node.NodeType || XmlNodeType.Element == node.NodeType || XmlNodeType.ProcessingInstruction == node.NodeType)
            {
                this.InspectWhitespace(node, level);
            }

            // inspect this node
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    // first inspect the attributes of the node in a very generic fashion
                    foreach (XmlAttribute attribute in node.Attributes)
                    {
                        this.InspectAttribute(attribute);
                    }

                    // inspect the node in much greater detail
                    if ("http://wixtoolset.org/schemas/v4/2005/10/sca" == node.NamespaceURI)
                    {
                    }
                    else
                    {
                        XmlElement element = (XmlElement)node;

                        switch (node.LocalName)
                        {
                            case "Wix":
                                node = this.InspectWixElement(element);
                                break;
                            case "WixLocalization":
                                node = this.InspectWixLocalizationElement(element);
                                break;
                        }
                    }
                    break;

                case XmlNodeType.Text:
                    this.InspectText((XmlText)node);
                    break;
            }

            // inspect all children of this node
            if (null != node)
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    XmlNode child = node.ChildNodes[i];

                    XmlNode inspectedNode = this.InspectNode(child, level + 1);

                    // inspected node was deleted, don't skip the next one
                    if (null == inspectedNode)
                    {
                        i--;
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// Inspect an attribute.
        /// </summary>
        /// <param name="attribute">The attribute to inspect.</param>
        private void InspectAttribute(XmlAttribute attribute)
        {
            // Check for namespaces that need updating.
            if (XmlnsNamespaceURI == attribute.NamespaceURI)
            {
                string newNamespaceUri;
                if (Inspector.OldToNewNamespaceMapping.TryGetValue(attribute.Value, out newNamespaceUri))
                {
                    if (this.OnError(InspectorTestType.XmlnsValueWrong, attribute, "The namespace '{0}' is out of date.  It must be '{1}'.", attribute.Value, newNamespaceUri))
                    {
                        attribute.Value = newNamespaceUri;
                    }
                }
            }
            else // check text.
            {
                MatchCollection matches = WixVariableRegex.Matches(attribute.Value);

                foreach (Match match in matches)
                {
                    if ('$' == attribute.Value[match.Index])
                    {
                        if (this.OnError(InspectorTestType.DeprecatedLocalizationVariablePrefix, attribute, "The localization variable $(loc.{0}) uses a deprecated prefix '$'.  Please use the '!' prefix instead.  Since the prefix '$' is also used by the preprocessor, it has been deprecated to avoid namespace collisions.", match.Groups["name"].Value))
                        {
                            attribute.Value = attribute.Value.Remove(match.Index, 1);
                            attribute.Value = attribute.Value.Insert(match.Index, "!");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inspect a text node.
        /// </summary>
        /// <param name="text">The text node to inspect.</param>
        private void InspectText(XmlText text)
        {
            MatchCollection matches = WixVariableRegex.Matches(text.Value);

            foreach (Match match in matches)
            {
                if ('$' == text.Value[match.Index])
                {
                    if (this.OnError(InspectorTestType.DeprecatedLocalizationVariablePrefix, text, "The localization variable $(loc.{0}) uses a deprecated prefix '$'.  Please use the '!' prefix instead.  Since the prefix '$' is also used by the preprocessor, it has been deprecated to avoid namespace collisions.", match.Groups["name"].Value))
                    {
                        text.Value = text.Value.Remove(match.Index, 1);
                        text.Value = text.Value.Insert(match.Index, "!");
                    }
                }
            }
        }

        /// <summary>
        /// Inspect the whitespace adjacent to a node.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="level">The depth level of the node.</param>
        private void InspectWhitespace(XmlNode node, int level)
        {
            // fix the whitespace before this node
            XmlNode whitespace = node.PreviousSibling;
            if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType)
            {
                if (XmlNodeType.CDATA == node.NodeType)
                {
                    if (this.OnError(InspectorTestType.WhitespacePrecedingCDATAWrong, node, "There should be no whitespace preceding a CDATA node."))
                    {
                        whitespace.ParentNode.RemoveChild(whitespace);
                    }
                }
                else
                {
                    if (!IsLegalWhitespace(this.indentationAmount, level, whitespace.Value))
                    {
                        if (this.OnError(InspectorTestType.WhitespacePrecedingNodeWrong, node, "The whitespace preceding this node is incorrect."))
                        {
                            FixWhitespace(this.indentationAmount, level, whitespace);
                        }
                    }
                }
            }

            // fix the whitespace inside this node (except for Error which may contain just whitespace)
            if (XmlNodeType.Element == node.NodeType && "Error" != node.LocalName)
            {
                XmlElement element = (XmlElement)node;

                if (!element.IsEmpty && String.IsNullOrEmpty(element.InnerXml.Trim()))
                {
                    if (this.OnError(InspectorTestType.NotEmptyElement, element, "This should be an empty element since it contains nothing but whitespace."))
                    {
                        element.IsEmpty = true;
                    }
                }
            }

            // fix the whitespace before the end element or after for CDATA nodes
            if (XmlNodeType.CDATA == node.NodeType)
            {
                whitespace = node.NextSibling;
                if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType)
                {
                    if (this.OnError(InspectorTestType.WhitespaceFollowingCDATAWrong, node, "There should be no whitespace following a CDATA node."))
                    {
                        whitespace.ParentNode.RemoveChild(whitespace);
                    }
                }
            }
            else if (XmlNodeType.Element == node.NodeType)
            {
                whitespace = node.LastChild;

                // Error may contain just whitespace
                if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType && "Error" != node.LocalName)
                {
                    if (!IsLegalWhitespace(this.indentationAmount, level, whitespace.Value))
                    {
                        if (this.OnError(InspectorTestType.WhitespacePrecedingEndElementWrong, whitespace, "The whitespace preceding this end element is incorrect."))
                        {
                            FixWhitespace(this.indentationAmount, level, whitespace);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inspects an Include element.
        /// </summary>
        /// <param name="element">The Include element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectIncludeElement(XmlElement element)
        {
            XmlAttribute xmlns = element.GetAttributeNode("xmlns");

            if (null == xmlns)
            {
                if (this.OnError(InspectorTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixNamespaceURI))
                {
                    return SetNamespaceURI(element, WixNamespaceURI);
                }
            }
            else if (WixNamespaceURI != xmlns.Value)
            {
                if (this.OnError(InspectorTestType.XmlnsValueWrong, xmlns, "The xmlns attribute's value is wrong.  It must be '{0}'.", WixNamespaceURI))
                {
                    return SetNamespaceURI(element, WixNamespaceURI);
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects a Package element.
        /// </summary>
        /// <param name="element">The Package element to inspect.</param>
        private void InspectPackageElement(XmlElement element)
        {
            XmlAttribute compressed = element.GetAttributeNode("Compressed");
            XmlAttribute id = element.GetAttributeNode("Id");
            XmlAttribute platforms = element.GetAttributeNode("Platforms");
            XmlAttribute platform = element.GetAttributeNode("Platform");

            if ("Module" == element.ParentNode.LocalName)
            {
                if (null != compressed)
                {
                    if (this.OnError(InspectorTestType.PackageCompressedIllegal, compressed, "The Package/@Compressed attribute is illegal under a Module element because merge modules must always be compressed."))
                    {
                        element.Attributes.Remove(compressed);
                    }
                }
            }

            if (null != id && 0 <= id.Value.IndexOf("????????-????-????-????-????????????", StringComparison.Ordinal))
            {
                if (this.OnError(InspectorTestType.GuidWildcardDeprecated, id, "The guid value '{0}' is deprecated.  Remove the Package/@Id attribute to get the same functionality.", id.Value))
                {
                    element.Attributes.Remove(id);
                }
            }

            if (null != platforms)
            {
                if (this.OnError(InspectorTestType.PackagePlatformsDeprecated, platforms, "The Package/@Platforms attribute is deprecated. Use Package/@Platform instead.  Platform accepts only a single platform (x86, x64, or ia64). If the value in Package/@Platforms corresponds to one of those values, it will be updated."))
                {
                    string platformsValue = platforms.Value.ToLower();
                    if ("intel" == platformsValue || "x64" == platformsValue || "intel64" == platformsValue)
                    {
                        XmlAttribute newPlatform = element.OwnerDocument.CreateAttribute("Platform");
                        switch (platformsValue)
                        {
                            case "intel":
                                platformsValue = "x86";
                                break;
                            case "x64":
                                break;
                            case "intel64":
                                platformsValue = "ia64";
                                break;
                        }
                        newPlatform.Value = platformsValue;
                        element.Attributes.InsertAfter(newPlatform, platforms);
                        element.Attributes.Remove(platforms);
                    }
                }
            }

            if (null != platform && platform.Value == "intel")
            {
                if (this.OnError(InspectorTestType.PackagePlatformIntel, platform, "The Package/@Platform attribute value 'intel' is deprecated. Use 'x86' instead."))
                {
                    platform.Value = "x86";
                }
            }

            if (null != platform && platform.Value == "intel64")
            {
                if (this.OnError(InspectorTestType.PackagePlatformIntel64, platform, "The Package/@Platform attribute value 'intel64' is deprecated. Use 'ia64' instead."))
                {
                    platform.Value = "ia64";
                }
            }
        }

        /// <summary>
        /// Inspects a Wix element.
        /// </summary>
        /// <param name="element">The Wix element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectWixElement(XmlElement element)
        {
            XmlAttribute xmlns = element.GetAttributeNode("xmlns");

            if (null == xmlns)
            {
                if (this.OnError(InspectorTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixNamespaceURI))
                {
                    return SetNamespaceURI(element, WixNamespaceURI);
                }
            }
            else if (WixNamespaceURI != xmlns.Value)
            {
                if (this.OnError(InspectorTestType.XmlnsValueWrong, xmlns, "The xmlns attribute's value is wrong.  It must be '{0}'.", WixNamespaceURI))
                {
                    xmlns.Value = WixNamespaceURI;
                }
            }

            return element;
        }

        /// <summary>
        /// Inspects a WixLocalization element.
        /// </summary>
        /// <param name="element">The Wix element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private XmlElement InspectWixLocalizationElement(XmlElement element)
        {
            XmlAttribute xmlns = element.GetAttributeNode("xmlns");

            if (null == xmlns)
            {
                if (this.OnError(InspectorTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixLocalizationNamespaceURI))
                {
                    return SetNamespaceURI(element, WixLocalizationNamespaceURI);
                }
            }
            else if (WixLocalizationNamespaceURI != xmlns.Value)
            {
                if (this.OnError(InspectorTestType.XmlnsValueWrong, xmlns, "The xmlns attribute's value is wrong.  It must be '{0}'.", WixLocalizationNamespaceURI))
                {
                    return SetNamespaceURI(element, WixLocalizationNamespaceURI);
                }
            }

            return element;
        }

        /// <summary>
        /// Output an error message to the console.
        /// </summary>
        /// <param name="inspectorTestType">The type of inspector test.</param>
        /// <param name="node">The node that caused the error.</param>
        /// <param name="message">Detailed error message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        /// <returns>Returns true indicating that action should be taken on this error, and false if it should be ignored.</returns>
        private bool OnError(InspectorTestType inspectorTestType, XmlNode node, string message, params object[] args)
        {
            if (this.ignoreErrors.Contains(inspectorTestType)) // ignore the error
            {
                return false;
            }

            // increase the error count
            this.errors++;

            // set the warning/error part of the message
            string warningError;
            if (this.errorsAsWarnings.Contains(inspectorTestType)) // error as warning
            {
                warningError = "warning";
            }
            else // normal error
            {
                warningError = "error";
            }

            if (null != node)
            {
                Console.WriteLine("{0}({1}) : {2} WXCP{3:0000} : {4} ({5})", this.sourceFile, ((IXmlLineInfo)node).LineNumber, warningError, (int)inspectorTestType, String.Format(CultureInfo.CurrentCulture, message, args), inspectorTestType.ToString());
            }
            else
            {
                string source = (null == this.sourceFile ? "wixcop.exe" : this.sourceFile);

                Console.WriteLine("{0} : {1} WXCP{2:0000} : {3} ({4})", source, warningError, (int)inspectorTestType, String.Format(CultureInfo.CurrentCulture, message, args), inspectorTestType.ToString());
            }

            return true;
        }

        /// <summary>
        /// Output a message to the console.
        /// </summary>
        /// <param name="node">The node that caused the message.</param>
        /// <param name="message">Detailed message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        private void OnVerbose(XmlNode node, string message, params string[] args)
        {
            this.errors++;

            if (null != node)
            {
                Console.WriteLine("{0}({1}) : {2}", this.sourceFile, ((IXmlLineInfo)node).LineNumber, String.Format(CultureInfo.CurrentCulture, message, args));
            }
            else
            {
                string source = (null == this.sourceFile ? "wixcop.exe" : this.sourceFile);

                Console.WriteLine("{0} : {1}", source, String.Format(CultureInfo.CurrentCulture, message, args));
            }
        }
    }
}
