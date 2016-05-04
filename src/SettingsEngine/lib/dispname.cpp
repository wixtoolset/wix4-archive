// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

void DisplayNameArrayFree(
    __in DISPLAY_NAME *rgDisplayNames,
    __in DWORD cDisplayNames
    )
{
    for (DWORD i = 0; i < cDisplayNames; ++i)
    {
        ReleaseDisplayName(rgDisplayNames[i]);
    }

    ReleaseMem(rgDisplayNames);
}

HRESULT DisplayNameLookup(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in DWORD dwLCID,
    __out LPWSTR *psczDisplayName
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    SCE_QUERY_HANDLE query = NULL;
    SCE_ROW_HANDLE row = NULL;

    hr = SceBeginQuery(pcdb->psceDb, PRODUCT_DISPLAY_NAME_TABLE, 1, &query);
    ExitOnFailure(hr, "Failed to begin query into display name table");

    hr = SceSetQueryColumnDword(query, dwAppID);
    ExitOnFailure(hr, "Failed to set query column to appid");

    hr = SceSetQueryColumnDword(query, dwLCID);
    ExitOnFailure(hr, "Failed to set query column to lcid");

    hr = SceRunQueryExact(&query, &row);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    else
    {
        ExitOnFailure(hr, "Failed to run display name query");

        hr = SceGetColumnString(row, PRODUCT_DISPLAY_NAME_NAME, psczDisplayName);
        ExitOnFailure(hr, "Failed to get display name");
    }

LExit:
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    ReleaseSceQuery(query);
    ReleaseSceRow(row);

    return hr;
}

HRESULT DisplayNameEnumerate(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __out DISPLAY_NAME **prgDisplayNames,
    __out DWORD *pcDisplayNames
    )
{
    HRESULT hr = S_OK;
    DWORD dwNewItemIndex;
    SCE_QUERY_RESULTS_HANDLE results = NULL;
    SCE_QUERY_HANDLE query = NULL;
    SCE_ROW_HANDLE row = NULL;

    hr = SceBeginQuery(pcdb->psceDb, PRODUCT_DISPLAY_NAME_TABLE, 1, &query);
    ExitOnFailure(hr, "Failed to begin query into display name table");

    hr = SceSetQueryColumnDword(query, dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", pcdb->dwAppID);

    hr = SceRunQueryRange(&query, &results);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query");

    *pcDisplayNames = 0;

    hr = SceGetNextResultRow(results, &row);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next result row");

        dwNewItemIndex = *pcDisplayNames;
        hr = MemEnsureArraySize(reinterpret_cast<void **>(prgDisplayNames), *pcDisplayNames + 1, sizeof(DISPLAY_NAME), 0);
        ExitOnFailure(hr, "Failed to ensure display name array size");
        ++(*pcDisplayNames);

        hr = SceGetColumnDword(row, PRODUCT_DISPLAY_NAME_LCID, &(*prgDisplayNames)[dwNewItemIndex].dwLCID);
        ExitOnFailure(hr, "Failed to get display name lcid");

        hr = SceGetColumnString(row, PRODUCT_DISPLAY_NAME_NAME, &(*prgDisplayNames)[dwNewItemIndex].sczName);
        ExitOnFailure(hr, "Failed to get display name");

        ReleaseNullSceRow(row);
        hr = SceGetNextResultRow(results, &row);
    }
    hr = S_OK;

LExit:
    ReleaseSceQuery(query);
    ReleaseSceQueryResults(results);
    ReleaseSceRow(row);

    return hr;
}

HRESULT DisplayNameAny(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __out DWORD *pdwLCID,
    __out LPWSTR *psczDisplayName
    )
{
    HRESULT hr = S_OK;
    BOOL fInSceTransaction = FALSE;
    SCE_QUERY_HANDLE query = NULL;
    SCE_ROW_HANDLE row = NULL;

    hr = SceBeginQuery(pcdb->psceDb, PRODUCT_DISPLAY_NAME_TABLE, 1, &query);
    ExitOnFailure(hr, "Failed to begin query into display name table");

    hr = SceSetQueryColumnDword(query, dwAppID);
    ExitOnFailure(hr, "Failed to set query column to appid");

    hr = SceRunQueryExact(&query, &row);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    else
    {
        ExitOnFailure(hr, "Failed to run lcid-neutral display name query");

        hr = SceGetColumnDword(row, PRODUCT_DISPLAY_NAME_LCID, pdwLCID);
        ExitOnFailure(hr, "Failed to get LCID");

        hr = SceGetColumnString(row, PRODUCT_DISPLAY_NAME_NAME, psczDisplayName);
        ExitOnFailure(hr, "Failed to get display name");
    }

LExit:
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    ReleaseSceQuery(query);
    ReleaseSceRow(row);

    return hr;
}

HRESULT DisplayNamePersist(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in DWORD dwLCID,
    __in LPCWSTR wzDisplayName
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDisplayName = NULL;
    BOOL fInSceTransaction = FALSE;
    SCE_QUERY_HANDLE query = NULL;
    SCE_ROW_HANDLE row = NULL;

    hr = SceBeginQuery(pcdb->psceDb, PRODUCT_DISPLAY_NAME_TABLE, 1, &query);
    ExitOnFailure(hr, "Failed to begin query into display name table");

    hr = SceSetQueryColumnDword(query, dwAppID);
    ExitOnFailure(hr, "Failed to set query column to appid");

    hr = SceSetQueryColumnDword(query, dwLCID);
    ExitOnFailure(hr, "Failed to set query column to lcid");

    hr = SceRunQueryExact(&query, &row);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;

        hr = SceBeginTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to start transaction");
        fInSceTransaction = TRUE;

        hr = ScePrepareInsert(pcdb->psceDb, PRODUCT_DISPLAY_NAME_TABLE, &row);
        ExitOnFailure(hr, "Failed to prepare insert");

        hr = SceSetColumnDword(row, PRODUCT_DISPLAY_NAME_APPID, dwAppID);
        ExitOnFailure(hr, "Failed to set display name");

        hr = SceSetColumnDword(row, PRODUCT_DISPLAY_NAME_LCID, dwLCID);
        ExitOnFailure(hr, "Failed to set display name");

        hr = SceSetColumnString(row, PRODUCT_DISPLAY_NAME_NAME, wzDisplayName);
        ExitOnFailure(hr, "Failed to set display name");

        hr = SceFinishUpdate(row);
        ExitOnFailure(hr, "Failed to finish insert");

        hr = SceCommitTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to commit transaction");
        fInSceTransaction = FALSE;
    }
    else
    {
        ExitOnFailure(hr, "Failed to run display name query");

        hr = SceGetColumnString(row, PRODUCT_DISPLAY_NAME_NAME, &sczDisplayName);
        ExitOnFailure(hr, "Failed to get display name");

        // If the strings are not identical, update to the desired value
        if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, sczDisplayName, -1, wzDisplayName, -1))
        {
            hr = SceBeginTransaction(pcdb->psceDb);
            ExitOnFailure(hr, "Failed to start transaction");
            fInSceTransaction = TRUE;

            hr = SceSetColumnString(row, PRODUCT_DISPLAY_NAME_NAME, wzDisplayName);
            ExitOnFailure(hr, "Failed to set display name");

            hr = SceCommitTransaction(pcdb->psceDb);
            ExitOnFailure(hr, "Failed to commit transaction");
            fInSceTransaction = FALSE;
        }
    }

LExit:
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    ReleaseStr(sczDisplayName);
    ReleaseSceQuery(query);
    ReleaseSceRow(row);

    return hr;
}

HRESULT DisplayNameRemoveAllForAppID(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_RESULTS_HANDLE results = NULL;
    SCE_QUERY_HANDLE query = NULL;
    SCE_ROW_HANDLE row = NULL;

    hr = SceBeginQuery(pcdb->psceDb, PRODUCT_DISPLAY_NAME_TABLE, 0, &query);
    ExitOnFailure(hr, "Failed to begin query into display name table");

    hr = SceSetQueryColumnDword(query, dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", pcdb->dwAppID);

    hr = SceRunQueryRange(&query, &results);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query");

    hr = SceGetNextResultRow(results, &row);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next result row");

        hr = SceDeleteRow(&row);
        ExitOnFailure(hr, "Failed to delete row");

        ReleaseNullSceRow(row);
        hr = SceGetNextResultRow(results, &row);
    }
    hr = S_OK;

LExit:
    ReleaseSceQuery(query);
    ReleaseSceQueryResults(results);
    ReleaseSceRow(row);

    return hr;
}

