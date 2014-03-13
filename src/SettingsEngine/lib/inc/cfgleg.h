//-------------------------------------------------------------------------------------------------
// <copyright file="cfgleg.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Legacy settings engine API (these functions are NOT for legacy Apps -
//           they are for apps that want to help manage user data for legacy apps)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

// Like the old legacy sync, except there are no conflicts, we just suck in everything from the local machine
HRESULT CFGAPI CfgLegacyReadLatest(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    );

HRESULT CFGAPI CfgLegacyImportProductFromXMLFile(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzXmlFilePath
    );

#ifdef __cplusplus
}
#endif
