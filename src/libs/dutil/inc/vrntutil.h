//-------------------------------------------------------------------------------------------------
// <copyright file="vrntutil.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

#if defined(__cplusplus)
extern "C" {
#endif

enum VRNTUTIL_VARIANT_TYPE
{
    VRNTUTIL_VARIANT_TYPE_NONE,
    VRNTUTIL_VARIANT_TYPE_NUMERIC,
    VRNTUTIL_VARIANT_TYPE_STRING,
    VRNTUTIL_VARIANT_TYPE_VERSION,
};

typedef void* VRNTUTIL_VARIANT_HANDLE;

extern const int VRNTUTIL_VARIANT_HANDLE_BYTES;

/********************************************************************
VrntUninitialize - resets the variant and frees any memory that it's using.

********************************************************************/
void DAPI VrntUninitialize(
    __in VRNTUTIL_VARIANT_HANDLE pVariant
    );

/********************************************************************
VrntGetNumeric - returns the current value in numeric form.
                 If the current type is not numeric,
                 it will attempt to convert it.

********************************************************************/
HRESULT DAPI VrntGetNumeric(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __out LONGLONG* pllValue
    );

/********************************************************************
VrntGetString - returns the current value in string form.
                If the current type is not string,
                it will attempt to convert it.

********************************************************************/
HRESULT DAPI VrntGetString(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __out_z LPWSTR* psczValue
    );

/********************************************************************
VrntGetVersion - returns the current value in version form.
                 If the current type is not version,
                 it will attempt to convert it.

********************************************************************/
HRESULT DAPI VrntGetVersion(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __out DWORD64* pqwValue
    );

/********************************************************************
VrntSetNumeric - sets the type to numeric and sets the value.

********************************************************************/
HRESULT DAPI VrntSetNumeric(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in LONGLONG llValue
    );

/********************************************************************
VrntSetString - sets the type to string and sets the value.

NOTE: cchSource does not have to equal the length of wzValue
NOTE: if cchSource == 0, length of wzValue is used instead
********************************************************************/
HRESULT DAPI VrntSetString(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in_z_opt LPCWSTR wzValue,
    __in DWORD_PTR cchValue
    );

/********************************************************************
VrntSetVersion - sets the type to version and sets the value.

********************************************************************/
HRESULT DAPI VrntSetVersion(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in DWORD64 qwValue
    );

/********************************************************************
VrntSetValue - Convenience function that calls VrntUninitialize,
               VrntSetNumeric, VrntSetString, or VrntSetVersion
               based on the type of pValue.
               The encryption state of pVariant is preserved.

********************************************************************/
HRESULT DAPI VrntSetValue(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in VRNTUTIL_VARIANT_HANDLE pValue
    );

/********************************************************************
VrntCopy - creates a copy of pSource.
           The encryption state of pTarget is set to
           the encryption state of pSource.

********************************************************************/
HRESULT DAPI VrntCopy(
    __in VRNTUTIL_VARIANT_HANDLE pSource,
    __out VRNTUTIL_VARIANT_HANDLE pTarget
    );

/********************************************************************
VrntSetEncryption - sets the encryption state of pVariant.
                    If the encryption state matches the requested state,
                    this function does nothing.

********************************************************************/
HRESULT DAPI VrntSetEncryption(
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in BOOL fEncrypt
    );

#if defined(__cplusplus)
}
#endif
