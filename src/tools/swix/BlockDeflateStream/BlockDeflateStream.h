#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


namespace WixToolset
{
    public enum class BlockDeflateCompressionLevel : int
    {
        None = 0,
        Fastest = 1,
        Best = 9,
        Default = -1,
    };

    public ref class BlockDeflateStream
    {
    public:
        BlockDeflateStream(System::IO::Stream^ output, BlockDeflateCompressionLevel level);
        ~BlockDeflateStream();

        int Deflate(array<System::Byte>^ buffer, int offset, int count);
        void Flush();

    private:
        static void checkErr(int err);

    private:
        System::IO::Stream^ output;
        ZLib::z_stream* pzStream;
    };
}
