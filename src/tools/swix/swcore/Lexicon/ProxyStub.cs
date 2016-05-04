// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System.Collections.Generic;
    using System.Windows.Markup;

    /// <summary>
    /// Creates a proxy stub.
    /// </summary>
    [DefaultCollectionProperty("Interfaces")]
    public class ProxyStub : PackageItemTargetsFile
    {
        public ProxyStub()
        {
            this.Interfaces = new List<Interface>();
        }

        /// <summary>
        /// Guid of the proxy stub.
        /// </summary>
        public string Guid { get; set; }

        /// <summary>
        /// List of interfaces supported by the proxy stub.
        /// </summary>
        public List<Interface> Interfaces { get; private set; }
    }
}
