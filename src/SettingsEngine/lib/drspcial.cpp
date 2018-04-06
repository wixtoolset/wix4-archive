// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT FindSpecialInLegacyFile(
    __in const LEGACY_FILE *pFile,
    __in_z LPCWSTR wzSubPath,
    __out LEGACY_FILE_SPECIAL **pplfsFileSpecial
    );


HRESULT DirSpecialFileRead(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_FILE *pFile,
    __in_z LPCWSTR wzFullPath,
    __in_z LPCWSTR wzSubPath,
    __out BOOL *pfContinueProcessing
    )
{
    HRESULT hr = S_OK;
    LEGACY_FILE_SPECIAL *pFileSpecial = NULL;

    hr = FindSpecialInLegacyFile(pFile, wzSubPath, &pFileSpecial);
    if (E_NOTFOUND == hr)
    {
        *pfContinueProcessing = TRUE;
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to find special for subpath: %ls in legacy file", wzSubPath);

    *pfContinueProcessing = FALSE;

    if (1 < pFileSpecial->cIniInfo)
    {
        hr = E_FAIL;
        ExitOnFailure(hr, "Can't parse INI file %ls with two different Cfg file formats for the same file", wzSubPath);
    }
    else if (1 == pFileSpecial->cIniInfo)
    {
        hr = IniFileRead(pcdb, pSyncProductSession, wzFullPath, pFileSpecial->rgIniInfo + 0);
        ExitOnFailure(hr, "Failed to parse INI file at path: %ls", wzFullPath);
    }

LExit:
    return hr;
}

static HRESULT FindSpecialInLegacyFile(
    __in const LEGACY_FILE *pFile,
    __in_z LPCWSTR wzSubPath,
    __out LEGACY_FILE_SPECIAL **pplfsFileSpecial
    )
{
    HRESULT hr = S_OK;

    // If no specials to find, exit now with not found
    if (0 == pFile->cFileSpecials)
    {
        *pplfsFileSpecial = NULL;
        ExitFunction1(hr = E_NOTFOUND);
    }

    // If this legacy file only points to a particular file, there can only be one file to specially handle, so return that if it exists
    if (LEGACY_FILE_PLAIN == pFile->legacyFileType)
    {
        if(1 < pFile->cFileSpecials)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "A legacy_file struct for a specific file shouldn't be able to have more than one specially handled file in it!");
        }

        *pplfsFileSpecial = pFile->rgFileSpecials;
        ExitFunction1(hr = S_OK);
    }
    
    // Otherwise, search through all special file handlers to see if one matches our subpath
    for (DWORD i = 0; i < pFile->cFileSpecials; ++i)
    {
        if (lstrcmpiW(pFile->rgFileSpecials[i].sczLocation, wzSubPath) == 0)
        {
            *pplfsFileSpecial = pFile->rgFileSpecials + i;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}
