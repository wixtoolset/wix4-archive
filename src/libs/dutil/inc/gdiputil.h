//-------------------------------------------------------------------------------------------------
// <copyright file="gdiputil.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    GDI+ helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#define ExitOnGdipFailure(g, x, s, ...) { x = GdipHresultFromStatus(g); if (FAILED(x)) { Dutil_RootFailure(__FILE__, __LINE__, x); ExitTrace(x, s, __VA_ARGS__); goto LExit; } }

#ifdef __cplusplus
extern "C" {
#endif

HRESULT DAPI GdipBitmapFromResource(
    __in_opt HINSTANCE hinst,
    __in_z LPCSTR szId,
    __out Gdiplus::Bitmap **ppBitmap
    );

HRESULT DAPI GdipBitmapFromFile(
    __in_z LPCWSTR wzFileName,
    __out Gdiplus::Bitmap **ppBitmap
    );

HRESULT DAPI GdipHresultFromStatus(
    __in Gdiplus::Status gs
    );

#ifdef __cplusplus
}
#endif
