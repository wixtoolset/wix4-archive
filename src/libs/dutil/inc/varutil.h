//-------------------------------------------------------------------------------------------------
// <copyright file="varutil.h" company="Outercurve Foundation">
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

#define ReleaseVariables(vh) if (vh) { VarDestroy(vh, NULL); }
#define ReleaseVariableEnumValue(v) if (v) { VarFreeEnumValue(v); }
#define ReleaseVariableValue(v) if (v) { VarFreeValue(v); }
#define ReleaseNullVariables(vh) if (vh) { VarDestroy(vh, NULL); vh = NULL; }
#define ReleaseNullVariableValue(v) if (v) { VarFreeValue(v); v = NULL; }

typedef void* VARIABLE_ENUM_HANDLE;
typedef void* VARIABLES_HANDLE;
typedef const void* C_VARIABLES_HANDLE;

extern const int VARIABLE_ENUM_HANDLE_BYTES;
extern const int VARIABLES_HANDLE_BYTES;

typedef void(DAPI *PFN_FREEVARIABLECONTEXT)(
    __in LPVOID pvContext
    );

typedef enum VARIABLE_VALUE_TYPE
{
    VARIABLE_VALUE_TYPE_NONE,
    VARIABLE_VALUE_TYPE_NUMERIC,
    VARIABLE_VALUE_TYPE_STRING,
    VARIABLE_VALUE_TYPE_VERSION,
} VARIABLE_VALUE_TYPE;

typedef struct _VARIABLE_VALUE
{
    VARIABLE_VALUE_TYPE type;
    union
    {
        LONGLONG llValue;
        DWORD64 qwValue;
        LPWSTR sczValue;
    };
    BOOL fHidden;
    LPVOID pvContext;
} VARIABLE_VALUE;

typedef struct _VARIABLE_ENUM_VALUE
{
    LPWSTR sczName;
    VARIABLE_VALUE value;
} VARIABLE_ENUM_VALUE;

/********************************************************************
VarCreate - creates a variables group.

********************************************************************/
HRESULT DAPI VarCreate(
    __out_bcount(VARIABLES_HANDLE_BYTES) VARIABLES_HANDLE* ppVariables
    );

/********************************************************************
VarDestroy - destroys a variables group, accepting an optional callback
             to help free the variable contexts.

********************************************************************/
void DAPI VarDestroy(
    __in_bcount(VARIABLES_HANDLE_BYTES) VARIABLES_HANDLE pVariables,
    __in_opt PFN_FREEVARIABLECONTEXT pfnFreeVariableContext
    );

/********************************************************************
VarFreeEnumValue - frees a variable enum value.

********************************************************************/
void DAPI VarFreeEnumValue(
    __in VARIABLE_ENUM_VALUE* pValue
    );

/********************************************************************
VarFreeValue - frees a variable value.

********************************************************************/
void DAPI VarFreeValue(
    __in VARIABLE_VALUE* pValue
    );

/********************************************************************
VarEscapeString - escapes special characters in wzIn so that it can
                  be used in conditions or variable values.

********************************************************************/
HRESULT DAPI VarEscapeString(
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    );

/********************************************************************
VarFormatString - similar to MsiFormatRecord.

********************************************************************/
HRESULT DAPI VarFormatString(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    );

/********************************************************************
VarGetFormatted - gets the formatted value of a single variable.

********************************************************************/
HRESULT DAPI VarGetFormatted(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );

/********************************************************************
VarGetNumeric - gets the numeric value of a variable.  If the type of
                the variable is not numeric, it will attempt to
                convert the value into a number.

********************************************************************/
HRESULT DAPI VarGetNumeric(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    );

/********************************************************************
VarGetString - gets the unformatted string value of a variable.  If
               the type of the variable is not string, it will
               convert the value to a string.

********************************************************************/
HRESULT DAPI VarGetString(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );

/********************************************************************
VarGetVersion - gets the version value of a variable.  If the type of
                the variable is not version, it will attempt to
                convert the value into a version.

********************************************************************/
HRESULT DAPI VarGetVersion(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    );

/********************************************************************
VarGetValue - gets the value of a variable along with its metadata.

********************************************************************/
HRESULT DAPI VarGetValue(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    );

/********************************************************************
VarSetNumeric - sets the value of the variable to a number, the type
                of the variable to numeric, and adds the variable to
                the group if necessary.

********************************************************************/
HRESULT DAPI VarSetNumeric(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    );

/********************************************************************
VarSetString - sets the value of the variable to a string, the type
of the variable to string, and adds the variable to
the group if necessary.
********************************************************************/
HRESULT DAPI VarSetString(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    );

/********************************************************************
VarSetVersion - sets the value of the variable to a version, the type
                of the variable to version, and adds the variable to
                the group if necessary.

********************************************************************/
HRESULT DAPI VarSetVersion(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    );

/********************************************************************
VarSetValue - sets the value of the variable along with its metadata.
              Also adds the variable to the group if necessary.

********************************************************************/
HRESULT DAPI VarSetValue(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in VARIABLE_VALUE* pValue,
    __in BOOL fLog
    );

/********************************************************************
VarStartEnum - starts the enumeration of the variable group.
               There is no guarantee for the order of the variable enumeration.

NOTE: caller is responsible for calling VarFinishEnum even if function fails
********************************************************************/
HRESULT DAPI VarStartEnum(
    __in C_VARIABLES_HANDLE pVariables,
    __out_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE* ppEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    );

/********************************************************************
VarNextVariable - continues the enumeration of the variable group.
                  It will fail if any variables were added or removed
                  during the enumeration.

NOTE: caller is responsible for calling VarFinishEnum even if function fails
********************************************************************/
HRESULT DAPI VarNextVariable(
    __in_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE pEnum,
    __out VARIABLE_ENUM_VALUE** ppValue
    );

/********************************************************************
VarFinishEnum - cleans up resources used for the enumeration.

********************************************************************/
void DAPI VarFinishEnum(
    __in_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE pEnum
    );

#if defined(__cplusplus)
}
#endif
