//-------------------------------------------------------------------------------------------------
// <copyright file="PackageArchitecture.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified
{
    /// <summary>
    /// Architecture of the resulting package.
    /// </summary>
    public enum PackageArchitecture
    {
        Unknown,
        Neutral,
        Arm,
        X64,
        X86,
    }
}
