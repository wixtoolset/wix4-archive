//-------------------------------------------------------------------------------------------------
// <copyright file="ComponentKeyPath.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using System;

    public enum ComponentKeyPathType
    {
        /// <summary>
        /// Not a key path.
        /// </summary>
        None,

        /// <summary>
        /// File resource as a key path.
        /// </summary>
        File,

        /// <summary>
        /// Folder as a key path.
        /// </summary>
        Directory,

        /// <summary>
        /// ODBC data source as a key path.
        /// </summary>
        OdbcDataSource,

        /// <summary>
        /// A simple registry key acting as a key path.
        /// </summary>
        Registry,

        /// <summary>
        /// A registry key that contains a formatted property acting as a key path.
        /// </summary>
        RegistryFormatted
    }

    public class ComponentKeyPath
    {
        /// <summary>
        /// Identifier of the resource to be a key path.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Indicates whether the key path was explicitly set for this resource.
        /// </summary>
        public bool Explicit { get; set; }

        /// <summary>
        /// Type of resource to be the key path.
        /// </summary>
        public ComponentKeyPathType Type { get; set; }
    }
}
