//-------------------------------------------------------------------------------------------------
// <copyright file="Util.cs" company="Outercurve Foundation">
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
    /// Common Wix utility methods and types.
    /// </summary>
    public sealed class Util
    {
        /// <summary>
        /// Set by WixToolTasks to indicate WIX is running inside MSBuild
        /// </summary>
        public static bool RunningInMsBuild { get; set; }
    }
}