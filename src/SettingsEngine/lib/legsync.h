//-------------------------------------------------------------------------------------------------
// <copyright file="legsync.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Settings engine functions related to syncing the legacy database
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

const LPCWSTR wzLegacyPublicKey = L"0000000000000000";
const LPCWSTR wzLegacyVersion = L"1.0.0.0";
const LPCWSTR wzLegacyDetectCacheValuePrefix = L"Reserved:\\Legacy\\Detect\\";

struct LEGACY_SYNC_PRODUCT_SESSION
{
    BOOL fRegistered;
    BOOL fNewlyRegistered;

    STRINGDICT_HANDLE shDictValuesSeen;

    STRINGDICT_HANDLE shIniFilesByNamespace;
    LEGACY_INI_FILE *rgIniFiles;
    DWORD cIniFiles;

    LEGACY_PRODUCT product;
};

struct LEGACY_SYNC_SESSION
{
    BOOL fInSceTransaction;

    ARP_PRODUCTS arpProducts;
    EXE_PRODUCTS exeProducts;

    BOOL fWriteBackToMachine;
    BOOL fDetect;

    LEGACY_SYNC_PRODUCT_SESSION syncProductSession;
};

HRESULT LegacySyncInitializeSession(
    __in BOOL fWriteBackToMachine,
    __in BOOL fDetect,
    __out LEGACY_SYNC_SESSION *pSyncSession
    );
HRESULT LegacySyncSetProduct(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_SYNC_SESSION *pSyncSession,
    __in LPCWSTR wzName
    );
HRESULT LegacyProductMachineToDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    );
HRESULT LegacySyncFinalizeProduct(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_SESSION *pSyncSession
    );
void LegacySyncUninitializeSession(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_SYNC_SESSION *pSyncSession
    );
HRESULT LegacyPull(
    __in CFGDB_STRUCT *pcdb
    );
HRESULT LegacyPullProduct(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_SYNC_SESSION * pSyncSession,
    __in SCE_ROW_HANDLE sceProductRow
    );
HRESULT LegacySyncPullDeletedValues(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    );

#ifdef __cplusplus
}
#endif
