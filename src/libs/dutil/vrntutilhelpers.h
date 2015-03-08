//-------------------------------------------------------------------------------------------------
// <copyright file="vrntutilcore.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

typedef struct _VrntCoreFunctions
{
    PFN_STRSECUREZEROFREESTRING pfnStrSecureZeroFreeString;
} VrntCoreFunctions;

#define vpfnStrSecureZeroFreeString(pwz) { pFunctions->pfnStrSecureZeroFreeString(pwz); }

static void DAPI VrntUninitializeHelper(
    __in VrntCoreFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant
    )
{
    if (VRNTUTIL_VARIANT_TYPE_STRING == pVariant->Type)
    {
        vpfnStrSecureZeroFreeString(pVariant->sczValue);
    }
    SecureZeroMemory(pVariant, sizeof(VRNTUTIL_VARIANT));
}

static HRESULT DAPI VrntGetNumericHelper(
    __in VrntCoreFunctions* /*pFunctions*/,
    __in VRNTUTIL_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;

    switch (pVariant->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        *pllValue = pVariant->llValue;
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = StrStringToInt64(pVariant->sczValue, 0, pllValue);
        if (FAILED(hr))
        {
            hr = DISP_E_TYPEMISMATCH;
        }
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        *pllValue = (LONGLONG)pVariant->qwValue;
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

static HRESULT DAPI VrntGetStringHelper(
    __in VrntCoreFunctions* /*pFunctions*/,
    __in VRNTUTIL_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    switch (pVariant->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        hr = StrAllocFormatted(psczValue, L"%I64d", pVariant->llValue);
        ExitOnFailure(hr, "Failed to convert int64 to string.");
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = StrAllocString(psczValue, pVariant->sczValue, 0);
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        hr = StrAllocFormattedSecure(psczValue, L"%hu.%hu.%hu.%hu",
            (WORD)(pVariant->qwValue >> 48),
            (WORD)(pVariant->qwValue >> 32),
            (WORD)(pVariant->qwValue >> 16),
            (WORD)pVariant->qwValue);
        ExitOnFailure(hr, "Failed to convert version to string.");
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

LExit:
    return hr;
}

static HRESULT DAPI VrntGetVersionHelper(
    __in VrntCoreFunctions* /*pFunctions*/,
    __in VRNTUTIL_VARIANT* pVariant,
    __out DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;

    switch (pVariant->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        *pqwValue = (LONGLONG)pVariant->qwValue;
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = FileVersionFromStringEx(pVariant->sczValue, 0, pqwValue);
        if (FAILED(hr))
        {
            hr = DISP_E_TYPEMISMATCH;
        }
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        *pqwValue = pVariant->qwValue;
        break;
    default:
        hr = E_INVALIDARG;
        break;
    }

    return hr;
}

static HRESULT DAPI VrntSetNumericHelper(
    __in VrntCoreFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in LONGLONG llValue
    )
{
    HRESULT hr = S_OK;

    VrntUninitializeHelper(pFunctions, pVariant);
    pVariant->Type = VRNTUTIL_VARIANT_TYPE_NUMERIC;
    pVariant->llValue = llValue;

    return hr;
}

static HRESULT DAPI VrntSetStringHelper(
    __in VrntCoreFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    )
{
    HRESULT hr = S_OK;

    VrntUninitializeHelper(pFunctions, pVariant);

    if (wzValue)
    {
        pVariant->Type = VRNTUTIL_VARIANT_TYPE_STRING;
        hr = StrAllocString(&pVariant->sczValue, wzValue, cchValue);
    }

    return hr;
}

static HRESULT DAPI VrntSetVersionHelper(
    __in VrntCoreFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;

    VrntUninitializeHelper(pFunctions, pVariant);
    pVariant->Type = VRNTUTIL_VARIANT_TYPE_VERSION;
    pVariant->qwValue = qwValue;

    return hr;
}

static HRESULT DAPI VrntSetValueHelper(
    __in VrntCoreFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pVariant,
    __in VRNTUTIL_VARIANT* pValue
    )
{
    HRESULT hr = S_OK;

    VrntUninitializeHelper(pFunctions, pVariant);

    switch (pValue->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NONE:
        break;
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        hr = VrntSetNumericHelper(pFunctions, pVariant, pValue->llValue);
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = VrntSetStringHelper(pFunctions, pVariant, pValue->sczValue, 0);
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        hr = VrntSetVersionHelper(pFunctions, pVariant, pValue->qwValue);
        break;
    default:
        hr = E_INVALIDARG;
    }

    return hr;
}

static HRESULT DAPI VrntCopyHelper(
    __in VrntCoreFunctions* pFunctions,
    __in VRNTUTIL_VARIANT* pSource,
    __out VRNTUTIL_VARIANT* pTarget
    )
{
    HRESULT hr = S_OK;

    VrntUninitializeHelper(pFunctions, pTarget);

    switch (pSource->Type)
    {
    case VRNTUTIL_VARIANT_TYPE_NONE:
        break;
    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        hr = VrntSetNumericHelper(pFunctions, pTarget, pSource->llValue);
        break;
    case VRNTUTIL_VARIANT_TYPE_STRING:
        hr = VrntSetStringHelper(pFunctions, pTarget, pSource->sczValue, 0);
        break;
    case VRNTUTIL_VARIANT_TYPE_VERSION:
        hr = VrntSetVersionHelper(pFunctions, pTarget, pSource->qwValue);
        break;
    default:
        hr = E_INVALIDARG;
    }

    return hr;
}
