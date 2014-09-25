//-------------------------------------------------------------------------------------------------
// <copyright file="BundleChainAttributes.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;

    /// <summary>
    /// Attributes available for a bundle chain.
    /// </summary>
    [Flags]
    internal enum BundleChainAttributes : int
    {
        None = 0x0,
        DisableRollback = 0x1,
        DisableSystemRestore = 0x2,
        ParallelCache = 0x4,
    }
}
