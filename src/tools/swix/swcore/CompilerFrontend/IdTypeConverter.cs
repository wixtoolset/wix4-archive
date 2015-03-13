//-------------------------------------------------------------------------------------------------
// <copyright file="IdTypeConverter.cs" company="Outercurve Foundation">
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
    using System.Globalization;
    using System.Reflection;
    using System.Windows.Markup;
    using WixToolset.Simplified.Lexicon;

    internal class IdTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(String) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string lookup = (string)value;
            object instance = null;

            IProvideValueTarget targetProvider = (IProvideValueTarget)context.GetService(typeof(IProvideValueTarget));
            PackageItem targetItem = (PackageItem)targetProvider.TargetObject;

            string delayedLookupKey = null;
            DelayedItemLookup delayedLookup = null;

            PropertyInfo targetProperty = targetProvider.TargetProperty as PropertyInfo;
            if (targetProperty != null)
            {
                delayedLookupKey = targetProperty.Name;
                delayedLookup = new DelayedItemLookup(lookup, targetItem, targetProperty);
            }
            else // attached property
            {
                MethodInfo method = (MethodInfo)targetProvider.TargetProperty;
                ParameterInfo[] parameters =  method.GetParameters(); // target type always comes from the second parameter.

                delayedLookupKey = String.Concat(method.DeclaringType.Namespace, ".", method.DeclaringType.Name, ".", method.Name, "()");
                delayedLookup = new DelayedItemLookup(lookup, targetItem, parameters[1].ParameterType);
            }

            targetItem.DelayedLookup.Add(delayedLookupKey, delayedLookup);

            return instance;
        }
    }
}
