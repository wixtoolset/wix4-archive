//-------------------------------------------------------------------------------------------------
// <copyright file="varutilhelpers.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

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
    __out VARIABLES_STRUCT** ppVariables
    );
static void VarDestroyHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_opt PFN_FREEVARIABLECONTEXT pfnFreeVariableContext
    );
static void VarFreeEnumValueHelper(
    __in VARIABLE_ENUM_VALUE* pValue
    );
static void VarFreeValueHelper(
    __in VARIABLE_VALUE* pValue
    );
static HRESULT VarEscapeStringHelper(
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    );
static HRESULT VarFormatStringHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    );
static HRESULT VarGetFormattedHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
static HRESULT VarGetNumericHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    );
static HRESULT VarGetStringHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
static HRESULT VarGetVersionHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    );
static HRESULT VarGetValueHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    );
static HRESULT VarSetNumericHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    );
static HRESULT VarSetStringHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    );
static HRESULT VarSetVersionHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    );
static HRESULT VarSetValueHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in VARIABLE_VALUE* pValue
    );
static HRESULT VarStartEnumHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __out VARIABLE_ENUM_STRUCT** ppEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    );
static HRESULT VarNextVariableHelper(
    __in VARIABLE_ENUM_STRUCT* pEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    );
static void VarFinishEnumHelper(
    __in VARIABLE_ENUM_STRUCT* pEnum
    );
static HRESULT FindVariableIndexByName(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out DWORD* piVariable
    );
static HRESULT ForceGetVariant(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VRNTUTIL_VARIANT_HANDLE* ppVariant
    );
static HRESULT InsertVariable(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD iPosition
    );

static HRESULT VarCreateHelper(
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
    __in VARIABLE_ENUM_VALUE* pValue
    )
{
    if (pValue)
    {
        ReleaseStr(pValue->sczName);
        VarFreeValueHelper(&pValue->value);
    }
}

static void VarFreeValueHelper(
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
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    )
{
    UNREFERENCED_PARAMETER(wzIn);
    UNREFERENCED_PARAMETER(psczOut);
    return E_NOTIMPL;
}

static HRESULT VarFormatStringHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzIn);
    UNREFERENCED_PARAMETER(psczOut);
    UNREFERENCED_PARAMETER(pcchOut);
    return E_NOTIMPL;
}

static HRESULT VarGetFormattedHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(psczValue);
    return E_NOTIMPL;
}

static HRESULT VarGetNumericHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    hr = FindVariableIndexByName(pVariables, wzVariable, &iVariable);
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
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    hr = FindVariableIndexByName(pVariables, wzVariable, &iVariable);
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
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    hr = FindVariableIndexByName(pVariables, wzVariable, &iVariable);
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
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
}

static HRESULT VarSetNumericHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_HANDLE pVariant = NULL;

    hr = ForceGetVariant(pVariables, wzVariable, &pVariant);
    ExitOnFailure(hr, "Failed to get variable '&ls' variant.", wzVariable);

    hr = VrntSetNumeric(pVariant, llValue);

LExit:
    return hr;
}

static HRESULT VarSetStringHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_HANDLE pVariant = NULL;

    hr = ForceGetVariant(pVariables, wzVariable, &pVariant);
    ExitOnFailure(hr, "Failed to get variable '&ls' variant.", wzVariable);

    hr = VrntSetString(pVariant, wzValue, 0);

LExit:
    return hr;
}

static HRESULT VarSetVersionHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_HANDLE pVariant = NULL;

    hr = ForceGetVariant(pVariables, wzVariable, &pVariant);
    ExitOnFailure(hr, "Failed to get variable '&ls' variant.", wzVariable);

    hr = VrntSetVersion(pVariant, qwValue);

LExit:
    return hr;
}

static HRESULT VarSetValueHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in VARIABLE_VALUE* pValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(pValue);
    return E_NOTIMPL;
}

static HRESULT VarStartEnumHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __out VARIABLE_ENUM_STRUCT** ppEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(ppEnum);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
}

static HRESULT VarNextVariableHelper(
    __in VARIABLE_ENUM_STRUCT* pEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pEnum);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
}

static void VarFinishEnumHelper(
    __in VARIABLE_ENUM_STRUCT* pEnum
    )
{
    UNREFERENCED_PARAMETER(pEnum);
}

static HRESULT FindVariableIndexByName(
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

static HRESULT ForceGetVariant(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __out VRNTUTIL_VARIANT_HANDLE* ppVariant
    )
{
    HRESULT hr = S_OK;
    DWORD iVariable = 0;

    hr = FindVariableIndexByName(pVariables, wzVariable, &iVariable);
    ExitOnFailure(hr, "Failed to find variable value '%ls'.", wzVariable);

    if (S_FALSE == hr)
    {
        hr = InsertVariable(pVariables, wzVariable, iVariable);
        ExitOnFailure(hr, "Failed to insert variable '%ls'.", wzVariable);
    }

    *ppVariant = pVariables->rgVariables[iVariable].value;

LExit:
    return hr;
}

static HRESULT InsertVariable(
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
