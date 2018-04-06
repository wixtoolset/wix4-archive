// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using WixToolset.Simplified.Lexicon.Msi;

    internal class WixProperty : WixItem
    {
        public WixProperty(WixBackendCompiler backend, Property property) :
            base(backend, property)
        {
        }

        protected override string CalculateMsiId()
        {
            return this.Item.Id;
        }
    }
}
