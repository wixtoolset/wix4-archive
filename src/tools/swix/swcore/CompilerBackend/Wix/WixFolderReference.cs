//-------------------------------------------------------------------------------------------------
// <copyright file="WixFolderReference.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using WixToolset.Simplified.Lexicon;

    internal class WixFolderReference : WixItem
    {
        public WixFolderReference(WixBackendCompiler backend, Folder folderRef) :
            base(backend, folderRef)
        {
        }

        /// <summary>
        /// Folder references never create Components
        /// </summary>
        /// <returns>String.Empty so this only gets called once but doesn't not create an actual Component Id.</returns>
        protected override string CalculateComponentMsiId()
        {
            return String.Empty;
        }

        protected override string CalculateMsiId()
        {
            Folder folderRef = (Folder)this.Item;
            return WixBackendCompilerServices.GenerateMsiId(this.Backend, this.Item, null);
        }
    }
}
