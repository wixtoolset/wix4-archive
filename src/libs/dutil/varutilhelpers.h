//-------------------------------------------------------------------------------------------------
// <copyright file="varutilhelpers.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

// structs

typedef struct _VarMockableFunctions
{
    PFN_LOGSTRINGLINE pfnLogStringLine;
    PFN_ENTERCRITICALSECTION pfnEnterCriticalSection;
    PFN_LEAVECRITICALSECTION pfnLeaveCriticalSection;
} VarMockableFunctions;

typedef struct _VARUTIL_VARIABLE
{
    LPWSTR sczName;
    VRNTUTIL_VARIANT_HANDLE value;
    BOOL fHidden;
    LPVOID pvContext;
} VARUTIL_VARIABLE;

typedef struct _VARIABLES_STRUCT
{
    CRITICAL_SECTION csAccess;
    LONG cVersion;

    DWORD dwMaxVariables;
    DWORD cVariables;
    VARUTIL_VARIABLE* rgVariables;

    PFN_VARIABLENOTFOUND pfnVariableNotFound;
    LPVOID pvVariableNotFoundContext;
} VARIABLES_STRUCT;

typedef struct _VARIABLE_ENUM_STRUCT
{
    LONG cVersion;
    DWORD iPosition;
    VARIABLES_STRUCT* pVariables;
} VARIABLE_ENUM_STRUCT;


// constants

const int VARIABLE_ENUM_HANDLE_BYTES = sizeof(VARIABLE_ENUM_STRUCT);
const int VARIABLES_HANDLE_BYTES = sizeof(VARIABLES_STRUCT);
const DWORD GROW_VARIABLE_ARRAY = 3;


// function declarations

static HRESULT VarCreateHelper(
    __in VarMockableFunctions* pFunctions,
    __in_opt PFN_VARIABLENOTFOUND pfnVariableNotFound,
    __in_opt LPVOID pvVariableNotFoundContext,
    __out VARIABLES_STRUCT** ppVariables
    );
static void VarDestroyHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_opt PFN_FREEVARIABLECONTEXT pfnFreeVariableContext
    );
static void VarFreeEnumValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLE_ENUM_VALUE* pValue
    );
static void VarFreeValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLE_VALUE* pValue
    );
static HRESULT VarEscapeStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    );
static HRESULT VarFormatStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    );
static HRESULT VarFormatStringObfuscatedHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    );
static HRESULT VarGetFormattedHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
static HRESULT VarGetNumericHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    );
static HRESULT VarGetStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
static HRESULT VarGetVersionHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    );
static HRESULT VarGetValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    );
static HRESULT VarGetVariantHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VRNTUTIL_VARIANT_HANDLE pVariant
    );
static HRESULT VarSetNumericHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    );
static HRESULT VarSetStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    );
static HRESULT VarSetVersionHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    );
static HRESULT VarSetValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in VARIABLE_VALUE* pValue,
    __in BOOL fLog
    );
static HRESULT VarSetVariantHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in LPCWSTR wzVariable,
    __in VRNTUTIL_VARIANT_HANDLE pVariant
    );
static HRESULT VarStartEnumHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __out VARIABLE_ENUM_STRUCT** ppEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    );
static HRESULT VarNextVariableHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLE_ENUM_STRUCT* pEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    );
static void VarFinishEnumHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLE_ENUM_STRUCT* pEnum
    );
static HRESULT VarStrAllocHelper(
    __in VarMockableFunctions* pFunctions,
    __in BOOL fZeroOnRealloc,
    __deref_out_ecount_part(cch, 0) LPWSTR* ppwz,
    __in DWORD_PTR cch
    );
static HRESULT VarStrAllocStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in BOOL fZeroOnRealloc,
    __deref_out_ecount_z(cchSource + 1) LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    );
static HRESULT VarStrAllocConcatHelper(
    __in VarMockableFunctions* pFunctions,
    __in BOOL fZeroOnRealloc,
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    );
static HRESULT VarStrAllocFormattedHelper(
    __in VarMockableFunctions* pFunctions,
    __in BOOL fZeroOnRealloc,
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    ...
    );


// internal function declarations

static HRESULT ConvertVarVariableToVarValue(
    __in VarMockableFunctions* pFunctions,
    __in VARUTIL_VARIABLE* pVariable,
    __in VARIABLE_VALUE* pValue
    );
static HRESULT FindVariableIndexByName(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out DWORD* piVariable
    );
static HRESULT FormatString(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut,
    __in BOOL fObfuscateHiddenVariables
    );
static HRESULT GetVariable(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARUTIL_VARIABLE** ppVariable
    );
static HRESULT InsertVariable(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD iPosition
    );
static HRESULT IsVariableHidden(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out BOOL* pfHidden
    );
static HRESULT SetVariableValue(
    __in VarMockableFunctions* pFunctions,
    __in VARUTIL_VARIABLE* pVariable,
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in BOOL fLog
    );


// function definitions

static HRESULT VarCreateHelper(
    __in VarMockableFunctions* pFunctions,
    __in_opt PFN_VARIABLENOTFOUND pfnVariableNotFound,
    __in_opt LPVOID pvVariableNotFoundContext,
    __out VARIABLES_STRUCT** ppVariables
    )
{
    HRESULT hr = S_OK;
    VARIABLES_STRUCT* pVariables = NULL;

    ExitOnNull(ppVariables, hr, E_INVALIDARG, "Handle not specified while creating variables.");

    pVariables = reinterpret_cast<VARIABLES_STRUCT*>(MemAlloc(VARIABLES_HANDLE_BYTES, TRUE));
    ExitOnNull(pVariables, hr, E_OUTOFMEMORY, "Failed to allocate variables object.");

    ::InitializeCriticalSection(&pVariables->csAccess);
    pVariables->pfnVariableNotFound = pfnVariableNotFound;
    pVariables->pvVariableNotFoundContext = pvVariableNotFoundContext;

    *ppVariables = pVariables;
    pVariables = NULL;

LExit:
    if (pVariables)
    {
        VarDestroyHelper(pFunctions, pVariables, NULL);
    }

    return hr;
}

static void VarDestroyHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __in VARIABLES_STRUCT* pVariables,
    __in_opt PFN_FREEVARIABLECONTEXT pfnFreeVariableContext
    )
{
    ::DeleteCriticalSection(&pVariables->csAccess);

    if (pVariables->rgVariables)
    {
        for (DWORD i = 0; i < pVariables->cVariables; ++i)
        {
            VARUTIL_VARIABLE* pVariable = &pVariables->rgVariables[i];
            if (pVariable)
            {
                ReleaseStr(pVariable->sczName);
                VrntUninitialize(pVariable->value);
                MemFree(pVariable->value);

                if (pfnFreeVariableContext && pVariable->pvContext)
                {
                    pfnFreeVariableContext(pVariable->pvContext);
                }
            }
        }
        MemFree(pVariables->rgVariables);
    }

    ReleaseMem(pVariables);
}

static void VarFreeEnumValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLE_ENUM_VALUE* pValue
    )
{
    if (pValue)
    {
        ReleaseStr(pValue->sczName);
        VarFreeValueHelper(pFunctions, &pValue->value);
    }
}

static void VarFreeValueHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __in VARIABLE_VALUE* pValue
    )
{
    if (pValue)
    {
        if (VARIABLE_VALUE_TYPE_STRING == pValue->type)
        {
            ReleaseStr(pValue->sczValue);
        }
        ReleaseMem(pValue);
    }
}

static HRESULT VarEscapeStringHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzRead = NULL;
    LPWSTR pwzEscaped = NULL;
    LPWSTR pwz = NULL;
    SIZE_T i = 0;

    // Allocate buffer for escaped string.
    hr = StrAlloc(&pwzEscaped, lstrlenW(wzIn) + 1);
    ExitOnFailure(hr, "Failed to allocate buffer for escaped string.");

    // Read through string and move characters, inserting escapes as needed.
    wzRead = wzIn;
    for (;;)
    {
        // Find next character needing escaping.
        i = wcscspn(wzRead, L"[]{}");

        // Copy skipped characters.
        if (0 < i)
        {
            hr = StrAllocConcat(&pwzEscaped, wzRead, i);
            ExitOnFailure(hr, "Failed to append characters.");
        }

        if (L'\0' == wzRead[i])
        {
            break; // end reached.
        }

        // Escape character.
        hr = StrAllocFormatted(&pwz, L"[\\%c]", wzRead[i]);
        ExitOnFailure(hr, "Failed to format escape sequence.");

        hr = StrAllocConcat(&pwzEscaped, pwz, 0);
        ExitOnFailure(hr, "Failed to append escape sequence.");

        // Update read pointer.
        wzRead += i + 1;
    }

    // Return value.
    hr = StrAllocString(psczOut, pwzEscaped, 0);
    ExitOnFailure(hr, "Failed to copy string.");

LExit:
    ReleaseStr(pwzEscaped);
    ReleaseStr(pwz);

    return hr;
}

static HRESULT VarFormatStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    )
{
    return FormatString(pFunctions, pVariables, wzIn, psczOut, pcchOut, FALSE);
}

static HRESULT VarFormatStringObfuscatedHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    )
{
    return FormatString(pFunctions, pVariables, wzIn, psczOut, pcchOut, TRUE);
}

static HRESULT VarGetFormattedHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    VARUTIL_VARIABLE* pVariable = NULL;
    VRNTUTIL_VARIANT_TYPE type = VRNTUTIL_VARIANT_TYPE_NONE;
    LPWSTR scz = NULL;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pFunctions, pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get variable: '%ls'", wzVariable);

    hr = VrntGetType(pVariable->value, &type);
    ExitOnFailure(hr, "Failed to get the variable type: %ls", wzVariable);

    if (VRNTUTIL_VARIANT_TYPE_NONE == type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    // Only strings need to get expanded.
    if (VRNTUTIL_VARIANT_TYPE_STRING == type)
    {
        hr = VrntGetString(pVariable->value, &scz);
        ExitOnFailure(hr, "Failed to get unformatted string.");

        hr = VarFormatStringHelper(pFunctions, pVariables, scz, psczValue, NULL);
        ExitOnFailure(hr, "Failed to format value '%ls' of variable: %ls", pVariable->fHidden ? L"*****" : scz, wzVariable);
    }
    else
    {
        hr = VrntGetString(pVariable->value, psczValue);
        ExitOnFailure(hr, "Failed to get value as string for variable: %ls", wzVariable);
    }

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    StrSecureZeroFreeString(scz);

    return hr;
}

static HRESULT VarGetNumericHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;
    VARUTIL_VARIABLE* pVariable = NULL;
    VRNTUTIL_VARIANT_TYPE type = VRNTUTIL_VARIANT_TYPE_NONE;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pFunctions, pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get variable: '%ls'", wzVariable);

    hr = VrntGetType(pVariable->value, &type);
    ExitOnFailure(hr, "Failed to get the variable type: %ls", wzVariable);

    if (VRNTUTIL_VARIANT_TYPE_NONE == type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = VrntGetNumeric(pVariable->value, pllValue);

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

static HRESULT VarGetStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    VARUTIL_VARIABLE* pVariable = NULL;
    VRNTUTIL_VARIANT_TYPE type = VRNTUTIL_VARIANT_TYPE_NONE;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pFunctions, pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get variable: '%ls'", wzVariable);

    hr = VrntGetType(pVariable->value, &type);
    ExitOnFailure(hr, "Failed to get the variable type: %ls", wzVariable);

    if (VRNTUTIL_VARIANT_TYPE_NONE == type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = VrntGetString(pVariable->value, psczValue);

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

static HRESULT VarGetVersionHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    VARUTIL_VARIABLE* pVariable = NULL;
    VRNTUTIL_VARIANT_TYPE type = VRNTUTIL_VARIANT_TYPE_NONE;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pFunctions, pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get variable: '%ls'", wzVariable);

    hr = VrntGetType(pVariable->value, &type);
    ExitOnFailure(hr, "Failed to get the variable type: %ls", wzVariable);

    if (VRNTUTIL_VARIANT_TYPE_NONE == type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = VrntGetVersion(pVariable->value, pqwValue);

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

static HRESULT VarGetValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    )
{
    HRESULT hr = S_OK;
    VARUTIL_VARIABLE* pVariable = NULL;
    VARIABLE_VALUE* pValue = NULL;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pFunctions, pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get variable: '%ls'", wzVariable);

    pValue = reinterpret_cast<VARIABLE_VALUE*>(MemAlloc(sizeof(VARIABLE_VALUE), TRUE));
    ExitOnNull(pValue, hr, E_OUTOFMEMORY, "Failed to allocate memory for VarValue.");

    hr = ConvertVarVariableToVarValue(pFunctions, pVariable, pValue);
    ExitOnFailure(hr, "Failed to copy variable.");

    *ppValue = pValue;
    pValue = NULL;

LExit:
    if (pValue)
    {
        VarFreeValueHelper(pFunctions, pValue);
    }

    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

static HRESULT VarGetVariantHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VRNTUTIL_VARIANT_HANDLE pVariant
    )
{
    HRESULT hr = S_OK;
    VARUTIL_VARIABLE* pVariable = NULL;
    VRNTUTIL_VARIANT_TYPE type = VRNTUTIL_VARIANT_TYPE_NONE;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pFunctions, pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get variable: '%ls'", wzVariable);

    hr = VrntGetType(pVariable->value, &type);
    ExitOnFailure(hr, "Failed to get the variable type: %ls", wzVariable);

    if (VRNTUTIL_VARIANT_TYPE_NONE == type)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = VrntCopy(pVariable->value, pVariant);

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

static HRESULT VarSetNumericHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_HANDLE pVariant = NULL;

    pVariant = MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, TRUE);
    ExitOnNull(pVariant, hr, E_OUTOFMEMORY, "Failed to allocate memory for variant.");

    hr = VrntSetNumeric(pVariant, llValue);
    ExitOnFailure(hr, "Failed to set numeric variant.");

    hr = VarSetVariantHelper(pFunctions, pVariables, wzVariable, pVariant);

LExit:
    if (pVariant)
    {
        VrntUninitialize(pVariant);
        ReleaseMem(pVariant);
    }

    return hr;
}

static HRESULT VarSetStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_HANDLE pVariant = NULL;

    pVariant = MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, TRUE);
    ExitOnNull(pVariant, hr, E_OUTOFMEMORY, "Failed to allocate memory for variant.");

    hr = VrntSetString(pVariant, wzValue, 0);
    ExitOnFailure(hr, "Failed to set string variant.");

    hr = VarSetVariantHelper(pFunctions, pVariables, wzVariable, pVariant);

LExit:
    if (pVariant)
    {
        VrntUninitialize(pVariant);
        ReleaseMem(pVariant);
    }

    return hr;
}

static HRESULT VarSetVersionHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_HANDLE pVariant = NULL;

    pVariant = MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, TRUE);
    ExitOnNull(pVariant, hr, E_OUTOFMEMORY, "Failed to allocate memory for variant.");

    hr = VrntSetVersion(pVariant, qwValue);
    ExitOnFailure(hr, "Failed to set version variant.");

    hr = VarSetVariantHelper(pFunctions, pVariables, wzVariable, pVariant);

LExit:
    if (pVariant)
    {
        VrntUninitialize(pVariant);
        ReleaseMem(pVariant);
    }

    return hr;
}

static HRESULT VarSetValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in VARIABLE_VALUE* pValue,
    __in BOOL fLog
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;
    VARUTIL_VARIABLE* pVariable = NULL;
    VRNTUTIL_VARIANT_HANDLE pVariant = NULL;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    pVariant = MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, FALSE);
    ExitOnNull(pVariant, hr, E_OUTOFMEMORY, "Failed to allocate memory for variant.");

    switch (pValue->type)
    {
    case VARIABLE_VALUE_TYPE_NONE:
        VrntUninitialize(pVariant);
        break;

    case VARIABLE_VALUE_TYPE_NUMERIC:
        hr = VrntSetNumeric(pVariant, pValue->llValue);
        break;

    case VARIABLE_VALUE_TYPE_STRING:
        hr = VrntSetString(pVariant, pValue->sczValue, 0);
        break;

    case VARIABLE_VALUE_TYPE_VERSION:
        hr = VrntSetVersion(pVariant, pValue->qwValue);
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unknown variable type: %u", pValue->type);
        break;
    }
    ExitOnFailure(hr, "Failed to set variant value for variable: %ls", wzVariable);

    hr = FindVariableIndexByName(pFunctions, pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    if (S_FALSE == hr)
    {
        // Don't call pfnVariableNotFound here because we were already given a value.
        hr = InsertVariable(pFunctions, pVariables, wzVariable, iVariable);
        ExitOnFailure(hr, "Failed to insert variable '%ls'.", wzVariable);
    }

    pVariable = pVariables->rgVariables + iVariable;
    pVariable->fHidden = pValue->fHidden;
    pVariable->pvContext = pValue->pvContext;

    hr = SetVariableValue(pFunctions, pVariable, pVariant, fLog);

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    if (pVariant)
    {
        VrntUninitialize(pVariant);
        ReleaseMem(pVariant);
    }

    return hr;
}

static HRESULT VarSetVariantHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in LPCWSTR wzVariable,
    __in VRNTUTIL_VARIANT_HANDLE pVariant
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = FindVariableIndexByName(pFunctions, pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    if (S_FALSE == hr)
    {
        // Don't call pfnVariableNotFound here because we were already given a value.
        hr = InsertVariable(pFunctions, pVariables, wzVariable, iVariable);
        ExitOnFailure(hr, "Failed to insert variable '%ls'.", wzVariable);
    }

    hr = SetVariableValue(pFunctions, pVariables->rgVariables + iVariable, pVariant, TRUE);

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

static HRESULT VarStartEnumHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __out VARIABLE_ENUM_STRUCT** ppEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    )
{
    HRESULT hr = S_OK;
    VARIABLE_ENUM_STRUCT* pEnum = NULL;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    if (!pVariables->cVariables)
    {
        ExitFunction1(hr = E_NOMOREITEMS);
    }

    pEnum = reinterpret_cast<VARIABLE_ENUM_STRUCT*>(MemAlloc(VARIABLE_ENUM_HANDLE_BYTES, TRUE));
    ExitOnNull(pEnum, hr, E_OUTOFMEMORY, "Failed to allocate memory for variable enum struct.");

    pEnum->cVersion = pVariables->cVersion;
    pEnum->iPosition = 0;
    pEnum->pVariables = pVariables;

    hr = VarNextVariableHelper(pFunctions, pEnum, ppValue);
    ExitOnFailure(hr, "Failed to get first variable.");

    *ppEnum = pEnum;
    pEnum = NULL;

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    ReleaseMem(pEnum);

    return hr;
}

static HRESULT VarNextVariableHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLE_ENUM_STRUCT* pEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    )
{
    HRESULT hr = S_OK;
    VARIABLE_ENUM_VALUE* pValue = NULL;
    VARUTIL_VARIABLE* pVariable = NULL;

    pFunctions->pfnEnterCriticalSection(&pEnum->pVariables->csAccess);

    if (pEnum->cVersion != pEnum->pVariables->cVersion)
    {
        ExitFunction1(hr = E_INVALIDSTATE);
    }

    if (pEnum->iPosition == pEnum->pVariables->cVariables)
    {
        ExitFunction1(hr = E_NOMOREITEMS);
    }

    pValue = reinterpret_cast<VARIABLE_ENUM_VALUE*>(MemAlloc(sizeof(VARIABLE_ENUM_VALUE), TRUE));
    ExitOnNull(pValue, hr, E_OUTOFMEMORY, "Failed to allocate memory for variable enum value.");

    pVariable = pEnum->pVariables->rgVariables + pEnum->iPosition;

    hr = ConvertVarVariableToVarValue(pFunctions, pVariable, &pValue->value);
    ExitOnFailure(hr, "Failed to copy variable value for enumeration.");

    hr = StrAllocString(&pValue->sczName, pVariable->sczName, 0);
    ExitOnFailure(hr, "Failed to copy variable name for enumeration.");

    ++pEnum->iPosition;

    *ppValue = pValue;
    pValue = NULL;

LExit:
    pFunctions->pfnLeaveCriticalSection(&pEnum->pVariables->csAccess);

    if (pValue)
    {
        VarFreeEnumValueHelper(pFunctions, pValue);
    }

    return hr;
}

static void VarFinishEnumHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __in VARIABLE_ENUM_STRUCT* pEnum
    )
{
    ReleaseMem(pEnum);
}

static HRESULT VarStrAllocHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __in BOOL fZeroOnRealloc,
    __deref_out_ecount_part(cch, 0) LPWSTR* ppwz,
    __in DWORD_PTR cch
    )
{
    HRESULT hr = S_OK;

    if (fZeroOnRealloc)
    {
        hr = StrAllocSecure(ppwz, cch);
    }
    else
    {
        hr = StrAlloc(ppwz, cch);
    }

    return hr;
}

static HRESULT VarStrAllocStringHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __in BOOL fZeroOnRealloc,
    __deref_out_ecount_z(cchSource + 1) LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    )
{
    HRESULT hr = S_OK;

    if (fZeroOnRealloc)
    {
        hr = StrAllocStringSecure(ppwz, wzSource, cchSource);
    }
    else
    {
        hr = StrAllocString(ppwz, wzSource, cchSource);
    }

    return hr;
}

static HRESULT VarStrAllocConcatHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __in BOOL fZeroOnRealloc,
    __deref_out_z LPWSTR* ppwz,
    __in_z LPCWSTR wzSource,
    __in DWORD_PTR cchSource
    )
{
    HRESULT hr = S_OK;

    if (fZeroOnRealloc)
    {
        hr = StrAllocConcatSecure(ppwz, wzSource, cchSource);
    }
    else
    {
        hr = StrAllocConcat(ppwz, wzSource, cchSource);
    }

    return hr;
}

static HRESULT VarStrAllocFormattedHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __in BOOL fZeroOnRealloc,
    __deref_out_z LPWSTR* ppwz,
    __in __format_string LPCWSTR wzFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    va_start(args, wzFormat);
    if (fZeroOnRealloc)
    {
        hr = StrAllocFormattedArgsSecure(ppwz, wzFormat, args);
    }
    else
    {
        hr = StrAllocFormattedArgs(ppwz, wzFormat, args);
    }
    va_end(args);

    return hr;
}


// internal function definitions

/********************************************************************
ConvertVarVariableToVarValue - copy a variable into a VARIABLE_VALUE,
                               usually for external consumers.

********************************************************************/
static HRESULT ConvertVarVariableToVarValue(
    __in VarMockableFunctions* /*pFunctions*/,
    __in VARUTIL_VARIABLE* pVariable,
    __in VARIABLE_VALUE* pValue
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_TYPE variantType = VRNTUTIL_VARIANT_TYPE_NONE;

    pValue->fHidden = pVariable->fHidden;
    pValue->pvContext = pVariable->pvContext;
    
    hr = VrntGetType(pVariable->value, &variantType);
    ExitOnFailure(hr, "Failed to get the type of variable: %ls", pVariable->sczName);

    switch (variantType)
    {
    case VRNTUTIL_VARIANT_TYPE_NONE:
        pValue->type = VARIABLE_VALUE_TYPE_NONE;
        break;

    case VRNTUTIL_VARIANT_TYPE_NUMERIC:
        pValue->type = VARIABLE_VALUE_TYPE_NUMERIC;
        hr = VrntGetNumeric(pVariable->value, &pValue->llValue);
        ExitOnFailure(hr, "Failed to get the numeric value of variable: %ls", pVariable->sczName);
        break;

    case VRNTUTIL_VARIANT_TYPE_STRING:
        pValue->type = VARIABLE_VALUE_TYPE_STRING;
        hr = VrntGetString(pVariable->value, &pValue->sczValue);
        ExitOnFailure(hr, "Failed to get the string value of variable: %ls", pVariable->sczName);
        break;

    case VRNTUTIL_VARIANT_TYPE_VERSION:
        pValue->type = VARIABLE_VALUE_TYPE_VERSION;
        hr = VrntGetVersion(pVariable->value, &pValue->qwValue);
        ExitOnFailure(hr, "Failed to get the version value of variable: %ls", pVariable->sczName);
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unknown variant type: %u", variantType);
        break;
    }

LExit:
    return hr;
}

/********************************************************************
FindVariableIndexByName - finds the index for the given variable name.
                          If the variable doesn't exist, returns S_FALSE
                          with the index that it should be inserted at.

********************************************************************/
static HRESULT FindVariableIndexByName(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out DWORD* piVariable
    )
{
    HRESULT hr = S_OK;
    DWORD iRangeFirst = 0;
    DWORD cRangeLength = pVariables->cVariables;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    while (cRangeLength)
    {
        // Get variable in middle of range.
        DWORD iPosition = cRangeLength / 2;
        VARUTIL_VARIABLE* pVariable = &pVariables->rgVariables[iRangeFirst + iPosition];

        switch (::CompareStringW(LOCALE_INVARIANT, SORT_STRINGSORT, wzVariable, -1, pVariable->sczName, -1))
        {
        case CSTR_LESS_THAN:
            // Restrict range to elements before the current.
            cRangeLength = iPosition;
            break;
        case CSTR_EQUAL:
            // Found the variable.
            *piVariable = iRangeFirst + iPosition;
            ExitFunction1(hr = S_OK);
        case CSTR_GREATER_THAN:
            // Restrict range to elements after the current.
            iRangeFirst += iPosition + 1;
            cRangeLength -= iPosition + 1;
            break;
        default:
            ExitWithLastError(hr, "Failed to compare strings.");
        }
    }

    *piVariable = iRangeFirst;
    hr = S_FALSE; // variable not found.

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

/********************************************************************
FormatString - formats wzIn using the variables in pVariables.

********************************************************************/
static HRESULT FormatString(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut,
    __in BOOL fObfuscateHiddenVariables
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    LPWSTR sczUnformatted = NULL;
    LPWSTR sczFormat = NULL;
    LPCWSTR wzRead = NULL;
    LPCWSTR wzOpen = NULL;
    LPCWSTR wzClose = NULL;
    LPWSTR scz = NULL;
    LPWSTR* rgVariables = NULL;
    DWORD cVariables = 0;
    DWORD cch = 0;
    BOOL fHidden = FALSE;
    MSIHANDLE hRecord = NULL;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    // Allocate buffer for format string.
    hr = StrAlloc(&sczFormat, lstrlenW(wzIn) + 1);
    ExitOnFailure(hr, "Failed to allocate buffer for format string.");

    // Read out variables from the unformatted string and build a format string.
    wzRead = wzIn;
    for (;;)
    {
        // Scan for opening '['.
        wzOpen = wcschr(wzRead, L'[');
        if (!wzOpen)
        {
            // End reached, append the remainder of the string and end loop.
            hr = VarStrAllocConcatHelper(pFunctions, !fObfuscateHiddenVariables, &sczFormat, wzRead, 0);
            ExitOnFailure(hr, "Failed to append string.");
            break;
        }

        // Scan for closing ']'.
        wzClose = wcschr(wzOpen + 1, L']');
        if (!wzClose)
        {
            // End reached, treat unterminated expander as literal.
            hr = VarStrAllocConcatHelper(pFunctions, !fObfuscateHiddenVariables, &sczFormat, wzRead, 0);
            ExitOnFailure(hr, "Failed to append string.");
            break;
        }
        cch = (DWORD)(wzClose - wzOpen - 1);

        if (0 == cch)
        {
            // Blank, copy all text including the terminator.
            hr = VarStrAllocConcatHelper(pFunctions, !fObfuscateHiddenVariables, &sczFormat, wzRead, (DWORD_PTR)(wzClose - wzRead) + 1);
            ExitOnFailure(hr, "Failed to append string.");
        }
        else
        {
            // Append text preceding expander.
            if (wzOpen > wzRead)
            {
                hr = VarStrAllocConcatHelper(pFunctions, !fObfuscateHiddenVariables, &sczFormat, wzRead, (DWORD_PTR)(wzOpen - wzRead));
                ExitOnFailure(hr, "Failed to append string.");
            }

            // Get variable name.
            hr = VarStrAllocStringHelper(pFunctions, !fObfuscateHiddenVariables, &scz, wzOpen + 1, cch);
            ExitOnFailure(hr, "Failed to get variable name.");

            // Allocate space in variable array.
            if (rgVariables)
            {
                LPVOID pv = MemReAlloc(rgVariables, sizeof(LPWSTR) * (cVariables + 1), TRUE);
                ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to reallocate variable array.");
                rgVariables = (LPWSTR*)pv;
            }
            else
            {
                rgVariables = (LPWSTR*)MemAlloc(sizeof(LPWSTR) * (cVariables + 1), TRUE);
                ExitOnNull(rgVariables, hr, E_OUTOFMEMORY, "Failed to allocate variable array.");
            }

            // Set variable value.
            if (2 <= cch && L'\\' == wzOpen[1])
            {
                // Escape sequence, copy character.
                hr = VarStrAllocStringHelper(pFunctions, !fObfuscateHiddenVariables, &rgVariables[cVariables], &wzOpen[2], 1);
            }
            else
            {
                if (fObfuscateHiddenVariables)
                {
                    hr = IsVariableHidden(pFunctions, pVariables, scz, &fHidden);
                    ExitOnFailure(hr, "Failed to determine variable visibility: '%ls'.", scz);
                }

                if (fHidden)
                {
                    hr = StrAllocString(&rgVariables[cVariables], L"*****", 0);
                }
                else
                {
                    // Get formatted variable value.
                    hr = VarGetFormattedHelper(pFunctions, pVariables, scz, &rgVariables[cVariables]);
                    if (E_NOTFOUND == hr) // variable not found.
                    {
                        hr = StrAllocStringSecure(&rgVariables[cVariables], L"", 0);
                    }
                }
            }
            ExitOnFailure(hr, "Failed to set variable value.");
            ++cVariables;

            // Append placeholder to format string.
            hr = VarStrAllocFormattedHelper(pFunctions, !fObfuscateHiddenVariables, &scz, L"[%d]", cVariables);
            ExitOnFailure(hr, "Failed to format placeholder string.");

            hr = VarStrAllocConcatHelper(pFunctions, !fObfuscateHiddenVariables, &sczFormat, scz, 0);
            ExitOnFailure(hr, "Failed to append placeholder.");
        }

        // Update read pointer.
        wzRead = wzClose + 1;
    }

    // Create record.
    hRecord = ::MsiCreateRecord(cVariables);
    ExitOnNull(hRecord, hr, E_OUTOFMEMORY, "Failed to allocate record.");

    // Set format string.
    er = ::MsiRecordSetStringW(hRecord, 0, sczFormat);
    ExitOnWin32Error(er, hr, "Failed to set record format string.");

    // Copy record fields.
    for (DWORD i = 0; i < cVariables; ++i)
    {
        if (*rgVariables[i]) // not setting if blank.
        {
            er = ::MsiRecordSetStringW(hRecord, i + 1, rgVariables[i]);
            ExitOnWin32Error(er, hr, "Failed to set record string.");
        }
    }

    // Get formatted character count.
    cch = 0;
#pragma prefast(push)
#pragma prefast(disable:6298)
    er = ::MsiFormatRecordW(NULL, hRecord, L"", &cch);
#pragma prefast(pop)
    if (ERROR_MORE_DATA != er)
    {
        ExitOnWin32Error(er, hr, "Failed to get formatted length.");
    }

    // Return formatted string.
    if (psczOut)
    {
        hr = VarStrAllocHelper(pFunctions, !fObfuscateHiddenVariables, &scz, ++cch);
        ExitOnFailure(hr, "Failed to allocate string.");

        er = ::MsiFormatRecordW(NULL, hRecord, scz, &cch);
        ExitOnWin32Error(er, hr, "Failed to format record.");

        hr = VarStrAllocStringHelper(pFunctions, !fObfuscateHiddenVariables, psczOut, scz, 0);
        ExitOnFailure(hr, "Failed to copy string.");
    }

    // Return character count.
    if (pcchOut)
    {
        *pcchOut = cch;
    }

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    if (rgVariables)
    {
        for (DWORD i = 0; i < cVariables; ++i)
        {
            StrSecureZeroFreeString(rgVariables[i]);
        }
        MemFree(rgVariables);
    }

    if (hRecord)
    {
        ::MsiCloseHandle(hRecord);
    }

    StrSecureZeroFreeString(sczUnformatted);
    StrSecureZeroFreeString(sczFormat);
    StrSecureZeroFreeString(scz);

    return hr;
}

/********************************************************************
GetVariable - gets the VARUTIL_VARIABLE for the given variable name.
              If the variable doesn't exist, it will attempt to use
              the VariableNotFound callback.  If it still doesn't
              exist, returns E_NOTFOUND.

********************************************************************/
static HRESULT GetVariable(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARUTIL_VARIABLE** ppVariable
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;
    VARIABLE_VALUE* pValue = NULL;
    VARUTIL_VARIABLE* pVariable = NULL;
    BOOL fLog = FALSE;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = FindVariableIndexByName(pFunctions, pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find index of variable: '%ls'", wzVariable);

    if (S_FALSE == hr)
    {
        if (pVariables->pfnVariableNotFound)
        {
            hr = pVariables->pfnVariableNotFound(wzVariable, pVariables->pvVariableNotFoundContext, &fLog, &pValue);
            ExitOnFailure(hr, "VariableNotFound callback failed.");
        }

        if (S_FALSE == hr)
        {
            ExitFunction1(hr = E_NOTFOUND);
        }
        ExitOnNull(pValue, hr, E_INVALIDARG, "VariableNotFound callback returned null on success.");

        hr = VarSetValueHelper(pFunctions, pVariables, wzVariable, pValue, fLog);
        ExitOnFailure(hr, "Failed to set variable value from VariableNotFound callback: '%ls'", wzVariable);
    }

    pVariable = &pVariables->rgVariables[iVariable];

    *ppVariable = pVariable;

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

/********************************************************************
InsertVariable - inserts the given variable into the internal data structure.

********************************************************************/
static HRESULT InsertVariable(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD iPosition
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    // Ensure there is room in the variable array.
    if (pVariables->cVariables == pVariables->dwMaxVariables)
    {
        hr = ::DWordAdd(pVariables->dwMaxVariables, GROW_VARIABLE_ARRAY, &(pVariables->dwMaxVariables));
        ExitOnRootFailure(hr, "Overflow while growing variable array size.");

        if (pVariables->rgVariables)
        {
            hr = ::SizeTMult(sizeof(VARUTIL_VARIABLE), pVariables->dwMaxVariables, &cbAllocSize);
            ExitOnRootFailure(hr, "Overflow while calculating size of variable array buffer.");

            LPVOID pv = MemReAlloc(pVariables->rgVariables, cbAllocSize, FALSE);
            ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate room for more variables.");

            // Prefast claims it's possible to hit this. Putting the check in just in case.
            if (pVariables->dwMaxVariables < pVariables->cVariables)
            {
                hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
                ExitOnRootFailure(hr, "Overflow while dealing with variable array buffer allocation.");
            }

            pVariables->rgVariables = reinterpret_cast<VARUTIL_VARIABLE*>(pv);
            memset(&pVariables->rgVariables[pVariables->cVariables], 0, sizeof(VARUTIL_VARIABLE) * (pVariables->dwMaxVariables - pVariables->cVariables));
        }
        else
        {
            pVariables->rgVariables = reinterpret_cast<VARUTIL_VARIABLE*>(MemAlloc(sizeof(VARUTIL_VARIABLE) * pVariables->dwMaxVariables, TRUE));
            ExitOnNull(pVariables->rgVariables, hr, E_OUTOFMEMORY, "Failed to allocate room for variables.");
        }
    }

    // Move variables.
    if (0 < pVariables->cVariables - iPosition)
    {
        memmove(&pVariables->rgVariables[iPosition + 1], &pVariables->rgVariables[iPosition], sizeof(VARUTIL_VARIABLE) * (pVariables->cVariables - iPosition));
        memset(&pVariables->rgVariables[iPosition], 0, sizeof(VARUTIL_VARIABLE));
    }

    ++pVariables->cVariables;
    ++pVariables->cVersion;

    // Allocate name.
    hr = StrAllocString(&(pVariables->rgVariables[iPosition].sczName), wzVariable, 0);
    ExitOnFailure(hr, "Failed to copy variable name.");

    // Allocate value.
    pVariables->rgVariables[iPosition].value = reinterpret_cast<VRNTUTIL_VARIANT_HANDLE>(MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, TRUE));
    ExitOnNull(pVariables->rgVariables[iPosition].value, hr, E_OUTOFMEMORY, "Failed to allocate memory for variant.");

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

/********************************************************************
IsVariableHidden - returns whether the given variable is hidden.

********************************************************************/
static HRESULT IsVariableHidden(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out BOOL* pfHidden
    )
{
    HRESULT hr = S_OK;
    VARUTIL_VARIABLE* pVariable = NULL;

    pFunctions->pfnEnterCriticalSection(&pVariables->csAccess);

    hr = GetVariable(pFunctions, pVariables, wzVariable, &pVariable);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get variable: '%ls'", wzVariable);
    }

    if (E_NOTFOUND == hr)
    {
        // A missing variable does not need its data hidden.
        *pfHidden = FALSE;

        hr = S_OK;
    }
    else
    {
        *pfHidden = pVariable->fHidden;
    }

LExit:
    pFunctions->pfnLeaveCriticalSection(&pVariables->csAccess);

    return hr;
}

/********************************************************************
SetVariableValue - sets the value of the given variable, logging appropriately.

********************************************************************/
static HRESULT SetVariableValue(
    __in VarMockableFunctions* pFunctions,
    __in VARUTIL_VARIABLE* pVariable,
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in BOOL fLog
    )
{
    HRESULT hr = S_OK;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    DWORD64 qwValue = 0;
    VRNTUTIL_VARIANT_TYPE variantType;

    if (fLog)
    {
        if (pVariable->fHidden)
        {
            pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting hidden variable '%ls'", pVariable->sczName);
            hr = VrntSetValue(pVariable->value, pVariant);
        }
        else
        {
            hr = VrntGetType(pVariant, &variantType);
            ExitOnFailure(hr, "Failed to get variant type.");

            switch (variantType)
            {
            case VRNTUTIL_VARIANT_TYPE_NONE:
                pFunctions->pfnLogStringLine(REPORT_STANDARD, "Unsetting variable '%ls'", pVariable->sczName);

                VrntUninitialize(pVariable->value);
                break;

            case VRNTUTIL_VARIANT_TYPE_NUMERIC:
                hr = VrntGetNumeric(pVariant, &llValue);
                ExitOnFailure(hr, "Failed to get variant numeric value.");

                pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting numeric variable '%ls' to value %lld", pVariable->sczName, llValue);

                hr = VrntSetNumeric(pVariable->value, llValue);
                break;

            case VRNTUTIL_VARIANT_TYPE_STRING:
                hr = VrntGetString(pVariant, &sczValue);
                ExitOnFailure(hr, "Failed to get variant string value.");

                if (!sczValue)
                {
                    pFunctions->pfnLogStringLine(REPORT_STANDARD, "Unsetting variable '%ls'", pVariable->sczName);
                }
                else
                {
                    pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting string variable '%ls' to value '%ls'", pVariable->sczName, sczValue);
                }

                hr = VrntSetString(pVariable->value, sczValue, 0);
                break;

            case VRNTUTIL_VARIANT_TYPE_VERSION:
                hr = VrntGetVersion(pVariant, &qwValue);
                ExitOnFailure(hr, "Failed to get variant version value.");

                pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting version variable '%ls' to value '%hu.%hu.%hu.%hu'", pVariable->sczName, (WORD)(qwValue >> 48), (WORD)(qwValue >> 32), (WORD)(qwValue >> 16), (WORD)(qwValue));

                hr = VrntSetVersion(pVariable->value, qwValue);
                break;

            default:
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Unknown variant type: %u", variantType);
                break;
            }
        }
    }
    else
    {
        hr = VrntSetValue(pVariable->value, pVariant);
    }
    ExitOnFailure(hr, "Failed to set value of variable: %ls", pVariable->sczName);

LExit:
    SecureZeroMemory(&llValue, sizeof(LONGLONG));
    SecureZeroMemory(&qwValue, sizeof(DWORD64));
    StrSecureZeroFreeString(sczValue);

    if (FAILED(hr) && fLog)
    {
        pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting variable failed: ID '%ls', HRESULT 0x%x", pVariable->sczName, hr);
    }

    return hr;
}
