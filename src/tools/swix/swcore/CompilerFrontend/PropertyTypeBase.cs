//-------------------------------------------------------------------------------------------------
// <copyright file="PropertyTypeBase.cs" company="Outercurve Foundation">
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
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Base type for properties and attached properties.
    /// </summary>
    public abstract class PropertyTypeBase
    {
        private TypeConverter converter;

        public string Name { get; protected set; }

        public Type ValueType { get; protected set; }

        protected bool TryCoerceValue(object parent, MemberInfo memberInfo, object value, out object result)
        {
            result = value; // default result to value.

            Type valueType = value.GetType();
            if (!this.ValueType.IsAssignableFrom(valueType))
            {
                if (this.converter != null && this.converter.CanConvertFrom(valueType))
                {
                    TypeConverterContext context = new TypeConverterContext(parent, memberInfo);
                    result = this.converter.ConvertFrom(context, null, value);
                }
                else if (this.ValueType.IsEnum)
                {
                    try
                    {
                        result = Enum.Parse(this.ValueType, (string)value);
                    }
                    catch (ArgumentNullException)
                    {
                        return false;
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                    catch (OverflowException)
                    {
                        return false;
                    }
                }
                else
                {
                    // Try to find a constructor that takes our value type.
                    ConstructorInfo constructor = this.ValueType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[] { valueType }, null);
                    if (constructor != null)
                    {
                        result = constructor.Invoke(new object[] { value });
                    }
                    else // try the default conversion.
                    {
                        try
                        {
                            result = Convert.ChangeType(value, this.ValueType);
                        }
                        catch (FormatException)
                        {
                            return false;
                        }
                        catch (InvalidCastException)
                        {
                            return false;
                        }
                        catch (OverflowException)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        protected bool TrySetTypeConverter(ICustomAttributeProvider provider)
        {
            Debug.Assert(this.converter == null);

            TypeConverterAttribute[] typeConverters = (TypeConverterAttribute[])provider.GetCustomAttributes(typeof(TypeConverterAttribute), false);
            if (typeConverters.Length > 0)
            {
                Type converterType = Type.GetType(typeConverters[0].ConverterTypeName);
                this.converter = (TypeConverter)Activator.CreateInstance(converterType);
            }

            return this.converter != null;
        }
    }
}
