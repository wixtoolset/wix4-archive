// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace WixToolset;
using namespace ZLib;

BlockDeflateStream::BlockDeflateStream(System::IO::Stream^ output, BlockDeflateCompressionLevel level)
{
    this->pzStream = new z_stream();
    int err = deflateInit2(this->pzStream, (int)level, Z_DEFLATED, -15, 8, Z_DEFAULT_STRATEGY);
    checkErr(err);

    this->output = output;
}

BlockDeflateStream::~BlockDeflateStream()
{
    this->Flush();

    delete this->pzStream;
    this->output = nullptr;
}

void BlockDeflateStream::checkErr(int err)
{
    if (err != Z_OK)
    {
        throw gcnew System::ApplicationException( "BlockDeflateStream Error" );
    }
}

int BlockDeflateStream::Deflate(array<System::Byte>^ buffer, int offset, int count)
{
    int err = Z_OK;
    int compressed = 0;

    array<System::Byte>^ outputBuffer = gcnew array<System::Byte>((count + 128 ) * 2);
    pin_ptr<System::Byte> pinInput = nullptr;
    pin_ptr<System::Byte> pinOutput = nullptr;

    // Point the zStream to the fixed input buffer at the appropriate offset.
    pinInput = &buffer[offset];
    this->pzStream->next_in = (Bytef*)pinInput;
    this->pzStream->avail_in = count - offset;

    // Point the zStream to a clean output buffer.
    System::Array::Clear(outputBuffer, 0, outputBuffer->Length);
    pinOutput = &outputBuffer[0];
    this->pzStream->next_out = (Bytef*)pinOutput;
    this->pzStream->avail_out = outputBuffer->Length;

    // Compress.
    err = deflate(this->pzStream, Z_FULL_FLUSH);
    checkErr(err);

    // Available in should be zero after doing a successful flush.
    if (this->pzStream->avail_in)
    {
        checkErr(Z_BUF_ERROR);
    }

    pinInput = nullptr;
    pinOutput = nullptr;

    // Write any available ouput into the output stream.
    compressed = outputBuffer->Length - this->pzStream->avail_out;
    this->output->Write(outputBuffer, 0, compressed);

    return compressed;
}

void BlockDeflateStream::Flush()
{
    if (this->pzStream)
    {
        int err = Z_OK;
        array<System::Byte>^ outputBuffer = gcnew array<System::Byte>(64 * 1024 + 128);
        pin_ptr<System::Byte> pinOutput = nullptr;

        // Continue until there's no more output
        while (err != Z_STREAM_END)
        {
            // Set the output buffers and make the input buffer empty.
            System::Array::Clear(outputBuffer, 0, outputBuffer->Length);
            pinOutput = &outputBuffer[0];
            this->pzStream->next_out = (Bytef*)pinOutput;
            this->pzStream->avail_out = outputBuffer->Length;

            this->pzStream->avail_in = 0;

            // Compress.
            err = deflate(this->pzStream, Z_FINISH);

            pinOutput = nullptr;

            // Do not check for error if we reach the end of the stream
            if (err != Z_STREAM_END)
            {
                checkErr(err);
            }

            // Write any available ouput into the output stream.
            int ready = outputBuffer->Length - this->pzStream->avail_out;
            output->Write(outputBuffer, 0, ready);
        }

        err = deflateEnd(this->pzStream);
        checkErr(err);
    }
}
