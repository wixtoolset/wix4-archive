//-------------------------------------------------------------------------------------------------
// <copyright file="BootstrapperApplication.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

enum BOOTSTRAPPER_DISPLAY
{
    BOOTSTRAPPER_DISPLAY_UNKNOWN,
    BOOTSTRAPPER_DISPLAY_EMBEDDED,
    BOOTSTRAPPER_DISPLAY_NONE,
    BOOTSTRAPPER_DISPLAY_PASSIVE,
    BOOTSTRAPPER_DISPLAY_FULL,
};

enum BOOTSTRAPPER_RESTART
{
    BOOTSTRAPPER_RESTART_UNKNOWN,
    BOOTSTRAPPER_RESTART_NEVER,
    BOOTSTRAPPER_RESTART_PROMPT,
    BOOTSTRAPPER_RESTART_AUTOMATIC,
    BOOTSTRAPPER_RESTART_ALWAYS,
};

enum BOOTSTRAPPER_RESUME_TYPE
{
    BOOTSTRAPPER_RESUME_TYPE_NONE,
    BOOTSTRAPPER_RESUME_TYPE_INVALID,        // resume information is present but invalid
    BOOTSTRAPPER_RESUME_TYPE_INTERRUPTED,    // relaunched after an unexpected interruption
    BOOTSTRAPPER_RESUME_TYPE_REBOOT_PENDING, // reboot has not taken place yet
    BOOTSTRAPPER_RESUME_TYPE_REBOOT,         // relaunched after reboot
    BOOTSTRAPPER_RESUME_TYPE_SUSPEND,        // relaunched after suspend
    BOOTSTRAPPER_RESUME_TYPE_ARP,            // launched from ARP
};

enum BOOTSTRAPPER_ERROR_TYPE
{
    BOOTSTRAPPER_ERROR_TYPE_ELEVATE,            // error occurred trying to elevate.
    BOOTSTRAPPER_ERROR_TYPE_WINDOWS_INSTALLER,  // error came from windows installer.
    BOOTSTRAPPER_ERROR_TYPE_EXE_PACKAGE,        // error came from an exe package.
    BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_SERVER,   // error occurred trying to authenticate with HTTP server.
    BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_PROXY,    // error occurred trying to authenticate with HTTP proxy.
    BOOTSTRAPPER_ERROR_TYPE_APPLY,              // error occurred during apply.
};

enum BOOTSTRAPPER_RELATED_OPERATION
{
    BOOTSTRAPPER_RELATED_OPERATION_NONE,
    BOOTSTRAPPER_RELATED_OPERATION_DOWNGRADE,
    BOOTSTRAPPER_RELATED_OPERATION_MINOR_UPDATE,
    BOOTSTRAPPER_RELATED_OPERATION_MAJOR_UPGRADE,
    BOOTSTRAPPER_RELATED_OPERATION_REMOVE,
    BOOTSTRAPPER_RELATED_OPERATION_INSTALL,
    BOOTSTRAPPER_RELATED_OPERATION_REPAIR,
};

enum BOOTSTRAPPER_CACHE_OPERATION
{
    BOOTSTRAPPER_CACHE_OPERATION_COPY,
    BOOTSTRAPPER_CACHE_OPERATION_DOWNLOAD,
    BOOTSTRAPPER_CACHE_OPERATION_EXTRACT,
};

enum BOOTSTRAPPER_APPLY_RESTART
{
    BOOTSTRAPPER_APPLY_RESTART_NONE,
    BOOTSTRAPPER_APPLY_RESTART_REQUIRED,
    BOOTSTRAPPER_APPLY_RESTART_INITIATED,
};

enum BOOTSTRAPPER_RELATION_TYPE
{
    BOOTSTRAPPER_RELATION_NONE,
    BOOTSTRAPPER_RELATION_DETECT,
    BOOTSTRAPPER_RELATION_UPGRADE,
    BOOTSTRAPPER_RELATION_ADDON,
    BOOTSTRAPPER_RELATION_PATCH,
    BOOTSTRAPPER_RELATION_DEPENDENT,
    BOOTSTRAPPER_RELATION_UPDATE,
};

enum BOOTSTRAPPER_APPLICATION_MESSAGE
{
    BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN,
    BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE,
    BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN,
    BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE,
};

struct BOOTSTRAPPER_COMMAND
{
    BOOTSTRAPPER_ACTION action;
    BOOTSTRAPPER_DISPLAY display;
    BOOTSTRAPPER_RESTART restart;

    LPWSTR wzCommandLine;
    int nCmdShow;

    BOOTSTRAPPER_RESUME_TYPE resumeType;
    HWND hwndSplashScreen;

    // If this was run from a related bundle, specifies the relation type
    BOOTSTRAPPER_RELATION_TYPE relationType;
    BOOL fPassthrough;

    LPWSTR wzLayoutDirectory;
};

struct BA_ONDETECTBEGIN_ARGS
{
    DWORD cbSize;
    BOOL fInstalled;
    DWORD cPackages;
};

struct BA_ONDETECTBEGIN_RESULTS
{
    DWORD cbSize;
    BOOL fCancel;
};

struct BA_ONDETECTCOMPLETE_ARGS
{
    DWORD cbSize;
    HRESULT hrStatus;
};

struct BA_ONDETECTCOMPLETE_RESULTS
{
    DWORD cbSize;
};

struct BA_ONPLANBEGIN_ARGS
{
    DWORD cbSize;
    DWORD cPackages;
};

struct BA_ONPLANBEGIN_RESULTS
{
    DWORD cbSize;
    BOOL fCancel;
};

struct BA_ONPLANCOMPLETE_ARGS
{
    DWORD cbSize;
    HRESULT hrStatus;
};

struct BA_ONPLANCOMPLETE_RESULTS
{
    DWORD cbSize;
};


extern "C" typedef HRESULT(WINAPI *PFN_BOOTSTRAPPER_APPLICATION_PROC)(
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    );

extern "C" typedef void (WINAPI *PFN_BOOTSTRAPPER_APPLICATION_DESTROY)();
