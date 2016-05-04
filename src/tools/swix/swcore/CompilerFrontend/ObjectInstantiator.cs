// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using WixToolset.Simplified.CompilerFrontend.Parser;

    internal class ObjectInstantiator
    {
        public ObjectInstantiator(CompilerMessageDelegate messages)
        {
            this.OnMessage = messages;
            this.DefaultNamespaces = new Dictionary<string, string>();
            this.TypeCache = new ObjectInstanceTypeCache();
        }

        public IDictionary<string, string> DefaultNamespaces { get; private set; }

        public ObjectInstanceTypeCache TypeCache { get; private set; }

        private CompilerMessageDelegate OnMessage;

        public List<object> Instantiate(string path, StatementNode node)
        {
            Dictionary<string, string> namespaces = new Dictionary<string, string>(this.DefaultNamespaces);
            List<object> items = new List<object>();

            this.Instantiate(items, path, namespaces, node);
            return items;
        }

        private object Instantiate(List<object> items, string path, Dictionary<string, string> namespaces, StatementNode node)
        {
            object item = null;

            if (node.Statement.StatementType == StatementType.Use)
            {
                if (node.Statement.Tokens[1].TokenType != ParserTokenType.NamespacePrefixDeclaration)
                {
                    this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.ExpectedToken("namespace prefix declaration", node.Statement.Tokens[1].Value), path, node.Statement.Range));
                    return null;
                }

                string prefix = node.Statement.Tokens[1].Value;
                string typeNamespace;
                if (!this.TypeCache.TryGetNamespaceByPrefix(prefix, out typeNamespace))
                {
                    this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.UnknownNamespacePrefix(prefix), path, node.Statement.Range));
                    return null;
                }

                namespaces.Add(prefix, typeNamespace);
            }
            else if (node.Statement.StatementType == StatementType.Object || node.Statement.StatementType == StatementType.ObjectStart)
            {
                item = this.InstantiateObject(items, path, namespaces, node);
            }
            else // ignorable stuff.
            {
                Debug.Assert(node.Children.Count == 0);
            }

            return item;
        }

        private object InstantiateObject(List<object> items, string path, Dictionary<string, string> namespaces, StatementNode node)
        {
            // First, search ahead for any namespace declarations on the object.
            namespaces = this.GetObjectNamespaces(path, namespaces, node);

            // Determine object type prefix (optional), namespace and name.
            int token = 0;
            string prefix = node.Statement.Tokens[token].TokenType == ParserTokenType.NamespacePrefix ? node.Statement.Tokens[token++].Value : String.Empty;
            string typeName = node.Statement.Tokens[token++].Value;

            string typeNamespace;
            if (!namespaces.TryGetValue(prefix, out typeNamespace))
            {
                this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.UnknownNamespacePrefix(prefix), path, node.Statement.Range));
                return null;
            }

            // Create the object as an item.
            ObjectInstanceType objectType;
            if (!this.TypeCache.TryGetObjectInstanceType(typeNamespace, typeName, out objectType))
            {
                this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.UnknownItem(typeNamespace, typeName), path, node.Statement.Range));
                return null;
            }

            // Offset the range line number for the actual source file line number.
            object item = objectType.CreateInstance(new FileLineNumber(path, node.Statement.Tokens[0].Range.Start.Line + 1, node.Statement.Tokens[0].Range.Start.Column));

            // Process the properties.
            this.InstantiateProperties(path, namespaces, node, token, objectType, item);

            // Process any children.
            foreach (StatementNode childNode in node.Children)
            {
                object childItem = this.Instantiate(items, path, namespaces, childNode);
                if (childItem != null)
                {
                    // TODO: error if childItem does not implementation of type from objectType.GetChildrenTypes()
                    objectType.AddChild(item, childItem);
                }
            }

            items.Add(item);
            return item;
        }

        private void InstantiateProperties(string path, Dictionary<string, string> namespaces, StatementNode node, int token, ObjectInstanceType objectType, object item)
        {
            // Process the name=value properties on the item.
            for (int i = token; i < node.Statement.Tokens.Count; ++i)
            {
                // These were already processed by GetObjectNamespaces().
                if (node.Statement.Tokens[i].TokenType == ParserTokenType.NamespacePrefixDeclaration ||
                    node.Statement.Tokens[i].TokenType == ParserTokenType.NamespaceDeclaration)
                {
                    continue;
                }

                string typeNamespace = null;

                // If a namespace is present then we must be working with an attached property.
                if (node.Statement.Tokens[i].TokenType == ParserTokenType.NamespacePrefix)
                {
                    string prefix = node.Statement.Tokens[i].Value;
                    ++i;

                    typeNamespace = namespaces[prefix];
                }

                if (node.Statement.Tokens[i].TokenType == ParserTokenType.AttachedPropertyObject)
                {
                    string attachedPropertyName = node.Statement.Tokens[i].Value;
                    ++i;

                    if (node.Statement.Tokens[i].TokenType != ParserTokenType.PropertyName)
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.ExpectedToken("property name", node.Statement.Tokens[i].Value), path, node.Statement.Range));
                        break;
                    }

                    string name = node.Statement.Tokens[i].Value;
                    ++i;

                    if (node.Statement.Tokens[i].TokenType != ParserTokenType.Equals)
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.ExpectedToken("equals", node.Statement.Tokens[i].Value), path, node.Statement.Range));
                        break;
                    }
                    ++i;

                    if (node.Statement.Tokens[i].TokenType != ParserTokenType.PropertyValue)
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.ExpectedToken("property value", node.Statement.Tokens[i].Value), path, node.Statement.Range));
                        break;
                    }

                    string value = node.Statement.Tokens[i].Value;

                    if (String.IsNullOrEmpty(typeNamespace))
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.AttachedPropertyRequiresNamespace(attachedPropertyName), path, node.Statement.Range));
                        break;
                    }

                    AttachedPropertyObjectType apt;
                    if (!this.TypeCache.TryGetAttachedPropertyType(typeNamespace, attachedPropertyName, out apt))
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.UnknownAttachedProperty(typeNamespace, attachedPropertyName), path, node.Statement.Range));
                    }
                    else // set the property value.
                    {
                        AttachedPropertySetterType setter;
                        if (!apt.TryGetPropertySetter(name, out setter))
                        {
                            this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.UnknownAttachedPropertyValue(typeNamespace, attachedPropertyName, name), path, node.Statement.Range));
                        }
                        else if (!setter.TrySetValue(item, value))
                        {
                            this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.CannotAssignAttachedPropertyVale(typeNamespace, attachedPropertyName, name, value), path, node.Statement.Range));
                        }
                    }
                }
                else // must be a simple property assignment.
                {
                    // Get the property name (if there isn't one, we must want the "default" property).
                    string name = null;
                    if (node.Statement.Tokens[i].TokenType == ParserTokenType.PropertyName)
                    {
                        name = node.Statement.Tokens[i].Value;
                        ++i;

                        if (node.Statement.Tokens[i].TokenType != ParserTokenType.Equals)
                        {
                            this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.ExpectedToken("=", node.Statement.Tokens[i].Value), path, node.Statement.Range));
                            break;
                        }

                        ++i;
                    }

                    // Set the property value.
                    if (node.Statement.Tokens[i].TokenType != ParserTokenType.PropertyValue)
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.ExpectedToken("property value", node.Statement.Tokens[i].Value), path, node.Statement.Range));
                    }

                    string value = node.Statement.Tokens[i].Value;

                    if (!String.IsNullOrEmpty(typeNamespace))
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.PropertyCannotSpecifyNamespace(name, typeNamespace), path, node.Statement.Range));
                        break;
                    }

                    PropertyInstanceType propertyType;
                    if (!objectType.TryGetProperty(name, out propertyType))
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.UnknownProperty(name), path, node.Statement.Range));
                    }
                    else if (!propertyType.TrySetValue(item, value))
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.CannotAssignPropertyValue(name, value), path, node.Statement.Range));
                    }
                }
            }
        }

        private Dictionary<string, string> GetObjectNamespaces(string path, Dictionary<string, string> namespaces, StatementNode node)
        {
            Dictionary<string, string> newNamespaces = null;
            for (int i = 0; i < node.Statement.Tokens.Count; ++i)
            {
                string declaredPrefix = String.Empty;
                if (node.Statement.Tokens[i].TokenType == ParserTokenType.NamespacePrefixDeclaration)
                {
                    declaredPrefix = node.Statement.Tokens[i].Value;
                    ++i;

                    if (node.Statement.Tokens[i].TokenType != ParserTokenType.NamespaceDeclaration)
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.ExpectedToken("namespace declaration", node.Statement.Tokens[i].Value), path, node.Statement.Range));
                        break;
                    }
                }

                if (node.Statement.Tokens[i].TokenType == ParserTokenType.NamespaceDeclaration)
                {
                    if (newNamespaces == null)
                    {
                        newNamespaces = new Dictionary<string, string>(namespaces);
                    }

                    newNamespaces[declaredPrefix] = node.Statement.Tokens[i].Value;
                }
            }

            return (newNamespaces == null) ? namespaces : newNamespaces;
        }
    }
}
