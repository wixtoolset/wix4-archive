//-------------------------------------------------------------------------------------------------
// <copyright file="Inspector.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// WiX source code inspector.
    /// </summary>
    public class Inspector
    {
        private const string XDocumentNewLine = "\n"; // XDocument normlizes "\r\n" to just "\n".
        private static readonly XNamespace WixNamespace = "http://wixtoolset.org/schemas/v4/wxs";

        private static readonly XName FileElementName = WixNamespace + "File";
        private static readonly XName ExePackageElementName = WixNamespace + "ExePackage";
        private static readonly XName MsiPackageElementName = WixNamespace + "MsiPackage";
        private static readonly XName MspPackageElementName = WixNamespace + "MspPackage";
        private static readonly XName MsuPackageElementName = WixNamespace + "MsuPackage";
        private static readonly XName PayloadElementName = WixNamespace + "Payload";
        private static readonly XName WixElementWithoutNamespaceName = XNamespace.None + "Wix";

        private static readonly Dictionary<string, XNamespace> OldToNewNamespaceMapping = new Dictionary<string, XNamespace>()
        {
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

        private Dictionary<XName, Action<XElement>> InspectElementMapping;

        /// <summary>
        /// Instantiate a new Inspector class.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="errorsAsWarnings">Test errors to display as warnings.</param>
        /// <param name="ignoreErrors">Test errors to ignore.</param>
        public Inspector(int indentationAmount, IEnumerable<string> errorsAsWarnings = null, IEnumerable<string> ignoreErrors = null)
        {
            this.InspectElementMapping = new Dictionary<XName, Action<XElement>>()
            {
                { FileElementName, this.InspectFileElement },
                { ExePackageElementName, this.InspectChainPackageElement },
                { MsiPackageElementName, this.InspectChainPackageElement },
                { MspPackageElementName, this.InspectChainPackageElement },
                { MsuPackageElementName, this.InspectChainPackageElement },
                { PayloadElementName, this.InspectPayloadElement },
                { WixElementWithoutNamespaceName, this.InspectWixElementWithoutNamespace },
            };

            this.IndentationAmount = indentationAmount;

            this.ErrorsAsWarnings = new HashSet<InspectorTestType>(this.YieldInspectorTypes(errorsAsWarnings));

            this.IgnoreErrors = new HashSet<InspectorTestType>(this.YieldInspectorTypes(ignoreErrors));
        }

        private int Errors { get; set; }

        private HashSet<InspectorTestType> ErrorsAsWarnings { get; set; }

        private HashSet<InspectorTestType> IgnoreErrors { get; set; }

        private int IndentationAmount { get; set; }

        private string SourceFile { get; set; }

        /// <summary>
        /// Inspector test types.  These are used to condition error messages down to warnings.
        /// </summary>
        private enum InspectorTestType
        {
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
            /// Assign an identifier to a File element when on Id attribute is specified.
            /// </summary>
            AssignAnonymousFileId,

            /// <summary>
            /// SuppressSignatureValidation attribute is deprecated and replaced with EnableSignatureValidation.
            /// </summary>
            SuppressSignatureValidationDeprecated,
        }

        /// <summary>
        /// Inspect a file.
        /// </summary>
        /// <param name="inspectSourceFile">The file to inspect.</param>
        /// <param name="fixErrors">Option to fix errors that are found.</param>
        /// <returns>The number of errors found.</returns>
        public int InspectFile(string inspectSourceFile, bool fixErrors)
        {
            XDocument document;

            // Set the instance info.
            this.Errors = 0;
            this.SourceFile = inspectSourceFile;

            try
            {
                document = XDocument.Load(this.SourceFile, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
            catch (XmlException e)
            {
                this.OnError(InspectorTestType.XmlException, (XObject)null, "The xml is invalid.  Detail: '{0}'", e.Message);

                return this.Errors;
            }

            this.InspectDocument(document);


            // Fix errors if requested and necessary.
            if (fixErrors && 0 < this.Errors)
            {
                try
                {
                    using (StreamWriter writer = File.CreateText(this.SourceFile))
                    {
                        document.Save(writer, SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    this.OnError(InspectorTestType.UnauthorizedAccessException, (XObject)null, "Could not write to file.");
                }
            }

            return this.Errors;
        }

        /// <summary>
        /// Inspect a document.
        /// </summary>
        /// <param name="document">The document to inspect.</param>
        /// <returns>The number of errors found.</returns>
        public int InspectDocument(XDocument document)
        {
            XDeclaration declaration = document.Declaration;

            // inspect the declaration
            if (null != declaration)
            {
                if (!String.Equals("utf-8", declaration.Encoding, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.OnError(InspectorTestType.DeclarationEncodingWrong, document.Root, "The XML declaration encoding is not properly set to 'utf-8'."))
                    {
                        declaration.Encoding = "utf-8";
                    }
                }
            }
            else // missing declaration
            {
                if (this.OnError(InspectorTestType.DeclarationMissing, (XNode)null, "This file is missing an XML declaration on the first line."))
                {
                    document.Declaration = new XDeclaration("1.0", "utf-8", null);
                    document.Root.AddBeforeSelf(new XText(XDocumentNewLine));
                }
            }

            // Start inspecting the nodes at the top.
            this.InspectNode(document.Root, 0);

            return this.Errors;
        }

        /// <summary>
        /// Inspect a single xml node.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="level">The depth level of the node.</param>
        /// <returns>The inspected node.</returns>
        private void InspectNode(XNode node, int level)
        {
            // Inspect this node's whitespace.
            if ((XmlNodeType.Comment == node.NodeType && 0 > ((XComment)node).Value.IndexOf(XDocumentNewLine, StringComparison.Ordinal)) ||
                XmlNodeType.CDATA == node.NodeType || XmlNodeType.Element == node.NodeType || XmlNodeType.ProcessingInstruction == node.NodeType)
            {
                this.InspectWhitespace(node, level);
            }

            // Inspect this node if it is an element.
            XElement element = node as XElement;

            if (null != element)
            {
                this.InspectElement(element);

                // inspect all children of this element.
                List<XNode> children = element.Nodes().ToList();

                foreach (XNode child in children)
                {
                    this.InspectNode(child, level + 1);
                }
            }
        }

        private void InspectElement(XElement element)
        {
            // Gather any deprecated namespaces, then update this element tree based on those deprecations.
            Dictionary<XNamespace, XNamespace> deprecatedToUpdatedNamespaces = new Dictionary<XNamespace, XNamespace>();

            foreach (XAttribute declaration in element.Attributes().Where(a => a.IsNamespaceDeclaration))
            {
                XNamespace ns;

                if (Inspector.OldToNewNamespaceMapping.TryGetValue(declaration.Value, out ns))
                {
                    if (this.OnError(InspectorTestType.XmlnsValueWrong, declaration, "The namespace '{0}' is out of date.  It must be '{1}'.", declaration.Value, ns.NamespaceName))
                    {
                        deprecatedToUpdatedNamespaces.Add(declaration.Value, ns);
                    }
                }
            }

            if (deprecatedToUpdatedNamespaces.Any())
            {
                UpdateElementsWithDeprecatedNamespaces(element.DescendantsAndSelf(), deprecatedToUpdatedNamespaces);
            }

            // Inspect the node in much greater detail.
            Action<XElement> convert;

            if (this.InspectElementMapping.TryGetValue(element.Name, out convert))
            {
                convert(element);
            }
        }

        private void InspectFileElement(XElement element)
        {
            if (null == element.Attribute("Id"))
            {
                XAttribute attribute = element.Attribute("Name");

                if (null == attribute)
                {
                    attribute = element.Attribute("Source");
                }

                if (null == attribute)
                {
                    // TODO: do something since we can't get an id for this file
                }
                else
                {
                    string name = Path.GetFileName(attribute.Value);

                    if (this.OnError(InspectorTestType.AssignAnonymousFileId, element, "The file id is being updated to '{0}' to ensure it remains the same as the default", name))
                    {
                        List<XAttribute> attributes = element.Attributes().ToList();
                        element.RemoveAttributes();
                        element.Add(new XAttribute("Id", name)); // TODO: make this a safe id.
                        element.Add(attributes);
                    }
                }
            }
        }

        private void InspectChainPackageElement(XElement element)
        {
            XAttribute suppressSignatureValidation = element.Attribute("SuppressSignatureValidation");

            if (null != suppressSignatureValidation)
            {
                if (this.OnError(InspectorTestType.SuppressSignatureValidationDeprecated, element, "The chain package element contains deprecated '{0}' attribute. Use the 'EnableSignatureValidation' instead.", suppressSignatureValidation))
                {
                    if ("no" == suppressSignatureValidation.Value)
                    {
                        element.Add(new XAttribute("EnableSignatureValidation", "yes"));
                    }
                }

                suppressSignatureValidation.Remove();
            }
        }

        private void InspectPayloadElement(XElement element)
        {
            XAttribute suppressSignatureValidation = element.Attribute("SuppressSignatureValidation");

            if (null != suppressSignatureValidation)
            {
                if (this.OnError(InspectorTestType.SuppressSignatureValidationDeprecated, element, "The payload element contains deprecated '{0}' attribute. Use the 'EnableSignatureValidation' instead.", suppressSignatureValidation))
                {
                    if ("no" == suppressSignatureValidation.Value)
                    {
                        element.Add(new XAttribute("EnableSignatureValidation", "yes"));
                    }
                }

                suppressSignatureValidation.Remove();
            }
        }

        /// <summary>
        /// Inspects a Wix element.
        /// </summary>
        /// <param name="element">The Wix element to inspect.</param>
        /// <returns>The inspected element.</returns>
        private void InspectWixElementWithoutNamespace(XElement element)
        {
            if (this.OnError(InspectorTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixNamespace.NamespaceName))
            {
                element.Name = WixNamespace.GetName(element.Name.LocalName);

                element.Add(new XAttribute("xmlns", WixNamespace.NamespaceName)); // set the default namespace.

                foreach (XElement elementWithoutNamespace in element.Elements().Where(e => XNamespace.None == e.Name.Namespace))
                {
                    elementWithoutNamespace.Name = WixNamespace.GetName(elementWithoutNamespace.Name.LocalName);
                }
            }
        }

        /// <summary>
        /// Inspect the whitespace adjacent to a node.
        /// </summary>
        /// <param name="node">The node to inspect.</param>
        /// <param name="level">The depth level of the node.</param>
        private void InspectWhitespace(XNode node, int level)
        {
            // Fix the whitespace before this node.
            XText whitespace = node.PreviousNode as XText;

            if (null != whitespace)
            {
                if (XmlNodeType.CDATA == node.NodeType)
                {
                    if (this.OnError(InspectorTestType.WhitespacePrecedingCDATAWrong, node, "There should be no whitespace preceding a CDATA node."))
                    {
                        whitespace.Remove();
                    }
                }
                else
                {
                    if (!IsLegalWhitespace(this.IndentationAmount, level, whitespace.Value))
                    {
                        if (this.OnError(InspectorTestType.WhitespacePrecedingNodeWrong, node, "The whitespace preceding this node is incorrect."))
                        {
                            FixWhitespace(this.IndentationAmount, level, whitespace);
                        }
                    }
                }
            }

            // Fix the whitespace after CDATA nodes.
            XCData cdata = node as XCData;

            if (null != cdata)
            {
                whitespace = cdata.NextNode as XText;

                if (null != whitespace)
                {
                    if (this.OnError(InspectorTestType.WhitespaceFollowingCDATAWrong, node, "There should be no whitespace following a CDATA node."))
                    {
                        whitespace.Remove();
                    }
                }
            }
            else
            {
                // Fix the whitespace inside and after this node (except for Error which may contain just whitespace).
                XElement element = node as XElement;

                if (null != element && "Error" != element.Name.LocalName)
                {
                    if (!element.HasElements && !element.IsEmpty && String.IsNullOrEmpty(element.Value.Trim()))
                    {
                        if (this.OnError(InspectorTestType.NotEmptyElement, element, "This should be an empty element since it contains nothing but whitespace."))
                        {
                            element.RemoveNodes();
                        }
                    }

                    whitespace = node.NextNode as XText;

                    if (null != whitespace)
                    {
                        if (!IsLegalWhitespace(this.IndentationAmount, level - 1, whitespace.Value))
                        {
                            if (this.OnError(InspectorTestType.WhitespacePrecedingEndElementWrong, whitespace, "The whitespace preceding this end element is incorrect."))
                            {
                                FixWhitespace(this.IndentationAmount, level - 1, whitespace);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<InspectorTestType> YieldInspectorTypes(IEnumerable<string> types)
        {
            if (null != types)
            {
                foreach (string type in types)
                {
                    InspectorTestType itt;

                    if (Enum.TryParse<InspectorTestType>(type, true, out itt))
                    {
                        yield return itt;
                    }
                    else // not a known InspectorTestType
                    {
                        this.OnError(InspectorTestType.InspectorTestTypeUnknown, (XObject)null, "Unknown error type: '{0}'.", type);
                    }
                }
            }
        }

        private static void UpdateElementsWithDeprecatedNamespaces(IEnumerable<XElement> elements, Dictionary<XNamespace, XNamespace> deprecatedToUpdatedNamespaces)
        {
            foreach (XElement element in elements)
            {
                XNamespace ns;

                if (deprecatedToUpdatedNamespaces.TryGetValue(element.Name.Namespace, out ns))
                {
                    element.Name = ns.GetName(element.Name.LocalName);
                }

                // Remove all the attributes and add them back to with their namespace updated (as necessary).
                List<XAttribute> attributes = element.Attributes().ToList();
                element.RemoveAttributes();

                foreach (XAttribute attribute in attributes)
                {
                    XAttribute convertedAttribute = attribute;

                    if (attribute.IsNamespaceDeclaration)
                    {
                        if (deprecatedToUpdatedNamespaces.TryGetValue(attribute.Value, out ns))
                        {
                            convertedAttribute = ("xmlns" == attribute.Name.LocalName) ? new XAttribute(attribute.Name.LocalName, ns.NamespaceName) : new XAttribute(XNamespace.Xmlns + attribute.Name.LocalName, ns.NamespaceName);
                        }
                    }
                    else if (deprecatedToUpdatedNamespaces.TryGetValue(attribute.Name.Namespace, out ns))
                    {
                        convertedAttribute = new XAttribute(ns.GetName(attribute.Name.LocalName), attribute.Value);
                    }

                    element.Add(convertedAttribute);
                }
            }
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
            while (whitespace.StartsWith(XDocumentNewLine, StringComparison.Ordinal))
            {
                whitespace = whitespace.Substring(XDocumentNewLine.Length);
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
        /// Fix the whitespace in a Whitespace node.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level of the desired whitespace.</param>
        /// <param name="whitespace">The whitespace node to fix.</param>
        private static void FixWhitespace(int indentationAmount, int level, XText whitespace)
        {
            int newLineCount = 0;

            for (int i = 0; i + 1 < whitespace.Value.Length; ++i)
            {
                if (XDocumentNewLine == whitespace.Value.Substring(i, 2))
                {
                    ++i; // skip an extra character
                    ++newLineCount;
                }
            }

            if (0 == newLineCount)
            {
                newLineCount = 1;
            }

            // reset the whitespace value
            whitespace.Value = String.Empty;

            // add the correct number of newlines
            for (int i = 0; i < newLineCount; ++i)
            {
                whitespace.Value = String.Concat(whitespace.Value, XDocumentNewLine);
            }

            // add the correct number of spaces based on configured indentation amount
            whitespace.Value = String.Concat(whitespace.Value, new string(' ', level * indentationAmount));
        }

        /// <summary>
        /// Output an error message to the console.
        /// </summary>
        /// <param name="inspectorTestType">The type of inspector test.</param>
        /// <param name="node">The node that caused the error.</param>
        /// <param name="message">Detailed error message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        /// <returns>Returns true indicating that action should be taken on this error, and false if it should be ignored.</returns>
        private bool OnError(InspectorTestType inspectorTestType, XObject node, string message, params object[] args)
        {
            if (this.IgnoreErrors.Contains(inspectorTestType)) // ignore the error
            {
                return false;
            }

            // increase the error count
            this.Errors++;

            // set the warning/error part of the message
            string warningError;
            if (this.ErrorsAsWarnings.Contains(inspectorTestType)) // error as warning
            {
                warningError = "warning";
            }
            else // normal error
            {
                warningError = "error";
            }

            if (null != node)
            {
                Console.Error.WriteLine("{0}({1}) : {2} WXCP{3:0000} : {4} ({5})", this.SourceFile, ((IXmlLineInfo)node).LineNumber, warningError, (int)inspectorTestType, String.Format(CultureInfo.CurrentCulture, message, args), inspectorTestType.ToString());
            }
            else
            {
                string source = this.SourceFile ?? "wixcop.exe";

                Console.Error.WriteLine("{0} : {1} WXCP{2:0000} : {3} ({4})", source, warningError, (int)inspectorTestType, String.Format(CultureInfo.CurrentCulture, message, args), inspectorTestType.ToString());
            }

            return true;
        }

        /// <summary>
        /// Output a message to the console.
        /// </summary>
        /// <param name="node">The node that caused the message.</param>
        /// <param name="message">Detailed message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        private void OnVerbose(XNode node, string message, params string[] args)
        {
            this.Errors++;

            if (null != node)
            {
                Console.WriteLine("{0}({1}) : {2}", this.SourceFile, ((IXmlLineInfo)node).LineNumber, String.Format(CultureInfo.CurrentCulture, message, args));
            }
            else
            {
                string source = this.SourceFile ?? "wixcop.exe";

                Console.WriteLine("{0} : {1}", source, String.Format(CultureInfo.CurrentCulture, message, args));
            }
        }
    }
}
