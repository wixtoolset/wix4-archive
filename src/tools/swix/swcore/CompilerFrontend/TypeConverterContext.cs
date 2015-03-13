//-------------------------------------------------------------------------------------------------
// <copyright file="TypeConverterContext.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
