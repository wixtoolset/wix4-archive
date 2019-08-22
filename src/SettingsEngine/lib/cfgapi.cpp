// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

volatile static DWORD s_dwRefCount = 0;
static BOOL s_fXmlInitialized = FALSE;
static CFGDB_STRUCT s_cdb = { };
volatile static BOOL vfComInitialized = FALSE;

const int CFGDB_HANDLE_BYTES = sizeof(CFGDB_STRUCT);
const int CFG_ENUMERATION_HANDLE_BYTES = sizeof(CFG_ENUMERATION);

static HRESULT InitializeImpersonationToken(
    __inout CFGDB_STRUCT *pcdb
    );

extern "C" HRESULT CFGAPI CfgInitialize(
    __deref_out_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE *pcdHandle,
    __in_opt PFN_BACKGROUNDSTATUS vpfBackgroundStatus,
    __in_opt PFN_BACKGROUNDCONFLICTSFOUND vpfConflictsFound,
    __in_opt LPVOID pvCallbackContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDbFilePath = NULL;
    CFGDB_STRUCT *pcdb = &s_cdb;

    ExitOnNull(pcdHandle, hr, E_INVALIDARG, "Database handle output pointer must not be NULL");

    *pcdHandle = pcdb;

    ::InterlockedIncrement(&s_dwRefCount);

    if (1 == s_dwRefCount)
    {
        ExitOnNull(vpfBackgroundStatus, hr, E_INVALIDARG, "Background status function pointer must not be NULL");
        ExitOnNull(vpfConflictsFound, hr, E_INVALIDARG, "Conflicts found function pointer must not be NULL");

        if (!IsLogInitialized())
        {
            LogInitialize(NULL);
        }

        hr = LogOpen(NULL, L"CfgAPI", NULL, L".log", FALSE, TRUE, NULL);
        ExitOnFailure(hr, "Failed to initialize log");

        hr = ::CoInitialize(0);
        ExitOnFailure(hr, "Failed to initialize COM");
        vfComInitialized = TRUE;

        ZeroMemory(&s_cdb, sizeof(s_cdb));
        ::InitializeCriticalSection(&pcdb->cs);
        pcdb->dwAppID = DWORD_MAX;

        hr = CfgAdminInitialize(reinterpret_cast<CFGDB_HANDLE *>(&pcdb->pcdbAdmin), FALSE);
        ExitOnFailure(hr, "Failed to initialize Cfg Admin Db");

        hr = InitializeImpersonationToken(pcdb);
        ExitOnFailure(hr, "Failed to initialize impersonation token");

        hr = DatabaseGetUserDir(&pcdb->sczDbDir);
        ExitOnFailure(hr, "Failed to get user database directory");

        hr = XmlInitialize();
        ExitOnFailure(hr, "Failed to initialize MSXML");
        s_fXmlInitialized = TRUE;

        // Setup expected schema in memory
        hr = DatabaseSetupSchema(DATABASE_TYPE_LOCAL, &pcdb->dsSceDb);
        ExitOnFailure(hr, "Failed to setup user database schema structure in memory");

        // Get the path to the exact file
        hr = DatabaseGetUserPath(&sczDbFilePath);
        ExitOnFailure(hr, "Failed to get user database path");

        // Create the database
        hr = SceEnsureDatabase(sczDbFilePath, wzSqlCeDllPath, L"CfgUser", 1, &pcdb->dsSceDb, &pcdb->psceDb);
        ExitOnFailure(hr, "Failed to create SQL CE database");

        hr = HandleEnsureSummaryDataTable(pcdb);
        ExitOnFailure(hr, "Failed to ensure summary data");

        hr = PathConcat(pcdb->sczDbDir, L"Streams", &pcdb->sczStreamsDir);
        ExitOnFailure(hr, "Failed to get path to streams directory");

        hr = ProductEnsureCreated(pcdb, wzCfgProductId, wzCfgVersion, wzCfgPublicKey, &pcdb->dwCfgAppID, NULL);
        ExitOnFailure(hr, "Failed to ensure cfg product id exists");

        pcdb->vpfBackgroundStatus = vpfBackgroundStatus;
        pcdb->vpfConflictsFound = vpfConflictsFound;
        pcdb->pvCallbackContext = pvCallbackContext;

        hr = ProductRegister(pcdb, wzCfgProductId, wzCfgVersion, wzCfgPublicKey, TRUE);
        ExitOnFailure(hr, "Failed to register cfg product");

        pcdb->hBackgroundThreadWaitOnStartup = ::CreateEventW(NULL, TRUE, FALSE, NULL);
        ExitOnNullWithLastError(pcdb->hBackgroundThreadWaitOnStartup, hr, "Failed to create anonymous event for background thread");

        hr = BackgroundStartThread(pcdb);
        ExitOnFailure(hr, "Failed to start background thread");
    }

LExit:
    ReleaseStr(sczDbFilePath);

    return hr;
}

extern "C" HRESULT CFGAPI CfgUninitialize(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");

    ::InterlockedDecrement(&s_dwRefCount);

    if (0 == s_dwRefCount)
    {
        hr = BackgroundStopThread(pcdb);
        ExitOnFailure(hr, "Failed to start background thread");

        ::CloseHandle(pcdb->hBackgroundThreadWaitOnStartup);

        pcdb->dwAppID = DWORD_MAX;
        pcdb->fProductSet = FALSE;
        ReleaseNullStr(pcdb->sczGuid);
        ReleaseNullStr(pcdb->sczGuidLocalInRemoteKey);
        ReleaseNullStr(pcdb->sczGuidRemoteInLocalKey);
        ReleaseNullStr(pcdb->sczDbCopiedPath);
        ReleaseNullStr(pcdb->sczDbDir);
        ReleaseNullStr(pcdb->sczStreamsDir);
        ReleaseNullStrArray(pcdb->rgsczStreamsToDelete, pcdb->cStreamsToDelete);

        if (s_fXmlInitialized)
        {
            s_fXmlInitialized = FALSE;
            XmlUninitialize();
        }

        hr = CfgAdminUninitialize(pcdb->pcdbAdmin);
        ExitOnFailure(hr, "Failed to uninitialize Cfg Admin Db");

        hr = SceCloseDatabase(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to close user database");

        DatabaseReleaseSceSchema(&pcdb->dsSceDb);

        if (vfComInitialized)
        {
            ::CoUninitialize();
            vfComInitialized = FALSE;
        }

        ReleaseHandle(pcdb->hToken);

        LogUninitialize(TRUE);

        ReleaseNullMem(pcdb->rgpcdbOpenDatabases);
        ::DeleteCriticalSection(&pcdb->cs);
    }

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgResumeBackgroundThread(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);

    if (pcdb->fBackgroundThreadWaitOnStartupTriggered)
    {
        // Already triggered, nothing to do
        ExitFunction1(hr = S_OK);
    }

    if (!::SetEvent(pcdb->hBackgroundThreadWaitOnStartup))
    {
        ExitWithLastError(hr, "Failed to set background thread wait on startup event while shutting down cfg api");
    }

    pcdb->fBackgroundThreadWaitOnStartupTriggered = TRUE;

LExit:
    return hr;
}

extern "C" HRESULT CfgGetEndpointGuid(
    __in_bcount(CFGDB_HANDLE_BYTES) C_CFGDB_HANDLE cdHandle,
    __out_z LPWSTR *psczGuid
    )
{
    HRESULT hr = S_OK;
    const CFGDB_STRUCT *pcdb = static_cast<const CFGDB_STRUCT *>(cdHandle);

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(psczGuid, hr, E_INVALIDARG, "Guid must not be NULL");

    if (NULL == pcdb->sczGuid)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = StrAllocString(psczGuid, pcdb->sczGuid, 0);
    ExitOnFailure(hr, "Failed to allocate copy of guid string");

LExit:
    return hr;
}

// Generally this is the first call after CfgInitialize() an app will make - it tells the settings engine your application's unique identifier,
// and is roughly equivalent to setting the namespace for all your configuration data which will be stored
extern "C" HRESULT CfgSetProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LPWSTR sczLowPublicKey = NULL;
    BOOL fLegacyProduct = (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wzPublicKey, -1, wzLegacyPublicKey, -1));
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzProductName, hr, E_INVALIDARG, "Product Name must not be NULL");
    ExitOnNull(wzVersion, hr, E_INVALIDARG, "Version must not be NULL");
    ExitOnNull(wzPublicKey, hr, E_INVALIDARG, "Public Key must not be NULL for non-legacy databases");

    hr = StrAllocString(&sczLowPublicKey, wzPublicKey, 0);
    ExitOnFailure(hr, "Failed to allocate 2nd public key buffer");

    // Convert to lower case characters
    StrStringToLower(sczLowPublicKey);

    hr = ProductValidateName(wzProductName);
    ExitOnFailure(hr, "Failed to validate ProductName: %ls", wzProductName);

    hr = ProductValidateVersion(wzVersion);
    ExitOnFailure(hr, "Failed to validate Version while setting product: %ls", wzVersion);

    hr = ProductValidatePublicKey(sczLowPublicKey);
    ExitOnFailure(hr, "Failed to validate Public Key: %ls", sczLowPublicKey);

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when setting produt");
    fLocked = TRUE;

    // Unset the previously set product in case caller ignores a
    // failed return value and starts writing to the previously set product
    pcdb->fProductSet = FALSE;

    // Don't allow creating legacy products from here, because they won't have manifests and thus won't be sync-able
    hr = ProductSet(pcdb, wzProductName, wzVersion, sczLowPublicKey, fLegacyProduct, NULL);
    ExitOnFailure(hr, "Failed to call internal set product function");

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseStr(sczLowPublicKey);

    return hr;
}

// Get / set DWORD values
extern "C" HRESULT CfgSetDword(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in DWORD dwValue
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LEGACY_SYNC_SESSION syncSession = { };
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when setting dword");
    fLocked = TRUE;

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        hr = LegacySyncInitializeSession(TRUE, FALSE, &syncSession);
        ExitOnFailure(hr, "Failed to initialize legacy sync session");

        hr = LegacySyncSetProduct(pcdb, &syncSession, pcdb->sczProductName);
        ExitOnFailure(hr, "Failed to set product in legacy sync session");

        hr = LegacyProductMachineToDb(pcdb, &syncSession.syncProductSession);
        ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");
    }

    hr = ValueSetDword(dwValue, NULL, pcdb->sczGuid, &cvValue);
    ExitOnFailure(hr, "Failed to set dword value in memory");

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzName, &cvValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set DWORD value: %u", dwValue);

    if (!pcdb->fRemote)
    {
        if (pcdb->fProductIsLegacy)
        {
            hr = LegacySyncFinalizeProduct(pcdb, &syncSession);
            ExitOnFailure(hr, "Failed to finalize product in legacy sync session");
        }
        else
        {
            hr = BackgroundSyncRemotes(pcdb);
            ExitOnFailure(hr, "Failed to sync remotes");
        }
    }
    else
    {
        hr = BackgroundMarkRemoteChanged(pcdb);
        ExitOnFailure(hr, "Failed to mark remote as changed");
    }

LExit:
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        LegacySyncUninitializeSession(pcdb, &syncSession);
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CfgGetDword(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out DWORD *pdwValue
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    SCE_ROW_HANDLE sceRow = NULL;
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzName, hr, E_INVALIDARG, "Name of value must not be NULL");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when getting dword");
    fLocked = TRUE;

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = ValueFindRow(pcdb, pcdb->dwAppID, wzName, &sceRow);
    ExitOnFailure(hr, "Failed to find config value for AppID: %u, Config Value named: %ls", pcdb->dwAppID, wzName);

    hr = ValueRead(pcdb, sceRow, &cvValue);
    ExitOnFailure(hr, "Failed to retrieve deleted column");

    if (VALUE_DELETED == cvValue.cvType)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    if (VALUE_DWORD != cvValue.cvType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Tried to retrieve value as dword, but it's of type: %d", cvValue.cvType);
    }

    *pdwValue = cvValue.dword.dwValue;

LExit:
    ReleaseSceRow(sceRow);
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

// Get / set QWORD values
extern "C" HRESULT CfgSetQword(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in DWORD64 qwValue
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LEGACY_SYNC_SESSION syncSession = { };
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when setting qword");
    fLocked = TRUE;

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        hr = LegacySyncInitializeSession(TRUE, FALSE, &syncSession);
        ExitOnFailure(hr, "Failed to initialize legacy sync session");

        hr = LegacySyncSetProduct(pcdb, &syncSession, pcdb->sczProductName);
        ExitOnFailure(hr, "Failed to set product in legacy sync session");

        hr = LegacyProductMachineToDb(pcdb, &syncSession.syncProductSession);
        ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");
    }

    hr = ValueSetQword(qwValue, NULL, pcdb->sczGuid, &cvValue);
    ExitOnFailure(hr, "Failed to set qword value in memory");

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzName, &cvValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set QWORD value: %I64u", qwValue);

    if (!pcdb->fRemote)
    {
        if (pcdb->fProductIsLegacy)
        {
            hr = LegacySyncFinalizeProduct(pcdb, &syncSession);
            ExitOnFailure(hr, "Failed to finalize product in legacy sync session");
        }
        else
        {
            hr = BackgroundSyncRemotes(pcdb);
            ExitOnFailure(hr, "Failed to sync remotes");
        }
    }
    else
    {
        hr = BackgroundMarkRemoteChanged(pcdb);
        ExitOnFailure(hr, "Failed to mark remote as changed");
    }

LExit:
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        LegacySyncUninitializeSession(pcdb, &syncSession);
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CfgGetQword(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out DWORD64 *pqwValue
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    SCE_ROW_HANDLE sceRow = NULL;
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzName, hr, E_INVALIDARG, "Name of value must not be NULL");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when getting qword");
    fLocked = TRUE;

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = ValueFindRow(pcdb, pcdb->dwAppID, wzName, &sceRow);
    ExitOnFailure(hr, "Failed to find config value for AppID: %u, Config Value named: %ls", pcdb->dwAppID, wzName);

    hr = ValueRead(pcdb, sceRow, &cvValue);
    ExitOnFailure(hr, "Failed to retrieve deleted column");

    if (VALUE_DELETED == cvValue.cvType)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    if (VALUE_QWORD != cvValue.cvType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Tried to retrieve value as dword, but it's of type: %d", cvValue.cvType);
    }

    *pqwValue = cvValue.qword.qwValue;

LExit:
    ReleaseSceRow(sceRow);
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

// Get / set string values
extern "C" HRESULT CfgSetString(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in_z LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LEGACY_SYNC_SESSION syncSession = { };
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when setting string");
    fLocked = TRUE;

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    // TODO: Optimize this better (only write the value changed)
    // Also TODO: better transactionality - catch the situation when the data on local machine has
    // changed since last read, and return error in this case
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        hr = LegacySyncInitializeSession(TRUE, FALSE, &syncSession);
        ExitOnFailure(hr, "Failed to initialize legacy sync session");

        hr = LegacySyncSetProduct(pcdb, &syncSession, pcdb->sczProductName);
        ExitOnFailure(hr, "Failed to set product in legacy sync session");

        hr = LegacyProductMachineToDb(pcdb, &syncSession.syncProductSession);
        ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");
    }

    hr = ValueSetString(wzValue, FALSE, NULL, pcdb->sczGuid, &cvValue);
    ExitOnFailure(hr, "Failed to set string value in memory");

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzName, &cvValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set string value '%ls' to '%ls'", wzName, wzValue);

    if (!pcdb->fRemote)
    {
        if (pcdb->fProductIsLegacy)
        {
            hr = LegacySyncFinalizeProduct(pcdb, &syncSession);
            ExitOnFailure(hr, "Failed to finalize product in legacy sync session");
        }
        else
        {
            hr = BackgroundSyncRemotes(pcdb);
            ExitOnFailure(hr, "Failed to sync remotes");
        }
    }
    else
    {
        hr = BackgroundMarkRemoteChanged(pcdb);
        ExitOnFailure(hr, "Failed to mark remote as changed");
    }

LExit:
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        LegacySyncUninitializeSession(pcdb, &syncSession);
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CfgGetString(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out LPWSTR *psczValue
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    SCE_ROW_HANDLE sceRow = NULL;
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzName, hr, E_INVALIDARG, "Name of value must not be NULL");

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when getting string");
    fLocked = TRUE;

    hr = ValueFindRow(pcdb, pcdb->dwAppID, wzName, &sceRow);
    ExitOnFailure(hr, "Failed to find config value for AppID: %u, Config Value named: %ls", pcdb->dwAppID, wzName);

    hr = ValueRead(pcdb, sceRow, &cvValue);
    ExitOnFailure(hr, "Failed to retrieve deleted column");

    if (VALUE_DELETED == cvValue.cvType)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    if (VALUE_STRING != cvValue.cvType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Tried to retrieve value as string, but it's of type: %d", cvValue.cvType);
    }

    ReleaseStr(*psczValue);
    *psczValue = cvValue.string.sczValue;
    cvValue.string.sczValue = NULL;

LExit:
    ReleaseSceRow(sceRow);
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CFGAPI CfgSetBool(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in BOOL fValue
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LEGACY_SYNC_SESSION syncSession = { };
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when setting bool");
    fLocked = TRUE;

    // TODO: Optimize this better (only write the value changed)
    // Also TODO: better transactionality - catch the situation when the data on local machine has
    // changed since last read, and return error in this case
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        hr = LegacySyncInitializeSession(TRUE, FALSE, &syncSession);
        ExitOnFailure(hr, "Failed to initialize legacy sync session");

        hr = LegacySyncSetProduct(pcdb, &syncSession, pcdb->sczProductName);
        ExitOnFailure(hr, "Failed to set product in legacy sync session");

        hr = LegacyProductMachineToDb(pcdb, &syncSession.syncProductSession);
        ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");
    }

    hr = ValueSetBool(fValue, NULL, pcdb->sczGuid, &cvValue);
    ExitOnFailure(hr, "Failed to set bool value in memory");

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzName, &cvValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set BOOL value named: %ls", wzName);

    if (!pcdb->fRemote)
    {
        if (pcdb->fProductIsLegacy)
        {
            hr = LegacySyncFinalizeProduct(pcdb, &syncSession);
            ExitOnFailure(hr, "Failed to finalize product in legacy sync session");
        }
        else
        {
            hr = BackgroundSyncRemotes(pcdb);
            ExitOnFailure(hr, "Failed to sync remotes");
        }
    }
    else
    {
        hr = BackgroundMarkRemoteChanged(pcdb);
        ExitOnFailure(hr, "Failed to mark remote as changed");
    }

LExit:
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        LegacySyncUninitializeSession(pcdb, &syncSession);
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CFGAPI CfgGetBool(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __out BOOL *pfValue
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    SCE_ROW_HANDLE sceRow = NULL;
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzName, hr, E_INVALIDARG, "Name of value must not be NULL");

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when getting bool");
    fLocked = TRUE;

    hr = ValueFindRow(pcdb, pcdb->dwAppID, wzName, &sceRow);
    ExitOnFailure(hr, "Failed to find config value for AppID: %u, Config Value named: %ls", pcdb->dwAppID, wzName);

    hr = ValueRead(pcdb, sceRow, &cvValue);
    ExitOnFailure(hr, "Failed to retrieve deleted column");

    if (VALUE_DELETED == cvValue.cvType)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    if (VALUE_BOOL != cvValue.cvType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Tried to retrieve value as bool, but it's of type: %d", cvValue.cvType);
    }

    *pfValue = cvValue.boolean.fValue;

LExit:
    ReleaseSceRow(sceRow);
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CfgDeleteValue(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LEGACY_SYNC_SESSION syncSession = { };
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzName, hr, E_INVALIDARG, "Name of value must not be NULL");

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when deleting value");
    fLocked = TRUE;

    // TODO: Optimize this better (only write the value changed)
    // Also TODO: better transactionality - catch the situation when the data on local machine has
    // changed since last read, and return error in this case
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        hr = LegacySyncInitializeSession(TRUE, FALSE, &syncSession);
        ExitOnFailure(hr, "Failed to initialize legacy sync session");

        hr = LegacySyncSetProduct(pcdb, &syncSession, pcdb->sczProductName);
        ExitOnFailure(hr, "Failed to set product in legacy sync session");

        hr = LegacyProductMachineToDb(pcdb, &syncSession.syncProductSession);
        ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");
    }

    hr = ValueSetDelete(NULL, pcdb->sczGuid, &cvValue);
    ExitOnFailure(hr, "Failed to set delete value in memory");

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzName, &cvValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to delete value: %ls", wzName);

    if (!pcdb->fRemote)
    {
        if (pcdb->fProductIsLegacy)
        {
            hr = LegacySyncFinalizeProduct(pcdb, &syncSession);
            ExitOnFailure(hr, "Failed to finalize product in legacy sync session");
        }
        else
        {
            hr = BackgroundSyncRemotes(pcdb);
            ExitOnFailure(hr, "Failed to sync remotes");
        }
    }
    else
    {
        hr = BackgroundMarkRemoteChanged(pcdb);
        ExitOnFailure(hr, "Failed to mark remote as changed");
    }

LExit:
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        LegacySyncUninitializeSession(pcdb, &syncSession);
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CFGAPI CfgSetBlob(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LEGACY_SYNC_SESSION syncSession = { };
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzName, hr, E_INVALIDARG, "Name of file must not be NULL");

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when setting blob");
    fLocked = TRUE;

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    if (cbBuffer > 0)
    {
        ExitOnNull(pbBuffer, hr, E_INVALIDARG, "Size of file was more than zero, but no content was provided!");
    }
    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    // TODO: Optimize this better (only write the value changed)
    // Also TODO: better transactionality - catch the situation when the data on local machine has
    // changed since last read, and return error in this case
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        hr = LegacySyncInitializeSession(TRUE, FALSE, &syncSession);
        ExitOnFailure(hr, "Failed to initialize legacy sync session");

        hr = LegacySyncSetProduct(pcdb, &syncSession, pcdb->sczProductName);
        ExitOnFailure(hr, "Failed to set product in legacy sync session");

        hr = LegacyProductMachineToDb(pcdb, &syncSession.syncProductSession);
        ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");
    }

    hr = ValueSetBlob(pbBuffer, cbBuffer, FALSE, NULL, pcdb->sczGuid, &cvValue);
    ExitOnFailure(hr, "Failed to set blob value in memory");

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzName, &cvValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set blob: %ls", wzName);

    if (!pcdb->fRemote)
    {
        if (pcdb->fProductIsLegacy)
        {
            hr = LegacySyncFinalizeProduct(pcdb, &syncSession);
            ExitOnFailure(hr, "Failed to finalize product in legacy sync session");
        }
        else
        {
            hr = BackgroundSyncRemotes(pcdb);
            ExitOnFailure(hr, "Failed to sync remotes");
        }
    }
    else
    {
        hr = BackgroundMarkRemoteChanged(pcdb);
        ExitOnFailure(hr, "Failed to mark remote as changed");
    }

LExit:
    if (!pcdb->fRemote && pcdb->fProductIsLegacy)
    {
        LegacySyncUninitializeSession(pcdb, &syncSession);
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CFGAPI CfgGetBlob(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __deref_opt_out_bcount_opt(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    DWORD dwContentID = DWORD_MAX;
    SCE_ROW_HANDLE sceRow = NULL;
    CONFIG_VALUE cvValue = { };
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(ppbBuffer, hr, E_INVALIDARG, "Byte buffer must not be NULL");
    ExitOnNull(piBuffer, hr, E_INVALIDARG, "Size buffer must not be NULL");

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when getting blob");
    fLocked = TRUE;

    hr = ValueFindRow(pcdb, pcdb->dwAppID, wzName, &sceRow);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to find blob named: %ls for AppID: %u", wzName, pcdb->dwAppID);

    hr = ValueRead(pcdb, sceRow, &cvValue);
    ExitOnFailure(hr, "Failed to retrieve deleted column");

    if (VALUE_DELETED == cvValue.cvType)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    if (VALUE_BLOB != cvValue.cvType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Tried to retrieve value as blob, but it's of type: %d", cvValue.cvType);
    }

    if (CFG_BLOB_DB_STREAM != cvValue.blob.cbType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Tried to retrieve value as db stream blob, but it's of type: %d", cvValue.blob.cbType);
    }

    hr = StreamRead(pcdb, cvValue.blob.dbstream.dwContentID, NULL, ppbBuffer, piBuffer);
    ExitOnFailure(hr, "Failed to get binary content of blob named: %ls, with content ID: %u", wzName, dwContentID);

LExit:
    ReleaseSceRow(sceRow);
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);

    return hr;
}

extern "C" HRESULT CfgEnumerateValues(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in CONFIG_VALUETYPE cvType,
    __deref_out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out_opt DWORD *pcCount
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(ppvHandle, hr, E_INVALIDARG, "Output handle must not be NULL");

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when enumerating values");
    fLocked = TRUE;

    hr = EnumValues(pcdb, cvType, reinterpret_cast<CFG_ENUMERATION **>(ppvHandle), pcCount);
    ExitOnFailure(hr, "Failed to enumerate values");

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

extern "C" HRESULT CfgEnumerateProducts(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __deref_out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out_opt DWORD *pcCount
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    SCE_ROW_HANDLE sceRow = NULL;
    BOOL fLocked = FALSE;
    DWORD dwAppID = DWORD_MAX;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(ppvHandle, hr, E_INVALIDARG, "Must pass in pointer to output handle to CfgEnumerateProducts()");

    // Allocate the Enumeration struct and its members
    CFG_ENUMERATION *pcesEnum = static_cast<CFG_ENUMERATION *>(MemAlloc(sizeof(CFG_ENUMERATION), TRUE));
    ExitOnNull(pcesEnum, hr, E_OUTOFMEMORY, "Failed to allocate Cfg Enumeration Struct");

    pcesEnum->enumType = ENUMERATION_PRODUCTS;

    hr = EnumResize(pcesEnum, 64);
    ExitOnFailure(hr, "Failed to resize enumeration struct immediately after its creation");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when enumerating products");
    fLocked = TRUE;

    hr = SceGetFirstRow(pcdb->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get row from table: %u", PRODUCT_INDEX_TABLE);

        if (pcesEnum->dwNumValues >= pcesEnum->dwMaxValues)
        {
            DWORD dwNewSize = pcesEnum->dwMaxValues * 2;

            hr = EnumResize(pcesEnum, dwNewSize);
            ExitOnFailure(hr, "Failed to resize enumeration struct");
        }

        hr = SceGetColumnString(sceRow, PRODUCT_NAME, &(pcesEnum->products.rgsczName[pcesEnum->dwNumValues]));
        ExitOnFailure(hr, "Failed to retrieve product name while enumerating products");

        hr = SceGetColumnString(sceRow, PRODUCT_VERSION, &(pcesEnum->products.rgsczVersion[pcesEnum->dwNumValues]));
        ExitOnFailure(hr, "Failed to retrieve version while enumerating products");

        hr = SceGetColumnString(sceRow, PRODUCT_PUBLICKEY, &(pcesEnum->products.rgsczPublicKey[pcesEnum->dwNumValues]));
        ExitOnFailure(hr, "Failed to retrieve public key while enumerating products");

        hr = SceGetColumnBool(sceRow, PRODUCT_REGISTERED, &(pcesEnum->products.rgfRegistered[pcesEnum->dwNumValues]));
        ExitOnFailure(hr, "Failed to retrieve registered flag while enumerating products");

        hr = SceGetColumnDword(sceRow, PRODUCT_ID, &dwAppID);
        ExitOnFailure(hr, "Failed to retrieve dwAppID while enumerating products");

        hr = DisplayNameEnumerate(pcdb, dwAppID, &(pcesEnum->products.rgrgDisplayNames[pcesEnum->dwNumValues]), &(pcesEnum->products.rgcDisplayNames[pcesEnum->dwNumValues]));
        ExitOnFailure(hr, "Failed to retrieve registered flag while enumerating products");

        ++pcesEnum->dwNumValues;

        ReleaseNullSceRow(sceRow);
        hr = SceGetNextRow(pcdb->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
    }

    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }

    if (NULL != pcCount)
    {
        *pcCount = pcesEnum->dwNumValues;
    }

    if (0 == pcesEnum->dwNumValues)
    {
        EnumFree(pcesEnum);
        pcesEnum = NULL;
    }
    else if (pcesEnum->dwNumValues < pcesEnum->dwMaxValues)
    {
        // Now that we'll no longer be adding to the struct, free any unneeded space we allocated
        hr = EnumResize(pcesEnum, pcesEnum->dwNumValues);
        ExitOnFailure(hr, "Failed to free unneeded memory from enumeration struct");
    }

    *ppvHandle = (static_cast<CFG_ENUMERATION_HANDLE>(pcesEnum));

LExit:
    ReleaseSceRow(sceRow);
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

extern "C" HRESULT CfgEnumPastValues(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzName,
    __deref_opt_out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out DWORD *pcCount
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzName, hr, E_INVALIDARG, "Value name must not be NULL");

    if (!pcdb->fProductSet)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME));
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when enumerating past values");
    fLocked = TRUE;

    hr = EnumPastValues(pcdb, wzName, reinterpret_cast<CFG_ENUMERATION **>(ppvHandle), pcCount);
    ExitOnFailure(hr, "Failed to call internal enumerate past values function on value named: %ls", wzName);

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumDatabaseList(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __deref_opt_out_bcount(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *ppvHandle,
    __out DWORD *pcCount
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when enumerating database list");
    fLocked = TRUE;

    hr = EnumDatabaseList(pcdb, reinterpret_cast<CFG_ENUMERATION **>(ppvHandle), pcCount);
    ExitOnFailure(hr, "Failed to call internal enumerate database list function");

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadDataType(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out_opt CONFIG_VALUETYPE *pcvType
    )
{
    HRESULT hr = S_OK;
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);

    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadDataType() requires an enumeration handle");
    ExitOnNull(cedData, hr, E_INVALIDARG, "CfgEnumReadDataType()'s CFG_ENUM_DATA parameter must not be ENUM_DATA_NULL");
    ExitOnNull(pcvType, hr, E_INVALIDARG, "CfgEnumReadDataType()'s output parameter must not be NULL");

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    switch (pcesEnum->enumType)
    {
    case ENUMERATION_VALUES:
        switch (cedData)
        {
        case ENUM_DATA_VALUETYPE:
            ExitOnNull(pcvType, hr, E_INVALIDARG, "CfgEnumReadDataType() must not be sent NULL for pdwDword parameter when requesting ENUM_DATA_VALUEDWORD");

            *pcvType = pcesEnum->values.rgcValues[dwIndex].cvType;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for datatype. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    case ENUMERATION_VALUE_HISTORY:
        switch (cedData)
        {
        case ENUM_DATA_VALUETYPE:
            ExitOnNull(pcvType, hr, E_INVALIDARG, "CfgEnumReadDataType() must not be sent NULL for pdwDword parameter when requesting ENUM_DATA_VALUEDWORD");

            *pcvType = pcesEnum->valueHistory.rgcValues[dwIndex].cvType;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for datatype. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unsupported request for datatype. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadString(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __deref_opt_out_z LPCWSTR *pwzString
    )
{
    HRESULT hr = S_OK;
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);

    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadString() requires an enumeration handle");
    ExitOnNull(cedData, hr, E_INVALIDARG, "CfgEnumReadString()'s CFG_ENUM_DATA parameter must not be ENUM_DATA_NULL");
    ExitOnNull(pwzString, hr, E_INVALIDARG, "CfgEnumReadString() must not be sent NULL for string output parameter");

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    switch (pcesEnum->enumType)
    {
    case ENUMERATION_VALUES:
        switch (cedData)
        {
        case ENUM_DATA_VALUENAME:
            *pwzString = pcesEnum->values.rgsczName[dwIndex];
            break;

        case ENUM_DATA_VALUESTRING:
            *pwzString = pcesEnum->values.rgcValues[dwIndex].string.sczValue;
            break;

        case ENUM_DATA_BY:
            *pwzString = pcesEnum->valueHistory.rgcValues[dwIndex].sczBy;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for string. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    case ENUMERATION_PRODUCTS:
        switch (cedData)
        {
        case ENUM_DATA_PRODUCTNAME:
            *pwzString = pcesEnum->products.rgsczName[dwIndex];
            break;

        case ENUM_DATA_VERSION:
            *pwzString = pcesEnum->products.rgsczVersion[dwIndex];
            break;

        case ENUM_DATA_PUBLICKEY:
            *pwzString = pcesEnum->products.rgsczPublicKey[dwIndex];
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for string. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    case ENUMERATION_VALUE_HISTORY:
        switch (cedData)
        {
        case ENUM_DATA_VALUENAME:
            *pwzString = pcesEnum->valueHistory.sczName;
            break;

        case ENUM_DATA_VALUESTRING:
            *pwzString = pcesEnum->valueHistory.rgcValues[dwIndex].string.sczValue;
            break;

        case ENUM_DATA_BY:
            *pwzString = pcesEnum->valueHistory.rgcValues[dwIndex].sczBy;
            break;

        case ENUM_DATA_DATABASE_REFERENCES:
            *pwzString = pcesEnum->valueHistory.rgsczDbReferences[dwIndex];
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for string. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    case ENUMERATION_DATABASE_LIST:
        switch (cedData)
        {
        case ENUM_DATA_FRIENDLY_NAME:
            *pwzString = pcesEnum->databaseList.rgsczFriendlyName[dwIndex];
            break;

        case ENUM_DATA_PATH:
            *pwzString = pcesEnum->databaseList.rgsczPath[dwIndex];
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for string. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    default:
        hr = E_INVALIDARG;
          ExitOnFailure(hr, "Unsupported request for string. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadDword(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out_opt DWORD *pdwDword
    )
{
    HRESULT hr = S_OK;
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);

    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadDword() requires an enumeration handle");
    ExitOnNull(cedData, hr, E_INVALIDARG, "CfgEnumReadDword()'s CFG_ENUM_DATA parameter must not be ENUM_DATA_NULL");
    ExitOnNull(pdwDword, hr, E_INVALIDARG, "CfgEnumReadDword() must not be sent NULL for pdwDword parameter");

    if (ENUM_DATA_COUNT == cedData)
    {
        *pdwDword = *(reinterpret_cast<const DWORD *>(&(pcesEnum->dwNumValues)));
        ExitFunction1(hr = S_OK);
    }

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    switch (pcesEnum->enumType)
    {
    case ENUMERATION_VALUES:
        switch (cedData)
        {
        case ENUM_DATA_VALUEDWORD:
            *pdwDword = pcesEnum->values.rgcValues[dwIndex].dword.dwValue;
            break;

        case ENUM_DATA_BLOBSIZE:
            *pdwDword = pcesEnum->values.rgcValues[dwIndex].blob.cbValue;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for dword. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    case ENUMERATION_PRODUCTS:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unsupported request for dword. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;

    case ENUMERATION_VALUE_HISTORY:
        switch (cedData)
        {
        case ENUM_DATA_VALUEDWORD:
            *pdwDword = pcesEnum->valueHistory.rgcValues[dwIndex].dword.dwValue;
            break;

        case ENUM_DATA_BLOBSIZE:
            *pdwDword = pcesEnum->valueHistory.rgcValues[dwIndex].blob.cbValue;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for dword. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    default:
        hr = E_INVALIDARG;
          ExitOnFailure(hr, "Unsupported request for dword. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadQword(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out_opt DWORD64 *pqwQword
    )
{
    HRESULT hr = S_OK;
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);

    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadQword() requires an enumeration handle");
    ExitOnNull(cedData, hr, E_INVALIDARG, "CfgEnumReadQword()'s CFG_ENUM_DATA parameter must not be ENUM_DATA_NULL");
    ExitOnNull(pqwQword, hr, E_INVALIDARG, "CfgEnumReadQword() must not be sent NULL for pqwQword parameter");

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    switch (pcesEnum->enumType)
    {
    case ENUMERATION_VALUES:
        switch (cedData)
        {
        case ENUM_DATA_VALUEQWORD:
            *pqwQword = pcesEnum->values.rgcValues[dwIndex].qword.qwValue;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for qword. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    case ENUMERATION_VALUE_HISTORY:
        switch (cedData)
        {
        case ENUM_DATA_VALUEQWORD:
            *pqwQword = pcesEnum->valueHistory.rgcValues[dwIndex].qword.qwValue;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for qword. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unsupported request for qword. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadBool(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out_opt BOOL *pfBool
    )
{
    HRESULT hr = S_OK;
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);

    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadBool() requires an enumeration handle");
    ExitOnNull(cedData, hr, E_INVALIDARG, "CfgEnumReadBool()'s CFG_ENUM_DATA parameter must not be ENUM_DATA_NULL");
    ExitOnNull(pfBool, hr, E_INVALIDARG, "CfgEnumReadBool() must not be sent NULL for bool output parameter");

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    switch (pcesEnum->enumType)
    {
    case ENUMERATION_VALUES:
        switch (cedData)
        {
        case ENUM_DATA_VALUEBOOL:
            *pfBool = pcesEnum->values.rgcValues[dwIndex].boolean.fValue;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for bool. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    case ENUMERATION_PRODUCTS:
        switch (cedData)
        {
        case ENUM_DATA_REGISTERED:
            *pfBool = pcesEnum->products.rgfRegistered[dwIndex];
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for bool. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    case ENUMERATION_VALUE_HISTORY:
        switch (cedData)
        {
        case ENUM_DATA_VALUEBOOL:
            *pfBool = pcesEnum->valueHistory.rgcValues[dwIndex].boolean.fValue;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for bool. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    case ENUMERATION_DATABASE_LIST:
        switch (cedData)
        {
        case ENUM_DATA_SYNC_BY_DEFAULT:
            *pfBool = pcesEnum->databaseList.rgfSyncByDefault[dwIndex];
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for bool. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unsupported request for bool. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadHash(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __deref_out_bcount(CFG_ENUM_HASH_LEN) BYTE** ppbBuffer
    )
{
    HRESULT hr = S_OK;
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);

    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadHash() requires an enumeration handle");
    ExitOnNull(cedData, hr, E_INVALIDARG, "CfgEnumReadHash()'s CFG_ENUM_DATA parameter must not be ENUM_DATA_NULL");
    ExitOnNull(ppbBuffer, hr, E_INVALIDARG, "CfgEnumReadHash() must not be sent NULL for BYTE * output parameter");

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    switch (pcesEnum->enumType)
    {
    case ENUMERATION_VALUES:
        switch (cedData)
        {
        case ENUM_DATA_BLOBHASH:
            *ppbBuffer = pcesEnum->values.rgcValues[dwIndex].blob.rgbHash;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for hash. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    case ENUMERATION_VALUE_HISTORY:
        switch (cedData)
        {
        case ENUM_DATA_BLOBHASH:
            *ppbBuffer = pcesEnum->valueHistory.rgcValues[dwIndex].blob.rgbHash;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for hash. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unsupported request for hash. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadSystemTime(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __out SYSTEMTIME *pst
    )
{
    HRESULT hr = S_OK;
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);

    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadSystemTime() requires an enumeration handle");
    ExitOnNull(cedData, hr, E_INVALIDARG, "CfgEnumReadSystemTime()'s CFG_ENUM_DATA parameter must not be ENUM_DATA_NULL");
    ExitOnNull(pst, hr, E_INVALIDARG, "CfgEnumReadSystemTime() must not be sent NULL for pst parameter");

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    switch (pcesEnum->enumType)
    {
    case ENUMERATION_VALUES:
        switch (cedData)
        {
        case ENUM_DATA_WHEN:
            *pst = pcesEnum->valueHistory.rgcValues[dwIndex].stWhen;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for systemtime. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    case ENUMERATION_VALUE_HISTORY:
        switch (cedData)
        {
        case ENUM_DATA_WHEN:
            *pst = pcesEnum->valueHistory.rgcValues[dwIndex].stWhen;
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for systemtime. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unsupported request for systemtime. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadBinary(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __in CFG_ENUM_DATA cedData,
    __deref_out_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "CfgEnumReadBinary() requires a database handle");
    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadBinary() requires an enumeration handle");
    ExitOnNull(cedData, hr, E_INVALIDARG, "CfgEnumReadBinary()'s CFG_ENUM_DATA parameter must not be ENUM_DATA_NULL");
    ExitOnNull(ppbBuffer, hr, E_INVALIDARG, "CfgEnumReadBinary() must not be sent NULL for BYTE * output parameter");
    ExitOnNull(piBuffer, hr, E_INVALIDARG, "CfgEnumReadBinary() must not be sent NULL for SIZE_T output parameter");

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when reading from enumeration");
    fLocked = TRUE;

    switch (pcesEnum->enumType)
    {
    case ENUMERATION_VALUES:
        switch (cedData)
        {
        case ENUM_DATA_BLOBCONTENT:
            hr = StreamRead(pcdb, pcesEnum->values.rgcValues[dwIndex].blob.dbstream.dwContentID, NULL, ppbBuffer, piBuffer);
            ExitOnFailure(hr, "Failed to get blob content with ID: %u", pcesEnum->values.rgcValues[dwIndex].blob.dbstream.dwContentID);
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for blob. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }
        break;

    case ENUMERATION_VALUE_HISTORY:
        switch (cedData)
        {
        case ENUM_DATA_BLOBCONTENT:
            hr = StreamRead(pcdb, pcesEnum->valueHistory.rgcValues[dwIndex].blob.dbstream.dwContentID, NULL, ppbBuffer, piBuffer);
            ExitOnFailure(hr, "Failed to get binary content with ID: %u", pcesEnum->valueHistory.rgcValues[dwIndex].blob.dbstream.dwContentID);
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported request for blob. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
            break;
        }

        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unsupported request for blob. Enumeration type: %d, request type: %d", pcesEnum->enumType, cedData);
        break;
    }

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

extern "C" HRESULT CFGAPI CfgEnumReadDisplayNameArray(
    __in_bcount(CFG_ENUMERATION_HANDLE_BYTES) C_CFG_ENUMERATION_HANDLE cehHandle,
    __in DWORD dwIndex,
    __out DISPLAY_NAME **prgDisplayNames,
    __out DWORD *pcDisplayNames
    )
{
    HRESULT hr = S_OK;
    const CFG_ENUMERATION *pcesEnum = static_cast<const CFG_ENUMERATION *>(cehHandle);

    ExitOnNull(pcesEnum, hr, E_INVALIDARG, "CfgEnumReadDisplayNameArray() requires an enumeration handle");
    ExitOnNull(prgDisplayNames, hr, E_INVALIDARG, "CfgEnumReadDisplayNameArray()'s must not be sent NULL for its DISPLAY_NAME ** output parameter");
    ExitOnNull(pcDisplayNames, hr, E_INVALIDARG, "CfgEnumReadDisplayNameArray() must not be sent NULL for DWORD * output parameter");

    // Index out of bounds
    if (dwIndex >= pcesEnum->dwNumValues)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Index %u out of bounds (max value: %u)", dwIndex, pcesEnum->dwNumValues);
    }

    if (ENUMERATION_PRODUCTS != pcesEnum->enumType)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Only product enumeration type supports enumerating display names");
    }

    *prgDisplayNames = pcesEnum->products.rgrgDisplayNames[dwIndex];
    *pcDisplayNames = pcesEnum->products.rgcDisplayNames[dwIndex];

LExit:
    return hr;
}

extern "C" void CfgReleaseEnumeration(
    __in_bcount_opt(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE cehHandle
    )
{
    CFG_ENUMERATION *pcesEnum = static_cast<CFG_ENUMERATION *>(cehHandle);

    EnumFree(pcesEnum);
}

extern "C" HRESULT CfgSync(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __deref_out_ecount_opt(*pcProduct) CONFLICT_PRODUCT **prgcpProductList,
    __out DWORD *pcProduct
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    BOOL fLocked = FALSE;
    BOOL fLockedLocal = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "CfgSync cannot be sent NULL for its first parameter");
    ExitOnNull(pcdb->pcdbLocal, hr, E_INVALIDARG, "CfgSync must be sent a remote database for its first parameter");

    // Lock local DB first, and release it last
    hr = HandleLock(pcdb->pcdbLocal);
    ExitOnFailure(hr, "Failed to lock local handle when syncing");
    fLockedLocal = TRUE;

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock remote handle when syncing");
    fLocked = TRUE;

    hr = UtilSyncDb(pcdb, prgcpProductList, pcProduct);
    ExitOnFailure(hr, "Failed to sync with remote database");

LExit:
    if (fLocked)
    {
        if (SUCCEEDED(hr))
        {
            pcdb->fUpdateLastModified = TRUE;
        }
        HandleUnlock(pcdb);
    }
    if (fLockedLocal)
    {
        HandleUnlock(pcdb->pcdbLocal);
    }

    return hr;
}

extern "C" void CfgReleaseConflictProductArray(
    __in_ecount_opt(cProduct) CONFLICT_PRODUCT *rgcpProduct,
    __in DWORD cProduct
    )
{
    DWORD i;

    if (NULL == rgcpProduct)
    {
        return;
    }

    for (i = 0; i < cProduct; ++i)
    {
        ReleaseStr(rgcpProduct[i].sczProductName);
        ReleaseStr(rgcpProduct[i].sczVersion);
        ReleaseStr(rgcpProduct[i].sczPublicKey);

        ReleaseDisplayNameArray(rgcpProduct[i].rgDisplayNames, rgcpProduct[i].cDisplayNames);

        for (DWORD j = 0; j < rgcpProduct[i].cValues; ++j)
        {
            CfgReleaseEnumeration(rgcpProduct[i].rgcesValueEnumLocal[j]);
            CfgReleaseEnumeration(rgcpProduct[i].rgcesValueEnumRemote[j]);
        }

        ReleaseMem(rgcpProduct[i].rgcesValueEnumLocal);
        ReleaseMem(rgcpProduct[i].rgdwValueCountLocal);

        ReleaseMem(rgcpProduct[i].rgcesValueEnumRemote);
        ReleaseMem(rgcpProduct[i].rgdwValueCountRemote);

        ReleaseMem(rgcpProduct[i].rgrcValueChoices);
    }

    ReleaseMem(rgcpProduct);
}

extern "C" HRESULT CfgResolve(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_ecount(cProduct) CONFLICT_PRODUCT *rgcpProduct,
    __in DWORD cProduct
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LEGACY_SYNC_SESSION syncSession = { };
    DWORD dwProductIndex;
    DWORD dwValueIndex;
    BOOL fRevertProducts = FALSE;
    DWORD dwOriginalAppIDLocal = 0;
    DWORD dwOriginalAppIDRemote = 0;
    BOOL fLegacy = FALSE;
    BOOL fLocked = FALSE;
    BOOL fLockedLocal = FALSE;

    // Check for invalid args
    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(pcdb->pcdbLocal, hr, E_INVALIDARG, "CfgResolve must be sent a remote database for its first parameter");

    fRevertProducts = TRUE;
    dwOriginalAppIDLocal = pcdb->pcdbLocal->dwAppID;
    dwOriginalAppIDRemote = pcdb->dwAppID;

    // Lock local DB first, and release it last
    hr = HandleLock(pcdb->pcdbLocal);
    ExitOnFailure(hr, "Failed to lock local handle when resolving");
    fLockedLocal = TRUE;

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock remote handle when resolving");
    fLocked = TRUE;

    hr = LegacySyncInitializeSession(TRUE, FALSE, &syncSession);
    ExitOnFailure(hr, "Failed to initialize legacy sync session");

    for (dwProductIndex = 0; dwProductIndex < cProduct; ++dwProductIndex)
    {
        // TODO: error out if the value changed since last sync
        hr = ProductSet(pcdb->pcdbLocal, rgcpProduct[dwProductIndex].sczProductName, rgcpProduct[dwProductIndex].sczVersion, rgcpProduct[dwProductIndex].sczPublicKey, TRUE, NULL);
        ExitOnFailure(hr, "Failed to set appropriate product in local DB");

        hr = ProductSet(pcdb, rgcpProduct[dwProductIndex].sczProductName, rgcpProduct[dwProductIndex].sczVersion, rgcpProduct[dwProductIndex].sczPublicKey, TRUE, &fLegacy);
        ExitOnFailure(hr, "Failed to set appropriate product in remote DB");

        if (fLegacy)
        {
            hr = LegacySyncSetProduct(pcdb->pcdbLocal, &syncSession, rgcpProduct[dwProductIndex].sczProductName);
            ExitOnFailure(hr, "Failed to set legacy product: ", rgcpProduct[dwProductIndex].sczProductName);
        }

        for (dwValueIndex = 0; dwValueIndex < rgcpProduct[dwProductIndex].cValues; ++dwValueIndex)
        {
            hr = ConflictResolve(pcdb, &(rgcpProduct[dwProductIndex]), dwValueIndex);
            ExitOnFailure(hr, "Failed to resolve legacy value");
        }

        if (fLegacy)
        {
            hr = LegacySyncFinalizeProduct(pcdb->pcdbLocal, &syncSession);
            ExitOnFailure(hr, "Failed to finalize legacy product - this may have the effect that some resolved conflicts will appear again.");
        }
        else
        {
            hr = BackgroundSyncRemotes(pcdb->pcdbLocal);
            ExitOnFailure(hr, "Failed to sync remotes");
        }
    }

LExit:
    LegacySyncUninitializeSession(pcdb->pcdbLocal, &syncSession);

    // Restore the previous AppIDs that were set before resolving began
    if (fRevertProducts)
    {
        if (NULL != pcdb)
        {
            pcdb->dwAppID = dwOriginalAppIDRemote;
            if (NULL != pcdb->pcdbLocal)
            {
                pcdb->pcdbLocal->dwAppID = dwOriginalAppIDLocal;
            }
        }
    }

    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    if (fLockedLocal)
    {
        HandleUnlock(pcdb->pcdbLocal);
    }

    return hr;
}

extern "C" HRESULT CFGAPI CfgRegisterProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LPWSTR sczLowPublicKey = NULL;
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Must pass in database handle to CfgRegisterProduct()");
    ExitOnNull(wzProductName, hr, E_INVALIDARG, "Product Name must not be NULL");
    ExitOnNull(wzVersion, hr, E_INVALIDARG, "Version must not be NULL");
    ExitOnNull(wzPublicKey, hr, E_INVALIDARG, "Public Key must not be NULL");

    hr = StrAllocString(&sczLowPublicKey, wzPublicKey, 0);
    ExitOnFailure(hr, "Failed to allocate 2nd public key buffer");

    // Convert to lower case characters
    StrStringToLower(sczLowPublicKey);

    hr = ProductValidateName(wzProductName);
    ExitOnFailure(hr, "Failed to validate ProductName: %ls", wzProductName);

    hr = ProductValidateVersion(wzVersion);
    ExitOnFailure(hr, "Failed to validate Version: %ls", wzVersion);

    hr = ProductValidatePublicKey(sczLowPublicKey);
    ExitOnFailure(hr, "Failed to validate Public Key: %ls", sczLowPublicKey);

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when registering product");
    fLocked = TRUE;

    hr = ProductRegister(pcdb, wzProductName, wzVersion, sczLowPublicKey, TRUE);
    ExitOnFailure(hr, "Failed to register product");

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseStr(sczLowPublicKey);

    return hr;
}

extern "C" HRESULT CFGAPI CfgUnregisterProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LPWSTR sczLowPublicKey = NULL;
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Must pass in database handle to CfgUnregisterProduct()");
    ExitOnNull(wzProductName, hr, E_INVALIDARG, "Product Name must not be NULL");
    ExitOnNull(wzVersion, hr, E_INVALIDARG, "Version must not be NULL");
    ExitOnNull(wzPublicKey, hr, E_INVALIDARG, "Public Key must not be NULL");

    hr = StrAllocString(&sczLowPublicKey, wzPublicKey, 0);
    ExitOnFailure(hr, "Failed to allocate 2nd public key buffer");

    // Convert to lower case characters
    StrStringToLower(sczLowPublicKey);

    hr = ProductValidateName(wzProductName);
    ExitOnFailure(hr, "Failed to validate ProductName: %ls", wzProductName);

    hr = ProductValidateVersion(wzVersion);
    ExitOnFailure(hr, "Failed to validate Version: %ls", wzVersion);

    hr = ProductValidatePublicKey(sczLowPublicKey);
    ExitOnFailure(hr, "Failed to validate Public Key: %ls", sczLowPublicKey);

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when unregistering product");
    fLocked = TRUE;

    hr = ProductRegister(pcdb, wzProductName, wzVersion, sczLowPublicKey, FALSE);
    ExitOnFailure(hr, "Failed to unregister product");

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseStr(sczLowPublicKey);

    return hr;
}

extern "C" HRESULT CFGAPI CfgIsProductRegistered(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey,
    __out BOOL *pfRegistered
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LPWSTR sczLowPublicKey = NULL;
    BOOL fLocked = FALSE;

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Must pass in database handle to CfgUnregisterProduct()");
    ExitOnNull(wzProductName, hr, E_INVALIDARG, "Product Name must not be NULL");
    ExitOnNull(wzVersion, hr, E_INVALIDARG, "Version must not be NULL");
    ExitOnNull(wzPublicKey, hr, E_INVALIDARG, "Public Key must not be NULL");

    hr = StrAllocString(&sczLowPublicKey, wzPublicKey, 0);
    ExitOnFailure(hr, "Failed to allocate 2nd public key buffer");

    // Convert to lower case characters
    StrStringToLower(sczLowPublicKey);

    hr = ProductValidateName(wzProductName);
    ExitOnFailure(hr, "Failed to validate ProductName: %ls", wzProductName);

    hr = ProductValidateVersion(wzVersion);
    ExitOnFailure(hr, "Failed to validate Version: %ls", wzVersion);

    hr = ProductValidatePublicKey(sczLowPublicKey);
    ExitOnFailure(hr, "Failed to validate Public Key: %ls", sczLowPublicKey);

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when checking if product is registered");
    fLocked = TRUE;

    hr = ProductIsRegistered(pcdb, wzProductName, wzVersion, sczLowPublicKey, pfRegistered);
    ExitOnFailure(hr, "Failed to check if product is registered");

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseStr(sczLowPublicKey);

    return hr;
}

HRESULT CFGAPI CfgForgetProduct(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzProductName,
    __in_z LPCWSTR wzVersion,
    __in_z LPCWSTR wzPublicKey
    )
{
    HRESULT hr = S_OK;
    BOOL fLocked = FALSE;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);

    ExitOnNull(pcdb, hr, E_INVALIDARG, "Database handle must not be NULL");
    ExitOnNull(wzProductName, hr, E_INVALIDARG, "Product Name must not be NULL");
    ExitOnNull(wzVersion, hr, E_INVALIDARG, "Version must not be NULL");
    ExitOnNull(wzPublicKey, hr, E_INVALIDARG, "Public Key must not be NULL for non-legacy databases");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle when forgetting product");
    fLocked = TRUE;

    hr = LogStringLine(REPORT_STANDARD, "Forgetting product %ls, %ls, %ls by explicit user request", wzProductName, wzVersion, wzPublicKey);
    ExitOnFailure(hr, "Failed to log line");

    // Unset the previously set product in case we delete the currently set product
    pcdb->fProductSet = FALSE;

    hr = ProductForget(pcdb, wzProductName, wzVersion, wzPublicKey);
    ExitOnFailure(hr, "Failed to forget about product");

    hr = BackgroundSyncRemotes(pcdb);
    ExitOnFailure(hr, "Failed to sync remotes");

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

static HRESULT InitializeImpersonationToken(
    __inout CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;
    BOOL fImpersonating = FALSE;

    if (!ImpersonateSelf(SecurityImpersonation))
    {
        ExitWithLastError(hr, "Failed to impersonate self for access check");
    }
    fImpersonating = TRUE;

    if (!::OpenThreadToken(::GetCurrentThread(), TOKEN_QUERY | TOKEN_IMPERSONATE, TRUE, &pcdb->hToken))
    {
        ExitOnLastError(hr, "Failed to open thread token.");
    }

LExit:
    if (fImpersonating)
    {
        RevertToSelf();
        fImpersonating = FALSE;
    }

    return hr;
}

