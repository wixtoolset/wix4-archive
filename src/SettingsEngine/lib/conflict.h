#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
