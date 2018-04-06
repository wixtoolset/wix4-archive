#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT DatabaseListInsert(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzFriendlyName,
    __in BOOL fSyncByDefault,
    __in LPCWSTR wzPath
    );
HRESULT DatabaseListFind(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzFriendlyName,
    __out SCE_ROW_HANDLE *pSceRow
    );
HRESULT DatabaseListDelete(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzFriendlyName
    );

#ifdef __cplusplus
}
#endif
