// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System.Collections.Generic;
    using System.Windows.Markup;

    /// <summary>
    /// In process server.
    /// </summary>
    [DefaultCollectionProperty("Classes")]
    public class InprocServer : PackageItemTargetsFile
    {
        public InprocServer()
        {
            this.Classes = new List<Class>();
        }

        /// <summary>
        /// Gets or sets an inline class.
        /// </summary>
        //public string Class { get; set; }

        /// <summary>
        /// Gets the classes for the inproc server.
        /// </summary>
        public List<Class> Classes { get; private set; }

        /// <summary>
        /// Gets or sets the threading model for the inproc server.
        /// </summary>
        //public ThreadingModelType ThreadingModel { get; set; }
    }
}
