//-------------------------------------------------------------------------------------------------
// <copyright file="SplashScreen.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    public class SplashScreen : PackageItem
    {
        public QualifiedFile Image { get; set; }

        public string Background { get; set; }

        protected override void OnResolveEnd(FrontendCompiler context)
        {
            if (this.Image != null)
            {
                this.Image.ResolveFiles(context, this);
            }
        }
    }
}
