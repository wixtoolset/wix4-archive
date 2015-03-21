//-------------------------------------------------------------------------------------------------
// <copyright file="vrntutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include "vrntutilhelpers.h"

static VrntMockableFunctions vFunctions = 
{
    StrSecureZeroFreeString,
    CrypEncryptMemory,
    CrypDecryptMemory,
};

DAPI_(void) VrntUninitialize(
    __in VRNTUTIL_VARIANT_HANDLE pVariant
    )
{
    VrntUninitializeHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant);
}

DAPI_(HRESULT) VrntGetType(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __out VRNTUTIL_VARIANT_TYPE* pType
    )
{
    return VrntGetTypeHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, pType);
}

DAPI_(HRESULT) VrntGetNumeric(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __out LONGLONG* pllValue
    )
{
    return VrntGetNumericHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, pllValue);
}

DAPI_(HRESULT) VrntGetString(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __out_z LPWSTR* psczValue
    )
{
    return VrntGetStringHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, psczValue);
}

DAPI_(HRESULT) VrntGetVersion(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __out DWORD64* pqwValue
    )
{
    return VrntGetVersionHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, pqwValue);
}

DAPI_(HRESULT) VrntSetNumeric(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in LONGLONG llValue
    )
{
    return VrntSetNumericHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, llValue);
}

DAPI_(HRESULT) VrntSetString(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    )
{
    return VrntSetStringHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, wzValue, cchValue);
}

DAPI_(HRESULT) VrntSetVersion(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in DWORD64 qwValue
    )
{
    return VrntSetVersionHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, qwValue);
}

DAPI_(HRESULT) VrntSetValue(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in VRNTUTIL_VARIANT_HANDLE pValue
    )
{
    return VrntSetValueHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, (VRNTUTIL_VARIANT*)pValue);
}

DAPI_(HRESULT) VrntCopy(
    __in VRNTUTIL_VARIANT_HANDLE pSource,
    __out VRNTUTIL_VARIANT_HANDLE pTarget
    )
{
    return VrntCopyHelper(&vFunctions, (VRNTUTIL_VARIANT*)pSource, (VRNTUTIL_VARIANT*)pTarget);
}

DAPI_(HRESULT) VrntSetEncryption(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in BOOL fEncrypt
    )
{
    return VrntSetEncryptionHelper(&vFunctions, (VRNTUTIL_VARIANT*)pVariant, fEncrypt);
}
