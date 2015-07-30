//-------------------------------------------------------------------------------------------------
// <copyright file="dispname.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Utilities for interacting with the PRODUCT_DISPLAY_NAME_TABLE
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseDisplayName(x) { ReleaseStr(x.sczName); }
#define ReleaseDisplayNameArray(rg, c) { if (rg) { for (DWORD i = 0; i < c; ++i) { ReleaseDisplayName(rg[i]); } } ReleaseMem(rg); }

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
