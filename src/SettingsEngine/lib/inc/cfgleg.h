#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
