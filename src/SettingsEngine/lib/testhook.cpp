//-------------------------------------------------------------------------------------------------
// <copyright file="testhook.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for tests to hook into
// </summary>
//-------------------------------------------------------------------------------------------------

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
