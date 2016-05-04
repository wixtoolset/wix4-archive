#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

// When reading the registry, we do it one registry value at a time. These easily map to our structs.
HRESULT RegDefaultReadValue(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzNamespace,
    __in HKEY hkKey,
    __in_z LPCWSTR wzRegKey,
    __in_z LPCWSTR wzValueName,
    __in DWORD dwRegType
    );
HRESULT RegDefaultWriteValue(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzName,
    __in const CONFIG_VALUE *pcvValue,
    __out BOOL *pfHandled
    );

#ifdef __cplusplus
}
#endif
