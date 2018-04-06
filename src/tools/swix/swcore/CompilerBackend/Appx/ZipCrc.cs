// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;

    internal class ZipCrc
    {
        private const uint Poly = 0x04C11DB7u;
        private static uint[] CrcTable = MakeCrcTable();

        public uint Crc { get; private set; }

        /// <summary>
        /// Updates the CRC with a range of bytes that were read or written.
        /// </summary>
        public void UpdateCrc(byte[] buffer, int offset, int count)
        {
            this.Crc = ~this.Crc;
            for (; count > 0; --count, ++offset)
            {
                this.Crc = (this.Crc >> 8) ^ ZipCrc.CrcTable[(this.Crc & 0xFF) ^ buffer[offset]];
            }
            this.Crc = ~this.Crc;
        }

        /// <summary>
        /// Computes a table that speeds up calculation of the CRC.
        /// </summary>
        private static uint[] MakeCrcTable()
        {
            uint[] crcTable = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = ZipCrc.Reflect(n, 8);
                c = c << 24;
                for (uint k = 0; k < 8; k++)
                {
                    c = (c << 1) ^ ((c & 0x80000000u) != 0 ? ZipCrc.Poly : 0);
                }

                crcTable[n] = ZipCrc.Reflect(c, 32);
            }

            return crcTable;
        }

        /// <summary>
        /// Reflects the ordering of certain number of bits. For exmample when reflecting
        /// one byte, bit one is swapped with bit eight, bit two with bit seven, etc.
        /// </summary>
        private static uint Reflect(uint value, int bits)
        {
            for (int i = 0; i < bits / 2; i++)
            {
                uint leftBit = 1u << (bits - 1 - i);
                uint rightBit = 1u << i;
                if (((value & leftBit) != 0) != ((value & rightBit) != 0))
                {
                    value ^= leftBit | rightBit;
                }
            }

            return value;
        }
    }
}
