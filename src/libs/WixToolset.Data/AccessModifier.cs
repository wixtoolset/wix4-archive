//-------------------------------------------------------------------------------------------------
// <copyright file="AccessModifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    public enum AccessModifier
    {
        /// <summary>
        /// Indicates the identifier is publicly visible to all other sections.
        /// </summary>
        Public,

        /// <summary>
        /// Indicates the identifier is visible only to sections in the same library.
        /// </summary>
        Internal,

        /// <summary>
        /// Indicates the identifier is visible only to sections in the same source file.
        /// </summary>
        Protected,

        /// <summary>
        /// Indicates the identifiers is visible only to the section where it is defined.
        /// </summary>
        Private,
    }
}
