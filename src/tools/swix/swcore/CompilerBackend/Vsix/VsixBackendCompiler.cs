﻿//-------------------------------------------------------------------------------------------------
// <copyright file="VsixBackendCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.CompilerBackend.Vsix
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Packaging;
    using System.Xml.Schema;

    /// <summary>
    /// Backend compiler that generates .vsix files.
    /// </summary>
    internal class VsixBackendCompiler : BackendCompiler
    {
        /// <summary>
        /// Creates a backend compiler for a specific architecture.
        /// </summary>
        internal VsixBackendCompiler() :
            base(CompilerOutputType.VsixPackage)
        {
        }

        /// <summary>
        /// Generates the final output from a set of intermediates.
        /// </summary>
        /// <param name="intermediates">Intermediates that provide data to be generated.</param>
        /// <param name="outputPath">Path for output file to be generated.</param>
        public override void Generate(IEnumerable<Intermediate> intermediates, string outputPath)
        {
            VsixManifest manifest = new VsixManifest(this);

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

            FileTransfer packageTransfer = FileTransfer.Create(null, Path.GetTempFileName(), outputPath, "VsixPackage", true);
            using (Package package = Package.Open(packageTransfer.Source, FileMode.Create))
            {
                // Add all the manifest files.
                foreach (PackageFile file in manifest.Files)
                {
                    this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.CompressFile(file.SourcePath), file.File.LineNumber));

                    using (Stream fileStream = File.Open(file.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        PackagePart part = package.CreatePart(new Uri(file.PartUri, UriKind.Relative), file.MimeType, CompressionOption.Normal);
                        this.SaveStreamToPart(fileStream, part);
                    }
                }

                // Save the manifest to the package.
                using (Stream manifestStream = manifest.GetStream())
                {
                    PackagePart part = package.CreatePart(new Uri("/extension.vsixmanifest", UriKind.Relative), "text/xml", CompressionOption.Normal);
                    this.SaveStreamToPart(manifestStream, part);
                }
            }

            FileTransfer.ExecuteTransfer(this, packageTransfer);
        }

        /// <summary>
        /// Saves the stream to a package part's stream.
        /// </summary>
        /// <param name="stream">Stream to save.</param>
        /// <param name="part">Package part to get the stream.</param>
        private void SaveStreamToPart(Stream stream, PackagePart part)
        {
            using (Stream partStream = part.GetStream(FileMode.Create))
            {
                int read = 0;
                byte[] buffer = new byte[65536];
                while (0 < (read = stream.Read(buffer, 0, buffer.Length)))
                {
                    partStream.Write(buffer, 0, read);
                }
            }
        }

        /// <summary>
        /// Saves the manifest stream to a file.
        /// </summary>
        /// <param name="manifest">Manifest to save.</param>
        /// <param name="path">Path to save manifest to.</param>
        private void SaveManifest(VsixManifest manifest, string path)
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
