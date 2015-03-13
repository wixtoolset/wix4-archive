//-------------------------------------------------------------------------------------------------
// <copyright file="drdfault.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for Cfg Legacy API (for purposes of default directory fhandling)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

HRESULT DirDefaultReadFile(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzNamespace,
    __in_z LPCWSTR wzFilePath,
    __in_z LPCWSTR wzSubPath
    );
HRESULT DirDefaultWriteFile(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzName,
    __in const CONFIG_VALUE *pcvValue,
    __out BOOL *pfHandled
    );

#ifdef __cplusplus
}
#endif
