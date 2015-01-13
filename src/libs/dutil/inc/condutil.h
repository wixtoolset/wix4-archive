//-------------------------------------------------------------------------------------------------
// <copyright file="condutil.h" company="Outercurve Foundation">
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

// function declarations

HRESULT DAPI CondEvaluate(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    );

#if defined(__cplusplus)
}
#endif
