//-------------------------------------------------------------------------------------------------
// <copyright file="varutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include "varutilhelpers.h"

static VarMockableFunctions vFunctions =
{
    LogStringLine,
};

// function definitions

DAPI_(HRESULT) VarCreate(
    __out_bcount(VARIABLES_HANDLE_BYTES) VARIABLES_HANDLE* ppVariables
    )
{
    return VarCreateHelper(&vFunctions, reinterpret_cast<VARIABLES_STRUCT**>(ppVariables));
}

DAPI_(void) VarDestroy(
    __in_bcount(VARIABLES_HANDLE_BYTES) VARIABLES_HANDLE pVariables,
    __in_opt PFN_FREEVARIABLECONTEXT pfnFreeVariableContext
    )
{
    VarDestroyHelper(&vFunctions, reinterpret_cast<VARIABLES_STRUCT*>(pVariables), pfnFreeVariableContext);
}

DAPI_(void) VarFreeEnumValue(
    __in VARIABLE_ENUM_VALUE* pValue
    )
{
    VarFreeEnumValueHelper(&vFunctions, pValue);
}

DAPI_(void) VarFreeValue(
    __in VARIABLE_VALUE* pValue
    )
{
    VarFreeValueHelper(&vFunctions, pValue);
}

DAPI_(HRESULT) VarEscapeString(
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    )
{
    return VarEscapeStringHelper(&vFunctions, wzIn, psczOut);
}

DAPI_(HRESULT) VarFormatString(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    )
{
    return VarFormatStringHelper(&vFunctions, reinterpret_cast<C_VARIABLES_STRUCT*>(pVariables), wzIn, psczOut, pcchOut);
}

DAPI_(HRESULT) VarGetFormatted(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    return VarGetFormattedHelper(&vFunctions, reinterpret_cast<C_VARIABLES_STRUCT*>(pVariables), wzVariable, psczValue);
}

DAPI_(HRESULT) VarGetNumeric(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    return VarGetNumericHelper(&vFunctions, reinterpret_cast<C_VARIABLES_STRUCT*>(pVariables), wzVariable, pllValue);
}

DAPI_(HRESULT) VarGetString(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    return VarGetStringHelper(&vFunctions, reinterpret_cast<C_VARIABLES_STRUCT*>(pVariables), wzVariable, psczValue);
}

DAPI_(HRESULT) VarGetVersion(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    )
{
    return VarGetVersionHelper(&vFunctions, reinterpret_cast<C_VARIABLES_STRUCT*>(pVariables), wzVariable, pqwValue);
}

DAPI_(HRESULT) VarGetValue(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    )
{
    return VarGetValueHelper(&vFunctions, reinterpret_cast<C_VARIABLES_STRUCT*>(pVariables), wzVariable, ppValue);
}

DAPI_(HRESULT) VarSetNumeric(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    )
{
    return VarSetNumericHelper(&vFunctions, reinterpret_cast<VARIABLES_STRUCT*>(pVariables), wzVariable, llValue);
}

DAPI_(HRESULT) VarSetString(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    )
{
    return VarSetStringHelper(&vFunctions, reinterpret_cast<VARIABLES_STRUCT*>(pVariables), wzVariable, wzValue);
}

DAPI_(HRESULT) VarSetVersion(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    )
{
    return VarSetVersionHelper(&vFunctions, reinterpret_cast<VARIABLES_STRUCT*>(pVariables), wzVariable, qwValue);
}

DAPI_(HRESULT) VarSetValue(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in VARIABLE_VALUE* pValue
    )
{
    return VarSetValueHelper(&vFunctions, reinterpret_cast<VARIABLES_STRUCT*>(pVariables), wzVariable, pValue);
}

DAPI_(HRESULT) VarStartEnum(
    __in C_VARIABLES_HANDLE pVariables,
    __out_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE* ppEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    )
{
    return VarStartEnumHelper(&vFunctions, reinterpret_cast<C_VARIABLES_STRUCT*>(pVariables), reinterpret_cast<VARIABLE_ENUM_STRUCT**>(ppEnum), ppValue);
}

DAPI_(HRESULT) VarNextVariable(
    __in_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE pEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    )
{
    return VarNextVariableHelper(&vFunctions, reinterpret_cast<VARIABLE_ENUM_STRUCT*>(pEnum), ppValue);
}

DAPI_(void) VarFinishEnum(
    __in_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE pEnum
    )
{
    VarFinishEnumHelper(&vFunctions, reinterpret_cast<VARIABLE_ENUM_STRUCT*>(pEnum));
}
