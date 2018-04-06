// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class Zip
    {
        public IList<ZipFileHeader> GetCentralDirectory(Stream streamContext)
        {
            Stream archiveStream = null;
            //this.currentArchiveNumber = 0;
            try
            {
                List<ZipFileHeader> headers = new List<ZipFileHeader>();
                archiveStream = streamContext;
                //archiveStream = this.OpenArchive(streamContext, 0);

                ZipEndOfCentralDirectory eocd = this.GetEOCD(archiveStream);
                if (eocd == null)
                {
                    return null;
                }
                else if (eocd.totalEntries == 0)
                {
                    return headers;
                }

                headers.Capacity = (int)eocd.totalEntries;

                if (eocd.dirOffset > archiveStream.Length - ZipFileHeader.CFH_FIXEDSIZE)
                {
                    //streamContext.CloseArchiveReadStream(this.currentArchiveNumber, String.Empty, archiveStream);
                    archiveStream = null;
                }
                else
                {
                    archiveStream.Seek(eocd.dirOffset, SeekOrigin.Begin);
                    uint sig = new BinaryReader(archiveStream).ReadUInt32();
                    if (sig != ZipFileHeader.CFHSIG)
                    {
                        //streamContext.CloseArchiveReadStream(this.currentArchiveNumber, String.Empty, archiveStream);
                        archiveStream = null;
                    }
                }

                if (archiveStream == null)
                {
                    //this.currentArchiveNumber = (short)(eocd.dirStartDiskNumber + 1);
                    //archiveStream = streamContext.OpenArchiveReadStream(this.currentArchiveNumber, String.Empty, this);

                    if (archiveStream == null)
                    {
                        return null;
                    }
                }

                archiveStream.Seek(eocd.dirOffset, SeekOrigin.Begin);

                while (headers.Count < eocd.totalEntries)
                {
                    ZipFileHeader header = new ZipFileHeader(true);
                    if (!header.Read(archiveStream))
                    {
                        throw new InvalidDataException("Missing or invalid central directory file header");
                    }

                    headers.Add(header);

                    if (headers.Count < eocd.totalEntries && archiveStream.Position == archiveStream.Length)
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

                return headers;
            }
            finally
            {
                //if (archiveStream != null)
                //{
                //    streamContext.CloseArchiveReadStream(this.currentArchiveNumber, String.Empty, archiveStream);
                //}
            }
        }

        /// <summary>
        /// Locates and reads the end of central directory record near the
        /// end of the archive.
        /// </summary>
        private ZipEndOfCentralDirectory GetEOCD(Stream archiveStream)
        {
            BinaryReader reader = new BinaryReader(archiveStream);

            // Start searching for the Zip End of Central Directory signature from the end
            // of the file backwards (towards the beginning of the file). This allows us to
            // find Zip archives with extra data appended to the archive (for whatever reason
            // someone might want to do that).
            long offset = archiveStream.Length - ZipEndOfCentralDirectory.EOCD_RECORD_FIXEDSIZE;
            for (; offset >= 0; --offset)
            {
                archiveStream.Seek(offset, SeekOrigin.Begin);

                uint sig = reader.ReadUInt32();
                if (sig == ZipEndOfCentralDirectory.EOCDSIG)
                {
                    break;
                }
            }

            if (offset < 0)
            {
                return null;
            }

            ZipEndOfCentralDirectory eocd = new ZipEndOfCentralDirectory();
            archiveStream.Seek(offset, SeekOrigin.Begin);
            eocd.Read(archiveStream);

            if (eocd.dirOffset == (long)UInt32.MaxValue)
            {
                //string saveComment = eocd.comment;

                //archiveStream.Seek(offset - Zip64EndOfCentralDirectoryLocator.EOCDL64_SIZE, SeekOrigin.Begin);

                //Zip64EndOfCentralDirectoryLocator eocdl = new Zip64EndOfCentralDirectoryLocator();
                //if (!eocdl.Read(archiveStream))
                //{
                //    throw new InvalidDataException("Missing or invalid end of central directory record locator");
                //}

                //if (eocdl.dirStartDiskNumber == eocdl.totalDisks - 1)
                //{
                //    // ZIP64 eocd is entirely in current stream.
                //    archiveStream.Seek(eocdl.dirOffset, SeekOrigin.Begin);
                //    if (!eocd.Read(archiveStream))
                //    {
                //        throw new InvalidDataException("Missing or invalid ZIP64 end of central directory record");
                //    }
                //}
                //else
                //{
                //    // TODO: handle EOCD64 spanning archives!
                //    throw new NotImplementedException("Zip implementation does not handle end of central directory record that spans archives.");
                //}

                //eocd.comment = saveComment;
            }

            return eocd;
        }
    }
}
