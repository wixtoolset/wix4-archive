// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.ComponentModel;
    using System.Reflection;
    using System.Windows.Markup;

    public class TypeConverterContext : ITypeDescriptorContext, IProvideValueTarget
    {
        /// <summary>
        /// Creates a type converter context to set a property.
        /// </summary>
        /// <param name="item">Target object to set.</param>
        /// <param name="memberInfo">Property to set on target object.</param>
        public TypeConverterContext(object item, MemberInfo memberInfo)
        {
            this.TargetObject = item;
            this.TargetProperty = memberInfo;
        }

        public IContainer Container
        {
            get { return null; }
        }

        public object Instance
        {
            get { return this.TargetObject; }
        }

        public PropertyDescriptor PropertyDescriptor
        {
            get { throw new NotImplementedException(); }
        }

        public void OnComponentChanged()
        {
            throw new NotImplementedException();
        }

        public bool OnComponentChanging()
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            return serviceType.IsInstanceOfType(this) ? this : null;
        }

        public object TargetObject { get; private set; }

        public object TargetProperty { get; private set; }
    }
}
