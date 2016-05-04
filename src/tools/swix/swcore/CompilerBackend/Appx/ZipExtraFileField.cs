// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.IO;

    internal enum ZipExtraFileFieldType : ushort
    {
        ZIP64 = 0x0001,
        NTFS_TIMES = 0x000A,
        NTFS_ACLS = 0x4453,
        EXTIME = 0x5455
    }

    internal class ZipExtraFileField
    {
        public ZipExtraFileFieldType fieldType;
        public byte[] data;

        public bool Read(Stream stream, ref int bytesRemaining)
        {
            if (bytesRemaining < 4)
            {
                return false;
            }

            BinaryReader reader = new BinaryReader(stream);

            this.fieldType = (ZipExtraFileFieldType)reader.ReadUInt16();
            ushort dataSize = reader.ReadUInt16();
            bytesRemaining -= 4;

            if (bytesRemaining < dataSize)
            {
                return false;
            }

            this.data = reader.ReadBytes(dataSize);
            bytesRemaining -= dataSize;

            return true;
        }

        public void Write(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((ushort)this.fieldType);

            byte[] dataBytes = (this.data != null ? this.data : new byte[0]);
            writer.Write((ushort)dataBytes.Length);
            writer.Write(dataBytes);
        }

        public bool GetZip64Data(out long compressedSize, out long uncompressedSize, out long localHeaderOffset, out int diskStart)
        {
            uncompressedSize = 0;
            compressedSize = 0;
            localHeaderOffset = 0;
            diskStart = 0;

            // TOOD: 28 should be 24
            if (this.fieldType != ZipExtraFileFieldType.ZIP64 || this.data == null || (this.data.Length != 24 && this.data.Length != 28))
            {
                return false;
            }

            using (MemoryStream dataStream = new MemoryStream(this.data))
            {
                BinaryReader reader = new BinaryReader(dataStream);
                uncompressedSize = reader.ReadInt64();
                compressedSize = reader.ReadInt64();
                localHeaderOffset = reader.ReadInt64();

                if (this.data.Length == 28)
                {
                    diskStart = reader.ReadInt32();
                }
            }

            return true;
        }

        public bool SetZip64Data( long compressedSize,
            long uncompressedSize,
            long localHeaderOffset,
            int diskStart)
        {
            if (this.fieldType != ZipExtraFileFieldType.ZIP64)
            {
                return false;
            }

            using (MemoryStream dataStream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(dataStream);
                writer.Write(uncompressedSize);
                writer.Write(compressedSize);
                writer.Write(localHeaderOffset);
                writer.Write(diskStart);
                this.data = dataStream.ToArray();
            }

            return true;
        }
    }
}
