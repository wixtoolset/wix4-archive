﻿//-------------------------------------------------------------------------------------------------
// <copyright file="FileTransfer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Common binder core of the WiX toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System;
    using System.IO;
    using WixToolset.Data;

    /// <summary>
    /// Structure used for all file transfer information.
    /// </summary>
    internal class FileTransfer
    {
        /// <summary>Source path to file.</summary>
        public string Source { get; set; }

        /// <summary>Destination path for file.</summary>
        public string Destination { get; set; }

        /// <summary>Flag if file should be moved (optimal).</summary>
        public bool Move { get; set; }

        /// <summary>Optional source line numbers where this file transfer orginated.</summary>
        public SourceLineNumber SourceLineNumbers { get; set; }

        /// <summary>Optional type of file this transfer is moving or copying.</summary>
        public string Type { get; set; }

        /// <summary>Indicates whether the file transer was a built by this build or copied from other some build.</summary>
        internal bool Built { get; set; }

        /// <summary>Set during layout of media when the file transfer when the source and target resolve to the same path.</summary>
        internal bool Redundant { get; set; }

        /// <summary>
        /// Prefer the TryCreate() method to create FileTransfer objects.
        /// </summary>
        /// <param name="source">Source path to file.</param>
        /// <param name="destination">Destination path for file.</param>
        /// <param name="move">File if file should be moved (optimal).</param>
        /// <param name="type">Optional type of file this transfer is transferring.</param>
        /// <param name="sourceLineNumbers">Optional source line numbers wher this transfer originated.</param>
        public FileTransfer(string source, string destination, bool move, string type = null, SourceLineNumber sourceLineNumbers = null)
        {
            this.Source = source;
            this.Destination = destination;
            this.Move = move;

            this.Type = type;
            this.SourceLineNumbers = sourceLineNumbers;
        }

        /// <summary>
        /// Creates a file transfer if the source and destination are different.
        /// </summary>
        /// <param name="source">Source path to file.</param>
        /// <param name="destination">Destination path for file.</param>
        /// <param name="move">File if file should be moved (optimal).</param>
        /// <param name="type">Optional type of file this transfer is transferring.</param>
        /// <param name="sourceLineNumbers">Optional source line numbers wher this transfer originated.</param>
        /// <returns>true if the source and destination are the different, false if no file transfer is created.</returns>
        public static bool TryCreate(string source, string destination, bool move, string type, SourceLineNumber sourceLineNumbers, out FileTransfer transfer)
        {
            string sourceFullPath = null;
            string fileLayoutFullPath = null;

            try
            {
                sourceFullPath = Path.GetFullPath(source);
            }
            catch (System.ArgumentException)
            {
                throw new WixException(WixErrors.InvalidFileName(sourceLineNumbers, source));
            }
            catch (System.IO.PathTooLongException)
            {
                throw new WixException(WixErrors.PathTooLong(sourceLineNumbers, source));
            }

            try
            {
                fileLayoutFullPath = Path.GetFullPath(destination);
            }
            catch (System.ArgumentException)
            {
                throw new WixException(WixErrors.InvalidFileName(sourceLineNumbers, destination));
            }
            catch (System.IO.PathTooLongException)
            {
                throw new WixException(WixErrors.PathTooLong(sourceLineNumbers, destination));
            }

            // if the current source path (where we know that the file already exists) and the resolved
            // path as dictated by the Directory table are not the same, then propagate the file.  The
            // image that we create may have already been done by some other process other than the linker, so 
            // there is no reason to copy the files to the resolved source if they are already there.
            if (String.Equals(sourceFullPath, fileLayoutFullPath, StringComparison.OrdinalIgnoreCase))
            {
                transfer = null;
                return false;
            }

            transfer = new FileTransfer(source, destination, move, type, sourceLineNumbers);
            return true;
        }
    }
}
