// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT IniHasValues(
    __in INI_HANDLE pIniHandle,
    __out BOOL *pfHasValues
    );

HRESULT IniFileRead(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzFullPath,
    __in LEGACY_FILE_INI_INFO *pIniInfo
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFullValueName = NULL;
    INI_VALUE *rgIniValues = NULL;
    DWORD cIniValues = 0;
    BOOL fIgnore = FALSE;
    BOOL fRet = FALSE;
    LEGACY_INI_FILE *pIniFile = NULL;
    FILETIME ft;
    SYSTEMTIME st;
    FILE_ENCODING feEncoding = FILE_ENCODING_UNSPECIFIED;
    CONFIG_VALUE cvValue = { };

    hr = DictGetValue(pSyncProductSession->shIniFilesByNamespace, pIniInfo->sczNamespace, reinterpret_cast<void **>(&pIniFile));
    ExitOnFailure(hr, "Error finding INI struct for namespace: %ls", pIniInfo->sczNamespace);

    hr = IniParse(pIniFile->pIniHandle, wzFullPath, &feEncoding);
    if (E_PATHNOTFOUND == hr || E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to parse INI file: %ls", wzFullPath);

    pIniFile->fetReadEncoding = FileEncodingToIniFileEncoding(feEncoding);

    hr = IniGetValueList(pIniFile->pIniHandle, &rgIniValues, &cIniValues);
    ExitOnFailure(hr, "Failed to get ini value list");

    hr = FileGetTime(wzFullPath, NULL, NULL, &ft);
    ExitOnFailure(hr, "failed to get modified time of file : %ls", wzFullPath);

    fRet = FileTimeToSystemTime(&ft, &st);
    if (!fRet)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Failed to convert file time to system time for file: %ls", wzFullPath);
    }

    for (DWORD i = 0; i < cIniValues; ++i)
    {
        hr = MapFileToCfgName(pIniFile->sczNamespace, rgIniValues[i].wzName, &sczFullValueName);
        ExitOnFailure(hr, "Failed ot map INI value name: %ls, %ls", pIniFile->sczNamespace, rgIniValues[i].wzName);

        hr = FilterCheckValue(&pSyncProductSession->product, sczFullValueName, &fIgnore, NULL);
        ExitOnFailure(hr, "Failed to check if ini value should be ignored: %ls", sczFullValueName);

        if (fIgnore)
        {
            continue;
        }

        hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczFullValueName);
        ExitOnFailure(hr, "Failed to add to dictionary value: %ls", sczFullValueName);

        ReleaseNullCfgValue(cvValue);
        hr = ValueSetString(rgIniValues[i].wzValue, FALSE, &st, pcdb->sczGuid, &cvValue);
        ExitOnFailure(hr, "Failed to set string in memory for value named %ls", sczFullValueName);

        hr = ValueWrite(pcdb, pcdb->dwAppID, sczFullValueName, &cvValue, TRUE, NULL);
        ExitOnFailure(hr, "Failed to set value from INI: %ls", sczFullValueName);
    }

LExit:
    ReleaseStr(sczFullValueName);
    ReleaseCfgValue(cvValue);

    return hr;
}

HRESULT IniFileSetValue(
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzName,
    __in const CONFIG_VALUE *pcvValue,
    __out BOOL *pfHandled
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczNamespace = NULL;
    LPWSTR sczPlainValueName = NULL;
    LEGACY_INI_FILE *pIniFile = NULL;

    *pfHandled = FALSE;

    hr = MapGetNamespace(wzName, &sczNamespace, &sczPlainValueName);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = DictGetValue(pSyncProductSession->shIniFilesByNamespace, sczNamespace, reinterpret_cast<void **>(&pIniFile));
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    else
    {
        ExitOnFailure(hr, "Failed to lookup namespace in list of INI file namespaces: %ls", sczNamespace);
        *pfHandled = TRUE;

        if (VALUE_DELETED == pcvValue->cvType)
        {
            hr = IniSetValue(pIniFile->pIniHandle, sczPlainValueName, NULL);
            ExitOnFailure(hr, "Failed to set value to null in INI");
        }
        else if (VALUE_STRING == pcvValue->cvType)
        {
            hr = IniSetValue(pIniFile->pIniHandle, sczPlainValueName, pcvValue->string.sczValue);
            ExitOnFailure(hr, "Failed to set value %ls in INI", sczPlainValueName);
        }
        else
        {
            LogErrorString(E_FAIL, "TODO: we don't yet support writing non-string values to INI!");
            ExitFunction1(hr = S_OK);
        }
    }

LExit:
    ReleaseStr(sczNamespace);
    ReleaseStr(sczPlainValueName);

    return hr;
}

HRESULT IniFileOpen(
    __in LEGACY_FILE *pFile,
    __in LEGACY_FILE_SPECIAL *pFileSpecial,
    __in LEGACY_FILE_INI_INFO *pFileIniInfo,
    __inout LEGACY_INI_FILE *pIniFile
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczTemp = NULL;
    LPWSTR sczFullPath = NULL;
    LPWSTR sczNamespace = NULL;

    switch (pFile->legacyFileType)
    {
    case LEGACY_FILE_DIRECTORY:
        hr = StrAllocString(&sczTemp, pFile->sczExpandedPath, 0);
        ExitOnFailure(hr, "Failed to copy full path to INI file");
        
        hr = PathConcat(sczTemp, pFileSpecial->sczLocation, &sczFullPath);\
        ExitOnFailure(hr, "Failed to concatenate path to INI file");
        break;

    case LEGACY_FILE_PLAIN:
        hr = StrAllocString(&sczFullPath, pFile->sczExpandedPath, 0);
        ExitOnFailure(hr, "Failed to copy full path to INI file");
        break;

    default:
        hr = E_FAIL;
        ExitOnFailure(hr, "Unexpected legacy file type encountered: %u", pFile->legacyFileType);
        break;
    }

    hr = StrAllocString(&sczNamespace, pFileIniInfo->sczNamespace, 0);
    ExitOnFailure(hr, "Failed to copy full path to INI file");

    hr = IniInitialize(&pIniFile->pIniHandle);
    ExitOnFailure(hr, "Failed to initialize iniutil handle");

    hr = IniSetOpenTag(pIniFile->pIniHandle, pFileIniInfo->sczSectionPrefix, pFileIniInfo->sczSectionPostfix);
    ExitOnFailure(hr, "Failed to set ini open tag info");

    hr = IniSetValueStyle(pIniFile->pIniHandle, pFileIniInfo->sczValuePrefix, pFileIniInfo->sczValueSeparator);
    ExitOnFailure(hr, "Failed to set ini open tag info");

    for (DWORD i = 0; i < pFileIniInfo->cValueSeparatorException; ++i)
    {
        hr = IniSetValueSeparatorException(pIniFile->pIniHandle, pFileIniInfo->rgsczValueSeparatorException[i]);
        ExitOnFailure(hr, "Failed to set value separator exception %ls", pFileIniInfo->rgsczValueSeparatorException[i]);
    }

    hr = IniSetCommentStyle(pIniFile->pIniHandle, pFileIniInfo->sczCommentPrefix);
    ExitOnFailure(hr, "Failed to set comment info");

    pIniFile->fetManifestEncoding = pFileIniInfo->fetManifestEncoding;
    pIniFile->sczFullPath = sczFullPath;
    sczFullPath = NULL;
    pIniFile->sczNamespace = sczNamespace;
    sczNamespace = NULL;

LExit:
    ReleaseStr(sczTemp);
    ReleaseStr(sczFullPath);
    ReleaseStr(sczNamespace);

    return hr;
}

HRESULT IniFileWrite(
    __in LEGACY_INI_FILE *pIniFile
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDirectoryToCreate = NULL;
    LPWSTR sczVirtualStorePath = NULL;
    BOOL fIniHasValues = 0;

    if (NULL == pIniFile->sczFullPath)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = IniHasValues(pIniFile->pIniHandle, &fIniHasValues);
    ExitOnFailure(hr, "Failed to check if INI file has any vbalues");

    if (fIniHasValues)
    {
        hr = PathGetDirectory(pIniFile->sczFullPath, &sczDirectoryToCreate);
        ExitOnFailure(hr, "Failed to get directory portion of path: %ls", pIniFile->sczFullPath);

        hr = DirEnsureExists(sczDirectoryToCreate, NULL);
        ExitOnFailure(hr, "Failed to ensure directory exists: %ls", sczDirectoryToCreate);

        hr = IniWriteFile(pIniFile->pIniHandle, pIniFile->sczFullPath, IniFileEncodingToFileEncoding(pIniFile->fetManifestEncoding));
        if (E_ACCESSDENIED == hr)
        {
            hr = UtilConvertToVirtualStorePath(pIniFile->sczFullPath, &sczVirtualStorePath);
            ExitOnFailure(hr, "Failed to convert path to virtual store path: %ls", pIniFile->sczFullPath);

            hr = PathGetDirectory(sczVirtualStorePath, &sczDirectoryToCreate);
            ExitOnFailure(hr, "Failed to get directory portion of path: %ls", pIniFile->sczFullPath);

            hr = DirEnsureExists(sczDirectoryToCreate, NULL);
            ExitOnFailure(hr, "Failed to ensure directory exists: %ls", sczDirectoryToCreate);

            hr = IniWriteFile(pIniFile->pIniHandle, sczVirtualStorePath, IniFileEncodingToFileEncoding(pIniFile->fetManifestEncoding));
        }
        ExitOnFailure(hr, "Failed to flush INI file back to disk");
    }
    else
    {
        hr = FileEnsureDelete(pIniFile->sczFullPath);
        if (E_ACCESSDENIED == hr)
        {
            hr = UtilConvertToVirtualStorePath(pIniFile->sczFullPath, &sczVirtualStorePath);
            ExitOnFailure(hr, "Failed to convert path to virtual store path: %ls", pIniFile->sczFullPath);

            hr = FileEnsureDelete(sczVirtualStorePath);
        }
        ExitOnFailure(hr, "Failed to delete empty INI file: %ls", pIniFile->sczFullPath);
    }

LExit:
    ReleaseStr(sczDirectoryToCreate);
    ReleaseStr(sczVirtualStorePath);

    return hr;
}

HRESULT IniHasValues(
    __in INI_HANDLE pIniHandle,
    __out BOOL *pfHasValues
    )
{
    HRESULT hr = S_OK;
    INI_VALUE *rgIniValues = NULL;
    DWORD cIniValues = 0;

    hr = IniGetValueList(pIniHandle, &rgIniValues, &cIniValues);
    ExitOnFailure(hr, "Failed to get list of values from INI");

    for (DWORD i = 0; i < cIniValues; ++i)
    {
        if (rgIniValues[i].wzValue != NULL)
        {
            *pfHasValues = TRUE;
            ExitFunction1(hr = S_OK);
        }
    }

    *pfHasValues = FALSE;
    ExitFunction1(hr = S_OK);

LExit:
    return hr;
}

void IniFree(
    __in LEGACY_INI_FILE *pIniFile
    )
{
    ReleaseStr(pIniFile->sczNamespace);
    ReleaseStr(pIniFile->sczFullPath);
    ReleaseIni(pIniFile->pIniHandle);
}

