// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT AddLegacyRegistrySpecialCfgValuesToDict(
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __inout STRINGDICT_HANDLE *pshDictCfgValuesSeen
    );
static void ManifestFreeFileIniInfo(
    LEGACY_FILE_INI_INFO *pIniInfo
    );
static void ManifestFreeFileSpecial(
    LEGACY_FILE_SPECIAL *pFileSpecial
    );
static void ManifestFreeFile(
    LEGACY_FILE *pFile
    );
static void ManifestFreeRegistrySpecial(
    LEGACY_REGISTRY_SPECIAL *pRegKeySpecial
    );
static void ManifestFreeRegistryKey(
    LEGACY_REGISTRY_KEY *pRegKey
    );
static void ManifestFreeFilter(
    LEGACY_VALUE_FILTER *pFilter
    );

FILE_ENCODING IniFileEncodingToFileEncoding(
    __in PERSISTED_FILE_ENCODING_TYPE fetEncoding
    )
{
    switch (fetEncoding)
    {
    case PERSISTED_FILE_ENCODING_UNSPECIFIED:
        return FILE_ENCODING_UNSPECIFIED;
        break;
    case PERSISTED_FILE_ENCODING_UTF8:
        return FILE_ENCODING_UTF8;
        break;
    case PERSISTED_FILE_ENCODING_UTF8_WITH_BOM:
        return FILE_ENCODING_UTF8_WITH_BOM;
        break;
    case PERSISTED_FILE_ENCODING_UTF16:
        return FILE_ENCODING_UTF16;
        break;
    case PERSISTED_FILE_ENCODING_UTF16_WITH_BOM:
        return FILE_ENCODING_UTF16_WITH_BOM;
        break;
    default:
        return FILE_ENCODING_UNSPECIFIED;
        break;
    }
}

PERSISTED_FILE_ENCODING_TYPE FileEncodingToIniFileEncoding(
    __in FILE_ENCODING feEncoding
    )
{
    switch (feEncoding)
    {
    case FILE_ENCODING_UNSPECIFIED:
        return PERSISTED_FILE_ENCODING_UNSPECIFIED;
        break;
    case FILE_ENCODING_UTF8:
        return PERSISTED_FILE_ENCODING_UTF8;
        break;
    case FILE_ENCODING_UTF8_WITH_BOM:
        return PERSISTED_FILE_ENCODING_UTF8_WITH_BOM;
        break;
    case FILE_ENCODING_UTF16:
        return PERSISTED_FILE_ENCODING_UTF16;
        break;
    case FILE_ENCODING_UTF16_WITH_BOM:
        return PERSISTED_FILE_ENCODING_UTF16_WITH_BOM;
        break;
    default:
        return PERSISTED_FILE_ENCODING_UNSPECIFIED;
        break;
    }
}

HKEY ManifestConvertToRootKey(
    __in DWORD dwRootEnum
    )
{
    switch (dwRootEnum)
    {
    case CfgLegacyDbRegistryRootCurrentUser:
        return HKEY_CURRENT_USER;

    case CfgLegacyDbRegistryRootLocalMachine:
        return HKEY_LOCAL_MACHINE;

    default:
        return NULL;
    }
}

void ManifestFreeProductStruct(
    __inout LEGACY_PRODUCT *pProduct
    )
{
    ReleaseStr(pProduct->sczProductId);
    ReleaseDict(pProduct->shRegistrySpeciallyHandled);
    ReleaseDict(pProduct->shRegKeys);
    ReleaseDict(pProduct->shFiles);

    DetectFree(&pProduct->detect);

    for (DWORD i = 0; i < pProduct->cRegKeys; ++i)
    {
        ManifestFreeRegistryKey(pProduct->rgRegKeys + i);
    }
    ReleaseMem(pProduct->rgRegKeys);

    for (DWORD i = 0; i < pProduct->cFiles; ++i)
    {
        ManifestFreeFile(pProduct->rgFiles + i);
    }
    ReleaseMem(pProduct->rgFiles);

    for (DWORD i = 0; i < pProduct->cFilters; ++i)
    {
        ManifestFreeFilter(pProduct->rgFilters + i);
    }
    ReleaseMem(pProduct->rgFilters);

    for (DWORD i = 0; i < pProduct->cDisplayNames; ++i)
    {
        ReleaseStr(pProduct->rgDisplayNames[i].sczName);
    }
    ReleaseMem(pProduct->rgDisplayNames);
}

void ManifestFreeFileIniInfo(
    LEGACY_FILE_INI_INFO *pIniInfo
    )
{
    ReleaseStr(pIniInfo->sczNamespace);

    ReleaseStr(pIniInfo->sczSectionPrefix);
    ReleaseStr(pIniInfo->sczSectionPostfix);
    ReleaseStr(pIniInfo->sczValuePrefix);
    ReleaseStr(pIniInfo->sczValueSeparator);

    for (DWORD i = 0; i < pIniInfo->cValueSeparatorException; ++i)
    {
        ReleaseStr(pIniInfo->rgsczValueSeparatorException[i]);
    }
    ReleaseMem(pIniInfo->rgsczValueSeparatorException);

    ReleaseStr(pIniInfo->sczCommentPrefix);
}


void ManifestFreeFileSpecial(
    LEGACY_FILE_SPECIAL *pFileSpecial
    )
{
    ReleaseStr(pFileSpecial->sczLocation);

    for (DWORD i = 0; i < pFileSpecial->cIniInfo; ++i)
    {
        ManifestFreeFileIniInfo(pFileSpecial->rgIniInfo + i);
    }
    ReleaseMem(pFileSpecial->rgIniInfo);
}

void ManifestFreeFile(
    LEGACY_FILE *pFile
    )
{
    ReleaseStr(pFile->sczName);
    ReleaseStr(pFile->sczLocation);
    ReleaseStr(pFile->sczExpandedPath);

    for (DWORD i = 0; i < pFile->cFileSpecials; ++i)
    {
        ManifestFreeFileSpecial(pFile->rgFileSpecials + i);
    }
    ReleaseMem(pFile->rgFileSpecials);
}

void ManifestFreeRegistrySpecial(
    LEGACY_REGISTRY_SPECIAL *pRegKeySpecial
    )
{
    ReleaseStr(pRegKeySpecial->sczRegValueName);

    for (DWORD i = 0; i < pRegKeySpecial->cFlagsInfo; ++i)
    {
        ReleaseStr(pRegKeySpecial->rgFlagsInfo[i].sczCfgValueName);
    }

    ReleaseMem(pRegKeySpecial->rgFlagsInfo);
}

void ManifestFreeRegistryKey(
    LEGACY_REGISTRY_KEY *pRegKey
    )
{
    ReleaseStr(pRegKey->sczKey);
    ReleaseStr(pRegKey->sczNamespace);

    for (DWORD i = 0; i < pRegKey->cRegKeySpecials; ++i)
    {
        ManifestFreeRegistrySpecial(pRegKey->rgRegKeySpecials + i);
    }

    ReleaseMem(pRegKey->rgRegKeySpecials);
}

void ManifestFreeFilter(
    LEGACY_VALUE_FILTER *pFilter
    )
{
    ReleaseStr(pFilter->sczExactName);
    ReleaseStr(pFilter->sczPrefix);
    ReleaseStr(pFilter->sczPostfix);
}
