//-------------------------------------------------------------------------------------------------
// <copyright file="dblist.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Internal Utility Functions for dealing with the remembered list of databases
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
