//-------------------------------------------------------------------------------------------------
// <copyright file="DeviceCapability.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;

    /// <summary>
    /// Specifies access to a device is required.
    /// </summary>
    public class DeviceCapability : PackageItem
    {
        public string Name { get; set; }
    }
}
