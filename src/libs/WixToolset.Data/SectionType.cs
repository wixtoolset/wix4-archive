//-------------------------------------------------------------------------------------------------
// <copyright file="SectionType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    /// <summary>
    /// Type of section.
    /// </summary>
    public enum SectionType
    {
        /// <summary>Unknown section type, default and invalid.</summary>
        Unknown,

        /// <summary>Bundle section type.</summary>
        Bundle,

        /// <summary>Fragment section type.</summary>
        Fragment,

        /// <summary>Module section type.</summary>
        Module,

        /// <summary>Product section type.</summary>
        Product,

        /// <summary>Patch creation section type.</summary>
        PatchCreation,

        /// <summary>Patch section type.</summary>
        Patch
    }
}
