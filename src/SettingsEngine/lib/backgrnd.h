//-------------------------------------------------------------------------------------------------
// <copyright file="backgrnd.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Functions for managing the background thread of the settings engine
// The background thread manages automatically syncing products whose settings have changed
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
