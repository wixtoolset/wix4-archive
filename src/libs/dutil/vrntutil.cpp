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

static VrntCoreFunctions vFunctions = 
{
    StrSecureZeroFreeString
};

DAPI_(void) VrntUninitialize(
    __in VRNTUTIL_VARIANT* pVariant
    )
{
    VrntUninitializeHelper(&vFunctions, pVariant);
}

DAPI_(HRESULT) VrntGetNumeric(
    __in VRNTUTIL_VARIANT* pVariant,
    __out LONGLONG* pllValue
    )
{
    return VrntGetNumericHelper(&vFunctions, pVariant, pllValue);
}

DAPI_(HRESULT) VrntGetString(
    __in VRNTUTIL_VARIANT* pVariant,
    __out_z LPWSTR* psczValue
    )
{
    return VrntGetStringHelper(&vFunctions, pVariant, psczValue);
}

DAPI_(HRESULT) VrntGetVersion(
    __in VRNTUTIL_VARIANT* pVariant,
    __out DWORD64* pqwValue
    )
{
    return VrntGetVersionHelper(&vFunctions, pVariant, pqwValue);
}

DAPI_(HRESULT) VrntSetNumeric(
    __in VRNTUTIL_VARIANT* pVariant,
    __in LONGLONG llValue
    )
{
    return VrntSetNumericHelper(&vFunctions, pVariant, llValue);
}

DAPI_(HRESULT) VrntSetString(
    __in VRNTUTIL_VARIANT* pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    )
{
    return VrntSetStringHelper(&vFunctions, pVariant, wzValue, cchValue);
}

DAPI_(HRESULT) VrntSetVersion(
    __in VRNTUTIL_VARIANT* pVariant,
    __in DWORD64 qwValue
    )
{
    return VrntSetVersionHelper(&vFunctions, pVariant, qwValue);
}

DAPI_(HRESULT) VrntSetValue(
    __in VRNTUTIL_VARIANT* pVariant,
    __in VRNTUTIL_VARIANT* pValue
    )
{
    return VrntSetValueHelper(&vFunctions, pVariant, pValue);
}

DAPI_(HRESULT) VrntCopy(
    __in VRNTUTIL_VARIANT* pSource,
    __out VRNTUTIL_VARIANT* pTarget
    )
{
    return VrntCopyHelper(&vFunctions, pSource, pTarget);
}
