//-------------------------------------------------------------------------------------------------
// <copyright file="BlockDeflateStream.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Block deflate stream header.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

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
