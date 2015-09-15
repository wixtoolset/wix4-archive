//-------------------------------------------------------------------------------------------------
// <copyright file="boxutil.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Box container library
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#if defined(__cplusplus)
extern "C" {
#endif


// constants

enum WIX_BOX_OPERATION
{
    WIX_BOX_OPERATION_NONE,
    WIX_BOX_OPERATION_NEXT_STREAM,
    WIX_BOX_OPERATION_STREAM_TO_FILE,
    WIX_BOX_OPERATION_STREAM_TO_BUFFER,
    WIX_BOX_OPERATION_SKIP_STREAM,
    WIX_BOX_OPERATION_CLOSE,
};


// structs

typedef struct _WIX_BOX
{
    LPWSTR sczId;
    BOOL fPrimary;
    BOOL fAttached;
    DWORD dwAttachedIndex;
    DWORD64 qwFileSize;
    LPWSTR sczHash;
    LPWSTR sczFilePath;         // relative path to box.
    LPWSTR sczSourcePath;
    DOWNLOAD_SOURCE downloadSource;

    BYTE* pbHash;
    DWORD cbHash;
    DWORD64 qwAttachedOffset;
    BOOL fActuallyAttached;     // indicates whether an attached box is attached or missing.

    //LPWSTR* rgsczPayloads;
    //DWORD cPayloads;
} WIX_BOX;

typedef struct _WIX_BOXES
{
    WIX_BOX* rgBoxes;
    DWORD cBoxes;
} WIX_BOXES;

typedef struct _WIX_BOX_CONTEXT_CABINET_VIRTUAL_FILE_POINTER
{
    HANDLE hFile;
    LARGE_INTEGER liPosition;
} WIX_BOX_CONTEXT_CABINET_VIRTUAL_FILE_POINTER;

typedef struct _WIX_BOX_CONTEXT_CABINET
{
    LPWSTR sczFile;

    HANDLE hThread;
    HANDLE hBeginOperationEvent;
    HANDLE hOperationCompleteEvent;

    WIX_BOX_OPERATION operation;
    HRESULT hrError;

    LPWSTR* psczStreamName;
    LPCWSTR wzTargetFile;
    HANDLE hTargetFile;
    BYTE* pbTargetBuffer;
    DWORD cbTargetBuffer;
    DWORD iTargetBuffer;

    WIX_BOX_CONTEXT_CABINET_VIRTUAL_FILE_POINTER* rgVirtualFilePointers;
    DWORD cVirtualFilePointers;
} WIX_BOX_CONTEXT_CABINET;

typedef struct _WIX_BOX_CONTEXT
{
    HANDLE hFile;
    DWORD64 qwOffset;
    DWORD64 qwSize;

    union
    {
        WIX_BOX_CONTEXT_CABINET Cabinet;
    };

} WIX_BOX_CONTEXT;


// functions

HRESULT BoxesAddBox(
    __in WIX_BOXES* pBoxes,
    __in WIX_BOX* pBox
    );
void BoxesUninitialize(
    __in WIX_BOXES* pBoxes
    );
HRESULT BoxOpen(
    __in WIX_BOX_CONTEXT* pContext,
    __in WIX_BOX* pBox,
    __in HANDLE hBoxFile,
    __in_z LPCWSTR wzFilePath
    );
HRESULT BoxNextStream(
    __inout WIX_BOX_CONTEXT* pContext,
    __inout_z LPWSTR* psczStreamName
    );
HRESULT BoxStreamToFile(
    __in WIX_BOX_CONTEXT* pContext,
    __in_z LPCWSTR wzFileName
    );
HRESULT BoxStreamToBuffer(
    __in WIX_BOX_CONTEXT* pContext,
    __out BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    );
HRESULT BoxSkipStream(
    __in WIX_BOX_CONTEXT* pContext
    );
HRESULT BoxClose(
    __in WIX_BOX_CONTEXT* pContext
    );
HRESULT BoxFindById(
    __in WIX_BOXES* pBoxes,
    __in_z LPCWSTR wzId,
    __out WIX_BOX** ppBox
    );


#if defined(__cplusplus)
}
#endif
