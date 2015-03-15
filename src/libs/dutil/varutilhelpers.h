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
                VrntUninitialize(&pVariable->value);

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
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(pllValue);
    return E_NOTIMPL;
}

static HRESULT VarGetStringHelper(
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

static HRESULT VarGetVersionHelper(
    __in C_VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(pqwValue);
    return E_NOTIMPL;
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
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(llValue);
    return E_NOTIMPL;
}

static HRESULT VarSetStringHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(wzValue);
    return E_NOTIMPL;
}

static HRESULT VarSetVersionHelper(
    __in VARIABLES_STRUCT* pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(qwValue);
    return E_NOTIMPL;
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
