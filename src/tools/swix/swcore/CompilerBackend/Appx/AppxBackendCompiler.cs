// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Schema;
    using WixToolset.Simplified.CompilerBackend.Appx;

    /// <summary>
    /// Backend compiler that generates .appx files.
    /// </summary>
    internal class AppxBackendCompiler : BackendCompiler
    {
        /// <summary>
        /// Creates a backend compiler for a specific architecture.
        /// </summary>
        internal AppxBackendCompiler(CompilerOutputType outputType) :
            base(CompilerOutputType.AppxPackage)
        {
        }

        /// <summary>
        /// Generates the final output from a set of intermediates.
        /// </summary>
        /// <param name="intermediates">Intermediates that provide data to be generated.</param>
        /// <param name="outputPath">Path for output file to be generated.</param>
        public override void Generate(IEnumerable<Intermediate> intermediates, string outputPath)
        {
            AppxManifest manifest = new AppxManifest(this);

            manifest.ProcessIntermediates(intermediates);
            if (this.EncounteredError)
            {
                return;
            }

            manifest.Validate(this.ValidationErrorHandler);
            if (this.EncounteredError)
            {
                string tempPath = Path.GetTempFileName();
                this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.SavedManifest(tempPath), null, 0, 0));

                this.SaveManifest(manifest, tempPath);
                return;
            }

            FileTransfer appxPackageTransfer = FileTransfer.Create(null, Path.GetTempFileName(), outputPath, "AppxPackage", true);
            using (Stream packageStream = File.Open(appxPackageTransfer.Source, FileMode.Create, FileAccess.ReadWrite, FileShare.Delete))
            {
                AppxPackage package = new AppxPackage(packageStream);

                foreach (PackageFile file in manifest.Files)
                {
                    using (Stream fileStream = File.Open(file.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.CompressFile(file.SourcePath), file.File.LineNumber));
                        package.AddFile(fileStream, file.PartUri, file.MimeType, CompressionLevel.DefaultCompression);
                    }
                }

                using (Stream manifestStream = manifest.GetStream())
                {
                    package.Finish(manifestStream);
                }
            }

            FileTransfer.ExecuteTransfer(this, appxPackageTransfer);
        }

        /// <summary>
        /// Saves the manifest stream to a file.
        /// </summary>
        /// <param name="manifest">Manifest to save.</param>
        /// <param name="path">Path to save manifest to.</param>
        private void SaveManifest(IAppxManifest manifest, string path)
        {
            using (Stream file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Delete))
            {
                using (Stream manifestStream = manifest.GetStream())
                {
                    int read = 0;
                    byte[] buffer = new byte[65536];
                    while (0 < (read = manifestStream.Read(buffer, 0, buffer.Length)))
                    {
                        file.Write(buffer, 0, read);
                    }
                }
            }
        }

        /// <summary>
        /// Callback for XML schema validation errors that are all treated as errors
        /// because the compiler should have prevented them from being possible.
        /// </summary>
        /// <param name="sender">Object sending the validation error.</param>
        /// <param name="e">Event arguments about the validation error.</param>
        private void ValidationErrorHandler(object sender, ValidationEventArgs e)
        {
            this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.UnexpectedValidationError(e.Message), null, 0, 0));
        }
    }
}
