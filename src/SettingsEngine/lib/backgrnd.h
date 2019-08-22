#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT BackgroundStartThread(
    __inout CFGDB_STRUCT *pcdb
    );
HRESULT BackgroundStopThread(
    __inout CFGDB_STRUCT *pcdb
    );
HRESULT BackgroundUpdateProduct(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzProductId
    );
HRESULT BackgroundMarkRemoteChanged(
    __in CFGDB_STRUCT *pcdbRemote
    );
HRESULT BackgroundSyncRemotes(
    __in CFGDB_STRUCT *pcdb
    );
HRESULT BackgroundRemoveProduct(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzProductId
    );
HRESULT BackgroundAddRemote(
    __in CFGDB_STRUCT *pcdbLocal,
    __in LPCWSTR wzPath
    );
HRESULT BackgroundRemoveRemote(
    __in CFGDB_STRUCT *pcdbLocal,
    __in LPCWSTR wzPath
    );

#ifdef __cplusplus
}
#endif
