#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

struct LEGACY_SYNC_PRODUCT_SESSION;

enum LEGACY_DETECT_TYPE
{
    LEGACY_DETECT_TYPE_INVALID = 0,
    LEGACY_DETECT_TYPE_ARP = 1,
    LEGACY_DETECT_TYPE_EXE = 2,
};

struct LEGACY_DETECT_ARP_HINT
{
    LPWSTR sczKeyName;
};

struct LEGACY_DETECT
{
    LEGACY_DETECT_TYPE ldtType;

    union
    {
        struct
        {
            LPWSTR sczDisplayName;
            LPWSTR sczRegKeyName;

            LPWSTR sczInstallLocationProperty;
            LPWSTR wzInstallLocationValue; // This is the value of the directory portion of InstallLocation

            LPWSTR sczUninstallStringDirProperty;
            LPWSTR wzUninstallStringDirValue; // This is the value of the directory portion of UninstallString

            LPWSTR sczDisplayIconDirProperty;
            LPWSTR wzDisplayIconDirValue; // This is the value of the directory portion of DisplayIcon
        } arp;
        struct
        {
            LPWSTR sczFileName; // Name of the filename we're detecting, i.e. "Notepad2.exe"
            LPWSTR wzFileDirValue; // full path to the file from the current detection

            LPWSTR sczDetectedFileDir; // full path to the parent directory of the file from the current detection
            LPWSTR sczFileDirProperty; // Name of the property to store the filedir into, i.e. "InstallDir"
            LPWSTR sczFileDirCachedValue; // Value retrieved from previous detection via cached column in database - this is a fallback, in case today's detection finds nothing
        } exe;
    };

    BOOL fFound;
};

struct ARP_PRODUCT
{
    LPWSTR sczDisplayName;
    LPWSTR sczRegKeyName;
    LPWSTR sczInstallLocation;
    LPWSTR sczUninstallStringDir;
    LPWSTR sczDisplayIconDir;
};

struct ARP_PRODUCTS
{
    BOOL fEnumerationRun;
    STRINGDICT_HANDLE shProductsFoundByDisplayName; // Dictionary associating display names with key names
    STRINGDICT_HANDLE shProductsFoundByRegKeyName; // Dictionary associating reg key names with key names

    ARP_PRODUCT *rgProducts;
    DWORD cProducts;
};

struct EXE_PRODUCT
{
    LPWSTR sczFilePath;
    LPWSTR sczFileName;
    LPWSTR sczFileDir;
};

struct EXE_PRODUCTS
{
    BOOL fEnumerationRun;
    STRINGDICT_HANDLE shProductsFound; // Dictionary associating exe filenames with key names

    EXE_PRODUCT *rgProducts;
    DWORD cProducts;
};

struct LEGACY_CACHED_DETECTION_RESULT
{
    LPWSTR sczPropertyName;
    LPWSTR sczPropertyValue;
};

struct LEGACY_DETECTION
{
    LEGACY_DETECT *rgDetects;
    DWORD cDetects;

    // These are the cached property values
    STRINGDICT_HANDLE shCachedDetectionPropertyValues;
    LEGACY_CACHED_DETECTION_RESULT *rgCachedDetectionProperties;
    DWORD cCachedDetectionProperties;
};

HRESULT DetectReadCache(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_DETECTION *pDetect
    );
HRESULT DetectUpdateCache(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    );
HRESULT DetectGetArpProducts(
    __out ARP_PRODUCTS *pArpProducts
    );
HRESULT DetectGetExeProducts(
    __out EXE_PRODUCTS *pArpProducts
    );
HRESULT DetectProduct(
    __in CFGDB_STRUCT *pcdb,
    __in BOOL fJustReadCache,
    __in ARP_PRODUCTS *pArpProducts,
    __in EXE_PRODUCTS *pExeProducts,
    __inout LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    );
HRESULT DetectExpandDirectoryPath(
    __in LPCWSTR wzInput,
    __in LEGACY_DETECTION *pDetect,
    __deref_out LPWSTR *psczOutput
    );
void DetectFree(
    __in LEGACY_DETECTION *pDetection
    );
void DetectFreeArpProducts(
    __in ARP_PRODUCTS *pArpProducts
    );
void DetectFreeExeProducts(
    __in EXE_PRODUCTS *pExeProducts
    );

#ifdef __cplusplus
}
#endif
