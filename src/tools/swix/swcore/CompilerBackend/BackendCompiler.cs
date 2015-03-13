﻿//-------------------------------------------------------------------------------------------------
// <copyright file="BackendCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.CompilerBackend
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Simplified.CompilerBackend.Appx;
    using WixToolset.Simplified.CompilerBackend.Nuget;
    using WixToolset.Simplified.CompilerBackend.Vsix;
    using WixToolset.Simplified.CompilerBackend.Wix;

    internal enum CompilerOutputType
    {
        AppxPackage,
        NugetPackage,
        MsiModule,
        MsiPackage,
        VsixPackage,
        WixLibrary,
    }

    /// <summary>
    /// Backend compiler generates the final output.
    /// </summary>
    internal abstract class BackendCompiler : IDisposable
    {
        private bool disposed;

        public bool EncounteredError { get; private set; }
        protected List<string> TempPaths { get; private set; }

        /// <summary>
        /// Cannot create instance of this object. Use the <seealso cref="Create"/> method instead.
        /// </summary>
        /// <param name="outputType">Type of output to generate.</param>
        protected BackendCompiler(CompilerOutputType outputType)
        {
            this.TempPaths = new List<string>();

            this.OutputType = outputType;
        }

        /// <summary>
        /// Event for messages fired from the backend compiler.
        /// </summary>
        public event EventHandler<CompilerMessageEventArgs> Messages;

        /// <summary>
        /// Gets the generated package architecture.
        /// </summary>
        public PackageArchitecture Architecture { get; internal set; }

        /// <summary>
        /// Gets or sets the file manager to resolve .
        /// </summary>
        public CompilerFileManager FileManager { get; internal set; }

        /// <summary>
        /// Gets the generated package languages.
        /// </summary>
        public CultureInfo[] Languages { get; internal set; }

        /// <summary>
        /// Gets the output type of the final output.
        /// </summary>
        public CompilerOutputType OutputType { get; internal set; }

        /// <summary>
        /// Creates the appropriate backend compiler based on the output hint path.
        /// </summary>
        /// <param name="type">Package type to generate.</param>
        /// <param name="outputPathHint">Hint for the output file name, used to determine which backend compiler to create.</param>
        /// <returns>Appropriate backend compiler for the output file name.</returns>
        public static BackendCompiler Create(PackageType type, string outputPathHint)
        {
            BackendCompiler backend = null;
            switch (type)
            {
                case PackageType.Appx:
                    backend = new AppxBackendCompiler(CompilerOutputType.AppxPackage);
                    break;

                case PackageType.Nuget:
                    backend = new NugetBackendCompiler();
                    break;

                case PackageType.Msi:
                    //backend = new MsiBackendCompiler(CompilerOutputType.MsiPackage);
                    break;

                case PackageType.Vsix:
                    backend = new VsixBackendCompiler();
                    break;

                case PackageType.Wixlib:
                    backend = new WixBackendCompiler(CompilerOutputType.WixLibrary);
                    break;
            }

            return backend;
        }

        /// <summary>
        /// Generates the final output from a set of intermediates.
        /// </summary>
        /// <param name="intermediates">Intermediates that provide data to be generated.</param>
        /// <param name="outputPath">Path for output file to be generated.</param>
        public abstract void Generate(IEnumerable<Intermediate> intermediates, string outputPath);

        /// <summary>
        /// Cleans up after the backend compiler.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // Take yourself off the Finalization queue to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sends a message via the <seealso cref="Messages"/> event.
        /// </summary>
        /// <param name="e">Event arguments for compiler message.</param>
        public void OnMessage(CompilerMessageEventArgs e)
        {
            if (e.Message.Type == CompilerMessage.CompilerMessageType.LexerError ||
                e.Message.Type == CompilerMessage.CompilerMessageType.Error)
            {
                this.EncounteredError = true;
            }

            if (this.Messages != null)
            {
                this.Messages(this, e);
            }
        }

        /// <summary>
        /// Cleans up after the backend compiler.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed and unmanaged resources.
                if (disposing)
                {
                    foreach (string path in this.TempPaths)
                    {
                        bool retry = false;
                        do
                        {
                            try
                            {
                                File.Delete(path);
                            }
                            catch
                            {
                                // If we already retried, give up on this file.
                                if (retry)
                                {
                                    // TODO: log a warning or verbose message that we left behind a temp file?
                                    break;
                                }

                                try
                                {
                                    // Ensure the file is not read-only, then try again.
                                    FileAttributes attributes = File.GetAttributes(path);
                                    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);

                                    retry = true;
                                }
                                catch
                                {
                                }
                            }
                        } while (retry);
                    }

                    this.TempPaths.Clear();
                }
            }

            this.disposed = true;
        }
    }
}
