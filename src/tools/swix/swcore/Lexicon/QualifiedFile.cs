//-------------------------------------------------------------------------------------------------
// <copyright file="QualifiedFile.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Qualified file.
    /// </summary>
    [TypeConverter(typeof(QualifiedFileTypeConverter))]
    public class QualifiedFile
    {
        /// <summary>
        /// Creates a qualified file.
        /// </summary>
        public QualifiedFile()
        {
            this.Files = new List<File>();
        }

        /// <summary>
        /// Non-qualified name for qualified files.
        /// </summary>
        public string NonqualifiedName { get; set; }

        /// <summary>
        /// List of qualified files that match non-qualified name.
        /// </summary>
        public IEnumerable<File> Files { get; private set; }

        internal void ResolveFiles(FrontendCompiler context, PackageItem parentItem)
        {
            // TODO: set this.Files with the files that match this.NonqualifiedName.
            // TOOD: error if this.Files is empty set.
        }
    }
}
