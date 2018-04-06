// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [Flags]
    internal enum ZipFileFlags : ushort
    {
        None = 0x0000,
        Encrypt = 0x0001,
        CompressOption1 = 0x0002,
        CompressOption2 = 0x0004,
        DataDescriptor = 0x0008,
        StrongEncrypt = 0x0040,
        UTF8 = 0x0800
    }

    internal class ZipFileHeader
    {
        public const uint LFHSIG = 0x04034B50;
        public const uint CFHSIG = 0x02014B50;

        public const uint SPANSIG = 0x08074b50;
        public const uint SPANSIG2 = 0x30304b50;

        public const uint LFH_FIXEDSIZE = 30;
        public const uint CFH_FIXEDSIZE = 46;

        public ushort versionMadeBy;
        public ushort versionNeeded;
        public ZipFileFlags flags;
        public ZipCompressionMethod compressionMethod;
        public short lastModTime;
        public short lastModDate;
        public uint crc32;
        public uint compressedSize;
        public uint uncompressedSize;
        public ushort diskStart;
        public ushort internalFileAttrs;
        public uint externalFileAttrs;
        public uint localHeaderOffset;
        public string fileName;
        public ZipExtraFileField[] extraFields;
        public string fileComment;
        public bool zip64;

        public bool central;

        public ZipFileHeader(bool central)
        {
            this.versionMadeBy = 20;
            this.versionNeeded = 20;

            this.central = central;
        }

        /// <summary>
        /// Creates a central file header from the information in a local file header.
        /// </summary>
        /// <param name="localFileHeader">Local file header to </param>
        public ZipFileHeader(ZipFileHeader localFileHeader)
        {
            if (localFileHeader.central)
            {
                throw new ArgumentException();
            }

            this.central = true;
            this.compressionMethod = localFileHeader.compressionMethod;
            this.fileName = localFileHeader.fileName;
            this.fileComment = localFileHeader.fileComment;
            this.flags = localFileHeader.flags;
            this.externalFileAttrs = localFileHeader.externalFileAttrs;
            this.internalFileAttrs = localFileHeader.internalFileAttrs;
            this.lastModDate = localFileHeader.lastModDate;
            this.lastModTime = localFileHeader.lastModTime;
            this.crc32 = localFileHeader.crc32;

            if (this.zip64)
            {
                this.versionMadeBy = 45;
                this.versionNeeded = 45;

                this.compressedSize = UInt32.MaxValue;
                this.uncompressedSize = UInt32.MaxValue;
                this.diskStart = UInt16.MaxValue;

                ZipExtraFileField field = new ZipExtraFileField();
                field.fieldType = ZipExtraFileFieldType.ZIP64;
                field.SetZip64Data(localFileHeader.compressedSize, localFileHeader.uncompressedSize, localFileHeader.localHeaderOffset, localFileHeader.diskStart);
                this.extraFields = new ZipExtraFileField[] { field };
            }
            else
            {
                this.versionMadeBy = 20;
                this.versionNeeded = 20;

                this.localHeaderOffset = localFileHeader.localHeaderOffset;
                this.compressedSize = localFileHeader.compressedSize;
                this.uncompressedSize = localFileHeader.uncompressedSize;
            }
        }

        //public ZipFileHeader(ZipFileInfo fileInfo, bool zip64)
        //    : this()
        //{
        //    this.flags = ZipFileFlags.None;
        //    this.compressionMethod = fileInfo.CompressionMethod;
        //    this.fileName = Path.Combine(fileInfo.Path, fileInfo.Name);
        //    CompressionEngine.DateTimeToDosDateAndTime(fileInfo.LastWriteTime, out this.lastModDate, out this.lastModTime);
        //    this.zip64 = zip64;

        //    if (this.zip64)
        //    {
        //        this.compressedSize = UInt32.MaxValue;
        //        this.uncompressedSize = UInt32.MaxValue;
        //        this.diskStart = UInt16.MaxValue;
        //        this.versionMadeBy = 45;
        //        this.versionNeeded = 45;
        //        ZipExtraFileField field = new ZipExtraFileField();
        //        field.fieldType = ZipExtraFileFieldType.ZIP64;
        //        field.SetZip64Data(
        //            fileInfo.CompressedLength,
        //            fileInfo.Length,
        //            0,
        //            fileInfo.ArchiveNumber);
        //        this.extraFields = new ZipExtraFileField[] { field };
        //    }
        //    else
        //    {
        //        this.compressedSize = (uint)fileInfo.CompressedLength;
        //        this.uncompressedSize = (uint)fileInfo.Length;
        //        this.diskStart = (ushort)fileInfo.ArchiveNumber;
        //    }
        //}

        public void Update(long compressedSize, long uncompressedSize, uint crc32, long localHeaderOffset, int archiveNumber)
        {
            this.crc32 = crc32;

            if (this.zip64)
            {
                this.compressedSize = UInt32.MaxValue;
                this.uncompressedSize = UInt32.MaxValue;
                this.localHeaderOffset = UInt32.MaxValue;
                this.diskStart = UInt16.MaxValue;

                if (this.extraFields != null)
                {
                    foreach (ZipExtraFileField field in this.extraFields)
                    {
                        if (field.fieldType == ZipExtraFileFieldType.ZIP64)
                        {
                            field.SetZip64Data(
                                compressedSize,
                                uncompressedSize,
                                localHeaderOffset,
                                archiveNumber);
                        }
                    }
                }
            }
            else
            {
                this.compressedSize = (uint)compressedSize;
                this.uncompressedSize = (uint)uncompressedSize;
                this.localHeaderOffset = (uint)localHeaderOffset;
                this.diskStart = (ushort)archiveNumber;
            }
        }

        public bool Read(Stream stream)
        {
            if (stream.Length - stream.Position < (this.central ? CFH_FIXEDSIZE : LFH_FIXEDSIZE))
            {
                return false;
            }

            BinaryReader reader = new BinaryReader(stream);
            uint sig = reader.ReadUInt32();

            if (sig == SPANSIG || sig == SPANSIG2)
            {
                // Spanned zip files may optionally begin with a special marker.
                // Just ignore it and move on.
                sig = reader.ReadUInt32();
            }

            if (sig != (this.central ? CFHSIG : LFHSIG))
            {
                return false;
            }

            this.versionMadeBy = (this.central ? reader.ReadUInt16() : (ushort)0);
            this.versionNeeded = reader.ReadUInt16();
            this.flags = (ZipFileFlags)reader.ReadUInt16();
            this.compressionMethod = (ZipCompressionMethod)reader.ReadUInt16();
            this.lastModTime = reader.ReadInt16();
            this.lastModDate = reader.ReadInt16();
            this.crc32 = reader.ReadUInt32();
            this.compressedSize = reader.ReadUInt32();
            this.uncompressedSize = reader.ReadUInt32();

            this.zip64 = this.uncompressedSize == UInt32.MaxValue;

            int fileNameLength = reader.ReadUInt16();
            int extraFieldLength = reader.ReadUInt16();
            int fileCommentLength;

            if (this.central)
            {
                fileCommentLength = reader.ReadUInt16();

                this.diskStart = reader.ReadUInt16();
                this.internalFileAttrs = reader.ReadUInt16();
                this.externalFileAttrs = reader.ReadUInt32();
                this.localHeaderOffset = reader.ReadUInt32();
            }
            else
            {
                fileCommentLength = 0;
                this.diskStart = 0;
                this.internalFileAttrs = 0;
                this.externalFileAttrs = 0;
                this.localHeaderOffset = 0;
            }

            if (stream.Length - stream.Position < fileNameLength + extraFieldLength + fileCommentLength)
            {
                return false;
            }

            Encoding headerEncoding = ((this.flags | ZipFileFlags.UTF8) != 0 ? Encoding.UTF8 : Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage));

            byte[] fileNameBytes = reader.ReadBytes(fileNameLength);
            this.fileName = headerEncoding.GetString(fileNameBytes);

            List<ZipExtraFileField> fields = new List<ZipExtraFileField>();
            while (extraFieldLength > 0)
            {
                ZipExtraFileField field = new ZipExtraFileField();
                if (!field.Read(stream, ref extraFieldLength))
                {
                    return false;
                }
                fields.Add(field);
                if (field.fieldType == ZipExtraFileFieldType.ZIP64)
                {
                    this.zip64 = true;
                }
            }
            this.extraFields = fields.ToArray();

            byte[] fileCommentBytes = reader.ReadBytes(fileCommentLength);
            this.fileComment = headerEncoding.GetString(fileCommentBytes);

            return true;
        }

        public void Write(Stream stream)
        {
            byte[] fileNameBytes = String.IsNullOrEmpty(this.fileName) ? new byte[0]: Encoding.UTF8.GetBytes(this.fileName);
            byte[] fileCommentBytes = String.IsNullOrEmpty(this.fileComment) ? new byte[0] : Encoding.UTF8.GetBytes(this.fileComment);
            bool useUtf8 = (this.fileName != null && fileNameBytes.Length > this.fileName.Length) || (this.fileComment != null && fileCommentBytes.Length > this.fileComment.Length);
            if (useUtf8)
            {
                this.flags |= ZipFileFlags.UTF8;
            }

            short time = this.lastModTime;
            short date = this.lastModDate;
            if (time == 0 && date == 0)
            {
                long filetime = DateTime.UtcNow.ToFileTimeUtc();
                FileTimeToDosDateTime(ref filetime, out date, out time);
            }

            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(this.central ? CFHSIG : LFHSIG);
            if (this.central)
            {
                writer.Write(this.versionMadeBy);
            }
            writer.Write(this.versionNeeded);
            writer.Write((ushort)this.flags);
            writer.Write((ushort)this.compressionMethod);
            writer.Write(time);
            writer.Write(date);
            writer.Write(this.crc32);
            writer.Write(this.compressedSize);
            writer.Write(this.uncompressedSize);

            ushort extraFieldLength = 0;
            if (this.extraFields != null)
            {
                foreach (ZipExtraFileField field in this.extraFields)
                {
                    if (field.data != null)
                    {
                        extraFieldLength += (ushort)(4 + field.data.Length);
                    }
                }
            }

            writer.Write((ushort)fileNameBytes.Length);
            writer.Write(extraFieldLength);

            if (this.central)
            {
                writer.Write((ushort)fileCommentBytes.Length);

                writer.Write(this.diskStart);
                writer.Write(this.internalFileAttrs);
                writer.Write(this.externalFileAttrs);
                writer.Write(this.localHeaderOffset);
            }

            writer.Write(fileNameBytes);

            if (this.extraFields != null)
            {
                foreach (ZipExtraFileField field in this.extraFields)
                {
                    if (field.data != null)
                    {
                        field.Write(stream);
                    }
                }
            }

            if (this.central)
            {
                writer.Write(fileCommentBytes);
            }
        }

        public void WriteDataDescriptor(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(SPANSIG);
            writer.Write(this.crc32);
            if (this.zip64)
            {
                writer.Write((UInt64)this.compressedSize);
                writer.Write((UInt64)this.uncompressedSize);
            }
            else
            {
                writer.Write(this.compressedSize);
                writer.Write(this.uncompressedSize);
            }
        }

        public void GetZip64Fields(
            out long compressedSize,
            out long uncompressedSize,
            out long localHeaderOffset,
            out int archiveNumber,
            out uint crc)
        {
            compressedSize = this.compressedSize;
            uncompressedSize = this.uncompressedSize;
            localHeaderOffset = this.localHeaderOffset;
            archiveNumber = this.diskStart;
            crc = this.crc32;

            foreach (ZipExtraFileField field in this.extraFields)
            {
                if (field.fieldType == ZipExtraFileFieldType.ZIP64)
                {
                    field.GetZip64Data(
                        out compressedSize,
                        out uncompressedSize,
                        out localHeaderOffset,
                        out archiveNumber);
                }
            }
        }

        //public ZipFileInfo ToZipFileInfo()
        //{
        //    string name = this.fileName;

        //    long compressedSizeL;
        //    long uncompressedSizeL;
        //    long localHeaderOffsetL;
        //    int archiveNumberL;
        //    uint crc;
        //    this.GetZip64Fields(
        //        out compressedSizeL,
        //        out uncompressedSizeL,
        //        out localHeaderOffsetL,
        //        out archiveNumberL,
        //        out crc);

        //    DateTime dateTime;
        //    CompressionEngine.DosDateAndTimeToDateTime(this.lastModDate, this.lastModTime, out dateTime);
        //    FileAttributes attrs = FileAttributes.Normal;
        //    // TODO: look for attrs or times in extra fields

        //    return new ZipFileInfo(name, archiveNumberL, attrs, dateTime,
        //        uncompressedSizeL, compressedSizeL, this.compressionMethod);
        //}

        public bool IsDirectory
        {
            get
            {
                return this.fileName != null && (this.fileName.EndsWith("/", StringComparison.Ordinal) || this.fileName.EndsWith("\\", StringComparison.Ordinal));
            }
        }

        public int GetSize()
        {
            int size = (int)(this.central ? CFH_FIXEDSIZE : LFH_FIXEDSIZE);

            int fileNameSize = (String.IsNullOrEmpty(this.fileName) ? 0 : Encoding.UTF8.GetByteCount(this.fileName));
            size += fileNameSize;

            if (this.extraFields != null)
            {
                foreach (ZipExtraFileField field in this.extraFields)
                {
                    if (field.data != null)
                    {
                        size += 4 + field.data.Length;
                    }
                }
            }

            if (this.central)
            {
                int fileCommentSize = (String.IsNullOrEmpty(this.fileComment) ? 0 : Encoding.UTF8.GetByteCount(this.fileComment));
                size += fileCommentSize;
            }

            return size;
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FileTimeToDosDateTime(ref long fileTime, out short wFatDate, out short wFatTime);
    }
}
