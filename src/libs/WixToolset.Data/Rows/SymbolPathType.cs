//-------------------------------------------------------------------------------------------------
// <copyright file="SymbolPathType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    /// <summary>
    /// The types that the WixDeltaPatchSymbolPaths table can hold.
    /// </summary>
    /// <remarks>The order of these values is important since WixDeltaPatchSymbolPaths are sorted by this type.</remarks>
    public enum SymbolPathType
    {
        File,
        Component,
        Directory,
        Media,
        Product
    };
}
