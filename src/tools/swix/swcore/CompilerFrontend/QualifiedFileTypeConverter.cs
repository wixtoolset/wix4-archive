//-------------------------------------------------------------------------------------------------
// <copyright file="QualifiedFileTypeConverter.cs" company="Outercurve Foundation">
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
    using WixToolset.Simplified.Lexicon;

    internal class QualifiedFileTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(String) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return new QualifiedFile() { NonqualifiedName = (string)value };
        }
    }
}
