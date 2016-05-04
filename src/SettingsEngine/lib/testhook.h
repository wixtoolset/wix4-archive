#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

typedef void (WINAPI *PFN_GETSYSTEMTIME)(
    __out SYSTEMTIME *pst
    );

HRESULT __stdcall TestHookOverrideUserDatabasePath(
    __in_z LPCWSTR wzNewUserDatabasePath
    );
HRESULT __stdcall TestHookOverrideAdminDatabasePath(
    __in_z LPCWSTR wzNewAdminDatabasePath
    );
HRESULT __stdcall TestHookOverrideArpPath(
    __in_z LPCWSTR wzNewArpPath
    );
HRESULT __stdcall TestHookOverrideApplicationsPath(
    __in_z LPCWSTR wzNewApplicationsPath
    );
HRESULT __stdcall TestHookOverrideGetSystemTime(
    __in PFN_GETSYSTEMTIME systemTimeGetter
    );

#ifdef __cplusplus
}
#endif
