// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static LPWSTR FindLastUnescapedBackslash(
    __in_z LPCWSTR wzSearchString
    );

HRESULT MapGetNamespace(
    __in_z LPCWSTR wzFullName,
    __out_z LPWSTR *psczNamespace,
    __out_z LPWSTR *psczValue
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzSeparator = NULL;

    wzSeparator = wcschr(wzFullName, NAMESPACE_DELIMITER_CHARACTER);

    if (NULL != wzSeparator)
    {
        hr = StrAllocString(psczNamespace, wzFullName, wzSeparator - wzFullName);
        ExitOnFailure(hr, "Failed to make copy of namespace string");

        hr = StrAllocString(psczValue, wzSeparator + 2, 0);
        ExitOnFailure(hr, "Failed to make copy of value string");
    }
    else
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

LExit:
    return hr;
}

HRESULT MapFileToCfgName(
    __in_z LPCWSTR wzName,
    __in_z LPCWSTR wzSubPath,
    __out_z LPWSTR *psczOutput
    )
{
    HRESULT hr = S_OK;

    if (0 == lstrlenW(wzSubPath))
    {
        // This is an exact file in the tracklist, and the name is the actual value name, not a namespace
        hr = StrAllocString(psczOutput, wzName, 0);
        ExitOnFailure(hr, "Failed to copy name while formatting legacy file name");
    }
    else
    {
        // Name represents a namespace, so treat it as such
        hr = StrAllocFormatted(psczOutput, L"%ls%wc\\%ls", wzName, NAMESPACE_DELIMITER_CHARACTER, wzSubPath);
        ExitOnFailure(hr, "Failed to format legacy file name");
    }

LExit:
    return hr;
}

HRESULT MapRegValueToCfgName(
    __in_z LPCWSTR wzNamespace,
    __in_z LPCWSTR wzKey,
    __in_z LPCWSTR wzRawValueName,
    __out_z LPWSTR *psczOutput
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczEscapedValueName = NULL;
    LPCWSTR wzResolvedValueName = NULL;
    LPWSTR sczResolvedValueAndKey = NULL;

    if (NULL == wcsrchr(wzRawValueName, L'\\'))
    {
        wzResolvedValueName = wzRawValueName;
    }
    else
    {
        hr = StrAllocString(&sczEscapedValueName, wzRawValueName, 0);
        ExitOnFailure(hr, "Failed to allocate copy of raw registry value name for escaping: %ls", wzRawValueName);

        hr = StrReplaceStringAll(&sczEscapedValueName, L"\\", L"\\\\");
        ExitOnFailure(hr, "Failed to escape backslashes in registry value name");
        hr = S_OK; // Kill any S_FALSE that may happen to appear

        wzResolvedValueName = sczEscapedValueName;
    }

    hr = PathConcat(wzKey, wzResolvedValueName, &sczResolvedValueAndKey);
    ExitOnFailure(hr, "Failed to concatenate path");

    if (0 == lstrlenW(wzResolvedValueName))
    {
        hr = StrAllocConcat(&sczResolvedValueAndKey, L"\\", 0);
        ExitOnFailure(hr, "Failed to append backslash for (Default) registry value");
    }

    hr = StrAllocFormatted(psczOutput, L"%ls%wc\\%ls", wzNamespace, NAMESPACE_DELIMITER_CHARACTER, sczResolvedValueAndKey);
    ExitOnFailure(hr, "Failed to format legacy value name");

LExit:
    ReleaseStr(sczResolvedValueAndKey);
    ReleaseStr(sczEscapedValueName);

    return hr;
}

HRESULT MapRegSpecialToCfgValueName(
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in const LEGACY_REGISTRY_SPECIAL *pRegKeySpecial,
    __out_z LPWSTR *psczOutput
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSubKey = NULL;
    LPWSTR pwcBackslash = NULL;
    LPCWSTR wzValueNamePortion = NULL;

    pwcBackslash = wcsrchr(pRegKeySpecial->sczRegValueName, L'\\');

    if (NULL == pwcBackslash)
    {
        hr = MapRegValueToCfgName(pRegKey->sczNamespace, L"", pRegKeySpecial->sczRegValueName, psczOutput);
        ExitOnFailure(hr, "Failed to map registry value name to cfg value name (basic)");
    }
    else
    {
        hr = StrAllocString(&sczSubKey, pRegKeySpecial->sczRegValueName, 0);
        ExitOnFailure(hr, "Failed to allocate copy of regvaluename");

        pwcBackslash = wcsrchr(sczSubKey, L'\\');

        wzValueNamePortion = pwcBackslash + 1;
        *pwcBackslash = L'\0';

        hr = MapRegValueToCfgName(pRegKey->sczNamespace, sczSubKey, wzValueNamePortion, psczOutput);
        ExitOnFailure(hr, "Failed to map registry value name to cfg value name (with subkey in name)");
    }

LExit:
    ReleaseStr(sczSubKey);

    return hr;
}

HRESULT MapCfgNameToFile(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzCfgName,
    __out_z LPWSTR *psczOutput
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczNameCopy = NULL;
    LPWSTR sczExpandedPath = NULL;
    LPCWSTR wzName = NULL;
    LPWSTR wzFileName = NULL;
    LEGACY_FILE *pFile = NULL;

    hr = StrAllocString(&sczNameCopy, wzCfgName, 0);
    ExitOnFailure(hr, "Failed to allocate copy of string");

    wzFileName = const_cast<LPWSTR>(wcschr(sczNameCopy, NAMESPACE_DELIMITER_CHARACTER));
    if (NULL == wzFileName)
    {
        // No namespace, so this is a raw file
        wzName = sczNameCopy;
    }
    else
    {
        *wzFileName = L'\0'; // Zero out the delimiter to separate the namespace and the name
        ++wzFileName; // The name begins one character after the namespace delimiter
        wzName = sczNameCopy; // Namespace starts at the beginning of the string
    }

    // Get past the initial backslash that is expected of all regular files
    if (NULL != wzFileName)
    {
        if (L'\\' != *wzFileName)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
        else
        {
            ++wzFileName;
        }
    }

    hr = DictGetValue(pProduct->shFiles, wzName, reinterpret_cast<void **>(&pFile));
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }
    ExitOnFailure(hr, "Failed to lookup name %ls in file dictionary", wzName);

    hr = UtilExpandLegacyPath(pFile->sczLocation, &pProduct->detect, &sczExpandedPath);
    ExitOnFailure(hr, "Failed to expand legacy path: %ls", pFile->sczLocation);

    if (NULL != wzFileName)
    {
        hr = PathConcat(sczExpandedPath, wzFileName, psczOutput);
        ExitOnFailure(hr, "Failed to concatenate base path '%ls' with name: %ls", sczExpandedPath, wzFileName);
    }
    else
    {
        hr = StrAllocString(psczOutput, sczExpandedPath, 0);
        ExitOnFailure(hr, "Failed to allocate copy of expanded path string");
    }

LExit:
    ReleaseStr(sczNameCopy);
    ReleaseStr(sczExpandedPath);

    return hr;
}

HRESULT MapCfgNameToRegValue(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzCfgName,
    __out DWORD *pdwRoot,
    __out LPWSTR *psczKey,
    __out LPWSTR *psczValueName
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczNameCopy = NULL;
    LPCWSTR wzNamespace = NULL;
    LPWSTR wzValueName = NULL;
    LPWSTR wzLastBackslash = NULL;
    LEGACY_REGISTRY_KEY *pRegKey = NULL;

    hr = StrAllocString(&sczNameCopy, wzCfgName, 0);
    ExitOnFailure(hr, "Failed to allocate copy of string");

    wzValueName = const_cast<LPWSTR>(wcschr(sczNameCopy, NAMESPACE_DELIMITER_CHARACTER));
    if (NULL == wzValueName)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }
    *wzValueName = L'\0'; // Zero out the delimiter to separate the namespace and the name
    ++wzValueName; // The name begins one character after the namespace delimiter
    wzNamespace = sczNameCopy; // Namespace starts at the beginning of the string

    // Get past the ininitial backslash that is expected of all regular registry values
    if (L'\\' != *wzValueName)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }
    else
    {
        ++wzValueName;
    }

    hr = DictGetValue(pProduct->shRegKeys, wzNamespace, reinterpret_cast<void **>(&pRegKey));
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }
    ExitOnFailure(hr, "Failed to lookup namespace %ls in regkey dictionary", wzNamespace);

    *pdwRoot = pRegKey->dwRoot;

    hr = StrAllocString(psczKey, pRegKey->sczKey, 0);
    ExitOnFailure(hr, "Failed to copy registry key string");

    wzLastBackslash = FindLastUnescapedBackslash(wzValueName);

    if (NULL == wzLastBackslash)
    {
        hr = StrAllocString(psczValueName, wzValueName, 0);
        ExitOnFailure(hr, "Failed to allocate copy of name");
    }
    else
    {
        *wzLastBackslash = L'\0';

        hr = StrAllocConcat(psczKey, wzValueName, 0);
        ExitOnFailure(hr, "Failed to concat subkey to key");

        hr = StrAllocConcat(psczKey, L"\\", 1);
        ExitOnFailure(hr, "Failed to concat backslash to key");

        // Everything after the last backslash is the name
        hr = StrAllocString(psczValueName, wzLastBackslash + 1, 0);
        ExitOnFailure(hr, "Failed to allocate valuename");
    }

    hr = StrReplaceStringAll(psczValueName, L"\\\\", L"\\");
    ExitOnFailure(hr, "Failed to unescape any backslashes");
    hr = S_OK; // Kill any S_FALSE that may happen to appear

LExit:
    ReleaseStr(sczNameCopy);

    return hr;
}

// Static functions
static LPWSTR FindLastUnescapedBackslash(
    __in_z LPCWSTR wzSearchString
    )
{
    LPWSTR wzLastBackslash;

    wzLastBackslash = const_cast<LPWSTR>(wcsrchr(wzSearchString, L'\\'));

    // If we found an escaped backslash, skip to the next until we find an unescaped backslash
    while (wzLastBackslash > (wzSearchString + 1) && wzLastBackslash[-1] == L'\\')
    {
        wzLastBackslash -= 2;

        while (wzLastBackslash > wzSearchString && *wzLastBackslash != L'\\')
        {
            --wzLastBackslash;
        }

        if (wzLastBackslash == wzSearchString)
        {
            wzLastBackslash = NULL;
        }
    }

    return wzLastBackslash;
}
