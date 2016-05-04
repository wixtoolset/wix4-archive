#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

const LPCWSTR wzCfgProductId = L"WiX_Cfg";
const LPCWSTR wzCfgVersion = L"1.0.0.0";
const LPCWSTR wzCfgPublicKey = L"B77A5C561934E089";

struct LEGACY_SYNC_SESSION;

struct CFGDB_STRUCT
{
    CRITICAL_SECTION cs;
    DWORD dwLockRefCount;

    BOOL fRemote;
    // This bool controls whether ftLastModified is updated upon remote database unlock. This tracks last sync time, so should only be updated
    // if the remote will be fully synced with local upon unlock
    BOOL fUpdateLastModified;
    // For databases that release during unlock (such as remote databases), tracks the last write time
    FILETIME ftLastModified;
    // For remote databases: whether to include in auto sync (and notify UI it should be part of manual syncs by default)
    BOOL fSyncByDefault;
    // For admin databases: if the database doesn't exist and we don't have permission to create it
    // instead of failing, this flag will be set to true
    BOOL fMissing;
    BOOL fNetwork;
    LPWSTR sczOriginalDbPath; // The original path to the DB (before converting mounted drives to UNC paths)
    LPWSTR sczOriginalDbDir; // The original path to the DB (before converting mounted drives to UNC paths)
    LPWSTR sczDbPath; // The full path to the database file that was opened
    // The modified time of the remote db at the moment we copied it. Before we copy it back, we verify nobody else has changed it.
    FILETIME ftBeforeModify;
    LPWSTR sczDbCopiedPath; // The full path to the database file that was copied locally (used when remote databases are opened)
    LPWSTR sczDbDir; // The directory this DB was opened or created in
    LPWSTR sczStreamsDir; // The directory where external streams for this database should be stored
    SCE_DATABASE *psceDb;

    BOOL fProductSet;
    BOOL fProductIsLegacy;
    DWORD dwAppID;
    LPWSTR sczProductName;
    
    // The AppID for WiX_Cfg settings
    DWORD dwCfgAppID;

    // The GUID for this endpoint
    LPWSTR sczGuid;

    // Only used for remote databases
    // Respectively, the key (in string form) of the local database in the remote database's table, and vice-versa.
    // This is used to track references to a last known history value from the other database, to guarantee the common sync point
    // isn't wiped out when old settings are expired. If we didn't do this, expiring old settings would have a high likelihood
    // of creating sync conflicts that the user must resolve.
    LPWSTR sczGuidLocalInRemoteKey;
    LPWSTR sczGuidRemoteInLocalKey;

    // This defines our database schema (to instruct SceUtil how to create it)
    SCE_DATABASE_SCHEMA dsSceDb;

    // Local Db
    CFGDB_STRUCT *pcdbLocal;

    // Admin Db
    CFGDB_STRUCT *pcdbAdmin;

    // Impersonation token, used by some legacy database operations
    HANDLE hToken;

    // Background thread information
    HANDLE hBackgroundThread;
    HANDLE hBackgroundThreadWaitOnStartup;
    BOOL fBackgroundThreadWaitOnStartupTriggered;
    DWORD dwBackgroundThreadId;
    BOOL fBackgroundThreadMessageQueueInitialized;

    // User-specified callback function pointer & context
    PFN_BACKGROUNDSTATUS vpfBackgroundStatus;
    PFN_BACKGROUNDCONFLICTSFOUND vpfConflictsFound;
    LPVOID pvCallbackContext;

    CFGDB_STRUCT **rgpcdbOpenDatabases;
    DWORD cOpenDatabases;

    // Defer all stream deletes to when the database is closed - deleting a stream before database changes are committed is dangerous in case of rollback,
    // and remotes don't *really* commit until database is recopied
    LPWSTR *rgsczStreamsToDelete;
    DWORD cStreamsToDelete;
};

HRESULT HandleLock(
    __inout CFGDB_STRUCT *pcdb
    );
void HandleUnlock(
    __inout CFGDB_STRUCT *pcdb
    );
HRESULT HandleEnsureSummaryDataTable(
    __in CFGDB_STRUCT *pcdb
    );

#ifdef __cplusplus
}
#endif
