//-------------------------------------------------------------------------------------------------
// <copyright file="BootstrapperEngine.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

#define IDERROR -1
#define IDNOACTION 0

#define IDDOWNLOAD 101 // Only valid as a return code from OnResolveSource() to instruct the engine to use the download source.
#define IDRESTART  102
#define IDSUSPEND  103
#define IDRELOAD_BOOTSTRAPPER 104

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
};


extern "C" typedef HRESULT(WINAPI *PFN_BOOTSTRAPPER_ENGINE_PROC)(
    __in LPVOID pvContext,
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __in LPVOID pvResults
    );
