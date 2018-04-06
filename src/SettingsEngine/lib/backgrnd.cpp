// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const DWORD BACKGROUND_ARRAY_GROWTH = 40;
const DWORD REGKEY_SILENCE_PERIOD = 500;
const DWORD DIRECTORY_SILENCE_PERIOD = 500;
const DWORD ARP_SILENCE_PERIOD = 500;
const DWORD REMOTEDB_SILENCE_PERIOD = 500;
const DWORD REMOTEDB_BUSY_RETRY_PERIOD = 500;

const DWORD NUM_RETRIES = 60;
const DWORD RETRY_INTERVAL_IN_MS = 1000;

LPVOID ARP_KEY_CONTEXT = reinterpret_cast<LPVOID>(1);

#define ReleaseSyncRequest(x) if (x) { FreeSyncRequest(x); }

enum BACKGROUND_THREAD_MESSAGE
{
    BACKGROUND_THREAD_SYNC_TO_REMOTES = WM_APP + 1, // Propagate local changes to all remotes
    BACKGROUND_THREAD_SYNC_FROM_REMOTE, // Propagate changes from a remote to the local db and then to all other remotes
    BACKGROUND_THREAD_UPDATE_PRODUCT, // new product, or new manifest for existing product
    BACKGROUND_THREAD_REMOVE_PRODUCT, // remove manifest for product
    BACKGROUND_THREAD_ADD_REMOTE, // add remote store
    BACKGROUND_THREAD_REMOVE_REMOTE, // remove remote store
    BACKGROUND_THREAD_SYNC_FROM_MONITOR, // the right directory or regkey changed, sync the product or remote db
    BACKGROUND_THREAD_DRIVE_STATUS_UPDATE, // a drive was added or removed, such as a removable drive.
    BACKGROUND_THREAD_DETECT, // ARP changed, re-detect products
    BACKGROUND_THREAD_ERROR_MONITORING, // Can't monitor a directory or regkey for changes, notify user
    BACKGROUND_THREAD_STOP
};

enum MONITOR_TYPE
{
    MONITOR_INVALID = 0,
    MONITOR_DIRECTORY,
    MONITOR_REGKEY,
};

struct SYNC_REQUEST
{
    MONITOR_TYPE type;

    // Directory or subkey
    LPWSTR sczPath;

    BOOL fRecursive;

    // Only applies to regkeys
    HKEY hkRoot;
};

struct MONITOR_ITEM
{
    MONITOR_TYPE type;

    HRESULT hrStatus;

    // Directory or subkey
    LPWSTR sczPath;
    BOOL fRecursive;

    // Only applies to regkeys
    HKEY hkRoot;

    // The legacy product(s) to which this monitor request applies
    LPWSTR *rgsczProductName;
    DWORD cProductName;

    // Whether there is a remote to sync at this monitor location
    BOOL fRemote;

    // If the last attempt to sync this monitor item failed, this is the current retry count
    DWORD cRetries;
};

struct MONITOR_CONTEXT
{
    // Use MonUtil to monitor directories & regkeys
    MON_HANDLE monitorHandle;

    // List of all things we're monitoring so we can easily figure out AppID from a monitored directory or regkey
    // TODO: consider dictionaries here when dictutil supports remove?
    MONITOR_ITEM *rgMonitorItems;
    DWORD cMonitorItems;

    // To post messages to
    DWORD dwBackgroundThreadId;
};

static DWORD WINAPI BackgroundThread(
    __in_bcount(sizeof(CFGDB_STRUCT)) LPVOID pvContext
    );
static HRESULT BeginMonitoring(
    __in CFGDB_STRUCT *pcdb,
    __inout MONITOR_CONTEXT *pContext
    );
static HRESULT BeginMonitoringProduct(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE sceProductRow,
    __inout LEGACY_SYNC_SESSION *pSyncSession,
    __inout MONITOR_CONTEXT *pContext
    );
static void MonGeneralCallback(
    __in HRESULT hrResult,
    __in_opt LPVOID pvContext
    );
static void MonDriveStatusCallback(
    __in WCHAR chDrive,
    __in BOOL fArriving,
    __in_opt LPVOID pvContext
    );
static void MonDirectoryCallback(
    __in HRESULT hrResult,
    __in_z LPCWSTR wzPath,
    __in BOOL fRecursive,
    __in_opt LPVOID pvContext,
    __in_opt LPVOID pvDirectoryContext
    );
static void MonRegKeyCallback(
    __in HRESULT hrResult,
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS kbKeyBitness,
    __in BOOL fRecursive,
    __in_opt LPVOID pvContext,
    __in_opt LPVOID pvRegKeyContext
    );
static HRESULT HandleSyncRequest(
    __in CFGDB_STRUCT *pcdb,
    __in MONITOR_CONTEXT *pContext,
    __in SYNC_REQUEST *pSyncRequest
    );
static HRESULT HandleDriveStatusUpdate(
    __in CFGDB_STRUCT *pcdb,
    __in WCHAR chDrive,
    __in BOOL fArriving
    );
static HRESULT HandleDetectRequest(
    __inout CFGDB_STRUCT *pcdb,
    __inout MONITOR_CONTEXT *pContext
    );
static HRESULT NotifyMonitorFailure(
    __inout CFGDB_STRUCT *pcdb,
    __inout MONITOR_CONTEXT *pContext,
    __in HRESULT hrError,
    __in SYNC_REQUEST *pSyncRequest
    );
static HRESULT FindSyncRequest(
    __in MONITOR_CONTEXT *pContext,
    __in SYNC_REQUEST *pSyncRequest,
    __out DWORD *pdwIndex
    );
static HRESULT AddProductToMonitorList(
    __in CFGDB_STRUCT *pcdb,
    __in const LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzProductName,
    __inout MONITOR_CONTEXT *pContext
    );
static HRESULT RemoveProductFromMonitorList(
    __in_z LPCWSTR wzProductName,
    __inout MONITOR_CONTEXT *pContext
    );
static HRESULT UpdateProductInMonitorList(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzProductName,
    __inout MONITOR_CONTEXT *pContext
    );
static HRESULT AddRemoteToMonitorList(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzPath,
    __inout MONITOR_CONTEXT *pContext
    );
static HRESULT RemoveRemoteFromMonitorList(
    __in_z LPCWSTR wzPath,
    __inout MONITOR_CONTEXT *pContext
    );
static HRESULT PropagateRemotes(
    __in CFGDB_STRUCT *pcdb,
    __in_z_opt LPCWSTR wzFrom,
    __in BOOL fCheckDbTimestamp
    );
// Syncs all remotes which are part of automatic syncing, optionally starting with the one specified by wzFrom, meaning
// we are propagating changes from wzFrom to the local DB and to other remotes.
// If directory is NULL or directory is a database directory we have no record of, just sync all remotes in default order,
// meaning we are propagating changes from local DB to remotes.
// Local DB MUST be locked before calling this
static HRESULT SyncRemotes(
    __in CFGDB_STRUCT *pcdb,
    __in_z_opt LPCWSTR wzFrom,
    __in BOOL fCheckDbTimestamp
    );
// Syncs a single remote, locking it appropriately
// if fCheckDbTimestamp is TRUE, will check the timestamp of the remote and avoid syncing if it hasn't changed
static HRESULT SyncRemote(
    __in CFGDB_STRUCT *pcdb,
    __in BOOL fCheckDbTimestamp,
    __out BOOL *pfChanged
    );
static BOOL FindDirectoryMonitorIndex(
    __in MONITOR_CONTEXT *pContext,
    __in_z LPCWSTR wzPath,
    __in BOOL fRecursive,
    __out DWORD *pdwIndex
    );
static BOOL FindRegKeyMonitorIndex(
    __in MONITOR_CONTEXT *pContext,
    __in_z LPCWSTR wzSubKey,
    __in HKEY hkRoot,
    __out DWORD *pdwIndex
    );
static void FreeSyncRequest(
    __in SYNC_REQUEST *pSyncRequest
    );
static void FreeMonitorItem(
    __in MONITOR_ITEM *pItem
    );
static void FreeMonitorContext(
    __in MONITOR_CONTEXT *pContext
    );
static DWORD GetRemoteIndexByDirectory(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzDirectory
    );

HRESULT BackgroundStartThread(
    __inout CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;
    DWORD cRetries = 1000;
    const DWORD dwRetryPeriodInMs = 10;

    pcdb->hBackgroundThread = ::CreateThread(NULL, 0, BackgroundThread, pcdb, 0, &pcdb->dwBackgroundThreadId);
    if (!pcdb->hBackgroundThread)
    {
        ExitWithLastError(hr, "Failed to create background thread.");
    }

    // Ensure the created thread initializes its message queue. It does this first thing, so if it doesn't within 10 seconds, there must be a huge problem.
    while (!pcdb->fBackgroundThreadMessageQueueInitialized && 0 < cRetries)
    {
        ::Sleep(dwRetryPeriodInMs);
        --cRetries;
    }

    if (0 == cRetries)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Background thread apparently never initialized its message queue.");
    }

LExit:
    return hr;
}

HRESULT BackgroundStopThread(
    __inout CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    if (!::PostThreadMessageW(pcdb->dwBackgroundThreadId, BACKGROUND_THREAD_STOP, static_cast<WPARAM>(S_OK), 0))
    {
        er = ::GetLastError();
        if (ERROR_INVALID_THREAD_ID == er)
        {
            // It already halted, or doesn't exist for some other reason, so let's just ignore it and clean up
            er = ERROR_SUCCESS;
        }
        ExitOnWin32Error(er, hr, "Failed to send message to background thread to halt");
    }

    // If client never told background thread to unblock, unblock it first before we close the event handle
    if (!pcdb->fBackgroundThreadWaitOnStartupTriggered)
    {
        if (!::SetEvent(pcdb->hBackgroundThreadWaitOnStartup))
        {
            ExitWithLastError(hr, "Failed to set background thread wait on startup event while shutting down cfg api");
        }
    }

    if (pcdb->hBackgroundThread)
    {
        ::WaitForSingleObject(pcdb->hBackgroundThread, INFINITE);
        ::CloseHandle(pcdb->hBackgroundThread);
        pcdb->hBackgroundThread = NULL;
        pcdb->fBackgroundThreadMessageQueueInitialized = FALSE;
    }

LExit:
    return hr;
}

HRESULT BackgroundUpdateProduct(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzProductId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczProductId = NULL;

    if (pcdb->hBackgroundThread)
    {
        hr = StrAllocString(&sczProductId, wzProductId, 0);
        ExitOnFailure(hr, "Failed to copy product ID to send message");

        if (!::PostThreadMessageW(pcdb->dwBackgroundThreadId, BACKGROUND_THREAD_UPDATE_PRODUCT, reinterpret_cast<WPARAM>(sczProductId), 0))
        {
            ExitWithLastError(hr, "Failed to send message to background thread to update product");
        }
        sczProductId = NULL;
    }
    else
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Background thread doesn't exist to send updated product message to!");
    }

LExit:
    ReleaseStr(sczProductId);

    return hr;
}

HRESULT BackgroundMarkRemoteChanged(
    __in CFGDB_STRUCT *pcdbRemote
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDirectory = NULL;

    if (pcdbRemote->pcdbLocal->hBackgroundThread)
    {
        hr = StrAllocString(&sczDirectory, pcdbRemote->sczDbDir, 0);
        ExitOnFailure(hr, "Failed to allocate copy of directory string");

        if (!::PostThreadMessageW(pcdbRemote->pcdbLocal->dwBackgroundThreadId, BACKGROUND_THREAD_SYNC_FROM_REMOTE, reinterpret_cast<WPARAM>(sczDirectory), static_cast<LPARAM>(FALSE)))
        {
            ExitWithLastError(hr, "Failed to send message to background thread to sync from remote at directory %ls", sczDirectory);
        }
        sczDirectory = NULL;
    }
    else
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Background thread doesn't exist to send syncremotes message to!");
    }

LExit:
    ReleaseStr(sczDirectory);

    return hr;
}

HRESULT BackgroundSyncRemotes(
    __in CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;

    if (pcdb->hBackgroundThread)
    {
        if (!::PostThreadMessageW(pcdb->dwBackgroundThreadId, BACKGROUND_THREAD_SYNC_TO_REMOTES, 0, 0))
        {
            ExitWithLastError(hr, "Failed to send message to background thread to sync remotes");
        }
    }
    else
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Background thread doesn't exist to send syncremotes message to!");
    }

LExit:
    return hr;
}

HRESULT BackgroundRemoveProduct(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzProductId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczProductId = NULL;

    if (pcdb->hBackgroundThread)
    {
        hr = StrAllocString(&sczProductId, wzProductId, 0);
        ExitOnFailure(hr, "Failed to copy product ID to send message");

        if (!::PostThreadMessageW(pcdb->dwBackgroundThreadId, BACKGROUND_THREAD_REMOVE_PRODUCT, reinterpret_cast<WPARAM>(sczProductId), 0))
        {
            ExitWithLastError(hr, "Failed to send message to background thread to remove product");
        }
        sczProductId = NULL;
    }
    else
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Background thread doesn't exist to send remove product message to!");
    }

LExit:
    ReleaseStr(sczProductId);

    return hr;
}

HRESULT BackgroundAddRemote(
    __in CFGDB_STRUCT *pcdbLocal,
    __in LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

    if (pcdbLocal->hBackgroundThread)
    {
        hr = StrAllocString(&sczPath, wzPath, 0);
        ExitOnFailure(hr, "Failed to copy remote path to send message");

        if (!::PostThreadMessageW(pcdbLocal->dwBackgroundThreadId, BACKGROUND_THREAD_ADD_REMOTE, reinterpret_cast<WPARAM>(sczPath), 0))
        {
            ExitWithLastError(hr, "Failed to send message to background thread to add remote");
        }
        sczPath = NULL;
    }
    else
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Background thread doesn't exist to send add remote message to!");
    }

LExit:
    ReleaseStr(sczPath);

    return hr;
}

HRESULT BackgroundRemoveRemote(
    __in CFGDB_STRUCT *pcdbLocal,
    __in LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

    if (pcdbLocal->hBackgroundThread)
    {
        hr = StrAllocString(&sczPath, wzPath, 0);
        ExitOnFailure(hr, "Failed to copy remote path to send message");

        if (!::PostThreadMessageW(pcdbLocal->dwBackgroundThreadId, BACKGROUND_THREAD_REMOVE_REMOTE, reinterpret_cast<WPARAM>(sczPath), 0))
        {
            ExitWithLastError(hr, "Failed to send message to background thread to remove remote");
        }
        sczPath = NULL;
    }
    else
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Background thread doesn't exist to send remove remote message to!");
    }

LExit:
    ReleaseStr(sczPath);

    return hr;
}

static DWORD WINAPI BackgroundThread(
    __in_bcount(sizeof(CFGDB_STRUCT)) LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    BOOL fRet = FALSE;
    DWORD er = ERROR_SUCCESS;
    SYNC_REQUEST *pSyncRequest = NULL;
    LPWSTR sczString1 = NULL;
    MSG msg = { };
    MSG msgTemp = { };
    MONITOR_CONTEXT monitorContext = { };
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(pvContext);
    BOOL fComInitialized = FALSE;

    monitorContext.dwBackgroundThreadId = pcdb->dwBackgroundThreadId;

    // Ensure the thread has a message queue
    ::PeekMessage(&msg, NULL, WM_USER, WM_USER, PM_NOREMOVE);
    pcdb->fBackgroundThreadMessageQueueInitialized = TRUE;

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    er = ::WaitForSingleObject(pcdb->hBackgroundThreadWaitOnStartup, INFINITE);
    ExitOnWin32Error(er, hr, "Failed to wait for background thread startup");

    // Before we start pumping messages, we need to do some work (locking critical section whenever reading from or writing to the database)
    // 1. Setup monitoring for ARP regkeys
    // 2. Setup appropriate lists and run monitoring for each product we know about
    // 3. Pull all settings from each product
    // 4. Notify user a product was synced
    // 5. Notify user when background thread auto sync has finished starting up
    // 6. Run message loop
    // 7. Notify user when background thread auto sync has stopped and/or of errors

    hr = BeginMonitoring(pcdb, &monitorContext);
    ExitOnFailure(hr, "Failed to begin monitoring");
    pcdb->vpfBackgroundStatus(S_OK, BACKGROUND_STATUS_AUTOSYNC_RUNNING, NULL, NULL, NULL, pcdb->pvCallbackContext);

    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnRootFailure(hr, "Unexpected return value from message pump.");
        }
        else
        {
            switch (msg.message)
            {
            case BACKGROUND_THREAD_SYNC_TO_REMOTES:
                // Do a simple de-dupe check - TODO: improve by reading in whole array of pending messages and truly deduping all of them
                if (::PeekMessage(&msgTemp, NULL, BACKGROUND_THREAD_SYNC_TO_REMOTES, BACKGROUND_THREAD_SYNC_TO_REMOTES, PM_NOREMOVE))
                {
                    LogStringLine(REPORT_STANDARD, "Skipping sync to remotes message because of pending %u message.", msgTemp.message);
                    // We were told to propagate to all remotes later on in our message queue, so delay propagation until later to pickup more changes
                    continue;
                }

                LogStringLine(REPORT_STANDARD, "Processing message to sync to all remotes.");
                hr = PropagateRemotes(pcdb, NULL, FALSE);
                ExitOnFailure(hr, "Failed to propagate to remotes");
                break;

            case BACKGROUND_THREAD_SYNC_FROM_REMOTE:
                ReleaseStr(sczString1);
                sczString1 = reinterpret_cast<LPWSTR>(msg.wParam);

                if (::PeekMessage(&msgTemp, NULL, BACKGROUND_THREAD_SYNC_FROM_REMOTE, BACKGROUND_THREAD_SYNC_FROM_REMOTE, PM_NOREMOVE))
                {
                    if (0 != msgTemp.wParam && NULL != sczString1 && ::CompareStringW(LOCALE_INVARIANT, 0, reinterpret_cast<LPCWSTR>(msgTemp.wParam), -1, sczString1, -1) == CSTR_EQUAL && msg.lParam == msgTemp.lParam)
                    {
                        LogStringLine(REPORT_STANDARD, "Skipping sync from remote %ls message with fCheckDbTimestamp=%ls because of pending identical message.", sczString1, static_cast<BOOL>(msg.lParam) ? L"TRUE" : L"FALSE");
                        continue;
                    }
                }

                // Intentionally don't log here, because MonUtil will occasionally send false positive "ping" notifications

                hr = PropagateRemotes(pcdb, sczString1, static_cast<BOOL>(msg.lParam));
                ExitOnFailure(hr, "Failed to propagate remotes with from value of %ls, fCheckDbTimestamp=%ls", sczString1, static_cast<BOOL>(msg.lParam) ? L"TRUE" : L"FALSE");
                break;

            case BACKGROUND_THREAD_UPDATE_PRODUCT:
                ReleaseStr(sczString1);
                sczString1 = reinterpret_cast<LPWSTR>(msg.wParam);
                hr = UpdateProductInMonitorList(pcdb, sczString1, &monitorContext);
                ExitOnFailure(hr, "Failed to refresh product in monitor list");
                break;

            case BACKGROUND_THREAD_REMOVE_PRODUCT:
                ReleaseStr(sczString1);
                sczString1 = reinterpret_cast<LPWSTR>(msg.wParam);
                RemoveProductFromMonitorList(sczString1, &monitorContext);
                ExitOnFailure(hr, "Failed to remove product from monitor list");
                break;

            case BACKGROUND_THREAD_ADD_REMOTE:
                ReleaseStr(sczString1);
                sczString1 = reinterpret_cast<LPWSTR>(msg.wParam);
                AddRemoteToMonitorList(pcdb, sczString1, &monitorContext);
                ExitOnFailure(hr, "Failed to add remote to monitor list");
                break;

            case BACKGROUND_THREAD_REMOVE_REMOTE:
                ReleaseStr(sczString1);
                sczString1 = reinterpret_cast<LPWSTR>(msg.wParam);
                RemoveRemoteFromMonitorList(sczString1, &monitorContext);
                ExitOnFailure(hr, "Failed to remove remote from monitor list");
                break;

            case BACKGROUND_THREAD_SYNC_FROM_MONITOR:
                // Don't free the sync request - HandleSyncRequest will free it when appropriate
                hr = HandleSyncRequest(pcdb, &monitorContext, reinterpret_cast<SYNC_REQUEST *>(msg.wParam));
                if (E_NOTFOUND == hr)
                {
                    // Sync path wasn't found, may have been removed in a race condition like situation, so log it and move on
                    LogErrorString(hr, "Detected changes, but no product was found for them - ignoring.");
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to handle sync request");
                break;

            case BACKGROUND_THREAD_DRIVE_STATUS_UPDATE:
                hr = HandleDriveStatusUpdate(pcdb, static_cast<WCHAR>(msg.wParam), static_cast<BOOL>(msg.lParam));
                ExitOnFailure(hr, "Failed to handle drive status update");
                break;

            case BACKGROUND_THREAD_DETECT:
                hr = HandleDetectRequest(pcdb, &monitorContext);
                ExitOnFailure(hr, "Failed to redetect products");
                break;

            case BACKGROUND_THREAD_ERROR_MONITORING:
                ReleaseSyncRequest(pSyncRequest);
                ReleaseMem(pSyncRequest);
                pSyncRequest = reinterpret_cast<SYNC_REQUEST *>(msg.lParam);
                hr = NotifyMonitorFailure(pcdb, &monitorContext, static_cast<HRESULT>(msg.wParam), pSyncRequest);
                ExitOnFailure(hr, "Failed to notify of a monitor failure");
                break;

            case BACKGROUND_THREAD_STOP:
                LogStringLine(REPORT_STANDARD, "Stopping settings engine background thread.");
                ExitFunction1(hr = static_cast<HRESULT>(msg.wParam));

            default:
                Assert(false);
                ExitFunction1(hr = E_UNEXPECTED);
            }
        }
    }

LExit:
    LogStringLine(REPORT_STANDARD, "Background thread shutting down with status 0x%X.", hr);
    // If thread is exiting, notify client
    if (FAILED(hr))
    {
        pcdb->vpfBackgroundStatus(hr, BACKGROUND_STATUS_GENERAL_ERROR, NULL, NULL, NULL, pcdb->pvCallbackContext);
    }
    ReleaseStr(sczString1);
    ReleaseSyncRequest(pSyncRequest);
    ReleaseMem(pSyncRequest);
    FreeMonitorContext(&monitorContext);

    // uninitialize COM
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return hr;
}

static HRESULT BeginMonitoring(
    __in CFGDB_STRUCT *pcdb,
    __inout MONITOR_CONTEXT *pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczProductId = NULL;
    BOOL fIsLegacy = FALSE;
    SCE_ROW_HANDLE sceRow = NULL;
    LEGACY_SYNC_SESSION syncSession = { };
    DWORD dwOriginalAppIDLocal = DWORD_MAX;
    BOOL fLocked = FALSE;

    ReleaseNullMon(pContext->monitorHandle);
    hr = MonCreate(&pContext->monitorHandle, MonGeneralCallback, MonDriveStatusCallback, MonDirectoryCallback, MonRegKeyCallback, static_cast<LPVOID>(pContext));
    ExitOnFailure(hr, "Failed to create MonUtil object");

    hr = MonAddRegKey(pContext->monitorHandle, HKEY_LOCAL_MACHINE, wzArpPath, REG_KEY_32BIT, TRUE, ARP_SILENCE_PERIOD, ARP_KEY_CONTEXT);
    ExitOnFailure(hr, "Failed to add ARP HKLM regkey for monitoring");

    hr = MonAddRegKey(pContext->monitorHandle, HKEY_CURRENT_USER, wzArpPath, REG_KEY_DEFAULT, TRUE, ARP_SILENCE_PERIOD, ARP_KEY_CONTEXT);
    ExitOnFailure(hr, "Failed to add ARP HKCU regkey for monitoring");

    hr = MonAddRegKey(pContext->monitorHandle, HKEY_LOCAL_MACHINE, wzApplicationsPath, REG_KEY_32BIT, TRUE, ARP_SILENCE_PERIOD, ARP_KEY_CONTEXT);
    ExitOnFailure(hr, "Failed to add applications HKLM regkey for monitoring");

    hr = MonAddRegKey(pContext->monitorHandle, HKEY_CURRENT_USER, wzApplicationsPath, REG_KEY_DEFAULT, TRUE, ARP_SILENCE_PERIOD, ARP_KEY_CONTEXT);
    ExitOnFailure(hr, "Failed to add applications HKCU regkey for monitoring");

    if (UtilIs64BitSystem())
    {
        hr = MonAddRegKey(pContext->monitorHandle, HKEY_LOCAL_MACHINE, wzArpPath, REG_KEY_64BIT, TRUE, ARP_SILENCE_PERIOD, ARP_KEY_CONTEXT);
        ExitOnFailure(hr, "Failed to add ARP 64-bit HKLM regkey for monitoring");

        hr = MonAddRegKey(pContext->monitorHandle, HKEY_LOCAL_MACHINE, wzApplicationsPath, REG_KEY_64BIT, TRUE, ARP_SILENCE_PERIOD, ARP_KEY_CONTEXT);
        ExitOnFailure(hr, "Failed to add applications 64-bit HKLM regkey for monitoring");
    }

    hr = LegacySyncInitializeSession(TRUE, TRUE, &syncSession);
    ExitOnFailure(hr, "Failed to initialize legacy sync session");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle while beginning monitoring");
    fLocked = TRUE;
    dwOriginalAppIDLocal = pcdb->dwAppID;

    hr = SceGetFirstRow(pcdb->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get row from table: %u", PRODUCT_INDEX_TABLE);

        hr = SceGetColumnBool(sceRow, PRODUCT_IS_LEGACY, &fIsLegacy);
        ExitOnFailure(hr, "Failed to check if product is legacy");

        if (fIsLegacy)
        {
            hr = SceGetColumnString(sceRow, PRODUCT_NAME, &sczProductId);
            ExitOnFailure(hr, "Failed to get product name");

            hr = BeginMonitoringProduct(pcdb, sceRow, &syncSession, pContext);
            // Don't just die if a product fails - this could be due to a badly written manifest, or a bad ARP key
            // We log and move on to give user a chance to fix it
            // TODO: we should have a way to notify UI of a warning (non-catastrophic error)
            if (FAILED(hr))
            {
                LogStringLine(REPORT_STANDARD, "Failed to begin monitoring product %ls with error 0x%X", sczProductId, hr);
                hr = S_OK;
            }
        }

        ReleaseNullSceRow(sceRow);
        hr = SceGetNextRow(pcdb->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
    }
    hr = S_OK;

    for (DWORD i = 0; i < pcdb->cOpenDatabases; ++i)
    {
        if (pcdb->rgpcdbOpenDatabases[i]->fSyncByDefault)
        {
            hr = AddRemoteToMonitorList(pcdb, pcdb->rgpcdbOpenDatabases[i]->sczOriginalDbPath, pContext);
            ExitOnFailure(hr, "Failed to add remote to monitor list");
        }
    }

LExit:
    ReleaseSceRow(sceRow);
    LegacySyncUninitializeSession(pcdb, &syncSession);
    if (DWORD_MAX != dwOriginalAppIDLocal)
    {
        pcdb->dwAppID = dwOriginalAppIDLocal;
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseStr(sczProductId);

    return hr;
}

static HRESULT BeginMonitoringProduct(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE sceProductRow,
    __inout LEGACY_SYNC_SESSION *pSyncSession,
    __inout MONITOR_CONTEXT *pContext
    )
{
    HRESULT hr = S_OK;
    BOOL fSyncingProduct = FALSE;
    LPWSTR sczName = NULL;
    DWORD dwAppID = DWORD_MAX;

    hr = SceGetColumnString(sceProductRow, PRODUCT_NAME, &sczName);
    ExitOnFailure(hr, "Failed to get legacy product name");

    hr = SceGetColumnDword(sceProductRow, PRODUCT_ID, &dwAppID);
    ExitOnFailure(hr, "Failed to get legacy product ID");

    hr = LegacySyncSetProduct(pcdb, pSyncSession, sczName);
    ExitOnFailure(hr, "Failed to set product in legacy sync session");

    hr = AddProductToMonitorList(pcdb, &pSyncSession->syncProductSession.product, sczName, pContext);
    ExitOnFailure(hr, "Failed to add product %ls to monitor list", sczName);

    pcdb->vpfBackgroundStatus(S_OK, BACKGROUND_STATUS_SYNCING_PRODUCT, sczName, wzLegacyVersion, wzLegacyPublicKey, pcdb->pvCallbackContext);
    fSyncingProduct = TRUE;

    // Pull in the data
    hr = LegacyProductMachineToDb(pcdb, &pSyncSession->syncProductSession);
    ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");

    if (pSyncSession->syncProductSession.fRegistered && !pSyncSession->syncProductSession.fNewlyRegistered)
    {
        hr = LegacySyncPullDeletedValues(pcdb, &pSyncSession->syncProductSession);
        ExitOnFailure(hr, "Failed to check for deleted registry values for product %u", dwAppID);
    }

    hr = LegacySyncFinalizeProduct(pcdb, pSyncSession);
    ExitOnFailure(hr, "Failed to finalize product in legacy sync session");

LExit:
    if (fSyncingProduct)
    {
        pcdb->vpfBackgroundStatus(hr, BACKGROUND_STATUS_SYNC_PRODUCT_FINISHED, sczName, wzLegacyVersion, wzLegacyPublicKey, pcdb->pvCallbackContext);
    }
    ReleaseStr(sczName);

    return hr;
}

static void MonGeneralCallback(
    __in HRESULT hrResult,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    MONITOR_CONTEXT *pContext = static_cast<MONITOR_CONTEXT *>(pvContext);

    if (SUCCEEDED(hrResult))
    {
        hrResult = E_UNEXPECTED;
        LogStringLine(REPORT_STANDARD, "Received successful general result from MonUtil, which doesn't make any sense. Overriding with E_UNEXPECTED");
    }

    LogErrorString(hrResult, "Received general result from MonUtil");

    if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_STOP, static_cast<WPARAM>(hrResult), 0))
    {
        ExitWithLastError(hr, "Failed to send message to worker thread to stop processing due to MonUtil general error");
    }

LExit:
    return;
}

static void MonDriveStatusCallback(
    __in WCHAR chDrive,
    __in BOOL fArriving,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    MONITOR_CONTEXT *pContext = static_cast<MONITOR_CONTEXT *>(pvContext);

    LogStringLine(REPORT_STANDARD, "Received drive status change notification, drive %wc, arriving=%ls", chDrive, fArriving ? L"TRUE" : L"FALSE");

    if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_DRIVE_STATUS_UPDATE, static_cast<WPARAM>(chDrive), static_cast<LPARAM>(fArriving)))
    {
        ExitWithLastError(hr, "Failed to send message to worker thread to notify of drive status update");
    }

LExit:
    return;
}

static void MonDirectoryCallback(
    __in HRESULT hrResult,
    __in_z LPCWSTR wzPath,
    __in BOOL fRecursive,
    __in_opt LPVOID pvContext,
    __in_opt LPVOID /*pvDirectoryContext*/
    )
{
    HRESULT hr = S_OK;
    SYNC_REQUEST *pSyncRequest = NULL;
    MONITOR_CONTEXT *pContext = static_cast<MONITOR_CONTEXT *>(pvContext);

    if (FAILED(hrResult))
    {
        LogErrorString(hrResult, "Received failed directory result from MonUtil for directory %ls", wzPath);
    }

    pSyncRequest = reinterpret_cast<SYNC_REQUEST *>(MemAlloc(sizeof(SYNC_REQUEST), TRUE));
    ExitOnNull(pSyncRequest, hr, E_OUTOFMEMORY, "Failed to allocate space for sync request");

    hr = StrAllocString(&pSyncRequest->sczPath, wzPath, 0);
    ExitOnFailure(hr, "Failed to copy path into sync request");

    pSyncRequest->fRecursive = fRecursive;
    pSyncRequest->type = MONITOR_DIRECTORY;

    if (FAILED(hrResult))
    {
        if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_ERROR_MONITORING, static_cast<WPARAM>(hrResult), reinterpret_cast<LPARAM>(pSyncRequest)))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to notify MonUtil cannot watch directory %ls", wzPath);
        }
    }
    else
    {
        if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_SYNC_FROM_MONITOR, reinterpret_cast<WPARAM>(pSyncRequest), 0))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to sync product");
        }
    }
    pSyncRequest = NULL;

LExit:
    ReleaseSyncRequest(pSyncRequest);
    ReleaseMem(pSyncRequest);
    if (FAILED(hr))
    {
        if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_STOP, static_cast<WPARAM>(hrResult), 0))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to stop processing");
        }
    }

    return;
}

static void MonRegKeyCallback(
    __in HRESULT hrResult,
    __in HKEY hkRoot,
    __in_z LPCWSTR wzSubKey,
    __in REG_KEY_BITNESS /*kbKeyBitness*/,
    __in BOOL fRecursive,
    __in_opt LPVOID pvContext,
    __in_opt LPVOID pvRegKeyContext
    )
{
    HRESULT hr = S_OK;
    SYNC_REQUEST *pSyncRequest = NULL;
    MONITOR_CONTEXT *pContext = static_cast<MONITOR_CONTEXT *>(pvContext);

    if (FAILED(hrResult))
    {
        LogErrorString(hrResult, "Received failed regkey result from MonUtil for regkey %ls", wzSubKey);
    }

    if (SUCCEEDED(hrResult) && pvRegKeyContext == ARP_KEY_CONTEXT)
    {
        if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_DETECT, static_cast<WPARAM>(hrResult), 0))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to notify MonUtil to re-detect", wzSubKey);
        }
        ExitFunction1(hr = S_OK);
    }

    pSyncRequest = reinterpret_cast<SYNC_REQUEST *>(MemAlloc(sizeof(SYNC_REQUEST), TRUE));
    ExitOnNull(pSyncRequest, hr, E_OUTOFMEMORY, "Failed to allocate space for sync request");

    hr = StrAllocString(&pSyncRequest->sczPath, wzSubKey, 0);
    ExitOnFailure(hr, "Failed to copy path into sync request");

    pSyncRequest->type = MONITOR_REGKEY;
    pSyncRequest->fRecursive = fRecursive;
    pSyncRequest->hkRoot = hkRoot;

    if (FAILED(hrResult))
    {
        if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_ERROR_MONITORING, static_cast<WPARAM>(hrResult), reinterpret_cast<LPARAM>(pSyncRequest)))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to notify MonUtil cannot watch subkey %ls", wzSubKey);
        }
    }
    else
    {
        if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_SYNC_FROM_MONITOR, reinterpret_cast<WPARAM>(pSyncRequest), 0))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to sync product");
        }
    }
    pSyncRequest = NULL;

LExit:
    ReleaseSyncRequest(pSyncRequest);
    ReleaseMem(pSyncRequest);
    if (FAILED(hr))
    {
        if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_STOP, static_cast<WPARAM>(hrResult), 0))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to stop processing");
        }
    }

    return;
}

static HRESULT HandleSyncRequest(
    __in CFGDB_STRUCT *pcdb,
    __in MONITOR_CONTEXT *pContext,
    __in SYNC_REQUEST *pSyncRequest
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    BOOL fSyncingProduct = FALSE;
    LPCWSTR wzSyncingProductName = NULL;
    LPWSTR sczTemp = NULL;
    DWORD dwMonitorIndex = DWORD_MAX;
    LEGACY_SYNC_SESSION syncSession = { };
    DWORD dwOriginalAppIDLocal = DWORD_MAX;
    BOOL fLocked = FALSE;
    BOOL fReconnected = FALSE;
    BOOL fCheckDbTimestamp = FALSE;
    MONITOR_ITEM *pMonitorItem = NULL;

    hr = FindSyncRequest(pContext, pSyncRequest, &dwMonitorIndex);
    if (E_NOTFOUND == hr)
    {
        // There can be race conditions where we were notified of changes for a product, then we dropped that product from our list, but the message was still pending
        // So exit gracefully if we have no record of a sync request
        LogErrorString(hr, "Failed to find any outstanding sync request for path %ls, continuing", pSyncRequest->sczPath);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to find sync request");

    pMonitorItem = pContext->rgMonitorItems + dwMonitorIndex;

    if (FAILED(pMonitorItem->hrStatus))
    {
        fReconnected = TRUE;
    }
    pMonitorItem->hrStatus = S_OK;

    hr = LegacySyncInitializeSession(FALSE, FALSE, &syncSession);
    ExitOnFailure(hr, "Failed to initialize legacy sync session");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle while handling sync request");
    fLocked = TRUE;
    dwOriginalAppIDLocal = pcdb->dwAppID;

    SceResetDatabaseChanged(pcdb->psceDb);

    for (i = 0; i < pMonitorItem->cProductName; ++i)
    {
        pcdb->vpfBackgroundStatus(S_OK, BACKGROUND_STATUS_SYNCING_PRODUCT, pMonitorItem->rgsczProductName[i], wzLegacyVersion, wzLegacyPublicKey, pcdb->pvCallbackContext);
        fSyncingProduct = TRUE;
        wzSyncingProductName = pMonitorItem->rgsczProductName[i];

        LogStringLine(REPORT_STANDARD, "Syncing legacy product %ls due to detected changes under %ls %ls", pMonitorItem->rgsczProductName[i], MONITOR_DIRECTORY == pSyncRequest->type ? L"directory" : L"regkey", pSyncRequest->sczPath);

        // Sync the product
        hr = LegacySyncSetProduct(pcdb, &syncSession, pMonitorItem->rgsczProductName[i]);
        ExitOnFailure(hr, "Failed to set product in legacy sync session");

        hr = LegacyProductMachineToDb(pcdb, &syncSession.syncProductSession);
        ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");

        hr = LegacySyncFinalizeProduct(pcdb, &syncSession);
        ExitOnFailure(hr, "Failed to finalize product in legacy sync session");

        pcdb->vpfBackgroundStatus(S_OK, BACKGROUND_STATUS_SYNC_PRODUCT_FINISHED, pMonitorItem->rgsczProductName[i], wzLegacyVersion, wzLegacyPublicKey, pcdb->pvCallbackContext);
        fSyncingProduct = FALSE;
    }

    if (pMonitorItem->fRemote)
    {
        hr = StrAllocString(&sczTemp, pSyncRequest->sczPath, 0);
        ExitOnFailure(hr, "Failed to copy sync request string %ls", pSyncRequest->sczPath);

        // If we previously failed to connect to this remote and now we can again, don't check timestamp, because we need to both push and pull changes
        fCheckDbTimestamp = !fReconnected;

        if (!::PostThreadMessageW(pcdb->dwBackgroundThreadId, BACKGROUND_THREAD_SYNC_FROM_REMOTE, reinterpret_cast<WPARAM>(sczTemp), static_cast<LPARAM>(fCheckDbTimestamp)))
        {
            ExitWithLastError(hr, "Failed to send message to background thread to sync from remote %ls", sczTemp);
        }
        sczTemp = NULL;
    }
    else if (SceDatabaseChanged(pcdb->psceDb))
    {
        // Only propagate to remotes if something actually changed
        LogStringLine(REPORT_STANDARD, "Sending request to sync all remotes", pSyncRequest->sczPath);

        hr = BackgroundSyncRemotes(pcdb);
        ExitOnFailure(hr, "Failed to send message to background thread to sync remotes");
    }

LExit:
    if (NULL != pMonitorItem)
    {
        if (FAILED(hr) && NUM_RETRIES > pMonitorItem->cRetries)
        {
            ++pMonitorItem->cRetries;
            LogErrorString(hr, "Error while syncing path %ls, retrying %u of %u times (with %u ms interval between retries)", pSyncRequest->sczPath, pMonitorItem->cRetries, NUM_RETRIES, RETRY_INTERVAL_IN_MS);
            hr = S_OK;
            ::Sleep(RETRY_INTERVAL_IN_MS);
            if (!::PostThreadMessageW(pContext->dwBackgroundThreadId, BACKGROUND_THREAD_SYNC_FROM_MONITOR, reinterpret_cast<WPARAM>(pSyncRequest), 0))
            {
                LogErrorString(hr, "Failed to send message to worker thread to sync product");
            }
            else
            {
                pSyncRequest = NULL;
            }
        }
        else 
        {
            pMonitorItem->cRetries = 0;
        }
    }
    if (fSyncingProduct)
    {
        pcdb->vpfBackgroundStatus(hr, BACKGROUND_STATUS_SYNC_PRODUCT_FINISHED, wzSyncingProductName, wzLegacyVersion, wzLegacyPublicKey, pcdb->pvCallbackContext);
    }
    LegacySyncUninitializeSession(pcdb, &syncSession);
    if (DWORD_MAX != dwOriginalAppIDLocal)
    {
        pcdb->dwAppID = dwOriginalAppIDLocal;
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseStr(sczTemp);
    ReleaseSyncRequest(pSyncRequest);
    ReleaseMem(pSyncRequest);

    return hr;
}

static HRESULT HandleDriveStatusUpdate(
    __in CFGDB_STRUCT *pcdb,
    __in WCHAR chDrive,
    __in BOOL fArriving
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDirectory = NULL;

    for (DWORD i = 0; i < pcdb->cOpenDatabases; ++i)
    {
        if (pcdb->rgpcdbOpenDatabases[i]->fSyncByDefault && NULL != pcdb->rgpcdbOpenDatabases[i]->sczDbPath && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &chDrive, 1, &pcdb->rgpcdbOpenDatabases[i]->sczDbPath[0], 1))
        {
            if (fArriving)
            {
                hr = StrAllocString(&sczDirectory, pcdb->rgpcdbOpenDatabases[i]->sczDbDir, 0);
                ExitOnFailure(hr, "Failed to allocate copy of database dir");

                // TODO: What if someone plugged in a different USB drive, that happened to have a different database at the same path and same drive?
                // we should check that the DB guid matches expected value!
                if (!::PostThreadMessageW(pcdb->dwBackgroundThreadId, BACKGROUND_THREAD_SYNC_FROM_REMOTE, reinterpret_cast<WPARAM>(sczDirectory), static_cast<LPARAM>(FALSE)))
                {
                    ExitWithLastError(hr, "Failed to send message to background thread to sync from remote %ls", sczDirectory);
                }
                sczDirectory = NULL;
            }
            else
            {
                pcdb->vpfBackgroundStatus(HRESULT_FROM_WIN32(ERROR_INVALID_DRIVE), BACKGROUND_STATUS_REMOTE_ERROR, pcdb->rgpcdbOpenDatabases[i]->sczOriginalDbPath, NULL, NULL, pcdb->pvCallbackContext);
            }
        }
    }

LExit:
    ReleaseStr(sczDirectory);

    return hr;
}

static HRESULT HandleDetectRequest(
    __inout CFGDB_STRUCT *pcdb,
    __inout MONITOR_CONTEXT *pContext
    )
{
    HRESULT hr = S_OK;
    BOOL fRedetecting = FALSE;
    LEGACY_SYNC_SESSION syncSession = { };
    BOOL fLocked = FALSE;

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle while handling detect request");
    fLocked = TRUE;

    LogStringLine(REPORT_STANDARD, "Redetecting all products due to detected changes");

    pcdb->vpfBackgroundStatus(S_OK, BACKGROUND_STATUS_REDETECTING_PRODUCTS, NULL, NULL, NULL, pcdb->pvCallbackContext);
    fRedetecting = TRUE;

    ReleaseNullMon(pContext->monitorHandle);
    for (DWORD i = 0; i < pContext->cMonitorItems; ++i)
    {
        FreeMonitorItem(pContext->rgMonitorItems + i);
    }
    pContext->cMonitorItems = 0;
    ReleaseNullMem(pContext->rgMonitorItems);

    // An ARP key had changes, so completely restart monitoring (to pick up any changes to our watch directories)
    // TODO: for better perf, we could do a lot less here
    hr = BeginMonitoring(pcdb, pContext);
    ExitOnFailure(hr, "Failed to restart monitoring");

LExit:
    if (fRedetecting)
    {
        pcdb->vpfBackgroundStatus(hr, BACKGROUND_STATUS_REDETECT_PRODUCTS_FINISHED, NULL, NULL, NULL, pcdb->pvCallbackContext);
    }
    LegacySyncUninitializeSession(pcdb, &syncSession);
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

static HRESULT NotifyMonitorFailure(
    __inout CFGDB_STRUCT *pcdb,
    __inout MONITOR_CONTEXT *pContext,
    __in HRESULT hrError,
    __in SYNC_REQUEST *pSyncRequest
    )
{
    HRESULT hr = S_OK;
    DWORD dwMonitorIndex = DWORD_MAX;
    DWORD dwRemoteIndex = DWORD_MAX;

    hr = FindSyncRequest(pContext, pSyncRequest, &dwMonitorIndex);
    ExitOnFailure(hr, "Failed to find sync request");

    if (pContext->rgMonitorItems[dwMonitorIndex].fRemote)
    {
        Assert(MONITOR_DIRECTORY == pSyncRequest->type);

        dwRemoteIndex = GetRemoteIndexByDirectory(pcdb, pSyncRequest->sczPath);
        if (DWORD_MAX != dwRemoteIndex)
        {
            pcdb->vpfBackgroundStatus(hrError, BACKGROUND_STATUS_REMOTE_ERROR, pcdb->rgpcdbOpenDatabases[dwRemoteIndex]->sczOriginalDbPath, NULL, NULL, pcdb->pvCallbackContext);
        }
    }
    else
    {
        for (DWORD i = 0; i < pContext->rgMonitorItems[dwMonitorIndex].cProductName; ++i)
        {
            pcdb->vpfBackgroundStatus(hrError, BACKGROUND_STATUS_PRODUCT_ERROR, pContext->rgMonitorItems[dwMonitorIndex].rgsczProductName[i], wzLegacyVersion, wzLegacyPublicKey, pcdb->pvCallbackContext);
        }
    }

    pContext->rgMonitorItems[dwMonitorIndex].hrStatus = hrError;

LExit:
    return hr;
}

static HRESULT FindSyncRequest(
    __in MONITOR_CONTEXT *pContext,
    __in SYNC_REQUEST *pSyncRequest,
    __out DWORD *pdwIndex
    )
{
    HRESULT hr = S_OK;
    *pdwIndex = DWORD_MAX;

    for (DWORD i = 0; i < pContext->cMonitorItems; ++i)
    {
        if (pSyncRequest->type == pContext->rgMonitorItems[i].type && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->rgMonitorItems[i].sczPath, -1, pSyncRequest->sczPath, -1) && pSyncRequest->fRecursive == pContext->rgMonitorItems[i].fRecursive)
        {
            switch (pSyncRequest->type)
            {
            case MONITOR_DIRECTORY:
                *pdwIndex = i;
                break;
            case MONITOR_REGKEY:
                if (pSyncRequest->hkRoot == pContext->rgMonitorItems[i].hkRoot)
                {
                    *pdwIndex = i;
                }
                break;
            default:
                ExitFunction1(hr = E_INVALIDARG);
            }
        }
    }

    if (DWORD_MAX == *pdwIndex)
    {
        hr = E_NOTFOUND;
        ExitOnFailure(hr, "Monitor of type %u for path %ls wasn't found", pSyncRequest->type, pSyncRequest->sczPath);
    }

LExit:
    return hr;
}

static HRESULT AddProductToMonitorList(
    __in CFGDB_STRUCT *pcdb,
    __in const LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzProductName,
    __inout MONITOR_CONTEXT *pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDirectory = NULL;
    DWORD dwIndex = 0;
    LPWSTR sczProductName = NULL;
    MONITOR_ITEM *pItem = NULL;

    // Before we actually do the pull, add the regkeys & directories to our list for monitoring
    for (DWORD i = 0; i < pProduct->cRegKeys; ++i)
    {
        hr = StrAllocString(&sczProductName, wzProductName, 0);
        ExitOnFailure(hr, "Failed to allocate copy of product name string");

        // If this regkey is already watched by some other product, add our product ID to the appID list for that monitor
        if (FindRegKeyMonitorIndex(pContext, pProduct->rgRegKeys[i].sczKey, ManifestConvertToRootKey(pProduct->rgRegKeys[i].dwRoot), &dwIndex))
        {
            hr = MemEnsureArraySize(reinterpret_cast<void **>(&pContext->rgMonitorItems[dwIndex].rgsczProductName), pContext->rgMonitorItems[dwIndex].cProductName + 1, sizeof(LPWSTR), 0);
            ExitOnFailure(hr, "Failed to grow productname array");
            ++pContext->rgMonitorItems[dwIndex].cProductName;

            pContext->rgMonitorItems[dwIndex].rgsczProductName[pContext->rgMonitorItems[dwIndex].cProductName - 1] = sczProductName;
            sczProductName = NULL;
        }
        // Otherwise add a new monitor
        else
        {
            hr = MonAddRegKey(pContext->monitorHandle, ManifestConvertToRootKey(pProduct->rgRegKeys[i].dwRoot), pProduct->rgRegKeys[i].sczKey, REG_KEY_DEFAULT, TRUE, REGKEY_SILENCE_PERIOD, NULL);
            if (FAILED(hr))
            {
                LogErrorString(hr, "Failed to add regkey %ls to monitor list for product %ls, continuing.", pProduct->rgRegKeys[i].sczKey, wzProductName);
                pcdb->vpfBackgroundStatus(hr, BACKGROUND_STATUS_PRODUCT_ERROR, wzProductName, wzLegacyVersion, wzLegacyPublicKey, pcdb->pvCallbackContext);
                hr = S_OK;
                continue;
            }

            hr = MemEnsureArraySize(reinterpret_cast<void **>(&pContext->rgMonitorItems), pContext->cMonitorItems + 1, sizeof(MONITOR_ITEM), BACKGROUND_ARRAY_GROWTH);
            ExitOnFailure(hr, "Failed to increase space after %u monitor items", pContext->cMonitorItems);
            ++pContext->cMonitorItems;

            pItem = pContext->rgMonitorItems + pContext->cMonitorItems - 1;
            pItem->type = MONITOR_REGKEY;
            pItem->fRecursive = TRUE;

            hr = StrAllocString(&pItem->sczPath, pProduct->rgRegKeys[i].sczKey, 0);
            ExitOnFailure(hr, "Failed to copy subkey path");

            pItem->hkRoot = ManifestConvertToRootKey(pProduct->rgRegKeys[i].dwRoot);

            hr = MemEnsureArraySize(reinterpret_cast<void **>(&pItem->rgsczProductName), pItem->cProductName + 1, sizeof(LPWSTR), 0);
            ExitOnFailure(hr, "Failed to grow productname array");
            ++pItem->cProductName;

            pItem->rgsczProductName[0] = sczProductName;
            sczProductName = NULL;
        }
    }

    for (DWORD i = 0; i < pProduct->cFiles; ++i)
    {
        hr = StrAllocString(&sczProductName, wzProductName, 0);
        ExitOnFailure(hr, "Failed to allocate copy of product name string");

        if (NULL != pProduct->rgFiles[i].sczExpandedPath)
        {
            hr = PathGetDirectory(pProduct->rgFiles[i].sczExpandedPath, &sczDirectory);
            ExitOnFailure(hr, "Failed to get directory portion of path %ls", pProduct->rgFiles[i].sczExpandedPath);

            BOOL fRecursive = (LEGACY_FILE_PLAIN == pProduct->rgFiles[i].legacyFileType) ? FALSE : TRUE;

            // If this directory is already watched by some other product, add our product ID to the appID list for that monitor
            if (FindDirectoryMonitorIndex(pContext, sczDirectory, fRecursive, &dwIndex))
            {
                hr = MemEnsureArraySize(reinterpret_cast<void **>(&pContext->rgMonitorItems[dwIndex].rgsczProductName), pContext->rgMonitorItems[dwIndex].cProductName + 1, sizeof(LPWSTR), 0);
                ExitOnFailure(hr, "Failed to grow productname array");
                ++pContext->rgMonitorItems[dwIndex].cProductName;

                pContext->rgMonitorItems[dwIndex].rgsczProductName[pContext->rgMonitorItems[dwIndex].cProductName - 1] = sczProductName;
                sczProductName = NULL;
            }
            else
            {
                hr = MonAddDirectory(pContext->monitorHandle, sczDirectory, fRecursive, DIRECTORY_SILENCE_PERIOD, NULL);
                if (FAILED(hr))
                {
                    LogErrorString(hr, "Failed to add directory %ls to monitor list for product %ls, continuing.", sczDirectory, wzProductName);
                    pcdb->vpfBackgroundStatus(hr, BACKGROUND_STATUS_PRODUCT_ERROR, wzProductName, wzLegacyVersion, wzLegacyPublicKey, pcdb->pvCallbackContext);
                    hr = S_OK;
                    continue;
                }

                hr = MemEnsureArraySize(reinterpret_cast<void **>(&pContext->rgMonitorItems), pContext->cMonitorItems + 1, sizeof(MONITOR_ITEM), BACKGROUND_ARRAY_GROWTH);
                ExitOnFailure(hr, "Failed to increase space after %u monitor items", pContext->cMonitorItems);
                ++pContext->cMonitorItems;

                pItem = pContext->rgMonitorItems + pContext->cMonitorItems - 1;
                pItem->type = MONITOR_DIRECTORY;
                pItem->fRecursive = fRecursive;

                pItem->sczPath = sczDirectory;
                sczDirectory = NULL;

                hr = MemEnsureArraySize(reinterpret_cast<void **>(&pItem->rgsczProductName), pItem->cProductName + 1, sizeof(LPWSTR), 0);
                ExitOnFailure(hr, "Failed to grow productname array");
                ++pItem->cProductName;

                pItem->rgsczProductName[0] = sczProductName;
                sczProductName = NULL;
            }
        }
    }

LExit:
    ReleaseStr(sczProductName);
    ReleaseStr(sczDirectory);

    return hr;
}

static HRESULT RemoveProductFromMonitorList(
    __in_z LPCWSTR wzProductName,
    __inout MONITOR_CONTEXT *pContext
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pContext->cMonitorItems; ++i)
    {
        for (DWORD j = 0; j < pContext->rgMonitorItems[i].cProductName; ++j)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pContext->rgMonitorItems[i].rgsczProductName[j], -1, wzProductName, -1))
            {
                if (1 == pContext->rgMonitorItems[i].cProductName && !pContext->rgMonitorItems[i].fRemote)
                {
                    // It's a monitor unique to this product, so remove the entire monitor
                    if (MONITOR_DIRECTORY == pContext->rgMonitorItems[i].type)
                    {
                        hr = MonRemoveDirectory(pContext->monitorHandle, pContext->rgMonitorItems[i].sczPath, pContext->rgMonitorItems[i].fRecursive);
                        ExitOnFailure(hr, "Failed to remove directory monitor");
                    }
                    else if (MONITOR_REGKEY == pContext->rgMonitorItems[i].type)
                    {
                        hr = MonRemoveRegKey(pContext->monitorHandle, pContext->rgMonitorItems[i].hkRoot, pContext->rgMonitorItems[i].sczPath, REG_KEY_DEFAULT, pContext->rgMonitorItems[i].fRecursive);
                        ExitOnFailure(hr, "Failed to remove regkey monitor");
                    }
                    else
                    {
                        Assert(false);
                    }
                    FreeMonitorItem(pContext->rgMonitorItems + i);
                    MemRemoveFromArray(reinterpret_cast<void *>(pContext->rgMonitorItems), i, 1, pContext->cMonitorItems, sizeof(MONITOR_ITEM), TRUE);
                    --pContext->cMonitorItems;
                    --i; // We just removed the item at i, so re-process this index
                    break;
                }
                else
                {
                    // Just remove this product from the list of apps on this monitor
                    MemRemoveFromArray(reinterpret_cast<void *>(pContext->rgMonitorItems[i].rgsczProductName), j, 1, pContext->rgMonitorItems[i].cProductName, sizeof(LPWSTR), TRUE);
                    --pContext->rgMonitorItems[i].cProductName;
                    break;
                }
            }
        }
    }

LExit:
    return hr;
}

static HRESULT UpdateProductInMonitorList(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzProductName,
    __inout MONITOR_CONTEXT *pContext
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE sceRow = NULL;
    LEGACY_SYNC_SESSION syncSession = { };
    DWORD dwOriginalAppIDLocal = DWORD_MAX;
    BOOL fLocked = FALSE;

    hr = RemoveProductFromMonitorList(wzProductName, pContext);
    ExitOnFailure(hr, "Failed to remove product from monitor list while updating product");

    // Write to machine, because updated manifest could cause values to be written to disk differently
    hr = LegacySyncInitializeSession(TRUE, TRUE, &syncSession);
    ExitOnFailure(hr, "Failed to initialize legacy sync session");

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle while updating product in monitor list");
    fLocked = TRUE;
    dwOriginalAppIDLocal = pcdb->dwAppID;

    hr = ProductFindRow(pcdb, PRODUCT_INDEX_TABLE, wzProductName, wzLegacyVersion, wzLegacyPublicKey, &sceRow);
    ExitOnFailure(hr, "Failed to find row for legacy product %ls", wzProductName);

    hr = BeginMonitoringProduct(pcdb, sceRow, &syncSession, pContext);
    ExitOnFailure(hr, "Failed to begin monitoring product %ls while updating product in monitor list", wzProductName);

    hr = BackgroundSyncRemotes(pcdb);
    ExitOnFailure(hr, "Failed to send message to background thread to sync to remotes");

LExit:
    LegacySyncUninitializeSession(pcdb, &syncSession);
    ReleaseSceRow(sceRow);
    if (DWORD_MAX != dwOriginalAppIDLocal)
    {
        pcdb->dwAppID = dwOriginalAppIDLocal;
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

static HRESULT AddRemoteToMonitorList(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzPath,
    __inout MONITOR_CONTEXT *pContext
    )
{
    HRESULT hr = S_OK;
    DWORD dwIndex = DWORD_MAX;
    LPWSTR sczDirectory = NULL;
    MONITOR_ITEM *pItem = NULL;
    DWORD dwOriginalAppIDLocal = DWORD_MAX;
    BOOL fLocked = FALSE;

    hr = PathGetDirectory(wzPath, &sczDirectory);
    ExitOnFailure(hr, "Failed to get directory portion of remote path %ls", wzPath);

    // If this directory is already watched by some other product, add our product ID to the appID list for that monitor
    if (!FindDirectoryMonitorIndex(pContext, sczDirectory, FALSE, &dwIndex))
    {
        hr = MemEnsureArraySize(reinterpret_cast<void **>(&pContext->rgMonitorItems), pContext->cMonitorItems + 1, sizeof(MONITOR_ITEM), BACKGROUND_ARRAY_GROWTH);
        ExitOnFailure(hr, "Failed to increase space after %u monitor items", pContext->cMonitorItems);
        ++pContext->cMonitorItems;

        pItem = pContext->rgMonitorItems + pContext->cMonitorItems - 1;
        pItem->type = MONITOR_DIRECTORY;
        pItem->fRemote = TRUE;
        // Recursive true is important in dropbox-like situations,
        // we may have received a new DB but not all dependent streams, so we need to notice when the streams arrive
        // Retrying cannot cover this because getting the streams might take longer than our retry interval
        pItem->fRecursive = TRUE;
        pItem->sczPath = sczDirectory;
        sczDirectory = NULL;

        hr = HandleLock(pcdb);
        ExitOnFailure(hr, "Failed to lock handle while adding remote to monitor list");
        fLocked = TRUE;
        dwOriginalAppIDLocal = pcdb->dwAppID;

        hr = MonAddDirectory(pContext->monitorHandle, pItem->sczPath, TRUE, REMOTEDB_SILENCE_PERIOD, NULL);
        ExitOnFailure(hr, "Failed to add directory %ls for monitoring", pItem->sczPath);

        hr = SyncRemotes(pcdb, pItem->sczPath, FALSE);
        ExitOnFailure(hr, "Failed to sync remotes starting with the one under directory %ls", pItem->sczPath);
    }
    else
    {
        // This remote is already monitored, so nothing to do
        ExitFunction1(hr = S_OK);
    }

LExit:
    if (DWORD_MAX != dwOriginalAppIDLocal)
    {
        pcdb->dwAppID = dwOriginalAppIDLocal;
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseStr(sczDirectory);

    return hr;
}

static HRESULT RemoveRemoteFromMonitorList(
    __in_z LPCWSTR wzPath,
    __inout MONITOR_CONTEXT *pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDirectory = NULL;

    hr = PathGetDirectory(wzPath, &sczDirectory);
    ExitOnFailure(hr, "Failed to get directory portion of remote path %ls", wzPath);

    for (DWORD i = 0; i < pContext->cMonitorItems; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->rgMonitorItems[i].sczPath, -1, sczDirectory, -1))
        {
            if (0 == pContext->rgMonitorItems[i].cProductName && pContext->rgMonitorItems[i].fRemote)
            {
                // It's a monitor unique to this remote, so remove the entire monitor
                if (MONITOR_DIRECTORY != pContext->rgMonitorItems[i].type)
                {
                    hr = E_UNEXPECTED;
                    Assert(false);
                }

                hr = MonRemoveDirectory(pContext->monitorHandle, pContext->rgMonitorItems[i].sczPath, pContext->rgMonitorItems[i].fRecursive);
                ExitOnFailure(hr, "Failed to remove directory monitor");

                FreeMonitorItem(pContext->rgMonitorItems + i);
                MemRemoveFromArray(reinterpret_cast<void *>(pContext->rgMonitorItems), i, 1, pContext->cMonitorItems, sizeof(MONITOR_ITEM), TRUE);
                --pContext->cMonitorItems;
                --i; // We just removed the item at i, so re-process this index
                break;
            }
            else
            {
                // Just remove this remote from the list of apps on this monitor
                pContext->rgMonitorItems[i].fRemote = FALSE;
                break;
            }
        }
    }

LExit:
    ReleaseStr(sczDirectory);

    return hr;
}

static HRESULT PropagateRemotes(
    __in CFGDB_STRUCT *pcdb,
    __in_z_opt LPCWSTR wzFrom,
    __in BOOL fCheckDbTimestamp
    )
{
    HRESULT hr = S_OK;
    DWORD dwOriginalAppIDLocal = DWORD_MAX;
    BOOL fLocked = FALSE;

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle while propagating remotes from %ls", wzFrom);
    fLocked = TRUE;
    dwOriginalAppIDLocal = pcdb->dwAppID;

    hr = SyncRemotes(pcdb, wzFrom, fCheckDbTimestamp);
    ExitOnFailure(hr, "Failed to sync all remotes");

LExit:
    if (DWORD_MAX != dwOriginalAppIDLocal)
    {
        pcdb->dwAppID = dwOriginalAppIDLocal;
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

static HRESULT SyncRemotes(
    __in CFGDB_STRUCT *pcdb,
    __in_z_opt LPCWSTR wzFrom,
    __in BOOL fCheckDbTimestamp
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFrom = NULL;
    BOOL fRetry = FALSE;
    DWORD dwFirstSyncedIndex = DWORD_MAX;
    BOOL fChanges = (NULL == wzFrom) ? TRUE : FALSE;
    MSG msg = { };

    if (NULL != wzFrom)
    {
        dwFirstSyncedIndex = GetRemoteIndexByDirectory(pcdb, wzFrom);
        if (DWORD_MAX != dwFirstSyncedIndex)
        {
            hr = SyncRemote(pcdb->rgpcdbOpenDatabases[dwFirstSyncedIndex], fCheckDbTimestamp, &fChanges);
            // Unfortunately SQL CE just returns E_FAIL if db is busy
            if (E_FAIL == hr || HRESULT_FROM_WIN32(ERROR_SEM_TIMEOUT) == hr || HRESULT_FROM_WIN32(ERROR_TIME_SKEW) == hr || HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION) == hr)
            {
                LogErrorString(hr, "Failed to sync remote DB at %ls, it may be busy. Will retry.", pcdb->rgpcdbOpenDatabases[dwFirstSyncedIndex]->sczDbDir);
                fRetry = TRUE;
                hr = S_OK;
            }
            // This may mean network connection with server or removable drive was lost temporarily. MonUtil will tell us when to retry.
            else if (HRESULT_FROM_WIN32(ERROR_BAD_NETPATH) == hr || HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME) == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_INVALID_DRIVE) == hr)
            {
                LogErrorString(hr, "Failed to sync remote DB at %ls, the network connection may be down, or the server may be down.", pcdb->rgpcdbOpenDatabases[dwFirstSyncedIndex]->sczDbDir);
                hr = S_OK;
            }
            else if (HRESULT_FROM_WIN32(PEERDIST_ERROR_MISSING_DATA) == hr)
            {
                LogErrorString(hr, "Stream file not (yet) present for database %ls. Autosync will not retry now, but will automatically retry if file later becomes present.", pcdb->rgpcdbOpenDatabases[dwFirstSyncedIndex]->sczDbDir);
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to sync first remote");
        }
        else
        {
            LogErrorString(HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND), "Unable to sync remote at directory because it was not found in remote database list: %ls", wzFrom);
            return S_OK;
        }
    }

    if (!fRetry && fChanges)
    {
        for (DWORD i = 0; i < pcdb->cOpenDatabases; ++i)
        {
            if (pcdb->rgpcdbOpenDatabases[i]->fSyncByDefault && i != dwFirstSyncedIndex)
            {
                hr = SyncRemote(pcdb->rgpcdbOpenDatabases[i], FALSE, NULL);
                // Unfortunately SQL CE just returns E_FAIL if db is busy
                if (E_FAIL == hr || HRESULT_FROM_WIN32(ERROR_SEM_TIMEOUT) == hr || HRESULT_FROM_WIN32(ERROR_TIME_SKEW) == hr || HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION) == hr)
                {
                    LogErrorString(hr, "Failed to sync remote DB at %ls, it may be busy. Will retry.", pcdb->rgpcdbOpenDatabases[i]->sczDbDir);
                    fRetry = TRUE;
                    hr = S_OK;
                    break;
                }
                // This may mean network connection with server or removable drive was lost temporarily. MonUtil will tell us when to retry.
                else if (HRESULT_FROM_WIN32(ERROR_BAD_NETPATH) == hr || HRESULT_FROM_WIN32(ERROR_BAD_PATHNAME) == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_INVALID_DRIVE) == hr)
                {
                    LogErrorString(hr, "Failed to sync remote DB at %ls, the network connection may be down, or the server may be down.", pcdb->rgpcdbOpenDatabases[i]->sczDbDir);
                    hr = S_OK;
                }
                else if (HRESULT_FROM_WIN32(PEERDIST_ERROR_MISSING_DATA) == hr)
                {
                    LogErrorString(hr, "Stream file not (yet) present for database %ls. Autosync will not retry now, but will automatically retry if file later becomes present.", pcdb->rgpcdbOpenDatabases[i]->sczDbDir);
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to sync another remote");
            }
        }
    }

    if (fRetry)
    {
        // Only sleep if there are no pending messages at the moment
        if (!::PeekMessage(&msg, NULL, 0, 0, PM_NOREMOVE))
        {
            // TODO: it may be a bad idea for perf hold up the entire background thread with this sleep, but this works for now
            ::Sleep(REMOTEDB_BUSY_RETRY_PERIOD);
        }

        if (NULL != wzFrom)
        {
            hr = StrAllocString(&sczFrom, wzFrom, 0);
            ExitOnFailure(hr, "Failed to copy from string");
        }

        if (!::PostThreadMessageW(pcdb->dwBackgroundThreadId, NULL == sczFrom ? BACKGROUND_THREAD_SYNC_TO_REMOTES : BACKGROUND_THREAD_SYNC_FROM_REMOTE, reinterpret_cast<WPARAM>(sczFrom), static_cast<LPARAM>(fCheckDbTimestamp)))
        {
            ExitWithLastError(hr, "Failed to send message to background thread to sync remotes");
        }
        sczFrom = NULL;
    }

LExit:
    ReleaseStr(sczFrom);

    return hr;
}

static HRESULT SyncRemote(
    __in CFGDB_STRUCT *pcdb,
    __in BOOL fCheckDbTimestamp,
    __out BOOL *pfChanged
    )
{
    HRESULT hr = S_OK;
    BOOL fSyncingRemote = FALSE;
    CONFLICT_PRODUCT *rgProductConflicts = NULL;
    DWORD cProductConflicts = 0;
    FILETIME ftLastModified = { };
    BOOL fLocked = FALSE;

    if (NULL != pfChanged)
    {
        *pfChanged = FALSE;
    }

    if (fCheckDbTimestamp)
    {
        hr = FileGetTime(pcdb->sczDbPath, NULL, NULL, &ftLastModified);
        ExitOnFailure(hr, "Failed to get file time of remote db: %ls", pcdb->sczDbPath);

        if (0 == ::CompareFileTime(&ftLastModified, &pcdb->ftLastModified))
        {
            // If we're not syncing notify that the remote is at least in good shape in case it wasn't before
            pcdb->pcdbLocal->vpfBackgroundStatus(S_OK, BACKGROUND_STATUS_REMOTE_GOOD, pcdb->sczOriginalDbPath, NULL, NULL, pcdb->pcdbLocal->pvCallbackContext);
            ExitFunction1(hr = S_OK);
        }
    }

    LogStringLine(REPORT_STANDARD, "Actually syncing with remote %ls", pcdb->sczDbPath);

    // Lock, sync, unlock
    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock remote handle");
    fLocked = TRUE;

    pcdb->pcdbLocal->vpfBackgroundStatus(S_OK, BACKGROUND_STATUS_SYNCING_REMOTE, pcdb->sczOriginalDbPath, NULL, NULL, pcdb->pcdbLocal->pvCallbackContext);
    fSyncingRemote = TRUE;

    hr = UtilSyncDb(pcdb, &rgProductConflicts, &cProductConflicts);
    ExitOnFailure(hr, "Failed to sync with remote database");

    if (NULL != pfChanged)
    {
        *pfChanged = SceDatabaseChanged(pcdb->psceDb) || SceDatabaseChanged(pcdb->pcdbLocal->psceDb);
    }

    if (0 < cProductConflicts)
    {
        pcdb->pcdbLocal->vpfConflictsFound(pcdb, rgProductConflicts, cProductConflicts, pcdb->pcdbLocal->pvCallbackContext);
        rgProductConflicts = NULL;
        cProductConflicts = 0;
    }

LExit:
    if (FAILED(hr) || fSyncingRemote)
    {
        pcdb->pcdbLocal->vpfBackgroundStatus(hr, BACKGROUND_STATUS_SYNC_REMOTE_FINISHED, pcdb->sczOriginalDbPath, NULL, NULL, pcdb->pcdbLocal->pvCallbackContext);
    }
    if (fLocked)
    {
        if (SUCCEEDED(hr))
        {
            // We can't fully rely on HandleUnlock's "did database change?" check because a sync can succeed partially, but fail on some other product
            // in that case, we need to avoid updating our last modified timestamp so we will sync the database again and pickup the rest of those changes.
            pcdb->fUpdateLastModified = TRUE;
        }
        HandleUnlock(pcdb);
    }
    CfgReleaseConflictProductArray(rgProductConflicts, cProductConflicts);

    return hr;
}

static BOOL FindDirectoryMonitorIndex(
    __in MONITOR_CONTEXT *pContext,
    __in_z LPCWSTR wzPath,
    __in BOOL fRecursive,
    __out DWORD *pdwIndex
    )
{
    for (DWORD i = 0; i < pContext->cMonitorItems; ++i)
    {
        if (MONITOR_DIRECTORY == pContext->rgMonitorItems[i].type && fRecursive == pContext->rgMonitorItems[i].fRecursive && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->rgMonitorItems[i].sczPath, -1, wzPath, -1))
        {
            *pdwIndex = i;
            return TRUE;
        }
    }

    return FALSE;
}

static BOOL FindRegKeyMonitorIndex(
    __in MONITOR_CONTEXT *pContext,
    __in_z LPCWSTR wzSubKey,
    __in HKEY hkRoot,
    __out DWORD *pdwIndex
    )
{
    for (DWORD i = 0; i < pContext->cMonitorItems; ++i)
    {
        if (MONITOR_REGKEY == pContext->rgMonitorItems[i].type && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->rgMonitorItems[i].sczPath, -1, wzSubKey, -1)
            && pContext->rgMonitorItems[i].hkRoot == hkRoot)
        {
            *pdwIndex = i;
            return TRUE;
        }
    }

    return FALSE;
}

static void FreeSyncRequest(
    __in SYNC_REQUEST *pSyncRequest
    )
{
    ReleaseStr(pSyncRequest->sczPath);
}

static void FreeMonitorItem(
    __in MONITOR_ITEM *pItem
    )
{
    ReleaseStr(pItem->sczPath);
    ReleaseMem(pItem->rgsczProductName);
}

static void FreeMonitorContext(
    __in MONITOR_CONTEXT *pContext
    )
{
    ReleaseMon(pContext->monitorHandle);
    for (DWORD i = 0; i < pContext->cMonitorItems; ++i)
    {
        FreeMonitorItem(pContext->rgMonitorItems + i);
    }
    ReleaseMem(pContext->rgMonitorItems);
}

static DWORD GetRemoteIndexByDirectory(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzDirectory
    )
{
    for (DWORD i = 0; i < pcdb->cOpenDatabases; ++i)
    {
        if (pcdb->rgpcdbOpenDatabases[i]->fSyncByDefault && (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pcdb->rgpcdbOpenDatabases[i]->sczOriginalDbDir, -1, wzDirectory, -1)))
        {
            return i;
        }
    }

    LogStringLine(REPORT_STANDARD, "Couldn't find remote index with directory %ls", wzDirectory);

    return DWORD_MAX;
}
