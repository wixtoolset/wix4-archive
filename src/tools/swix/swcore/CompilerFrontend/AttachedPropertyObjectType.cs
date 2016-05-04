// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class AttachedPropertyObjectType
    {
        private Dictionary<string, AttachedPropertySetterType> setters;

        private AttachedPropertyObjectType(Type type, IEnumerable<AttachedPropertySetterType> setters)
        {
            this.Type = type;

            this.setters = new Dictionary<string, AttachedPropertySetterType>();
            foreach (var setter in setters)
            {
                this.setters.Add(setter.Name, setter);
            }
        }

        public Type Type { get; private set; }

        public IEnumerable<AttachedPropertySetterType> Setters { get { return this.setters.Values; } }

        public static bool TryCreate(Type type, out AttachedPropertyObjectType attachedPropertyObjectType)
        {
            var methods = AttachedPropertyObjectType.GetSetterMethods(type);
            if (!methods.Any())
            {
                attachedPropertyObjectType = null;
                return false;
            }

            var setters = methods.Select(m => new AttachedPropertySetterType(m));
            attachedPropertyObjectType = new AttachedPropertyObjectType(type, setters);

            return true;
        }

        public bool TryGetPropertySetter(string name, out AttachedPropertySetterType setter)
        {
            name = Char.ToUpperInvariant(name[0]) + name.Substring(1);
            return this.setters.TryGetValue(name, out setter);
        }

        public static IEnumerable<MethodInfo> GetSetterMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                       .Where(m => m.Name.StartsWith("Set", StringComparison.Ordinal))
                       .OrderBy(m => m.Name);
        }
    }
}
