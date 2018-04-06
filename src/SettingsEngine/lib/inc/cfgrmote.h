#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT CFGAPI CfgCreateRemoteDatabase(
    __in LPCWSTR wzPath,
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle
    );
HRESULT CFGAPI CfgOpenRemoteDatabase(
    __in LPCWSTR wzPath,
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle
    );
HRESULT CFGAPI CfgRememberDatabase(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdLocalHandle,
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdRemoteHandle,
    __in LPCWSTR wzFriendlyName,
    __in BOOL fSyncByDefault
    );
HRESULT CFGAPI CfgOpenKnownRemoteDatabase(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdLocalHandle,
    __in LPCWSTR wzFriendlyName,
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle
    );
HRESULT CFGAPI CfgForgetDatabase(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdLocalHandle,
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdRemoteHandle,
    __in LPCWSTR wzFriendlyName
    );
HRESULT CFGAPI CfgRemoteDisconnect(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    );

#ifdef __cplusplus
}
#endif
