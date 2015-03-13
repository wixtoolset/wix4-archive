//-------------------------------------------------------------------------------------------------
// <copyright file="scaweblog7.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Custom Actions for handling log settings for a particular IIS Website
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
// sql queries
LPCWSTR vcsWebLogQuery7 = L"SELECT `Log`, `Format` "
                         L"FROM `IIsWebLog`  WHERE `Log`=?";

enum eWebLogQuery { wlqLog = 1, wlqFormat };

/* ****************************************************************
 * ScaGetWebLog7 -Retrieves Log table data for the specified Log key
 *
 * ****************************************************************/
HRESULT ScaGetWebLog7(
    __in_z LPCWSTR wzLog,
    __in WCA_WRAPQUERY_HANDLE hWebLogQuery,
    __out SCA_WEB_LOG* pswl
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzData = NULL;
    MSIHANDLE hRec;

    if (0 == WcaGetQueryRecords(hWebLogQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaGetWebLog() - no records to process");
        ExitFunction1(hr = S_FALSE);
    }

    WcaFetchWrappedReset(hWebLogQuery);

    hr = WcaFetchWrappedRecordWhereString(hWebLogQuery, wlqLog, wzLog, &hRec);
    if (E_NOMOREITEMS == hr)
    {
        ExitOnFailure(hr, "cannot locate IIsWebLog.Log='%ls'", wzLog);
    }
    HRESULT hrTemp = WcaFetchWrappedRecordWhereString(hWebLogQuery, wlqLog, wzLog, &hRec);

    if (SUCCEEDED(hrTemp))
    {
        ExitOnFailure(hr, "error - found multiple matching IIsWebLog rows");
    }

    ::ZeroMemory(pswl, sizeof(SCA_WEB_LOG));

    // check that log key matches
    hr = WcaGetRecordString(hRec, wlqLog, &pwzData);
    ExitOnFailure(hr, "failed to get IIsWebLog.Log for Log: %ls", wzLog);
    hr = ::StringCchCopyW(pswl->wzLog, countof(pswl->wzLog), pwzData);
    ExitOnFailure(hr, "failed to copy log name: %ls", pwzData);

    hr = WcaGetRecordString(hRec, wlqFormat, &pwzData);
    ExitOnFailure(hr, "failed to get IIsWebLog.Format for Log:", wzLog);

    //translate WIX log format name strings to IIS7
    if (0 == lstrcmpW(pwzData, L"Microsoft IIS Log File Format"))
    {
        hr = ::StringCchCopyW(pswl->wzFormat, countof(pswl->wzFormat), L"IIS");
        ExitOnFailure(hr, "failed to copy log format: %ls", pwzData);
    }
    else if (0 == lstrcmpW(pwzData, L"NCSA Common Log File Format"))
    {
        hr = ::StringCchCopyW(pswl->wzFormat, countof(pswl->wzFormat), L"NCSA");
        ExitOnFailure(hr, "failed to copy log format: %ls", pwzData);
    }
    else if (0 == lstrcmpW(pwzData, L"none"))
    {
        hr = ::StringCchCopyW(pswl->wzFormat, countof(pswl->wzFormat), L"none");
        ExitOnFailure(hr, "failed to copy log format: %ls", pwzData);
    }
    else if (0 == lstrcmpW(pwzData, L"ODBC Logging"))
    {
        hr = ::StringCchCopyW(pswl->wzFormat, countof(pswl->wzFormat), L"W3C");
        ExitOnFailure(hr, "failed to copy log format: %ls", pwzData);
    }
    else if (0 == lstrcmpW(pwzData, L"W3C Extended Log File Format"))
    {
        hr = ::StringCchCopyW(pswl->wzFormat, countof(pswl->wzFormat), L"W3C");
        ExitOnFailure(hr, "failed to copy log format: %ls", pwzData);
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_INDEX);
        ExitOnFailure(hr, "Invalid log file format: %ls", pwzData);
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}


/* ****************************************************************
 * ScaWriteWebLog -Writes the IIS log values to the metabase.
 *
 * ****************************************************************/
HRESULT ScaWriteWebLog7(
    LPCWSTR wzWebBase,
    const SCA_WEB_LOG *pswl
    )
{
    HRESULT hr = S_OK;

    if (*pswl->wzFormat)
    {
        //write pswl->wzFormat
        hr = ScaWriteConfigID(IIS_WEBLOG);
        ExitOnFailure(hr, "Failed to write log format id");
        hr = ScaWriteConfigString(wzWebBase);
        ExitOnFailure(hr, "Failed to write log web key");
        hr = ScaWriteConfigString(pswl->wzFormat);
        ExitOnFailure(hr, "Failed to write log format string");
    }

LExit:
    return hr;
}


