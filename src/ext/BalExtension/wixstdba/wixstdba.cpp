//-------------------------------------------------------------------------------------------------
// <copyright file="wixstdba.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Setup chainer/bootstrapper standard UI for WiX toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

static HINSTANCE vhInstance = NULL;

extern "C" BOOL WINAPI DllMain(
    IN HINSTANCE hInstance,
    IN DWORD dwReason,
    IN LPVOID /* pvReserved */
    )
{
    switch(dwReason)
    {
    case DLL_PROCESS_ATTACH:
        ::DisableThreadLibraryCalls(hInstance);
        vhInstance = hInstance;
        break;

    case DLL_PROCESS_DETACH:
        vhInstance = NULL;
        break;
    }

    return TRUE;
}


extern "C" HRESULT WINAPI BootstrapperApplicationCreate(
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __in BOOTSTRAPPER_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;

    BalInitialize(pArgs->pEngine);

    hr = CreateBootstrapperApplication(vhInstance, FALSE, S_OK, pArgs, pResults);
    BalExitOnFailure(hr, "Failed to create bootstrapper application interface.");

LExit:
    return hr;
}


extern "C" void WINAPI BootstrapperApplicationDestroy()
{
    BalUninitialize();
}


extern "C" HRESULT WINAPI MbaPrereqBootstrapperApplicationCreate(
    __in HRESULT hrHostInitialization,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __in BOOTSTRAPPER_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;

    BalInitialize(pArgs->pEngine);

    hr = CreateBootstrapperApplication(vhInstance, TRUE, hrHostInitialization, pArgs, pResults);
    BalExitOnFailure(hr, "Failed to create managed prerequisite bootstrapper application interface.");

LExit:
    return hr;
}


extern "C" void WINAPI MbaPrereqBootstrapperApplicationDestroy()
{
    BalUninitialize();
}
