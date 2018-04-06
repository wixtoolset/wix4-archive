#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define CFGAPI __stdcall
#define CFGAPIV __cdecl // used only for functions taking variable length arguments

extern const int CFGDB_HANDLE_BYTES;
extern const int CFG_ENUMERATION_HANDLE_BYTES;

typedef void* CFGDB_HANDLE;
typedef const void* C_CFGDB_HANDLE;

typedef void* CFG_ENUMERATION_HANDLE;
typedef const void* C_CFG_ENUMERATION_HANDLE;

#define CFG_HASH_LEN SHA1_HASH_LEN

enum CONFIG_VALUETYPE
{
    VALUE_INVALID = 0, // Initialize variables to this invalid state
    VALUE_DELETED = 0x1,
    VALUE_BLOB = 0x2,
    VALUE_STRING = 0x4,
    VALUE_DWORD = 0x8,
    VALUE_QWORD = 0x10,
    VALUE_BOOL = 0x20,

    // Excludes 'deleted' types
    VALUE_ANY_TYPE = 0x3E,
    VALUE_ANY_BUT_BLOB = 0x3C
};

enum CFG_ENUM_DATA
{
    ENUM_DATA_INVALID = 0, // Initialize variables to this invalid state
    ENUM_DATA_COUNT,
    ENUM_DATA_VALUENAME,
    ENUM_DATA_VALUETYPE,
    ENUM_DATA_BLOBCONTENT,
    ENUM_DATA_BLOBHASH,
    ENUM_DATA_BLOBSIZE,
    ENUM_DATA_VALUESTRING,
    ENUM_DATA_VALUEDWORD,
    ENUM_DATA_VALUEQWORD,
    ENUM_DATA_VALUEBOOL,
    ENUM_DATA_PRODUCTNAME,
    ENUM_DATA_VERSION,
    ENUM_DATA_PUBLICKEY,
    ENUM_DATA_REGISTERED,
    ENUM_DATA_WHEN,
    ENUM_DATA_BY,
    ENUM_DATA_FRIENDLY_NAME,
    ENUM_DATA_SYNC_BY_DEFAULT,
    ENUM_DATA_DATABASE_REFERENCES,
    ENUM_DATA_PATH
};

enum RESOLUTION_CHOICE
{
    RESOLUTION_UNRESOLVED = 0,
    RESOLUTION_LOCAL,
    RESOLUTION_REMOTE
};

struct DISPLAY_NAME
{
    LPWSTR sczName;
    DWORD dwLCID;
};

struct CONFLICT_PRODUCT
{
    LPWSTR sczProductName;
    LPWSTR sczVersion;
    LPWSTR sczPublicKey;

    DISPLAY_NAME *rgDisplayNames;
    DWORD cDisplayNames;

    // An enumeration just like what is returned from CfgEnumPastValues, but only contains conflicting values in local store
    CFG_ENUMERATION_HANDLE *rgcesValueEnumLocal;
    DWORD *rgdwValueCountLocal;

    // An enumeration just like what is returned from CfgEnumPastValues, but only contains conflicting values in remote store
    CFG_ENUMERATION_HANDLE *rgcesValueEnumRemote;
    DWORD *rgdwValueCountRemote;

    RESOLUTION_CHOICE *rgrcValueChoices;

    // This is the number of conflicting values for the product. This is also the array size for both rgcesValueEnumLocal, rgcesValueEnumRemote, and rgrcValueChoices
    DWORD cValues;
};

enum BACKGROUND_STATUS_TYPE
{
    BACKGROUND_STATUS_INVALID = 0,
    BACKGROUND_STATUS_AUTOSYNC_RUNNING,
    BACKGROUND_STATUS_GENERAL_ERROR,
    BACKGROUND_STATUS_REMOTE_ERROR,
    BACKGROUND_STATUS_PRODUCT_ERROR,
    BACKGROUND_STATUS_REMOTE_GOOD,
    BACKGROUND_STATUS_SYNCING_PRODUCT,
    BACKGROUND_STATUS_SYNC_PRODUCT_FINISHED,
    BACKGROUND_STATUS_REDETECTING_PRODUCTS,
    BACKGROUND_STATUS_REDETECT_PRODUCTS_FINISHED,
    BACKGROUND_STATUS_SYNCING_REMOTE,
    BACKGROUND_STATUS_SYNC_REMOTE_FINISHED
};

// Callback from background thread
// hr represents error code if it's an error notification
// wzString1, wzString2, and wzString3 tell which product (ID, version, public key), if the message is about a specific product
// wzString1 tells the name of the remote database, if the message is about a specific remote database
// pvContext is the pvContext passed in on CfgInitialize
typedef void (*PFN_BACKGROUNDSTATUS)(
    __in HRESULT hr,
    __in BACKGROUND_STATUS_TYPE type,
    __in_z_opt LPCWSTR wzString1,
    __in_z_opt LPCWSTR wzString2,
    __in_z_opt LPCWSTR wzString3,
    __in_opt LPVOID pvContext
    );
// If conflicts are found while syncing from the background thread, this callback is used to notify
typedef void (*PFN_BACKGROUNDCONFLICTSFOUND)(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_ecount(cProduct) CONFLICT_PRODUCT *rgcpProduct,
    __in DWORD cProduct,
    __in_opt LPVOID pvContext
    );

HRESULT CFGAPI CfgInitialize(
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle,
    __in_opt PFN_BACKGROUNDSTATUS vpfBackgroundStatus,
    __in_opt PFN_BACKGROUNDCONFLICTSFOUND vpfConflictsFound,
    __in_opt LPVOID pvCallbackContext
    );
HRESULT CFGAPI CfgUninitialize(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    );

// The background thread will hold off doing work until UI is ready for it to begin
HRESULT CFGAPI CfgResumeBackgroundThread(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    );

HRESULT CFGAPI CfgGetEndpointGuid(
    __in_bcount(CFGDB_HANDLE_BYTES) C_CFGDB_HANDLE cdHandle,
    __out_z LPWSTR *psczGuid
    );

// Generally this is the first call after CfgInitialize() an app will make - it tells the settings engine your application's unique identifier,
// and is roughly equivalent to setting the namespace for all your configuration data which will be stored
HRESULT CFGAPI CfgSetProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    );

HRESULT CFGAPI CfgGetType(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out CONFIG_VALUETYPE *pcvType
    );

// Get / set DWORD values
HRESULT CFGAPI CfgSetDword(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in DWORD dwValue
    );
HRESULT CFGAPI CfgGetDword(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out DWORD *pdwValue
    );

// Get / set QWORD values
HRESULT CFGAPI CfgSetQword(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in DWORD64 qwValue
    );
HRESULT CFGAPI CfgGetQword(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out DWORD64 *pqwValue
    );

// Get / set string values
HRESULT CFGAPI CfgSetString(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in_z LPCWSTR wzValue
    );
HRESULT CFGAPI CfgGetString(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out LPWSTR *psczValue
    );

// Get / set bool values
HRESULT CFGAPI CfgSetBool(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in BOOL fValue
    );
HRESULT CFGAPI CfgGetBool(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out BOOL *pfValue
    );

// Delete value
HRESULT CFGAPI CfgDeleteValue(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName
    );

// Modify blobs
HRESULT CFGAPI CfgSetBlob(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer
    );
HRESULT CFGAPI CfgGetBlob(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __deref_opt_out_bcount_opt(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    );

// General Enumeration functions
HRESULT CFGAPI CfgEnumerateValues(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in CONFIG_VALUETYPE cvType, // Can be VALUE_ANY_TYPE to query for all types
    __deref_out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out_opt DWORD *pcCount
    );

// Enumerate products
HRESULT CFGAPI CfgEnumerateProducts(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __deref_opt_out_bcount_opt(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out_opt DWORD *pcCount
    );
// TODO: implement CfgEnumerateVersions
HRESULT CFGAPI CfgEnumerateVersions(
    __in_bcount(CFGDB_HANDLE_BYTES) C_CFGDB_HANDLE cdHandle,
    __in_z LPWSTR wzProductName,
    __in_z LPWSTR wzPublicKey,
    __deref_out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out_opt DWORD *pcCount
    );

// Enumerate through historical values
HRESULT CFGAPI CfgEnumPastValues(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __deref_opt_out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out_opt DWORD *pcCount
    );

// Enumerate the list of known remote databases we were asked to remember
HRESULT CFGAPI CfgEnumDatabaseList(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __deref_opt_out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out_opt DWORD *pcCount
    );

// To get actual data out of an enumeration handle
HRESULT CFGAPI CfgEnumReadDataType(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out_opt CONFIG_VALUETYPE *pcvType
    );
HRESULT CFGAPI CfgEnumReadString(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __deref_opt_out_z LPCWSTR *pwzString
    );
HRESULT CFGAPI CfgEnumReadDword(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out_opt DWORD *pdwDword
    );
HRESULT CFGAPI CfgEnumReadQword(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out_opt DWORD64 *pqwQword
    );
HRESULT CFGAPI CfgEnumReadBool(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out_opt BOOL *pfBool
    );
// Returns a byte * to CFG_HASH_LEN number of bytes
HRESULT CFGAPI CfgEnumReadHash(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __deref_out_bcount(CFG_HASH_LEN) BYTE** ppbBuffer
    );
HRESULT CFGAPI CfgEnumReadSystemTime(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out SYSTEMTIME *pst
    );
HRESULT CFGAPI CfgEnumReadBinary(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __deref_out_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    );
HRESULT CFGAPI CfgEnumReadDisplayNameArray(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __out DISPLAY_NAME **prgDisplayNames,
    __out DWORD *pcDisplayNames
    );

void CFGAPI CfgReleaseEnumeration(
    __in_bcount_opt(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE cehHandle
    );

// Sync-related functions
HRESULT CFGAPI CfgSync(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __deref_out_ecount_opt(*pcProduct) CONFLICT_PRODUCT **prgcpProductList,
    __out DWORD *pcProduct
    );
void CFGAPI CfgReleaseConflictProductArray(
    __in_ecount_opt(cProduct) CONFLICT_PRODUCT *rgcpProductList,
    __in DWORD cProduct
    );
HRESULT CFGAPI CfgResolve(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_ecount(cProduct) CONFLICT_PRODUCT *rgcpProduct,
    __in DWORD cProduct
    );

// Per-user product registration
HRESULT CFGAPI CfgRegisterProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    );
HRESULT CFGAPI CfgUnregisterProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    );
HRESULT CFGAPI CfgIsProductRegistered(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __out BOOL *pfRegistered
    );

// Cleanup / deletion functions

// This entirely forgets a product as far as the entire database is concerned
// Does not touch admin "installed" or "uninstalled" state
// For legacy products, does NOT write to or delete any local registry or file settings
//     tracked by any loaded manifest (but does forget about the loaded manifest itself)
HRESULT CFGAPI CfgForgetProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    );

#ifdef __cplusplus
}
#endif
