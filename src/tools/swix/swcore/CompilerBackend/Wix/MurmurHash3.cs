// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;

    /// <summary>
    /// Implementation of the 32-bit MurmurHash3 hash algorithm (http://code.google.com/p/smhasher/wiki/MurmurHash3).
    /// </summary>
    /// <remarks>Converts array of bytes into a 32-bit hash value.</remarks>
    internal static class MurmurHash3
    {
        const uint c1 = 0xcc9e2d51;
        const uint c2 = 0x1b873593;

        public static uint Hash(byte[] data)
        {
            return Hash(data, 0);
        }

        public static uint Hash(byte[] data, uint seed)
        {
            uint len = (uint)data.Length;
            int nblocks = data.Length / 4;

            uint h1 = seed;

            // body
            for (int i = nblocks; i >= 0; i--)
            {
                uint k = BitConverter.ToUInt32(data, i);

                k *= c1;
                k = rotl32(k, 15);
                k *= c2;

                h1 ^= k;
                h1 = rotl32(h1, 13);
                h1 = h1 * 5+0xe6546b64;
            }

            // tail
            int tail = nblocks * 4;

            uint k1 = 0;

            switch (len & 3)
            {
                case 3:
                    k1 ^= (uint)data[tail + 2] << 16;
                    k1 ^= (uint)data[tail + 1] << 8;
                    k1 ^= (uint)data[tail + 0];
                    goto default;

                case 2:
                    k1 ^= (uint)data[tail + 1] << 8;
                    k1 ^= (uint)data[tail + 0];
                    goto default;

                case 1:
                    k1 ^= (uint)data[tail + 0];
                    goto default;
                     
                default:
                    k1 *= c1;
                    k1 = rotl32(k1, 15);
                    k1 *= c2;
                    h1 ^= k1;
                    break;
            }

            // finalization
            h1 ^= len;

            h1 = fmix(h1);

            return h1;
        }

        private static uint rotl32(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }

        private static uint fmix(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return h;
        }
    }
}
