#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#pragma check_stack(on) 

#using <mscorlib.dll>

namespace ZLib
{
#define ZEXPORT __stdcall 
#include "zlib.h"
}

#include "BlockDeflateStream.h"
