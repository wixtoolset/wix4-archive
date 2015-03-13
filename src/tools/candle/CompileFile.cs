//-------------------------------------------------------------------------------------------------
// <copyright file="CompileFile.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Tools
{
    /// <summary>
    /// Source code file to be compiled.
    /// </summary>
    public class CompileFile
    {
        /// <summary>
        /// Path to the source code file.
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Path to compile the output to.
        /// </summary>
        public string OutputPath { get; set; }
    }
}
