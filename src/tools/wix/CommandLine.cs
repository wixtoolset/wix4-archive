//-------------------------------------------------------------------------------------------------
// <copyright file="CommandLine.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Common utilities for Wix command-line processing.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// Common utilities for Wix command-line processing.
    /// </summary>
    public static class CommandLine
    {
        /// <summary>
        /// Get a set of files that possibly have a search pattern in the path (such as '*').
        /// </summary>
        /// <param name="searchPath">Search path to find files in.</param>
        /// <param name="fileType">Type of file; typically "Source".</param>
        /// <returns>An array of files matching the search path.</returns>
        /// <remarks>
        /// This method is written in this verbose way because it needs to support ".." in the path.
        /// It needs the directory path isolated from the file name in order to use Directory.GetFiles
        /// or DirectoryInfo.GetFiles.  The only way to get this directory path is manually since
        /// Path.GetDirectoryName does not support ".." in the path.
        /// </remarks>
        /// <exception cref="WixFileNotFoundException">Throws WixFileNotFoundException if no file matching the pattern can be found.</exception>
        public static string[] GetFiles(string searchPath, string fileType)
        {
            if (null == searchPath)
            {
                throw new ArgumentNullException("searchPath");
            }

            // convert alternate directory separators to the standard one
            string filePath = searchPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            int lastSeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            string[] files = null;

            try
            {
                if (0 > lastSeparator)
                {
                    files = Directory.GetFiles(".", filePath);
                }
                else // found directory separator
                {
                    files = Directory.GetFiles(filePath.Substring(0, lastSeparator + 1), filePath.Substring(lastSeparator + 1));
                }
            }
            catch (DirectoryNotFoundException)
            {
                // don't let this function throw the DirectoryNotFoundException. (this exception
                // occurs for non-existant directories and invalid characters in the searchPattern)
            }
            catch (ArgumentException)
            {
                // don't let this function throw the ArgumentException. (this exception
                // occurs in certain situations such as when passing a malformed UNC path)
            }
            catch (IOException)
            {
                throw new WixFileNotFoundException(searchPath, fileType);
            }

            // file could not be found or path is invalid in some way
            if (null == files || 0 == files.Length)
            {
                throw new WixFileNotFoundException(searchPath, fileType);
            }

            return files;
        }

        /// <summary>
        /// Validates that a valid string parameter (without "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be validated.</param>
        /// <returns>True if a valid string parameter exists there, false if not.</returns>
        public static bool IsValidArg(string[] args, int index)
        {
            if (args.Length <= index || String.IsNullOrEmpty(args[index]) || '/' == args[index][0] || '-' == args[index][0])
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Validates that a commandline parameter is a valid file or directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string VerifyPath(string path)
        {
            return VerifyPath(path, false);
        }

        /// <summary>
        /// Validates that a commandline parameter is a valid file or directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <param name="allowPrefix">Indicates if a colon-delimited prefix is allowed.</param>
        /// <returns>The full path if it is valid, null if it is invalid.</returns>
        public static string VerifyPath(string path, bool allowPrefix)
        {
            string fullPath;

            if (0 <= path.IndexOf('\"'))
            {
                Messaging.Instance.OnMessage(WixErrors.PathCannotContainQuote(path));
                return null;
            }

            try
            {
                string prefix = null;
                if (allowPrefix)
                {
                    int prefixLength = path.IndexOf('=') + 1;
                    if (0 != prefixLength)
                    {
                      prefix = path.Substring(0, prefixLength);
                      path = path.Substring(prefixLength);
                    }
                }

                if (String.IsNullOrEmpty(prefix))
                {
                    fullPath = Path.GetFullPath(path);
                }
                else
                {
                    fullPath = String.Concat(prefix, Path.GetFullPath(path));
                }
            }
            catch (Exception e)
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidCommandLineFileName(path, e.Message));
                return null;
            }

            return fullPath;
        }

        /// <summary>
        /// Validates that a string is a valid bind path, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <returns>The bind path if it is valid, null if it is invalid.</returns>
        public static BindPath GetBindPath(string commandlineSwitch, string[] args, int index)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                Messaging.Instance.OnMessage(WixErrors.DirectoryPathRequired(commandlineSwitch));
                return null;
            }

            BindPath bindPath = BindPath.Parse(args[index]);

            if (File.Exists(bindPath.Path))
            {
                Messaging.Instance.OnMessage(WixErrors.ExpectedDirectoryGotFile(commandlineSwitch, bindPath.Path));
                return null;
            }

            bindPath.Path = VerifyPath(bindPath.Path, true);
            return String.IsNullOrEmpty(bindPath.Path) ? null : bindPath;
        }

        /// <summary>
        /// Validates that a commandline parameter is a valid file or directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetFileOrDirectory(string commandlineSwitch, string[] args, int index)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                Messaging.Instance.OnMessage(WixErrors.FileOrDirectoryPathRequired(commandlineSwitch));
                return null;
            }

            return VerifyPath(args[index]);
        }

        /// <summary>
        /// Validates that a string is a valid directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <param name="allowPrefix">Indicates if a colon-delimited prefix is allowed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetDirectory(string commandlineSwitch, string[] args, int index, bool allowPrefix = false)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                Messaging.Instance.OnMessage(WixErrors.DirectoryPathRequired(commandlineSwitch));
                return null;
            }

            if (File.Exists(args[index]))
            {
                Messaging.Instance.OnMessage(WixErrors.ExpectedDirectoryGotFile(commandlineSwitch, args[index]));
                return null;
            }

            return VerifyPath(args[index], allowPrefix);
        }

        /// <summary>
        /// Validates that a string is a valid filename, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetFile(string commandlineSwitch, string[] args, int index)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                Messaging.Instance.OnMessage(WixErrors.FilePathRequired(commandlineSwitch));
                return null;
            }

            if (Directory.Exists(args[index]))
            {
                Messaging.Instance.OnMessage(WixErrors.ExpectedFileGotDirectory(commandlineSwitch, args[index]));
                return null;
            }

            return VerifyPath(args[index]);
        }
    }
}
