//-------------------------------------------------------------------------------------------------
// <copyright file="FileType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// File type for a file extension.
    /// </summary>
    public class FileType : PackageItem
    {
        public string Extension { get; set; }

        public string ContentType { get; set; }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (String.IsNullOrEmpty(this.Extension))
            {
                // TODO: error message that extension is required.
            }
            else if (!this.Extension.StartsWith(".", StringComparison.Ordinal))
            {
                // TODO: warn that we are automatically prefixing a dot on the extension.
                this.Extension = String.Concat(".", this.Extension);
            }
        }
    }
}
