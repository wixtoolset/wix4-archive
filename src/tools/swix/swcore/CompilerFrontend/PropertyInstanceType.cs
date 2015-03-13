//-------------------------------------------------------------------------------------------------
// <copyright file="PropertyInstanceType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Reflection;

    public class PropertyInstanceType : PropertyTypeBase
    {
        public PropertyInstanceType(PropertyInfo info, bool defaultProperty)
        {
            this.PropertyInfo = info;

            this.Default = defaultProperty;
            this.Name = info.Name;
            this.ValueType = info.PropertyType;

            // Try to get a type converter off the property. If it isn't found there try to get the
            // converter from the value type.
            if (!this.TrySetTypeConverter(info))
            {
                this.TrySetTypeConverter(info.PropertyType);
            }
        }

        public PropertyInfo PropertyInfo { get; private set; }

        public bool Default { get; private set; }

        internal bool TrySetValue(object parent, object value)
        {
            object result;
            if (!this.TryCoerceValue(parent, this.PropertyInfo, value, out result))
            {
                return false;
            }

            this.PropertyInfo.SetValue(parent, result, null);
            return true;
        }
    }
}
