// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
