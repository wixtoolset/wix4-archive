//-------------------------------------------------------------------------------------------------
// <copyright file="inifile.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for Cfg Legacy API (for purposes of dealing with legacy ini/cfg files)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
