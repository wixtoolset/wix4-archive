//-------------------------------------------------------------------------------------------------
// <copyright file="FileSavePicker.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
