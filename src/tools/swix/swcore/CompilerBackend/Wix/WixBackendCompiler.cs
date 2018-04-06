// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Simplified.Lexicon;
    using WixToolset.Simplified.Lexicon.Msi;
    using IO = System.IO;

    /// <summary>
    /// Backend compiler that generates .msi and .wixlib files.
    /// </summary>
    internal class WixBackendCompiler : BackendCompiler
    {
        /// <summary>
        /// Creates a backend compiler for a specific architecture targeting a specific output.
        /// </summary>
        /// <param name="outputType">Type of output to generate.</param>
        internal WixBackendCompiler(CompilerOutputType outputType) :
            base(outputType)
        {
        }

        public IDictionary<PackageItem, WixItem> WixItems { get; private set; }

        /// <summary>
        /// Generates the final output from a set of intermediates.
        /// </summary>
        /// <param name="intermediates">Intermediates that provide data to be generated.</param>
        /// <param name="outputPath">Path for output file to be generated.</param>
        public override void Generate(IEnumerable<Intermediate> intermediates, string outputPath)
        {
            if (this.OutputType != CompilerOutputType.WixLibrary)
            {
                throw new NotImplementedException("Only creating .wixlib files is currently supported.");
            }

            this.WixItems = this.ProcessIntermediates(intermediates);

            XElement library = new XElement(WixBackendCompilerServices.WixlibNamespace + "wixLibrary",
                new XAttribute("version", WixBackendCompilerServices.WixlibVersion));

            foreach (WixItem item in this.WixItems.Values)
            {
                if (item.System) // skip system items.
                {
                    continue;
                }

                WixSection section = item.GenerateSection();
                if (section != null)
                {
                    library.Add(section.Xml);
                }
            }

            FileTransfer wixlibTransfer = FileTransfer.Create(null, IO.Path.GetTempFileName(), outputPath, "WixLibrary", true);
            using (IO.Stream wixlibStream = IO.File.Open(wixlibTransfer.Source, IO.FileMode.Create, IO.FileAccess.ReadWrite, IO.FileShare.Delete))
            {
                library.Save(wixlibStream);
            }

            FileTransfer.ExecuteTransfer(this, wixlibTransfer);
        }

        private Dictionary<PackageItem, WixItem> ProcessIntermediates(IEnumerable<Intermediate> intermediates)
        {
            Dictionary<PackageItem, WixItem> wixItems = new Dictionary<PackageItem, WixItem>();
            foreach (Intermediate intermediate in intermediates)
            {
                foreach (PackageItem item in intermediate.Items)
                {
                    File file = item as File;
                    if (file != null)
                    {
                        WixFile msiFile = new WixFile(this, file);
                        wixItems.Add(file, msiFile);

                        NgenPackageItem ngen;
                        if (Ngen.TryGetPackageItem(file, out ngen))
                        {
                            ngen.LineNumber = file.LineNumber;
                            file.Items.Add(ngen);

                            WixNativeImage nativeImage = new WixNativeImage(this, ngen);
                            wixItems.Add(ngen, nativeImage);
                        }
                    }
                    else
                    {
                        Folder folder = item as Folder;
                        if (folder != null)
                        {
                            if (folder.External)
                            {
                                WixFolderReference msiFolderRef = new WixFolderReference(this, folder);
                                wixItems.Add(folder, msiFolderRef);
                            }
                            else
                            {
                                WixFolder msiFolder = new WixFolder(this, folder);
                                wixItems.Add(folder, msiFolder);
                            }
                        }
                        else
                        {
                            InprocServer inprocServer = item as InprocServer;
                            if (null != inprocServer)
                            {
                                WixClassId classId = new WixClassId(this, inprocServer);
                                wixItems.Add(inprocServer, classId);
                            }
                            else
                            {
                                OutprocServer outprocServer = item as OutprocServer;
                                if (null != outprocServer)
                                {
                                    WixClassId classId = new WixClassId(this, outprocServer);
                                    wixItems.Add(outprocServer, classId);
                                }
                                else
                                {
                                    Property property = item as Property;
                                    if (property != null)
                                    {
                                        WixProperty prop = new WixProperty(this, property);
                                        wixItems.Add(property, prop);
                                    }
                                    else
                                    {
                                        FileSearch fileSearch = item as FileSearch;
                                        if (fileSearch != null)
                                        {
                                            WixFileSearch fs = new WixFileSearch(this, fileSearch);
                                            wixItems.Add(fileSearch, fs);
                                        }
                                        else
                                        {
                                            Group group = item as Group;
                                            if (group != null)
                                            {
                                                WixGroup msiGroup = new WixGroup(this, group);
                                                wixItems.Add(group, msiGroup);
                                            }
                                            else if (item is Package)
                                            {
                                                // TODO: send an error message since library files cannot process Package elements.
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Fix up all the item parents and groups now that we have them
            // all processed into a single look up dictionary.
            foreach (var kv in wixItems)
            {
                PackageItem pi = kv.Key;
                WixItem wi = kv.Value;
                if (pi.Parent != null)
                {
                    wi.Parent = wixItems[pi.Parent];
                }

                if (pi.Group != null)
                {
                    wi.Group = (WixGroup)wixItems[pi.Group];
                    wi.Group.ContainedWixItems.Add(wi);
                }
            }

            return wixItems;
        }
    }
}
