//-------------------------------------------------------------------------------------------------
// <copyright file="EmptyRule.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    public enum EmptyRule
    {
        /// <summary>
        /// The trimmed value cannot be empty.
        /// </summary>
        MustHaveNonWhitespaceCharacters,

        /// <summary>
        /// The trimmed value can be empty, but the value itself cannot be empty.
        /// </summary>
        CanBeWhitespaceOnly,

        /// <summary>
        /// The value can be empty.
        /// </summary>
        CanBeEmpty
    }
}
