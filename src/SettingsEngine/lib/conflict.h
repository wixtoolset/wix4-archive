//-------------------------------------------------------------------------------------------------
// <copyright file="conflict.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Internal utility functions for detecting and resolving conflicts
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

HRESULT ConflictResolve(
    __in CFGDB_STRUCT *pcdb,
    __in CONFLICT_PRODUCT *cpProduct,
    __in DWORD dwValueIndex
    );
HRESULT ConflictGetList(
    __in const CFG_ENUMERATION *pcesInEnum1,
    __in DWORD dwCount1,
    __in const CFG_ENUMERATION *pcesInEnum2,
    __in DWORD dwCount2,
    __deref_out_bcount_opt(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *pcehOutHandle1,
    __out DWORD *pdwOutCount1,
    __deref_out_bcount_opt(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *pcehOutHandle2,
    __out DWORD *pdwOutCount2
    );

#ifdef __cplusplus
}
#endif
