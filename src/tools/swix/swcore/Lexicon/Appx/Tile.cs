// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    public enum TileForeground
    {
        Invalid,

        dark,
        light,
    }

    public enum TileShowName
    {
        Invalid,

        allLogos,
        noLogos,
        logoOnly,
        wideLogoOnly
    }

    /// <summary>
    /// Defines the required application tile.
    /// </summary>
    public class Tile : PackageItem
    {
        public QualifiedFile Image { get; set; }

        public QualifiedFile SmallImage { get; set; }

        public TileForeground Foreground { get; set; }

        public string Background { get; set; }

        public string ShortName { get; set; }

        public TileShowName ShowName { get; set; }

        public QualifiedFile WideImage { get; set; }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (this.Foreground == TileForeground.Invalid)
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Tile", "Foreground"), this));
            }

            if (String.IsNullOrEmpty(this.Background))
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Tile", "Background"), this));
            }

            // TODO: verify the colors are valid
        }

        protected override void OnResolveEnd(FrontendCompiler context)
        {
            if (this.Image != null)
            {
                this.Image.ResolveFiles(context, this);
            }

            if (this.SmallImage != null)
            {
                this.SmallImage.ResolveFiles(context, this);
            }

            if (this.WideImage != null)
            {
                this.WideImage.ResolveFiles(context, this);
            }
        }
    }
}
