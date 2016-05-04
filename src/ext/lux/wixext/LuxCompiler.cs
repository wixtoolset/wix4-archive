// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// Lux operators.
    /// </summary>
    public enum Operator
    {
        /// <summary>No value has been set (defaults to Equal).</summary>
        NotSet,

        /// <summary>Case-sensitive equality.</summary>
        Equal,

        /// <summary>Case-sensitive inequality.</summary>
        NotEqual,

        /// <summary>Case-insensitive equality.</summary>
        CaseInsensitiveEqual,

        /// <summary>Case-insensitive inequality.</summary>
        CaseInsensitiveNotEqual,
    }

    /// <summary>
    /// The compiler for the WiX Toolset Lux Extension.
    /// </summary>
    public sealed class LuxCompiler : CompilerExtension
    {
        /// <summary>
        /// Initializes a new instance of the LuxCompiler class.
        /// </summary>
        public LuxCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/lux";
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
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "UnitTestRef":
                            this.ParseUnitTestRefElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                    switch (element.Name.LocalName)
                    {
                        case "Mutation":
                            this.ParseMutationElement(element);
                            break;
                        case "UnitTest":
                            this.ParseUnitTestElement(element, null);
                            break;
                        case "UnitTestRef":
                            this.ParseUnitTestRefElement(element);
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
        /// Parses a Mutation element to create Lux unit test mutationss.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseMutationElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string mutation = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            mutation = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(mutation))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                        {
                            case "UnitTest":
                                this.ParseUnitTestElement(child, mutation);
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
        }

        /// <summary>
        /// Parses a UnitTest element to create Lux unit tests.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="args">Used while parsing multi-value property tests to pass values from the parent element.</param>
        private void ParseUnitTestElement(XElement node, string mutation, params string[] args)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            bool multiValue = 0 < args.Length;
            Identifier id = null;
            string action = multiValue ? args[0] : null;
            string property = multiValue ? args[1] : null;
            string op = null;
            Operator oper = Operator.NotSet;
            string value = null;
            string expression = null;
            string valueSep = multiValue ? args[2] : null;
            string nameValueSep = multiValue ? args[3] : null;
            string condition = null;
            string index = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "CustomAction":
                        case "Property":
                        case "Expression":
                        case "ValueSeparator":
                        case "NameValueSeparator":
                            if (multiValue)
                            {
                                this.Core.OnMessage(LuxErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            break;
                    }

                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "CustomAction":
                            action = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Property":
                            property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Operator":
                            op = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < op.Length)
                            {
                                switch (op)
                                {
                                    case "equal":
                                        oper = Operator.Equal;
                                        break;
                                    case "notEqual":
                                        oper = Operator.NotEqual;
                                        break;
                                    case "caseInsensitiveEqual":
                                        oper = Operator.CaseInsensitiveEqual;
                                        break;
                                    case "caseInsensitiveNotEqual":
                                        oper = Operator.CaseInsensitiveNotEqual;
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, op, "equal", "notEqual", "caseInsensitiveEqual", "caseInsensitiveNotEqual"));
                                        break;
                                }
                            }
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ValueSeparator":
                            valueSep = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "NameValueSeparator":
                            nameValueSep = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Index":
                            if (!multiValue)
                            {
                                this.Core.OnMessage(LuxErrors.IllegalAttributeWhenNotNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            index = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            bool isParent = false;
            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                        {
                            case "Condition":
                                // the condition should not be empty
                                condition = this.Core.GetConditionInnerText(child);
                                if (null == condition || 0 == condition.Length)
                                {
                                    condition = null;
                                    this.Core.OnMessage(WixErrors.ConditionExpected(sourceLineNumbers, child.Name.LocalName));
                                }
                                break;
                            case "Expression":
                                // the expression should not be empty
                                expression = this.Core.GetConditionInnerText(child);
                                if (null == expression || 0 == expression.Length)
                                {
                                    expression = null;
                                    this.Core.OnMessage(WixErrors.ConditionExpected(sourceLineNumbers, child.Name.LocalName));
                                }
                                break;
                            case "UnitTest":
                                if (multiValue)
                                {
                                    SourceLineNumber childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
                                    this.Core.OnMessage(LuxErrors.ElementTooDeep(childSourceLineNumbers, child.Name.LocalName, node.Name.LocalName));
                                }

                                this.ParseUnitTestElement(child, mutation, action, property, valueSep, nameValueSep);
                                isParent = true;
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

            if (isParent)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    this.Core.OnMessage(LuxErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, "Value"));
                }
            }
            else
            {
                // the children generate multi-value unit test rows; the parent doesn't generate anything

                if (!String.IsNullOrEmpty(property) && String.IsNullOrEmpty(value))
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.LocalName, "Property", "Value"));
                }

                if (!String.IsNullOrEmpty(property) && !String.IsNullOrEmpty(expression))
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "Expression"));
                }

                if (multiValue && String.IsNullOrEmpty(valueSep) && String.IsNullOrEmpty(nameValueSep))
                {
                    this.Core.OnMessage(LuxErrors.MissingRequiredParentAttribute(sourceLineNumbers, node.Name.LocalName, "ValueSeparator", "NameValueSeparator"));
                }

                if (!String.IsNullOrEmpty(valueSep) && !String.IsNullOrEmpty(nameValueSep))
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "ValueSeparator", "NameValueSeparator"));
                }

                if (!this.Core.EncounteredError)
                {
                    if (null == id)
                    {
                        id = this.Core.CreateIdentifier("lux", action, property, index, condition, mutation);
                    }

                    if (Operator.NotSet == oper)
                    {
                        oper = Operator.Equal;
                    }

                    Row row = this.Core.CreateRow(sourceLineNumbers, "WixUnitTest", id);
                    row[1] = action;
                    row[2] = property;
                    row[3] = (int)oper;
                    row[4] = value;
                    row[5] = expression;
                    row[6] = condition;
                    row[7] = valueSep;
                    row[8] = nameValueSep;
                    row[9] = index;
                    if (!string.IsNullOrEmpty(mutation))
                    {
                        row[10] = mutation;
                    }

                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", action);
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixRunImmediateUnitTests");
                }
            }
        }

        /// <summary>
        /// Parses a UnitTestRef element to reference Lux unit tests.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseUnitTestRefElement(XElement node)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
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

            if (String.IsNullOrEmpty(id))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (!this.Core.EncounteredError)
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "WixUnitTest", id);
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixRunImmediateUnitTests");
            }
        }
    }
}
