// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
