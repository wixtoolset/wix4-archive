//-------------------------------------------------------------------------------------------------
// <copyright file="drspcial.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for Cfg Legacy API (for purposes of special directory handling)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

HRESULT DirSpecialFileRead(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_FILE *pFile,
    __in_z LPCWSTR wzFullPath,
    __in_z LPCWSTR wzSubPath,
    __out BOOL *pfContinueProcessing
    );

HRESULT DirSpecialProductWrite(
    __in CFGDB_STRUCT *pcdb,
    __in_ecount(cRegKeys) const LEGACY_FILE *rglfFiles,
    __in DWORD cFiles
    );

#ifdef __cplusplus
}
#endif
