// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using WixToolset.Simplified.Lexicon;

    internal class WixReference : WixItem
    {
        public WixReference(WixBackendCompiler backend, PackageItem item) :
            base(backend, item)
        {
        }
    }
}
