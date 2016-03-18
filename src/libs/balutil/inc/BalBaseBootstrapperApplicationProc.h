//-------------------------------------------------------------------------------------------------
// <copyright file="BalBaseBootstrapperApplicationProc.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

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

static HRESULT BalBaseBAProcOnDetectComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTCOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnDetectComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnPlanBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANBEGIN_ARGS* pArgs,
    __inout BA_ONPLANBEGIN_RESULTS* pResults
    )
{
    return pBA->OnPlanBegin(pArgs->cPackages, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANCOMPLETE_ARGS* pArgs,
    __inout BA_ONPLANCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnPlanComplete(pArgs->hrStatus);
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
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE:
            hr = BalBaseBAProcOnDetectComplete(pBA, reinterpret_cast<BA_ONDETECTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN:
            hr = BalBaseBAProcOnPlanBegin(pBA, reinterpret_cast<BA_ONPLANBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE:
            hr = BalBaseBAProcOnPlanComplete(pBA, reinterpret_cast<BA_ONPLANCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPLETE_RESULTS*>(pvResults));
            break;
        }
    }

    pBA->BAProcFallback(message, pvArgs, pvResults, &hr, pvContext);

    return hr;
}
