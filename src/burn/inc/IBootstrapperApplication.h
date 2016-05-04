#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


DECLARE_INTERFACE_IID_(IBootstrapperApplication, IUnknown, "53C31D56-49C0-426B-AB06-099D717C67FE")
{
    // OnStartup - called when the engine is ready for the bootstrapper application to start.
    //
    STDMETHOD(OnStartup)() = 0;

    // OnShutdown - called after the bootstrapper application quits the engine.
    //
    // Return:
    //  IDRESTART instructs the engine to restart. The engine will not launch again after the machine
    //            is rebooted. Ignored if reboot was already initiated by OnExecutePackageComplete().
    //
    // IDRELOAD_BOOTSTRAPPER instructs the engine to unload the bootstrapper application and restart
    //                       the engine which will load the bootstrapper application again. Typically
    //                       used to switch from a native bootstrapper application to a managed one.
    //
    //  All other return codes are ignored.
    STDMETHOD_(int, OnShutdown)() = 0;

    // OnSystemShutdown - called when the operating system is instructed to shutdown the machine.
    //
    // Return:
    //  IDCANCEL instructs the engine to block the shutdown of the machine.
    //
    //  All other return codes allow the shutdown to commence.
    STDMETHOD_(int, OnSystemShutdown)(
        __in DWORD dwEndSession,
        __in int nRecommendation
        ) = 0;

    // OnDetectBegin - called when the engine begins detection.
    STDMETHOD(OnDetectBegin)(
        __in BOOL fInstalled,
        __in DWORD cPackages,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectForwardCompatibleBundle - called when the engine detects a forward compatible bundle.
    //
    // Return:
    //  IDOK instructs the engine to use the forward compatible bundle.
    //
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to not use the forward compatible bundle.
    STDMETHOD_(int, OnDetectForwardCompatibleBundle)(
        __in_z LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATION_TYPE relationType,
        __in_z LPCWSTR wzBundleTag,
        __in BOOL fPerMachine,
        __in DWORD64 dw64Version,
        __in int nRecommendation
        ) = 0;

    // OnDetectUpdateBegin - called when the engine begins detection for bundle update.
    //
    // Return:
    //  IDOK instructs the engine to attempt update detection.
    //
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to skip update detection.
    STDMETHOD_(int, OnDetectUpdateBegin)(
        __in_z LPCWSTR wzUpdateLocation,
        __in int nRecommendation
        ) = 0;

    // OnDetectUpdate - called when the engine has an update candidate for bundle update.
    //
    // Return:
    //  IDOK instructs the engine to stop further update detection.
    //
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to process further update candidates.
    STDMETHOD_(int, OnDetectUpdate)(
        __in_z_opt LPCWSTR wzUpdateLocation,
        __in DWORD64 dw64Size,
        __in DWORD64 dw64Version,
        __in_z_opt LPCWSTR wzTitle,
        __in_z_opt LPCWSTR wzSummary,
        __in_z_opt LPCWSTR wzContentType,
        __in_z_opt LPCWSTR wzContent,
        __in int nRecommendation
        ) = 0;

    // OnDetectUpdateComplete - called when the engine completes detection for bundle update.
    //
    // Remarks:
    //  wzUpdateLocation is null if no update was available.
    STDMETHOD_(int, OnDetectUpdateComplete)(
        __in HRESULT hrStatus,
        __in_z_opt LPCWSTR wzUpdateLocation,
        __in int nRecommendation
        ) = 0;

    // OnDetectRelatedBundle - called when the engine detects a related bundle.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnDetectRelatedBundle)(
        __in_z LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATION_TYPE relationType,
        __in_z LPCWSTR wzBundleTag,
        __in BOOL fPerMachine,
        __in DWORD64 dw64Version,
        __in BOOTSTRAPPER_RELATED_OPERATION operation
        ) = 0;

    // OnDetectPackageBegin - called when the engine begins detecting a package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnDetectPackageBegin)(
        __in_z LPCWSTR wzPackageId
        ) = 0;

    // OnDetectCompatiblePackage - called when the engine detects that a package is not installed but a newer package using the same provider key is.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnDetectCompatiblePackage)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzCompatiblePackageId
        ) = 0;

    // OnDetectRelatedMsiPackage - called when the engine begins detects a related package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnDetectRelatedMsiPackage)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzProductCode,
        __in BOOL fPerMachine,
        __in DWORD64 dw64Version,
        __in BOOTSTRAPPER_RELATED_OPERATION operation
        ) = 0;

    // OnDetectTargetMsiPackage - called when the engine detects a target MSI package for
    //                            an MSP package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnDetectTargetMsiPackage)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzProductCode,
        __in BOOTSTRAPPER_PACKAGE_STATE patchState
        ) = 0;

    // OnDetectMsiFeature - called when the engine detects a feature in an MSI package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop detection.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnDetectMsiFeature)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzFeatureId,
        __in BOOTSTRAPPER_FEATURE_STATE state
        ) = 0;

    // OnDetectPackageComplete - called after the engine detects a package.
    //
    STDMETHOD_(void, OnDetectPackageComplete)(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_PACKAGE_STATE state
        ) = 0;

    // OnDetectPackageComplete - called after the engine completes detection.
    //
    STDMETHOD(OnDetectComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnPlanBegin - called when the engine begins planning.
    STDMETHOD(OnPlanBegin)(
        __in DWORD cPackages,
        __inout BOOL* pfCancel
        ) = 0;

    // OnPlanRelatedBundle - called when the engine begins planning a related bundle.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop planning.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnPlanRelatedBundle)(
        __in_z LPCWSTR wzBundleId,
        __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
        ) = 0;

    // OnPlanPackageBegin - called when the engine begins planning a package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop planning.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnPlanPackageBegin)(
        __in_z LPCWSTR wzPackageId,
        __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
        ) = 0;

    // OnPlanCompatiblePackage - called when the engine plans a newer, compatible package using the same provider key.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop planning.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnPlanCompatiblePackage)(
        __in_z LPCWSTR wzPackageId,
        __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
        ) = 0;

    // OnPlanTargetMsiPackage - called when the engine plans an MSP package
    //                          to apply to an MSI package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop planning.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnPlanTargetMsiPackage)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzProductCode,
        __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
        ) = 0;

    // OnPlanMsiFeature - called when the engine plans a feature in an
    //                    MSI package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop planning.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnPlanMsiFeature)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzFeatureId,
        __inout BOOTSTRAPPER_FEATURE_STATE* pRequestedState
        ) = 0;

    // OnPlanPackageComplete - called after the engine plans a package.
    //
    STDMETHOD_(void, OnPlanPackageComplete)(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_PACKAGE_STATE state,
        __in BOOTSTRAPPER_REQUEST_STATE requested,
        __in BOOTSTRAPPER_ACTION_STATE execute,
        __in BOOTSTRAPPER_ACTION_STATE rollback
        ) = 0;

    // OnPlanComplete - called when the engine completes planning.
    //
    STDMETHOD(OnPlanComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnApplyBegin - called when the engine begins applying the plan.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop applying.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnApplyBegin)(
        __in DWORD dwPhaseCount
        ) = 0;

    // OnElevate - called before the engine displays an elevation prompt.
    //             Will only happen once per execution of the engine.
    //
    // Return:
    //  IDCANCEL instructs the engine to abort elevation and stop applying.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnElevate)() = 0;

    // OnProgress - called when the engine makes progress.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop applying.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnProgress)(
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallPercentage
        ) = 0;

    // OnError - called when the engine encounters an error.
    //
    // Return:
    //  uiFlags is a combination of valid ID* return values appropriate for
    //          the error.
    //
    //  IDNOACTION instructs the engine to pass the error through to default
    //             handling which usually results in the apply failing.
    STDMETHOD_(int, OnError)(
        __in BOOTSTRAPPER_ERROR_TYPE errorType,
        __in_z_opt LPCWSTR wzPackageId,
        __in DWORD dwCode,
        __in_z_opt LPCWSTR wzError,
        __in DWORD uiFlags,
        __in DWORD cData,
        __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
        __in int nRecommendation
        ) = 0;

    // OnRegisterBegin - called when the engine registers the bundle.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop applying.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnRegisterBegin)() = 0;

    // OnRegisterComplete - called when the engine registration is
    //                      complete.
    //
    STDMETHOD_(void, OnRegisterComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnCacheBegin - called when the engine begins caching.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop caching.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnCacheBegin)() = 0;

    // OnCachePackageBegin - called when the engine begins caching
    //                       a package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop caching.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnCachePackageBegin)(
        __in_z LPCWSTR wzPackageId,
        __in DWORD cCachePayloads,
        __in DWORD64 dw64PackageCacheSize
        )  = 0;

    // OnCacheAcquireBegin - called when the engine begins copying or
    //                       downloading a payload to the working folder.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop caching.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnCacheAcquireBegin)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in BOOTSTRAPPER_CACHE_OPERATION operation,
        __in_z LPCWSTR wzSource
        ) = 0;

    // OnCacheAcquireProgress - called when the engine makes progresss copying
    //                          or downloading a payload to the working folder.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop caching.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnCacheAcquireProgress)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage
        ) = 0;

    // OnResolveSource - called when a payload or container cannot be found locally.
    //
    // Parameters:
    //  wzPayloadId will be NULL when resolving a container.
    //  wzDownloadSource will be NULL if the container or payload does not provide a DownloadURL.
    //
    // Return:
    //  IDRETRY instructs the engine to try the local source again.
    //
    //  IDDOWNLOAD instructs the engine to try the download source.
    //
    //  All other return codes result in an error.
    //
    // Notes:
    //  It is expected the BA may call IBurnCore::SetLocalSource() or IBurnCore::SetDownloadSource()
    //  to update the source location before returning IDRETRY or IDDOWNLOAD.
    STDMETHOD_(int, OnResolveSource)(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzLocalSource,
        __in_z_opt LPCWSTR wzDownloadSource
        ) = 0;

    // OnCacheAcquireComplete - called after the engine copied or downloaded
    //                          a payload to the working folder.
    //
    // Return:
    //  IDRETRY instructs the engine to try the copy or download of the payload again.
    //
    //  All other return codes are ignored.
    STDMETHOD_(int, OnCacheAcquireComplete)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in int nRecommendation
        ) = 0;

    // OnCacheVerifyBegin - called when the engine begins to verify then copy
    //                      a payload or container to the package cache folder.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop caching.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnCacheVerifyBegin)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId
        ) = 0;

    // OnCacheVerifyComplete - called after the engine verifies and copies
    //                         a payload or container to the package cache folder.
    //
    // Return:
    //  IDRETRY instructs the engine to try the verification of the payload again.
    //          Ignored if hrStatus is success.
    //
    //  IDTRYAGAIN instructs the engine to acquire the payload again. Ignored if
    //             hrStatus is success.
    //
    //  All other return codes are ignored.
    STDMETHOD_(int, OnCacheVerifyComplete)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in int nRecommendation
        ) = 0;

    // OnCachePackageComplete - called after the engine attempts to copy or download all
    //                          payloads of a package into the package cache folder.
    //
    // Return:
    //  IDIGNORE instructs the engine to ignore non-vital package failures and continue with the
    //           caching. Ignored if hrStatus is a success or the package is vital.
    //
    //  IDRETRY instructs the engine to try the acquisition and verification of the package
    //          again. Ignored if hrStatus is a success.
    //
    //  All other return codes are ignored.
    STDMETHOD_(int, OnCachePackageComplete)(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in int nRecommendation
        )  = 0;

    // OnCacheComplete - called when the engine caching is complete.
    //
    STDMETHOD_(void, OnCacheComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnExecuteBegin - called when the engine begins executing the plan.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop applying.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnExecuteBegin)(
        __in DWORD cExecutingPackages
        ) = 0;

    // OnExecuteBegin - called when the engine begins executing a package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop applying.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnExecutePackageBegin)(
        __in_z LPCWSTR wzPackageId,
        __in BOOL fExecute
        ) = 0;

    // OnExecutePatchTarget - called when the engine executes one or more patches targeting
    //                        a product.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop applying.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnExecutePatchTarget)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzTargetProductCode
        ) = 0;

    // OnExecuteProgress - called when the engine makes progress executing a package.
    //
    // Return:
    //  IDCANCEL instructs the engine to stop applying.
    //
    //  IDNOACTION instructs the engine to continue.
    STDMETHOD_(int, OnExecuteProgress)(
        __in_z LPCWSTR wzPackageId,
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallPercentage
        ) = 0;

    // OnExecuteMsiMessage - called when the engine receives an MSI package message.
    //
    // Return:
    //  uiFlags is a combination of valid ID* return values appropriate for
    //          the message.
    //
    //  IDNOACTION instructs the engine to pass the message through to default
    //             handling which usually results in the execution continuing.
    STDMETHOD_(int, OnExecuteMsiMessage)(
        __in_z LPCWSTR wzPackageId,
        __in INSTALLMESSAGE mt,
        __in UINT uiFlags,
        __in_z LPCWSTR wzMessage,
        __in DWORD cData,
        __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
        __in int nRecommendation
        ) = 0;

    // OnExecuteFilesInUse - called when the engine encounters files in use while
    //                       executing a package.
    //
    // Return:
    //  IDOK instructs the engine to let the Restart Manager attempt to close the
    //       applications to avoid a restart.
    //
    //  IDCANCEL instructs the engine to abort the execution and start rollback.
    //
    //  IDIGNORE instructs the engine to ignore the running applications. A restart will be
    //           required.
    //
    //  IDRETRY instructs the engine to check if the applications are still running again.
    //
    //  IDNOACTION is equivalent to ignoring the running applications. A restart will be
    //             required.
    STDMETHOD_(int, OnExecuteFilesInUse)(
        __in_z LPCWSTR wzPackageId,
        __in DWORD cFiles,
        __in_ecount_z(cFiles) LPCWSTR* rgwzFiles
        ) = 0;

    // OnExecutePackageComplete - called when a package execution is complete.
    //
    // Parameters:
    //  restart will indicate whether this package requires a reboot or initiated the reboot already.
    //
    // Return:
    //  IDIGNORE instructs the engine to ignore non-vital package failures and continue with the
    //           install. Ignored if hrStatus is a success or the package is vital.
    //
    //  IDRETRY instructs the engine to try the execution of the package again. Ignored if hrStatus
    //          is a success.
    //
    //  IDRESTART instructs the engine to stop processing the chain and restart. The engine will
    //            launch again after the machine is restarted.
    //
    //  IDSUSPEND instructs the engine to stop processing the chain and suspend the current state.
    //
    //  All other return codes are ignored.
    STDMETHOD_(int, OnExecutePackageComplete)(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART restart,
        __in int nRecommendation
        ) = 0;

    // OnExecuteComplete - called when the engine execution is complete.
    //
    STDMETHOD_(void, OnExecuteComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnUnregisterBegin - called when the engine unregisters the bundle.
    //
    STDMETHOD_(void, OnUnregisterBegin)() = 0;

    // OnUnregisterComplete - called when the engine unregistration is complete.
    //
    STDMETHOD_(void, OnUnregisterComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnApplyComplete - called after the plan has been applied.
    //
    // Parameters:
    //  restart will indicate whether any package required a reboot or initiated the reboot already.
    //
    // Return:
    //  IDRESTART instructs the engine to restart. The engine will not launch again after the machine
    //            is rebooted. Ignored if reboot was already initiated by OnExecutePackageComplete().
    //
    //  All other return codes are ignored.
    STDMETHOD_(int, OnApplyComplete)(
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART restart
        ) = 0;

    // OnLaunchApprovedExeBegin - called before trying to launch the preapproved executable.
    // 
    STDMETHOD_(int, OnLaunchApprovedExeBegin)() = 0;


    // OnLaunchApprovedExeComplete - called after trying to launch the preapproved executable.
    //
    // Parameters:
    //  dwProcessId is only valid if the operation succeeded.
    //
    STDMETHOD_(void, OnLaunchApprovedExeComplete)(
        __in HRESULT hrStatus,
        __in DWORD dwProcessId
        ) = 0;

    // BAProc - The PFN_BOOTSTRAPPER_APPLICATION_PROC can call this method to give the BA raw access to the callback from the engine.
    //          This might be used to help the BA support more than one version of the engine.
    STDMETHOD(BAProc)(
        __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __in_opt LPVOID pvContext
        ) = 0;

    // BAProcFallback - The PFN_BOOTSTRAPPER_APPLICATION_PROC can call this method
    //                  to give the BA the ability to use default behavior
    //                  and then forward the message to extensions.
    STDMETHOD_(void, BAProcFallback)(
        __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __inout HRESULT* phr,
        __in_opt LPVOID pvContext
        ) = 0;
};


// TODO: Move into BootstrapperApplication.h once IBootstrapperEngine and IBootstrapperApplication have been moved out of the engine.
struct BOOTSTRAPPER_CREATE_ARGS
{
    DWORD cbSize;
    DWORD64 qwEngineAPIVersion;
    IBootstrapperEngine* pEngine; // TODO: remove once IBootstrapperEngine is moved out of the engine.
    PFN_BOOTSTRAPPER_ENGINE_PROC pfnBootstrapperEngineProc;
    LPVOID pvBootstrapperEngineProcContext;
    BOOTSTRAPPER_COMMAND* pCommand;
};

struct BOOTSTRAPPER_CREATE_RESULTS
{
    DWORD cbSize;
    IBootstrapperApplication* pApplication; // TODO: remove once IBootstrapperApplication is moved out of the engine.
    PFN_BOOTSTRAPPER_APPLICATION_PROC pfnBootstrapperApplicationProc;
    LPVOID pvBootstrapperApplicationProcContext;
};

extern "C" typedef HRESULT(WINAPI *PFN_BOOTSTRAPPER_APPLICATION_CREATE)(
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    );
