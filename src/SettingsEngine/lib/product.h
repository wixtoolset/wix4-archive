#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT ProductValidateName(
    __in_z LPCWSTR wzProductName
    );
HRESULT ProductValidateVersion(
    __in_z LPCWSTR wzVersion
    );
HRESULT ProductValidatePublicKey(
    __in_z LPCWSTR wzPublicKey
    );
HRESULT ProductFindRow(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwTableIndex,
    __in_z LPCWSTR wzProductName,
    __in_z_opt LPCWSTR wzVersion,
    __in_z_opt LPCWSTR wzPublicKey,
    __out SCE_ROW_HANDLE *pSceRow
    );
HRESULT ProductSyncValues(
    __in CFGDB_STRUCT *pcdb1,
    __in CFGDB_STRUCT *pcdb2,
    __in BOOL fAllowLocalToReceiveData,
    __in STRINGDICT_HANDLE shDictValuesSeen,
    __out CONFLICT_PRODUCT **ppcpProduct
    );
HRESULT ProductEnsureCreated(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __out_opt DWORD *pdwAppID,
    __out_opt BOOL *pfLegacy
    );
HRESULT ProductSet(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __in BOOL fDontCreate,
    __out_opt BOOL *pfLegacy
    );
HRESULT ProductUnset(
    __in CFGDB_STRUCT *pcdb
    );
HRESULT ProductForget(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    );
// Gets corresponding manifest path for the given product name
HRESULT ProductGetLegacyManifestValueName(
    __in_z LPCWSTR wzProductName,
    __deref_out_z LPWSTR* psczManifestValueName
    );
// Checks if the value name is a legacy manifest path, and if it is, returns the product name
// Returns E_NOTFOUND if it is not
HRESULT ProductIsLegacyManifestValueName(
    __in_z LPCWSTR wzValueName,
    __deref_out_z LPWSTR* psczProductName
    );
HRESULT ProductIsRegistered(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __out BOOL *pfRegistered
    );
HRESULT ProductRegister(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __in BOOL fRegister
    );

#ifdef __cplusplus
}
#endif
