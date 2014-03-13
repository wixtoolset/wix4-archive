//-------------------------------------------------------------------------------------------------
// <copyright file="BindPath.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using System;

    /// <summary>
    /// Bind path representation.
    /// </summary>
    public class BindPath
    {
        /// <summary>
        /// Creates an unnamed bind path.
        /// </summary>
        /// <param name="path">Path for the bind path.</param>
        public BindPath(string path) : this(String.Empty, path)
        {
        }

        /// <summary>
        /// Creates a named bind path.
        /// </summary>
        /// <param name="name">Name of the bind path.</param>
        /// <param name="path">Path for the bind path.</param>
        public BindPath(string name, string path)
        {
            this.Name = name;
            this.Path = path;
        }

        /// <summary>
        /// Parses a bind path from its string representation
        /// </summary>
        /// <param name="bindPath">String representation of bind path that looks like: [name=]path</param>
        /// <returns>Parsed bind path.</returns>
        public static BindPath Parse(string bindPath)
        {
            string[] namedPath = bindPath.Split(new char[] { '=' }, 2);
            return (1 == namedPath.Length) ? new BindPath(namedPath[0]) : new BindPath(namedPath[0], namedPath[1]);
        }

        /// <summary>
        /// Name of the bind path or String.Empty if the path is unnamed.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path for the bind path.
        /// </summary>
        public string Path { get; set; }
    }
}
