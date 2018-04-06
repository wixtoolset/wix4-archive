#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

LPCWSTR UIGetResolutionText(
    __in RESOLUTION_CHOICE rcChoice,
    __in_z LPCWSTR wzRemoteDatabaseName
    );
LPWSTR UIGetTypeDisplayName(
    __in CONFIG_VALUETYPE cvType
    );
HRESULT UIGetSingleSelectedItemFromListView(
    __in HWND hwnd,
    __out_opt DWORD *pdwIndex,
    __out_opt DWORD *pdwParam
    );
void UIClearSelectionFromListView(
    __in HWND hwnd
    );
HRESULT UISetListViewText(
    __in HWND hwnd,
    __in_z LPCWSTR sczText
    );
HRESULT UISetListViewToProductEnum(
    __in HWND hwnd,
    __in C_CFG_ENUMERATION_HANDLE cehProducts,
    __in const BOOL *rgfInstalled,
    __in BOOL fShowUninstalledProducts
    );
HRESULT UIForgetProductsFromListView(
    __in HWND hwnd,
    __in CFGDB_HANDLE cdhHandle,
    __in C_CFG_ENUMERATION_HANDLE cehProducts
    );
HRESULT UISetListViewToValueEnum(
    __in HWND hwnd,
    __in_opt C_CFG_ENUMERATION_HANDLE cehValues,
    __in BOOL fShowDeletedValues
    );
HRESULT UISetListViewToValueHistoryEnum(
    __in HWND hwnd,
    __in_opt C_CFG_ENUMERATION_HANDLE cehValues
    );
HRESULT UIDeleteValuesFromListView(
    __in HWND hwnd,
    __in CFGDB_HANDLE cdhHandle,
    __in C_CFG_ENUMERATION_HANDLE cehValues
    );
HRESULT UISetValueConflictsFromListView(
    __in HWND hwnd,
    __in_z LPCWSTR wzDatabaseName,
    __in CONFLICT_PRODUCT *pcpProductConflict,
    __in RESOLUTION_CHOICE rcChoice
    );
HRESULT UISetProductConflictsFromListView(
    __in HWND hwnd,
    __in_z LPCWSTR wzDatabaseName,
    __in_ecount(cProductCount) CONFLICT_PRODUCT *pcplProductConflictList,
    __in DWORD cProductCount,
    __in RESOLUTION_CHOICE rcChoice
    );
HRESULT UISetListViewToValueConflictArray(
    __in HWND hwnd,
    __in LPCWSTR wzDatabaseName,
    __in_ecount(cConflictValueCount) C_CFG_ENUMERATION_HANDLE *pcehHandle,
    __in_ecount(cConflictValueCount) const RESOLUTION_CHOICE *rgrcValueChoices,
    __in DWORD cConflictValueCount
    );
HRESULT UISetListViewToProductConflictArray(
    __in HWND hwnd,
    __in LPCWSTR wzDatabaseName,
    __in HRESULT hrSyncResult,
    __in_ecount(dwConflictProductCount) const CONFLICT_PRODUCT *pcpProductConflict,
    __in DWORD dwConflictProductCount
    );
HRESULT UIListViewInsertItem(
    __in const HWND hwnd,
    __inout DWORD *pdwIndex,
    __in LPCWSTR sczText,
    __in DWORD dwParam,
    __in DWORD dwImage
    );
HRESULT UIListViewSetItemText(
    __in const HWND hwnd,
    __in DWORD dwIndex,
    __in DWORD dwColumnIndex,
    __in LPCWSTR sczText
    );
HRESULT UIListViewSetItem(
    __in const HWND hwnd,
    __in DWORD dwIndex,
    __in LPCWSTR sczText,
    __in DWORD dwParam,
    __in DWORD dwImage
    );
void UIListViewTrimSize(
    __in const HWND hwnd,
    __in DWORD dwNewRowCount
    );
HRESULT UIExportFile(
    __in const HWND hwnd,
    __out_z LPWSTR *psczPath
    );
HRESULT UIMessageBoxDisplayError(
    __in HWND hwnd,
    __in LPCWSTR wzErrorMessage,
    __in HRESULT hr
    );
HRESULT UISelectBestLCIDToDisplay(
    __in DISPLAY_NAME *rgDisplayNames,
    __in DWORD cDisplayNames,
    __out DWORD *pdwIndex
    );

#ifdef __cplusplus
}
#endif
