#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

extern PFN_GETSYSTEMTIME SystemTimeGetter;

struct LEGACY_DIRECTORY_MAP
{
    int nFolder; // CSIDL, i.e. CSIDL_MYDOCUMENTS
    LPCWSTR wzInput; // what appears in the XML, i.e. "MyDocumentsFolder:\"
    LPCWSTR wzAppend;
};

HRESULT UtilSyncDb(
    __in CFGDB_STRUCT *pcdbRemote,
    __deref_out_ecount_opt(*pcProduct) CONFLICT_PRODUCT **prgcpProductList,
    __out DWORD *pcProduct
    );
HRESULT UtilSyncAllProducts(
    __in CFGDB_STRUCT *pcdb,
    __out CONFLICT_PRODUCT **prgConflictProducts,
    __out DWORD *pcConflictProducts
    );
HRESULT UtilExpandLegacyPath(
    __in LPCWSTR wzInput,
    __in LEGACY_DETECTION *pDetect,
    __deref_out LPWSTR *psczOutput
    );
int UtilCompareSystemTimes(
    __in const SYSTEMTIME *pst1,
    __in const SYSTEMTIME *pst2
    );
// Outputs *pst1 - *pst2 in seconds
HRESULT UtilSubtractSystemTimes(
    __in const SYSTEMTIME *pst1,
    __in const SYSTEMTIME *pst2,
    __out LONGLONG *pSeconds
    );
HRESULT UtilAddToSystemTime(
    __in DWORD dwSeconds,
    __inout SYSTEMTIME *pst
    );
HRESULT UtilTestWriteAccess(
    __in HANDLE hToken,
    __in_z LPCWSTR wzPath
    );
HRESULT UtilConvertToVirtualStorePath(
    __in_z LPCWSTR wzOriginalPath,
    __out LPWSTR *psczOutput
    );
void UtilGetSystemTime(
    __inout SYSTEMTIME *pst
    );
BOOL UtilIs64BitSystem();

#ifdef __cplusplus
}
#endif
