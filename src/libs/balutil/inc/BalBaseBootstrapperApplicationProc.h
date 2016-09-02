#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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

static HRESULT BalBaseBAProcOnStartup(
    __in IBootstrapperApplication* pBA,
    __in BA_ONSTARTUP_ARGS* /*pArgs*/,
    __inout BA_ONSTARTUP_RESULTS* /*pResults*/
    )
{
    return pBA->OnStartup();
}

static HRESULT BalBaseBAProcOnShutdown(
    __in IBootstrapperApplication* pBA,
    __in BA_ONSHUTDOWN_ARGS* /*pArgs*/,
    __inout BA_ONSHUTDOWN_RESULTS* pResults
    )
{
    return pBA->OnShutdown(&pResults->action);
}

static HRESULT BalBaseBAProcOnSystemShutdown(
    __in IBootstrapperApplication* pBA,
    __in BA_ONSYSTEMSHUTDOWN_ARGS* pArgs,
    __inout BA_ONSYSTEMSHUTDOWN_RESULTS* pResults
    )
{
    return pBA->OnSystemShutdown(pArgs->dwEndSession, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectForwardCompatibleBundle(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS* pArgs,
    __inout BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS* pResults
    )
{
    return pBA->OnDetectForwardCompatibleBundle(pArgs->wzBundleId, pArgs->relationType, pArgs->wzBundleTag, pArgs->fPerMachine, pArgs->dw64Version, &pResults->fCancel, &pResults->fIgnoreBundle);
}

static HRESULT BalBaseBAProcOnDetectUpdateBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTUPDATEBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTUPDATEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnDetectUpdateBegin(pArgs->wzUpdateLocation, &pResults->fCancel, &pResults->fSkip);
}

static HRESULT BalBaseBAProcOnDetectUpdate(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTUPDATE_ARGS* pArgs,
    __inout BA_ONDETECTUPDATE_RESULTS* pResults
    )
{
    return pBA->OnDetectUpdate(pArgs->wzUpdateLocation, pArgs->dw64Size, pArgs->dw64Version, pArgs->wzTitle, pArgs->wzSummary, pArgs->wzContentType, pArgs->wzContent, &pResults->fCancel, &pResults->fStopProcessingUpdates);
}

static HRESULT BalBaseBAProcOnDetectUpdateComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTUPDATECOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTUPDATECOMPLETE_RESULTS* pResults
    )
{
    return pBA->OnDetectUpdateComplete(pArgs->hrStatus, &pResults->fIgnoreError);
}

static HRESULT BalBaseBAProcOnDetectRelatedBundle(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTRELATEDBUNDLE_ARGS* pArgs,
    __inout BA_ONDETECTRELATEDBUNDLE_RESULTS* pResults
    )
{
    return pBA->OnDetectRelatedBundle(pArgs->wzBundleId, pArgs->relationType, pArgs->wzBundleTag, pArgs->fPerMachine, pArgs->dw64Version, pArgs->operation, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectPackageBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnDetectPackageBegin(pArgs->wzPackageId, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectCompatiblePackage(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTCOMPATIBLEPACKAGE_ARGS* pArgs,
    __inout BA_ONDETECTCOMPATIBLEPACKAGE_RESULTS* pResults
    )
{
    return pBA->OnDetectCompatiblePackage(pArgs->wzPackageId, pArgs->wzCompatiblePackageId, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectRelatedMsiPackage(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTRELATEDMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONDETECTRELATEDMSIPACKAGE_RESULTS* pResults
    )
{
    return pBA->OnDetectRelatedMsiPackage(pArgs->wzPackageId, pArgs->wzUpgradeCode, pArgs->wzProductCode, pArgs->fPerMachine, pArgs->dw64Version, pArgs->operation, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectTargetMsiPackage(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTTARGETMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONDETECTTARGETMSIPACKAGE_RESULTS* pResults
    )
{
    return pBA->OnDetectTargetMsiPackage(pArgs->wzPackageId, pArgs->wzProductCode, pArgs->patchState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectMsiFeature(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTMSIFEATURE_ARGS* pArgs,
    __inout BA_ONDETECTMSIFEATURE_RESULTS* pResults
    )
{
    return pBA->OnDetectMsiFeature(pArgs->wzPackageId, pArgs->wzFeatureId, pArgs->state, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectPackageComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTPACKAGECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnDetectPackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->state);
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
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP:
            hr = BalBaseBAProcOnStartup(pBA, reinterpret_cast<BA_ONSTARTUP_ARGS*>(pvArgs), reinterpret_cast<BA_ONSTARTUP_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN:
            hr = BalBaseBAProcOnShutdown(pBA, reinterpret_cast<BA_ONSHUTDOWN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSHUTDOWN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMSHUTDOWN:
            hr = BalBaseBAProcOnSystemShutdown(pBA, reinterpret_cast<BA_ONSYSTEMSHUTDOWN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSYSTEMSHUTDOWN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE:
            hr = BalBaseBAProcOnDetectForwardCompatibleBundle(pBA, reinterpret_cast<BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN:
            hr = BalBaseBAProcOnDetectUpdateBegin(pBA, reinterpret_cast<BA_ONDETECTUPDATEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE:
            hr = BalBaseBAProcOnDetectUpdate(pBA, reinterpret_cast<BA_ONDETECTUPDATE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE:
            hr = BalBaseBAProcOnDetectUpdateComplete(pBA, reinterpret_cast<BA_ONDETECTUPDATECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE:
            hr = BalBaseBAProcOnDetectRelatedBundle(pBA, reinterpret_cast<BA_ONDETECTRELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN:
            hr = BalBaseBAProcOnDetectPackageBegin(pBA, reinterpret_cast<BA_ONDETECTPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPATIBLEPACKAGE:
            hr = BalBaseBAProcOnDetectCompatiblePackage(pBA, reinterpret_cast<BA_ONDETECTCOMPATIBLEPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTCOMPATIBLEPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE:
            hr = BalBaseBAProcOnDetectRelatedMsiPackage(pBA, reinterpret_cast<BA_ONDETECTRELATEDMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTTARGETMSIPACKAGE:
            hr = BalBaseBAProcOnDetectTargetMsiPackage(pBA, reinterpret_cast<BA_ONDETECTTARGETMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTTARGETMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE:
            hr = BalBaseBAProcOnDetectMsiFeature(pBA, reinterpret_cast<BA_ONDETECTMSIFEATURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTMSIFEATURE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE:
            hr = BalBaseBAProcOnDetectPackageComplete(pBA, reinterpret_cast<BA_ONDETECTPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        }
    }

    pBA->BAProcFallback(message, pvArgs, pvResults, &hr, pvContext);

    return hr;
}
