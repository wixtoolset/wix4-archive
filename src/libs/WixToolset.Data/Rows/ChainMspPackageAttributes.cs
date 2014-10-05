//-------------------------------------------------------------------------------------------------
// <copyright file="ChainMspPackageAttributes.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data.Rows
{
    using System;

    [Flags]
    public enum ChainMspPackageAttributes
    {
        DisplayInternalUI = 0x1,
        Slipstream = 0x2,
        TargetUnspecified = 0x4,
    }
}
