//-------------------------------------------------------------------------------------------------
// <copyright file="database.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    All the details of where each database is stored, and what its format is
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

LPCWSTR DEFAULT_ARP_PATH = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
LPCWSTR DEFAULT_APPLICATIONS_PATH = L"SOFTWARE\\Classes\\Applications";

LPCWSTR wzUserDatabasePath = NULL;
LPCWSTR wzAdminDatabasePath = NULL;
LPCWSTR wzArpPath = DEFAULT_ARP_PATH;
LPCWSTR wzApplicationsPath = DEFAULT_APPLICATIONS_PATH;
LPCWSTR wzSqlCeDllPath = L".\\sqlceoledb40.dll";

#define ASSIGN_INDEX_STRUCT(a, b, c) {a.wzName = c; a.rgColumns = b; a.cColumns = countof(b);};

HRESULT DatabaseGetUserDir(
    __out LPWSTR *psczDbFileDir
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

    if (NULL != wzUserDatabasePath)
    {
        hr = PathExpand(psczDbFileDir, wzUserDatabasePath, PATH_EXPAND_ENVIRONMENT);
        ExitOnFailure(hr, "Failed to expand path to directory of user Database filename");
    }
    else
    {
        hr = PathGetKnownFolder(CSIDL_LOCAL_APPDATA, &sczPath);
        ExitOnFailure1(hr, "Failed to get known folder with ID CSIDL_LOCAL_APPDATA", CSIDL_LOCAL_APPDATA);

        hr = PathConcat(sczPath, L"Wix\\SettingsStore\\", psczDbFileDir);
        ExitOnFailure(hr, "Failed to concatenate settingsstore subpath to path");
    }

LExit:
    ReleaseStr(sczPath);

    return hr;
}

HRESULT DatabaseGetUserPath(
    __out LPWSTR *psczDbFilePath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDbFileDir = NULL;

    hr = DatabaseGetUserDir(&sczDbFileDir);
    ExitOnFailure(hr, "Failed to get directory of user Database filename");

    hr = PathConcat(sczDbFileDir, L"User.sdf", psczDbFilePath);
    ExitOnFailure(hr, "Failed to concatenate path to user Database filename");

LExit:
    ReleaseStr(sczDbFileDir);

    return hr;
}

HRESULT DatabaseSetupUserSchema(
    __in USERTABLES tableCount,
    __out SCE_DATABASE_SCHEMA *pdsSchema
    )
{
    HRESULT hr = S_OK;
    DWORD i;
    size_t cbAllocSize = 0;
    pdsSchema->cTables = tableCount;

    // Initialize table list struct
    hr = ::SizeTMult(pdsSchema->cTables, sizeof(SCE_TABLE_SCHEMA), &(cbAllocSize));
    ExitOnFailure(hr, "Maximum allocation exceeded.");

    pdsSchema->rgTables = static_cast<SCE_TABLE_SCHEMA*>(MemAlloc(cbAllocSize, TRUE));
    ExitOnNull(pdsSchema->rgTables, hr, E_OUTOFMEMORY, "Failed to allocate tables for user database schema");

    // Set table info
    pdsSchema->rgTables[SUMMARY_DATA_TABLE].wzName = L"SummaryData";
    pdsSchema->rgTables[SUMMARY_DATA_TABLE].cColumns = SUMMARY_DATA_COLUMNS;

    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].wzName = L"Product";
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].cColumns = PRODUCT_INDEX_COLUMNS;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].cIndexes = 2;

    pdsSchema->rgTables[VALUE_INDEX_TABLE].wzName = L"ValueIndex";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].cColumns = VALUE_INDEX_COLUMNS;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].cIndexes = 1;

    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].wzName = L"ValueIndexHistory";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].cColumns = VALUE_INDEX_HISTORY_COLUMNS;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].cIndexes = 2;

    pdsSchema->rgTables[BINARY_CONTENT_TABLE].wzName = L"BinaryContent";
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].cColumns = BINARY_CONTENT_COLUMNS;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].cIndexes = 2;

    if (SHARED_TABLES_NUMBER < tableCount)
    {
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].wzName = L"DatabaseList";
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].cColumns = DATABASE_INDEX_COLUMNS;
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].cIndexes = 2;
    }

    // Allocate space for columns and indexes
    for (i = 0; i < pdsSchema->cTables; ++i)
    {
        hr = ::SizeTMult(pdsSchema->rgTables[i].cColumns, sizeof(SCE_COLUMN_SCHEMA), &(cbAllocSize));
        ExitOnFailure(hr, "Maximum allocation exceeded while allocating column schema structs.");

        pdsSchema->rgTables[i].rgColumns = static_cast<SCE_COLUMN_SCHEMA*>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pdsSchema->rgTables[i].rgColumns, hr, E_OUTOFMEMORY, "Failed to allocate columns for user database schema");

        if (0 < pdsSchema->rgTables[i].cIndexes)
        {
            hr = ::SizeTMult(pdsSchema->rgTables[i].cIndexes, sizeof(SCE_INDEX_SCHEMA), &(cbAllocSize));
            ExitOnFailure(hr, "Maximum allocation exceeded while allocating index schema structs.");

            pdsSchema->rgTables[i].rgIndexes = static_cast<SCE_INDEX_SCHEMA*>(MemAlloc(cbAllocSize, TRUE));
            ExitOnNull(pdsSchema->rgTables[i].rgIndexes, hr, E_OUTOFMEMORY, "Failed to allocate indexes for user database schema");
        }
    }

    // Set column info
    pdsSchema->rgTables[SUMMARY_DATA_TABLE].rgColumns[SUMMARY_GUID].wzName = L"Guid";
    pdsSchema->rgTables[SUMMARY_DATA_TABLE].rgColumns[SUMMARY_GUID].dbtColumnType = DBTYPE_WSTR;

    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_ID].wzName = L"ID";
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_ID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_ID].fAutoIncrement = TRUE;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_ID].fPrimaryKey = TRUE;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_NAME].wzName = L"Name";
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_NAME].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_VERSION].wzName = L"Version";
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_VERSION].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_VERSION].dwLength = 24;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_PUBLICKEY].wzName = L"PublicKey";
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_PUBLICKEY].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_PUBLICKEY].dwLength = 20;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_REGISTERED].wzName = L"Installed";
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_REGISTERED].dbtColumnType = DBTYPE_BOOL;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_IS_LEGACY].wzName = L"IsLegacy";
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_IS_LEGACY].dbtColumnType = DBTYPE_BOOL;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_LEGACY_SEQUENCE].wzName = L"Sequence";
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_LEGACY_SEQUENCE].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgColumns[PRODUCT_LEGACY_SEQUENCE].fNullable = TRUE;

    static DWORD rgdwUserProductIndex1[] = { PRODUCT_ID };
    static DWORD rgdwUserProductIndex2[] = { PRODUCT_NAME, PRODUCT_VERSION, PRODUCT_PUBLICKEY };
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgIndexes[0], rgdwUserProductIndex1, L"PrimaryKey");
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[PRODUCT_INDEX_TABLE].rgIndexes[1], rgdwUserProductIndex2, L"Name_Version_PublicKey");

    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_ID].wzName = L"ID";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_ID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_ID].fAutoIncrement = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_ID].fPrimaryKey = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_APPID].wzName = L"AppID";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_APPID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_APPID].wzRelationName = L"AppID";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_APPID].dwForeignKeyTable = PRODUCT_INDEX_TABLE;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_APPID].dwForeignKeyColumn = PRODUCT_ID;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_NAME].wzName = L"Name";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_NAME].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_TYPE].wzName = L"ValueType";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_TYPE].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBSIZE].wzName = L"BlobSize";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBSIZE].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBSIZE].fNullable = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBHASH].wzName = L"BlobHash";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBHASH].dbtColumnType = DBTYPE_BYTES;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBHASH].fNullable = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBHASH].dwLength = CFG_HASH_LEN;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].wzName = L"BlobContentID";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].fNullable = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].wzRelationName = L"ValueBlobContentID";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].dwForeignKeyTable = BINARY_CONTENT_TABLE;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].dwForeignKeyColumn = BINARY_ID;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_STRINGVALUE].wzName = L"StringValue";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_STRINGVALUE].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_STRINGVALUE].dwLength = 1024 * 1024;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_LONGVALUE].wzName = L"LongValue";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_LONGVALUE].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_LONGLONGVALUE].wzName = L"LongLongValue";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_LONGLONGVALUE].dbtColumnType = DBTYPE_I8;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BOOLVALUE].wzName = L"BoolValue";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BOOLVALUE].dbtColumnType = DBTYPE_BOOL;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_WHEN].wzName = L"When";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_WHEN].dbtColumnType = DBTYPE_DBTIMESTAMP;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BY].wzName = L"By";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_COMMON_BY].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_LAST_HISTORY_ID].wzName = L"LastHistoryID";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_LAST_HISTORY_ID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_LAST_HISTORY_ID].wzRelationName = L"LastHistoryID";
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_LAST_HISTORY_ID].dwForeignKeyTable = VALUE_INDEX_HISTORY_TABLE;
    pdsSchema->rgTables[VALUE_INDEX_TABLE].rgColumns[VALUE_LAST_HISTORY_ID].dwForeignKeyColumn = VALUE_COMMON_ID;

    static DWORD rgdwUserValueIndex1[] = { VALUE_COMMON_APPID, VALUE_COMMON_NAME };
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[VALUE_INDEX_TABLE].rgIndexes[0], rgdwUserValueIndex1, L"AppID_Name");

    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_ID].wzName = L"ID";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_ID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_ID].fAutoIncrement = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_ID].fPrimaryKey = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_APPID].wzName = L"AppID";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_APPID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_APPID].wzRelationName = L"AppID";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_APPID].dwForeignKeyTable = PRODUCT_INDEX_TABLE;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_APPID].dwForeignKeyColumn = PRODUCT_ID;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_NAME].wzName = L"Name";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_NAME].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_TYPE].wzName = L"ValueType";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_TYPE].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBSIZE].wzName = L"BlobSize";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBSIZE].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBSIZE].fNullable = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBHASH].wzName = L"BlobHash";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBHASH].dbtColumnType = DBTYPE_BYTES;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBHASH].fNullable = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBHASH].dwLength = CFG_HASH_LEN;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].wzName = L"BlobContentID";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].fNullable = TRUE;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].wzRelationName = L"ValueHistoryBlobContentID";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].dwForeignKeyTable = BINARY_CONTENT_TABLE;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BLOBCONTENTID].dwForeignKeyColumn = BINARY_ID;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_STRINGVALUE].wzName = L"StringValue";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_STRINGVALUE].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_STRINGVALUE].dwLength = 1024 * 1024;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_LONGVALUE].wzName = L"LongValue";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_LONGVALUE].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_LONGLONGVALUE].wzName = L"LongLongValue";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_LONGLONGVALUE].dbtColumnType = DBTYPE_I8;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BOOLVALUE].wzName = L"BoolValue";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BOOLVALUE].dbtColumnType = DBTYPE_BOOL;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_WHEN].wzName = L"When";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_WHEN].dbtColumnType = DBTYPE_DBTIMESTAMP;
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BY].wzName = L"By";
    pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgColumns[VALUE_COMMON_BY].dbtColumnType = DBTYPE_WSTR;

    static DWORD rgdwUserValueHistoryIndex1[] = { VALUE_COMMON_ID };
    static DWORD rgdwUserValueHistoryIndex2[] = { VALUE_COMMON_APPID, VALUE_COMMON_NAME, VALUE_COMMON_WHEN };
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgIndexes[0], rgdwUserValueHistoryIndex1, L"PrimaryKey");
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[VALUE_INDEX_HISTORY_TABLE].rgIndexes[1], rgdwUserValueHistoryIndex2, L"AppID_Name_When");

    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_ID].wzName = L"ID";
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_ID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_ID].fPrimaryKey = TRUE;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_ID].fAutoIncrement = TRUE;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_REFCOUNT].wzName = L"RefCount";
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_REFCOUNT].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_DELTA_FROM_ID].wzName = L"DeltaFromID";
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_DELTA_FROM_ID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_DELTA_FROM_ID].fNullable = TRUE;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_DELTA_FROM_ID].dwForeignKeyTable = BINARY_CONTENT_TABLE;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_DELTA_FROM_ID].dwForeignKeyColumn = BINARY_ID;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_COMPRESSION].wzName = L"Compression";
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_COMPRESSION].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_RAW_SIZE].wzName = L"BinaryRawSize";
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_RAW_SIZE].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_HASH].wzName = L"Hash";
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_HASH].dbtColumnType = DBTYPE_BYTES;
    pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgColumns[BINARY_HASH].dwLength = CFG_HASH_LEN;

    static DWORD rgdwUserBinaryContentIndex1[] = { BINARY_ID };
    static DWORD rgdwUserBinaryContentIndex2[] = { BINARY_HASH };
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgIndexes[0], rgdwUserBinaryContentIndex1, L"PrimaryKey");
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[BINARY_CONTENT_TABLE].rgIndexes[1], rgdwUserBinaryContentIndex2, L"Hash");

    if (SHARED_TABLES_NUMBER < tableCount)
    {
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_ID].wzName = L"ID";
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_ID].dbtColumnType = DBTYPE_I4;
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_ID].fPrimaryKey = TRUE;
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_ID].fAutoIncrement = TRUE;
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_FRIENDLY_NAME].wzName = L"FriendlyName";
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_FRIENDLY_NAME].dbtColumnType = DBTYPE_WSTR;
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_SYNC_BY_DEFAULT].wzName = L"SyncByDefault";
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_SYNC_BY_DEFAULT].dbtColumnType = DBTYPE_BOOL;
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_PATH].wzName = L"Path";
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_PATH].dbtColumnType = DBTYPE_WSTR;
        pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgColumns[DATABASE_INDEX_PATH].fNullable = TRUE;

        static DWORD rgdwUserDatabaseIndex1[] = { DATABASE_INDEX_ID };
        static DWORD rgdwUserDatabaseIndex2[] = { DATABASE_INDEX_FRIENDLY_NAME };
        ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgIndexes[0], rgdwUserDatabaseIndex1, L"PrimaryKey");
        ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[DATABASE_INDEX_TABLE].rgIndexes[1], rgdwUserDatabaseIndex2, L"FriendlyName");
    }

LExit:
    return hr;
}

HRESULT DatabaseGetAdminDir(
    __out LPWSTR *psczDbFileDir
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

    if (NULL != wzAdminDatabasePath)
    {
        hr = PathExpand(psczDbFileDir, wzAdminDatabasePath, PATH_EXPAND_ENVIRONMENT);
        ExitOnFailure(hr, "Failed to expand path to directory of admin Database filename");
    }
    else
    {
        hr = PathGetKnownFolder(CSIDL_WINDOWS, &sczPath);
        ExitOnFailure1(hr, "Failed to get known folder with ID CSIDL_WINDOWS", CSIDL_WINDOWS);

        hr = PathConcat(sczPath, L"Wix\\SettingsStore\\", psczDbFileDir);
        ExitOnFailure(hr, "Failed to concatenate settingsstore subpath to path");
    }

LExit:
    ReleaseStr(sczPath);

    return hr;
}

HRESULT DatabaseGetAdminPath(__out LPWSTR *psczDbFilePath)
{
    HRESULT hr = S_OK;
    LPWSTR sczDbFileDir = NULL;

    hr = DatabaseGetAdminDir(&sczDbFileDir);
    ExitOnFailure(hr, "Failed to get directory of admin Database filename");

    hr = PathConcat(sczDbFileDir, L"Admin.sdf", psczDbFilePath);
    ExitOnFailure(hr, "Failed to concatenate path to admin Database filename");

LExit:
    ReleaseStr(sczDbFileDir);

    return hr;
}

HRESULT DatabaseSetupAdminSchema(
    __out SCE_DATABASE_SCHEMA *pdsSchema
    )
{
    HRESULT hr = S_OK;
    DWORD i;
    size_t cbAllocSize = 0;
    pdsSchema->cTables = ADMIN_TABLES_NUMBER;

    // Initialize table list struct
    hr = ::SizeTMult(pdsSchema->cTables, sizeof(SCE_TABLE_SCHEMA), &(cbAllocSize));
    ExitOnFailure(hr, "Maximum allocation exceeded.");

    pdsSchema->rgTables = static_cast<SCE_TABLE_SCHEMA*>(MemAlloc(cbAllocSize, TRUE));
    ExitOnNull(pdsSchema->rgTables, hr, E_OUTOFMEMORY, "Failed to allocate tables for admin database schema");

    // Set actual table info - this is the 1st interesting bit
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].wzName = L"AdminProduct";
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].cColumns = ADMIN_PRODUCT_INDEX_COLUMNS;
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].cIndexes = 2;

    // Allocate 
    for (i = 0; i < pdsSchema->cTables; ++i)
    {
        hr = ::SizeTMult(pdsSchema->rgTables[i].cColumns, sizeof(SCE_COLUMN_SCHEMA), &(cbAllocSize));
        ExitOnFailure(hr, "Maximum allocation exceeded.");

        pdsSchema->rgTables[i].rgColumns = static_cast<SCE_COLUMN_SCHEMA*>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pdsSchema->rgTables, hr, E_OUTOFMEMORY, "Failed to allocate columns for admin database schema");

        if (0 < pdsSchema->rgTables[i].cIndexes)
        {
            hr = ::SizeTMult(pdsSchema->rgTables[i].cIndexes, sizeof(SCE_INDEX_SCHEMA), &(cbAllocSize));
            ExitOnFailure(hr, "Maximum allocation exceeded while allocating index schema structs.");

            pdsSchema->rgTables[i].rgIndexes = static_cast<SCE_INDEX_SCHEMA*>(MemAlloc(cbAllocSize, TRUE));
            ExitOnNull(pdsSchema->rgTables[i].rgIndexes, hr, E_OUTOFMEMORY, "Failed to allocate indexes for admin database schema");
        }
    }

    // Set actual column info - this is the other interesting bit
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_ID].wzName = L"ID";
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_ID].dbtColumnType = DBTYPE_I4;
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_ID].fAutoIncrement = TRUE;
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_ID].fPrimaryKey = TRUE;
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_NAME].wzName = L"Name";
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_NAME].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_VERSION].wzName = L"Version";
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_VERSION].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_VERSION].dwLength = 24;
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_PUBLICKEY].wzName = L"PublicKey";
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_PUBLICKEY].dbtColumnType = DBTYPE_WSTR;
    pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgColumns[ADMIN_PRODUCT_PUBLICKEY].dwLength = 20;

    static DWORD rgdwAdminProductIndex1[] = { ADMIN_PRODUCT_ID };
    static DWORD rgdwAdminProductIndex2[] = { ADMIN_PRODUCT_NAME, ADMIN_PRODUCT_VERSION, ADMIN_PRODUCT_PUBLICKEY };
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgIndexes[0], rgdwAdminProductIndex1, L"PrimaryKey");
    ASSIGN_INDEX_STRUCT(pdsSchema->rgTables[ADMIN_PRODUCT_INDEX_TABLE].rgIndexes[1], rgdwAdminProductIndex2, L"Name_Version_PublicKey");

LExit:
    return hr;
}


void DatabaseReleaseSceSchema(
    __in SCE_DATABASE_SCHEMA *pdsSchema
    )
{
    DWORD dwTable;
    DWORD dwColumn;

    for (dwTable = 0; dwTable < pdsSchema->cTables; ++dwTable)
    {
        // Don't release this, it should be a constant string
        //ReleaseStr(pdsSchema->rgTables[dwTable].wzName);

        for (dwColumn = 0; dwColumn < pdsSchema->rgTables[dwTable].cColumns; ++dwColumn)
        {
            // Don't release this either, it should be a constant string
            //ReleaseStr(pdsSchema->rgTables[dwTable].pcsColumns[dwColumn].wzName);
        }

        ReleaseNullMem(pdsSchema->rgTables[dwTable].rgColumns);
        ReleaseNullMem(pdsSchema->rgTables[dwTable].rgIndexes);
    }

    ReleaseMem(pdsSchema->rgTables);

    return;
}

