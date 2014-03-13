#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Block deflate stream precompiled header.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma check_stack(on) 

#using <mscorlib.dll>

namespace ZLib
{
#define ZEXPORT __stdcall 
#include "zlib.h"
}

#include "BlockDeflateStream.h"
