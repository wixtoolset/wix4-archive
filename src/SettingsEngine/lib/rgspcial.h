//-------------------------------------------------------------------------------------------------
// <copyright file="rgspcial.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for Cfg Legacy API (for purposes of special registry value handling)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

// When reading the registry, we do it one registry value at a time. These easily map to our structs.
HRESULT RegSpecialValueRead(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in HKEY hkKey,
    __in_z LPCWSTR wzSubKey,
    __in_z LPCWSTR wzValueName,
    __in DWORD dwValueType,
    __out BOOL *pfContinueProcessing
    );

// When writing the registry, it's a little harder - e.g. maybe values 1 & 5 map to the same registry value via "flags",
// but 2, 3, and 4 are something different.
// To handle cases like when all flags are deleted (and then we must delete the corresponding binary value),
// these writes are easiest to handle for an entire product at once
HRESULT RegSpecialsProductWrite(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    );

#ifdef __cplusplus
}
#endif
