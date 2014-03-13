//-------------------------------------------------------------------------------------------------
// <copyright file="ITargetFile.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon
{
    using System;

    /// <summary>
    /// Indicates an item can target a file object.
    /// </summary>
    public interface ITargetFile : IPackageItem
    {
        /// <summary>
        /// Gets the File targeted.
        /// </summary>
        /// <returns>File targeted.</returns>
        File GetTargetedFile();
    }
}
