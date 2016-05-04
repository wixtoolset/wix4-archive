// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
