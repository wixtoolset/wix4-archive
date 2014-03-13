//-------------------------------------------------------------------------------------------------
// <copyright file="cfgadmin.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Core settings engine API (functions for administrative purposes)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#include "cfgapi.h"

#ifdef __cplusplus
extern "C" {
#endif

HRESULT CFGAPI CfgAdminInitialize(
    __out CFGDB_HANDLE *pcdHandle,
    __in BOOL fAssumeAdmin
    );
HRESULT CFGAPI CfgAdminUninitialize(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    );

HRESULT CFGAPI CfgAdminRegisterProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    );
HRESULT CFGAPI CfgAdminUnregisterProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    );
HRESULT CFGAPI CfgAdminIsProductRegistered(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __out BOOL *pfRegistered
    );
HRESULT CFGAPI CfgAdminEnumerateProducts(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z_opt LPCWSTR wzPublicKey,
    __out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out DWORD *rgdwCount
    );

#ifdef __cplusplus
}
#endif
