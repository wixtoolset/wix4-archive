#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
