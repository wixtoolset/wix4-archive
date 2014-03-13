//-------------------------------------------------------------------------------------------------
// <copyright file="OutputType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    /// <summary>
    /// Various types of output.
    /// </summary>
    public enum OutputType
    {
        /// <summary>Unknown output type.</summary>
        Unknown,

        /// <summary>Bundle output type.</summary>
        Bundle,

        /// <summary>Module output type.</summary>
        Module,

        /// <summary>Patch output type.</summary>
        Patch,

        /// <summary>Patch Creation output type.</summary>
        PatchCreation,

        /// <summary>Product output type.</summary>
        Product,

        /// <summary>Transform output type.</summary>
        Transform
    }
}
