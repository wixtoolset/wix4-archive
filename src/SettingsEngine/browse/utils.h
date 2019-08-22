#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

enum DATABASE_TYPE
{
    DATABASE_UNKNOWN = 0,
    DATABASE_LOCAL = 1,
    DATABASE_ADMIN = 2,
    DATABASE_REMOTE = 3
};

enum HISTORY_MODE
{
    HISTORY_UNKNOWN = 0,
    HISTORY_NORMAL = 1,
    HISTORY_LOCAL_CONFLICTS = 2,
    HISTORY_REMOTE_CONFLICTS = 3
};

struct PRODUCT
{
    LPWSTR sczName;
    LPWSTR sczVersion;
    LPWSTR sczPublicKey;
};

struct BROWSE_ENUM
{
    BOOL fRefreshing;
    HRESULT hrResult;
    CFG_ENUMERATION_HANDLE cehItems;
    DWORD cItems;
    LPCWSTR wzDisplayStatusText;
};

struct BROWSE_DATABASE
{
    // Don't need to enter this every time you're messing with the struct. If you're just modifying a field that's only meant
    // to be accessed by the same thread (such as fSyncing or hrSyncResult), DON'T enter the critical section.
    // Also don't enter the critical section if you're just reading
    // DO enter the critical section if you're modifying any member accessed by multiple threads such as a Cfg Enum
    // TODO: we really need to split this struct up into members that only UI thread uses, only browse thread uses, and multiple critical sections
    // For example, this will allow UI thread to proceed reading an enumeration when the worker thread is syncing (but not modifying that particular enum)
    CRITICAL_SECTION cs;

    BOOL fInitialized;
    BOOL fVisible; // This database does not appear in the list
    BOOL fChecked; // Stores whether it's actually checked or not in the other databases UI - this is N/A to the main local DB
    BOOL fRemember;

    CFGDB_HANDLE cdb;
    WCHAR wzLegacyManifestPath[MAX_PATH];
    LPWSTR sczPath;
    LPWSTR sczName;
    BOOL fSyncByDefault;
    LPWSTR sczStatusMessage;
    LPWSTR sczCurrentProductDisplayName;

    BOOL fReadingLegacySettings;
    HRESULT hrReadLegacySettingsResult;

    DATABASE_TYPE dtType;
    BOOL fImportingLegacyManifest;
    BOOL fForgettingProduct;

    BOOL fInitializing;
    HRESULT hrInitializeResult;

    BOOL fUninitializing;
    HRESULT hrUninitializeResult;

    BOOL fSyncing;
    HRESULT hrSyncResult;

    BOOL fResolving;
    HRESULT hrResolveResult;

    BOOL fRemembering;
    HRESULT hrRememberResult;

    BOOL fForgetting;
    HRESULT hrForgetResult;

    // Product setting functionality
    BOOL fProductSet;
    BOOL fSettingProduct;
    HRESULT hrSetProductResult;

    // Product enumeration
    BROWSE_ENUM productEnum;
    BOOL *rgfProductInstalled;
    PRODUCT prodCurrent;

    // Database enumeration
    BROWSE_ENUM dbEnum;

    // Value enumeration
    BROWSE_ENUM valueEnum;
    BOOL fNewValue;

    // Set Value Screen
    CONFIG_VALUETYPE cdSetValueType;

    // Value history information
    BROWSE_ENUM valueHistoryEnum;
    HISTORY_MODE vhmValueHistoryMode;
    LPWSTR sczValueName;

    // Conflicts
    CONFLICT_PRODUCT *pcplConflictProductList;
    DWORD dwConflictProductCount;
};

struct BROWSE_DATABASE_LIST
{
    CRITICAL_SECTION cs;

    BROWSE_DATABASE *rgDatabases;
    DWORD cDatabases;
};

// If user runs multiple browser instances with commandline args, this is how we send data to the central instance
struct COMMANDLINE_REQUEST
{
    LPWSTR *rgsczLegacyManifests;
    DWORD cLegacyManifests;

    BOOL fHelpRequested;
};

void UtilFreeDatabase(
    BROWSE_DATABASE *pDatabase
    );
HRESULT UtilGrowDatabaseList(
    __inout BROWSE_DATABASE_LIST *pbdlDatabaseList,
    __out DWORD *pdwNewIndex
    );
BOOL UtilReadyToSync(
    __in BROWSE_DATABASE *pbdDatabase
    );
void UtilWipeEnum(
    __in BROWSE_DATABASE *pDatabase,
    __inout BROWSE_ENUM *pEnum
    );

#ifdef __cplusplus
}
#endif
