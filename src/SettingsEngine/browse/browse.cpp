// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const DWORD INTER_PROCESS_PROTOCOL_VERSION = 1;

HWND hwnd = NULL;
CFGDB_HANDLE cdbLocal = NULL;
DWORD dwLocalDatabaseIndex = 0;
BOOL fCfgInitialized = FALSE;
// Used to track when auto sync has finished starting up. This is to avoid us unnecessarily (and repeatedly) refreshing products while it's starting up.
// If background thread reports a success message before this is set to true, we don't do anything.
// If background thread reports a failure, we still report it to UI.
BOOL fCfgAutoSyncRunning = FALSE;
BOOL fCfgRedetectingProducts = FALSE;
BOOL fCfgAdminInitialized = FALSE;
BROWSE_DATABASE_LIST bdlDatabaseList = { };

static HRESULT ProcessCommandLine(
    __in_z LPCWSTR wzCommandLine,
    __out COMMANDLINE_REQUEST * pCommandLineRequest
    );
static void FreeCommandLineRequest(
    __inout COMMANDLINE_REQUEST & commandLineRequest
    );
static BOOL ProcessMessage(
    MSG *msg,
    BrowseWindow *browser
    );
static HRESULT CheckProductInstalledState(
    __in CFGDB_HANDLE pcdLocalHandle,
    __in C_CFG_ENUMERATION_HANDLE cehProducts,
    __in DWORD dwProductCount,
    __deref_out_ecount_opt(dwProductCount) BOOL **prgfInstalled
    );
static HRESULT CheckSingleInstance(
    __in const COMMANDLINE_REQUEST & commandLineRequest,
    __out HANDLE *phLockFile
    );
static void BackgroundStatusCallback(
    __in HRESULT hr,
    __in BACKGROUND_STATUS_TYPE type,
    __in_z LPCWSTR wzString1,
    __in_z LPCWSTR wzString2,
    __in_z LPCWSTR wzString3,
    __in LPVOID pvContext
    );
static void BackgroundConflictsFoundCallback(
    __in CFGDB_HANDLE cdHandle,
    __in CONFLICT_PRODUCT *rgcpProduct,
    __in DWORD cProduct,
    __in LPVOID pvContext
    );
static DWORD FindRemoteDatabaseByPath(
    __in LPCWSTR wzPath
    );

int WINAPI wWinMain(
    __in HINSTANCE hInstance,
    __in_opt HINSTANCE hPrevInstance,
    __in_opt LPWSTR lpCmdLine,
    __in int nCmdShow
    )
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);
    UNREFERENCED_PARAMETER(nCmdShow);

    HRESULT hr = S_OK;
    HANDLE hLockFile = INVALID_HANDLE_VALUE;
    BOOL fRet = FALSE;
    BOOL fLoadedCommandLineManifests = FALSE;
    DWORD dwUIThreadId = 0;
    DWORD dwExitCode = 0;
    LPWSTR sczTemp = NULL;
    BOOL fThemeInitialized = FALSE;
    MSG msg = { };
    BrowseWindow *browser = NULL;
    COMMANDLINE_REQUEST commandLineRequest = { };
    BOOL fComInitialized = FALSE;

    (void)HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    if (!IsLogInitialized())
    {
        LogInitialize(NULL);
    }

    hr = ProcessCommandLine(lpCmdLine, &commandLineRequest);
    ExitOnFailure(hr, "Failed to process command line");

    if (commandLineRequest.fHelpRequested)
    {
        ::MessageBoxW(NULL, L"CfgBrowser.exe [/?] | [/manifest manifest.udm] [/manifest anothermanifest.udm] ...", L"WiX Settings Browser Commandline Help", MB_OK);
        return 0;
    }

    hr = CheckSingleInstance(commandLineRequest, &hLockFile);
    if (HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS) == hr)
    {
        Trace(REPORT_STANDARD, "Another browser instance is running, deferring to that instance");
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to ensure this is the main browser instance");

    // initialize COM
    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    hr = ThemeInitialize(::GetModuleHandleW(NULL));
    ExitOnFailure(hr, "Failed to initialize ThmUtil");

    ::InitializeCriticalSection(&bdlDatabaseList.cs);

    browser = new BrowseWindow(hInstance, &bdlDatabaseList);
    ExitOnNull(browser, hr, E_OUTOFMEMORY, "Failed to allocate main window class");

    ::PeekMessageW(&msg, NULL, WM_USER, WM_USER, PM_NOREMOVE);

    hr = browser->Initialize(&dwUIThreadId);
    ExitOnFailure(hr, "Failed to initialize main window");

    // Nobody cares about failure - this is just best effort to boost UI responsiveness.
    ::SetThreadPriority(::GetCurrentThread(), THREAD_MODE_BACKGROUND_BEGIN);

    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnRootFailure(hr, "Unexpected return value from message pump.");
        }
        else
        {
            if (!ProcessMessage(&msg, browser))
            {
                break; // If we can't communicate with the UI thread, get out of here!
            }
            
            // Now queue up any legacy manifests that were specified on the commandline
            if (!fLoadedCommandLineManifests && fCfgInitialized)
            {
                for (DWORD i = 0; i < commandLineRequest.cLegacyManifests; ++i)
                {
                    hr = StrAllocString(&sczTemp, commandLineRequest.rgsczLegacyManifests[i], 0);
                    ExitOnFailure(hr, "Failed to allocate copy of legacy manifest path string");

                    if (!::PostThreadMessageW(::GetCurrentThreadId(), WM_BROWSE_IMPORT_LEGACY_MANIFEST, reinterpret_cast<WPARAM>(sczTemp), 0))
                    {
                        ExitWithLastError(hr, "Failed to send WM_BROWSE_IMPORT_LEGACY_MANIFEST message to same thread");
                    }
                    sczTemp = NULL;
                }
                fLoadedCommandLineManifests = TRUE;
            }
        }
    }

    // get exit code
    dwExitCode = (DWORD)msg.wParam;

LExit:
    ReleaseStr(sczTemp);
    FreeCommandLineRequest(commandLineRequest);

    if (browser)
    {
        browser->Uninitialize();
        delete browser;
    }

    // UI is dead, this is no longer the background thread. Set thread priority back to normal for potentially quicker shutdown.
    ::SetThreadPriority(::GetCurrentThread(), THREAD_MODE_BACKGROUND_END);

    if (fThemeInitialized)
    {
        ThemeUninitialize();
        fThemeInitialized = FALSE;
    }

    // Release remote databases
    for (DWORD i = 0; i < bdlDatabaseList.cDatabases; ++i)
    {
        if (NULL != bdlDatabaseList.rgDatabases[i].cdb)
        {
            if (DATABASE_REMOTE == bdlDatabaseList.rgDatabases[i].dtType)
            {
                CfgRemoteDisconnect(bdlDatabaseList.rgDatabases[i].cdb);
            }
            else if (DATABASE_ADMIN == bdlDatabaseList.rgDatabases[i].dtType)
            {
                CfgAdminUninitialize(bdlDatabaseList.rgDatabases[i].cdb);
            }
            // Don't free local database here - we only have one open, it must be freed after all remotes, and is specifically freed below this loop
        }

        UtilFreeDatabase(bdlDatabaseList.rgDatabases + i);
    }
    ReleaseMem(bdlDatabaseList.rgDatabases);

    if (fCfgInitialized)
    {
        CfgUninitialize(cdbLocal);
        fCfgInitialized = FALSE;
    }

    ReleaseFileHandle(hLockFile);

    ::DeleteCriticalSection(&bdlDatabaseList.cs);

    // uninitialize COM
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return dwExitCode;
}

HRESULT ProcessCommandLine(
    __in_z LPCWSTR wzCommandLine,
    __out COMMANDLINE_REQUEST * pCommandLineRequest
    )
{
    HRESULT hr = S_OK;
    int argc = 0;
    LPWSTR* argv = NULL;
    BOOL fUnknownArg = FALSE;
    LPWSTR sczCommandLine = NULL;

    if (wzCommandLine && *wzCommandLine)
    {
        // CommandLineToArgvW tries to treat the first argument as the path to the process,
        // which fails pretty miserably if your first argument is something like
        // FOO="C:\Program Files\My Company". So give it something harmless to play with.
        hr = StrAllocConcat(&sczCommandLine, L"ignored ", 0);
        ExitOnFailure(hr, "Failed to initialize command line.");

        hr = StrAllocConcat(&sczCommandLine, wzCommandLine, 0);
        ExitOnFailure(hr, "Failed to copy command line.");

        argv = ::CommandLineToArgvW(sczCommandLine, &argc);
        ExitOnNullWithLastError(argv, hr, "Failed to get command line.");
    }

    for (int i = 1; i < argc; ++i) // skip "ignored" argument/hack
    {
        fUnknownArg = FALSE;

        if (argv[i][0] == L'-' || argv[i][0] == L'/')
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"manifest", -1))
            {
                if (i + 1 >= argc)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Must specify a path for legacy manifest.");
                }

                ++i;

                hr = MemEnsureArraySize((LPVOID *)&pCommandLineRequest->rgsczLegacyManifests, pCommandLineRequest->cLegacyManifests + 1, sizeof(LPWSTR), 10);
                ExitOnFailure(hr, "Failed to resize legacy manifest array to size %u", pCommandLineRequest->cLegacyManifests + 1);

                hr = PathExpand(&pCommandLineRequest->rgsczLegacyManifests[pCommandLineRequest->cLegacyManifests], argv[i], PATH_EXPAND_FULLPATH);
                ExitOnFailure(hr, "Failed to expand legacy manifest path %ls", argv[i]);

                ++(pCommandLineRequest->cLegacyManifests);
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"?", -1) ||
                     CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"h", -1) ||
                     CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"help", -1))
            {
                pCommandLineRequest->fHelpRequested = TRUE;
            }
            else
            {
                pCommandLineRequest->fHelpRequested = TRUE;
            }
        }
        else
        {
            pCommandLineRequest->fHelpRequested = TRUE;
        }
    }

LExit:
    if (argv)
    {
        ::LocalFree(argv);
    }
    ReleaseStr(sczCommandLine);

    return hr;
}

static void FreeCommandLineRequest(
    __inout COMMANDLINE_REQUEST & commandLineRequest
    )
{
    for (DWORD i = 0; i < commandLineRequest.cLegacyManifests; ++i)
    {
        ReleaseStr(commandLineRequest.rgsczLegacyManifests[i]);
    }

    ReleaseMem(commandLineRequest.rgsczLegacyManifests);
}

BOOL ProcessMessage(
    MSG *msg,
    BrowseWindow *browser
    )
{
    HRESULT hr = S_OK;
    HRESULT hrSend = S_OK;
    MSG msgTemp = { }; // Used for PeekMessage
    CFG_ENUMERATION_HANDLE cehHandle = NULL;
    DWORD dwEnumCount = 0;
    DWORD dwIndex = 0;
    BOOL fCsEntered = FALSE;
    DWORD dwTemp = 0;
    BYTE *pbData = NULL;
    DWORD cbData = 0;
    LPWSTR sczTemp = NULL;
    DWORD_STRING *pdsDwordString = NULL;
    QWORD_STRING *pqsQwordString = NULL;
    STRING_PAIR *pspStringPair = NULL;
    STRING_TRIPLET *pstStringTriplet = NULL;
    BACKGROUND_STATUS_CALLBACK *pBackgroundStatusCallback = NULL;
    BACKGROUND_CONFLICTS_FOUND_CALLBACK *pBackgroundConflictsFoundCallback = NULL;

    switch (msg->message)
    {
    case WM_BROWSE_RECEIVE_HWND:
        hwnd = reinterpret_cast<HWND>(msg->wParam);
        break;

    case WM_BROWSE_INITIALIZE:
        dwIndex = static_cast<DWORD>(msg->wParam);

        hrSend = CfgInitialize(&cdbLocal, BackgroundStatusCallback, BackgroundConflictsFoundCallback, reinterpret_cast<LPVOID>(::GetCurrentThreadId()));
        if (SUCCEEDED(hrSend))
        {
            bdlDatabaseList.rgDatabases[dwIndex].cdb = cdbLocal;
            dwLocalDatabaseIndex = dwIndex;
            fCfgInitialized = TRUE;
        }

        if (!::PostMessageW(hwnd, WM_BROWSE_INITIALIZE_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_INITIALIZE_FINISHED message");
        }
        break;
    case WM_BROWSE_ENUMERATE_PRODUCTS:
        dwIndex = static_cast<DWORD>(msg->wParam);
        hrSend = CfgEnumerateProducts(bdlDatabaseList.rgDatabases[dwIndex].cdb, &cehHandle, &dwEnumCount);

        ::EnterCriticalSection(&bdlDatabaseList.rgDatabases[dwIndex].cs);
        fCsEntered = TRUE;

        CfgReleaseEnumeration(bdlDatabaseList.rgDatabases[dwIndex].productEnum.cehItems);
        bdlDatabaseList.rgDatabases[dwIndex].productEnum.cehItems = cehHandle;
        cehHandle = NULL;

        ::LeaveCriticalSection(&bdlDatabaseList.rgDatabases[dwIndex].cs);
        fCsEntered = FALSE;

        if (SUCCEEDED(hr))
        {
            hrSend = CheckProductInstalledState(cdbLocal, bdlDatabaseList.rgDatabases[dwIndex].productEnum.cehItems, dwEnumCount, &bdlDatabaseList.rgDatabases[dwIndex].rgfProductInstalled);
            ExitOnFailure(hrSend, "Failed to check product installed state");
        }
        else
        {
            ReleaseNullMem(bdlDatabaseList.rgDatabases[dwIndex].rgfProductInstalled);
        }

        if (!::PostMessageW(hwnd, WM_BROWSE_ENUMERATE_PRODUCTS_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_ENUMERATE_PRODUCTS_FINISHED message");
        }
        break;
    case WM_BROWSE_ENUMERATE_DATABASES:
        dwIndex = static_cast<DWORD>(msg->wParam);
        hrSend = CfgEnumDatabaseList(bdlDatabaseList.rgDatabases[dwIndex].cdb, &cehHandle, &dwEnumCount);
        ::EnterCriticalSection(&bdlDatabaseList.rgDatabases[dwIndex].cs);
        fCsEntered = TRUE;

        CfgReleaseEnumeration(bdlDatabaseList.rgDatabases[dwIndex].dbEnum.cehItems);
        bdlDatabaseList.rgDatabases[dwIndex].dbEnum.cehItems = cehHandle;
        cehHandle = NULL;
        bdlDatabaseList.rgDatabases[dwIndex].dbEnum.cItems = dwEnumCount;

        if (!::PostMessageW(hwnd, WM_BROWSE_ENUMERATE_DATABASES_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_ENUMERATE_DATABASES_FINISHED message");
        }
        break;
    case WM_BROWSE_SET_PRODUCT:
        dwIndex = static_cast<DWORD>(msg->wParam);

        pstStringTriplet = reinterpret_cast<STRING_TRIPLET *>(msg->lParam);

        hrSend = CfgSetProduct(bdlDatabaseList.rgDatabases[dwIndex].cdb, pstStringTriplet->sczString1, pstStringTriplet->sczString2, pstStringTriplet->sczString3);
        if (!::PostMessageW(hwnd, WM_BROWSE_SET_PRODUCT_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_SET_PRODUCT_FINISHED message");
        }
        break;
    case WM_BROWSE_SET_STRING:
        dwIndex = static_cast<DWORD>(msg->wParam);

        pspStringPair = reinterpret_cast<STRING_PAIR *>(msg->lParam);

        hrSend = CfgSetString(bdlDatabaseList.rgDatabases[dwIndex].cdb, pspStringPair->sczString1, pspStringPair->sczString2);
        if (!::PostMessageW(hwnd, WM_BROWSE_SET_STRING_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_SET_STRING_FINISHED message");
        }
        break;
    case WM_BROWSE_SET_DWORD:
        dwIndex = static_cast<DWORD>(msg->wParam);

        pdsDwordString = reinterpret_cast<DWORD_STRING *>(msg->lParam);

        hrSend = CfgSetDword(bdlDatabaseList.rgDatabases[dwIndex].cdb, pdsDwordString->sczString1, pdsDwordString->dwDword1);
        if (!::PostMessageW(hwnd, WM_BROWSE_SET_DWORD_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_SET_DWORD_FINISHED message");
        }
        break;
    case WM_BROWSE_SET_QWORD:
        dwIndex = static_cast<DWORD>(msg->wParam);

        pqsQwordString = reinterpret_cast<QWORD_STRING *>(msg->lParam);

        hrSend = CfgSetQword(bdlDatabaseList.rgDatabases[dwIndex].cdb, pqsQwordString->sczString1, pqsQwordString->qwQword1);
        if (!::PostMessageW(hwnd, WM_BROWSE_SET_QWORD_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_SET_QWORD_FINISHED message");
        }
        break;
    case WM_BROWSE_SET_BOOL:
        dwIndex = static_cast<DWORD>(msg->wParam);

        pdsDwordString = reinterpret_cast<DWORD_STRING *>(msg->lParam);

        hrSend = CfgSetBool(bdlDatabaseList.rgDatabases[dwIndex].cdb, pdsDwordString->sczString1, static_cast<BOOL>(pdsDwordString->dwDword1));
        if (!::PostMessageW(hwnd, WM_BROWSE_SET_BOOL_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_SET_BOOL_FINISHED message");
        }
        break;
    case WM_BROWSE_ENUMERATE_VALUES:
        dwIndex = static_cast<DWORD>(msg->wParam);
        hrSend = CfgEnumerateValues(bdlDatabaseList.rgDatabases[dwIndex].cdb, static_cast<CONFIG_VALUETYPE>(msg->lParam), &cehHandle, &dwEnumCount);
        ::EnterCriticalSection(&bdlDatabaseList.rgDatabases[dwIndex].cs);
        fCsEntered = TRUE;

        CfgReleaseEnumeration(bdlDatabaseList.rgDatabases[dwIndex].valueEnum.cehItems);
        bdlDatabaseList.rgDatabases[dwIndex].valueEnum.cehItems = cehHandle;
        cehHandle = NULL;
        bdlDatabaseList.rgDatabases[dwIndex].valueEnum.cItems = dwEnumCount;

        if (!::PostMessageW(hwnd, WM_BROWSE_ENUMERATE_VALUES_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_ENUMERATE_VALUES_FINISHED message");
        }
        break;

    case WM_BROWSE_ENUMERATE_VALUE_HISTORY:
        dwIndex = static_cast<DWORD>(msg->wParam);
        sczTemp = reinterpret_cast<LPWSTR>(msg->lParam);

        hrSend = CfgEnumPastValues(bdlDatabaseList.rgDatabases[dwIndex].cdb, sczTemp, &cehHandle, &dwEnumCount);
        ::EnterCriticalSection(&bdlDatabaseList.rgDatabases[dwIndex].cs);
        fCsEntered = TRUE;

        CfgReleaseEnumeration(bdlDatabaseList.rgDatabases[dwIndex].valueHistoryEnum.cehItems);
        bdlDatabaseList.rgDatabases[dwIndex].valueHistoryEnum.cehItems = cehHandle;
        cehHandle = NULL;
        bdlDatabaseList.rgDatabases[dwIndex].valueHistoryEnum.cItems = dwEnumCount;

        if (!::PostMessageW(hwnd, WM_BROWSE_ENUMERATE_VALUE_HISTORY_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_ENUMERATE_VALUE_HISTORY_FINISHED message");
        }
        break;
    
    case WM_BROWSE_READ_LEGACY_SETTINGS:
        hrSend = CfgLegacyReadLatest(cdbLocal);
        if (!::PostMessageW(hwnd, WM_BROWSE_READ_LEGACY_SETTINGS_FINISHED, static_cast<WPARAM>(hrSend), 0))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_READ_LEGACY_SETTINGS_FINISHED message");
        }

        hr = S_OK;
        break;

    case WM_BROWSE_IMPORT_LEGACY_MANIFEST:
        sczTemp = reinterpret_cast<LPWSTR>(msg->wParam);

        hrSend = CfgLegacyImportProductFromXMLFile(cdbLocal, sczTemp);
        
        // Optimization: if legacy import was successful and we have more imports pending, DON'T notify UI thread that we finished yet.
        if (FAILED(hrSend) || !::PeekMessage(&msgTemp, NULL, WM_BROWSE_IMPORT_LEGACY_MANIFEST, WM_BROWSE_IMPORT_LEGACY_MANIFEST, PM_NOREMOVE))
        {
            if (!::PostMessageW(hwnd, WM_BROWSE_IMPORT_LEGACY_MANIFEST_FINISHED, static_cast<WPARAM>(hrSend), 0))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_IMPORT_LEGACY_MANIFEST_FINISHED message");
            }
        }

        hr = S_OK;
        break;

    case WM_BROWSE_SET_FILE:
        dwIndex = static_cast<DWORD>(msg->wParam);

        pspStringPair = reinterpret_cast<STRING_PAIR *>(msg->lParam); // sczString1 = Cfg DB file name, sczString2 = filesystem file path

        hrSend = FileRead(&pbData, &cbData, pspStringPair->sczString2);
        if (FAILED(hr))
        {
            if (!::PostMessageW(hwnd, WM_BROWSE_SET_FILE_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_SET_FILE_FINISHED message on failed file read");
            }
        }
        else
        {
            hrSend = CfgSetBlob(bdlDatabaseList.rgDatabases[dwIndex].cdb, pspStringPair->sczString1, pbData, cbData);
            if (!::PostMessageW(hwnd, WM_BROWSE_SET_FILE_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_SET_FILE_FINISHED message on failed set file");
            }
        }            
        break;
    case WM_BROWSE_EXPORT_FILE:
        dwIndex = static_cast<DWORD>(msg->wParam);

        pspStringPair = reinterpret_cast<STRING_PAIR *>(msg->lParam); // sczString1 = Cfg DB file name, sczString2 = filesystem file path

        hrSend = CfgGetBlob(bdlDatabaseList.rgDatabases[dwIndex].cdb, pspStringPair->sczString1, &pbData, &cbData);
        if (FAILED(hr))
        {
            if (!::PostMessageW(hwnd, WM_BROWSE_EXPORT_FILE_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_EXPORT_FILE_FINISHED message on failed file read");
            }
        }
        else
        {
            hrSend = FileWrite(pspStringPair->sczString2, FILE_ATTRIBUTE_NORMAL, pbData, cbData, NULL);
            if (FAILED(hr))
            {
                if (!::PostMessageW(hwnd, WM_BROWSE_EXPORT_FILE_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_EXPORT_FILE_FINISHED message on failed file write");
                }
            }
        }
        break;
    case WM_BROWSE_EXPORT_FILE_FROM_HISTORY:
        dwIndex = static_cast<DWORD>(msg->wParam);

        pdsDwordString = reinterpret_cast<DWORD_STRING *>(msg->lParam);

        hrSend = CfgEnumReadBinary(bdlDatabaseList.rgDatabases[dwIndex].cdb, bdlDatabaseList.rgDatabases[dwIndex].valueHistoryEnum.cehItems, pdsDwordString->dwDword1, ENUM_DATA_BLOBCONTENT, &pbData, &cbData);
        if (FAILED(hr))
        {
            if (!::PostMessageW(hwnd, WM_BROWSE_EXPORT_FILE_FROM_HISTORY_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_EXPORT_FILE_FROM_HISTORY_FINISHED message on failed file read");
            }
        }
        else
        {
            hrSend = FileWrite(pdsDwordString->sczString1, FILE_ATTRIBUTE_NORMAL, pbData, cbData, NULL);
            if (FAILED(hr))
            {
                if (!::PostMessageW(hwnd, WM_BROWSE_EXPORT_FILE_FROM_HISTORY_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_EXPORT_FILE_FROM_HISTORY_FINISHED message on failed file write");
                }
            }
        }
        break;
    case WM_BROWSE_SYNC:
        dwIndex = static_cast<DWORD>(msg->wParam);
        ::EnterCriticalSection(&bdlDatabaseList.rgDatabases[dwIndex].cs);
        fCsEntered = TRUE;

        // Release previous conflict product struct, if there is one
        CfgReleaseConflictProductArray(bdlDatabaseList.rgDatabases[dwIndex].pcplConflictProductList, bdlDatabaseList.rgDatabases[dwIndex].dwConflictProductCount);
        bdlDatabaseList.rgDatabases[dwIndex].pcplConflictProductList = NULL;
        bdlDatabaseList.rgDatabases[dwIndex].dwConflictProductCount = 0;

        hrSend = CfgSync(bdlDatabaseList.rgDatabases[dwIndex].cdb, &bdlDatabaseList.rgDatabases[dwIndex].pcplConflictProductList, &bdlDatabaseList.rgDatabases[dwIndex].dwConflictProductCount);
        if (!::PostMessageW(hwnd, WM_BROWSE_SYNC_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_SYNC_FINISHED message");
        }
        break;

    case WM_BROWSE_RESOLVE:
        dwIndex = static_cast<DWORD>(msg->wParam);

        hrSend = CfgResolve(bdlDatabaseList.rgDatabases[dwIndex].cdb, bdlDatabaseList.rgDatabases[dwIndex].pcplConflictProductList, bdlDatabaseList.rgDatabases[dwIndex].dwConflictProductCount);
        if (!::PostMessageW(hwnd, WM_BROWSE_RESOLVE_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_RESOLVE_FINISHED message");
        }
        break;

    case WM_BROWSE_CREATE_REMOTE:
        dwIndex = static_cast<DWORD>(msg->wParam);

        hrSend = CfgCreateRemoteDatabase(bdlDatabaseList.rgDatabases[dwIndex].sczPath, &(bdlDatabaseList.rgDatabases[dwIndex].cdb));
        if (!::PostMessageW(hwnd, WM_BROWSE_CREATE_REMOTE_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_CREATE_REMOTE_FINISHED message");
        }
        break;

    case WM_BROWSE_OPEN_REMOTE:
        dwIndex = static_cast<DWORD>(msg->wParam);

        if (NULL != bdlDatabaseList.rgDatabases[dwIndex].sczName)
        {
            hrSend = CfgOpenKnownRemoteDatabase(bdlDatabaseList.rgDatabases[dwLocalDatabaseIndex].cdb, bdlDatabaseList.rgDatabases[dwIndex].sczName, &(bdlDatabaseList.rgDatabases[dwIndex].cdb));

            if (E_NOTFOUND == hrSend)
            {
                // It's not a known remote db yet, so just open it plainly, a WM_BROWSE_REMEMBER message should be coming
                hrSend = CfgOpenRemoteDatabase(bdlDatabaseList.rgDatabases[dwIndex].sczPath, &(bdlDatabaseList.rgDatabases[dwIndex].cdb));
            }
        }
        else
        {
            hrSend = CfgOpenRemoteDatabase(bdlDatabaseList.rgDatabases[dwIndex].sczPath, &(bdlDatabaseList.rgDatabases[dwIndex].cdb));
        }

        if (!::PostMessageW(hwnd, WM_BROWSE_OPEN_REMOTE_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_OPEN_REMOTE_FINISHED message");
        }
        break;

    case WM_BROWSE_REMEMBER:
        dwIndex = static_cast<DWORD>(msg->wParam);

        hrSend = CfgRememberDatabase(cdbLocal, bdlDatabaseList.rgDatabases[dwIndex].cdb, bdlDatabaseList.rgDatabases[dwIndex].sczName, bdlDatabaseList.rgDatabases[dwIndex].fSyncByDefault);
        if (!::PostMessageW(hwnd, WM_BROWSE_REMEMBER_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_REMEMBER_FINISHED message");
        }
        break;

    case WM_BROWSE_FORGET:
        dwIndex = static_cast<DWORD>(msg->wParam);

        hrSend = CfgForgetDatabase(cdbLocal, bdlDatabaseList.rgDatabases[dwIndex].cdb, bdlDatabaseList.rgDatabases[dwIndex].sczName);
        if (!::PostMessageW(hwnd, WM_BROWSE_FORGET_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_FORGET_FINISHED message");
        }
        break;

    case WM_BROWSE_DISCONNECT:
        dwIndex = static_cast<DWORD>(msg->wParam);

        hrSend = CfgRemoteDisconnect(bdlDatabaseList.rgDatabases[dwIndex].cdb);
        if (!::PostMessageW(hwnd, WM_BROWSE_DISCONNECT_FINISHED, static_cast<WPARAM>(hrSend), static_cast<LPARAM>(dwIndex)))
        {
            ExitWithLastError(hr, "Failed to send WM_BROWSE_DISCONNECT_FINISHED message");
        }
        break;

    case WM_BROWSE_BACKGROUND_STATUS_CALLBACK:
        ReleaseBackgroundStatusCallback(pBackgroundStatusCallback);
        pBackgroundStatusCallback = reinterpret_cast<BACKGROUND_STATUS_CALLBACK *>(msg->wParam);
        if (BACKGROUND_STATUS_AUTOSYNC_RUNNING == pBackgroundStatusCallback->type)
        {
            fCfgAutoSyncRunning = TRUE;
            if (!::PostMessageW(hwnd, WM_BROWSE_SYNC_FINISHED, static_cast<WPARAM>(pBackgroundStatusCallback->hrStatus), static_cast<LPARAM>(dwLocalDatabaseIndex)))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_SYNC_FINISHED message");
            }
        }
        else if (BACKGROUND_STATUS_REDETECTING_PRODUCTS == pBackgroundStatusCallback->type)
        {
            fCfgRedetectingProducts = TRUE;
        }
        else if (BACKGROUND_STATUS_REDETECT_PRODUCTS_FINISHED == pBackgroundStatusCallback->type)
        {
            fCfgRedetectingProducts = FALSE;

            // Synced something in the local DB
            if (!::PostMessageW(hwnd, WM_BROWSE_SYNC_FINISHED, static_cast<WPARAM>(pBackgroundStatusCallback->hrStatus), static_cast<LPARAM>(dwLocalDatabaseIndex)))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_SYNC_FINISHED message");
            }
        }
        else if (BACKGROUND_STATUS_SYNCING_PRODUCT == pBackgroundStatusCallback->type)
        {
            // TODO: implement something to show when auto sync starts
        }
        else if (BACKGROUND_STATUS_SYNCING_REMOTE == pBackgroundStatusCallback->type)
        {
            fCfgAutoSyncRunning = TRUE;
            dwTemp = FindRemoteDatabaseByPath(pBackgroundStatusCallback->sczString1);
            if (dwTemp != DWORD_MAX)
            {
                bdlDatabaseList.rgDatabases[dwTemp].hrInitializeResult = S_OK;
                bdlDatabaseList.rgDatabases[dwTemp].fSyncing = TRUE;
                if (!::PostMessageW(hwnd, WM_BROWSE_AUTOSYNCING_REMOTE, static_cast<WPARAM>(dwTemp), 0))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_AUTOSYNCING_REMOTE message");
                }
            }
        }
        else if ((fCfgAutoSyncRunning || FAILED(pBackgroundStatusCallback->hrStatus)) && !fCfgRedetectingProducts && BACKGROUND_STATUS_SYNC_PRODUCT_FINISHED == pBackgroundStatusCallback->type)
        {
            // Synced something in the local DB
            if (!::PostMessageW(hwnd, WM_BROWSE_SYNC_FINISHED, static_cast<WPARAM>(pBackgroundStatusCallback->hrStatus), static_cast<LPARAM>(dwLocalDatabaseIndex)))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_SYNC_FINISHED message");
            }
        }
        else if ((fCfgAutoSyncRunning || FAILED(pBackgroundStatusCallback->hrStatus)) && (BACKGROUND_STATUS_SYNC_REMOTE_FINISHED == pBackgroundStatusCallback->type))
        {
            // When remote finishes, notify UI which database changed
            dwTemp = FindRemoteDatabaseByPath(pBackgroundStatusCallback->sczString1);
            if (DWORD_MAX != dwTemp)
            {
                if (!::PostMessageW(hwnd, WM_BROWSE_SYNC_FINISHED, static_cast<WPARAM>(pBackgroundStatusCallback->hrStatus), static_cast<LPARAM>(dwTemp)))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_SYNC_FINISHED message");
                }
            }
        }
        else if (BACKGROUND_STATUS_GENERAL_ERROR == pBackgroundStatusCallback->type)
        {
            if (!::PostMessageW(hwnd, WM_BROWSE_AUTOSYNC_GENERAL_FAILURE, static_cast<WPARAM>(pBackgroundStatusCallback->hrStatus), 0))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_AUTOSYNC_GENERAL_FAILURE message");
            }
        }
        else if (BACKGROUND_STATUS_PRODUCT_ERROR == pBackgroundStatusCallback->type)
        {
            if (!::PostMessageW(hwnd, WM_BROWSE_AUTOSYNC_PRODUCT_FAILURE, static_cast<WPARAM>(pBackgroundStatusCallback->hrStatus), 0))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_AUTOSYNC_PRODUCT_FAILURE message");
            }
        }
        else if (BACKGROUND_STATUS_REMOTE_ERROR == pBackgroundStatusCallback->type)
        {
            // Some remote is disconnected
            dwTemp = FindRemoteDatabaseByPath(pBackgroundStatusCallback->sczString1);
            if (DWORD_MAX != dwTemp)
            {
                if (!::PostMessageW(hwnd, WM_BROWSE_AUTOSYNC_REMOTE_FAILURE, static_cast<WPARAM>(dwTemp), static_cast<LPARAM>(pBackgroundStatusCallback->hrStatus)))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_AUTOSYNC_REMOTE_FAILURE message");
                }
            }
        }
        else if (BACKGROUND_STATUS_REMOTE_GOOD == pBackgroundStatusCallback->type)
        {
            // Some remote is connected
            dwTemp = FindRemoteDatabaseByPath(pBackgroundStatusCallback->sczString1);
            if (DWORD_MAX != dwTemp)
            {
                if (!::PostMessageW(hwnd, WM_BROWSE_AUTOSYNC_REMOTE_GOOD, static_cast<WPARAM>(dwTemp), static_cast<LPARAM>(pBackgroundStatusCallback->hrStatus)))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_AUTOSYNC_REMOTE_GOOD message");
                }
            }
        }
        break;

    case WM_BROWSE_BACKGROUND_CONFLICTS_FOUND_CALLBACK:
        ReleaseBackgroundConflictsFoundCallback(pBackgroundConflictsFoundCallback);
        pBackgroundConflictsFoundCallback = reinterpret_cast<BACKGROUND_CONFLICTS_FOUND_CALLBACK *>(msg->wParam);
        for (DWORD i = 0; i < bdlDatabaseList.cDatabases; ++i)
        {
            if (bdlDatabaseList.rgDatabases[i].cdb == pBackgroundConflictsFoundCallback->cdHandle)
            {
                dwIndex = i;
                ::EnterCriticalSection(&bdlDatabaseList.rgDatabases[dwIndex].cs);
                fCsEntered = TRUE;
                CfgReleaseConflictProductArray(bdlDatabaseList.rgDatabases[i].pcplConflictProductList, bdlDatabaseList.rgDatabases[i].dwConflictProductCount);
                bdlDatabaseList.rgDatabases[i].pcplConflictProductList = pBackgroundConflictsFoundCallback->rgcpProduct;
                bdlDatabaseList.rgDatabases[i].dwConflictProductCount = pBackgroundConflictsFoundCallback->cProduct;
                pBackgroundConflictsFoundCallback->rgcpProduct = NULL;
                pBackgroundConflictsFoundCallback->cProduct = 0;

                if (!::PostMessageW(hwnd, WM_BROWSE_SYNC_FINISHED, static_cast<WPARAM>(S_OK), static_cast<LPARAM>(i)))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_SYNC_FINISHED message");
                }
                break;
            }
        }
        break;

    case WM_BROWSE_READ_SETTINGS:
        // Ignore failure, it will be logged within the function
        browser->ReadSettings();
        break;

    case WM_BROWSE_PERSIST_SETTINGS:
        // Ignore failure, it will be logged within the function
        browser->PersistSettings();
        break;
    }

LExit:
    if (fCsEntered)
    {
        ::LeaveCriticalSection(&bdlDatabaseList.rgDatabases[dwIndex].cs);
    }

    ReleaseDwordString(pdsDwordString);
    ReleaseQwordString(pqsQwordString);
    ReleaseStringPair(pspStringPair);
    ReleaseStringTriplet(pstStringTriplet);
    ReleaseBackgroundStatusCallback(pBackgroundStatusCallback);
    ReleaseBackgroundConflictsFoundCallback(pBackgroundConflictsFoundCallback);
    ReleaseStr(sczTemp);
    ReleaseMem(pbData);
    // Something really serious happened - error out and wait for UI thread to exit
    if (FAILED(hr))
    {
        CfgReleaseEnumeration(cehHandle);
        return FALSE;
    }

    return TRUE;
}

static HRESULT CheckProductInstalledState(
    __in CFGDB_HANDLE pcdLocalHandle,
    __in C_CFG_ENUMERATION_HANDLE cehProducts,
    __in DWORD dwProductCount,
    __deref_out_ecount_opt(dwProductCount) BOOL **prgfInstalled
    )
{
    HRESULT hr = S_OK;
    DWORD cbAllocSize = 0;
    LPCWSTR wzProductName = NULL;
    LPCWSTR wzVersion = NULL;
    LPCWSTR wzPublicKey = NULL;

    if (0 == dwProductCount)
    {
        ReleaseNullMem(*prgfInstalled);
        ExitFunction1(hr = S_OK);
    }
    else
    {
        hr = ::DWordMult(dwProductCount, static_cast<DWORD>(sizeof(BOOL)), &cbAllocSize);
        ExitOnFailure(hr, "Failed to calculate size of boolean array to allocate to remember installed product state");

        if (NULL == *prgfInstalled)
        {
            *prgfInstalled = static_cast<BOOL *>(MemAlloc(cbAllocSize, TRUE));
            ExitOnNull(*prgfInstalled, hr, E_OUTOFMEMORY, "Failed to allocate memory for boolean array to remember installed product state");
        }
        else
        {
            *prgfInstalled = static_cast<BOOL *>(MemReAlloc(static_cast<void *>(*prgfInstalled), cbAllocSize, TRUE));
            ExitOnNull(*prgfInstalled, hr, E_OUTOFMEMORY, "Failed to allocate memory for boolean array to remember installed product state");
        }
    }

    for (DWORD i = 0; i < dwProductCount; ++i)
    {
        hr = CfgEnumReadString(cehProducts, i, ENUM_DATA_PRODUCTNAME, &wzProductName);
        ExitOnFailure(hr, "Failed to read product name from enum while checking installed state");

        hr = CfgEnumReadString(cehProducts, i, ENUM_DATA_VERSION, &wzVersion);
        ExitOnFailure(hr, "Failed to read product version from enum while checking installed state");

        hr = CfgEnumReadString(cehProducts, i, ENUM_DATA_PUBLICKEY, &wzPublicKey);
        ExitOnFailure(hr, "Failed to read product public key from enum while checking installed state");

        if (NULL == wzPublicKey)
        {
            wzPublicKey = L"0000000000000000";
        }

        hr = CfgIsProductRegistered(pcdLocalHandle, wzProductName, wzVersion, wzPublicKey, *prgfInstalled + i);
        ExitOnFailure(hr, "Failed to check if product is registered: %ls, %ls, %ls", wzProductName, wzVersion, (NULL == wzPublicKey) ? L"NULL" : wzPublicKey);
    }

LExit:
    return hr;
}

static HRESULT CheckSingleInstance(
    __in const COMMANDLINE_REQUEST & commandLineRequest,
    __out HANDLE *phLockFile
    )
{
    HRESULT hr = S_OK;
    HANDLE hLockFile = INVALID_HANDLE_VALUE;
    HWND hwndMainBrowser = NULL;
    LPWSTR sczFolderPath = NULL;
    LPWSTR sczLockFilePath = NULL;
    BOOL fRet = FALSE;
    DWORD dwRetryCount = 200;
    const DWORD dwSleepAmount = 100;

    hr = PathGetKnownFolder(CSIDL_LOCAL_APPDATA, &sczFolderPath);
    ExitOnFailure(hr, "Failed to get local app data folder");

    hr = PathConcat(sczFolderPath, L"WixSettingsBrowser.lock", &sczLockFilePath);
    ExitOnFailure(hr, "Failed to get path to lock file");

    do
    {
        // Try to delete just in case somehow the lock file exists with no owner. Ignore failures.
        FileEnsureDelete(sczLockFilePath);

        hLockFile = ::CreateFileW(sczLockFilePath, GENERIC_READ, 0, NULL, CREATE_ALWAYS, FILE_FLAG_DELETE_ON_CLOSE, NULL);
        if (INVALID_HANDLE_VALUE != hLockFile)
        {
            // We created the lock file and own it now, so exit out, sending it to the caller
            *phLockFile = hLockFile;
            ExitFunction1(hr = S_OK);
        }

        // The lock file may already exist, so see if we can find the existing window and give it focus
        hwndMainBrowser = ::FindWindowW(BROWSE_WINDOW_CLASS, NULL);
        if (NULL != hwndMainBrowser)
        {
            // We found a main window! Set it to the foreground
            fRet = ::SetForegroundWindow(hwndMainBrowser);
            if (fRet)
            {
                // Send any commandline arguments
                for (DWORD i = 0; i < commandLineRequest.cLegacyManifests; ++i)
                {
                    COPYDATASTRUCT cds = { };
                    cds.dwData = INTER_PROCESS_PROTOCOL_VERSION;
                    cds.cbData = (lstrlenW(commandLineRequest.rgsczLegacyManifests[i]) + 1) * sizeof(WCHAR);
                    cds.lpData = (LPVOID)commandLineRequest.rgsczLegacyManifests[i];
                    fRet = ::SendMessageW(hwndMainBrowser, WM_COPYDATA, NULL, (LPARAM)(LPVOID)&cds);
                    if (!fRet)
                    {
                        Trace(REPORT_STANDARD, "Main browser window seems to have ignored our message, manifest import request may have been lost.");
                    }
                }

                ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS));
            }
        }

        dwRetryCount--;
        ::Sleep(dwSleepAmount);
    } while (dwRetryCount > 0);

    // If we got here, it means we ran out of retries, but there is a settings browser running (which we couldn't interact with)
    hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
    ExitOnFailure(hr, "There was an existing settings browser process detected, but a window for it was not found.");

LExit:
    ReleaseStr(sczFolderPath);
    ReleaseStr(sczLockFilePath);

    return hr;
}

static void BackgroundStatusCallback(
    __in HRESULT hrStatus,
    __in BACKGROUND_STATUS_TYPE type,
    __in_z LPCWSTR wzString1,
    __in_z LPCWSTR wzString2,
    __in_z LPCWSTR wzString3,
    __in LPVOID pvContext
    )
{
    if (FAILED(hrStatus))
    {
        LogErrorString(hrStatus, "Received error of type %u, with string %ls, %ls, %ls from BackgroundStatusCallback", type, wzString1, wzString2, wzString3);
    }

    HRESULT hr = SendBackgroundStatusCallback(reinterpret_cast<DWORD>(pvContext), hrStatus, type, wzString1, wzString2, wzString3);
    ExitOnFailure(hr, "Failed to send background status callback");

LExit:
    return;
}

static void BackgroundConflictsFoundCallback(
    __in CFGDB_HANDLE cdHandle,
    __in CONFLICT_PRODUCT *rgcpProduct,
    __in DWORD cProduct,
    __in LPVOID pvContext
    )
{
    HRESULT hr = SendBackgroundConflictsFoundCallback(reinterpret_cast<DWORD>(pvContext), cdHandle, rgcpProduct, cProduct);
    ExitOnFailure(hr, "Failed to send background conflicts found callback");

LExit:
    return;
}

static DWORD FindRemoteDatabaseByPath(
    __in LPCWSTR wzPath
    )
{
    for (DWORD i = 0; i < bdlDatabaseList.cDatabases; ++i)
    {
        if (NULL != bdlDatabaseList.rgDatabases[i].sczPath && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, bdlDatabaseList.rgDatabases[i].sczPath, -1, wzPath, -1))
        {
            return i;
        }
    }

    LogStringLine(REPORT_STANDARD, "Couldn't find remote database with path %ls", wzPath);

    return DWORD_MAX;
}
