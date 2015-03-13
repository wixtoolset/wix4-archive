//-------------------------------------------------------------------------------------------------
// <copyright file="condutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// function definitions

/********************************************************************
CondEvaluate - evaluates the condition using the given variables.
********************************************************************/
extern "C" HRESULT DAPI CondEvaluate(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzCondition);
    UNREFERENCED_PARAMETER(pf);
    return E_NOTIMPL;
}
