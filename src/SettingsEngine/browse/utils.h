//-------------------------------------------------------------------------------------------------
// <copyright file="utils.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Window utility functions
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseDB(db) if (db) { UtilFreeDatabase(db); }
#define ReleaseNullDB(db) if (db) { UtilFreeDatabase(db); db = NULL; }

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
    LPCWSTR wzName;
    LPCWSTR wzVersion;
    LPCWSTR wzPublicKey;
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

    // Product enumeration
    BOOL fProductListLoading;
    HRESULT hrProductListResult;
    CFG_ENUMERATION_HANDLE cehProductList;
    BOOL *rgfProductInstalled;
    DWORD dwProductListCount;
    DWORD dwSelectedProductIndex;
    LPCWSTR wzProductListText;
    PRODUCT prodCurrent;

    // Database enumeration
    BOOL fDatabaseListLoading;
    HRESULT hrDatabaseListResult;
    CFG_ENUMERATION_HANDLE cehDatabaseList;
    DWORD dwDatabaseListCount;
    LPCWSTR wzDatabaseListText;

    // Product setting functionality
    BOOL fProductSet;
    BOOL fSettingProduct;
    HRESULT hrSetProductResult;
    DWORD dwSetProductIndex;

    // Value enumeration
    BOOL fValueListLoading;
    HRESULT hrValueListResult;
    CFG_ENUMERATION_HANDLE cehValueList;
    BOOL fNewValue;
    DWORD dwValueCount;
    LPCWSTR wzValueListText;

    // Set Value Screen
    CONFIG_VALUETYPE cdSetValueType;

    // Value history information
    HISTORY_MODE vhmValueHistoryMode;
    LPWSTR sczValueName;
    BOOL fValueHistoryLoading;
    HRESULT hrValueHistoryResult;
    CFG_ENUMERATION_HANDLE cehValueHistory;
    DWORD dwValueHistoryCount;
    LPCWSTR wzValueHistoryListText;

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

#ifdef __cplusplus
}
#endif
