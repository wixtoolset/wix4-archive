//-------------------------------------------------------------------------------------------------
// <copyright file="filter.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for Cfg Legacy API (for purposes of ignoring files / values)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

HRESULT FilterCheckValue(
    __in LEGACY_PRODUCT *pProduct,
    __in LPWSTR wzName,
    __out BOOL *pfIgnore
    );

#ifdef __cplusplus
}
#endif
