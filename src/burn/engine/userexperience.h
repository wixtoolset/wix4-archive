#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#define BAAPI HRESULT __stdcall

#if defined(__cplusplus)
extern "C" {
#endif


// constants

const DWORD MB_RETRYTRYAGAIN = 0xF;


// structs

struct BOOTSTRAPPER_ENGINE_CONTEXT;

typedef struct _BURN_USER_EXPERIENCE
{
    BOOL fSplashScreen;
    BURN_PAYLOADS payloads;

    HMODULE hUXModule;
    IBootstrapperApplication* pUserExperience;
    PFN_BOOTSTRAPPER_APPLICATION_PROC pfnBAProc;
    LPVOID pvBAProcContext;
    LPWSTR sczTempDirectory;

    CRITICAL_SECTION csEngineActive;    // Changing the engine active state in the user experience must be
                                        // syncronized through this critical section.
                                        // Note: The engine must never do a UX callback while in this critical section.

    BOOL fEngineActive;                 // Indicates that the engine is currently active with one of the execution
                                        // steps (detect, plan, apply), and cannot accept requests from the UX.
                                        // This flag should be cleared by the engine prior to UX callbacks that
                                        // allows altering of the engine state.

    HRESULT hrApplyError;               // Tracks is an error occurs during apply that requires the cache or
                                        // execute threads to bail.

    HWND hwndApply;                     // The window handle provided at the beginning of Apply(). Only valid
                                        // during apply.

    HWND hwndDetect;                    // The window handle provided at the beginning of Detect(). Only valid
                                        // during Detect.

    DWORD dwExitCode;                   // Exit code returned by the user experience for the engine overall.
} BURN_USER_EXPERIENCE;

// functions

HRESULT UserExperienceParseFromXml(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IXMLDOMNode* pixnBundle
    );
void UserExperienceUninitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT UserExperienceLoad(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in BOOTSTRAPPER_COMMAND* pCommand
    );
HRESULT UserExperienceUnload(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT UserExperienceEnsureWorkingFolder(
    __in LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczUserExperienceWorkingFolder
    );
HRESULT UserExperienceRemove(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
int UserExperienceSendError(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in HRESULT hrCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD uiFlags,
    __in int nRecommendation
    );
HRESULT UserExperienceActivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __out_opt BOOL* pfActivated
    );
void UserExperienceDeactivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT UserExperienceEnsureEngineInactive(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
void UserExperienceExecuteReset(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
void UserExperienceExecutePhaseComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrResult
    );
BAAPI UserExperienceOnApplyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwPhaseCount
    );
BAAPI UserExperienceOnApplyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
);
BAAPI UserExperienceOnCacheAcquireBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in BOOTSTRAPPER_CACHE_OPERATION operation,
    __in_z LPCWSTR wzSource
    );
BAAPI UserExperienceOnCacheAcquireComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus,
    __inout BOOL* pfRetry
    );
BAAPI UserExperienceOnCacheAcquireProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    );
BAAPI UserExperienceOnCacheBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnCacheComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnCachePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cCachePayloads,
    __in DWORD64 dw64PackageCacheSize
    );
BAAPI UserExperienceOnCachePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION* pAction
    );
BAAPI UserExperienceOnCacheVerifyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    );
BAAPI UserExperienceOnCacheVerifyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION* pAction
    );
BAAPI UserExperienceOnDetectBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fInstalled,
    __in DWORD cPackages
    );
BAAPI UserExperienceOnDetectCompatibleMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in DWORD64 dw64CompatiblePackageVersion
    );
BAAPI UserExperienceOnDetectComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnDetectForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in DWORD64 dw64Version,
    __inout BOOL* pfIgnoreBundle
    );
BAAPI UserExperienceOnDetectMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __in BOOTSTRAPPER_FEATURE_STATE state
    );
BAAPI UserExperienceOnDetectPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId
    );
BAAPI UserExperienceOnDetectPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state
    );
BAAPI UserExperienceOnDetectRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in DWORD64 dw64Version,
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    );
BAAPI UserExperienceOnDetectRelatedMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzUpgradeCode,
    __in_z LPCWSTR wzProductCode,
    __in BOOL fPerMachine,
    __in DWORD64 dw64Version,
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    );
BAAPI UserExperienceOnDetectTargetMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __in BOOTSTRAPPER_PACKAGE_STATE patchState
    );
BAAPI UserExperienceOnDetectUpdate(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzUpdateLocation,
    __in DWORD64 dw64Size,
    __in DWORD64 dw64Version,
    __in_z_opt LPCWSTR wzTitle,
    __in_z_opt LPCWSTR wzSummary,
    __in_z_opt LPCWSTR wzContentType,
    __in_z_opt LPCWSTR wzContent,
    __inout BOOL* pfStopProcessingUpdates
    );
BAAPI UserExperienceOnDetectUpdateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzUpdateLocation,
    __inout BOOL* pfSkip
    );
BAAPI UserExperienceOnDetectUpdateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __inout BOOL* pfIgnoreError
    );
BAAPI UserExperienceOnElevateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnElevateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnError(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in DWORD dwCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD dwUIHint,
    __in DWORD cData,
    __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
    __inout int* pnResult
    );
BAAPI UserExperienceOnExecuteBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cExecutingPackages
    );
BAAPI UserExperienceOnExecuteComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
);
BAAPI UserExperienceOnExecuteFilesInUse(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cFiles,
    __in_ecount_z_opt(cFiles) LPCWSTR* rgwzFiles,
    __inout int* pnResult
    );
BAAPI UserExperienceOnExecuteMsiMessage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in INSTALLMESSAGE messageType,
    __in DWORD dwUIHint,
    __in_z LPCWSTR wzMessage,
    __in DWORD cData,
    __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
    __inout int* pnResult
    );
BAAPI UserExperienceOnExecutePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOL fExecute
    );
BAAPI UserExperienceOnExecutePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION* pAction
    );
BAAPI UserExperienceOnExecutePatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzTargetProductCode
    );
BAAPI UserExperienceOnExecuteProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage,
    __inout int* pnResult
    );
BAAPI UserExperienceOnLaunchApprovedExeBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnLaunchApprovedExeComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in DWORD dwProcessId
    );
BAAPI UserExperienceOnPlanBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cPackages
    );
BAAPI UserExperienceOnPlanCompatibleMsiPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in DWORD64 dw64CompatiblePackageVersion,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
BAAPI UserExperienceOnPlanCompatibleMsiPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOTSTRAPPER_REQUEST_STATE requested,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback
    );
BAAPI UserExperienceOnPlanComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnPlanMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __inout BOOTSTRAPPER_FEATURE_STATE* pRequestedState
    );
BAAPI UserExperienceOnPlanPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
BAAPI UserExperienceOnPlanPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOTSTRAPPER_REQUEST_STATE requested,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback
    );
BAAPI UserExperienceOnPlanRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
BAAPI UserExperienceOnPlanTargetMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
BAAPI UserExperienceOnProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage
    );
BAAPI UserExperienceOnRegisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnRegisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnResolveSource(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPCWSTR wzLocalSource,
    __in_z_opt LPCWSTR wzDownloadSource,
    __inout BOOTSTRAPPER_RESOLVESOURCE_ACTION* pAction
    );
BAAPI UserExperienceOnShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
    );
BAAPI UserExperienceOnStartup(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnSystemShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwEndSession,
    __inout BOOL* pfCancel
    );
BAAPI UserExperienceOnUnregisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnUnregisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
HRESULT UserExperienceInterpretResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwAllowedResults,
    __in int nResult
    );
int UserExperienceCheckExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    );
HRESULT UserExperienceInterpretExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    );
#if defined(__cplusplus)
}
#endif
