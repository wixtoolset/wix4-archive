//-------------------------------------------------------------------------------------------------
// <copyright file="InprocServer.cs" company="Outercurve Foundation">
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
