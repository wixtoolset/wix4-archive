// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT SendDwordString(
    __in DWORD dwThreadId,
    __in DWORD dwMessageId,
    __in DWORD dwDatabaseIndex,
    __in DWORD dwDword1,
    __in_z LPCWSTR wzString1
    )
{
    HRESULT hr = S_OK;
    DWORD_STRING *pDwordString = NULL;

    pDwordString = static_cast<DWORD_STRING *>(MemAlloc(sizeof(DWORD_STRING), TRUE));

    pDwordString->dwDword1 = dwDword1;
    
    if (NULL != wzString1)
    {
        hr = StrAllocString(&pDwordString->sczString1, wzString1, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string1: %ls", wzString1);
    }

    if (!::PostThreadMessageW(dwThreadId, dwMessageId, dwDatabaseIndex, reinterpret_cast<LPARAM>(pDwordString)))
    {
        ExitWithLastError(hr, "Failed to send message %u to worker thread", dwMessageId);
    }

    pDwordString = NULL;

LExit:
    ReleaseDwordString(pDwordString);

    return hr;
}

HRESULT SendQwordString(
    __in DWORD dwThreadId,
    __in DWORD dwMessageId,
    __in DWORD dwDatabaseIndex,
    __in DWORD64 qwQword1,
    __in_z LPCWSTR wzString1
    )
{
    HRESULT hr = S_OK;
    QWORD_STRING *pQwordString = NULL;

    pQwordString = static_cast<QWORD_STRING *>(MemAlloc(sizeof(QWORD_STRING), TRUE));

    pQwordString->qwQword1 = qwQword1;
    
    if (NULL != wzString1)
    {
        hr = StrAllocString(&pQwordString->sczString1, wzString1, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string1: %ls", wzString1);
    }

    if (!::PostThreadMessageW(dwThreadId, dwMessageId, dwDatabaseIndex, reinterpret_cast<LPARAM>(pQwordString)))
    {
        ExitWithLastError(hr, "Failed to send message %u to worker thread", dwMessageId);
    }

    pQwordString = NULL;

LExit:
    ReleaseQwordString(pQwordString);

    return hr;
}

HRESULT SendStringPair(
    __in DWORD dwThreadId,
    __in DWORD dwMessageId,
    __in DWORD dwDatabaseIndex,
    __in_z LPCWSTR wzString1,
    __in_z LPCWSTR wzString2
    )
{
    HRESULT hr = S_OK;
    STRING_PAIR *pStringPair = NULL;

    pStringPair = static_cast<STRING_PAIR *>(MemAlloc(sizeof(STRING_PAIR), TRUE));

    if (NULL != wzString1)
    {
        hr = StrAllocString(&pStringPair->sczString1, wzString1, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string1: %ls", wzString1);
    }

    if (NULL != wzString2)
    {
        hr = StrAllocString(&pStringPair->sczString2, wzString2, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string2: %ls", wzString2);
    }

    if (!::PostThreadMessageW(dwThreadId, dwMessageId, dwDatabaseIndex, reinterpret_cast<LPARAM>(pStringPair)))
    {
        ExitWithLastError(hr, "Failed to send message %u to worker thread", dwMessageId);
    }

    pStringPair = NULL;

LExit:
    ReleaseStringPair(pStringPair);

    return hr;
}

HRESULT SendStringTriplet(
    __in DWORD dwThreadId,
    __in DWORD dwMessageId,
    __in DWORD dwDatabaseIndex,
    __in_z LPCWSTR wzString1,
    __in_z LPCWSTR wzString2,
    __in_z LPCWSTR wzString3
    )
{
    HRESULT hr = S_OK;
    STRING_TRIPLET *pStringTriplet = NULL;

    pStringTriplet = static_cast<STRING_TRIPLET *>(MemAlloc(sizeof(STRING_TRIPLET), TRUE));

    if (NULL != wzString1)
    {
        hr = StrAllocString(&pStringTriplet->sczString1, wzString1, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string1: %ls", wzString1);
    }

    if (NULL != wzString2)
    {
        hr = StrAllocString(&pStringTriplet->sczString2, wzString2, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string2: %ls", wzString2);
    }

    if (NULL != wzString3)
    {
        hr = StrAllocString(&pStringTriplet->sczString3, wzString3, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string3: %ls", wzString3);
    }

    if (!::PostThreadMessageW(dwThreadId, dwMessageId, dwDatabaseIndex, reinterpret_cast<LPARAM>(pStringTriplet)))
    {
        ExitWithLastError(hr, "Failed to send message %u to worker thread", dwMessageId);
    }

    pStringTriplet = NULL;

LExit:
    ReleaseStringTriplet(pStringTriplet);

    return hr;
}

HRESULT SendBackgroundStatusCallback(
    __in DWORD dwThreadId,
    __in HRESULT hrStatus,
    __in BACKGROUND_STATUS_TYPE type,
    __in_z LPCWSTR wzString1,
    __in_z LPCWSTR wzString2,
    __in_z LPCWSTR wzString3
    )
{
    HRESULT hr = S_OK;
    BACKGROUND_STATUS_CALLBACK *pBackgroundStatusCallback = NULL;

    pBackgroundStatusCallback = static_cast<BACKGROUND_STATUS_CALLBACK *>(MemAlloc(sizeof(BACKGROUND_STATUS_CALLBACK), TRUE));

    pBackgroundStatusCallback->hrStatus = hrStatus;
    pBackgroundStatusCallback->type = type;

    if (NULL != wzString1)
    {
        hr = StrAllocString(&pBackgroundStatusCallback->sczString1, wzString1, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string1: %ls", wzString1);
    }

    if (NULL != wzString2)
    {
        hr = StrAllocString(&pBackgroundStatusCallback->sczString2, wzString2, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string2: %ls", wzString2);
    }

    if (NULL != wzString3)
    {
        hr = StrAllocString(&pBackgroundStatusCallback->sczString3, wzString3, 0);
        ExitOnFailure(hr, "Failed to allocate copy of string3: %ls", wzString3);
    }

    if (!::PostThreadMessageW(dwThreadId, WM_BROWSE_BACKGROUND_STATUS_CALLBACK, reinterpret_cast<WPARAM>(pBackgroundStatusCallback), 0))
    {
        ExitWithLastError(hr, "Failed to send message WM_BROWSE_BACKGROUND_STATUS_CALLBACK to worker thread");
    }

    pBackgroundStatusCallback = NULL;

LExit:
    ReleaseBackgroundStatusCallback(pBackgroundStatusCallback);

    return hr;
}

HRESULT SendBackgroundConflictsFoundCallback(
    __in DWORD dwThreadId,
    __in CFGDB_HANDLE cdHandle,
    __in CONFLICT_PRODUCT *rgcpProduct,
    __in DWORD cProduct
    )
{
    HRESULT hr = S_OK;
    BACKGROUND_CONFLICTS_FOUND_CALLBACK *pBackgroundConflictsFoundCallback = NULL;

    pBackgroundConflictsFoundCallback = static_cast<BACKGROUND_CONFLICTS_FOUND_CALLBACK *>(MemAlloc(sizeof(BACKGROUND_CONFLICTS_FOUND_CALLBACK), TRUE));

    pBackgroundConflictsFoundCallback->cdHandle = cdHandle;
    pBackgroundConflictsFoundCallback->rgcpProduct = rgcpProduct;
    pBackgroundConflictsFoundCallback->cProduct = cProduct;

    if (!::PostThreadMessageW(dwThreadId, WM_BROWSE_BACKGROUND_CONFLICTS_FOUND_CALLBACK, reinterpret_cast<WPARAM>(pBackgroundConflictsFoundCallback), 0))
    {
        ExitWithLastError(hr, "Failed to send message WM_BROWSE_BACKGROUND_CONFLICTS_FOUND_CALLBACK to worker thread");
    }

    pBackgroundConflictsFoundCallback = NULL;

LExit:
    ReleaseBackgroundConflictsFoundCallback(pBackgroundConflictsFoundCallback);

    return hr;
}
