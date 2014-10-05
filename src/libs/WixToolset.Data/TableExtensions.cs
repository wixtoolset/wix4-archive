//-------------------------------------------------------------------------------------------------
// <copyright file="TableExtensions.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Methods that extend <see cref="Table"/>.
    /// </summary>
    public static class TableExtensions
    {
        /// <summary>
        /// Gets the rows contained in the table as a particular row type.
        /// </summary>
        /// <value>Rows contained in the table as a particular type.</value>
        public static IEnumerable<T> RowsAs<T>(this Table table) where T : Row
        {
            return (null == table) ? Enumerable.Empty<T>() : table.Rows.Cast<T>();
        }
    }
}
