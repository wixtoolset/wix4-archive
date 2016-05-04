// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR RESOLVE_MIXED_TEXT = L"(Mixed)";
static const LPCWSTR RESOLVE_MINE_TEXT = L"Mine";
static const LPCWSTR RESOLVE_NONE_TEXT = L"(Unresolved)";

static HRESULT ListViewSort(
    __in HWND hwnd
    );
static int CALLBACK ListViewItemCompare(
    __in LPARAM lParam1,
    __in LPARAM lParam2,
    __in LPARAM lParamSort
    );

LPCWSTR UIGetResolutionText(
    __in RESOLUTION_CHOICE rcChoice,
    __in_z LPCWSTR wzRemoteDatabaseName
    )
{
    switch (rcChoice)
    {
    case RESOLUTION_LOCAL:
        return RESOLVE_MINE_TEXT;

    case RESOLUTION_REMOTE:
        return wzRemoteDatabaseName;

    case RESOLUTION_UNRESOLVED: __fallthrough;
    default:
        return RESOLVE_NONE_TEXT;
    }
}

LPWSTR UIGetTypeDisplayName(
    __in CONFIG_VALUETYPE cvType
    )
{
    switch (cvType)
    {
    case VALUE_DELETED:
        return L"Deleted";
    case VALUE_BLOB:
        return L"Blob";
    case VALUE_BOOL:
        return L"Boolean";
    case VALUE_STRING:
        return L"String";
    case VALUE_DWORD:
        return L"Dword";
    case VALUE_QWORD:
        return L"Qword";
    case VALUE_INVALID:
        return L"Invalid";
    default:
        return L"Unknown";
    }
}

HRESULT UIGetSingleSelectedItemFromListView(
    __in HWND hwnd,
    __out_opt DWORD *pdwIndex, 
    __out_opt DWORD *pdwParam
    )
{
    HRESULT hr = S_OK;
    DWORD dwValueCount;
    LVITEM lvItem = { };
    DWORD dwFoundIndex = DWORD_MAX;
    DWORD dwFoundParam = DWORD_MAX;

    lvItem.mask = LVIF_PARAM;

    // Docs don't indicate any way for it to return failure
    dwValueCount = ::SendMessageW(hwnd, LVM_GETITEMCOUNT, 0, 0);

    for (DWORD i = 0; i < dwValueCount; ++i)
    {
        // If it's selected
        if (::SendMessageW(hwnd, LVM_GETITEMSTATE, i, LVIS_SELECTED))
        {
            if (DWORD_MAX != dwFoundIndex)
            {
                if (NULL != pdwIndex)
                {
                    *pdwIndex = DWORD_MAX;
                }
                if (NULL != pdwParam)
                {
                    *pdwParam = DWORD_MAX;
                }

                // More than one item found, so report the issue
                ExitFunction1(hr = E_NOTFOUND);
            }

            lvItem.iItem = i;

            if (!::SendMessageW(hwnd, LVM_GETITEM, 0, reinterpret_cast<LPARAM>(&lvItem)))
            {
                ExitWithLastError(hr, "Failed to get item lparam from list view to delete values");
            }

            dwFoundIndex = i;
            dwFoundParam = lvItem.lParam;
        }
    }

    if (NULL != pdwIndex)
    {
        *pdwIndex = dwFoundIndex;
    }
    if (NULL != pdwParam)
    {
        *pdwParam = dwFoundParam;
    }

LExit:
    return hr;
}

void UIClearSelectionFromListView(
    __in HWND hwnd
    )
{
    DWORD dwValueCount;
    LVITEM lvItem = { };

    lvItem.mask = LVIF_PARAM;

    // Docs don't indicate any way for it to return failure
    dwValueCount = ::SendMessageW(hwnd, LVM_GETITEMCOUNT, 0, 0);

    for (DWORD i = 0; i < dwValueCount; ++i)
    {
        // If it's selected, unselect it
        if (::SendMessageW(hwnd, LVM_GETITEMSTATE, i, LVIS_SELECTED))
        {
            ListView_SetItemState(hwnd, i, 0, LVIS_SELECTED);
        }
    }
}

HRESULT UISetListViewText(
    __in HWND hwnd,
    __in_z LPCWSTR sczText
    )
{
    HRESULT hr = S_OK;
    DWORD dwTopIndex = 0;
    DWORD dwNumRows = ListView_GetItemCount(hwnd);

    ::EnableWindow(hwnd, FALSE);

    // Figure out number of columns in listview
    HWND hwndHeader = reinterpret_cast<HWND>(SendMessageW(hwnd, LVM_GETHEADER, static_cast<WPARAM>(NULL), static_cast<LPARAM>(NULL)));
    DWORD64 uNumColumns = SendMessageW(hwndHeader, HDM_GETITEMCOUNT, static_cast<WPARAM>(NULL), static_cast<LPARAM>(NULL));

    if (0 == ListView_GetItemCount(hwnd))
    {
        hr = UIListViewInsertItem(hwnd, &dwTopIndex, sczText, 0, 0);
        ExitOnFailure(hr, "Failed to insert text into listview control");
    }
    else
    {
        dwTopIndex = ::SendMessageW(hwnd, LVM_GETTOPINDEX, 0, 0);
    }

    hr = UIListViewSetItem(hwnd, dwTopIndex, sczText, 0, DWORD_MAX);
    ExitOnFailure(hr, "Failed to set list view first visible item as text");

    // Clearing text in all remaining columns
    for (DWORD i = 1; i < uNumColumns; ++i)
    {
        hr = UIListViewSetItemText(hwnd, dwTopIndex, i, L"");
        ExitOnFailure(hr, "Failed to clear text on sub item %u", i);
    }

    // Now do so for all remaining rows of the listview that are visible
    for (DWORD i = dwTopIndex + 1; i < dwNumRows; ++i)
    {
        hr = UIListViewSetItem(hwnd, i, L"", 0, DWORD_MAX);
        ExitOnFailure(hr, "Failed to set row %u first column", i);

        for (DWORD j = 1; j < uNumColumns; ++j)
        {
            hr = UIListViewSetItemText(hwnd, i, j, L"");
            ExitOnFailure(hr, "Failed to clear text on sub item %u", j);
        }
    }

    hr = S_OK;

LExit:
    return hr;
}

HRESULT UISetListViewToProductEnum(
    __in HWND hwnd,
    __in C_CFG_ENUMERATION_HANDLE cehProducts,
    __in const BOOL *rgfInstalled,
    __in BOOL fShowUninstalledProducts
    )
{
    HRESULT hr = S_OK;
    DWORD dwInsertIndex = 0;
    DWORD dwCount = 0;
    DWORD dwListViewRowCount;
    DWORD dwInsertImage = 0;
    DWORD dwDisplayNameToDisplay = DWORD_MAX;
    DISPLAY_NAME *rgDisplayNames = NULL;
    DWORD cDisplayNames = 0;
    LPCWSTR wzText = NULL;

    dwListViewRowCount = ListView_GetItemCount(hwnd);

    if (NULL != cehProducts)
    {
        hr = CfgEnumReadDword(cehProducts, 0, ENUM_DATA_COUNT, &dwCount);
        ExitOnFailure(hr, "Failed to read count of product enumeration");
    }

    if (0 == dwCount)
    {
        hr = UISetListViewText(hwnd, L"No products to display.");
        ExitOnFailure(hr, "Failed to set 'no products to display' string in product list view");

        ExitFunction1(hr = S_OK);
    }

    dwInsertIndex = 0;
    for (DWORD i = 0; i < dwCount; ++i)
    {
        hr = CfgEnumReadDisplayNameArray(cehProducts, i, &rgDisplayNames, &cDisplayNames);
        ExitOnFailure(hr, "Failed to read display names from enumeration");

        hr = UISelectBestLCIDToDisplay(rgDisplayNames, cDisplayNames, &dwDisplayNameToDisplay);
        if (FAILED(hr))
        {
            hr = S_OK;

            // Fallback to regular product id
            hr = CfgEnumReadString(cehProducts, i, ENUM_DATA_PRODUCTNAME, &wzText);
            ExitOnFailure(hr, "Failed to read product name from enum");
        }
        else
        {
            wzText = rgDisplayNames[dwDisplayNameToDisplay].sczName;
        }

#pragma prefast(push)
#pragma prefast(disable:26007)
        if (rgfInstalled[i])
#pragma prefast(pop)
        {
            dwInsertImage = 1;
        }
        else
        {
            if (!fShowUninstalledProducts)
            {
                continue;
            }
            else
            {
                dwInsertImage = 0;
            }
        }

        if (dwInsertIndex >= dwListViewRowCount)
        {
            hr = UIListViewInsertItem(hwnd, &dwInsertIndex, wzText, i, dwInsertImage);
            ExitOnFailure(hr, "Failed to insert product into listview control");
        }
        else
        {
            hr = UIListViewSetItem(hwnd, dwInsertIndex, wzText, i, dwInsertImage);
            ExitOnFailure(hr, "Failed to set product in listview control");
        }

        ++dwInsertIndex;
    }
    UIListViewTrimSize(hwnd, dwInsertIndex);

    hr = ListViewSort(hwnd);
    ExitOnFailure(hr, "Failed to sort listview");

    EnableWindow(hwnd, TRUE);

LExit:
    return hr;
}

HRESULT UIForgetProductsFromListView(
    __in HWND hwnd,
    __in CFGDB_HANDLE cdhHandle,
    __in C_CFG_ENUMERATION_HANDLE cehProducts
    )
{
    HRESULT hr = S_OK;
    DWORD dwValueCount;
    LVITEM lvItem = { };
    LPCWSTR wzProductName = NULL;
    LPCWSTR wzProductVersion = NULL;
    LPCWSTR wzProductPublicKey = NULL;

    lvItem.mask = LVIF_PARAM;

    // Docs don't indicate any way for it to return failure
    dwValueCount = ::SendMessageW(hwnd, LVM_GETITEMCOUNT, 0, 0);

    for (DWORD i = 0; i < dwValueCount; ++i)
    {
        // If it's selected
        if (::SendMessageW(hwnd, LVM_GETITEMSTATE, i, LVIS_SELECTED))
        {
            lvItem.iItem = i;

            if (!::SendMessageW(hwnd, LVM_GETITEM, 0, reinterpret_cast<LPARAM>(&lvItem)))
            {
                ExitWithLastError(hr, "Failed to get item lparam from list view to delete values");
            }

            hr = CfgEnumReadString(cehProducts, lvItem.lParam, ENUM_DATA_PRODUCTNAME, &wzProductName);
            ExitOnFailure(hr, "Failed to read product name");

            hr = CfgEnumReadString(cehProducts, lvItem.lParam, ENUM_DATA_VERSION, &wzProductVersion);
            ExitOnFailure(hr, "Failed to read product version");

            hr = CfgEnumReadString(cehProducts, lvItem.lParam, ENUM_DATA_PUBLICKEY, &wzProductPublicKey);
            ExitOnFailure(hr, "Failed to read product publickey");

            // Ignore failures - what can we do? User will notice in UI that it's still there
            // TODO: kill this database interaction in UI thread, it's no good
            CfgForgetProduct(cdhHandle, wzProductName, wzProductVersion, wzProductPublicKey);
        }
    }

LExit:
    return hr;
}

HRESULT UISetListViewToValueEnum(
    __in HWND hwnd,
    __in_opt C_CFG_ENUMERATION_HANDLE cehValues,
    __in BOOL fShowDeletedValues
    )
{
    HRESULT hr = S_OK;

    DWORD i;
    SYSTEMTIME st = { };
    SYSTEMTIME stLocal = { };
    TIME_ZONE_INFORMATION tzi = { };
    DWORD dwCount = 0;
    DWORD dwValue = 0;
    DWORD dwListViewRowCount = 0;
    DWORD64 qwValue = 0;
    BOOL fValue = FALSE;
    DWORD dwInsertIndex;
    LPCWSTR wzText = NULL;
    LPWSTR sczText = NULL;
    CONFIG_VALUETYPE cvType = VALUE_INVALID;

    dwListViewRowCount = ListView_GetItemCount(hwnd);

    if (NULL != cehValues)
    {
        hr = CfgEnumReadDword(cehValues, 0, ENUM_DATA_COUNT, &dwCount);
        ExitOnFailure(hr, "Failed to read count of value enumeration");
    }

    if (0 == dwCount)
    {
        hr = UISetListViewText(hwnd, L"No values to display");
        ExitOnFailure(hr, "Failed to set 'no values' text in value listview");

        ExitFunction1(hr = S_OK);
    }

    if (!::GetTimeZoneInformation(&tzi))
    {
        ExitWithLastError(hr, "Failed to get time zone information");
    }

    dwInsertIndex = 0;
    for (i = 0; i < dwCount; ++i)
    {
        hr = CfgEnumReadString(cehValues, i, ENUM_DATA_VALUENAME, &wzText);
        ExitOnFailure(hr, "Failed to read value enumeration");

        if (dwInsertIndex >= dwListViewRowCount)
        {
            hr = UIListViewInsertItem(hwnd, &dwInsertIndex, wzText, i, 0);
            ExitOnFailure(hr, "Failed to insert product into listview control");
        }
        else
        {
            hr = UIListViewSetItem(hwnd, dwInsertIndex, wzText, i, 0);
            ExitOnFailure(hr, "Failed to set product in listview control");
        }

        hr = CfgEnumReadDataType(cehValues, i, ENUM_DATA_VALUETYPE, &cvType);
        ExitOnFailure(hr, "Failed to read type of value from value enumeration");

        hr = UIListViewSetItemText(hwnd, dwInsertIndex, 1, UIGetTypeDisplayName(cvType));
        ExitOnFailure(hr, "Failed to set value as listview subitem");

        switch (cvType)
        {
        case VALUE_BLOB:
            hr = CfgEnumReadDword(cehValues, i, ENUM_DATA_BLOBSIZE, &dwValue);
            ExitOnFailure(hr, "Failed to read blob size from enumeration");

            hr = StrAllocFormatted(&sczText, L"(Size %u)", dwValue);
            ExitOnFailure(hr, "Failed to format DWORD value into string");

            wzText = sczText;
            break;
        case VALUE_STRING:
            hr = CfgEnumReadString(cehValues, i, ENUM_DATA_VALUESTRING, &wzText);
            ExitOnFailure(hr, "Failed to read string value from enumeration");
            break;
        case VALUE_DWORD:
            hr = CfgEnumReadDword(cehValues, i, ENUM_DATA_VALUEDWORD, &dwValue);
            ExitOnFailure(hr, "Failed to read dword value from enumeration");

            hr = StrAllocFormatted(&sczText, L"%u", dwValue);
            ExitOnFailure(hr, "Failed to format DWORD value into string");

            wzText = sczText;
            break;
        case VALUE_QWORD:
            hr = CfgEnumReadQword(cehValues, i, ENUM_DATA_VALUEQWORD, &qwValue);
            ExitOnFailure(hr, "Failed to read qword value from enumeration");

            hr = StrAllocFormatted(&sczText, L"%I64u", qwValue);
            ExitOnFailure(hr, "Failed to format QWORD value into string");

            wzText = sczText;
            break;
        case VALUE_BOOL:
            hr = CfgEnumReadBool(cehValues, i, ENUM_DATA_VALUEBOOL, &fValue);
            ExitOnFailure(hr, "Failed to read bool value from enumeration");

            wzText = fValue ? L"True" : L"False";
            break;

        case VALUE_DELETED:
            if (fShowDeletedValues)
            {
                wzText = L"[Deleted]";
            }
            else
            {
                continue;
            }
        }

        hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, wzText);
        ExitOnFailure(hr, "Failed to set value as listview subitem");

        hr = CfgEnumReadSystemTime(cehValues, i, ENUM_DATA_WHEN, &st);
        ExitOnFailure(hr, "Failed to read when string from value history enumeration");

        if (!SystemTimeToTzSpecificLocalTime(&tzi, &st, &stLocal))
        {
            ExitWithLastError(hr, "Failed to convert systemtime to local time");
        }

        hr = TimeSystemToDateTimeString(&sczText, &stLocal, LOCALE_USER_DEFAULT);
        ExitOnFailure(hr, "Failed to convert value 'when' time to text");

        hr = UIListViewSetItemText(hwnd, dwInsertIndex, 3, sczText);
        ExitOnFailure(hr, "Failed to set when string as listview subitem");

        ++dwInsertIndex;
    }

    UIListViewTrimSize(hwnd, dwInsertIndex);

    hr = ListViewSort(hwnd);
    ExitOnFailure(hr, "Failed to sort listview");

    EnableWindow(hwnd, TRUE);

LExit:
    ReleaseStr(sczText);

    return hr;
}

HRESULT UISetListViewToValueHistoryEnum(
    __in HWND hwnd,
    __in_opt C_CFG_ENUMERATION_HANDLE cehValueHistory
    )
{
    HRESULT hr = S_OK;

    DWORD dwListViewRowCount = 0;
    DWORD dwCount = 0;
    DWORD dwValue = 0;
    DWORD64 qwValue = 0;
    BOOL fValue = FALSE;
    DWORD dwEnumReadIndex;
    DWORD dwInsertIndex;
    LPCWSTR wzText = NULL;
    LPWSTR sczText = NULL;
    SYSTEMTIME st = { };
    SYSTEMTIME stLocal = { };
    TIME_ZONE_INFORMATION tzi = { };
    CONFIG_VALUETYPE cvType = VALUE_INVALID;

    dwListViewRowCount = ListView_GetItemCount(hwnd);

    if (NULL != cehValueHistory)
    {
        hr = CfgEnumReadDword(cehValueHistory, 0, ENUM_DATA_COUNT, &dwCount);
        ExitOnFailure(hr, "Failed to read count of value history enumeration");
    }

    if (0 == dwCount)
    {
        hr = UISetListViewText(hwnd, L"No value history to display");
        ExitOnFailure(hr, "Failed to set 'no value history' text in value history listview");

        ExitFunction1(hr = S_OK);
    }

    if (!::GetTimeZoneInformation(&tzi))
    {
        ExitWithLastError(hr, "Failed to get time zone information");
    }

    UIListViewTrimSize(hwnd, dwCount);
    for (DWORD i = 0; i < dwCount; ++i)
    {
        dwEnumReadIndex = dwCount - i - 1;
        dwInsertIndex = i;

        hr = CfgEnumReadDataType(cehValueHistory, dwEnumReadIndex, ENUM_DATA_VALUETYPE, &cvType);
        ExitOnFailure(hr, "Failed to read type of value from value history enumeration");

        switch (cvType)
        {
        case VALUE_DELETED:
            wzText = L"";
            break;
        case VALUE_BLOB:
            hr = CfgEnumReadDword(cehValueHistory, dwEnumReadIndex, ENUM_DATA_BLOBSIZE, &dwValue);
            ExitOnFailure(hr, "Failed to read blob size from enumeration");

            hr = StrAllocFormatted(&sczText, L"(Size %u)", dwValue);
            ExitOnFailure(hr, "Failed to format DWORD value into string");

            wzText = sczText;
            break;
        case VALUE_STRING:
            hr = CfgEnumReadString(cehValueHistory, dwEnumReadIndex, ENUM_DATA_VALUESTRING, &wzText);
            ExitOnFailure(hr, "Failed to read string value from value history enumeration");
            break;
        case VALUE_DWORD:
            hr = CfgEnumReadDword(cehValueHistory, dwEnumReadIndex, ENUM_DATA_VALUEDWORD, &dwValue);
            ExitOnFailure(hr, "Failed to read dword value from value history enumeration");

            hr = StrAllocFormatted(&sczText, L"%u", dwValue);
            ExitOnFailure(hr, "Failed to format DWORD value into string");

            wzText = sczText;
            break;
        case VALUE_QWORD:
            hr = CfgEnumReadQword(cehValueHistory, dwEnumReadIndex, ENUM_DATA_VALUEQWORD, &qwValue);
            ExitOnFailure(hr, "Failed to read qword value from value history enumeration");

            hr = StrAllocFormatted(&sczText, L"%I64u", qwValue);
            ExitOnFailure(hr, "Failed to format QWORD value into string");

            wzText = sczText;
            break;
        case VALUE_BOOL:
            hr = CfgEnumReadBool(cehValueHistory, dwEnumReadIndex, ENUM_DATA_VALUEBOOL, &fValue);
            ExitOnFailure(hr, "Failed to read bool value from value history enumeration");

            wzText = fValue ? L"True" : L"False";
            break;
        }

        if (dwInsertIndex >= dwListViewRowCount)
        {
            hr = UIListViewInsertItem(hwnd, &dwInsertIndex, UIGetTypeDisplayName(cvType), i, 0);
            ExitOnFailure(hr, "Failed to insert product into listview control");
        }
        else
        {
            hr = UIListViewSetItem(hwnd, dwInsertIndex, UIGetTypeDisplayName(cvType), i, 0);
            ExitOnFailure(hr, "Failed to set product in listview control");
        }

        hr = UIListViewSetItemText(hwnd, dwInsertIndex, 1, wzText);
        ExitOnFailure(hr, "Failed to insert value name into listview control");

        hr = CfgEnumReadString(cehValueHistory, dwEnumReadIndex, ENUM_DATA_DATABASE_REFERENCES, &wzText);
        ExitOnFailure(hr, "Failed to read database references from value history enumeration");

        // Raw string is not very useful, just list whether it is referenced by someone or not
        fValue = (wzText && *wzText != L'\0');
        hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, fValue ? L"Yes" : L"No");
        ExitOnFailure(hr, "Failed to insert value name into listview control");

        hr = CfgEnumReadString(cehValueHistory, dwEnumReadIndex, ENUM_DATA_BY, &wzText);
        ExitOnFailure(hr, "Failed to read by string from value history enumeration");

        hr = UIListViewSetItemText(hwnd, dwInsertIndex, 3, wzText);
        ExitOnFailure(hr, "Failed to set value as listview subitem");

        hr = CfgEnumReadSystemTime(cehValueHistory, dwEnumReadIndex, ENUM_DATA_WHEN, &st);
        ExitOnFailure(hr, "Failed to read when string from value history enumeration");

        if (!SystemTimeToTzSpecificLocalTime(&tzi, &st, &stLocal))
        {
            ExitWithLastError(hr, "Failed to convert systemtime to local time");
        }

        hr = TimeSystemToDateTimeString(&sczText, &stLocal, LOCALE_USER_DEFAULT);
        ExitOnFailure(hr, "Failed to convert value history time to text");

        hr = UIListViewSetItemText(hwnd, dwInsertIndex, 4, sczText);
        ExitOnFailure(hr, "Failed to set value as listview subitem");
    }

    EnableWindow(hwnd, TRUE);

LExit:
    ReleaseStr(sczText);

    return hr;
}

HRESULT UIDeleteValuesFromListView(
    __in HWND hwnd,
    __in CFGDB_HANDLE cdhHandle,
    __in C_CFG_ENUMERATION_HANDLE cehValues
    )
{
    HRESULT hr = S_OK;
    DWORD dwValueCount;
    LVITEM lvItem = { };
    LPCWSTR wzValueName = NULL;

    lvItem.mask = LVIF_PARAM;

    // Docs don't indicate any way for it to return failure
    dwValueCount = ::SendMessageW(hwnd, LVM_GETITEMCOUNT, 0, 0);

    for (DWORD i = 0; i < dwValueCount; ++i)
    {
        // If it's selected
        if (::SendMessageW(hwnd, LVM_GETITEMSTATE, i, LVIS_SELECTED))
        {
            lvItem.iItem = i;

            if (!::SendMessageW(hwnd, LVM_GETITEM, 0, reinterpret_cast<LPARAM>(&lvItem)))
            {
                ExitWithLastError(hr, "Failed to get item lparam from list view to delete values");
            }

            hr = CfgEnumReadString(cehValues, lvItem.lParam, ENUM_DATA_VALUENAME, &wzValueName);
            ExitOnFailure(hr, "Failed to read valuename from enumeration while deleting values");

            // Ignore failures - what can we do? User will notice in UI that it's still there
            // TODO: kill this database interaction in UI thread, it's no good
            CfgDeleteValue(cdhHandle, wzValueName);
        }
    }

LExit:
    return hr;
}

HRESULT UISetValueConflictsFromListView(
    __in HWND hwnd,
    __in_z LPCWSTR wzDatabaseName,
    __in CONFLICT_PRODUCT *pcpProductConflict,
    __in RESOLUTION_CHOICE rcChoice
    )
{
    HRESULT hr = S_OK;
    DWORD cValueCount;
    LVITEM lvItem = { };
    LPCWSTR wzText;

    wzText = UIGetResolutionText(rcChoice, wzDatabaseName);
    lvItem.mask = LVIF_PARAM;

    // Docs don't indicate any way for it to return failure
    cValueCount = ::SendMessageW(hwnd, LVM_GETITEMCOUNT, 0, 0);

    for (DWORD i = 0; i < cValueCount; ++i)
    {
        // If it's selected
        if (::SendMessageW(hwnd, LVM_GETITEMSTATE, i, LVIS_SELECTED))
        {
            lvItem.iItem = i;

            if (!::SendMessageW(hwnd, LVM_GETITEM, 0, reinterpret_cast<LPARAM>(&lvItem)))
            {
                ExitWithLastError(hr, "Failed to get item lparam from list view to set product conflicts");
            }

            if (0 > lvItem.lParam || pcpProductConflict->cValues <= static_cast<DWORD>(lvItem.lParam))
            {
                hr = E_UNEXPECTED;
                ExitOnFailure(hr, "Invalid param %d stored in listview!", lvItem.lParam);
            }

            pcpProductConflict->rgrcValueChoices[lvItem.lParam] = rcChoice;

            hr = UIListViewSetItemText(hwnd, i, 1, wzText);
            ExitOnFailure(hr, "Failed to set text in column 1 of listview control");
        }
    }

LExit:
    return hr;
}

HRESULT UISetProductConflictsFromListView(
    __in HWND hwnd,
    __in_z LPCWSTR wzDatabaseName,
    __in_ecount(cProductCount) CONFLICT_PRODUCT *pcplProductConflictList,
    __in DWORD cProductCount,
    __in RESOLUTION_CHOICE rcChoice
    )
{
    HRESULT hr = S_OK;
    DWORD cListViewItemCount = 0;
    LVITEM lvItem = { };
    LPCWSTR wzText;

    wzText = UIGetResolutionText(rcChoice, wzDatabaseName);
    lvItem.mask = LVIF_PARAM;

    // Docs don't indicate any way for it to return failure
    cListViewItemCount = ::SendMessageW(hwnd, LVM_GETITEMCOUNT, 0, 0);

    for (DWORD i = 0; i < cListViewItemCount; ++i)
    {
        // If it's selected
        if (::SendMessageW(hwnd, LVM_GETITEMSTATE, i, LVIS_SELECTED))
        {
            lvItem.iItem = i;

            if (!::SendMessageW(hwnd, LVM_GETITEM, 0, reinterpret_cast<LPARAM>(&lvItem)))
            {
                ExitWithLastError(hr, "Failed to get item lparam from list view to set product conflicts");
            }
            
            if (0 > lvItem.lParam || cProductCount <= static_cast<DWORD>(lvItem.lParam))
            {
                hr = E_UNEXPECTED;
                ExitOnFailure(hr, "Invalid param %d stored in listview!", lvItem.lParam);
            }

            for (DWORD j = 0; j < pcplProductConflictList[lvItem.lParam].cValues; ++j)
            {
                pcplProductConflictList[lvItem.lParam].rgrcValueChoices[j] = rcChoice;
            }

            hr = UIListViewSetItemText(hwnd, i, 1, wzText);
            ExitOnFailure(hr, "Failed to set text in column 1 of listview control");
        }
    }

LExit:
    return hr;
}

HRESULT UISetListViewToValueConflictArray(
    __in HWND hwnd,
    __in LPCWSTR wzDatabaseName,
    __in_ecount(cConflictValueCount) C_CFG_ENUMERATION_HANDLE *pcehHandle,
    __in_ecount(cConflictValueCount) const RESOLUTION_CHOICE *rgrcValueChoices,
    __in DWORD cConflictValueCount
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzValueName = NULL;
    DWORD i;
    DWORD dwInsertIndex;

    if (!::SendMessageW(hwnd, LVM_DELETEALLITEMS, 0, 0))
    {
        ExitWithLastError(hr, "Failed to delete all items from list view");
    }
    EnableWindow(hwnd, TRUE);

    for (i = 0; i < cConflictValueCount; ++i)
    {
        dwInsertIndex = i;

        hr = CfgEnumReadString(pcehHandle[i], 0, ENUM_DATA_VALUENAME, &wzValueName);
        ExitOnFailure(hr, "Failed to read value name from value conflict array");

        hr = UIListViewInsertItem(hwnd, &dwInsertIndex, wzValueName, i, 0);
        ExitOnFailure(hr, "Failed to insert value name into value conflict listview control");

        switch (rgrcValueChoices[i])
        {
        case RESOLUTION_UNRESOLVED:
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 1, L"(Unresolved)");
            ExitOnFailure(hr, "Failed to set text in column 1 of listview control");
            break;
        case RESOLUTION_LOCAL:
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 1, L"Mine");
            ExitOnFailure(hr, "Failed to set text in column 1 of listview control");
            break;
        case RESOLUTION_REMOTE:
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 1, wzDatabaseName);
            ExitOnFailure(hr, "Failed to set text in column 1 of listview control");
            break;
        default:
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected resolution type encountered!");
            break;
        }
    }

LExit:
    return hr;
}

HRESULT UISetListViewToProductConflictArray(
    __in HWND hwnd,
    __in LPCWSTR wzDatabaseName,
    __in HRESULT hrSyncResult,
    __in_ecount(dwConflictProductCount) const CONFLICT_PRODUCT *pcpProductConflict,
    __in DWORD dwConflictProductCount
    )
{
    HRESULT hr = S_OK;
    DWORD dwInsertIndex;
    DWORD dwDisplayNameToDisplay = DWORD_MAX;
    LPCWSTR wzText = NULL;
    RESOLUTION_CHOICE rcChoice;
    BOOL fConsistentChoice;

    if (FAILED(hrSyncResult))
    {
        EnableWindow(hwnd, FALSE);
        hr = UISetListViewText(hwnd, L"Error synchronizing!");
        ExitOnFailure(hr, "Failed to set 'error synchronizing' text in product conflict listview");

        ExitFunction1(hr = S_OK);
    }
    if (0 == dwConflictProductCount)
    {
        EnableWindow(hwnd, FALSE);
        hr = UISetListViewText(hwnd, L"No conflicts to display");
        ExitOnFailure(hr, "Failed to set 'no conflicts' text in product conflict listview");

        ExitFunction1(hr = S_OK);
    }

    // Clear out and enable the listview
    if (!::SendMessageW(hwnd, LVM_DELETEALLITEMS, 0, 0))
    {
        ExitWithLastError(hr, "Failed to delete all items from list view");
    }
    EnableWindow(hwnd, TRUE);

    for (DWORD i = 0; i < dwConflictProductCount; ++i)
    {
        dwInsertIndex = i;

        fConsistentChoice = TRUE;

        // Take the first value's resolution, first
        if (1 <= pcpProductConflict[i].cValues)
        {
            rcChoice = pcpProductConflict[i].rgrcValueChoices[0];
        }
        else
        {
            rcChoice = RESOLUTION_UNRESOLVED;
        }

        // Then iterate to see if any other ones are different
        for (DWORD j = 1; j < pcpProductConflict[i].cValues; ++j)
        {
            if (rcChoice != pcpProductConflict[i].rgrcValueChoices[j])
            {
                fConsistentChoice = FALSE;
                break;
            }
        }

        hr = UISelectBestLCIDToDisplay(pcpProductConflict[i].rgDisplayNames, pcpProductConflict[i].cDisplayNames, &dwDisplayNameToDisplay);
        if (FAILED(hr))
        {
            hr = S_OK;

            // Fallback to regular product id
            wzText = pcpProductConflict[i].sczProductName;
        }
        else
        {
            wzText = pcpProductConflict[i].rgDisplayNames[dwDisplayNameToDisplay].sczName;
        }

        hr = UIListViewInsertItem(hwnd, &dwInsertIndex, wzText, i, 0);
        ExitOnFailure(hr, "Failed to insert value name into listview control");

        if (!fConsistentChoice)
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 1, RESOLVE_MIXED_TEXT);
            ExitOnFailure(hr, "Failed to set text in column 1 of listview control");
        }
        else
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 1, UIGetResolutionText(rcChoice, wzDatabaseName));
            ExitOnFailure(hr, "Failed to set text in column 1 of listview control");
        }
    }

LExit:
    return hr;
}

HRESULT UIListViewInsertItem(
    __in const HWND hwnd,
    __inout DWORD *pdwIndex,
    __in LPCWSTR sczText,
    __in DWORD dwParam,
    __in DWORD dwImage
    )
{
    HRESULT hr = S_OK;
    LONG lRetVal = 0;
    LV_ITEMW lvi = { };

    lvi.mask = LVIF_PARAM | LVIF_TEXT | LVIF_IMAGE;
    lvi.iItem = *pdwIndex;
    lvi.iSubItem = 0;
    lvi.iImage = dwImage;
    lvi.lParam = dwParam;
    lvi.pszText = const_cast<LPWSTR>(sczText);

    lRetVal = ::SendMessageW(hwnd, LVM_INSERTITEMW, 0, reinterpret_cast<LPARAM>(&lvi));
    if (-1 == lRetVal)
    {
        ExitWithLastError(hr, "Failed to insert row into listview");
    }
    else
    {
        *pdwIndex = lRetVal;
    }

LExit:
    return hr;
}


HRESULT UIListViewSetItemText(
    __in const HWND hwnd,
    __in DWORD dwIndex,
    __in DWORD dwColumnIndex,
    __in LPCWSTR sczText
    )
{
    HRESULT hr = S_OK;
    LV_ITEMW lvi = { };

    lvi.iSubItem = dwColumnIndex;
    lvi.pszText = const_cast<LPWSTR>(sczText);

    if (-1 == ::SendMessageW(hwnd, LVM_SETITEMTEXTW, static_cast<LPARAM>(dwIndex), reinterpret_cast<LPARAM>(&lvi)))
    {
        ExitWithLastError(hr, "Failed to set field in listview - row %u, column %u", dwIndex, dwColumnIndex);
    }

LExit:
    return hr;
}

HRESULT UIListViewSetItem(
    __in const HWND hwnd,
    __in DWORD dwIndex,
    __in LPCWSTR sczText,
    __in DWORD dwParam,
    __in DWORD dwImage
    )
{
    HRESULT hr = S_OK;
    LV_ITEMW lvi = { };

    lvi.mask = LVIF_PARAM | LVIF_TEXT | LVIF_IMAGE;
    lvi.iItem = dwIndex;
    lvi.iSubItem = 0;
    lvi.iImage = dwImage;
    lvi.lParam = dwParam;
    lvi.pszText = const_cast<LPWSTR>(sczText);

    if (-1 == ::SendMessageW(hwnd, LVM_SETITEMW, static_cast<LPARAM>(dwIndex), reinterpret_cast<LPARAM>(&lvi)))
    {
        ExitWithLastError(hr, "Failed to set first column item in listview - row %u", dwIndex);
    }

LExit:
    return hr;
}

void UIListViewTrimSize(
    __in const HWND hwnd,
    __in DWORD dwNewRowCount
    )
{
    DWORD dwListViewRowCount = ::SendMessageW(hwnd, LVM_GETITEMCOUNT, 0, 0);

    for (DWORD i = dwNewRowCount; i < dwListViewRowCount; ++i)
    {
        // Yes, we keep deleting the same index until we've deleted enough items
        ListView_DeleteItem(hwnd, dwNewRowCount);
    }
}

HRESULT UIExportFile(
    __in const HWND hwnd,
    __out_z LPWSTR *psczPath
    )
{
    HRESULT hr = S_OK;
    OPENFILENAMEW ofn = { };

    ofn.lStructSize = sizeof(ofn);
    ofn.hwndOwner = hwnd;
    hr = StrAlloc(&ofn.lpstrFile, NUM_FILE_SELECTION_DIALOG_CHARACTERS);
    ExitOnFailure(hr, "Failed to allocate space for file selection dialog while exporting file");
    ofn.nMaxFile = NUM_FILE_SELECTION_DIALOG_CHARACTERS;
    ofn.lpstrFilter = L"All Files\0*.*\0";
    ofn.nFilterIndex = 1;
    ofn.Flags = OFN_OVERWRITEPROMPT;
    ofn.lpstrTitle = L"Export File";

    if (::GetOpenFileNameW(&ofn))
    {
        *psczPath = ofn.lpstrFile;
        ofn.lpstrFile = NULL;
    }

LExit:
    ReleaseStr(ofn.lpstrFile);

    return hr;
}

HRESULT UIMessageBoxDisplayError(
    __in HWND hwnd,
    __in LPCWSTR wzErrorMessage,
    __in HRESULT hrDisplay
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDisplayMessage = NULL;
    
    hr = StrAllocFormatted(&sczDisplayMessage, L"HRESULT 0x%x - %ls",hrDisplay, wzErrorMessage);
    if (SUCCEEDED(hr))
    {
        ::MessageBoxW(hwnd, sczDisplayMessage, L"Error", MB_OK | MB_ICONERROR);
    }
    else
    {
        ::MessageBoxW(hwnd, L"Error encountered, and due to extreme conditions (such as lack of memory) the error code could not be displayed.", L"Error", MB_OK | MB_ICONERROR);
    }

    ReleaseStr(sczDisplayMessage);

    return hr;
}

HRESULT UISelectBestLCIDToDisplay(
    __in DISPLAY_NAME *rgDisplayNames,
    __in DWORD cDisplayNames,
    __out DWORD *pdwIndex
    )
{
    // Try to select English for now. TODO: figure out what language the user would like to display, and prefer that, with appropriate fallbacks if unavailable.
    for (DWORD i = 0; i < cDisplayNames; ++i)
    {
        if (1033 == rgDisplayNames[i].dwLCID)
        {
            *pdwIndex = i;
            return S_OK;
        }
    }

    if (0 < cDisplayNames)
    {
        *pdwIndex = 0;
        return S_OK;
    }

    return E_NOTFOUND;
}

static HRESULT ListViewSort(
    __in HWND hwnd
    )
{
    UIClearSelectionFromListView(hwnd);

    ListView_SortItemsEx(hwnd, ListViewItemCompare, reinterpret_cast<LPARAM>(hwnd));

    return S_OK;
}

static int CALLBACK ListViewItemCompare(
    __in LPARAM lParam1,
    __in LPARAM lParam2,
    __in LPARAM lParamSort
    )
{
    HWND hwnd = reinterpret_cast<HWND>(lParamSort);
    LVITEMW item1 = { };
    LVITEMW item2 = { };
    WCHAR wzString1[256];
    WCHAR wzString2[256];

    item1.iSubItem = 0;
    item1.pszText = wzString1;
    item1.cchTextMax = sizeof(wzString1)/sizeof(WCHAR);
    item2.iSubItem = 0;
    item2.pszText = wzString2;
    item2.cchTextMax = sizeof(wzString2)/sizeof(WCHAR);

    ::SendMessage(hwnd, LVM_GETITEMTEXTW, lParam1, reinterpret_cast<LPARAM>(&item1));
    ::SendMessage(hwnd, LVM_GETITEMTEXTW, lParam2, reinterpret_cast<LPARAM>(&item2));

    return wcscmp(wzString1, wzString2);
}
