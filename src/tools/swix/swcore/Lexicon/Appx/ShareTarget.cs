// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Registers the application as a "share to" target.
    /// </summary>
    [DefaultCollectionProperty("SupportedDataFormats")]
    public class ShareTarget : ApplicationExtensionItem
    {
        public ShareTarget()
        {
            this.SupportedDataFormats = new List<SupportedDataFormat>();
        }

        public ICollection<SupportedDataFormat> SupportedDataFormats { get; private set; }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (this.SupportedDataFormats.Count == 0)
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredElement("ShareTarget", "DataFormat"), this));
            }
            else
            {
                foreach (SupportedDataFormat df in this.SupportedDataFormats)
                {
                    df.Verify(context, this);
                }
            }
        }
    }
}
