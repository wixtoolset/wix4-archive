// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
