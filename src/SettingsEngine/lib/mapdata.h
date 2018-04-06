#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

const WCHAR NAMESPACE_DELIMITER_CHARACTER = L':';

HRESULT MapGetNamespace(
    __in_z LPCWSTR wzFullName,
    __out_z LPWSTR *psczNamespace,
    __out_z LPWSTR *psczValue
    );
HRESULT MapFileToCfgName(
    __in_z LPCWSTR wzName,
    __in_z LPCWSTR wzSubPath,
    __out_z LPWSTR *psczOutput
    );
HRESULT MapRegValueToCfgName(
    __in_z LPCWSTR wzNamespace,
    __in_z LPCWSTR wzKey,
    __in_z LPCWSTR wzRawValueName,
    __out_z LPWSTR *psczOutput
    );
HRESULT MapRegSpecialToCfgValueName(
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __out_z LPWSTR *psczOutput
    );
HRESULT MapCfgNameToFile(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzCfgName,
    __out_z LPWSTR *psczOutput
    );
HRESULT MapCfgNameToRegValue(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzCfgName,
    __out DWORD *pdwRoot,
    __out LPWSTR *psczKey,
    __out LPWSTR *psczValueName
    );

#ifdef __cplusplus
}
#endif
