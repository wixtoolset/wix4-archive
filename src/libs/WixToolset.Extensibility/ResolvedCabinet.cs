//-------------------------------------------------------------------------------------------------
// <copyright file="ResolvedCabinet.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Data returned from build file manager ResolveCabinet callback.
    /// </summary>
    public class ResolvedCabinet
    {
        /// <summary>
        /// Gets or sets the build option for the resolved cabinet.
        /// </summary>
        public CabinetBuildOption BuildOption { get; set; }

        /// <summary>
        /// Gets or sets the path for the resolved cabinet.
        /// </summary>
        public string Path { get; set; }
    }
}
