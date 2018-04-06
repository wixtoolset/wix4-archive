// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
