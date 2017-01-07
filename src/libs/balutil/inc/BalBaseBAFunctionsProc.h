#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include "BalBaseBootstrapperApplicationProc.h"
#include "BAFunctions.h"
#include "IBAFunctions.h"

static HRESULT BalBaseBAFunctionsProcOnThemeLoaded(
    __in IBAFunctions* pBAFunctions,
    __in BA_FUNCTIONS_ONTHEMELOADED_ARGS* pArgs,
    __inout BA_FUNCTIONS_ONTHEMELOADED_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnThemeLoaded(pArgs->pTheme, pArgs->pWixLoc);
}

/*******************************************************************
BalBaseBAFunctionsProc - requires pvContext to be of type IBAFunctions.
Provides a default mapping between the message based BAFunctions interface and
the COM-based BAFunctions interface.

*******************************************************************/
static HRESULT WINAPI BalBaseBAFunctionsProc(
    __in BA_FUNCTIONS_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    IBAFunctions* pBAFunctions = reinterpret_cast<IBAFunctions*>(pvContext);
    HRESULT hr = pBAFunctions->BAFunctionsProc(message, pvArgs, pvResults, pvContext);

    if (E_NOTIMPL == hr)
    {
        switch (message)
        {
        case BA_FUNCTIONS_MESSAGE_ONDETECTBEGIN:
        case BA_FUNCTIONS_MESSAGE_ONDETECTCOMPLETE:
        case BA_FUNCTIONS_MESSAGE_ONPLANBEGIN:
        case BA_FUNCTIONS_MESSAGE_ONPLANCOMPLETE:
        case BA_FUNCTIONS_MESSAGE_ONSTARTUP:
        case BA_FUNCTIONS_MESSAGE_ONSHUTDOWN:
        case BA_FUNCTIONS_MESSAGE_ONSYSTEMSHUTDOWN:
        case BA_FUNCTIONS_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE:
        case BA_FUNCTIONS_MESSAGE_ONDETECTUPDATEBEGIN:
        case BA_FUNCTIONS_MESSAGE_ONDETECTUPDATE:
        case BA_FUNCTIONS_MESSAGE_ONDETECTUPDATECOMPLETE:
        case BA_FUNCTIONS_MESSAGE_ONDETECTRELATEDBUNDLE:
        case BA_FUNCTIONS_MESSAGE_ONDETECTPACKAGEBEGIN:
        case BA_FUNCTIONS_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE:
        case BA_FUNCTIONS_MESSAGE_ONDETECTRELATEDMSIPACKAGE:
        case BA_FUNCTIONS_MESSAGE_ONDETECTTARGETMSIPACKAGE:
        case BA_FUNCTIONS_MESSAGE_ONDETECTMSIFEATURE:
        case BA_FUNCTIONS_MESSAGE_ONDETECTPACKAGECOMPLETE:
        case BA_FUNCTIONS_MESSAGE_ONPLANRELATEDBUNDLE:
        case BA_FUNCTIONS_MESSAGE_ONPLANPACKAGEBEGIN:
        case BA_FUNCTIONS_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN:
        case BA_FUNCTIONS_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE:
        case BA_FUNCTIONS_MESSAGE_ONPLANTARGETMSIPACKAGE:
        case BA_FUNCTIONS_MESSAGE_ONPLANMSIFEATURE:
        case BA_FUNCTIONS_MESSAGE_ONPLANPACKAGECOMPLETE:
            hr = BalBaseBootstrapperApplicationProc((BOOTSTRAPPER_APPLICATION_MESSAGE)message, pvArgs, pvResults, pvContext);
            break;
        case BA_FUNCTIONS_MESSAGE_ONTHEMELOADED:
            hr = BalBaseBAFunctionsProcOnThemeLoaded(pBAFunctions, reinterpret_cast<BA_FUNCTIONS_ONTHEMELOADED_ARGS*>(pvArgs), reinterpret_cast<BA_FUNCTIONS_ONTHEMELOADED_RESULTS*>(pvResults));
            break;
        }
    }

    return hr;
}
