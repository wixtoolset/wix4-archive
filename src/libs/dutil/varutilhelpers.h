//-------------------------------------------------------------------------------------------------
// <copyright file="varutilhelpers.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

typedef struct _VarMockableFunctions
{
    PFN_LOGSTRINGLINE pfnLogStringLine;
} VarMockableFunctions;

typedef struct _VARUTIL_VARIABLE
{
    LPWSTR sczName;
    VRNTUTIL_VARIANT_HANDLE value;
    BOOL fHidden;
    LPVOID pvContext;
} VARUTIL_VARIABLE;

typedef struct _VARIABLE_ENUM_STRUCT
{
} VARIABLE_ENUM_STRUCT;

typedef struct _VARIABLES_STRUCT
{
    CRITICAL_SECTION csAccess;
    DWORD dwMaxVariables;
    DWORD cVariables;
    VARUTIL_VARIABLE* rgVariables;
} VARIABLES_STRUCT;

typedef const VARIABLES_STRUCT C_VARIABLES_STRUCT;

const int VARIABLE_ENUM_HANDLE_BYTES = sizeof(VARIABLE_ENUM_STRUCT);
const int VARIABLES_HANDLE_BYTES = sizeof(VARIABLES_STRUCT);
const DWORD GROW_VARIABLE_ARRAY = 3;

static HRESULT VarCreateHelper(
    __in VarMockableFunctions* pFunctions,
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
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    );
static HRESULT VarGetFormattedHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
static HRESULT VarGetNumericHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    );
static HRESULT VarGetStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
static HRESULT VarGetVersionHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    );
static HRESULT VarGetValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
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
    __in VARIABLE_VALUE* pValue
    );
static HRESULT VarSetVrntHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in LPCWSTR wzVariable,
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in BOOL fLog,
    __out_opt VARUTIL_VARIABLE** ppVariable
    );
static HRESULT VarStartEnumHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
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
static HRESULT FindVariableIndexByName(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out DWORD* piVariable
    );
static HRESULT InsertVariable(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD iPosition
    );

static HRESULT VarCreateHelper(
    __in VarMockableFunctions* /*pFunctions*/,
    __out VARIABLES_STRUCT** ppVariables
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(ppVariables, hr, E_INVALIDARG, "Handle not specified while creating variables.");

    *ppVariables = reinterpret_cast<VARIABLES_STRUCT*>(MemAlloc(VARIABLES_HANDLE_BYTES, TRUE));
    ExitOnNull(*ppVariables, hr, E_OUTOFMEMORY, "Failed to allocate variables object.");

    ::InitializeCriticalSection(&(*ppVariables)->csAccess);

LExit:
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
    }
}

static HRESULT VarEscapeStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    )
{
    UNREFERENCED_PARAMETER(pFunctions);
    UNREFERENCED_PARAMETER(wzIn);
    UNREFERENCED_PARAMETER(psczOut);
    return E_NOTIMPL;
}

static HRESULT VarFormatStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    )
{
    UNREFERENCED_PARAMETER(pFunctions);
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzIn);
    UNREFERENCED_PARAMETER(psczOut);
    UNREFERENCED_PARAMETER(pcchOut);
    return E_NOTIMPL;
}

static HRESULT VarGetFormattedHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    UNREFERENCED_PARAMETER(pFunctions);
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(psczValue);
    return E_NOTIMPL;
}

static HRESULT VarGetNumericHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    hr = FindVariableIndexByName(pFunctions, pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    if (S_FALSE == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = VrntGetNumeric(pVariables->rgVariables[iVariable].value, pllValue);

LExit:
    return hr;
}

static HRESULT VarGetStringHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    hr = FindVariableIndexByName(pFunctions, pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    if (S_FALSE == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = VrntGetString(pVariables->rgVariables[iVariable].value, psczValue);

LExit:
    return hr;
}

static HRESULT VarGetVersionHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    hr = FindVariableIndexByName(pFunctions, pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    if (S_FALSE == hr)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = VrntGetVersion(pVariables->rgVariables[iVariable].value, pqwValue);

LExit:
    return hr;
}

static HRESULT VarGetValueHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pFunctions);
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
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

    hr = VarSetVrntHelper(pFunctions, pVariables, wzVariable, pVariant, TRUE, NULL);

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

    hr = VarSetVrntHelper(pFunctions, pVariables, wzVariable, pVariant, TRUE, NULL);

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

    hr = VarSetVrntHelper(pFunctions, pVariables, wzVariable, pVariant, TRUE, NULL);

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
    __in VARIABLE_VALUE* pValue
    )
{
    UNREFERENCED_PARAMETER(pFunctions);
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(pValue);
    return E_NOTIMPL;
}

static HRESULT VarSetVrntHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLES_STRUCT* pVariables,
    __in LPCWSTR wzVariable,
    __in VRNTUTIL_VARIANT_HANDLE pVariant,
    __in BOOL fLog,
    __out_opt VARUTIL_VARIABLE** ppVariable
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    DWORD64 qwValue = 0;
    VARUTIL_VARIABLE* pVariable;
    VRNTUTIL_VARIANT_TYPE variantType;

    hr = FindVariableIndexByName(pFunctions, pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    if (S_FALSE == hr)
    {
        hr = InsertVariable(pFunctions, pVariables, wzVariable, iVariable);
        ExitOnFailure(hr, "Failed to insert variable '%ls'.", wzVariable);
    }

    pVariable = pVariables->rgVariables + iVariable;

    if (fLog)
    {
        if (pVariable->fHidden)
        {
            pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting hidden variable '%ls'", wzVariable);
        }
        else
        {
            hr = VrntGetType(pVariant, &variantType);
            ExitOnFailure(hr, "Failed to get variant type.");

            switch (variantType)
            {
            case VRNTUTIL_VARIANT_TYPE_NONE:
                pFunctions->pfnLogStringLine(REPORT_STANDARD, "Unsetting variable '%ls'", wzVariable);

                VrntUninitialize(pVariable->value);
                break;

            case VRNTUTIL_VARIANT_TYPE_NUMERIC:
                hr = VrntGetNumeric(pVariant, &llValue);
                ExitOnFailure(hr, "Failed to get variant numeric value.");

                pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting numeric variable '%ls' to value %lld", wzVariable, llValue);

                hr = VrntSetNumeric(pVariable->value, llValue);
                break;

            case VRNTUTIL_VARIANT_TYPE_STRING:
                hr = VrntGetString(pVariant, &sczValue);
                ExitOnFailure(hr, "Failed to get variant string value.");

                if (!sczValue)
                {
                    pFunctions->pfnLogStringLine(REPORT_STANDARD, "Unsetting variable '%ls'", wzVariable);
                }
                else
                {
                    pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting string variable '%ls' to value '%ls'", wzVariable, sczValue);
                }

                hr = VrntSetString(pVariable->value, sczValue, 0);
                break;

            case VRNTUTIL_VARIANT_TYPE_VERSION:
                hr = VrntGetVersion(pVariant, &qwValue);
                ExitOnFailure(hr, "Failed to get variant version value.");

                pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting version variable '%ls' to value '%hu.%hu.%hu.%hu'", wzVariable, (WORD)(qwValue >> 48), (WORD)(qwValue >> 32), (WORD)(qwValue >> 16), (WORD)(qwValue));

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
    ExitOnFailure(hr, "Failed to set value of variable: %ls", wzVariable);

    if (ppVariable)
    {
        *ppVariable = pVariable;
    }

LExit:
    SecureZeroMemory(&llValue, sizeof(LONGLONG));
    SecureZeroMemory(&qwValue, sizeof(DWORD64));
    StrSecureZeroFreeString(sczValue);

    if (FAILED(hr) && fLog)
    {
        pFunctions->pfnLogStringLine(REPORT_STANDARD, "Setting variable failed: ID '%ls', HRESULT 0x%x", wzVariable, hr);
    }

    return hr;
}

static HRESULT VarStartEnumHelper(
    __in VarMockableFunctions* pFunctions,
    __in C_VARIABLES_STRUCT* pVariables,
    __out VARIABLE_ENUM_STRUCT** ppEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pFunctions);
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(ppEnum);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
}

static HRESULT VarNextVariableHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLE_ENUM_STRUCT* pEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pFunctions);
    UNREFERENCED_PARAMETER(pEnum);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
}

static void VarFinishEnumHelper(
    __in VarMockableFunctions* pFunctions,
    __in VARIABLE_ENUM_STRUCT* pEnum
    )
{
    UNREFERENCED_PARAMETER(pFunctions);
    UNREFERENCED_PARAMETER(pEnum);
}

static HRESULT FindVariableIndexByName(
    __in VarMockableFunctions* /*pFunctions*/,
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out DWORD* piVariable
    )
{
    HRESULT hr = S_OK;
    DWORD iRangeFirst = 0;
    DWORD cRangeLength = pVariables->cVariables;

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
    return hr;
}

static HRESULT InsertVariable(
    __in VarMockableFunctions* /*pFunctions*/,
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD iPosition
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;

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

    // Allocate name.
    hr = StrAllocString(&(pVariables->rgVariables[iPosition].sczName), wzVariable, 0);
    ExitOnFailure(hr, "Failed to copy variable name.");

    // Allocate value.
    pVariables->rgVariables[iPosition].value = reinterpret_cast<VRNTUTIL_VARIANT_HANDLE>(MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, TRUE));
    ExitOnNull(pVariables->rgVariables[iPosition].value, hr, E_OUTOFMEMORY, "Failed to allocate memory for variant.");

LExit:
    return hr;
}
