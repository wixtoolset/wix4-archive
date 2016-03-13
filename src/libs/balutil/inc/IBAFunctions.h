//-------------------------------------------------------------------------------------------------
// <copyright file="IBAFunctions.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

DECLARE_INTERFACE_IID_(IBAFunctions, IBootstrapperApplication, "0FB445ED-17BD-49C7-BE19-479776F8AE96")
{
    STDMETHOD(OnDetect)() = 0;
    STDMETHOD(OnPlan)() = 0;
};

// TODO: move everything below into BAFunctions.h
#ifdef __cplusplus
extern "C" {
#endif

struct BA_FUNCTIONS_CREATE_RESULTS
{
    DWORD cbSize;
    IBAFunctions* pBAFunctions;  // TODO: remove when all methods go through the proc
    PFN_BA_FUNCTIONS_PROC pfnBAFunctionsProc;
    LPVOID pvBAFunctionsProcContext;
};

typedef HRESULT(WINAPI *PFN_BA_FUNCTIONS_CREATE)(
    __in const BA_FUNCTIONS_CREATE_ARGS* pArgs,
    __inout BA_FUNCTIONS_CREATE_RESULTS* pResults
    );

#ifdef __cplusplus
}
#endif
