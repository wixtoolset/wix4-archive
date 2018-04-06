// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System.Collections.Generic;

    /// <summary>
    /// Class in an inproc or out of proc server.
    /// </summary>
    [DefaultCollectionProperty("Attributes")]
    public class Class
    {
        public Class()
        {
            this.Attributes = new List<ClassAttribute>();
        }

        /// <summary>
        /// Gets the optional attributes on a class.
        /// </summary>
        public ICollection<ClassAttribute> Attributes { get; private set; }

        /// <summary>
        /// Gets or sets the class id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the implementation for this class.
        /// </summary>
        /// <remarks>Only valid for Windows Installer based packages. AppX ignores this attribute.</remarks>
        public string Implementation { get; set; }

        /// <summary>
        /// Gets or sets the threading model of the class.
        /// </summary>
        public ThreadingModelType ThreadingModel { get; set; }
    }
}
