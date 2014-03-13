//-------------------------------------------------------------------------------------------------
// <copyright file="AttachedPropertySetterType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    public class AttachedPropertySetterType : PropertyTypeBase
    {
        private MethodInfo setter;

        public AttachedPropertySetterType(MethodInfo setter)
        {
            ParameterInfo[] parameters = setter.GetParameters();
            Debug.Assert(parameters.Length == 2);

            this.setter = setter;

            this.Name = setter.Name.Substring(3); // skip the letters "Set" that front all attached properties.
            this.ObjectType = parameters[0].ParameterType;
            this.ValueType = parameters[1].ParameterType;

            // TODO: Try to get a type converter off the return type from the matching "Get" method. If it isn't found there try to get the
            // converter from the value type.
            //if (!this.TrySetTypeConverter(getSetterReturnType))
            //{
                this.TrySetTypeConverter(this.ValueType);
            //}
        }

        public Type ObjectType { get; private set; }

        internal bool TrySetValue(object parent, object value)
        {
            Type objectType = parent.GetType();
            if (!this.ObjectType.IsAssignableFrom(objectType))
            {
                return false;
            }

            object result;
            if (!this.TryCoerceValue(parent, this.setter, value, out result))
            {
                return false;
            }

            this.setter.Invoke(null, new object[] { parent, result });
            return true;
        }
    }
}
