//-------------------------------------------------------------------------------------------------
// <copyright file="ProcessedStreamEventHandler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;
    using System.Xml.Linq;

    /// <summary>
    /// Preprocessed output stream event handler delegate.
    /// </summary>
    /// <param name="sender">Sender of the message.</param>
    /// <param name="ea">Arguments for the preprocessed stream event.</param>
    public delegate void ProcessedStreamEventHandler(object sender, ProcessedStreamEventArgs e);

    /// <summary>
    /// Event args for preprocessed stream event.
    /// </summary>
    public class ProcessedStreamEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new ProcessedStreamEventArgs.
        /// </summary>
        /// <param name="sourceFile">Source file that is preprocessed.</param>
        /// <param name="document">Preprocessed output document.</param>
        public ProcessedStreamEventArgs(string sourceFile, XDocument document)
        {
            this.SourceFile = sourceFile;
            this.Document = document;
        }

        /// <summary>
        /// Gets the full path of the source file.
        /// </summary>
        /// <value>The full path of the source file.</value>
        public string SourceFile { get; private set; }

        /// <summary>
        /// Gets the preprocessed output stream.
        /// </summary>
        /// <value>The the preprocessed output stream.</value>
        public XDocument Document { get; private set; }
    }
}
