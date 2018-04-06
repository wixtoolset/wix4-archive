// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Simplified.Lexicon;

    public class ObjectInstanceType
    {
        private PropertyInfo collectionProperty;
        private Type[] addChildTypes;
        private MethodInfo addChildMethod;
        Dictionary<string, PropertyInstanceType> properties;

        public ObjectInstanceType(Type type)
        {
            this.Type = type;

            // Get the properties that can be "publicly set" and note the default property if we have one.
            DefaultPropertyNameAttribute[] defaultPropertyAttribute = (DefaultPropertyNameAttribute[])this.Type.GetCustomAttributes(typeof(DefaultPropertyNameAttribute), true);
            string defaultProperty =  (defaultPropertyAttribute.Length == 1) ? defaultPropertyAttribute[0].Property : null;

            this.properties = new Dictionary<string, PropertyInstanceType>(StringComparer.Ordinal);
            foreach (var property in from p in this.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy) where p.GetSetMethod() != null orderby p.Name select new PropertyInstanceType(p, p.Name.Equals(defaultProperty, StringComparison.Ordinal)))
            {
                properties.Add(property.Name, property);
            }

            // Try to find collection for children.
            DefaultCollectionPropertyAttribute[] defaultCollectionAttributes = (DefaultCollectionPropertyAttribute[])this.Type.GetCustomAttributes(typeof(DefaultCollectionPropertyAttribute), true);
            if (defaultCollectionAttributes.Length == 1)
            {
                collectionProperty = this.Type.GetProperty(defaultCollectionAttributes[0].Collection, BindingFlags.Instance | BindingFlags.Public);
                if (collectionProperty.PropertyType.IsGenericType)
                {
                    this.addChildTypes = collectionProperty.PropertyType.GetGenericArguments();
                }
                else
                {
                    this.addChildTypes = new Type[] { collectionProperty.PropertyType };
                }

                this.addChildMethod = collectionProperty.PropertyType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy, null, this.addChildTypes, null);
            }
        }

        public Type Type { get; private set; }

        public IEnumerable<PropertyInstanceType> Properties { get { return this.properties.Values; } }

        public Type[] GetChildrenTypes()
        {
            // TODO: get allowed "ChildrenTypes" from all types that inherit from "addChildTypes".
            return this.addChildTypes;
        }

        public bool TryGetProperty(string name, out PropertyInstanceType property)
        {
            if (String.IsNullOrEmpty(name))
            {
                property = this.properties.Values.Where(p => p.Default).SingleOrDefault();
                return (property != null);
            }
            else
            {
                name = Char.ToUpperInvariant(name[0]) + name.Substring(1);
                return this.properties.TryGetValue(name, out property);
            }
        }

        internal object CreateInstance(FileLineNumber line)
        {
            object obj = Activator.CreateInstance(this.Type);
            if (line != null)
            {
                PropertyInfo lineNumberProperty = this.Type.GetProperty("LineNumber", BindingFlags.Instance | BindingFlags.NonPublic, null, typeof(FileLineNumber), new Type[0], null);
                if (lineNumberProperty != null)
                {
                    lineNumberProperty.SetValue(obj, line, null);
                }
            }

            return obj;
        }

        internal void AddChild(object parent, object child)
        {
            if (this.addChildMethod == null)
            {
                throw new InvalidOperationException();
            }

            object collection = this.collectionProperty.GetValue(parent, null);
            this.addChildMethod.Invoke(collection, new object[] { child });
        }
    }
}
