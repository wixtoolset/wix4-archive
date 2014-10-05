//-------------------------------------------------------------------------------------------------
// <copyright file="YesNoDefaultType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    /// <summary>
    /// Yes, No, Default xml simple type.
    /// </summary>
    public enum YesNoDefaultType
    {
        /// <summary>Not a valid yes, no or default value.</summary>
        IllegalValue = -2,

        /// <summary>Value not set; equivalent to null for reference types.</summary>
        NotSet = -1,

        /// <summary>The no value.</summary>
        No,

        /// <summary>The yes value.</summary>
        Yes,

        /// <summary>The default value.</summary>
        Default,
    }
}
