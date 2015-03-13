//-------------------------------------------------------------------------------------------------
// <copyright file="Property.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Msi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Property : PackageItem, IFileReference
    {
        public bool External { get; set; }

        /// <summary>
        /// Returns this object as a package item.
        /// </summary>
        /// <returns>this</returns>
        public PackageItem GetPackageItem()
        {
            return this;
        }

        protected override void OnResolveBegin(CompilerFrontend.FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (!this.External)
            {
                // TODO: display error message that properties must be external currently.
            }
        }
    }
}
