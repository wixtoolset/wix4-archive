// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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

void UtilWipeEnum(
    __in BROWSE_DATABASE *pDatabase,
    __inout BROWSE_ENUM *pEnum
    )
{
    ::EnterCriticalSection(&pDatabase->cs);

    CfgReleaseEnumeration(pEnum->cehItems);
    pEnum->cehItems = NULL;
    pEnum->cItems = 0;

    ::LeaveCriticalSection(&pDatabase->cs);
}
