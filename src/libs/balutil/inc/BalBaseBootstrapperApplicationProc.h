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
    __in BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS* pResults
    )
{
    return pBA->OnDetectCompatibleMsiPackage(pArgs->wzPackageId, pArgs->wzCompatiblePackageId, pArgs->dw64CompatiblePackageVersion, &pResults->fCancel);
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

static HRESULT BalBaseBAProcOnPlanRelatedBundle(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANRELATEDBUNDLE_ARGS* pArgs,
    __inout BA_ONPLANRELATEDBUNDLE_RESULTS* pResults
    )
{
    return pBA->OnPlanRelatedBundle(pArgs->wzBundleId, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanPackageBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONPLANPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnPlanPackageBegin(pArgs->wzPackageId, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanCompatibleMsiPackageBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnPlanCompatibleMsiPackageBegin(pArgs->wzPackageId, pArgs->wzCompatiblePackageId, pArgs->dw64CompatiblePackageVersion, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanCompatibleMsiPackageComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnPlanCompatibleMsiPackageComplete(pArgs->wzPackageId, pArgs->wzCompatiblePackageId, pArgs->hrStatus, pArgs->state, pArgs->requested, pArgs->execute, pArgs->rollback);
}

static HRESULT BalBaseBAProcOnPlanTargetMsiPackage(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANTARGETMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONPLANTARGETMSIPACKAGE_RESULTS* pResults
    )
{
    return pBA->OnPlanTargetMsiPackage(pArgs->wzPackageId, pArgs->wzProductCode, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanMsiFeature(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANMSIFEATURE_ARGS* pArgs,
    __inout BA_ONPLANMSIFEATURE_RESULTS* pResults
    )
{
    return pBA->OnPlanMsiFeature(pArgs->wzPackageId, pArgs->wzFeatureId, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanPackageComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONPLANPACKAGECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnPlanPackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->state, pArgs->requested, pArgs->execute, pArgs->rollback);
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
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE:
            hr = BalBaseBAProcOnDetectCompatiblePackage(pBA, reinterpret_cast<BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS*>(pvResults));
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
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE:
            hr = BalBaseBAProcOnPlanRelatedBundle(pBA, reinterpret_cast<BA_ONPLANRELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANRELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN:
            hr = BalBaseBAProcOnPlanPackageBegin(pBA, reinterpret_cast<BA_ONPLANPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN:
            hr = BalBaseBAProcOnPlanCompatibleMsiPackageBegin(pBA, reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE:
            hr = BalBaseBAProcOnPlanCompatibleMsiPackageComplete(pBA, reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANTARGETMSIPACKAGE:
            hr = BalBaseBAProcOnPlanTargetMsiPackage(pBA, reinterpret_cast<BA_ONPLANTARGETMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANTARGETMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE:
            hr = BalBaseBAProcOnPlanMsiFeature(pBA, reinterpret_cast<BA_ONPLANMSIFEATURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANMSIFEATURE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE:
            hr = BalBaseBAProcOnPlanPackageComplete(pBA, reinterpret_cast<BA_ONPLANPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        }
    }

    pBA->BAProcFallback(message, pvArgs, pvResults, &hr, pvContext);

    return hr;
}
