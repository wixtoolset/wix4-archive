#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

extern LPCWSTR DEFAULT_USER_DATABASE_PATH;
extern LPCWSTR DEFAULT_ADMIN_DATABASE_PATH;
extern LPCWSTR DEFAULT_ARP_PATH;
extern LPCWSTR DEFAULT_APPLICATIONS_PATH;

extern LPCWSTR wzUserDatabasePath;
extern LPCWSTR wzAdminDatabasePath;
extern LPCWSTR wzArpPath;
extern LPCWSTR wzApplicationsPath;
extern LPCWSTR wzSqlCeDllPath;

enum DATABASE_TYPE
{
    DATABASE_TYPE_LOCAL,
    DATABASE_TYPE_REMOTE
};

enum USERTABLES
{
    SUMMARY_DATA_TABLE = 0, // Only has 1 row - stores information global to this user's database
    PRODUCT_INDEX_TABLE = 1, // Associates a particular product name, version and public key with an ID number. This number is guaranteed unique within one user's DB, but isn't necessarily the same in another user's DB.
    PRODUCT_DISPLAY_NAME_TABLE = 2, // Associates a particular AppID and LCID combination with a displayable string.
    VALUE_INDEX_TABLE = 3, // Stores user data
    VALUE_INDEX_HISTORY_TABLE = 4, // Stores user data history
    BINARY_CONTENT_TABLE = 5, // Stores user blobs
    DATABASE_GUID_LIST_TABLE = 6, // Associates each database GUID with a unique ID

    // User-specific tables
    DATABASE_INDEX_TABLE = 7, // Remembers databases you may want to connect to
    USER_TABLES_NUMBER = 8, // not an actual table, just represents the number of tables

    // Remote-specific tables
    REMOTE_TABLES_NUMBER = 7 // not an actual table, just represents the number of tables
};

// User column enums
enum PRODUCT_INDEX_COLUMN
{
    PRODUCT_ID = 0,
    PRODUCT_NAME = 1,
    PRODUCT_VERSION = 2,
    PRODUCT_PUBLICKEY = 3,
    PRODUCT_REGISTERED = 4,
    PRODUCT_IS_LEGACY = 5,
    PRODUCT_INDEX_COLUMNS = 6
};

enum PRODUCT_DISPLAY_NAME_COLUMN
{
    PRODUCT_DISPLAY_NAME_ID = 0,
    PRODUCT_DISPLAY_NAME_APPID = 1,
    PRODUCT_DISPLAY_NAME_LCID = 2,
    PRODUCT_DISPLAY_NAME_NAME = 3,
    PRODUCT_DISPLAY_NAME_COLUMNS = 4
};

// Columns used by both value index and value index history tables
enum VALUE_COMMON_COLUMN
{
    VALUE_COMMON_ID = 0,
    VALUE_COMMON_APPID = 1,
    VALUE_COMMON_NAME = 2,
    VALUE_COMMON_TYPE = 3,
    VALUE_COMMON_BLOBSIZE = 4,
    VALUE_COMMON_BLOBHASH = 5,
    VALUE_COMMON_BLOBCONTENTID = 6,
    VALUE_COMMON_STRINGVALUE = 7,
    VALUE_COMMON_LONGVALUE = 8,
    VALUE_COMMON_LONGLONGVALUE = 9,
    VALUE_COMMON_BOOLVALUE = 10,
    VALUE_COMMON_WHEN = 11,
    VALUE_COMMON_BY = 12,
    VALUE_COMMON_COLUMNS = 13
};

// Must start after VALUE_COMMON_COLUMN, these are the value index specific columns
enum VALUE_INDEX_COLUMN
{
    VALUE_LAST_HISTORY_ID = 13,

    VALUE_INDEX_COLUMNS = 14
};

// Must start after VALUE_COMMON_COLUMN, these are the value history index specific columns
enum VALUE_INDEX_HISTORY_COLUMN
{
    VALUE_HISTORY_DB_REFERENCES = 13,

    VALUE_INDEX_HISTORY_COLUMNS = 14
};

enum COMPRESSION_FORMAT
{
    COMPRESSION_NONE = 0,
    COMPRESSION_CAB = 1,
};

enum BINARY_CONTENT_COLUMN
{
    BINARY_ID = 0,
    BINARY_REFCOUNT = 1,
    BINARY_DELTA_FROM_ID = 2,
    BINARY_COMPRESSION = 3,
    BINARY_RAW_SIZE = 4,
    BINARY_HASH = 5,
    BINARY_CONTENT_COLUMNS = 6
};

enum SUMMARY_DATA_COLUMN
{
    SUMMARY_GUID = 0,
    SUMMARY_DATA_COLUMNS = 1
};

enum DATABASE_INDEX_COLUMN
{
    DATABASE_INDEX_ID = 0,
    DATABASE_INDEX_FRIENDLY_NAME = 1,
    DATABASE_INDEX_SYNC_BY_DEFAULT = 2,
    DATABASE_INDEX_PATH = 3,
    DATABASE_INDEX_COLUMNS = 4
};

enum DATABASE_GUID_LIST_COLUMN
{
    DATABASE_GUID_LIST_ID = 0,
    DATABASE_GUID_LIST_STRING = 1,
    DATABASE_GUID_LIST_COLUMNS = 2,
};

enum ADMINTABLES
{
    ADMIN_PRODUCT_INDEX_TABLE = 0, // Associates a particular product name, version and public key with an ID number. This number is guaranteed unique within one DB, but isn't necessarily the same in another DB.
    ADMIN_PRODUCT_DISPLAY_NAME_TABLE = 1, // Associates a particular product name, version and public key with an ID number. This number is guaranteed unique within one DB, but isn't necessarily the same in another DB.
    ADMIN_TABLES_NUMBER = 2 // not an actual table, just represents the number of tables
};

HRESULT DatabaseGetUserDir(
    __out LPWSTR *psczDbFileDir
    );
HRESULT DatabaseGetUserPath(
    __out LPWSTR *psczDbFilePath
    );
HRESULT DatabaseSetupSchema(
    __in DATABASE_TYPE dbType,
    __out SCE_DATABASE_SCHEMA *pdsSceSchema
    );
HRESULT DatabaseGetAdminDir(
    __out LPWSTR *psczDbFileDir
    );
HRESULT DatabaseGetAdminPath(
    __out LPWSTR *psczDbFilePath
    );
HRESULT DatabaseSetupAdminSchema(
    __out SCE_DATABASE_SCHEMA *pdsSchema
    );
void DatabaseReleaseSceSchema(
    __in SCE_DATABASE_SCHEMA *pdsSchema
    );

#ifdef __cplusplus
}
#endif
