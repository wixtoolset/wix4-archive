//-------------------------------------------------------------------------------------------------
// <copyright file="BindDatabaseCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WixToolset.Data;

    /// <summary>
    /// Structure used to hold a row and field that contain binder variables, which need to be resolved
    /// later, once the files have been resolved.
    /// </summary>
    internal class DelayedField
    {
        /// <summary>
        /// Basic constructor for struct
        /// </summary>
        /// <param name="row">Row for the field.</param>
        /// <param name="field">Field needing further resolution.</param>
        public DelayedField(Row row, Field field)
        {
            this.Row = row;
            this.Field = field;
        }

        /// <summary>
        /// The row containing the field.
        /// </summary>
        public Row Row { get; private set; }

        /// <summary>
        /// The field needing further resolving.
        /// </summary>
        public Field Field { get; private set; }
    }
}
