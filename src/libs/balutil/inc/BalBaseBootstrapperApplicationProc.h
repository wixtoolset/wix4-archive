//-------------------------------------------------------------------------------------------------
// <copyright file="BalBaseBootstrapperApplicationProc.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include <windows.h>

#include "BootstrapperEngine.h"
#include "BootstrapperApplication.h"
#include "IBootstrapperEngine.h"
#include "IBootstrapperApplication.h"

static HRESULT BalBaseBAProcOnDetectBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTBEGIN_RESULTS* pResults
    )
{
    return pBA->OnDetectBegin(pArgs->fInstalled, pArgs->cPackages, &pResults->fCancel);
}

/*******************************************************************
BalBaseBootstrapperApplicationProc - requires pvContext to be of type IBootstrapperApplication.
                                     Provides a default mapping between the new message based BA interface and
                                     the old COM-based BA interface.

*******************************************************************/
static HRESULT WINAPI BalBaseBootstrapperApplicationProc(
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    IBootstrapperApplication* pBA = reinterpret_cast<IBootstrapperApplication*>(pvContext);
    HRESULT hr = pBA->BAProc(message, pvArgs, pvResults, pvContext);
    
    if (E_NOTIMPL == hr)
    {
        switch (message)
        {
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN:
            hr = BalBaseBAProcOnDetectBegin(pBA, reinterpret_cast<BA_ONDETECTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTBEGIN_RESULTS*>(pvResults));
            break;
        }
    }

    return hr;
}
