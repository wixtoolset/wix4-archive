//-------------------------------------------------------------------------------------------------
// <copyright file="PatchAttributeType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    using System;

    /// <summary>
    /// PatchAttribute values
    /// </summary>
    [Flags]
    public enum PatchAttributeType
    {
        None = 0,

        /// <summary>Prevents the updating of the file that is in fact changed in the upgraded image relative to the target images.</summary>
        Ignore = 1,

        /// <summary>Set if the entire file should be installed rather than creating a binary patch.</summary>
        IncludeWholeFile = 2,

        /// <summary>Set to indicate that the patch is non-vital.</summary>
        AllowIgnoreOnError = 4,

        /// <summary>Allowed bits.</summary>
        Defined = Ignore | IncludeWholeFile | AllowIgnoreOnError
    }
}
