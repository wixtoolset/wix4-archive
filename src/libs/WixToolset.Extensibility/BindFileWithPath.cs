﻿//-------------------------------------------------------------------------------------------------
// <copyright file="BindFileWithPath.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Bind file with its path.
    /// </summary>
    public class BindFileWithPath
    {
        /// <summary>
        /// Gets or sets the identifier of the file with this path.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string Path { get; set; }
    }
}
