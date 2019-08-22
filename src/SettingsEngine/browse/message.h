#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseDwordString(ds) if (ds) { ReleaseStr(ds->sczString1); ReleaseMem(ds); }
#define ReleaseNullDwordString(ds) if (ds) { ReleaseStr(ds->sczString1); ReleaseMem(ds); ds = NULL }
#define ReleaseQwordString(qs) if (qs) { ReleaseStr(qs->sczString1); ReleaseMem(qs); }
#define ReleaseNullQwordString(qs) if (qs) { ReleaseStr(qs->sczString1); ReleaseMem(qs); qs = NULL }
#define ReleaseStringPair(sp) if (sp) { ReleaseStr(sp->sczString1); ReleaseStr(sp->sczString2); ReleaseMem(sp); }
#define ReleaseNullStringPair(sp) if (sp) { ReleaseStr(sp->sczString1); ReleaseStr(sp->sczString2); ReleaseMem(sp); sp = NULL; }
#define ReleaseStringTriplet(st) if (st) { ReleaseStr(st->sczString1); ReleaseStr(st->sczString2); ReleaseStr(st->sczString3); ReleaseMem(st); }
#define ReleaseNullStringTriplet(st) if (sp) { ReleaseStr(st->sczString1); ReleaseStr(st->sczString2); ReleaseMem(st); st = NULL; }

#define ReleaseBackgroundStatusCallback(bsc) if (bsc) { ReleaseStr(bsc->sczString1); ReleaseStr(bsc->sczString2); ReleaseStr(bsc->sczString3); ReleaseMem(bsc); }
#define ReleaseNullBackgroundStatusCallback(st) if (bsc) { ReleaseStr(bsc->sczString1); ReleaseStr(bsc->sczString2); ReleaseStr(bsc->sczString3); ReleaseMem(bsc); bsc = NULL; }
#define ReleaseBackgroundConflictsFoundCallback(bcfc) if (bcfc) { CfgReleaseConflictProductArray(bcfc->rgcpProduct, bcfc->cProduct); ReleaseMem(bcfc); }
#define ReleaseNullBackgroundConflictsFoundCallback(bcfc) if (bcfc) { CfgReleaseConflictProductArray(bcfc->rgcpProduct, bcfc->cProduct); ReleaseMem(bcfc); bcfc = NULL; }

enum WM_BROWSE
{
    WM_BROWSE_RECEIVE_HWND = WM_APP + 1,
    WM_BROWSE_INITIALIZE,
    WM_BROWSE_INITIALIZE_FINISHED,
    WM_BROWSE_DISCONNECT,
    WM_BROWSE_DISCONNECT_FINISHED,
    WM_BROWSE_ENUMERATE_PRODUCTS,
    WM_BROWSE_ENUMERATE_PRODUCTS_FINISHED,
    WM_BROWSE_ENUMERATE_DATABASES,
    WM_BROWSE_ENUMERATE_DATABASES_FINISHED,
    WM_BROWSE_SET_PRODUCT,
    WM_BROWSE_SET_PRODUCT_FINISHED,
    WM_BROWSE_SET_FILE,
    WM_BROWSE_SET_FILE_FINISHED,
    WM_BROWSE_SET_DWORD,
    WM_BROWSE_SET_DWORD_FINISHED,
    WM_BROWSE_SET_QWORD,
    WM_BROWSE_SET_QWORD_FINISHED,
    WM_BROWSE_SET_STRING,
    WM_BROWSE_SET_STRING_FINISHED,
    WM_BROWSE_SET_BOOL,
    WM_BROWSE_SET_BOOL_FINISHED,
    WM_BROWSE_ENUMERATE_VALUES,
    WM_BROWSE_ENUMERATE_VALUES_FINISHED,
    WM_BROWSE_ENUMERATE_VALUE_HISTORY,
    WM_BROWSE_ENUMERATE_VALUE_HISTORY_FINISHED,
    WM_BROWSE_IMPORT_LEGACY_MANIFEST,
    WM_BROWSE_IMPORT_LEGACY_MANIFEST_FINISHED,
    WM_BROWSE_READ_LEGACY_SETTINGS,
    WM_BROWSE_READ_LEGACY_SETTINGS_FINISHED,
    WM_BROWSE_EXPORT_FILE,
    WM_BROWSE_EXPORT_FILE_FINISHED,
    WM_BROWSE_EXPORT_FILE_FROM_HISTORY,
    WM_BROWSE_EXPORT_FILE_FROM_HISTORY_FINISHED,
    WM_BROWSE_SYNC,
    WM_BROWSE_SYNC_FINISHED,
    WM_BROWSE_RESOLVE,
    WM_BROWSE_RESOLVE_FINISHED,
    WM_BROWSE_CREATE_REMOTE,
    WM_BROWSE_CREATE_REMOTE_FINISHED,
    WM_BROWSE_OPEN_REMOTE,
    WM_BROWSE_OPEN_REMOTE_FINISHED,
    WM_BROWSE_REMEMBER,
    WM_BROWSE_REMEMBER_FINISHED,
    WM_BROWSE_FORGET,
    WM_BROWSE_FORGET_FINISHED,
    WM_BROWSE_AUTOSYNCING_REMOTE,

    WM_BROWSE_TRAY_ICON_MESSAGE,
    WM_BROWSE_TRAY_ICON_EXIT,

    WM_BROWSE_AUTOSYNC_GENERAL_FAILURE,
    WM_BROWSE_AUTOSYNC_REMOTE_FAILURE,
    WM_BROWSE_AUTOSYNC_PRODUCT_FAILURE,
    WM_BROWSE_AUTOSYNC_REMOTE_GOOD,

    WM_BROWSE_BACKGROUND_STATUS_CALLBACK,
    WM_BROWSE_BACKGROUND_CONFLICTS_FOUND_CALLBACK,

    WM_BROWSE_PERSIST_SETTINGS,
    WM_BROWSE_READ_SETTINGS,
    WM_BROWSE_SETTINGS_CHANGED,
};

struct DWORD_STRING
{
    DWORD dwDword1;
    LPWSTR sczString1;
};

struct QWORD_STRING
{
    DWORD64 qwQword1;
    LPWSTR sczString1;
};

struct STRING_PAIR
{
    LPWSTR sczString1;
    LPWSTR sczString2;
};

struct STRING_TRIPLET
{
    LPWSTR sczString1;
    LPWSTR sczString2;
    LPWSTR sczString3;
};

struct BACKGROUND_STATUS_CALLBACK
{
    HRESULT hrStatus;
    BACKGROUND_STATUS_TYPE type;
    LPWSTR sczString1;
    LPWSTR sczString2;
    LPWSTR sczString3;
};

struct BACKGROUND_CONFLICTS_FOUND_CALLBACK
{
    CFGDB_HANDLE cdHandle;
    CONFLICT_PRODUCT *rgcpProduct;
    DWORD cProduct;
};

HRESULT SendDwordString(
    __in DWORD dwThreadId,
    __in DWORD dwMessageId,
    __in DWORD dwDatabaseIndex,
    __in DWORD dwDword1,
    __in_z LPCWSTR wzString1
    );
HRESULT SendQwordString(
    __in DWORD dwThreadId,
    __in DWORD dwMessageId,
    __in DWORD dwDatabaseIndex,
    __in DWORD64 qwQword1,
    __in_z LPCWSTR wzString1
    );
HRESULT SendStringPair(
    __in DWORD dwThreadId,
    __in DWORD dwMessageId,
    __in DWORD dwDatabaseIndex,
    __in_z LPCWSTR wzString1,
    __in_z LPCWSTR wzString2
    );
HRESULT SendStringTriplet(
    __in DWORD dwThreadId,
    __in DWORD dwMessageId,
    __in DWORD dwDatabaseIndex,
    __in_z LPCWSTR wzString1,
    __in_z LPCWSTR wzString2,
    __in_z LPCWSTR wzString3
    );
HRESULT SendBackgroundStatusCallback(
    __in DWORD dwThreadId,
    __in HRESULT hrStatus,
    __in BACKGROUND_STATUS_TYPE type,
    __in_z LPCWSTR wzString1,
    __in_z LPCWSTR wzString2,
    __in_z LPCWSTR wzString3
    );
HRESULT SendBackgroundConflictsFoundCallback(
    __in DWORD dwThreadId,
    __in CFGDB_HANDLE cdHandle,
    __in CONFLICT_PRODUCT *rgcpProduct,
    __in DWORD cProduct
    );

#ifdef __cplusplus
}
#endif
