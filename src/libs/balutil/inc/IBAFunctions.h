//-------------------------------------------------------------------------------------------------
// <copyright file="IBAFunctions.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

#include <windows.h>

#include "IBootstrapperApplication.h"
#include "IBootstrapperEngine.h"

DECLARE_INTERFACE_IID_(IBAFunctions, IBootstrapperApplication, "0FB445ED-17BD-49C7-BE19-479776F8AE96")
{
    STDMETHOD(OnDetect)() = 0;
    STDMETHOD(OnPlan)() = 0;
};

#ifdef __cplusplus
extern "C" {
#endif

typedef HRESULT(WINAPI *PFN_BA_FUNCTIONS_CREATE)(
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __out IBAFunctions** ppBAFunction
    );

#ifdef __cplusplus
}
#endif
