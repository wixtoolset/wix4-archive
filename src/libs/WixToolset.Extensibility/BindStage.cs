//-------------------------------------------------------------------------------------------------
// <copyright file="BindStage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Bind stage of a file.. The reason we need this is to change the ResolveFile behavior based on if
    /// dynamic bindpath plugin is desirable. We cannot change the signature of ResolveFile since it might
    /// break existing implementers which derived from BinderFileManager
    /// </summary>
    public enum BindStage
    {
        /// <summary>
        /// Normal binding
        /// </summary>
        Normal,

        /// <summary>
        /// Bind the file path of the target build file
        /// </summary>
        Target,

        /// <summary>
        /// Bind the file path of the updated build file
        /// </summary>
        Updated,
    }
}
