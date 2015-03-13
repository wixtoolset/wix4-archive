//-------------------------------------------------------------------------------------------------
// <copyright file="ExtractEmbeddedFilesCommand.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bind
{
    using System.IO;
    using System.Reflection;
    using WixToolset.Data;

    internal class ExtractEmbeddedFilesCommand : ICommand
    {
        public ExtractEmbeddedFiles FilesWithEmbeddedFiles { private get; set; }

        public void Execute()
        {
            foreach (var baseUri in this.FilesWithEmbeddedFiles.Uris)
            {
                Stream stream = null;
                try
                {
                    // If the embedded files are stored in an assembly resource stream (usually
                    // a .wixlib embedded in a WixExtension).
                    if ("embeddedresource" == baseUri.Scheme)
                    {
                        string assemblyPath = Path.GetFullPath(baseUri.LocalPath);
                        string resourceName = baseUri.Fragment.TrimStart('#');

                        Assembly assembly = Assembly.LoadFile(assemblyPath);
                        stream = assembly.GetManifestResourceStream(resourceName);
                    }
                    else // normal file (usually a binary .wixlib on disk).
                    {
                        stream = File.OpenRead(baseUri.LocalPath);
                    }

                    using (FileStructure fs = FileStructure.Read(stream))
                    {
                        foreach (var embeddedFile in this.FilesWithEmbeddedFiles.GetExtractFilesForUri(baseUri))
                        {
                            fs.ExtractEmbeddedFile(embeddedFile.EmbeddedFileIndex, embeddedFile.OutputPath);
                        }
                    }
                }
                finally
                {
                    if (null != stream)
                    {
                        stream.Close();
                    }
                }
            }
        }
    }
}
