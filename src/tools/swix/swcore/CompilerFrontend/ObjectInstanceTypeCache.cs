// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Markup;

    public class ObjectInstanceTypeCache
    {
        private Dictionary<string, string> prefixNamespace = new Dictionary<string, string>();
        private Dictionary<string, Assembly> namespaceAssemblyCache = new Dictionary<string, Assembly>();
        private Dictionary<string, ObjectInstanceType> objectInstanceTypeCache = new Dictionary<string, ObjectInstanceType>();
        private Dictionary<string, AttachedPropertyObjectType> attachedPropertyTypeCache = new Dictionary<string, AttachedPropertyObjectType>();

        public static string CalculateTypeName(string typeNamespace, string typeName)
        {
            typeName = Char.ToUpperInvariant(typeName[0]) + typeName.Substring(1);
            return String.Concat(typeNamespace, ".", typeName);
        }

        public void AddAssembly(Assembly assembly)
        {
            // Index the prefixes by XML namespaces.
            Dictionary<string, string> namespacePrefix = new Dictionary<string, string>();
            foreach (XmlnsPrefixAttribute prefix in assembly.GetCustomAttributes(typeof(XmlnsPrefixAttribute), false))
            {
                namespacePrefix.Add(prefix.XmlNamespace, prefix.Prefix);
            }

            // Map the CLR namespaces to the assembly and maybe prefixes to the CLR namespaces.
            foreach (XmlnsDefinitionAttribute xmlns in assembly.GetCustomAttributes(typeof(XmlnsDefinitionAttribute), false))
            {
                this.namespaceAssemblyCache.Add(xmlns.ClrNamespace, assembly);

                string prefix;
                if (namespacePrefix.TryGetValue(xmlns.XmlNamespace, out prefix))
                {
                    this.prefixNamespace.Add(prefix, xmlns.ClrNamespace);
                }
            }
        }

        public bool TryGetObjectInstanceType(string typeNamespace, string typeName, out ObjectInstanceType objectInstanceType)
        {
            string assemblyTypeName = ObjectInstanceTypeCache.CalculateTypeName(typeNamespace, typeName);

            if (!this.objectInstanceTypeCache.TryGetValue(assemblyTypeName, out objectInstanceType))
            {
                Assembly assembly;
                if (!this.namespaceAssemblyCache.TryGetValue(typeNamespace, out assembly))
                {
                    return false;
                }

                Type type = assembly.GetType(assemblyTypeName, false);
                if (type == null)
                {
                    return false;
                }

                objectInstanceType = new ObjectInstanceType(type);
                this.objectInstanceTypeCache.Add(assemblyTypeName, objectInstanceType);
            }

            return true;
        }

        public bool TryGetAttachedPropertyType(string typeNamespace, string typeName, out AttachedPropertyObjectType attachedPropertyObjectType)
        {
            string assemblyTypeName = ObjectInstanceTypeCache.CalculateTypeName(typeNamespace, typeName);

            if (!this.attachedPropertyTypeCache.TryGetValue(assemblyTypeName, out attachedPropertyObjectType))
            {
                Assembly assembly;
                if (!this.namespaceAssemblyCache.TryGetValue(typeNamespace, out assembly))
                {
                    return false;
                }

                Type type = assembly.GetType(assemblyTypeName, false);
                if (type == null)
                {
                    return false;
                }

                if (!AttachedPropertyObjectType.TryCreate(type, out attachedPropertyObjectType))
                {
                    return false;
                }

                this.attachedPropertyTypeCache.Add(assemblyTypeName, attachedPropertyObjectType);
            }

            return true;
        }

        public bool TryGetNamespaceByPrefix(string prefix, out string typeNamespace)
        {
            return this.prefixNamespace.TryGetValue(prefix, out typeNamespace);
        }

        public void PreloadCache()
        {
            // Enumerate/reflect all of the available types and pre-cache them.
            // REVIEW: This pattern will re-enumerate the same assembly each time for
            // each CLR namespace... it could be more efficient.
            foreach (var kv in this.namespaceAssemblyCache)
            {
                var clrNamespace = kv.Key;
                var assembly = kv.Value;

                foreach (var type in assembly.GetTypes().Where(t => !t.IsInterface && !t.IsNested && String.Equals(t.Namespace, clrNamespace, StringComparison.Ordinal)))
                {
                    if (!type.IsAbstract)
                    {
                        // REVIEW: any other attributes to check?
                        ObjectInstanceType ignored;
                        this.TryGetObjectInstanceType(type.Namespace, type.Name, out ignored);
                    }

                    // Look for attached properties and pre-load those as well...
                    if (AttachedPropertyObjectType.GetSetterMethods(type).Any())
                    {
                        AttachedPropertyObjectType ignored;
                        this.TryGetAttachedPropertyType(type.Namespace, type.Name, out ignored);
                    }
                }
            }
        }

        // REVIEW: The prefixes will eventually be defined by each source document.
        // For now, though, we just expose them here.
        public IEnumerable<string> GetPrefixes()
        {
            return this.prefixNamespace.Keys;
        }

        public string GetPrefixForClrNamespace(string clrNamespace)
        {
            var entry = this.prefixNamespace.SingleOrDefault(kv => string.Equals(kv.Value, clrNamespace));
            return entry.Key;
        }

        public IEnumerable<ObjectInstanceType> GetTypes()
        {
            return this.objectInstanceTypeCache.Values;
        }

        public IEnumerable<AttachedPropertyObjectType> GetAttachedProperties()
        {
            return this.attachedPropertyTypeCache.Values;
        }
    }
}
