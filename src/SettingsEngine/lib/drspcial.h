#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
