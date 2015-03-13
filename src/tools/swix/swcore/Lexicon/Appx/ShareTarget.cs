//-------------------------------------------------------------------------------------------------
// <copyright file="ShareTarget.cs" company="Outercurve Foundation">
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
