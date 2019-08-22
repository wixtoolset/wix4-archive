// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Nuget
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Packaging;
    using System.Xml.Schema;

    /// <summary>
    /// Backend compiler that generates .nupkg files.
    /// </summary>
    internal class NugetBackendCompiler : BackendCompiler
    {
        /// <summary>
        /// Creates a backend compiler for a specific architecture.
        /// </summary>
        internal NugetBackendCompiler() :
            base(CompilerOutputType.NugetPackage)
        {
        }

        /// <summary>
        /// Generates the final output from a set of intermediates.
        /// </summary>
        /// <param name="intermediates">Intermediates that provide data to be generated.</param>
        /// <param name="outputPath">Path for output file to be generated.</param>
        public override void Generate(IEnumerable<Intermediate> intermediates, string outputPath)
        {
            NugetManifest manifest = new NugetManifest(this);

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

            FileTransfer packageTransfer = FileTransfer.Create(null, Path.GetTempFileName(), outputPath, "NugetPackage", true);
            using (Package package = Package.Open(packageTransfer.Source, FileMode.Create))
            {
                // Add all the manifest files.
                foreach (PackageFile file in manifest.Files)
                {
                    this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.CompressFile(file.SourcePath), file.File.LineNumber));

                    using (Stream fileStream = File.Open(file.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        PackagePart part = package.CreatePart(new Uri(file.PartUri, UriKind.Relative), file.MimeType, CompressionOption.Maximum);
                        this.SaveStreamToPart(fileStream, part);
                    }
                }

                var manifestUri = new Uri(String.Concat("/", manifest.Name, ".nuspec"), UriKind.Relative);

                // Create the manifest relationship
                package.CreateRelationship(manifestUri, TargetMode.Internal, "http://schemas.microsoft.com/packaging/2010/07/manifest");

                // Save the manifest to the package.
                using (Stream manifestStream = manifest.GetStream())
                {
                    PackagePart part = package.CreatePart(manifestUri, "application/octet", CompressionOption.Maximum);
                    this.SaveStreamToPart(manifestStream, part);
                }

                package.PackageProperties.Creator = manifest.Manufacturer;
                package.PackageProperties.Description = manifest.Description;
                package.PackageProperties.Identifier = manifest.Name;
                package.PackageProperties.Version = manifest.Version;
                package.PackageProperties.Language = manifest.Language;
                package.PackageProperties.Keywords = manifest.Tags;
                package.PackageProperties.Title = manifest.DisplayName;
                package.PackageProperties.LastModifiedBy = "WiX Toolset v#.#.#.#";
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
        private void SaveManifest(NugetManifest manifest, string path)
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
