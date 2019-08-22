#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT StreamRead(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwContentID,
    __deref_opt_out_bcount_opt(CFG_HASH_LEN) BYTE **ppbHashBuffer,
    __deref_opt_out_bcount_opt(*pcbSize) BYTE **ppbFileBuffer,
    __out_opt SIZE_T *pcbSize
    );
HRESULT StreamWrite(
    __in CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE* pbHash,
    __in_bcount_opt(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __out DWORD *pdwContentID
    );
// Takes a stream from "pcdbFrom", and duplicates it in database "pcdbTo"
// This allows better performance such as avoiding decompressing and recompressing the file
HRESULT StreamCopy(
    __in CFGDB_STRUCT *pcdbFrom,
    __in DWORD dwContentIDFrom,
    __in CFGDB_STRUCT *pcdbTo,
    __out DWORD *pdwContentIDTo
    );
HRESULT StreamIncreaseRefcount(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwContentID,
    __in DWORD dwAmount
    );
HRESULT StreamDecreaseRefcount(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwContentID,
    __in DWORD dwAmount
    );
HRESULT StreamGetFilePath(
    __deref_out_z LPWSTR *psczPath,
    __in const CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __in BOOL fCreateDirectory
    );
HRESULT StreamValidateFileMatchesHash(
    __in_bcount(cbBuffer) const BYTE *pbBuffer,
    __in SIZE_T cbBuffer,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __out BOOL *pfValid
    );

#ifdef __cplusplus
}
#endif
