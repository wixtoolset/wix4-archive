//-------------------------------------------------------------------------------------------------
// <copyright file="cryputilhelpers.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

typedef struct _CrypMockableFunctions
{
    PFN_RTLENCRYPTMEMORY pfnRtlEncryptMemory;
    PFN_RTLDECRYPTMEMORY pfnRtlDecryptMemory;
    PFN_CRYPTPROTECTMEMORY pfnCryptProtectMemory;
    PFN_CRYPTUNPROTECTMEMORY pfnCryptUnprotectMemory;
} CrypMockableFunctions;

static HRESULT CrypEncryptMemoryHelper(
    __in CrypMockableFunctions* pFunctions,
	__inout LPVOID pData,
	__in DWORD cbData,
	__in DWORD dwFlags
    )
{
    HRESULT hr = S_OK;
    
    if (0 != cbData % CRYP_ENCRYPT_MEMORY_SIZE)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "cbData (%u) must be a multiple of CRYP_ENCRYPT_MEMORY_SIZE (%u).", cbData, CRYP_ENCRYPT_MEMORY_SIZE);
    }

    if (pFunctions->pfnCryptProtectMemory)
    {
        if (!pFunctions->pfnCryptProtectMemory(pData, cbData, dwFlags))
        {
            ExitFunctionWithLastError(hr);
        }

        hr = S_OK;
    }
    else if (pFunctions->pfnRtlEncryptMemory)
    {
        hr = static_cast<HRESULT>(pFunctions->pfnRtlEncryptMemory(pData, cbData, dwFlags));
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "At least one encryption method must be provided.");
    }

LExit:
    return hr;
}

static HRESULT CrypDecryptMemoryHelper(
    __in CrypMockableFunctions* pFunctions,
	__inout LPVOID pData,
	__in DWORD cbData,
	__in DWORD dwFlags
    )
{
    HRESULT hr = S_OK;
    
    if (0 != cbData % CRYP_ENCRYPT_MEMORY_SIZE)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "cbData (%u) must be a multiple of CRYP_ENCRYPT_MEMORY_SIZE (%u).", cbData, CRYP_ENCRYPT_MEMORY_SIZE);
    }

    if (pFunctions->pfnCryptUnprotectMemory)
    {
        if (!pFunctions->pfnCryptUnprotectMemory(pData, cbData, dwFlags))
        {
            ExitFunctionWithLastError(hr);
        }

        hr = S_OK;
    }
    else if (pFunctions->pfnRtlDecryptMemory)
    {
        hr = static_cast<HRESULT>(pFunctions->pfnRtlDecryptMemory(pData, cbData, dwFlags));
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "At least one decryption method must be provided.");
    }

LExit:
    return hr;
}

