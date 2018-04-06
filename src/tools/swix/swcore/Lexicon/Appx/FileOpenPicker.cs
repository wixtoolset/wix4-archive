// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Simplified.CompilerFrontend;

    [DefaultCollectionProperty("SupportedFileExtensions")]
    public class FileOpenPicker : ApplicationExtensionItem
    {
        public FileOpenPicker()
        {
            this.SupportedFileExtensions = new List<FileExtension>();
        }

        public ICollection<FileExtension> SupportedFileExtensions { get; private set; }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (this.SupportedFileExtensions.Count == 0)
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredElement("FileOpenPicker", "FileExtension"), this));
            }
            else
            {
                foreach (FileExtension df in this.SupportedFileExtensions)
                {
                    df.Verify(context, this);
                }
            }
        }
    }
}
