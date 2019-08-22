// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Base and default implementation for resolving files for the compiler.
    /// </summary>
    public class CompilerFileManager
    {
        /// <summary>
        /// Creates instance of the default compiler file manager.
        /// </summary>
        public CompilerFileManager()
        {
            this.SearchPaths = new List<string>();
        }

        /// <summary>
        /// Architecture of output being generated.
        /// </summary>
        public PackageArchitecture Architecture { get; internal set; }

        /// <summary>
        /// Language of output being generated.
        /// </summary>
        public CultureInfo Language { get; internal set; }

        /// <summary>
        /// Path for output being generated.
        /// </summary>
        public string OutputPath { get; internal set; }

        /// <summary>
        /// Gets the search paths to locate files.
        /// </summary>
        /// <value>The search paths to locate files.</value>
        public IList<string> SearchPaths { get; private set; }

        /// <summary>
        /// Tries to resolve the provided path to a real file on disk.
        /// </summary>
        /// <param name="path">Path to resolve.</param>
        /// <param name="resolvedPath">Resolved path.</param>
        /// <returns>True if path was resolved, false if file cannot be found.</returns>
        public virtual bool TryResolvePath(string path, out string resolvedPath)
        {
            bool found = false;
            int searchPathIndex = 0;

            resolvedPath = Path.GetFullPath(path);
            while (!(found = File.Exists(resolvedPath)) && searchPathIndex < this.SearchPaths.Count)
            {
                resolvedPath = Path.Combine(this.SearchPaths[searchPathIndex], path);
                ++searchPathIndex;
            }

            return found;
        }
    }
}
