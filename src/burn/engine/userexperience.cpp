// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// internal function declarations

static int FilterResult(
    __in DWORD dwAllowedResults,
    __in int nResult
    );


// function definitions

/*******************************************************************
 UserExperienceParseFromXml - 

*******************************************************************/
extern "C" HRESULT UserExperienceParseFromXml(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnUserExperienceNode = NULL;

    // select UX node
    hr = XmlSelectSingleNode(pixnBundle, L"UX", &pixnUserExperienceNode);
    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
    }
    ExitOnFailure(hr, "Failed to select user experience node.");

    // parse splash screen
    hr = XmlGetYesNoAttribute(pixnUserExperienceNode, L"SplashScreen", &pUserExperience->fSplashScreen);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to to get UX/@SplashScreen");
    }

    // parse payloads
    hr = PayloadsParseFromXml(&pUserExperience->payloads, NULL, NULL, pixnUserExperienceNode);
    ExitOnFailure(hr, "Failed to parse user experience payloads.");

    // make sure we have at least one payload
    if (0 == pUserExperience->payloads.cPayloads)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Too few UX payloads.");
    }

LExit:
    ReleaseObject(pixnUserExperienceNode);

    return hr;
}

/*******************************************************************
 UserExperienceUninitialize - 

*******************************************************************/
extern "C" void UserExperienceUninitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    ReleaseStr(pUserExperience->sczTempDirectory);
    PayloadsUninitialize(&pUserExperience->payloads);

    // clear struct
    memset(pUserExperience, 0, sizeof(BURN_USER_EXPERIENCE));
}

/*******************************************************************
 UserExperienceLoad - 

*******************************************************************/
extern "C" HRESULT UserExperienceLoad(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in BOOTSTRAPPER_COMMAND* pCommand
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_CREATE_ARGS args = { };
    BOOTSTRAPPER_CREATE_RESULTS results = { };

    args.cbSize = sizeof(BOOTSTRAPPER_CREATE_ARGS);
    args.pCommand = pCommand;
    args.pEngine = pEngineContext->pEngineForApplication;
    args.pfnBootstrapperEngineProc = EngineForApplicationProc;
    args.pvBootstrapperEngineProcContext = pEngineContext;
    args.qwEngineAPIVersion = MAKEQWORDVERSION(0, 0, 0, 4); // TODO: need to decide whether to keep this, and if so when to update it.

    results.cbSize = sizeof(BOOTSTRAPPER_CREATE_RESULTS);

    // Load BA DLL.
    pUserExperience->hUXModule = ::LoadLibraryExW(pUserExperience->payloads.rgPayloads[0].sczLocalFilePath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    ExitOnNullWithLastError(pUserExperience->hUXModule, hr, "Failed to load UX DLL.");

    // Get BootstrapperApplicationCreate entry-point.
    PFN_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = (PFN_BOOTSTRAPPER_APPLICATION_CREATE)::GetProcAddress(pUserExperience->hUXModule, "BootstrapperApplicationCreate");
    ExitOnNullWithLastError(pfnCreate, hr, "Failed to get BootstrapperApplicationCreate entry-point");

    // Create BA.
    hr = pfnCreate(&args, &results);
    ExitOnFailure(hr, "Failed to create BA.");

    pUserExperience->pUserExperience = results.pApplication;
    pUserExperience->pfnBAProc = results.pfnBootstrapperApplicationProc;
    pUserExperience->pvBAProcContext = results.pvBootstrapperApplicationProcContext;

LExit:
    return hr;
}

/*******************************************************************
 UserExperienceUnload - 

*******************************************************************/
extern "C" HRESULT UserExperienceUnload(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;

    ReleaseNullObject(pUserExperience->pUserExperience);

    if (pUserExperience->hUXModule)
    {
        // Get BootstrapperApplicationDestroy entry-point and call it if it exists.
        PFN_BOOTSTRAPPER_APPLICATION_DESTROY pfnDestroy = (PFN_BOOTSTRAPPER_APPLICATION_DESTROY)::GetProcAddress(pUserExperience->hUXModule, "BootstrapperApplicationDestroy");
        if (pfnDestroy)
        {
            pfnDestroy();
        }

        // Free BA DLL.
        if (!::FreeLibrary(pUserExperience->hUXModule))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
            TraceError(hr, "Failed to unload BA DLL.");
        }
        pUserExperience->hUXModule = NULL;
    }

//LExit:
    return hr;
}

extern "C" HRESULT UserExperienceEnsureWorkingFolder(
    __in LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczUserExperienceWorkingFolder
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;

    hr = CacheEnsureWorkingFolder(wzBundleId, &sczWorkingFolder);
    ExitOnFailure(hr, "Failed to create working folder.");

    hr = StrAllocFormatted(psczUserExperienceWorkingFolder, L"%ls%ls\\", sczWorkingFolder, L".ba");
    ExitOnFailure(hr, "Failed to calculate the bootstrapper application working path.");

    hr = DirEnsureExists(*psczUserExperienceWorkingFolder, NULL);
    ExitOnFailure(hr, "Failed create bootstrapper application working folder.");

LExit:
    ReleaseStr(sczWorkingFolder);

    return hr;
}


extern "C" HRESULT UserExperienceRemove(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;

    // Remove temporary UX directory
    if (pUserExperience->sczTempDirectory)
    {
        hr = DirEnsureDeleteEx(pUserExperience->sczTempDirectory, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
        TraceError(hr, "Could not delete bootstrapper application folder. Some files will be left in the temp folder.");
    }

//LExit:
    return hr;
}

extern "C" int UserExperienceSendError(
    __in IBootstrapperApplication* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in HRESULT hrCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD uiFlags,
    __in int nRecommendation
    )
{
    int nResult = IDNOACTION;
    DWORD dwCode = HRESULT_CODE(hrCode);
    LPWSTR sczError = NULL;

    // If no error string was provided, try to get the error string from the HRESULT.
    if (!wzError)
    {
        if (SUCCEEDED(StrAllocFromError(&sczError, hrCode, NULL)))
        {
            wzError = sczError;
        }
    }

    nResult = pUserExperience->OnError(errorType, wzPackageId, dwCode, wzError, uiFlags, 0, NULL, nRecommendation);

//LExit:
    ReleaseStr(sczError);
    return nResult;
}

extern "C" HRESULT UserExperienceActivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __out_opt BOOL* pfActivated
    )
{
    HRESULT hr = S_OK;
    BOOL fActivated;

    ::EnterCriticalSection(&pUserExperience->csEngineActive);
    if (InterlockedCompareExchange(reinterpret_cast<LONG*>(&pUserExperience->fEngineActive), TRUE, FALSE))
    {
        AssertSz(FALSE, "Engine should have been deactivated before activating it.");

        fActivated = FALSE;
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_STATE);
    }
    else
    {
        fActivated = TRUE;
    }
    ::LeaveCriticalSection(&pUserExperience->csEngineActive);

    if (pfActivated)
    {
        *pfActivated = fActivated;
    }
    ExitOnRootFailure(hr, "Engine active cannot be changed because it was already in that state.");

LExit:
    return hr;
}

extern "C" void UserExperienceDeactivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    BOOL fActive = InterlockedExchange(reinterpret_cast<LONG*>(&pUserExperience->fEngineActive), FALSE);
    fActive = fActive; // prevents warning in "ship" build.
    AssertSz(fActive, "Engine should have be active before deactivating it.");
}

extern "C" HRESULT UserExperienceEnsureEngineInactive(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = pUserExperience->fEngineActive ? HRESULT_FROM_WIN32(ERROR_BUSY) : S_OK;
    ExitOnRootFailure(hr, "Engine is active, cannot proceed.");

LExit:
    return hr;
}

extern "C" void UserExperienceExecuteReset(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    pUserExperience->hrApplyError = S_OK;
    pUserExperience->hwndApply = NULL;
}

extern "C" void UserExperienceExecutePhaseComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrResult
    )
{
    if (FAILED(hrResult))
    {
        pUserExperience->hrApplyError = hrResult;
    }
}

EXTERN_C BAAPI UserExperienceOnDetectBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fInstalled,
    __in DWORD cPackages
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTBEGIN_ARGS args = { };
    BA_ONDETECTBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.cPackages = cPackages;
    args.fInstalled = fInstalled;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectCompatibleMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in DWORD64 dw64CompatiblePackageVersion
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS args = { };
    BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzCompatiblePackageId = wzCompatiblePackageId;
    args.dw64CompatiblePackageVersion = dw64CompatiblePackageVersion;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectCompatibleMsiPackage failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTCOMPLETE_ARGS args = { };
    BA_ONDETECTCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in DWORD64 dw64Version,
    __inout BOOL* pfIgnoreBundle
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS args = { };
    BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzBundleId = wzBundleId;
    args.relationType = relationType;
    args.wzBundleTag = wzBundleTag;
    args.fPerMachine = fPerMachine;
    args.dw64Version = dw64Version;

    results.cbSize = sizeof(results);
    results.fIgnoreBundle = *pfIgnoreBundle;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectForwardCompatibleBundle failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pfIgnoreBundle = results.fIgnoreBundle;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __in BOOTSTRAPPER_FEATURE_STATE state
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTMSIFEATURE_ARGS args = { };
    BA_ONDETECTMSIFEATURE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzFeatureId = wzFeatureId;
    args.state = state;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectMsiFeature failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPACKAGEBEGIN_ARGS args = { };
    BA_ONDETECTPACKAGEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectPackageBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTPACKAGECOMPLETE_ARGS args = { };
    BA_ONDETECTPACKAGECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.state = state;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectPackageComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in DWORD64 dw64Version,
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDBUNDLE_ARGS args = { };
    BA_ONDETECTRELATEDBUNDLE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzBundleId = wzBundleId;
    args.relationType = relationType;
    args.wzBundleTag = wzBundleTag;
    args.fPerMachine = fPerMachine;
    args.dw64Version = dw64Version;
    args.operation = operation;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectRelatedBundle failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectRelatedMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzUpgradeCode,
    __in_z LPCWSTR wzProductCode,
    __in BOOL fPerMachine,
    __in DWORD64 dw64Version,
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTRELATEDMSIPACKAGE_ARGS args = { };
    BA_ONDETECTRELATEDMSIPACKAGE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzUpgradeCode = wzUpgradeCode;
    args.wzProductCode = wzProductCode;
    args.fPerMachine = fPerMachine;
    args.dw64Version = dw64Version;
    args.operation = operation;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectRelatedMsiPackage failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectTargetMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __in BOOTSTRAPPER_PACKAGE_STATE patchState
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTTARGETMSIPACKAGE_ARGS args = { };
    BA_ONDETECTTARGETMSIPACKAGE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzProductCode = wzProductCode;
    args.patchState = patchState;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTTARGETMSIPACKAGE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectTargetMsiPackage failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectUpdate(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzUpdateLocation,
    __in DWORD64 dw64Size,
    __in DWORD64 dw64Version,
    __in_z_opt LPCWSTR wzTitle,
    __in_z_opt LPCWSTR wzSummary,
    __in_z_opt LPCWSTR wzContentType,
    __in_z_opt LPCWSTR wzContent,
    __inout BOOL* pfStopProcessingUpdates
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATE_ARGS args = { };
    BA_ONDETECTUPDATE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzUpdateLocation = wzUpdateLocation;
    args.dw64Size = dw64Size;
    args.dw64Version = dw64Version;
    args.wzTitle = wzTitle;
    args.wzSummary = wzSummary;
    args.wzContentType = wzContentType;
    args.wzContent = wzContent;

    results.cbSize = sizeof(results);
    results.fStopProcessingUpdates = *pfStopProcessingUpdates;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectUpdate failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pfStopProcessingUpdates = results.fStopProcessingUpdates;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectUpdateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzUpdateLocation,
    __inout BOOL* pfSkip
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATEBEGIN_ARGS args = { };
    BA_ONDETECTUPDATEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzUpdateLocation = wzUpdateLocation;

    results.cbSize = sizeof(results);
    results.fSkip = *pfSkip;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectUpdateBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pfSkip = results.fSkip;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnDetectUpdateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __inout BOOL* pfIgnoreError
    )
{
    HRESULT hr = S_OK;
    BA_ONDETECTUPDATECOMPLETE_ARGS args = { };
    BA_ONDETECTUPDATECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);
    results.fIgnoreError = *pfIgnoreError;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnDetectUpdateComplete failed.");

    *pfIgnoreError = results.fIgnoreError;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cPackages
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANBEGIN_ARGS args = { };
    BA_ONPLANBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.cPackages = cPackages;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanCompatibleMsiPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in DWORD64 dw64CompatiblePackageVersion,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS args = { };
    BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzCompatiblePackageId = wzCompatiblePackageId;
    args.dw64CompatiblePackageVersion = dw64CompatiblePackageVersion;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanCompatibleMsiPackageBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanCompatibleMsiPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOTSTRAPPER_REQUEST_STATE requested,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS args = { };
    BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzCompatiblePackageId = wzCompatiblePackageId;
    args.hrStatus = hrStatus;
    args.state = state;
    args.requested = requested;
    args.execute = execute;
    args.rollback = rollback;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanCompatibleMsiPackageComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __inout BOOTSTRAPPER_FEATURE_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANMSIFEATURE_ARGS args = { };
    BA_ONPLANMSIFEATURE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzFeatureId = wzFeatureId;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanMsiFeature failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANCOMPLETE_ARGS args = { };
    BA_ONPLANCOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.hrStatus = hrStatus;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPACKAGEBEGIN_ARGS args = { };
    BA_ONPLANPACKAGEBEGIN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanPackageBegin failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOTSTRAPPER_REQUEST_STATE requested,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANPACKAGECOMPLETE_ARGS args = { };
    BA_ONPLANPACKAGECOMPLETE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.hrStatus = hrStatus;
    args.state = state;
    args.requested = requested;
    args.execute = execute;
    args.rollback = rollback;

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanPackageComplete failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANRELATEDBUNDLE_ARGS args = { };
    BA_ONPLANRELATEDBUNDLE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzBundleId = wzBundleId;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanRelatedBundle failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnPlanTargetMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    )
{
    HRESULT hr = S_OK;
    BA_ONPLANTARGETMSIPACKAGE_ARGS args = { };
    BA_ONPLANTARGETMSIPACKAGE_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wzPackageId = wzPackageId;
    args.wzProductCode = wzProductCode;

    results.cbSize = sizeof(results);
    results.requestedState = *pRequestedState;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANTARGETMSIPACKAGE, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnPlanTargetMsiPackage failed.");

    if (results.fCancel)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
    }
    *pRequestedState = results.requestedState;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BA_ONSHUTDOWN_ARGS args = { };
    BA_ONSHUTDOWN_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);
    results.action = *pAction;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnShutdown failed.");

    *pAction = results.action;

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnStartup(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    BA_ONSTARTUP_ARGS args = { };
    BA_ONSTARTUP_RESULTS results = { };

    args.cbSize = sizeof(args);

    results.cbSize = sizeof(results);

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnStartup failed.");

LExit:
    return hr;
}

EXTERN_C BAAPI UserExperienceOnSystemShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwEndSession,
    __inout BOOL* pfCancel
    )
{
    HRESULT hr = S_OK;
    BA_ONSYSTEMSHUTDOWN_ARGS args = { };
    BA_ONSYSTEMSHUTDOWN_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.dwEndSession = dwEndSession;

    results.cbSize = sizeof(results);
    results.fCancel = *pfCancel;

    hr = pUserExperience->pfnBAProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMSHUTDOWN, &args, &results, pUserExperience->pvBAProcContext);
    ExitOnFailure(hr, "BA OnSystemShutdown failed.");
    
    *pfCancel = results.fCancel;

LExit:
    return hr;
}

extern "C" int UserExperienceCheckExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    // Do not allow canceling while rolling back.
    if (fRollback && (IDCANCEL == nResult || IDABORT == nResult))
    {
        nResult = IDNOACTION;
    }
    else if (FAILED(pUserExperience->hrApplyError) && !fRollback) // if we failed cancel except not during rollback.
    {
        nResult = IDCANCEL;
    }

    nResult = FilterResult(dwAllowedResults, nResult);
    return nResult;
}

extern "C" HRESULT UserExperienceInterpretResult(
    __in BURN_USER_EXPERIENCE* /*pUserExperience*/,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    int nFilteredResult = FilterResult(dwAllowedResults, nResult);
    return IDOK == nFilteredResult || IDNOACTION == nFilteredResult ? S_OK : IDCANCEL == nFilteredResult || IDABORT == nFilteredResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
}

extern "C" HRESULT UserExperienceInterpretExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    HRESULT hr = S_OK;

    // If we failed return that error unless this is rollback which should roll on.
    if (FAILED(pUserExperience->hrApplyError) && !fRollback)
    {
        hr = pUserExperience->hrApplyError;
    }
    else
    {
        int nCheckedResult = UserExperienceCheckExecuteResult(pUserExperience, fRollback, dwAllowedResults, nResult);
        hr = IDOK == nCheckedResult || IDNOACTION == nCheckedResult ? S_OK : IDCANCEL == nCheckedResult || IDABORT == nCheckedResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
    }

    return hr;
}


// internal functions

static int FilterResult(
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    if (IDNOACTION == nResult || IDERROR == nResult) // do nothing and errors pass through.
    {
    }
    else
    {
        switch (dwAllowedResults)
        {
        case MB_OK:
            nResult = IDOK;
            break;

        case MB_OKCANCEL:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDOK;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult || IDNO == nResult)
            {
                nResult = IDCANCEL;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_ABORTRETRYIGNORE:
            if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDIGNORE == nResult)
            {
                nResult = IDIGNORE;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_YESNO:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDYES;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult || IDNO == nResult)
            {
                nResult = IDNO;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_YESNOCANCEL:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDYES;
            }
            else if (IDNO == nResult)
            {
                nResult = IDNO;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDCANCEL;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_RETRYCANCEL:
            if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_CANCELTRYCONTINUE:
            if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDCONTINUE == nResult || IDIGNORE == nResult)
            {
                nResult = IDCONTINUE;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case WIU_MB_OKIGNORECANCELRETRY: // custom Windows Installer utility return code.
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDOK;
            }
            else if (IDCONTINUE == nResult || IDIGNORE == nResult)
            {
                nResult = IDIGNORE;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDCANCEL;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult || IDNO == nResult)
            {
                nResult = IDRETRY;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_RETRYTRYAGAIN: // custom return code.
            if (IDRETRY != nResult && IDTRYAGAIN != nResult)
            {
                nResult = IDNOACTION;
            }
            break;

        default:
            AssertSz(FALSE, "Unknown allowed results.");
            break;
        }
    }

    return nResult;
}
