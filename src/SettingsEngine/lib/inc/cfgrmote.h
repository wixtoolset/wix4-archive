//-------------------------------------------------------------------------------------------------
// <copyright file="cfgrmote.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Settings engine API (functions for connecting to non-local databases)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

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
