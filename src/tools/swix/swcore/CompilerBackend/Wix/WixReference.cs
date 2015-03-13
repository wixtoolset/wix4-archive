//-------------------------------------------------------------------------------------------------
// <copyright file="WixReference.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
