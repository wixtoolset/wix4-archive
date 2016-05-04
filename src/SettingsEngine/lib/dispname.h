#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseDisplayName(x) { ReleaseStr(x.sczName); }
#define ReleaseDisplayNameArray(rg, c) { if (rg) { DisplayNameArrayFree(rg, c); } }


void DisplayNameArrayFree(
    __in DISPLAY_NAME *rgDisplayNames,
    __in DWORD cDisplayNames
    );
HRESULT DisplayNameLookup(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in DWORD dwLCID,
    __out LPWSTR *psczDisplayName
    );
HRESULT DisplayNameEnumerate(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __out DISPLAY_NAME **prgDisplayNames,
    __out DWORD *pcDisplayNames
    );
HRESULT DisplayNameAny(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __out DWORD *pdwLCID,
    __out LPWSTR *psczDisplayName
    );
HRESULT DisplayNamePersist(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in DWORD dwLCID,
    __in LPCWSTR wzDisplayName
    );
HRESULT DisplayNameRemoveAllForAppID(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID
    );

#ifdef __cplusplus
}
#endif
