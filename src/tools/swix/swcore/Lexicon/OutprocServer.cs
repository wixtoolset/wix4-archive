//-------------------------------------------------------------------------------------------------
// <copyright file="OutprocServer.cs" company="Outercurve Foundation">
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
    /// Out of proc server instance type.
    /// </summary>
    public enum InstanceType
    {
        Single,
        Multiple,
    }

    /// <summary>
    /// Out of process server.
    /// </summary>
    [DefaultCollectionProperty("Classes")]
    public class OutprocServer : PackageItemTargetsFile
    {
        public OutprocServer()
        {
            this.Classes = new List<Class>();
        }

        /// <summary>
        /// Gets or sets the out of proc server arguments.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Gets the classes for the out of proc server.
        /// </summary>
        public List<Class> Classes { get; private set; }

        /// <summary>
        /// Gets or sets the instancing of the out of proc server.
        /// </summary>
        public InstanceType Instance { get; set; }

        /// <summary>
        /// Gets or sets the name of the out of proc server.
        /// </summary>
        public string Name { get; set; }
    }
}
