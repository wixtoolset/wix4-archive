// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
