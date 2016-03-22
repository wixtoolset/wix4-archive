//-------------------------------------------------------------------------------------------------
// <copyright file="BalBaseBAFunctions.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

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
            hr = BalBaseBootstrapperApplicationProc((BOOTSTRAPPER_APPLICATION_MESSAGE)message, pvArgs, pvResults, pvContext);
            break;
        case BA_FUNCTIONS_MESSAGE_ONTHEMELOADED:
            hr = BalBaseBAFunctionsProcOnThemeLoaded(pBAFunctions, reinterpret_cast<BA_FUNCTIONS_ONTHEMELOADED_ARGS*>(pvArgs), reinterpret_cast<BA_FUNCTIONS_ONTHEMELOADED_RESULTS*>(pvResults));
            break;
        }
    }

    return hr;
}
