#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
