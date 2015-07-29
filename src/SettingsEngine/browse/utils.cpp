//-------------------------------------------------------------------------------------------------
// <copyright file="utils.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Internal utility functions for Browser App
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

void UtilFreeDatabase(
    BROWSE_DATABASE *pDatabase
    )
{
    ::DeleteCriticalSection(&pDatabase->cs);

    ReleaseStr(pDatabase->prodCurrent.sczName);
    ReleaseStr(pDatabase->prodCurrent.sczVersion);
    ReleaseStr(pDatabase->prodCurrent.sczPublicKey);

    ReleaseStr(pDatabase->sczPath);
    ReleaseStr(pDatabase->sczName);
    ReleaseStr(pDatabase->sczStatusMessage);
    ReleaseStr(pDatabase->sczCurrentProductDisplayName);
    ReleaseStr(pDatabase->sczValueName);
    CfgReleaseEnumeration(pDatabase->dbEnum.cehItems);
    CfgReleaseEnumeration(pDatabase->productEnum.cehItems);
    CfgReleaseEnumeration(pDatabase->valueEnum.cehItems);
    CfgReleaseEnumeration(pDatabase->valueHistoryEnum.cehItems);
    CfgReleaseConflictProductArray(pDatabase->pcplConflictProductList, pDatabase->dwConflictProductCount);
}

HRESULT UtilGrowDatabaseList(
    __inout BROWSE_DATABASE_LIST *pbdlDatabaseList,
    __out DWORD *pdwNewIndex
    )
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&pbdlDatabaseList->cs);

    // TODO: make UI thread appropriately enter database list critical section. For now, grow this array by 10 at a time
    hr = MemEnsureArraySize(reinterpret_cast<void **>(&(pbdlDatabaseList->rgDatabases)), pbdlDatabaseList->cDatabases + 1, sizeof(BROWSE_DATABASE), 10);
    ExitOnFailure(hr, "Failed to allocate space for one more database struct");

    *pdwNewIndex = pbdlDatabaseList->cDatabases;
    ++(pbdlDatabaseList->cDatabases);

    ::InitializeCriticalSection(&pbdlDatabaseList->rgDatabases[pbdlDatabaseList->cDatabases-1].cs);

    ::LeaveCriticalSection(&pbdlDatabaseList->cs);

LExit:
    return hr;
}

BOOL UtilReadyToSync(
    __in BROWSE_DATABASE *pbdDatabase
    )
{
    if (!pbdDatabase->fInitialized)
    {
        return FALSE;
    }

    return TRUE;
}
