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
    STDMETHOD(OnThemeLoaded)(
        THEME* pTheme,
        WIX_LOCALIZATION* pWixLoc
        ) = 0;

    // BAFunctionsProc - The PFN_BA_FUNCTIONS_PROC can call this method to give the BAFunctions raw access to the callback from WixStdBA.
    //                   This might be used to help the BAFunctions support more than one version of the engine/WixStdBA.
    STDMETHOD(BAFunctionsProc)(
        __in BA_FUNCTIONS_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __in_opt LPVOID pvContext
        ) = 0;
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
