// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Markup;
    using WixToolset.Simplified.CompilerFrontend;

    [DefaultCollectionProperty("SupportedFileExtensions")]
    public class FileSavePicker : ApplicationExtensionItem
    {
        public FileSavePicker()
        {
            this.SupportedFileExtensions = new List<FileExtension>();
        }

        public List<FileExtension> SupportedFileExtensions { get; private set; }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (this.SupportedFileExtensions.Count == 0)
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredElement("FileSavePicker", "FileExtension"), this));
            }
            else
            {
                this.SupportedFileExtensions.ForEach(df => df.Verify(context, this));
            }
        }
    }
}
