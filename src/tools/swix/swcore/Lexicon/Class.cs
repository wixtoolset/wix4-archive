//-------------------------------------------------------------------------------------------------
// <copyright file="Class.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
