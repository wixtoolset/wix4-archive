// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend
{
    using System;
    using WixToolset.Simplified.Lexicon;

    internal class PackageFile
    {
        /// <summary>
        /// Package files should be created via <see cref="TryCreate"/> method.
        /// </summary>
        private PackageFile()
        {
        }

        public File File { get; set; }

        public string SourcePath { get; set; }

        public string PartUri { get; set; }

        public string MimeType { get; set; }

        public static bool TryCreate(BackendCompiler backend, File file, out PackageFile packageFile)
        {
            packageFile = null;

            string path;
            if (backend.FileManager.TryResolvePath(file.Source, out path))
            {
                string mimeType = "application/x-msdownload"; // TODO: pick appropriate MIME type.
                string partUri = PackageFile.ConvertToPartUri(backend, file.Path);

                packageFile = new PackageFile() { File = file, MimeType = mimeType, PartUri = partUri, SourcePath = path };
            }
            else
            {
                backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.FileNotFound(file.Source), file));
            }

            return (packageFile != null);
        }

        private static string ConvertToPartUri(BackendCompiler backend, string path)
        {
            string[] idPath = path.Split(new char[] { ':' }, 2);
            if (!idPath[0].Equals("ApplicationFolder") && !idPath[0].Equals("InstallFolder"))
            {
                // TOOD: send warning that we are ignoring all other roots and we always put files in ApplicationFolder
            }

            return idPath[1].Replace('\\', '/');
        }
    }
}
