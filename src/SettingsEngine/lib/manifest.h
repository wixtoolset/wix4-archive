#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#include "inc\cfgapi.h"
#include "detect.h"

struct LEGACY_SYNC_PRODUCT_SESSION;

const DWORD CfgLegacyDbRegistryRootClassesRoot = 0;
const DWORD CfgLegacyDbRegistryRootCurrentUser = 1;
const DWORD CfgLegacyDbRegistryRootLocalMachine = 2;

// We by-design don't support this key.
// const DWORD CfgLegacyDbRegistryRootUsers = 3;

// These values are stored directly in the database, so do NOT change the order here, as it will require migration or break all existing databases!
// "Persisted" in the name refers to the fact that it's persisted in the database
enum PERSISTED_FILE_ENCODING_TYPE
{
    PERSISTED_FILE_ENCODING_UNSPECIFIED = 0,
    // TODO: distinguish between non-BOM utf-8 and ANSI in the future?
    PERSISTED_FILE_ENCODING_UTF8,
    PERSISTED_FILE_ENCODING_UTF8_WITH_BOM,
    PERSISTED_FILE_ENCODING_UTF16,
    PERSISTED_FILE_ENCODING_UTF16_WITH_BOM,
};

enum LEGACY_FILE_TYPE
{
    LEGACY_FILE_INVALID = 0,
    LEGACY_FILE_PLAIN = 1,
    LEGACY_FILE_DIRECTORY = 2
};

struct LEGACY_FLAGS_PARSE_INFO
{
    DWORD dwOffset;
    LPWSTR sczCfgValueName;
};

struct LEGACY_REGISTRY_SPECIAL
{
    LPWSTR sczRegValueName;
    DWORD dwRegValueType;
    BOOL fHandleNonTypecasted;

    LEGACY_FLAGS_PARSE_INFO *rgFlagsInfo;
    DWORD cFlagsInfo;
};

struct LEGACY_REGISTRY_KEY
{
    DWORD dwRoot;
    LPWSTR sczKey;
    LPWSTR sczNamespace;

    LEGACY_REGISTRY_SPECIAL *rgRegKeySpecials;
    DWORD cRegKeySpecials;
};

struct LEGACY_FILE_INI_INFO
{
    LPWSTR sczNamespace;
    PERSISTED_FILE_ENCODING_TYPE fetManifestEncoding;

    LPWSTR sczSectionPrefix;
    LPWSTR sczSectionPostfix;
    LPWSTR sczValuePrefix;
    LPWSTR sczValueSeparator;

    LPWSTR *rgsczValueSeparatorException;
    DWORD cValueSeparatorException;

    LPWSTR sczCommentPrefix;
};

struct LEGACY_FILE_SPECIAL
{
    LPWSTR sczLocation;

    LEGACY_FILE_INI_INFO *rgIniInfo;
    DWORD cIniInfo;
};

struct LEGACY_FILE
{
    LEGACY_FILE_TYPE legacyFileType;

    // This is the namespace for directories and cfg files, and the name for individual files
    LPWSTR sczName;

    LPWSTR sczLocation;
    LPWSTR sczExpandedPath;

    LEGACY_FILE_SPECIAL *rgFileSpecials;
    DWORD cFileSpecials;
};

struct LEGACY_VALUE_FILTER
{
    LPWSTR sczExactName;
    LPWSTR sczPrefix;
    LPWSTR sczPostfix;

    BOOL fIgnore;
    BOOL fShareWriteOnRead;
};

struct LEGACY_PRODUCT
{
    LPWSTR sczProductId;

    LEGACY_DETECTION detect;
 
    // list of registry values that are handled by special rules from manifest and should be ignored
    // by default handling mechanism
    STRINGDICT_HANDLE shRegistrySpeciallyHandled;

    STRINGDICT_HANDLE shRegKeys; // Dictionary associating namespaces with regkeys
    LEGACY_REGISTRY_KEY *rgRegKeys;
    DWORD cRegKeys;

    STRINGDICT_HANDLE shFiles; // Dictionary associating names or namespaces with files
    LEGACY_FILE *rgFiles;
    DWORD cFiles;

    LEGACY_VALUE_FILTER *rgFilters;
    DWORD cFilters;

    DISPLAY_NAME *rgDisplayNames;
    DWORD cDisplayNames;
};

FILE_ENCODING IniFileEncodingToFileEncoding(
    __in PERSISTED_FILE_ENCODING_TYPE fetEncoding
    );
PERSISTED_FILE_ENCODING_TYPE FileEncodingToIniFileEncoding(
    __in FILE_ENCODING feEncoding
    );
HKEY ManifestConvertToRootKey(
    __in DWORD dwRootEnum
    );
void ManifestFreeProductStruct(
    __inout LEGACY_PRODUCT *pplpProduct
    );

#ifdef __cplusplus
}
#endif
