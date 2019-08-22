// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HWND s_hwndMainApp = NULL;
static BOOL s_fInitialized = FALSE;

HRESULT TrayInitialize(
    __in HWND hwndMainApp,
    __in HICON hIcon
    )
{
    HRESULT hr = S_OK;
    NOTIFYICONDATAW notifyIconData = { };

    s_hwndMainApp = hwndMainApp;

    notifyIconData.cbSize = sizeof(NOTIFYICONDATAW);
    notifyIconData.uID = 1;
    notifyIconData.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
    wcscpy_s(notifyIconData.szTip, _countof(notifyIconData.szTip), L"WiX Settings Browser");
    notifyIconData.hIcon = hIcon;
    notifyIconData.uCallbackMessage = WM_BROWSE_TRAY_ICON_MESSAGE;
    notifyIconData.hWnd = s_hwndMainApp;

    BOOL fRet = ::Shell_NotifyIconW(NIM_ADD, &notifyIconData);
    if (!fRet)
    {
        ExitWithLastError(hr, "Failed to create tray icon");
    }

    s_fInitialized = TRUE;

LExit:
    return hr;
}

HRESULT TrayUninitialize()
{
    HRESULT hr = S_OK;
    NOTIFYICONDATAW notifyIconData = { };

    if (s_fInitialized)
    {
        s_fInitialized = FALSE;
        notifyIconData.cbSize = sizeof(NOTIFYICONDATAW);
        notifyIconData.uID = 1;
        notifyIconData.hWnd = s_hwndMainApp;

        BOOL fRet = ::Shell_NotifyIconW(NIM_DELETE, &notifyIconData);
        if (!fRet)
        {
            ExitWithLastError(hr, "Failed to delete tray icon");
        }
    }

LExit:
    return hr;
}

HRESULT TrayShowBalloon(
    __in LPCWSTR wzTitle,
    __in LPCWSTR wzMessage,
    __in DWORD dwInfoFlags
    )
{
    HRESULT hr = S_OK;
    NOTIFYICONDATAW notifyIconData = { };

    notifyIconData.cbSize = sizeof(NOTIFYICONDATAW);
    notifyIconData.uID = 1;
    notifyIconData.uFlags = NIF_INFO | NIF_MESSAGE;
    wcscpy_s(notifyIconData.szInfo, _countof(notifyIconData.szInfo), wzMessage);
    wcscpy_s(notifyIconData.szInfoTitle, _countof(notifyIconData.szInfoTitle), wzTitle);
    notifyIconData.dwInfoFlags = dwInfoFlags;
    notifyIconData.uCallbackMessage = WM_BROWSE_TRAY_ICON_MESSAGE;
    notifyIconData.hWnd = s_hwndMainApp;
    notifyIconData.uTimeout = 30000;

    BOOL fRet = ::Shell_NotifyIconW(NIM_MODIFY, &notifyIconData);
    if (!fRet)
    {
        ExitWithLastError(hr, "Failed to notify tray icon to show balloon");
    }

LExit:
    return hr;
}

HRESULT TrayHideBalloon()
{
    HRESULT hr = S_OK;
    NOTIFYICONDATAW notifyIconData = { };

    notifyIconData.cbSize = sizeof(NOTIFYICONDATAW);
    notifyIconData.uID = 1;
    notifyIconData.uFlags = NIF_INFO | NIF_MESSAGE;
    notifyIconData.szInfo[0] = L'\0';
    notifyIconData.szInfoTitle[0] = L'\0';
    notifyIconData.dwInfoFlags = 0;
    notifyIconData.uCallbackMessage = WM_BROWSE_TRAY_ICON_MESSAGE;
    notifyIconData.hWnd = s_hwndMainApp;
    notifyIconData.uTimeout = 30000;

    BOOL fRet = ::Shell_NotifyIconW(NIM_MODIFY, &notifyIconData);
    if (!fRet)
    {
        ExitWithLastError(hr, "Failed to notify tray icon to hide balloon");
    }

LExit:
    return hr;
}
