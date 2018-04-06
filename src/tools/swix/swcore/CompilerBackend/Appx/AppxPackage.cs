// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Xml.Linq;

    public enum CompressionLevel
    {
        DefaultCompression = -1,
        NoCompression = 0,
        BestSpeed = 1,
        BestCompression = 9,
    }

    public class AppxPackage
    {
        private const string BlockMapHashMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
        private static readonly XNamespace BlockMapNamespace = "http://schemas.microsoft.com/appx/2010/blockmap";
        private static readonly XNamespace ContentTypeNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";

        private ZipEndOfCentralDirectory eocd;
        private List<BlockMapFile> blockMapFiles;
        private Dictionary<string, ContentType> contentTypes;

        public AppxPackage(Stream output)
        {
            this.BaseStream = output;

            this.eocd = new ZipEndOfCentralDirectory();
            this.blockMapFiles = new List<BlockMapFile>();
            this.contentTypes = new Dictionary<string, ContentType>();
        }

        public Stream BaseStream { get; private set; }

        public void AddFile(Stream stream, string packageUri, string mimeType, CompressionLevel level)
        {
            string partName = this.DeterminePartName(packageUri);
            ContentType contentType = this.DetermineContentType(partName, mimeType);

            ZipFileHeader localFileHeader = new ZipFileHeader(false);
            localFileHeader.localHeaderOffset = (uint)this.BaseStream.Position;
            localFileHeader.zip64 = stream.Length > 0xFFFFFFFF;
            localFileHeader.versionMadeBy = 0;
            localFileHeader.versionNeeded = localFileHeader.zip64 ? (ushort)45 : (ushort)20;
            localFileHeader.fileName = partName;
            localFileHeader.compressionMethod = (level == CompressionLevel.NoCompression) ? ZipCompressionMethod.Store : ZipCompressionMethod.Deflate;
            localFileHeader.flags = this.BaseStream.CanSeek ? ZipFileFlags.None : ZipFileFlags.DataDescriptor;

            localFileHeader.Write(this.BaseStream);

            BlockMapFile blockMapFile = new BlockMapFile(partName) { Size = stream.Length, ZipLocalFileHeaderSize = localFileHeader.GetSize() };
            this.blockMapFiles.Add(blockMapFile);

            ZipCrc crc = new ZipCrc();
            BlockDeflateStream deflate = (level == CompressionLevel.NoCompression) ? null : new BlockDeflateStream(this.BaseStream, (BlockDeflateCompressionLevel)level);

            long outputStart = this.BaseStream.Position;

            int read = 0;
            byte[] buffer = new byte[64 * 1024];
            while (0 < (read = stream.Read(buffer, 0, buffer.Length)))
            {
                crc.UpdateCrc(buffer, 0, read);

                string hash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashedBytes = sha256.ComputeHash(buffer, 0, read);
                    hash = Convert.ToBase64String(hashedBytes);
                }

                long compressedBlockSize = 0;
                if (deflate != null)
                {
                    compressedBlockSize = deflate.Deflate(buffer, 0, read);
                }
                else
                {
                    this.BaseStream.Write(buffer, 0, read);
                    compressedBlockSize = -1;
                }

                BlockMapFileBlock block = new BlockMapFileBlock() { Hash = hash, CompressedSize = compressedBlockSize };
                blockMapFile.Blocks.Add(block);
            }

            if (deflate != null)
            {
                deflate.Flush();
            }
            else
            {
                this.BaseStream.Flush();
            }

            // Update the local file header with data discovered while processing file data.
            localFileHeader.crc32 = crc.Crc;
            localFileHeader.compressedSize = (uint)(this.BaseStream.Position - outputStart); // (uint)compressedSize;
            localFileHeader.uncompressedSize = (uint)stream.Length;

            // If we're using the data descriptor (because we can't see), append the updated information to the file data.
            if ((localFileHeader.flags & ZipFileFlags.DataDescriptor) == ZipFileFlags.DataDescriptor)
            {
                localFileHeader.WriteDataDescriptor(this.BaseStream);
            }
            else // go back to re-write the updated local file header in place, then seek back to our current location.
            {
                long position = this.BaseStream.Position;

                this.BaseStream.Seek(localFileHeader.localHeaderOffset, SeekOrigin.Begin);
                localFileHeader.Write(this.BaseStream);

                this.BaseStream.Seek(position, SeekOrigin.Begin);
            }

            // If we have a 64-bit local header then the central directory must be
            // ZIP64 as well.
            if (localFileHeader.zip64)
            {
                this.eocd.zip64 = true;
                this.eocd.versionMadeBy = 45;
                this.eocd.versionNeeded = 45;
            }

            ZipFileHeader centralFileHeader = new ZipFileHeader(localFileHeader);
            this.eocd.CentralDirectory.Add(centralFileHeader);
        }

        public void Finish(Stream manifestStream)
        {
            // Sort the block map then add the AppxManifest.xml to the end.
            this.blockMapFiles.Sort(delegate(BlockMapFile x, BlockMapFile y) { return String.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase); });
            this.AddFile(manifestStream, "AppxManifest.xml", "application/vnd.ms-appx.manifest+xml", CompressionLevel.DefaultCompression);

            // Generate the block map before adding the AppxBlockMap.xml file (obviously because we haven't saved it yet) and
            // without OPC [ContentTypes].xml file (also not created yet).
            XElement xBlockMap = this.CreateBlockMap();

            using (MemoryStream blockMapStream = new MemoryStream())
            {
                xBlockMap.Save(blockMapStream, SaveOptions.DisableFormatting);
                blockMapStream.Seek(0, SeekOrigin.Begin);

                this.AddFile(blockMapStream, "AppxBlockMap.xml", "application/vnd.ms-appx.blockmap+xml", CompressionLevel.DefaultCompression);
            }

            // Now that all the content and the Appx specific files have their content types captured, create
            // the OPC specific file then add it to the archive.
            XElement xContentTypes = new XElement(ContentTypeNamespace + "Types");
            foreach (ContentType type in this.contentTypes.Values)
            {
                XElement xType;
                if (String.IsNullOrEmpty(type.PartNameOverride))
                {
                    xType = new XElement(ContentTypeNamespace + "Default",
                        new XAttribute("Extension", type.Extension),
                        new XAttribute("ContentType", type.MimeType));
                }
                else
                {
                    xType = new XElement(ContentTypeNamespace + "Override",
                        new XAttribute("PartName", type.PartNameOverride),
                        new XAttribute("ContentType", type.MimeType));
                }
                xContentTypes.Add(xType);
            }

            using (MemoryStream contentTypesStream = new MemoryStream())
            {
                xContentTypes.Save(contentTypesStream, SaveOptions.DisableFormatting);
                contentTypesStream.Seek(0, SeekOrigin.Begin);

                this.AddFile(contentTypesStream, "[Content_Types].xml", "text/xml", CompressionLevel.DefaultCompression);
            }

            // Finalize the end of central directory.
            this.eocd.dirOffset = this.BaseStream.Position;
            this.eocd.dirSize = 0;
            this.eocd.dirStartDiskNumber = 0;
            this.eocd.diskNumber = 0;
            this.eocd.entriesOnDisk = 0;
            this.eocd.totalEntries = 0;

            // write the zip central directory
            foreach (ZipFileHeader centralRecord in this.eocd.CentralDirectory)
            {
                centralRecord.Write(this.BaseStream);

                this.eocd.dirSize += centralRecord.GetSize();
                ++this.eocd.entriesOnDisk;
                ++this.eocd.totalEntries;
            }

            // Write zip64 end of central directory record and zip64 end of central directory locator.
            if (this.eocd.zip64)
            {
                this.eocd.Write64(this.BaseStream);
            }

            // Write end of central directory record
            this.eocd.Write(this.BaseStream);
        }

        private XElement CreateBlockMap()
        {
            XElement xBlockMap = new XElement(BlockMapNamespace + "BlockMap",
                new XAttribute("HashMethod", BlockMapHashMethod));

            foreach (BlockMapFile file in this.blockMapFiles)
            {
                if (file.Size > 0)
                {
                    XElement xFile = new XElement(BlockMapNamespace + "File",
                        new XAttribute("Name", file.Name),
                        new XAttribute("Size", file.Size),
                        new XAttribute("LfhSize", file.ZipLocalFileHeaderSize));
                    xBlockMap.Add(xFile);

                    foreach (BlockMapFileBlock block in file.Blocks)
                    {
                        XElement xBlock = new XElement(BlockMapNamespace + "Block",
                            new XAttribute("Hash", block.Hash));
                        xFile.Add(xBlock);

                        if (block.CompressedSize > 0)
                        {
                            xBlock.Add(new XAttribute("Size", block.CompressedSize));
                        }
                    }
                }
            }

            return xBlockMap;
        }

        private string DeterminePartName(string packageUri)
        {
            return packageUri.StartsWith("/", StringComparison.Ordinal) ? packageUri = packageUri.Substring(1) : packageUri;
        }

        private ContentType DetermineContentType(string partName, string mimeType)
        {
            string extension = Path.GetExtension(partName).ToLowerInvariant().Substring(1); // get the extension in lower case minus the leading dot.

            ContentType contentType;
            if (!this.contentTypes.TryGetValue(extension, out contentType))
            {
                contentType = new ContentType() { Extension = extension, MimeType = mimeType };
                this.contentTypes.Add(contentType.Extension, contentType);
            }
            else if (!contentType.MimeType.Equals(mimeType, StringComparison.InvariantCultureIgnoreCase))
            {
                contentType = new ContentType() { PartNameOverride = partName.StartsWith("/") ? partName : String.Concat("/", partName), MimeType = mimeType };
                this.contentTypes.Add(contentType.PartNameOverride, contentType);
            }

            return contentType;
        }

        private class ContentType
        {
            public string Extension { get; set; }
            public string PartNameOverride { get; set; }
            public string MimeType { get; set; }
        }

        private class BlockMapFile
        {
            private List<BlockMapFileBlock> blocks = new List<BlockMapFileBlock>();

            public BlockMapFile(string name)
            {
                // Ensure file names in the blockmap do not use the OPC and ZIP directory separator
                // character "/" but use the Windows directory separator "\".
                this.Name = name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            public string Name { get; private set; }
            public long Size { get; set; }
            public int ZipLocalFileHeaderSize { get; set; }
            public List<BlockMapFileBlock> Blocks { get { return this.blocks; } }
        }

        private class BlockMapFileBlock
        {
            public string Hash { get; set; }
            public long CompressedSize { get; set; }
        }
    }
}
