//-------------------------------------------------------------------------------------------------
// <copyright file="SupportedDataFormat.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using WixToolset.Simplified.CompilerFrontend;

    public abstract class SupportedDataFormat
    {
        protected string typeName;

        public string Name { get; set; }

        internal void Verify(FrontendCompiler context, PackageItem parentItem)
        {
            if (String.IsNullOrEmpty(this.Name))
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute(this.typeName, "Name"), parentItem));
            }
        }
    }
}
