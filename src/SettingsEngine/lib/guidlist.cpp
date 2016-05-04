// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT GuidListEnsure(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzOtherGuid,
    __out LPWSTR *psczString
    )
{
    HRESULT hr = S_OK;
    DWORD dwRowId = 0;
    BOOL fInSceTransaction = FALSE;
    SCE_QUERY_HANDLE query = NULL;
    SCE_ROW_HANDLE row = NULL;

    hr = SceBeginQuery(pcdb->psceDb, DATABASE_GUID_LIST_TABLE, 1, &query);
    ExitOnFailure(hr, "Failed to begin query into database guid table");

    hr = SceSetQueryColumnString(query, wzOtherGuid);
    ExitOnFailure(hr, "Failed to set query column to guid string");

    hr = SceRunQueryExact(&query, &row);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;

        hr = SceBeginTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to start transaction");
        fInSceTransaction = TRUE;

        hr = ScePrepareInsert(pcdb->psceDb, DATABASE_GUID_LIST_TABLE, &row);
        ExitOnFailure(hr, "Failed to prepare insert");

        hr = SceSetColumnString(row, DATABASE_GUID_LIST_STRING, wzOtherGuid);
        ExitOnFailure(hr, "Failed to set database guid list string");

        hr = SceFinishUpdate(row);
        ExitOnFailure(hr, "Failed to finish insert");

        hr = SceCommitTransaction(pcdb->psceDb);
        ExitOnFailure(hr, "Failed to commit transaction");
        fInSceTransaction = FALSE;
    }
    else
    {
        ExitOnFailure(hr, "Failed to run guid string query");
    }

    hr = SceGetColumnDword(row, DATABASE_GUID_LIST_ID, &dwRowId);
    ExitOnFailure(hr, "Failed to get Id column");

    hr = StrAllocFormatted(psczString, L"%d", dwRowId);
    ExitOnFailure(hr, "Failed to convert id to string");

LExit:
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    ReleaseSceQuery(query);
    ReleaseSceRow(row);

    return hr;
}
