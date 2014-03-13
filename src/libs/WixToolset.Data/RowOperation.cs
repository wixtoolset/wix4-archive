//-------------------------------------------------------------------------------------------------
// <copyright file="RowOperation.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    /// <summary>
    /// The row transform operations.
    /// </summary>
    public enum RowOperation
    {
        /// <summary>
        /// No operation.
        /// </summary>
        None,

        /// <summary>
        /// Added row.
        /// </summary>
        Add,

        /// <summary>
        /// Deleted row.
        /// </summary>
        Delete,

        /// <summary>
        /// Modified row.
        /// </summary>
        Modify
    }
}
