//-------------------------------------------------------------------------------------------------
// <copyright file="WixProperty.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
