//-------------------------------------------------------------------------------------------------
// <copyright file="guidlist.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Utilities for interacting with the DATABASE_GUID_TABLE
// </summary>
//-------------------------------------------------------------------------------------------------

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
