//-------------------------------------------------------------------------------------------------
// <copyright file="guidutil.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

#define GUID_STRING_LENGTH 39

HRESULT DAPI GuidFixedCreate(
    _Out_z_cap_c_(GUID_STRING_LENGTH) WCHAR* wzGuid
    );

HRESULT DAPI GuidCreate(
    __deref_out_z LPWSTR* psczGuid
    );

#ifdef __cplusplus
}
#endif
