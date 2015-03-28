//-------------------------------------------------------------------------------------------------
// <copyright file="guidlist.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Utilities for interacting with the DATABASE_GUID_TABLE
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

HRESULT GuidListEnsure(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzOtherGuid,
    __out LPWSTR *psczString
    );

#ifdef __cplusplus
}
#endif
