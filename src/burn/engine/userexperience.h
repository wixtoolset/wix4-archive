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
    __in IBootstrapperApplication* pUserExperience,
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
