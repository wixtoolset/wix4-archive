// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;

    internal class ZipEndOfCentralDirectory
    {
        public const uint EOCDSIG = 0x06054B50;
        public const uint EOCD64SIG = 0x06064B50;

        public const uint EOCD_RECORD_FIXEDSIZE = 22;
        public const uint EOCD64_RECORD_FIXEDSIZE = 56;

        public ushort versionMadeBy;
        public ushort versionNeeded;
        public uint diskNumber;
        public uint dirStartDiskNumber;
        public long entriesOnDisk;
        public long totalEntries;
        public long dirSize;
        public long dirOffset;
        public string comment;
        public bool zip64;

        private List<ZipFileHeader> headers = new List<ZipFileHeader>();

        public ZipEndOfCentralDirectory()
        {
            this.versionMadeBy = 20;
            this.versionNeeded = 20;
        }

        public ZipEndOfCentralDirectory(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            // Start searching for the Zip End of Central Directory signature from the end
            // of the file backwards (towards the beginning of the file). This allows us to
            // find Zip archives with extra data appended to the archive (for whatever reason
            // someone might want to do that).
            long offset = stream.Length - ZipEndOfCentralDirectory.EOCD_RECORD_FIXEDSIZE;
            for (; offset >= 0; --offset)
            {
                stream.Seek(offset, SeekOrigin.Begin);

                uint sig = reader.ReadUInt32();
                if (sig == ZipEndOfCentralDirectory.EOCDSIG)
                {
                    break;
                }
            }

            if (offset < 0)
            {
                throw new InvalidDataException("Failed to find end of central directory record.");
            }

            stream.Seek(offset, SeekOrigin.Begin);
            this.Read(stream);

            // If the offset to the central directory is DWORD_MAX then this must be a 64-bit
            // archive.
            if (this.dirOffset == (long)UInt32.MaxValue)
            {
                string saveComment = this.comment;

                stream.Seek(offset - Zip64EndOfCentralDirectoryLocator.EOCDL64_SIZE, SeekOrigin.Begin);

                Zip64EndOfCentralDirectoryLocator eocdl = new Zip64EndOfCentralDirectoryLocator(stream);
                if (eocdl.dirStartDiskNumber == eocdl.totalDisks - 1)
                {
                    // ZIP64 eocd is entirely in current stream.
                    stream.Seek(eocdl.dirOffset, SeekOrigin.Begin);
                    this.Read64(stream);
                }
                else
                {
                    // TODO: handle EOCD64 spanning archives!
                    throw new NotImplementedException("Zip implementation does not handle end of central directory record that spans archives.");
                }

                this.comment = saveComment;
            }

            // Read the central directory for the archive.
            stream.Seek(this.dirOffset, SeekOrigin.Begin);

            while (this.headers.Count < this.totalEntries)
            {
                ZipFileHeader header = new ZipFileHeader(true);
                if (!header.Read(stream))
                {
                    throw new InvalidDataException("Missing or invalid central directory file header");
                }

                this.headers.Add(header);

                if (this.headers.Count < this.totalEntries && stream.Position == stream.Length)
                {
                    //streamContext.CloseArchiveReadStream(this.currentArchiveNumber, String.Empty, archiveStream);
                    //this.currentArchiveNumber++;
                    //archiveStream = streamContext.OpenArchiveReadStream(this.currentArchiveNumber, String.Empty, this);
                    //if (archiveStream == null)
                    //{
                    //    this.currentArchiveNumber = 0;
                    //    archiveStream = streamContext.OpenArchiveReadStream(this.currentArchiveNumber, String.Empty, this);
                    //}
                }
            }
        }

        public IList<ZipFileHeader> CentralDirectory { get { return this.headers; } }

        public void Read(Stream stream)
        {
            if (stream.Length - stream.Position < EOCD_RECORD_FIXEDSIZE)
            {
                throw new InvalidDataException("");
            }

            BinaryReader reader = new BinaryReader(stream);
            uint sig = reader.ReadUInt32();
            if (sig != EOCDSIG)
            {
                throw new InvalidDataException("");
            }

            this.zip64 = false;
            this.diskNumber = reader.ReadUInt16();
            this.dirStartDiskNumber = reader.ReadUInt16();
            this.entriesOnDisk = reader.ReadUInt16();
            this.totalEntries = reader.ReadUInt16();
            this.dirSize = reader.ReadUInt32();
            this.dirOffset = reader.ReadUInt32();

            int commentLength = reader.ReadUInt16();
            if (stream.Length - stream.Position < commentLength)
            {
                throw new InvalidDataException("");
            }

            byte[] commentBytes = reader.ReadBytes(commentLength);
            this.comment = Encoding.UTF8.GetString(commentBytes);
        }

        public void Read64(Stream stream)
        {
            if (stream.Length - stream.Position < EOCD64_RECORD_FIXEDSIZE)
            {
                throw new InvalidDataException("");
            }

            BinaryReader reader = new BinaryReader(stream);

            uint sig = reader.ReadUInt32();
            if (sig != EOCD64SIG)
            {
                throw new InvalidDataException("");
            }

            this.zip64 = true;
            long recordSize = reader.ReadInt64();
            this.versionMadeBy = reader.ReadUInt16();
            this.versionNeeded = reader.ReadUInt16();
            this.diskNumber = reader.ReadUInt32();
            this.dirStartDiskNumber = reader.ReadUInt32();
            this.entriesOnDisk = reader.ReadInt64();
            this.totalEntries = reader.ReadInt64();
            this.dirSize = reader.ReadInt64();
            this.dirOffset = reader.ReadInt64();

            // Ignore any extended zip64 eocd data.
            long exDataSize = recordSize + 12 - EOCD64_RECORD_FIXEDSIZE;
            if (stream.Length - stream.Position < exDataSize)
            {
                throw new InvalidDataException("Invalid extra data size in 64-bit end of central directory.");
            }

            stream.Seek(exDataSize, SeekOrigin.Current);
            this.comment = null;
        }

        public void Write(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(EOCDSIG);
            writer.Write(this.zip64 ? UInt16.MaxValue : (ushort)this.diskNumber);
            writer.Write(this.zip64 ? UInt16.MaxValue : (ushort)this.dirStartDiskNumber);
            writer.Write(this.zip64 ? UInt16.MaxValue : (ushort)this.entriesOnDisk);
            writer.Write(this.zip64 ? UInt16.MaxValue : (ushort)this.totalEntries);
            writer.Write(this.zip64 ? UInt32.MaxValue : (uint)this.dirSize);
            writer.Write(this.zip64 ? UInt32.MaxValue : (uint)this.dirOffset);

            byte[] commentBytes = (String.IsNullOrEmpty(this.comment) ? new byte[0] : Encoding.UTF8.GetBytes(this.comment));
            writer.Write((ushort)commentBytes.Length);
            writer.Write(commentBytes);
        }

        public void Write64(Stream stream)
        {
            if (!this.zip64)
            {
                throw new InvalidOperationException("Can only write 64-bit headers for 64-bit archives.");
            }

            Zip64EndOfCentralDirectoryLocator eocdl = new Zip64EndOfCentralDirectoryLocator(stream.Position);

            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(EOCD64SIG);
            writer.Write((long)EOCD64_RECORD_FIXEDSIZE);
            writer.Write(this.versionMadeBy);
            writer.Write(this.versionNeeded);
            writer.Write(this.diskNumber);
            writer.Write(this.dirStartDiskNumber);
            writer.Write(this.entriesOnDisk);
            writer.Write(this.totalEntries);
            writer.Write(this.dirSize);
            writer.Write(this.dirOffset);

            eocdl.Write(stream);
        }

        public int GetSize(bool zip64Size)
        {
            if (zip64Size)
            {
                return (int)EOCD64_RECORD_FIXEDSIZE;
            }
            else
            {
                int commentSize = (String.IsNullOrEmpty(this.comment) ? 0 : Encoding.UTF8.GetByteCount(this.comment));
                return (int)EOCD_RECORD_FIXEDSIZE + commentSize;
            }
        }

        private class Zip64EndOfCentralDirectoryLocator
        {
            public const uint EOCDL64SIG = 0x07064B50;
            public const uint EOCDL64_SIZE = 20;

            public uint dirStartDiskNumber;
            public long dirOffset;
            public uint totalDisks;

            public Zip64EndOfCentralDirectoryLocator(long position)
            {
                this.dirOffset = position;
                this.dirStartDiskNumber = 0;
                this.totalDisks = 1;
            }

            public Zip64EndOfCentralDirectoryLocator(Stream stream)
            {
                if (stream.Length - stream.Position < EOCDL64_SIZE)
                {
                    throw new InvalidDataException("Stream wasn't long enough to hold 64-bit central directory locator.");
                }

                BinaryReader reader = new BinaryReader(stream);
                uint sig = reader.ReadUInt32();

                if (sig != EOCDL64SIG)
                {
                    throw new InvalidDataException("Invalid signature found when looking for 64-bit central directory locator.");
                }

                this.dirStartDiskNumber = reader.ReadUInt32();
                this.dirOffset = reader.ReadInt64();
                this.totalDisks = reader.ReadUInt32();
            }

            public void Write(Stream stream)
            {
                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(EOCDL64SIG);
                writer.Write(this.dirStartDiskNumber);
                writer.Write(this.dirOffset);
                writer.Write(this.totalDisks);
            }
        }
    }
}
