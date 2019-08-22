// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

extern "C" HRESULT TestHookOverrideUserDatabasePath(
    __in_z LPCWSTR wzNewUserDatabasePath
    )
{
    if (NULL != wzNewUserDatabasePath)
    {
        wzUserDatabasePath = wzNewUserDatabasePath;
    }
    else
    {
        wzUserDatabasePath = NULL;
    }

    return S_OK;
}

extern "C" HRESULT TestHookOverrideAdminDatabasePath(
    __in_z LPCWSTR wzNewAdminDatabasePath
    )
{
    if (NULL != wzNewAdminDatabasePath)
    {
        wzAdminDatabasePath = wzNewAdminDatabasePath;
    }
    else
    {
        wzAdminDatabasePath = NULL;
    }

    return S_OK;
}

extern "C" HRESULT TestHookOverrideArpPath(
    __in_z LPCWSTR wzNewArpPath
    )
{
    if (NULL != wzNewArpPath)
    {
        wzArpPath = wzNewArpPath;
    }
    else
    {
        wzArpPath = DEFAULT_ARP_PATH;
    }

    return S_OK;
}

extern "C" HRESULT TestHookOverrideApplicationsPath(
    __in_z LPCWSTR wzNewApplicationsPath
    )
{
    if (NULL != wzNewApplicationsPath)
    {
        wzApplicationsPath = wzNewApplicationsPath;
    }
    else
    {
        wzApplicationsPath = DEFAULT_APPLICATIONS_PATH;
    }

    return S_OK;
}

extern "C" HRESULT TestHookOverrideGetSystemTime(
    __in PFN_GETSYSTEMTIME systemTimeGetter
    )
{
    SystemTimeGetter = systemTimeGetter;

    return S_OK;
}
