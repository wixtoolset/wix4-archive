#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
