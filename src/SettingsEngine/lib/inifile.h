#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

struct LEGACY_INI_FILE
{
    PERSISTED_FILE_ENCODING_TYPE fetManifestEncoding;
    PERSISTED_FILE_ENCODING_TYPE fetReadEncoding;
    LPWSTR sczNamespace;
    LPWSTR sczFullPath;
    INI_HANDLE pIniHandle;
};

HRESULT IniFileRead(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzFullPath,
    __in LEGACY_FILE_INI_INFO *pIniInfo
    );
HRESULT IniFileSetValue(
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzName,
    __in const CONFIG_VALUE *pcvValue,
    __out BOOL *pfHandled
    );
HRESULT IniFileOpen(
    __in LEGACY_FILE *pFile,
    __in LEGACY_FILE_SPECIAL *pFileSpecial,
    __in LEGACY_FILE_INI_INFO *pFileIniInfo,
    __inout LEGACY_INI_FILE *pIniFile
    );
HRESULT IniFileWrite(
    __in LEGACY_INI_FILE *pIniFile
    );
void IniFree(
    __in LEGACY_INI_FILE *pIniFile
    );

#ifdef __cplusplus
}
#endif
