//-------------------------------------------------------------------------------------------------
// <copyright file="ProxyStub.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
