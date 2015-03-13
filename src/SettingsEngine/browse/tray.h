//-------------------------------------------------------------------------------------------------
// <copyright file="tray.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    System tray functionality
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

HRESULT TrayInitialize(
    __in HWND hwndMainApp,
    __in HICON hIcon
    );
HRESULT TrayUninitialize();
HRESULT TrayShowBalloon(
    __in LPCWSTR wzTitle,
    __in LPCWSTR wzMessage,
    __in DWORD dwInfoFlags
    );
HRESULT TrayHideBalloon();

#ifdef __cplusplus
}
#endif
