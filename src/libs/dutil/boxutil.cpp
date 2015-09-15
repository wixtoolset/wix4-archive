//-------------------------------------------------------------------------------------------------
// <copyright file="boxutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Box
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// internal function declarations

static HRESULT GetAttachedContainerInfo(
    __in HANDLE hFile,
    __in DWORD iContainerIndex,
    __out DWORD* pdwFormat,
    __out DWORD64* pqwOffset,
    __out DWORD64* pqwSize
    );


// function definitions

extern "C" void BoxesUninitialize(
    __in WIX_BOXES* pBoxes
    )
{
    if (pBoxes->rgBoxes)
    {
        for (DWORD i = 0; i < pBoxes->cBoxes; ++i)
        {
            WIX_BOX* pBox = &pBoxes->rgBoxes[i];

            ReleaseStr(pBox->sczId);
            ReleaseStr(pBox->sczHash);
            ReleaseStr(pBox->sczSourcePath);
            ReleaseStr(pBox->sczFilePath);
            ReleaseMem(pBox->pbHash);
            ReleaseStr(pBox->downloadSource.sczUrl);
            ReleaseStr(pBox->downloadSource.sczUser);
            ReleaseStr(pBox->downloadSource.sczPassword);
        }
        MemFree(pBoxes->rgBoxes);
    }

    // clear struct
    memset(pBoxes, 0, sizeof(WIX_BOXES));
}

extern "C" HRESULT BoxOpen(
    __in WIX_BOX_CONTEXT* pContext,
    __in WIX_BOX* pBox,
    __in HANDLE hContainerFile,
    __in_z LPCWSTR wzFilePath
    )
{
    HRESULT hr = S_OK;
    LARGE_INTEGER li = { };

    // initialize context
    pContext->qwSize = pBox->qwFileSize;
    pContext->qwOffset = pBox->qwAttachedOffset;

    // If the handle to the container is not open already, open container file
    if (INVALID_HANDLE_VALUE == hContainerFile)
    {
        pContext->hFile = ::CreateFileW(wzFilePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
        ExitOnInvalidHandleWithLastError(pContext->hFile, hr, "Failed to open file: %ls", wzFilePath);
    }
    else // use the container file handle.
    {
        if (!::DuplicateHandle(::GetCurrentProcess(), hContainerFile, ::GetCurrentProcess(), &pContext->hFile, 0, FALSE, DUPLICATE_SAME_ACCESS))
        {
            ExitWithLastError(hr, "Failed to duplicate handle to container: %ls", wzFilePath);
        }
    }

    // If it is a container attached to an executable, seek to the container offset.
    if (pBox->fAttached)
    {
        li.QuadPart = (LONGLONG)pContext->qwOffset;
    }

    if (!::SetFilePointerEx(pContext->hFile, li, NULL, FILE_BEGIN))
    {
        ExitWithLastError(hr, "Failed to move file pointer to container offset.");
    }

    // open the archive
    hr = CabExtractOpen(pContext, wzFilePath);
    ExitOnFailure(hr, "Failed to open container.");

LExit:
    return hr;
}

extern "C" HRESULT BoxNextStream(
    __in WIX_BOX_CONTEXT* pContext,
    __inout_z LPWSTR* psczStreamName
    )
{
    HRESULT hr = S_OK;

    hr = CabExtractNextStream(pContext, psczStreamName);

//LExit:
    return hr;
}

extern "C" HRESULT BoxStreamToFile(
    __in WIX_BOX_CONTEXT* pContext,
    __in_z LPCWSTR wzFileName
    )
{
    HRESULT hr = S_OK;

    hr = CabExtractStreamToFile(pContext, wzFileName);

//LExit:
    return hr;
}

extern "C" HRESULT BoxStreamToBuffer(
    __in WIX_BOX_CONTEXT* pContext,
    __out BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    )
{
    HRESULT hr = S_OK;

    hr = CabExtractStreamToBuffer(pContext, ppbBuffer, pcbBuffer);

//LExit:
    return hr;
}

extern "C" HRESULT BoxSkipStream(
    __in WIX_BOX_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    hr = CabExtractSkipStream(pContext);

//LExit:
    return hr;
}

extern "C" HRESULT BoxClose(
    __in WIX_BOX_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    // close container
    hr = CabExtractClose(pContext);
    ExitOnFailure(hr, "Failed to close cabinet.");

LExit:
    ReleaseFile(pContext->hFile);

    if (SUCCEEDED(hr))
    {
        memset(pContext, 0, sizeof(WIX_BOX_CONTEXT));
    }

    return hr;
}

extern "C" HRESULT BoxFindById(
    __in WIX_BOXES* pBoxes,
    __in_z LPCWSTR wzId,
    __out WIX_BOX** ppBox
    )
{
    HRESULT hr = S_OK;
    WIX_BOX* pBox = NULL;

    for (DWORD i = 0; i < pBoxes->cBoxes; ++i)
    {
        pBox = &pBoxes->rgBoxes[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pBox->sczId, -1, wzId, -1))
        {
            *ppBox = pBox;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}
