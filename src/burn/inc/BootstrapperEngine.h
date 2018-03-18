#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

#define IDERROR -1
#define IDNOACTION 0

// Note that ordering of the enumeration values is important.
// Some code paths use < or > comparisions and simply reording values will break those comparisons.
enum BOOTSTRAPPER_ACTION
{
    BOOTSTRAPPER_ACTION_UNKNOWN,
    BOOTSTRAPPER_ACTION_HELP,
    BOOTSTRAPPER_ACTION_LAYOUT,
    BOOTSTRAPPER_ACTION_UNINSTALL,
    BOOTSTRAPPER_ACTION_CACHE,
    BOOTSTRAPPER_ACTION_INSTALL,
    BOOTSTRAPPER_ACTION_MODIFY,
    BOOTSTRAPPER_ACTION_REPAIR,
    BOOTSTRAPPER_ACTION_UPDATE_REPLACE,
    BOOTSTRAPPER_ACTION_UPDATE_REPLACE_EMBEDDED,
};

enum BOOTSTRAPPER_ACTION_STATE
{
    BOOTSTRAPPER_ACTION_STATE_NONE,
    BOOTSTRAPPER_ACTION_STATE_UNINSTALL,
    BOOTSTRAPPER_ACTION_STATE_INSTALL,
    BOOTSTRAPPER_ACTION_STATE_ADMIN_INSTALL,
    BOOTSTRAPPER_ACTION_STATE_MODIFY,
    BOOTSTRAPPER_ACTION_STATE_REPAIR,
    BOOTSTRAPPER_ACTION_STATE_MINOR_UPGRADE,
    BOOTSTRAPPER_ACTION_STATE_MAJOR_UPGRADE,
    BOOTSTRAPPER_ACTION_STATE_PATCH,
};

enum BOOTSTRAPPER_PACKAGE_STATE
{
    BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN,
    BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE,
    BOOTSTRAPPER_PACKAGE_STATE_ABSENT,
    BOOTSTRAPPER_PACKAGE_STATE_CACHED,
    BOOTSTRAPPER_PACKAGE_STATE_PRESENT,
    BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED,
};

enum BOOTSTRAPPER_REQUEST_STATE
{
    BOOTSTRAPPER_REQUEST_STATE_NONE,
    BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT,
    BOOTSTRAPPER_REQUEST_STATE_ABSENT,
    BOOTSTRAPPER_REQUEST_STATE_CACHE,
    BOOTSTRAPPER_REQUEST_STATE_PRESENT,
    BOOTSTRAPPER_REQUEST_STATE_REPAIR,
};

enum BOOTSTRAPPER_FEATURE_STATE
{
    BOOTSTRAPPER_FEATURE_STATE_UNKNOWN,
    BOOTSTRAPPER_FEATURE_STATE_ABSENT,
    BOOTSTRAPPER_FEATURE_STATE_ADVERTISED,
    BOOTSTRAPPER_FEATURE_STATE_LOCAL,
    BOOTSTRAPPER_FEATURE_STATE_SOURCE,
};

enum BOOTSTRAPPER_FEATURE_ACTION
{
    BOOTSTRAPPER_FEATURE_ACTION_NONE,
    BOOTSTRAPPER_FEATURE_ACTION_ADDLOCAL,
    BOOTSTRAPPER_FEATURE_ACTION_ADDSOURCE,
    BOOTSTRAPPER_FEATURE_ACTION_ADDDEFAULT,
    BOOTSTRAPPER_FEATURE_ACTION_REINSTALL,
    BOOTSTRAPPER_FEATURE_ACTION_ADVERTISE,
    BOOTSTRAPPER_FEATURE_ACTION_REMOVE,
};

enum BOOTSTRAPPER_LOG_LEVEL
{
    BOOTSTRAPPER_LOG_LEVEL_NONE,      // turns off report (only valid for XXXSetLevel())
    BOOTSTRAPPER_LOG_LEVEL_STANDARD,  // written if reporting is on
    BOOTSTRAPPER_LOG_LEVEL_VERBOSE,   // written only if verbose reporting is on
    BOOTSTRAPPER_LOG_LEVEL_DEBUG,     // reporting useful when debugging code
    BOOTSTRAPPER_LOG_LEVEL_ERROR,     // always gets reported, but can never be specified
};

enum BOOTSTRAPPER_UPDATE_HASH_TYPE
{
    BOOTSTRAPPER_UPDATE_HASH_TYPE_NONE,
    BOOTSTRAPPER_UPDATE_HASH_TYPE_SHA1,
};

enum BOOTSTRAPPER_ENGINE_MESSAGE
{
    BOOTSTRAPPER_ENGINE_MESSAGE_GETPACKAGECOUNT,
    BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLENUMERIC,
    BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLESTRING,
    BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLEVERSION,
    BOOTSTRAPPER_ENGINE_MESSAGE_FORMATSTRING,
    BOOTSTRAPPER_ENGINE_MESSAGE_ESCAPESTRING,
    BOOTSTRAPPER_ENGINE_MESSAGE_EVALUATECONDITION,
    BOOTSTRAPPER_ENGINE_MESSAGE_LOG,
    BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDERROR,
    BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDPROGRESS,
    BOOTSTRAPPER_ENGINE_MESSAGE_SETUPDATE,
    BOOTSTRAPPER_ENGINE_MESSAGE_SETLOCALSOURCE,
    BOOTSTRAPPER_ENGINE_MESSAGE_SETDOWNLOADSOURCE,
    BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLENUMERIC,
    BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLESTRING,
    BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLEVERSION,
    BOOTSTRAPPER_ENGINE_MESSAGE_CLOSESPLASHSCREEN,
    BOOTSTRAPPER_ENGINE_MESSAGE_DETECT,
    BOOTSTRAPPER_ENGINE_MESSAGE_PLAN,
    BOOTSTRAPPER_ENGINE_MESSAGE_ELEVATE,
    BOOTSTRAPPER_ENGINE_MESSAGE_APPLY,
    BOOTSTRAPPER_ENGINE_MESSAGE_QUIT,
    BOOTSTRAPPER_ENGINE_MESSAGE_LAUNCHAPPROVEDEXE,
};

typedef struct _BAENGINE_APPLY_ARGS
{
    DWORD cbSize;
    HWND hwndParent;
} BAENGINE_APPLY_ARGS;

typedef struct _BAENGINE_APPLY_RESULTS
{
    DWORD cbSize;
} BAENGINE_APPLY_RESULTS;

typedef struct _BAENGINE_CLOSESPLASHSCREEN_ARGS
{
    DWORD cbSize;
} BAENGINE_CLOSESPLASHSCREEN_ARGS;

typedef struct _BAENGINE_CLOSESPLASHSCREEN_RESULTS
{
    DWORD cbSize;
} BAENGINE_CLOSESPLASHSCREEN_RESULTS;

typedef struct _BAENGINE_DETECT_ARGS
{
    DWORD cbSize;
    HWND hwndParent;
} BAENGINE_DETECT_ARGS;

typedef struct _BAENGINE_DETECT_RESULTS
{
    DWORD cbSize;
} BAENGINE_DETECT_RESULTS;

typedef struct _BAENGINE_ELEVATE_ARGS
{
    DWORD cbSize;
    HWND hwndParent;
} BAENGINE_ELEVATE_ARGS;

typedef struct _BAENGINE_ELEVATE_RESULTS
{
    DWORD cbSize;
} BAENGINE_ELEVATE_RESULTS;

typedef struct _BAENGINE_ESCAPESTRING_ARGS
{
    DWORD cbSize;
    LPCWSTR wzIn;
} BAENGINE_ESCAPESTRING_ARGS;

typedef struct _BAENGINE_ESCAPESTRING_RESULTS
{
    DWORD cbSize;
    LPWSTR wzOut;
    // Should be initialized to the size of wzOut.
    DWORD cchOut;
} BAENGINE_ESCAPESTRING_RESULTS;

typedef struct _BAENGINE_EVALUATECONDITION_ARGS
{
    DWORD cbSize;
    LPCWSTR wzCondition;
} BAENGINE_EVALUATECONDITION_ARGS;

typedef struct _BAENGINE_EVALUATECONDITION_RESULTS
{
    DWORD cbSize;
    BOOL f;
} BAENGINE_EVALUATECONDITION_RESULTS;

typedef struct _BAENGINE_FORMATSTRING_ARGS
{
    DWORD cbSize;
    LPCWSTR wzIn;
} BAENGINE_FORMATSTRING_ARGS;

typedef struct _BAENGINE_FORMATSTRING_RESULTS
{
    DWORD cbSize;
    // The contents of wzOut may be sensitive, should keep encrypted and SecureZeroFree.
    LPWSTR wzOut;
    // Should be initialized to the size of wzOut.
    DWORD cchOut;
} BAENGINE_FORMATSTRING_RESULTS;

typedef struct _BAENGINE_GETPACKAGECOUNT_ARGS
{
    DWORD cbSize;
} BAENGINE_GETPACKAGECOUNT_ARGS;

typedef struct _BAENGINE_GETPACKAGECOUNT_RESULTS
{
    DWORD cbSize;
    DWORD cPackages;
} BAENGINE_GETPACKAGECOUNT_RESULTS;

typedef struct _BAENGINE_GETVARIABLENUMERIC_ARGS
{
    DWORD cbSize;
    LPCWSTR wzVariable;
} BAENGINE_GETVARIABLENUMERIC_ARGS;

typedef struct _BAENGINE_GETVARIABLENUMERIC_RESULTS
{
    DWORD cbSize;
    // The contents of llValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroMemory.
    LONGLONG llValue;
} BAENGINE_GETVARIABLENUMERIC_RESULTS;

typedef struct _BAENGINE_GETVARIABLESTRING_ARGS
{
    DWORD cbSize;
    LPCWSTR wzVariable;
} BAENGINE_GETVARIABLESTRING_ARGS;

typedef struct _BAENGINE_GETVARIABLESTRING_RESULTS
{
    DWORD cbSize;
    // The contents of wzValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroFree.
    LPWSTR wzValue;
    // Should be initialized to the size of wzValue.
    DWORD cchValue;
} BAENGINE_GETVARIABLESTRING_RESULTS;

typedef struct _BAENGINE_GETVARIABLEVERSION_ARGS
{
    DWORD cbSize;
    LPCWSTR wzVariable;
} BAENGINE_GETVARIABLEVERSION_ARGS;

typedef struct _BAENGINE_GETVARIABLEVERSION_RESULTS
{
    DWORD cbSize;
    // The contents of qwValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroMemory.
    DWORD64 qwValue;
} BAENGINE_GETVARIABLEVERSION_RESULTS;

typedef struct _BAENGINE_LAUNCHAPPROVEDEXE_ARGS
{
    DWORD cbSize;
    HWND hwndParent;
    LPCWSTR wzApprovedExeForElevationId;
    LPCWSTR wzArguments;
    DWORD dwWaitForInputIdleTimeout;
} BAENGINE_LAUNCHAPPROVEDEXE_ARGS;

typedef struct _BAENGINE_LAUNCHAPPROVEDEXE_RESULTS
{
    DWORD cbSize;
} BAENGINE_LAUNCHAPPROVEDEXE_RESULTS;

typedef struct _BAENGINE_LOG_ARGS
{
    DWORD cbSize;
    BOOTSTRAPPER_LOG_LEVEL level;
    LPCWSTR wzMessage;
} BAENGINE_LOG_ARGS;

typedef struct _BAENGINE_LOG_RESULTS
{
    DWORD cbSize;
} BAENGINE_LOG_RESULTS;

typedef struct _BAENGINE_PLAN_ARGS
{
    DWORD cbSize;
    BOOTSTRAPPER_ACTION action;
} BAENGINE_PLAN_ARGS;

typedef struct _BAENGINE_PLAN_RESULTS
{
    DWORD cbSize;
} BAENGINE_PLAN_RESULTS;

typedef struct _BAENGINE_QUIT_ARGS
{
    DWORD cbSize;
    DWORD dwExitCode;
} BAENGINE_QUIT_ARGS;

typedef struct _BAENGINE_QUIT_RESULTS
{
    DWORD cbSize;
} BAENGINE_QUIT_RESULTS;

typedef struct _BAENGINE_SENDEMBEDDEDERROR_ARGS
{
    DWORD cbSize;
    DWORD dwErrorCode;
    LPCWSTR wzMessage;
    DWORD dwUIHint;
} BAENGINE_SENDEMBEDDEDERROR_ARGS;

typedef struct _BAENGINE_SENDEMBEDDEDERROR_RESULTS
{
    DWORD cbSize;
    int nResult;
} BAENGINE_SENDEMBEDDEDERROR_RESULTS;

typedef struct _BAENGINE_SENDEMBEDDEDPROGRESS_ARGS
{
    DWORD cbSize;
    DWORD dwProgressPercentage;
    DWORD dwOverallProgressPercentage;
} BAENGINE_SENDEMBEDDEDPROGRESS_ARGS;

typedef struct _BAENGINE_SENDEMBEDDEDPROGRESS_RESULTS
{
    DWORD cbSize;
    int nResult;
} BAENGINE_SENDEMBEDDEDPROGRESS_RESULTS;

typedef struct _BAENGINE_SETDOWNLOADSOURCE_ARGS
{
    DWORD cbSize;
    LPCWSTR wzPackageOrContainerId;
    LPCWSTR wzPayloadId;
    LPCWSTR wzUrl;
    LPCWSTR wzUser;
    LPCWSTR wzPassword;
} BAENGINE_SETDOWNLOADSOURCE_ARGS;

typedef struct _BAENGINE_SETDOWNLOADSOURCE_RESULTS
{
    DWORD cbSize;
} BAENGINE_SETDOWNLOADSOURCE_RESULTS;

typedef struct _BAENGINE_SETLOCALSOURCE_ARGS
{
    DWORD cbSize;
    LPCWSTR wzPackageOrContainerId;
    LPCWSTR wzPayloadId;
    LPCWSTR wzPath;
} BAENGINE_SETLOCALSOURCE_ARGS;

typedef struct _BAENGINE_SETLOCALSOURCE_RESULTS
{
    DWORD cbSize;
} BAENGINE_SETLOCALSOURCE_RESULTS;

typedef struct _BAENGINE_SETUPDATE_ARGS
{
    DWORD cbSize;
    LPCWSTR wzLocalSource;
    LPCWSTR wzDownloadSource;
    DWORD64 qwSize;
    BOOTSTRAPPER_UPDATE_HASH_TYPE hashType;
    BYTE* rgbHash;
    DWORD cbHash;
} BAENGINE_SETUPDATE_ARGS;

typedef struct _BAENGINE_SETUPDATE_RESULTS
{
    DWORD cbSize;
} BAENGINE_SETUPDATE_RESULTS;

typedef struct _BAENGINE_SETVARIABLENUMERIC_ARGS
{
    DWORD cbSize;
    LPCWSTR wzVariable;
    LONGLONG llValue;
} BAENGINE_SETVARIABLENUMERIC_ARGS;

typedef struct _BAENGINE_SETVARIABLENUMERIC_RESULTS
{
    DWORD cbSize;
} BAENGINE_SETVARIABLENUMERIC_RESULTS;

typedef struct _BAENGINE_SETVARIABLESTRING_ARGS
{
    DWORD cbSize;
    LPCWSTR wzVariable;
    LPCWSTR wzValue;
} BAENGINE_SETVARIABLESTRING_ARGS;

typedef struct _BAENGINE_SETVARIABLESTRING_RESULTS
{
    DWORD cbSize;
} BAENGINE_SETVARIABLESTRING_RESULTS;

typedef struct _BAENGINE_SETVARIABLEVERSION_ARGS
{
    DWORD cbSize;
    LPCWSTR wzVariable;
    DWORD64 qwValue;
} BAENGINE_SETVARIABLEVERSION_ARGS;

typedef struct _BAENGINE_SETVARIABLEVERSION_RESULTS
{
    DWORD cbSize;
} BAENGINE_SETVARIABLEVERSION_RESULTS;


extern "C" typedef HRESULT(WINAPI *PFN_BOOTSTRAPPER_ENGINE_PROC)(
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    );

#if defined(__cplusplus)
}
#endif
