//-------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixToolset.Simplified.TypedMessageGenerator
{
    internal static class Extensions
    {
        internal static void AppendFormatLine(this StringBuilder builder, string format, params object[] args)
        {
            builder.AppendFormat(format, args);
            builder.AppendLine();
        }

        // Is using Select() a valid way to do this?  We don't really want to generate the output/projected list!
        internal static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (T t in list)
            {
                action(t);
            }
        }
    }
}
