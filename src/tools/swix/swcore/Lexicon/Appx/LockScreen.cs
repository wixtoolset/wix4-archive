// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    public enum LockScreenNotification
    {
        image,
        imageAndTileText,
    }

    public class LockScreen : PackageItem
    {
        public LockScreenNotification Notification { get; set; }

        public QualifiedFile Image { get; set; }

        protected override void OnResolveEnd(FrontendCompiler context)
        {
            if (this.Image != null)
            {
                this.Image.ResolveFiles(context, this);
            }
        }
    }
}
