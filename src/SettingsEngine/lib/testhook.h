//-------------------------------------------------------------------------------------------------
// <copyright file="testhook.h" company="Outercurve Foundation">
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

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

HRESULT __stdcall TestHookOverrideUserDatabasePath(
    __in_z LPCWSTR wzNewUserDatabasePath
    );
HRESULT __stdcall TestHookOverrideAdminDatabasePath(
    __in_z LPCWSTR wzNewAdminDatabasePath
    );
HRESULT __stdcall TestHookOverrideArpPath(
    __in_z LPCWSTR wzNewArpPath
    );
HRESULT __stdcall TestHookOverrideApplicationsPath(
    __in_z LPCWSTR wzNewApplicationsPath
    );

#ifdef __cplusplus
}
#endif
