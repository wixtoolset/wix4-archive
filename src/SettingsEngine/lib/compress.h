#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT CompressToCab(
    __in_bcount(cbBuffer) const BYTE *pbBuffer,
    __in SIZE_T cbBuffer,
    __in COMPRESSION_TYPE compressionType,
    __out LPWSTR *psczPath,
    __out DWORD *pcbCompressedSize
    );
HRESULT CompressFromCab(
    __in LPCWSTR wzPath,
    __deref_out_bcount(*pcbSize) BYTE **ppbFileBuffer,
    __out SIZE_T *pcbSize
    );
HRESULT CompressWriteStream(
    __in const CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __in_bcount(cbBuffer) const BYTE *pbBuffer,
    __in SIZE_T cbBuffer,
    __out COMPRESSION_FORMAT *pcfCompressionFormat
    );
HRESULT CompressReadStream(
    __in const CFGDB_STRUCT *pcdb,
    __in_bcount(CFG_HASH_LEN) const BYTE * pbHash,
    __in COMPRESSION_FORMAT cfCompressionFormat,
    __deref_opt_out_bcount_opt(*pcbSize) BYTE **ppbFileBuffer,
    __out DWORD *pcbSize
    );

#ifdef __cplusplus
}
#endif
