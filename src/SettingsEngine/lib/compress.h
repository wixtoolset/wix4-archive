//-------------------------------------------------------------------------------------------------
// <copyright file="compress.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Internal utility functions for compressing / uncompressing binary streams
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
