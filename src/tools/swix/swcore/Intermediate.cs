//-------------------------------------------------------------------------------------------------
// <copyright file="Intermediate.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified
{
    using System.Collections.Generic;
    using WixToolset.Simplified.Lexicon;

    /// <summary>
    /// Intermediate object that transfers information from frontend to backend of compiler.
    /// </summary>
    public sealed class Intermediate
    {
        /// <summary>
        /// Can only create intermediate object inside the compiler.
        /// </summary>
        /// <param name="items">Items processed by the frontend.</param>
        internal Intermediate(IEnumerable<PackageItem> items)
        {
            this.Items = items;
        }

        /// <summary>
        /// Items process by the frontend for use by the backend.
        /// </summary>
        public IEnumerable<PackageItem> Items { get; private set; }
    }
}
