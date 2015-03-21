//-------------------------------------------------------------------------------------------------
// <copyright file="vrntutilcore.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

typedef struct _VrntMockableFunctions
{
    PFN_STRSECUREZEROFREESTRING pfnStrSecureZeroFreeString;
    PFN_CRYPENCRYPTMEMORY pfnCrypEncryptMemory;
    PFN_CRYPDECRYPTMEMORY pfnCrypDecryptMemory;
} VrntMockableFunctions;

typedef struct _VRNTUTIL_VARIANT
{
    union
    {
        LONGLONG llValue;
        DWORD64 qwValue;
        LPWSTR sczValue;
        BYTE encryptionPadding[CRYP_ENCRYPT_MEMORY_SIZE];
    };

    VRNTUTIL_VARIANT_TYPE Type;
    BOOL fValueIsEncrypted;
} VRNTUTIL_VARIANT;

const int VRNTUTIL_VARIANT_HANDLE_BYTES = sizeof(VRNTUTIL_VARIANT);

#define VARIANT_ENCRYPTION_SCOPE CRYPTPROTECTMEMORY_SAME_PROCESS

static void DAPI VrntUninitializeHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant
    );
static HRESULT DAPI VrntGetTypeHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out VRNTUTIL_VARIANT_TYPE* pType
    );
static HRESULT DAPI VrntGetNumericHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out LONGLONG* pllValue
    );
static HRESULT DAPI VrntGetStringHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    );
static HRESULT DAPI VrntGetVersionHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out DWORD64* pqwValue
    );
static HRESULT DAPI VrntSetNumericHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in LONGLONG llValue
    );
static HRESULT DAPI VrntSetStringHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    );
static HRESULT DAPI VrntSetVersionHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in DWORD64 qwValue
    );
static HRESULT DAPI VrntSetValueHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in VRNTUTIL_VARIANT* pValue
    );
static HRESULT DAPI VrntCopyHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pSource,
    __out VRNTUTIL_VARIANT* pTarget
    );
static HRESULT DAPI VrntSetEncryptionHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in BOOL fEncrypt
    );
static HRESULT DAPI VrntEncryptNumberHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in BOOL fEncrypt
    );
static HRESULT DAPI VrntEncryptStringHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in BOOL fEncrypt
    );
static HRESULT DAPI VrntRetrieveDecryptedNumericHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out LONGLONG* pllValue
    );
static HRESULT DAPI VrntRetrieveDecryptedStringHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out LPWSTR* psczValue
    );
static HRESULT DAPI VrntRetrieveDecryptedVersionHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out DWORD64* pqwValue
    );

static void DAPI VrntUninitializeHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant
    )
{
    if (VRNTUTIL_VARIANT_TYPE_STRING == pVariant->Type)
    {
        pFunctions->pfnStrSecureZeroFreeString(pVariant->sczValue);
    }
    SecureZeroMemory(pVariant, sizeof(VRNTUTIL_VARIANT));
}

static HRESULT DAPI VrntGetTypeHelper(
    __in VrntMockableFunctions* /*pFunctions*/,
    __in VRNTUTIL_VARIANT* pVariant,
    __out VRNTUTIL_VARIANT_TYPE* pType
    )
{
    HRESULT hr = S_OK;

    *pType = pVariant->Type;

    return hr;
}

static HRESULT DAPI VrntGetNumericHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    switch (pVariant->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        hr = VrntRetrieveDecryptedNumericHelper(pFunctions, pVariant, pllValue);
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = VrntRetrieveDecryptedStringHelper(pFunctions, pVariant, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = StrStringToInt64(sczValue, 0, pllValue);
            if (FAILED(hr))
            {
                hr = DISP_E_TYPEMISMATCH;
            }
        }
        pFunctions->pfnStrSecureZeroFreeString(sczValue);
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        VrntRetrieveDecryptedVersionHelper(pFunctions, pVariant, (DWORD64*)pllValue);
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

static HRESULT DAPI VrntGetStringHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    DWORD64 qwValue = 0;

    switch (pVariant->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        hr = VrntRetrieveDecryptedNumericHelper(pFunctions, pVariant, &llValue);
        if (SUCCEEDED(hr))
        {
            hr = StrAllocFormattedSecure(psczValue, L"%I64d", llValue);
            ExitOnFailure(hr, "Failed to convert int64 to string.");
        }
        SecureZeroMemory(&llValue, sizeof(llValue));
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = VrntRetrieveDecryptedStringHelper(pFunctions, pVariant, psczValue);
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        hr = VrntRetrieveDecryptedVersionHelper(pFunctions, pVariant, &qwValue);
        if (SUCCEEDED(hr))
        {
            hr = StrAllocFormattedSecure(psczValue, L"%hu.%hu.%hu.%hu",
                (WORD)(qwValue >> 48),
                (WORD)(qwValue >> 32),
                (WORD)(qwValue >> 16),
                (WORD)qwValue);
            ExitOnFailure(hr, "Failed to convert version to string.");
        }
        SecureZeroMemory(&qwValue, sizeof(qwValue));
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

LExit:
    return hr;
}

static HRESULT DAPI VrntGetVersionHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    switch (pVariant->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        hr = VrntRetrieveDecryptedNumericHelper(pFunctions, pVariant, (LONGLONG*)pqwValue);
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = VrntRetrieveDecryptedStringHelper(pFunctions, pVariant, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = FileVersionFromStringEx(sczValue, 0, pqwValue);
            if (FAILED(hr))
            {
                hr = DISP_E_TYPEMISMATCH;
            }
        }
        pFunctions->pfnStrSecureZeroFreeString(sczValue);
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        VrntRetrieveDecryptedVersionHelper(pFunctions, pVariant, pqwValue);
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

static HRESULT DAPI VrntSetNumericHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in LONGLONG llValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncrypt = pVariant->fValueIsEncrypted;

    VrntUninitializeHelper(pFunctions, pVariant);
    pVariant->Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
    pVariant->llValue = llValue;
    hr = VrntSetEncryptionHelper(pFunctions, pVariant, fEncrypt);

    return hr;
}

static HRESULT DAPI VrntSetStringHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncrypt = pVariant->fValueIsEncrypted;

    VrntUninitializeHelper(pFunctions, pVariant);
    pVariant->Type = VRNTUTIL_VARIANT_TYPE_STRING;

    if (wzValue)
    {
        hr = StrAllocString(&pVariant->sczValue, wzValue, cchValue);
        ExitOnFailure(hr, "Failed to copy string.");
    }

    hr = VrntSetEncryptionHelper(pFunctions,pVariant, fEncrypt);

LExit:
    pVariant->fValueIsEncrypted = fEncrypt;

    return hr;
}

static HRESULT DAPI VrntSetVersionHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncrypt = pVariant->fValueIsEncrypted;

    VrntUninitializeHelper(pFunctions, pVariant);
    pVariant->Type = VRNTUTIL_VARIANT_TYPE_VERSION;
    pVariant->qwValue = qwValue;
    hr = VrntSetEncryptionHelper(pFunctions, pVariant, fEncrypt);

    return hr;
}

static HRESULT DAPI VrntSetValueHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in VRNTUTIL_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    DWORD64 qwValue = 0;
    BOOL fEncrypt = pVariant->fValueIsEncrypted;

    VrntUninitializeHelper(pFunctions, pVariant);

    switch (pValue->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NONE:
        break;
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        hr = VrntGetNumericHelper(pFunctions, pValue, &llValue);
        if (SUCCEEDED(hr))
        {
            hr = VrntSetNumericHelper(pFunctions, pVariant, llValue);
        }
        SecureZeroMemory(&llValue, sizeof(llValue));
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = VrntGetStringHelper(pFunctions, pValue, &sczValue);
        if (SUCCEEDED(hr))
        {
            hr = VrntSetStringHelper(pFunctions, pVariant, sczValue, 0);
        }
        pFunctions->pfnStrSecureZeroFreeString(sczValue);
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        hr = VrntGetVersionHelper(pFunctions, pValue, &qwValue);
        if (SUCCEEDED(hr))
        {
            hr = VrntSetVersionHelper(pFunctions, pVariant, qwValue);
        }
        SecureZeroMemory(&qwValue, sizeof(qwValue));
        break;
    default:
        hr = E_INVALIDARG;
    }
    ExitOnFailure(hr, "Failed to copy variant value.");

    hr = VrntSetEncryptionHelper(pFunctions, pVariant, fEncrypt);

LExit:
    pVariant->fValueIsEncrypted = fEncrypt;

    return hr;
}

static HRESULT DAPI VrntCopyHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pSource,
    __out VRNTUTIL_VARIANT* pTarget
    )
{
    HRESULT hr = S_OK;

    VrntUninitializeHelper(pFunctions, pTarget);
    pTarget->fValueIsEncrypted = pSource->fValueIsEncrypted;
    hr = VrntSetValueHelper(pFunctions, pTarget, pSource);

    return hr;
}

static HRESULT DAPI VrntSetEncryptionHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;

    if (pVariant->fValueIsEncrypted == fEncrypt)
    {
        // The requested encryption state is already applied.
        ExitFunction();
    }

    switch (pVariant->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NONE:
        hr = S_OK;
        pVariant->fValueIsEncrypted = fEncrypt;
        break;
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        hr = VrntEncryptNumberHelper(pFunctions, pVariant, fEncrypt);
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = VrntEncryptStringHelper(pFunctions, pVariant, fEncrypt);
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        hr = VrntEncryptNumberHelper(pFunctions, pVariant, fEncrypt);
        break;
    default:
        hr = E_INVALIDARG;
    }

    ExitOnFailure(hr, "Failed to set the variant's encryption state.");

LExit:
    return hr;
}

/********************************************************************
VrntEncryptNumberHelper - If fEncrypt is true then encrypts the
                          (number) value, otherwise decrypts it.

********************************************************************/
static HRESULT DAPI VrntEncryptNumberHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;

    if (fEncrypt)
    {
        hr = pFunctions->pfnCrypEncryptMemory(&pVariant->llValue, sizeof(pVariant->encryptionPadding), VARIANT_ENCRYPTION_SCOPE);
        ExitOnFailure(hr, "CrypEncryptMemory failed.");
    }
    else
    {
        hr = pFunctions->pfnCrypDecryptMemory(&pVariant->llValue, sizeof(pVariant->encryptionPadding), VARIANT_ENCRYPTION_SCOPE);
        ExitOnFailure(hr, "CrypDecryptMemory failed.");
    }

    pVariant->fValueIsEncrypted = fEncrypt;

LExit:
    return hr;
}

/********************************************************************
VrntEncryptStringHelper - If fEncrypt is true then encrypts the
                          (string) value, otherwise decrypts it.

********************************************************************/
static HRESULT DAPI VrntEncryptStringHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in BOOL fEncrypt
    )
{
    HRESULT hr = S_OK;
    SIZE_T cbData = 0;

    if (!pVariant->sczValue)
    {
        pVariant->fValueIsEncrypted = fEncrypt;
        ExitFunction();
    }

    if (fEncrypt)
    {
        hr = CrypReallocForEncryption(reinterpret_cast<LPVOID*>(&pVariant->sczValue), &cbData);
        ExitOnFailure(hr, "Failed to realloc string for encryption.");

        hr = pFunctions->pfnCrypEncryptMemory(pVariant->sczValue, static_cast<DWORD>(cbData), VARIANT_ENCRYPTION_SCOPE);
        ExitOnFailure(hr, "CrypEncryptMemory failed.");
    }
    else
    {
        // Assume that the string was already made the right size.
        cbData = MemSize(pVariant->sczValue);
        AssertSz(0 == cbData % CRYP_ENCRYPT_MEMORY_SIZE, "Illegal size for decryption.");

        hr = pFunctions->pfnCrypDecryptMemory(pVariant->sczValue, static_cast<DWORD>(cbData), VARIANT_ENCRYPTION_SCOPE);
        ExitOnFailure(hr, "CrypDecryptMemory failed.");
    }

    pVariant->fValueIsEncrypted = fEncrypt;

LExit:
    return hr;
}

/********************************************************************
VrntRetrieveDecryptedNumericHelper - Return the numeric value unencrypted,
                                     temporarily decrypting if required.

********************************************************************/
static HRESULT DAPI VrntRetrieveDecryptedNumericHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncrypted = pVariant->fValueIsEncrypted;

    if (fEncrypted)
    {
        hr = VrntEncryptNumberHelper(pFunctions, pVariant, FALSE);
        ExitOnFailure(hr, "Failed to decrypt numeric.");
    }

    *pllValue = pVariant->llValue;

    if (fEncrypted)
    {
        hr = VrntEncryptNumberHelper(pFunctions, pVariant, TRUE);
    }

LExit:
    return hr;
}

/********************************************************************
VrntRetrieveDecryptedStringHelper - Return the string value unencrypted,
                                    temporarily decrypting if required.

********************************************************************/
static HRESULT DAPI VrntRetrieveDecryptedStringHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncrypted = pVariant->fValueIsEncrypted;
    BOOL fReencrypt = FALSE;

    if (!pVariant->sczValue)
    {
        *psczValue = NULL;
        ExitFunction();
    }

    if (fEncrypted)
    {
        hr = VrntEncryptStringHelper(pFunctions, pVariant, FALSE);
        ExitOnFailure(hr, "Failed to decrypt string.");

        fReencrypt = TRUE;
    }

    hr = StrAllocStringSecure(psczValue, pVariant->sczValue, 0);
    ExitOnFailure(hr, "Failed to copy value.");

LExit:
    if (fReencrypt)
    {
        hr = VrntEncryptStringHelper(pFunctions, pVariant, TRUE);
    }

    return hr;
}

/********************************************************************
VrntRetrieveDecryptedVersionHelper - Return the version value unencrypted,
                                     temporarily decrypting if required.

********************************************************************/
static HRESULT DAPI VrntRetrieveDecryptedVersionHelper(
    __in VrntMockableFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __out DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    BOOL fEncrypted = pVariant->fValueIsEncrypted;

    if (fEncrypted)
    {
        hr = VrntEncryptNumberHelper(pFunctions, pVariant, FALSE);
        ExitOnFailure(hr, "Failed to decrypt version.");
    }

    *pqwValue = pVariant->qwValue;

    if (fEncrypted)
    {
        hr = VrntEncryptNumberHelper(pFunctions, pVariant, TRUE);
    }

LExit:
    return hr;
}
