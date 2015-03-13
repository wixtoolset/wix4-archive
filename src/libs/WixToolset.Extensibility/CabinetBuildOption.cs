//-------------------------------------------------------------------------------------------------
// <copyright file="CabinetBuildOption.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Options for building the cabinet.
    /// </summary>
    public enum CabinetBuildOption
    {
        /// <summary>
        /// Build the cabinet and move it to the target location.
        /// </summary>
        BuildAndMove,

        /// <summary>
        /// Build the cabinet and copy it to the target location.
        /// </summary>
        BuildAndCopy,

        /// <summary>
        /// Just copy the cabinet to the target location.
        /// </summary>
        Copy
    }
}
