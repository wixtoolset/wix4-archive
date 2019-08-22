// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define DATABASE(i) (m_pbdlDatabaseList->rgDatabases[i])
#define CURRENTDATABASE (DATABASE(m_dwDatabaseIndex))
#define UXDATABASE(i) (pUX->m_pbdlDatabaseList->rgDatabases[i])
#define CURRENTUXDATABASE (UXDATABASE(pUX->m_dwDatabaseIndex))

//
// Initialize - ensure all the necessary objects are created/initialized.
//
STDMETHODIMP BrowseWindow::Initialize(
    __out DWORD *pdwUIThread
    )
{
    HRESULT hr = S_OK;

    m_dwWorkThreadId = ::GetCurrentThreadId();

    // create UI thread
    m_hUiThread = ::CreateThread(NULL, 0, UiThreadProc, this, 0, pdwUIThread);
    if (!m_hUiThread)
    {
        ExitWithLastError(hr, "Failed to create UI thread.");
    }

LExit:
    return hr;
}

STDMETHODIMP_(void) BrowseWindow::Uninitialize()
{
    // wait for UX thread to terminate
    if (m_hUiThread)
    {
        ::WaitForSingleObject(m_hUiThread, INFINITE);
        ::CloseHandle(m_hUiThread);
    }
}

HRESULT BrowseWindow::SetSelectedProduct(
    __in DWORD dwIndex
    )
{
    HRESULT hr = S_OK;
    DISPLAY_NAME *rgDisplayNames = NULL;
    DWORD cDisplayNames = 0;
    DWORD dwDisplayNameToDisplay = DWORD_MAX;
    LPCWSTR wzTemp = NULL;
    ::EnterCriticalSection(&CURRENTDATABASE.cs);

    hr = CfgEnumReadDisplayNameArray(CURRENTDATABASE.productEnum.cehItems, dwIndex, &rgDisplayNames, &cDisplayNames);
    ExitOnFailure(hr, "Failed to read display names from enumeration");

    hr = CfgEnumReadString(CURRENTDATABASE.productEnum.cehItems, dwIndex, ENUM_DATA_PRODUCTNAME, &wzTemp);
    ExitOnFailure(hr, "Failed to read product name from enumeration");

    hr = StrAllocString(&CURRENTDATABASE.prodCurrent.sczName, wzTemp, 0);
    ExitOnFailure(hr, "Failed to copy name");

    hr = CfgEnumReadString(CURRENTDATABASE.productEnum.cehItems, dwIndex, ENUM_DATA_VERSION, &wzTemp);
    ExitOnFailure(hr, "Failed to read product version from enumeration");

    hr = StrAllocString(&CURRENTDATABASE.prodCurrent.sczVersion, wzTemp, 0);
    ExitOnFailure(hr, "Failed to copy name");

    hr = CfgEnumReadString(CURRENTDATABASE.productEnum.cehItems, dwIndex, ENUM_DATA_PUBLICKEY, &wzTemp);
    ExitOnFailure(hr, "Failed to read product public key from enumeration");

    hr = StrAllocString(&CURRENTDATABASE.prodCurrent.sczPublicKey, wzTemp, 0);
    ExitOnFailure(hr, "Failed to copy name");

    hr = UISelectBestLCIDToDisplay(rgDisplayNames, cDisplayNames, &dwDisplayNameToDisplay);
    if (FAILED(hr))
    {
        hr = S_OK;

        // Fallback to regular product id/version/publickey
        hr = StrAllocFormatted(&CURRENTDATABASE.sczCurrentProductDisplayName, L"%ls, %ls, %ls", CURRENTDATABASE.prodCurrent.sczName, CURRENTDATABASE.prodCurrent.sczVersion, CURRENTDATABASE.prodCurrent.sczPublicKey);
        ExitOnFailure(hr, "Failed to allocate product display name with public key");
    }
    else
    {
        hr = StrAllocString(&CURRENTDATABASE.sczCurrentProductDisplayName, rgDisplayNames[0].sczName, 0);
        ExitOnFailure(hr, "Failed to copy display name");
    }

LExit:
    ::LeaveCriticalSection(&CURRENTDATABASE.cs);

    return hr;
}

void BrowseWindow::Bomb(
    HRESULT hr
    )
{
    UIMessageBoxDisplayError(m_hWnd, L"Unrecoverable error encountered - exiting application!", hr);

    ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
}

HRESULT BrowseWindow::ReadSettings()
{
    HRESULT hr = S_OK;
    HRESULT hrTemp = S_OK;
    CFGDB_HANDLE cdbLocal = NULL;
    BOOL fTemp = FALSE;
    BOOL fSettingsChanged = FALSE;

    ::EnterCriticalSection(&DATABASE(m_dwLocalDatabaseIndex).cs);
    cdbLocal = DATABASE(m_dwLocalDatabaseIndex).cdb;

    hr = CfgSetProduct(cdbLocal, wzBrowserProductId, wzBrowserVersion, wzBrowserPublicKey);
    ExitOnFailure(hr, "Failed to set product id to browser");

    hr = CfgGetBool(cdbLocal, BROWSER_SETTING_SHOW_UNINSTALLED_PRODUCTS, &fTemp);
    if (E_NOTFOUND == hr)
    {
        hr = CfgSetBool(cdbLocal, BROWSER_SETTING_SHOW_UNINSTALLED_PRODUCTS, FALSE);
        ExitOnFailure(hr, "Failed to set uninstalled products flag to default value");

        m_fShowUninstalledProducts = FALSE;
        hr = S_OK;
    }
    else
    {
        ExitOnFailure(hr, "Failed to read show uninstalled products setting");

        if (fTemp != m_fShowUninstalledProducts)
        {
            m_fShowUninstalledProducts = fTemp;
            fSettingsChanged = TRUE;
        }
    }

    hr = CfgGetBool(cdbLocal, BROWSER_SETTING_SHOW_DELETED_VALUES, &fTemp);
    if (E_NOTFOUND == hr)
    {
        hr = CfgSetBool(cdbLocal, BROWSER_SETTING_SHOW_DELETED_VALUES, FALSE);
        ExitOnFailure(hr, "Failed to set show deleted values flag to default value");

        m_fShowDeletedValues = FALSE;
        hr = S_OK;
    }
    else
    {
        ExitOnFailure(hr, "Failed to read show deleted values setting");

        if (fTemp != m_fShowDeletedValues)
        {
            m_fShowDeletedValues = fTemp;
            fSettingsChanged = TRUE;
        }
    }

    if (fSettingsChanged && !::PostMessageW(m_hWnd, WM_BROWSE_SETTINGS_CHANGED, 0, 0))
    {
        ExitWithLastError(hr, "Failed to send WM_BROWSE_SETTINGS_CHANGED message");
    }

LExit:
    // Revert to prior product if one was set, even in case of some kind of failure
    if (NULL != DATABASE(m_dwLocalDatabaseIndex).prodCurrent.sczName)
    {
        hrTemp = CfgSetProduct(cdbLocal, DATABASE(m_dwLocalDatabaseIndex).prodCurrent.sczName, DATABASE(m_dwLocalDatabaseIndex).prodCurrent.sczVersion, DATABASE(m_dwLocalDatabaseIndex).prodCurrent.sczPublicKey);
        if (FAILED(hrTemp))
        {
            LogErrorString(hrTemp, "Failed to revert to prior product when persisting settings");
        }
    }
    ::LeaveCriticalSection(&DATABASE(m_dwLocalDatabaseIndex).cs);

    return hr;
}

HRESULT BrowseWindow::PersistSettings()
{
    HRESULT hr = S_OK;
    HRESULT hrTemp = S_OK;
    CFGDB_HANDLE cdbLocal = NULL;

    ::EnterCriticalSection(&DATABASE(m_dwLocalDatabaseIndex).cs);
    cdbLocal = DATABASE(m_dwLocalDatabaseIndex).cdb;

    hr = CfgSetProduct(cdbLocal, wzBrowserProductId, wzBrowserVersion, wzBrowserPublicKey);
    ExitOnFailure(hr, "Failed to set product id to browser");

    hr = CfgSetBool(cdbLocal, BROWSER_SETTING_SHOW_UNINSTALLED_PRODUCTS, m_fShowUninstalledProducts);
    ExitOnFailure(hr, "Failed to set show uninstalled products setting");

    hr = CfgSetBool(cdbLocal, BROWSER_SETTING_SHOW_DELETED_VALUES, m_fShowDeletedValues);
    ExitOnFailure(hr, "Failed to set show deleted values setting");

LExit:
    // Revert to prior product if one was set, even in case of some kind of failure
    if (NULL != DATABASE(m_dwLocalDatabaseIndex).prodCurrent.sczName)
    {
        hrTemp = CfgSetProduct(cdbLocal, DATABASE(m_dwLocalDatabaseIndex).prodCurrent.sczName, DATABASE(m_dwLocalDatabaseIndex).prodCurrent.sczVersion, DATABASE(m_dwLocalDatabaseIndex).prodCurrent.sczPublicKey);
        if (FAILED(hrTemp))
        {
            LogErrorString(hrTemp, "Failed to revert to prior product when persisting settings");
        }
    }
    ::LeaveCriticalSection(&DATABASE(m_dwLocalDatabaseIndex).cs);

    return hr;
}

HRESULT BrowseWindow::EnumerateProducts(
    DWORD dwIndex
    )
{
    HRESULT hr = S_OK;

    m_pbdlDatabaseList->rgDatabases[dwIndex].productEnum.fRefreshing = TRUE;
    m_pbdlDatabaseList->rgDatabases[dwIndex].productEnum.hrResult = S_OK;
    m_pbdlDatabaseList->rgDatabases[dwIndex].fProductSet = FALSE;

    hr = RefreshProductList(dwIndex);
    ExitOnFailure(hr, "Failed to update product list");

    if (!::PostThreadMessageW(m_dwWorkThreadId, WM_BROWSE_ENUMERATE_PRODUCTS, static_cast<WPARAM>(dwIndex), 0))
    {
        ExitWithLastError(hr, "Failed to send message to worker thread to enumerate products");
    }

LExit:
    return hr;
}

HRESULT BrowseWindow::EnumerateDatabases(
    DWORD dwIndex
    )
{
    HRESULT hr = S_OK;

    m_pbdlDatabaseList->rgDatabases[dwIndex].dbEnum.fRefreshing = TRUE;
    m_pbdlDatabaseList->rgDatabases[dwIndex].dbEnum.hrResult = S_OK;

    hr = RefreshOtherDatabaseList();
    ExitOnFailure(hr, "Failed to refresh database list");

    if (!::PostThreadMessageW(m_dwWorkThreadId, WM_BROWSE_ENUMERATE_DATABASES, static_cast<WPARAM>(dwIndex), 0))
    {
        ExitWithLastError(hr, "Failed to send message to worker thread to enumerate databases");
    }

LExit:
    return hr;
}

HRESULT BrowseWindow::EnumerateValues(
    DWORD dwIndex,
    BOOL fDifferentProduct
    )
{
    HRESULT hr = S_OK;

    m_pbdlDatabaseList->rgDatabases[dwIndex].valueEnum.fRefreshing = TRUE;
    m_pbdlDatabaseList->rgDatabases[dwIndex].valueEnum.hrResult = S_OK;

    // If we're changing to a different product, clear out the current enumeration so we don't show inaccurate data
    if (fDifferentProduct)
    {
        UtilWipeEnum(m_pbdlDatabaseList->rgDatabases + dwIndex, &m_pbdlDatabaseList->rgDatabases[dwIndex].valueEnum);
    }

    hr = RefreshValueList(dwIndex);
    ExitOnFailure(hr, "Failed to update value list");

    if (!::PostThreadMessageW(m_dwWorkThreadId, WM_BROWSE_ENUMERATE_VALUES, static_cast<WPARAM>(dwIndex), static_cast<LPARAM>(VALUE_ANY_TYPE | VALUE_DELETED)))
    {
        ExitWithLastError(hr, "Failed to send message to worker thread to enumerate values");
    }

LExit:
    return hr;
}

HRESULT BrowseWindow::EnumerateValueHistory(
    DWORD dwIndex,
    BOOL fDifferentValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValueName = NULL;

    m_pbdlDatabaseList->rgDatabases[dwIndex].vhmValueHistoryMode = HISTORY_NORMAL;
    m_pbdlDatabaseList->rgDatabases[dwIndex].valueHistoryEnum.fRefreshing = TRUE;
    m_pbdlDatabaseList->rgDatabases[dwIndex].valueHistoryEnum.hrResult = S_OK;

    // If we're changing to a different value, clear out the current enumeration so we don't show inaccurate data
    if (fDifferentValue)
    {
        UtilWipeEnum(m_pbdlDatabaseList->rgDatabases + dwIndex, &m_pbdlDatabaseList->rgDatabases[dwIndex].valueHistoryEnum);
    }

    hr = RefreshValueHistoryList(dwIndex);
    ExitOnFailure(hr, "Failed to update value history list");

    if (NULL != CURRENTDATABASE.sczValueName)
    {
        hr = StrAllocString(&sczValueName, CURRENTDATABASE.sczValueName, 0);
        ExitOnFailure(hr, "Failed to copy value name for WM_BROWSE_ENUMERATE_VALUE_HISTORY message");

        if (!::PostThreadMessageW(m_dwWorkThreadId, WM_BROWSE_ENUMERATE_VALUE_HISTORY, static_cast<WPARAM>(dwIndex), reinterpret_cast<LPARAM>(sczValueName)))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to enumerate value history");
        }
        sczValueName = NULL;
    }

LExit:
    ReleaseStr(sczValueName);

    return hr;
}

DWORD BrowseWindow::GetSelectedValueIndex()
{
    DWORD dwRetVal = DWORD_MAX;

    // Ignore errors
    UIGetSingleSelectedItemFromListView(::GetDlgItem(m_hWnd, BROWSE_CONTROL_VALUE_LIST_VIEW), NULL, &dwRetVal);

    return dwRetVal;
}

DWORD BrowseWindow::GetSelectedValueHistoryIndex()
{
    HWND hwnd = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_SINGLE_VALUE_HISTORY_LIST_VIEW);

    DWORD dwSelectionIndex = DWORD_MAX;
    DWORD dwListViewRowCount = ::SendMessageW(hwnd, LVM_GETITEMCOUNT, 0, 0);

    // Ignore errors
    UIGetSingleSelectedItemFromListView(hwnd, NULL, &dwSelectionIndex);

    if (DWORD_MAX == dwSelectionIndex || dwListViewRowCount <= dwSelectionIndex)
    {
        // Something weird, indicate error
        return DWORD_MAX;
    }
    else
    {
        return dwListViewRowCount - dwSelectionIndex - 1;
    }
}

DWORD BrowseWindow::GetSelectedConflictProductIndex()
{
    DWORD dwRetVal = DWORD_MAX;

    // Ignore errors
    UIGetSingleSelectedItemFromListView(::GetDlgItem(m_hWnd, BROWSE_CONTROL_SINGLE_DB_CONFLICTS_VIEW), NULL, &dwRetVal);

    return dwRetVal;
}

DWORD BrowseWindow::GetSelectedConflictValueIndex()
{
    DWORD dwRetVal = DWORD_MAX;

    // Ignore errors
    UIGetSingleSelectedItemFromListView(::GetDlgItem(m_hWnd, BROWSE_CONTROL_CONFLICT_VALUES_VIEW), NULL, &dwRetVal);

    return dwRetVal;
}

HRESULT BrowseWindow::RefreshProductList(DWORD dwDatabaseIndex)
{
    HRESULT hr = S_OK;
    BOOL fCsEntered = FALSE;
    HWND hwnd = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_PRODUCT_LIST_VIEW);

    // If we aren't viewing this database right now, don't bother touching any controls
    if ((dwDatabaseIndex == m_dwLocalDatabaseIndex && BROWSE_TAB_MAIN != m_tab)
        || (dwDatabaseIndex != m_dwLocalDatabaseIndex && BROWSE_TAB_OTHERDATABASES != m_tab))
    {
        ExitFunction1(hr = S_OK);
    }

    ::EnterCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    fCsEntered = TRUE;

    if (FAILED(DATABASE(dwDatabaseIndex).hrInitializeResult))
    {
        DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText = L"Error initializing Cfg API!";
    }
    else if (FAILED(DATABASE(dwDatabaseIndex).productEnum.hrResult))
    {
        DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText = L"Error enumerating products!";
    }
    else if (FAILED(DATABASE(dwDatabaseIndex).hrReadLegacySettingsResult))
    {
        DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText = L"Error reading legacy settings!";
    }
    else if (DATABASE(dwDatabaseIndex).fInitializing)
    {
        DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText = L"Initializing Cfg API...";
    }
    else if (DATABASE(dwDatabaseIndex).fUninitializing)
    {
        DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText = L"Uninitializing Cfg API...";
    }
    else if (NULL == DATABASE(dwDatabaseIndex).productEnum.cehItems && DATABASE(dwDatabaseIndex).productEnum.fRefreshing)
    {
        DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText = L"Enumerating products...";
    }
    else if (DATABASE(dwDatabaseIndex).fReadingLegacySettings)
    {
        DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText = L"Reading Legacy Settings...";
    }
    else
    {
        DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText = NULL;
    }

    if (NULL != DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText)
    {
        hr = UISetListViewText(hwnd, DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText);
        ExitOnFailure(hr, "Failed to set loading string in product list listview");
    }
    else
    {
        hr = UISetListViewToProductEnum(hwnd, DATABASE(dwDatabaseIndex).productEnum.cehItems, DATABASE(dwDatabaseIndex).rgfProductInstalled, m_fShowUninstalledProducts);
        ExitOnFailure(hr, "Failed to set listview to product enum for product list screen");
    }

LExit:
    if (fCsEntered)
    {
        ::LeaveCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    }

    return hr;
}

HRESULT BrowseWindow::SetDatabaseIndex(
    DWORD dwIndex
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzText = NULL;

    m_dwDatabaseIndex = dwIndex;

    wzText = m_pbdlDatabaseList->rgDatabases[dwIndex].sczName;

    hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_PRODUCT_LIST_DATABASE_NAME_TEXT, wzText);
    ExitOnFailure(hr, "Failed to set database name");

    hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SINGLE_PRODUCT_DATABASE_NAME_TEXT, wzText);
    ExitOnFailure(hr, "Failed to set database name");

    hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_DATABASE_NAME_TEXT, wzText);
    ExitOnFailure(hr, "Failed to set database name");

    hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SINGLE_VALUE_HISTORY_DATABASE_NAME_TEXT, wzText);
    ExitOnFailure(hr, "Failed to set database name");

    hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SINGLE_DB_CONFLICTS_DATABASE_NAME_TEXT, wzText);
    ExitOnFailure(hr, "Failed to set database name");

    hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_CONFLICT_VALUES_DATABASE_NAME_TEXT, wzText);
    ExitOnFailure(hr, "Failed to set database name");

LExit:
    return hr;
}

HRESULT BrowseWindow::OpenSetValueScreen()
{
    HRESULT hr = S_OK;
    DWORD dwValueIndex = 0;
    DWORD dwValue = 0;
    DWORD64 qwValue = 0;
    BOOL fValue = FALSE;
    LPCWSTR wzText = NULL;
    LPWSTR sczText = NULL;
    ::EnterCriticalSection(&CURRENTDATABASE.cs);

    if (VALUE_INVALID == CURRENTDATABASE.cdSetValueType)
    {
        CURRENTDATABASE.cdSetValueType = VALUE_STRING;
    }

    // We're writing to an existing value - show the name above
    dwValueIndex = GetSelectedValueIndex();
    if (!CURRENTDATABASE.fNewValue)
    {
        hr = CfgEnumReadString(CURRENTDATABASE.valueEnum.cehItems, dwValueIndex, ENUM_DATA_VALUENAME, &wzText);
        ExitOnFailure(hr, "Failed to read value name from enumeration");

        hr = CfgEnumReadDataType(CURRENTDATABASE.valueEnum.cehItems, dwValueIndex, ENUM_DATA_VALUETYPE, &CURRENTDATABASE.cdSetValueType);
        ExitOnFailure(hr, "Failed to read value type from enumeration");

        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_VALUE_NAME_EDITBOX, FALSE);

        hr = StrAllocString(&sczText, wzText, 0);
        ExitOnFailure(hr, "Failed to allocate copy of value name string");

        hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_NAME_EDITBOX, sczText);
        ExitOnFailure(hr, "Failed to set value name control in set value window");

        // Error on failures here, because if we don't report the existing value, the user could be inadvertently clearing the value and not knowing it!
        switch (CURRENTDATABASE.cdSetValueType)
        {
        case VALUE_BLOB:
            // Nothing to do
            break;
        case VALUE_STRING:
            hr = CfgEnumReadString(CURRENTDATABASE.valueEnum.cehItems, dwValueIndex, ENUM_DATA_VALUESTRING, &wzText);
            ExitOnFailure(hr, "Failed to read string value from enum");
            break;

        case VALUE_DWORD:
            hr = CfgEnumReadDword(CURRENTDATABASE.valueEnum.cehItems, dwValueIndex, ENUM_DATA_VALUEDWORD, &dwValue);
            ExitOnFailure(hr, "Failed to read dword value from enum");

            hr = StrAllocFormatted(&sczText, L"%u", dwValue);
            ExitOnFailure(hr, "Failed to format DWORD value into string");

            wzText = sczText;
            break;

        case VALUE_QWORD:
            hr = CfgEnumReadQword(CURRENTDATABASE.valueEnum.cehItems, dwValueIndex, ENUM_DATA_VALUEQWORD, &qwValue);
            ExitOnFailure(hr, "Failed to read qword value from enum");

            hr = StrAllocFormatted(&sczText, L"%I64u", qwValue);
            ExitOnFailure(hr, "Failed to format QWORD value into string");

            wzText = sczText;
            break;

        case VALUE_BOOL:
            hr = CfgEnumReadBool(CURRENTDATABASE.valueEnum.cehItems, dwValueIndex, ENUM_DATA_VALUEBOOL, &fValue);
            ExitOnFailure(hr, "Failed to read bool value from enum");

            // Per docs, this message always returns zero
            ::SendMessageW(::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_VALUE_CHECKBOX), BM_SETCHECK, static_cast<WPARAM>(fValue ? BST_CHECKED : BST_UNCHECKED), 0);
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unexpected data type encountered in database while loading set value screen");
        }

        hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_EDITBOX, wzText);
        ExitOnFailure(hr, "Failed to set value text control in set value window");
    }
    // We're creating a new value
    else
    {
        hr = S_OK;
        CURRENTDATABASE.fNewValue = FALSE;

        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_VALUE_NAME_EDITBOX, TRUE);

        hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_NAME_EDITBOX, L"");
        ExitOnFailure(hr, "Failed to clear set value name editbox text");

        hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_EDITBOX, L"");
        ExitOnFailure(hr, "Failed to clear set value value editbox text");
    }

    hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_ERROR_TEXT, L"");
    ExitOnFailure(hr, "Failed to clear error text");

    RefreshTypeRadioButton();

LExit:
    ::LeaveCriticalSection(&CURRENTDATABASE.cs);
    ReleaseStr(sczText);

    return hr;
}

HRESULT BrowseWindow::RefreshValueList(DWORD dwDatabaseIndex)
{
    HRESULT hr = S_OK;
    BOOL fCsEntered = FALSE;

    // If we aren't viewing this database right now, don't bother touching any controls
    if ((dwDatabaseIndex == m_dwLocalDatabaseIndex && BROWSE_TAB_MAIN != m_tab)
        || (dwDatabaseIndex != m_dwLocalDatabaseIndex && BROWSE_TAB_OTHERDATABASES != m_tab))
    {
        ExitFunction1(hr = S_OK);
    }
    HWND hwnd = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_VALUE_LIST_VIEW);

    ::EnterCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    fCsEntered = TRUE;

    // If the product list failed to enumerate, we can't enumerate values and that error message supersedes ours
    if (NULL != DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText)
    {
        DATABASE(dwDatabaseIndex).valueEnum.wzDisplayStatusText = DATABASE(dwDatabaseIndex).productEnum.wzDisplayStatusText;
    }
    else if (DATABASE(dwDatabaseIndex).fSettingProduct)
    {
        DATABASE(dwDatabaseIndex).valueEnum.wzDisplayStatusText = L"Setting product...";
    }
    else if (FAILED(DATABASE(dwDatabaseIndex).hrSetProductResult))
    {
        DATABASE(dwDatabaseIndex).valueEnum.wzDisplayStatusText = L"Error setting product!";
    }
    else if (FAILED(DATABASE(dwDatabaseIndex).valueEnum.hrResult))
    {
        DATABASE(dwDatabaseIndex).valueEnum.wzDisplayStatusText = L"Error enumerating values!";
    }
    else if (NULL == DATABASE(dwDatabaseIndex).valueEnum.cehItems && DATABASE(dwDatabaseIndex).valueEnum.fRefreshing)
    {
        DATABASE(dwDatabaseIndex).valueEnum.wzDisplayStatusText = L"Enumerating values...";
    }
    else
    {
        DATABASE(dwDatabaseIndex).valueEnum.wzDisplayStatusText = NULL;
    }

    if (NULL != DATABASE(dwDatabaseIndex).valueEnum.wzDisplayStatusText)
    {
        hr = UISetListViewText(hwnd, DATABASE(dwDatabaseIndex).valueEnum.wzDisplayStatusText);
        ExitOnFailure(hr, "Failed to set error string in value list listview");
    }
    else
    {
        hr = UISetListViewToValueEnum(hwnd, DATABASE(dwDatabaseIndex).valueEnum.cehItems, m_fShowDeletedValues);
        ExitOnFailure(hr, "Failed to set listview to value enum for product settings screen");
    }

LExit:
    if (fCsEntered)
    {
        ::LeaveCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    }

    return hr;
}

HRESULT BrowseWindow::RefreshValueHistoryList(DWORD dwDatabaseIndex)
{
    HRESULT hr = S_OK;
    BOOL fCsEntered = FALSE;

    // If we aren't viewing this database right now, don't bother touching any controls
    if ((dwDatabaseIndex == m_dwLocalDatabaseIndex && BROWSE_TAB_MAIN != m_tab)
        || (dwDatabaseIndex != m_dwLocalDatabaseIndex && BROWSE_TAB_OTHERDATABASES != m_tab))
    {
        ExitFunction1(hr = S_OK);
    }
    C_CFG_ENUMERATION_HANDLE pcehEnumSwitcher = NULL;
    HWND hwnd = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_SINGLE_VALUE_HISTORY_LIST_VIEW);

    ::EnterCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    fCsEntered = TRUE;

    switch (DATABASE(dwDatabaseIndex).vhmValueHistoryMode)
    {
    case HISTORY_NORMAL:
        pcehEnumSwitcher = DATABASE(dwDatabaseIndex).valueHistoryEnum.cehItems;
        break;
    case HISTORY_LOCAL_CONFLICTS:
        pcehEnumSwitcher = DATABASE(dwDatabaseIndex).pcplConflictProductList[GetSelectedConflictProductIndex()].rgcesValueEnumLocal[GetSelectedConflictValueIndex()];
        break;
    case HISTORY_REMOTE_CONFLICTS:
        pcehEnumSwitcher = DATABASE(dwDatabaseIndex).pcplConflictProductList[GetSelectedConflictProductIndex()].rgcesValueEnumRemote[GetSelectedConflictValueIndex()];
        break;
    default:
        hr = E_FAIL;
        Bomb(hr);
        ExitFunction();
    }

    if (HISTORY_NORMAL == DATABASE(dwDatabaseIndex).vhmValueHistoryMode && NULL == DATABASE(dwDatabaseIndex).valueHistoryEnum.cehItems && DATABASE(dwDatabaseIndex).valueHistoryEnum.fRefreshing)
    {
        DATABASE(dwDatabaseIndex).valueHistoryEnum.wzDisplayStatusText = L"Enumerating...";
    }
    else if (HISTORY_NORMAL == DATABASE(dwDatabaseIndex).vhmValueHistoryMode && FAILED(DATABASE(dwDatabaseIndex).valueHistoryEnum.hrResult))
    {
        DATABASE(dwDatabaseIndex).valueHistoryEnum.wzDisplayStatusText = L"Error!";
    }
    else if (NULL == pcehEnumSwitcher)
    {
        DATABASE(dwDatabaseIndex).valueHistoryEnum.wzDisplayStatusText = L"None.";
    }
    else
    {
        DATABASE(dwDatabaseIndex).valueHistoryEnum.wzDisplayStatusText = NULL;
    }

    if (NULL != DATABASE(dwDatabaseIndex).valueHistoryEnum.wzDisplayStatusText)
    {
        hr = UISetListViewText(hwnd, DATABASE(dwDatabaseIndex).valueHistoryEnum.wzDisplayStatusText);
        ExitOnFailure(hr, "Failed to set error string in value history list listview");
    }
    else
    {
        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SINGLE_VALUE_HISTORY_EXPORT_FILE_BUTTON, FALSE);

        hr = UISetListViewToValueHistoryEnum(hwnd, pcehEnumSwitcher);
        ExitOnFailure(hr, "Failed to set listview to value enum history for value history screen");
    }

LExit:
    if (fCsEntered)
    {
        ::LeaveCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    }

    return hr;
}

HRESULT BrowseWindow::RefreshSingleDatabaseConflictList(DWORD dwDatabaseIndex)
{
    HRESULT hr = S_OK;
    BOOL fCsEntered = FALSE;

    // If we aren't viewing this database right now, don't bother touching any controls
    if ((dwDatabaseIndex == m_dwLocalDatabaseIndex && BROWSE_TAB_MAIN != m_tab)
        || (dwDatabaseIndex != m_dwLocalDatabaseIndex && BROWSE_TAB_OTHERDATABASES != m_tab))
    {
        ExitFunction1(hr = S_OK);
    }
    HWND hwnd = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_SINGLE_DB_CONFLICTS_VIEW);
    LPCWSTR wzSingleDatabaseText = NULL;

    ::EnterCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    fCsEntered = TRUE;

    if (DATABASE(dwDatabaseIndex).fSyncing)
    {
        wzSingleDatabaseText = L"Syncing...";
    }
    else if (DATABASE(dwDatabaseIndex).fReadingLegacySettings)
    {
        wzSingleDatabaseText = L"Reading Legacy Settings...";
    }
    else if (DATABASE(dwDatabaseIndex).fResolving)
    {
        wzSingleDatabaseText = L"Resolving conflicts...";
    }
    else if (FAILED(DATABASE(dwDatabaseIndex).hrInitializeResult))
    {
        wzSingleDatabaseText = L"Failed to initialize Cfg API!";
    }
    else if (FAILED(DATABASE(dwDatabaseIndex).hrResolveResult))
    {
        wzSingleDatabaseText = L"Failed to resolve conflicts!";
    }
    else if (DATABASE(dwDatabaseIndex).fInitializing)
    {
        wzSingleDatabaseText = L"Initializing Cfg API...";
    }
    else if (DATABASE(dwDatabaseIndex).fUninitializing)
    {
        wzSingleDatabaseText = L"Uninitializing Cfg API...";
    }
    else
    {
        wzSingleDatabaseText = NULL;
    }

    if (NULL != wzSingleDatabaseText)
    {
        hr = UISetListViewText(hwnd, wzSingleDatabaseText);
        ExitOnFailure(hr, "Failed to set syncing string in single (other) database conflict list listview");
    }
    else
    {
        hr = UISetListViewToProductConflictArray(hwnd, DATABASE(dwDatabaseIndex).sczName, DATABASE(dwDatabaseIndex).hrSyncResult, m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].pcplConflictProductList, m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].dwConflictProductCount);
        ExitOnFailure(hr, "Failed to set list view to product conflict array in single (other) database conflict list listview");
    }

LExit:
    if (fCsEntered)
    {
        ::LeaveCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    }

    return hr;
}

HRESULT BrowseWindow::RefreshSingleProductValuesConflictList(DWORD dwDatabaseIndex)
{
    HRESULT hr = S_OK;
    BOOL fCsEntered = FALSE;

    // If we aren't viewing this database right now, don't bother touching any controls
    if ((dwDatabaseIndex == m_dwLocalDatabaseIndex && BROWSE_TAB_MAIN != m_tab)
        || (dwDatabaseIndex != m_dwLocalDatabaseIndex && BROWSE_TAB_OTHERDATABASES != m_tab))
    {
        ExitFunction1(hr = S_OK);
    }
    HWND hwnd = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_CONFLICT_VALUES_VIEW);
    LPCWSTR wzConflictValueStatusText = NULL;
    DWORD dwConflictProductIndex = DWORD_MAX;

    ::EnterCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    fCsEntered = TRUE;

    dwConflictProductIndex = GetSelectedConflictProductIndex();

    if (dwConflictProductIndex >= DATABASE(dwDatabaseIndex).dwConflictProductCount)
    {
        wzConflictValueStatusText = L"Invalid product selected!";
    }
    else
    {
        wzConflictValueStatusText = NULL;
    }

    if (NULL != wzConflictValueStatusText)
    {
        hr = UISetListViewText(hwnd, wzConflictValueStatusText);
        ExitOnFailure(hr, "Failed to set status string in value conflict list listview");
    }
    else
    {
        hr = UISetListViewToValueConflictArray(hwnd,
            m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].sczName,
            const_cast<C_CFG_ENUMERATION_HANDLE *>(m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].pcplConflictProductList[dwConflictProductIndex].rgcesValueEnumLocal),
            m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].pcplConflictProductList[dwConflictProductIndex].rgrcValueChoices,
            m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].pcplConflictProductList[dwConflictProductIndex].cValues);
        ExitOnFailure(hr, "Failed to display conflicting values in value conflict list listview");
    }

LExit:
    if (fCsEntered)
    {
        ::LeaveCriticalSection(&DATABASE(dwDatabaseIndex).cs);
    }

    return hr;
}

HRESULT BrowseWindow::RefreshOtherDatabaseList()
{
    HRESULT hr = S_OK;
    DWORD dwInsertIndex = 0;
    BOOL fCheckedBackup = FALSE;
    BOOL fCsEntered = FALSE;
    HWND hwnd = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_OTHERDATABASES_VIEW);

    if (!::SendMessageW(hwnd, LVM_DELETEALLITEMS, 0, 0))
    {
        ExitWithLastError(hr, "Failed to delete all items from list view");
    }
    ::EnableWindow(hwnd, TRUE);

    ::EnterCriticalSection(&m_pbdlDatabaseList->cs);
    fCsEntered = TRUE;

    for (DWORD i = 0; i < m_pbdlDatabaseList->cDatabases; ++i)
    {
        // Local databases don't show up in the other databases list
        if (DATABASE_LOCAL == DATABASE(i).dtType)
        {
            continue;
        }

        fCheckedBackup = DATABASE(i).fChecked;

        hr = UIListViewInsertItem(hwnd, &dwInsertIndex, DATABASE(i).sczName, i, 0);
        ExitOnFailure(hr, "Failed to insert value name into value conflict listview control");

        ListView_SetCheckState(hwnd, dwInsertIndex, fCheckedBackup);

        if (DATABASE(i).fInitializing)
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Loading...");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (FAILED(DATABASE(i).hrInitializeResult))
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Failed to Load!");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (DATABASE(i).fUninitializing)
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Disconnecting...");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (DATABASE(i).fSyncing)
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Syncing...");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (DATABASE(i).fReadingLegacySettings)
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Reading Legacy Settings...");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (DATABASE(i).fResolving)
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Resolving...");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (FAILED(DATABASE(i).hrSyncResult))
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Failed to Sync!");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (FAILED(DATABASE(i).hrResolveResult))
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Failed to Resolve!");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (NULL != DATABASE(i).pcplConflictProductList)
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Conflicts to Resolve");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else if (DATABASE(i).fInitialized)
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"OK");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }
        else
        {
            hr = UIListViewSetItemText(hwnd, dwInsertIndex, 2, L"Disconnected");
            ExitOnFailure(hr, "Failed to set text in column 1 of other database listview control");
        }

        ++dwInsertIndex;
    }

LExit:
    if (fCsEntered)
    {
        ::LeaveCriticalSection(&m_pbdlDatabaseList->cs);
    }

    return hr;
}

void BrowseWindow::RefreshTypeRadioButton()
{
    WPARAM wpChecked = static_cast<WPARAM>(BST_CHECKED);
    WPARAM wpUnchecked = static_cast<WPARAM>(BST_UNCHECKED);
    HWND hwndFile = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_VALUE_FILE_TYPE);
    HWND hwndDword = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_VALUE_DWORD_TYPE);
    HWND hwndQword = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_VALUE_QWORD_TYPE);
    HWND hwndString = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_VALUE_STRING_TYPE);
    HWND hwndBool = ::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_VALUE_BOOL_TYPE);

    // Per docs, these messages always return zero
    ::SendMessageW(hwndFile, BM_SETCHECK, (VALUE_BLOB == CURRENTDATABASE.cdSetValueType) ? wpChecked : wpUnchecked, 0);
    ::SendMessageW(hwndDword, BM_SETCHECK, (VALUE_DWORD == CURRENTDATABASE.cdSetValueType) ? wpChecked : wpUnchecked, 0);
    ::SendMessageW(hwndQword, BM_SETCHECK, (VALUE_QWORD == CURRENTDATABASE.cdSetValueType) ? wpChecked : wpUnchecked, 0);
    ::SendMessageW(hwndString, BM_SETCHECK, (VALUE_STRING == CURRENTDATABASE.cdSetValueType) ? wpChecked : wpUnchecked, 0);
    ::SendMessageW(hwndBool, BM_SETCHECK, (VALUE_BOOL == CURRENTDATABASE.cdSetValueType) ? wpChecked : wpUnchecked, 0);

    RefreshSetValueScreenVisibility();
}

void BrowseWindow::RefreshSetValueScreenVisibility()
{
    if (VALUE_BOOL == CURRENTDATABASE.cdSetValueType)
    {
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_CHECKBOX, TRUE);

        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_PATH_TEXT, FALSE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_PATH_EDITBOX, FALSE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_BROWSE_BUTTON, FALSE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_EDITBOX, FALSE);
    }
    else if (VALUE_BLOB == CURRENTDATABASE.cdSetValueType)
    {
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_PATH_TEXT, TRUE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_PATH_EDITBOX, TRUE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_BROWSE_BUTTON, TRUE);

        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_CHECKBOX, FALSE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_EDITBOX, FALSE);
    }
    else
    {
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_EDITBOX, TRUE);

        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_CHECKBOX, FALSE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_PATH_TEXT, FALSE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_PATH_EDITBOX, FALSE);
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_VALUE_BROWSE_BUTTON, FALSE);
    }
}

DWORD WINAPI BrowseWindow::UiThreadProc(
    __in_bcount(sizeof(BrowseWindow)) LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    DWORD dwDatabaseIndex = 0;
    BrowseWindow* pThis = (BrowseWindow*)pvContext;
    BOOL fComInitialized = FALSE;
    BOOL fRet = FALSE;
    MSG msg = { };

    // Nobody cares about failure - this is just best effort to boost UI responsiveness.
    ::SetThreadPriority(::GetCurrentThread(), THREAD_PRIORITY_ABOVE_NORMAL);

    // initialize COM
    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    hr = UtilGrowDatabaseList(pThis->m_pbdlDatabaseList, &dwDatabaseIndex);
    ExitOnFailure(hr, "Failed to grow database list");

    hr = StrAllocString(&(pThis->m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].sczName), L"(Local Database)", 0);
    ExitOnFailure(hr, "Failed to copy name of local database to database list entry");

    pThis->m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].dtType = DATABASE_LOCAL;
    pThis->m_pbdlDatabaseList->rgDatabases[dwDatabaseIndex].fInitializing = TRUE;

    // create main window
    hr = pThis->CreateMainWindow();
    ExitOnFailure(hr, "Failed to create main window.");

    // Record the local database index
    pThis->m_dwLocalDatabaseIndex = dwDatabaseIndex;

    hr = pThis->RefreshOtherDatabaseList();
    ExitOnFailure(hr, "Failed to refresh other database listview");

    if (!::PostThreadMessageW(pThis->m_dwWorkThreadId, WM_BROWSE_INITIALIZE, dwDatabaseIndex, 0))
    {
        ExitWithLastError(hr, "Failed to post initialize message to worker thread");
    }

    // message pump
    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected return value from message pump.");
        }
        // TODO: this is hacky and should be improved
        else if (WM_KEYDOWN == msg.message && VK_ESCAPE == msg.wParam)
        {
            hr = pThis->SetPreviousScreen();
            ExitOnFailure(hr, "Failed to set previous screen after user hit escape");
        }
        else if (!::IsDialogMessageW(pThis->m_hWnd, &msg))
        {
            ::TranslateMessage(&msg);
            ::DispatchMessageW(&msg);
        }
    }

LExit:
    // destroy main window
    pThis->DestroyMainWindow();

    // uninitialize COM
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return hr;
}

//
// CreateMainWindow - creates the main install window.
//
HRESULT BrowseWindow::CreateMainWindow()
{
    HRESULT hr = S_OK;
    WNDCLASSW wc = { };
    DWORD dwWindowStyle = 0;

    hr = ThemeLoadFromResource(m_hModule, MAKEINTRESOURCEA(BROWSE_RES_THEME_FILE), &m_pTheme);
    ExitOnFailure(hr, "Failed to load theme from embedded resource.");

    hr = LocLoadFromResource(m_hModule, MAKEINTRESOURCEA(BROWSE_RES_EN_US_LOC_FILE), &m_pLoc);
    ExitOnFailure(hr, "Failed to load loc stringset from resource.");

    hr = ThemeLocalize(m_pTheme, m_pLoc);
    ExitOnFailure(hr, "Failed to localize theme.");

    // Register the window class and create the window.
    wc.style = 0;
    wc.lpfnWndProc = BrowseWindow::WndProc;
    wc.cbClsExtra = 0;
    wc.cbWndExtra = 0;
    wc.hInstance = m_hModule;
    wc.hIcon = reinterpret_cast<HICON>(m_pTheme->hIcon);
    wc.hCursor = ::LoadCursorW(NULL, (LPCWSTR)IDC_ARROW);
    wc.hbrBackground = m_pTheme->rgFonts[m_pTheme->dwFontId].hBackground;
    wc.lpszMenuName = NULL;
    wc.lpszClassName = BROWSE_WINDOW_CLASS;
    if (!::RegisterClassW(&wc))
    {
        ExitWithLastError(hr, "Failed to register window class.");
    }

    m_fRegistered = TRUE;

    // Calculate the window style based on the theme style and command display value.
    dwWindowStyle = m_pTheme->dwStyle;

    m_hWnd = ::CreateWindowExW(0, wc.lpszClassName, m_pTheme->sczCaption, dwWindowStyle, CW_USEDEFAULT, CW_USEDEFAULT, m_pTheme->nWidth, m_pTheme->nHeight, HWND_DESKTOP, NULL, wc.hInstance, this);
    ExitOnNullWithLastError(m_hWnd, hr, "Failed to create window.");

    if (!::PostThreadMessageW(m_dwWorkThreadId, WM_BROWSE_RECEIVE_HWND, reinterpret_cast<WPARAM>(m_hWnd), 0))
    {
        hr = E_FAIL;
        ExitOnFailure(hr, "Failed to send HWND back to worker thread");
    }

    m_hMenu = ::CreatePopupMenu();
    ExitOnNullWithLastError(m_hMenu, hr, "Failed to create popup menu");

    if (!::AppendMenuW(m_hMenu, MF_STRING, WM_BROWSE_TRAY_ICON_EXIT, L"Exit"))
    {
        ExitWithLastError(hr, "Failed to append menu item");
    }

    hr = TrayInitialize(m_hWnd, reinterpret_cast<HICON>(m_pTheme->hIcon));
    ExitOnFailure(hr, "Failed to initialize tray icon");

    hr = RefreshProductList(m_dwDatabaseIndex);
    ExitOnFailure(hr, "Failed to display product list");

    hr = RefreshValueList(m_dwDatabaseIndex);
    ExitOnFailure(hr, "Failed to display value list");

LExit:
    ReleaseStr(m_sczLanguage);

    if (FAILED(hr))
    {
        // If we failed to create the window, tell the worker thread to exit
        ::PostThreadMessageW(m_dwWorkThreadId, WM_QUIT, 0, 0);
        DestroyMainWindow();
    }

    return hr;
}


//
// DestroyMainWindow - clean up all the window registration.
//
void BrowseWindow::DestroyMainWindow()
{
    TrayUninitialize();

    if (m_hMenu)
    {
        ::DestroyMenu(m_hMenu);
        m_hMenu = NULL;
    }

    if (m_hWnd)
    {
        ::CloseWindow(m_hWnd);
        m_hWnd = NULL;
    }

    if (m_fRegistered)
    {
        ::UnregisterClassW(BROWSE_WINDOW_CLASS, m_hModule);
        m_fRegistered = FALSE;
    }

    ReleaseTheme(m_pTheme);
    if (m_pLoc)
    {
        LocFree(m_pLoc);
        m_pLoc = NULL;
    }
}

//
// WndProc - standard windows message handler.
//
LRESULT CALLBACK BrowseWindow::WndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    HRESULT hr = S_OK;
    HRESULT hrTemp = S_OK;
    LRESULT lres = 0;
    int iRet = 0;
    DWORD dwIndex = 0;
    DWORD dwTemp1 = 0;
    DWORD *rgdwDwords = NULL;
    DWORD64 qwTemp = 0;
    BOOL fTemp1 = FALSE;
    BOOL fTemp2 = FALSE;
    LPWSTR sczTemp1 = NULL;
    LPWSTR sczTemp2 = NULL;
    LPCWSTR wzText = NULL;
    CONFIG_VALUETYPE cvType = VALUE_INVALID;
    LVITEMW lvItem = { };
    NMITEMACTIVATE *lpnmitem = NULL;
    OPENFILENAMEW ofn = { };
    COPYDATASTRUCT * pcds = NULL;
    BOOL fCsEntered = FALSE;
    POINT curPoint = { };

#pragma warning(suppress:4312)
    BrowseWindow* pUX = reinterpret_cast<BrowseWindow*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

    switch (uMsg)
    {
    case WM_NCCREATE:
        {
        LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
        pUX = reinterpret_cast<BrowseWindow*>(lpcs->lpCreateParams);
#pragma warning(suppress:4244)
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pUX));
        }
        break;

    case WM_NCHITTEST:
        if (pUX->m_pTheme->dwStyle & WS_POPUP)
        {
            return HTCAPTION; // allow pop-up windows to be moved by grabbing any non-control.
        }
        break;

    case WM_NCDESTROY:
        lres = ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        return lres;

    case WM_CREATE:
        if (!pUX->OnCreate(hWnd))
        {
            return -1;
        }
        break;

    case WM_CLOSE:
        LogStringLine(REPORT_STANDARD, "Close message received - exiting.");
        ::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_QUIT, 0, 0);
        ::DestroyWindow(hWnd);
        ExitFunction();

    case WM_DESTROY:
        ::PostQuitMessage(0);
        break;

    case WM_SYSCOMMAND:
        if (SC_MINIMIZE == wParam && pUX->m_fVisible)
        {
            pUX->m_fVisible = FALSE;
            ::ShowWindow(pUX->m_hWnd, SW_HIDE);
            return 0;
        }
        break;

    case WM_BROWSE_TRAY_ICON_MESSAGE:
        switch (lParam)
        {
        case WM_LBUTTONDOWN:
            pUX->m_fVisible = !pUX->m_fVisible;
            ::ShowWindow(pUX->m_hWnd, pUX->m_fVisible ? SW_SHOW : SW_HIDE);
            if (pUX->m_fVisible)
            {
                ::SetForegroundWindow(pUX->m_hWnd);
            }
            break;
        case WM_RBUTTONDOWN:
            // Don't fail if errors occur - best effort only
            if (!::GetCursorPos(&curPoint))
            {
                dwTemp1 = ::GetLastError();
                LogErrorString(HRESULT_FROM_WIN32(dwTemp1), "Failed to get cursor position");
            }
            else
            {
                if (!::TrackPopupMenu(pUX->m_hMenu, TPM_RIGHTALIGN | TPM_BOTTOMALIGN | TPM_NOANIMATION, curPoint.x, curPoint.y, 0, pUX->m_hWnd, NULL))
                {
                    dwTemp1 = ::GetLastError();
                    LogErrorString(HRESULT_FROM_WIN32(dwTemp1), "Failed to show popup menu");
                }
            }
            break;
        case NIN_BALLOONUSERCLICK:
            // Don't fail if errors occur - best effort only
            iRet = TabCtrl_SetCurSel(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_MAIN_TAB_CONTROL), BROWSE_TAB_OTHERDATABASES);
            if (0 != iRet)
            {
                LogErrorString(E_UNEXPECTED, "Failed to switch to other databases tab during balloon click");
            }

            hr = pUX->SetTab(BROWSE_TAB_OTHERDATABASES);
            ExitOnFailure(hr, "Failed while switching to 'other databases' tab");

            hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_STATE_MAIN);
            if (FAILED(hr))
            {
                LogErrorString(hr, "Failed to switch to main page of other databases tab during balloon click");
                hr = S_OK;
            }

            pUX->m_fVisible = TRUE;
            ::ShowWindow(pUX->m_hWnd, pUX->m_fVisible ? SW_SHOW : SW_HIDE);
            break;
        }
        break;

    case WM_DRAWITEM:
        ThemeDrawControl(pUX->m_pTheme, reinterpret_cast<LPDRAWITEMSTRUCT>(lParam));
        return TRUE;

    case WM_CTLCOLORSTATIC:
        {
        HBRUSH hBrush = NULL;
        if (ThemeSetControlColor(pUX->m_pTheme, reinterpret_cast<HDC>(wParam), reinterpret_cast<HWND>(hWnd), &hBrush))
        {
            return reinterpret_cast<LRESULT>(hBrush);
        }
        }
        break;

    case WM_SETCURSOR:
        ThemeHoverControl(pUX->m_pTheme, hWnd, reinterpret_cast<HWND>(wParam));
        break;

    case WM_PAINT:
        // If there is anything to update, do so.
        if (::GetUpdateRect(hWnd, NULL, FALSE))
        {
            PAINTSTRUCT ps;
            ::BeginPaint(hWnd, &ps);
            ThemeDrawBackground(pUX->m_pTheme, &ps);
            ::EndPaint(hWnd, &ps);
        }
        ExitFunction();

    case WM_COPYDATA:
        pcds = reinterpret_cast<COPYDATASTRUCT *>(lParam);
        if (NULL == pcds || NULL == pcds->lpData)
        {
            LogErrorString(E_POINTER, "NULL copy data struct received!");
        }
        else
        {
            wzText = (LPWSTR)pcds->lpData;

            hr = StrAllocString(&sczTemp1, wzText, 0);
            ExitOnFailure(hr, "Failed to allocate copy of string from COPYDATA message");

            if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_IMPORT_LEGACY_MANIFEST, reinterpret_cast<WPARAM>(sczTemp1), 0))
            {
                ExitWithLastError(hr, "Failed to send WM_BROWSE_IMPORT_LEGACY_MANIFEST message to worker thread");
            }
            sczTemp1 = NULL;
        }
        hr = S_OK;
        break;

    case WM_BROWSE_INITIALIZE_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).fInitializing = FALSE;
        UXDATABASE(dwIndex).hrInitializeResult = static_cast<HRESULT>(wParam);

        hr = pUX->SetDatabaseIndex(0);
        ExitOnFailure(hr, "Failed to set database index to 0");

        if (SUCCEEDED(UXDATABASE(dwIndex).hrInitializeResult))
        {
            UXDATABASE(dwIndex).fInitialized = TRUE;
            UXDATABASE(dwIndex).fInitializing = FALSE;

            // Read browser settings
            if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_READ_SETTINGS, 0, 0))
            {
                ExitWithLastError(hr, "Failed to send message to worker thread to read latest settings");
            }

            if (DATABASE_LOCAL == UXDATABASE(dwIndex).dtType)
            {
                hr = pUX->EnumerateDatabases(dwIndex);
                ExitOnFailure(hr, "Failed to enumerate databases from local database");
            }
        }
        else
        {
            hr = pUX->RefreshProductList(dwIndex);
            ExitOnFailure(hr, "Failed to refresh product list");
        }

        ExitFunction();

    case WM_BROWSE_DISCONNECT_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).hrUninitializeResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).fUninitializing = FALSE;
        UXDATABASE(dwIndex).fInitialized = FALSE;

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh database list");

        ExitFunction();

    case WM_BROWSE_ENUMERATE_PRODUCTS_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).productEnum.hrResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).productEnum.fRefreshing = FALSE;

        hr = pUX->RefreshProductList(dwIndex);
        ExitOnFailure(hr, "Failed to refresh product list");

        if (!pUX->m_fBackgroundThreadResumed)
        {
            pUX->m_fBackgroundThreadResumed = TRUE;

            hr = CfgResumeBackgroundThread(pUX->m_pbdlDatabaseList->rgDatabases[pUX->m_dwLocalDatabaseIndex].cdb);
            ExitOnFailure(hr, "Failed to resume background thread");
        }
        ExitFunction();

    case WM_BROWSE_ENUMERATE_DATABASES_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).dbEnum.hrResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).dbEnum.fRefreshing = FALSE;

        if (0 < UXDATABASE(dwIndex).dbEnum.cItems)
        {
            hr = MemEnsureArraySize(reinterpret_cast<void **>(&rgdwDwords), UXDATABASE(dwIndex).dbEnum.cItems, sizeof(DWORD), 0);
            ExitOnFailure(hr, "Failed to reserve space for index array while enumerating databases");

            for (DWORD i = 0; i < UXDATABASE(dwIndex).dbEnum.cItems; ++i)
            {
                hr = UtilGrowDatabaseList(pUX->m_pbdlDatabaseList, rgdwDwords + i);
                ExitOnFailure(hr, "Failed to grow database list");
            }

            ::EnterCriticalSection(&UXDATABASE(dwIndex).cs);
            fCsEntered = TRUE;

            for (DWORD i = 0; i < UXDATABASE(dwIndex).dbEnum.cItems; ++i)
            {
                hr = CfgEnumReadString(UXDATABASE(dwIndex).dbEnum.cehItems, i, ENUM_DATA_FRIENDLY_NAME, &wzText);
                ExitOnFailure(hr, "Failed to read friendly name from database enumeration at index: %u", i);

                hr = StrAllocString(&UXDATABASE(rgdwDwords[i]).sczName, wzText, 0);
                ExitOnFailure(hr, "Failed to copy friendly name to database array");

                hr = CfgEnumReadString(UXDATABASE(dwIndex).dbEnum.cehItems, i, ENUM_DATA_PATH, &wzText);
                ExitOnFailure(hr, "Failed to read path from database enumeration at index: %u", i);

                hr = StrAllocString(&UXDATABASE(rgdwDwords[i]).sczPath, wzText, 0);
                ExitOnFailure(hr, "Failed to copy path to database array");

                hr = CfgEnumReadBool(UXDATABASE(dwIndex).dbEnum.cehItems, i, ENUM_DATA_SYNC_BY_DEFAULT, &UXDATABASE(rgdwDwords[i]).fSyncByDefault);
                ExitOnFailure(hr, "Failed to read sync by default flag from database enumeration at index: %u", i);

                UXDATABASE(rgdwDwords[i]).fChecked = UXDATABASE(rgdwDwords[i]).fSyncByDefault;
                UXDATABASE(rgdwDwords[i]).fRemember = TRUE;
                UXDATABASE(rgdwDwords[i]).dtType = DATABASE_REMOTE;

                if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_OPEN_REMOTE, static_cast<LPARAM>(rgdwDwords[i]), 0))
                {
                    ExitWithLastError(hr, "Failed to send message to worker thread to open remote database (index %u)", rgdwDwords[i]);
                }
            }

            ::LeaveCriticalSection(&UXDATABASE(dwIndex).cs);
            fCsEntered = FALSE;
        }

        hr = pUX->EnumerateProducts(dwIndex);
        ExitOnFailure(hr, "Failed to enumerate products");

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh database list");

        ExitFunction();

    case WM_BROWSE_SET_PRODUCT_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).hrSetProductResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).fSettingProduct = FALSE;

        if (BROWSE_TAB_MAIN == pUX->m_tab)
        {
            hr = pUX->SetMainState(BROWSE_MAIN_STATE_SINGLEPRODUCT);
            ExitOnFailure(hr, "Failed while switching main state to single product screen");
        }
        else if (BROWSE_TAB_OTHERDATABASES == pUX->m_tab)
        {
            hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEPRODUCT);
            ExitOnFailure(hr, "Failed while switching other databases state to single product screen");
        }

        if (SUCCEEDED(CURRENTUXDATABASE.hrSetProductResult))
        {
            CURRENTUXDATABASE.fProductSet = TRUE;

            hr = pUX->EnumerateValues(dwIndex, TRUE);
            ExitOnFailure(hr, "Failed to enumerate values");
        }
        else
        {
            hr = pUX->RefreshValueList(dwIndex);
            ExitOnFailure(hr, "Failed to refresh value list");
        }
        ExitFunction();

    case WM_BROWSE_SET_FILE_FINISHED: __fallthrough;
    case WM_BROWSE_SET_STRING_FINISHED: __fallthrough;
    case WM_BROWSE_SET_DWORD_FINISHED: __fallthrough;
    case WM_BROWSE_SET_QWORD_FINISHED: __fallthrough;
    case WM_BROWSE_SET_BOOL_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        hr = static_cast<HRESULT>(wParam);

        if (FAILED(hr))
        {
            hr = StrAllocFormatted(&sczTemp1, L"Error setting value: 0x%x", hr);
            ExitOnFailure(hr, "Failed to format error string");

            hr = ThemeSetTextControl(pUX->m_pTheme, BROWSE_CONTROL_SET_VALUE_ERROR_TEXT, sczTemp1);
            ExitOnFailure(hr, "Failed to display error text");
        }
        else
        {
            hr = pUX->EnumerateValues(dwIndex, FALSE);
            ExitOnFailure(hr, "Failed to enumerate values after setting value");

            hr = pUX->SetPreviousScreen();
            ExitOnFailure(hr, "Failed while switching back to previous screen");
        }

        ExitFunction();

    case WM_BROWSE_ENUMERATE_VALUES_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).valueEnum.hrResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).valueEnum.fRefreshing = FALSE;

        hr = pUX->RefreshValueList(dwIndex);
        ExitOnFailure(hr, "Failed to refresh value list");
        ExitFunction();

    case WM_BROWSE_ENUMERATE_VALUE_HISTORY_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).valueHistoryEnum.hrResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).valueHistoryEnum.fRefreshing = FALSE;
        UXDATABASE(dwIndex).vhmValueHistoryMode = HISTORY_NORMAL;

        hr = pUX->RefreshValueHistoryList(dwIndex);
        ExitOnFailure(hr, "Failed to refresh value history listview");

        ExitFunction();

    case WM_BROWSE_IMPORT_LEGACY_MANIFEST_FINISHED:
        hr = static_cast<HRESULT>(wParam);
        CURRENTUXDATABASE.fImportingLegacyManifest = FALSE;

        if (FAILED(hr))
        {
            hr = UIMessageBoxDisplayError(pUX->m_hWnd, L"Failed result received from worker thread when importing legacy manifest", hr);
            ExitOnFailure(hr, "Failed to display error messagebox");
        }
        else
        {
            hr = pUX->EnumerateProducts(pUX->m_dwLocalDatabaseIndex);
            ExitOnFailure(hr, "Failed to enumerate products in local database");
        }

        ExitFunction();

    case WM_BROWSE_READ_LEGACY_SETTINGS_FINISHED:
        hr = static_cast<HRESULT>(wParam);
        CURRENTUXDATABASE.fReadingLegacySettings = FALSE;
        CURRENTUXDATABASE.hrReadLegacySettingsResult = hr;

        if (FAILED(hr))
        {
            hr = UIMessageBoxDisplayError(pUX->m_hWnd, L"Failed result received from worker thread when reading legacy settings", hr);
            ExitOnFailure(hr, "Failed to display error messagebox");
        }
        else
        {
            hr = pUX->EnumerateProducts(pUX->m_dwLocalDatabaseIndex);
            ExitOnFailure(hr, "Failed to enumerate products in local database");

            hr = pUX->EnumerateValues(pUX->m_dwLocalDatabaseIndex, FALSE);
            ExitOnFailure(hr, "Failed to enumerate values in local database");
        }

        ExitFunction();

    case WM_BROWSE_EXPORT_FILE_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        hr = static_cast<HRESULT>(wParam);

        if (FAILED(hr))
        {
            hr = UIMessageBoxDisplayError(pUX->m_hWnd, L"Failed result received from worker thread when exporting file", hr);
            ExitOnFailure(hr, "Failed to display error messagebox after failing to export file");
        }
        ExitFunction();

    case WM_BROWSE_EXPORT_FILE_FROM_HISTORY_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        hr = static_cast<HRESULT>(wParam);

        if (FAILED(hr))
        {
            hr = UIMessageBoxDisplayError(pUX->m_hWnd, L"Failed result received from worker thread when exporting file from history", hr);
            ExitOnFailure(hr, "Failed to display error messagebox after failing to export file from history");
        }
        ExitFunction();

    case WM_BROWSE_SYNC_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).hrSyncResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).fSyncing = FALSE;

        ::EnterCriticalSection(&UXDATABASE(dwIndex).cs);
        fCsEntered = TRUE;
        if (SUCCEEDED(UXDATABASE(dwIndex).hrSyncResult) && NULL != UXDATABASE(dwIndex).pcplConflictProductList)
        {
            hrTemp = TrayShowBalloon(L"Sync Conflicts", L"Sync conflicts occurred. Click here to open main browser window, and resolve them.", NIIF_WARNING);
            if (FAILED(hrTemp))
            {
                LogStringLine(REPORT_STANDARD, "Failed to show tray balloon");
            }
        }
        else
        {
            hrTemp = TrayHideBalloon();
            if (FAILED(hrTemp))
            {
                LogStringLine(REPORT_STANDARD, "Failed to hide tray balloon");
            }
        }

        // Read latest browser settings, in case they changed
        if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_READ_SETTINGS, 0, 0))
        {
            ExitWithLastError(hr, "Failed to send message to worker thread to read latest settings");
        }

        hr = pUX->RefreshSingleDatabaseConflictList(dwIndex);
        ExitOnFailure(hr, "Failed to refresh single database conflict display");

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh other database listview");

        // Display the new data for the local database
        hr = pUX->EnumerateProducts(pUX->m_dwLocalDatabaseIndex);
        ExitOnFailure(hr, "Failed to enumerate products in local database");

        hr = pUX->EnumerateValues(pUX->m_dwLocalDatabaseIndex, FALSE);
        ExitOnFailure(hr, "Failed to enumerate values in local database");

        hr = pUX->EnumerateValueHistory(pUX->m_dwLocalDatabaseIndex, FALSE);
        ExitOnFailure(hr, "Failed to enumerate value history in local database");

        // And for the database we synced with
        hr = pUX->EnumerateProducts(dwIndex);
        ExitOnFailure(hr, "Failed to enumerate products in remote database index:%u", dwIndex);

        hr = pUX->EnumerateValues(dwIndex, FALSE);
        ExitOnFailure(hr, "Failed to enumerate values in remote database index:%u", dwIndex);

        hr = pUX->EnumerateValueHistory(dwIndex, FALSE);
        ExitOnFailure(hr, "Failed to enumerate value history in remote database index:%u", dwIndex);

        ExitFunction();

    case WM_BROWSE_RESOLVE_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).hrResolveResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).fResolving = FALSE;

        if (SUCCEEDED(UXDATABASE(dwIndex).hrResolveResult))
        {
            UXDATABASE(dwIndex).fSyncing = TRUE;

            if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_SYNC, static_cast<WPARAM>(dwIndex), 0))
            {
                ExitWithLastError(hr, "Failed to send message to worker thread to sync");
            }
        }

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh other database list");

        hr = pUX->RefreshSingleDatabaseConflictList(dwIndex);
        ExitOnFailure(hr, "Failed to refresh single database product conflict list");

        ExitFunction();

    case WM_BROWSE_CREATE_REMOTE_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).hrInitializeResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).fInitialized = SUCCEEDED(UXDATABASE(dwIndex).hrInitializeResult);
        UXDATABASE(dwIndex).fInitializing = FALSE;

        if (UXDATABASE(dwIndex).fRemembering)
        {
            if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_REMEMBER, static_cast<WPARAM>(dwIndex), 0))
            {
                ExitWithLastError(hr, "Failed to send message to worker thread to remember database");
            }
        }

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh other database listview after create remote finished");

        ExitFunction();

    case WM_BROWSE_OPEN_REMOTE_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).hrInitializeResult = static_cast<HRESULT>(wParam);
        UXDATABASE(dwIndex).fInitialized = SUCCEEDED(UXDATABASE(dwIndex).hrInitializeResult);
        UXDATABASE(dwIndex).fInitializing = FALSE;

        if (UXDATABASE(dwIndex).fRemembering)
        {
            if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_REMEMBER, static_cast<WPARAM>(dwIndex), 0))
            {
                ExitWithLastError(hr, "Failed to send message to worker thread to remember database");
            }
        }

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh other database listview after open remote finished");

        ExitFunction();

    case WM_BROWSE_REMEMBER_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).hrRememberResult = static_cast<HRESULT>(wParam);

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh other database listview after remember finished");

        ExitFunction();

    case WM_BROWSE_FORGET_FINISHED:
        dwIndex = static_cast<DWORD>(lParam);

        UXDATABASE(dwIndex).hrForgetResult = static_cast<HRESULT>(wParam);
        if (SUCCEEDED(UXDATABASE(dwIndex).hrForgetResult) ||
            E_NOTFOUND == UXDATABASE(dwIndex).hrForgetResult)
        {
            UXDATABASE(dwIndex).fRemember = FALSE;
        }

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh database list");

        ExitFunction();

    case WM_BROWSE_AUTOSYNCING_REMOTE:
        UXDATABASE(dwIndex).hrInitializeResult = S_OK;
        UXDATABASE(dwIndex).fSyncing = TRUE;

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh other database listview due to autosyncing remote message");
        break;

    case WM_BROWSE_AUTOSYNC_GENERAL_FAILURE:
        hr = static_cast<HRESULT>(wParam);
        ExitFunction();
        break;

    case WM_BROWSE_AUTOSYNC_REMOTE_FAILURE:
        dwIndex = static_cast<DWORD>(wParam);

        UXDATABASE(dwIndex).hrInitializeResult = static_cast<HRESULT>(lParam);
        UXDATABASE(dwIndex).fSyncing = FALSE;

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh other database listview due to remote failure message");
        break;

    case WM_BROWSE_AUTOSYNC_REMOTE_GOOD:
        dwIndex = static_cast<DWORD>(wParam);

        UXDATABASE(dwIndex).hrInitializeResult = S_OK;
        UXDATABASE(dwIndex).fSyncing = FALSE;

        hr = pUX->RefreshOtherDatabaseList();
        ExitOnFailure(hr, "Failed to refresh other database listview due to remote good message");
        break;

    case WM_BROWSE_AUTOSYNC_PRODUCT_FAILURE:
        // Nothing to do. MonUtil will automatically remonitor it when it's possible to do so.
        break;

    case WM_BROWSE_SETTINGS_CHANGED:
        // one or more browser settings have changed, update UI accordingly
        ::SendMessageW(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_PRODUCT_LIST_SHOW_UNINSTALLED_PRODUCTS_CHECKBOX), BM_SETCHECK, static_cast<WPARAM>(pUX->m_fShowUninstalledProducts ? BST_CHECKED : BST_UNCHECKED), 0);
        ::SendMessageW(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_PRODUCT_LIST_SHOW_DELETED_VALUES_CHECKBOX), BM_SETCHECK, static_cast<WPARAM>(pUX->m_fShowDeletedValues ? BST_CHECKED : BST_UNCHECKED), 0);

        hr = pUX->RefreshProductList(pUX->m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh product list");

        hr = pUX->RefreshValueList(pUX->m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh value list");
        break;

    case WM_NOTIFY:
        switch (LOWORD(wParam))
        {
        case BROWSE_CONTROL_MAIN_TAB_CONTROL:
            lpnmitem = reinterpret_cast<LPNMITEMACTIVATE>(lParam);
            switch (lpnmitem->hdr.code)
            {
            case TCN_SELCHANGE:
                if (BROWSE_TAB_MAIN == ::SendMessageW(lpnmitem->hdr.hwndFrom, TCM_GETCURSEL, 0, 0))
                {
                    hr = pUX->SetTab(BROWSE_TAB_MAIN);
                    ExitOnFailure(hr, "Failed while switching to main tab");
                }
                else
                {
                    hr = pUX->SetTab(BROWSE_TAB_OTHERDATABASES);
                    ExitOnFailure(hr, "Failed while switching to 'other databases' tab");
                }
                break;
            }
            break;
        case BROWSE_CONTROL_PRODUCT_LIST_VIEW:
            lpnmitem = reinterpret_cast<LPNMITEMACTIVATE>(lParam);
            switch (lpnmitem->hdr.code)
            {
            case NM_DBLCLK:
                if (NULL == CURRENTUXDATABASE.sczCurrentProductDisplayName)
                {
                    ExitFunction();
                }

                // We're switching products in the UI - any old value list is inaccurate, and should be released immediately
                UtilWipeEnum(pUX->m_pbdlDatabaseList->rgDatabases + dwIndex, &pUX->m_pbdlDatabaseList->rgDatabases[dwIndex].valueEnum);

                hr = pUX->RefreshValueList(dwIndex);
                ExitOnFailure(hr, "Failed to refresh value list for main single product screen");

                CURRENTUXDATABASE.fSettingProduct = TRUE;

                if (BROWSE_TAB_MAIN == pUX->m_tab)
                {
                    hr = pUX->SetMainState(BROWSE_MAIN_STATE_SINGLEPRODUCT);
                    ExitOnFailure(hr, "Failed while switching main state to single product screen");
                }
                else if (BROWSE_TAB_OTHERDATABASES == pUX->m_tab)
                {
                    hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEPRODUCT);
                    ExitOnFailure(hr, "Failed while switching other databases state to single product screen");
                }

                hr = SendStringTriplet(pUX->m_dwWorkThreadId, WM_BROWSE_SET_PRODUCT, pUX->m_dwDatabaseIndex, CURRENTUXDATABASE.prodCurrent.sczName, CURRENTUXDATABASE.prodCurrent.sczVersion, CURRENTUXDATABASE.prodCurrent.sczPublicKey);
                ExitOnFailure(hr, "Failed to send WM_BROWSE_SET_PRODUCT message");
            break;
            case NM_CLICK:
                lvItem.mask = LVIF_PARAM;
                lvItem.iItem = lpnmitem->iItem;
                ListView_GetItem(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_PRODUCT_LIST_VIEW), &lvItem);

                hr = pUX->SetSelectedProduct(lvItem.lParam);
                ExitOnFailure(hr, "Failed to set selected product to index: %u", lpnmitem->lParam);
                break;
            default:
                break;
            }
            break;
        case BROWSE_CONTROL_VALUE_LIST_VIEW:
            lpnmitem = reinterpret_cast<LPNMITEMACTIVATE>(lParam);
            switch (lpnmitem->hdr.code)
            {
            case NM_DBLCLK:
                ::PostMessageW(hWnd, WM_COMMAND, BROWSE_CONTROL_SET_VALUE_BUTTON | BN_CLICKED, 0);
                break;
            }
            break;
        case BROWSE_CONTROL_SINGLE_VALUE_HISTORY_LIST_VIEW:
            lpnmitem = reinterpret_cast<LPNMITEMACTIVATE>(lParam);
            switch (lpnmitem->hdr.code)
            {
            case NM_CLICK:
                // Export file button is not visible on value conflict screens, so don't bother in that case
                if (BROWSE_TAB_OTHERDATABASES != pUX->m_tab || (BROWSE_OTHERDATABASES_VIEW_MY_VALUE_CONFLICTS != pUX->m_otherDatabasesState && BROWSE_OTHERDATABASES_VIEW_OTHER_VALUE_CONFLICTS != pUX->m_otherDatabasesState))
                {
                    dwTemp1 = pUX->GetSelectedValueHistoryIndex();
                    cvType = VALUE_DELETED;

                    if (DWORD_MAX != dwTemp1)
                    {
                        ::EnterCriticalSection(&CURRENTUXDATABASE.cs);
                        // Ignore failure (this is expected in certain race conditions), it just means button will be disabled
                        CfgEnumReadDataType(CURRENTUXDATABASE.valueHistoryEnum.cehItems, pUX->GetSelectedValueHistoryIndex(), ENUM_DATA_VALUETYPE, &cvType);
                        ::LeaveCriticalSection(&CURRENTUXDATABASE.cs);
                    }

                    ThemeControlEnable(pUX->m_pTheme, BROWSE_CONTROL_SINGLE_VALUE_HISTORY_EXPORT_FILE_BUTTON, VALUE_BLOB == cvType);
                }
                break;
            }
            break;
        case BROWSE_CONTROL_SINGLE_DB_CONFLICTS_VIEW:
            lpnmitem = reinterpret_cast<LPNMITEMACTIVATE>(lParam);
            switch (lpnmitem->hdr.code)
            {
            case NM_DBLCLK:
                if (DWORD_MAX == pUX->GetSelectedConflictProductIndex())
                {
                    ExitFunction1(hr = S_OK);
                }

                hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_PRODUCT_CONFLICTS);
                ExitOnFailure(hr, "Failed while switching other databases state to list of single product's conflicts");
                break;
            }
            break;

        case BROWSE_CONTROL_OTHERDATABASES_VIEW:
            lpnmitem = reinterpret_cast<LPNMITEMACTIVATE>(lParam);

            lvItem.mask = LVIF_PARAM;
            lvItem.iItem = lpnmitem->iItem;
            ListView_GetItem(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_OTHERDATABASES_VIEW), &lvItem);
            dwIndex = static_cast<DWORD>(lvItem.lParam);
            if (DWORD_MAX != dwIndex)
            {
                switch (lpnmitem->hdr.code)
                {
                case NM_DBLCLK:
                    hr = pUX->SetDatabaseIndex(dwIndex);
                    ExitOnFailure(hr, "Failed to set database index to: %u", dwIndex);

                    pUX->m_dwOtherDatabaseIndex = pUX->m_dwDatabaseIndex;

                    hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_PRODUCTLIST);
                    ExitOnFailure(hr, "Failed to move to single other database product list screen");
                    break;
                case NM_CLICK:
                    lvItem.mask = LVIF_PARAM;
                    lvItem.iItem = lpnmitem->iItem;
                    ListView_GetItem(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_OTHERDATABASES_VIEW), &lvItem);

                    hr = pUX->SetDatabaseIndex(dwIndex);
                    ExitOnFailure(hr, "Failed to set database index to: %u", static_cast<DWORD>(lvItem.lParam));

                    pUX->m_dwOtherDatabaseIndex = pUX->m_dwDatabaseIndex;
                    break;
                case LVN_ITEMCHANGED:
                    UXDATABASE(dwIndex).fChecked = ListView_GetCheckState(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_OTHERDATABASES_VIEW), lvItem.iItem);
                    break;
                }
            }
        }
        break;

    case WM_COMMAND:
        switch (LOWORD(wParam))
        {
        /* TODO: undo VK_ESCAPE hack and uncomment this
        case IDCANCEL:
            hr = pUX->SetPreviousScreen();
            ExitOnFailure(hr, "Failed to go to previous screen");
            break;*/

        case IDOK:
            if ((BROWSE_TAB_MAIN == pUX->m_tab && BROWSE_MAIN_STATE_SETVALUE == pUX->m_mainState) || BROWSE_TAB_OTHERDATABASES == pUX->m_tab && BROWSE_OTHERDATABASES_SINGLE_DATABASE_SETVALUE == pUX->m_otherDatabasesState)
            {
                ::PostMessageW(hWnd, WM_COMMAND, BROWSE_CONTROL_SET_VALUE_SAVE_BUTTON | BN_CLICKED, 0);
                ExitFunction();
            }
            break;

        case WM_BROWSE_TRAY_ICON_EXIT:
            LogStringLine(REPORT_STANDARD, "User closed main browser via tray exit menu - exiting.");
            ::PostMessageW(hWnd, WM_CLOSE, 0, 0);
            break;

        case BROWSE_CONTROL_PRODUCT_LIST_SHOW_UNINSTALLED_PRODUCTS_CHECKBOX:
            if (BN_CLICKED == HIWORD(wParam))
            {
                pUX->m_fShowUninstalledProducts = ThemeIsControlChecked(pUX->m_pTheme, BROWSE_CONTROL_PRODUCT_LIST_SHOW_UNINSTALLED_PRODUCTS_CHECKBOX);

                hr = pUX->RefreshProductList(pUX->m_dwDatabaseIndex);
                ExitOnFailure(hr, "Failed to refresh product list");

                if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_PERSIST_SETTINGS, 0, 0))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_PERSIST_SETTINGS message");
                }
            }
            break;

        case BROWSE_CONTROL_PRODUCT_LIST_SHOW_DELETED_VALUES_CHECKBOX:
            if (BN_CLICKED == HIWORD(wParam))
            {
                pUX->m_fShowDeletedValues = ThemeIsControlChecked(pUX->m_pTheme, BROWSE_CONTROL_PRODUCT_LIST_SHOW_DELETED_VALUES_CHECKBOX);

                hr = pUX->RefreshValueList(pUX->m_dwDatabaseIndex);
                ExitOnFailure(hr, "Failed to refresh value list");

                if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_PERSIST_SETTINGS, 0, 0))
                {
                    ExitWithLastError(hr, "Failed to send WM_BROWSE_PERSIST_SETTINGS message");
                }
            }
            break;

        case BROWSE_CONTROL_DELETE_SETTINGS_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                ::EnterCriticalSection(&UXDATABASE(dwIndex).cs);
                fCsEntered = TRUE;
                hr = UIDeleteValuesFromListView(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_VALUE_LIST_VIEW), CURRENTUXDATABASE.cdb, CURRENTUXDATABASE.valueEnum.cehItems);
                ExitOnFailure(hr, "Failed to delete values from list view");

                hr = pUX->EnumerateValues(pUX->m_dwDatabaseIndex, FALSE);
                ExitOnFailure(hr, "Failed to start value enumeration");
            }
            break;
        case BROWSE_CONTROL_SINGLE_PRODUCT_ACCEPT_MINE_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                ::EnterCriticalSection(&UXDATABASE(dwIndex).cs);
                fCsEntered = TRUE;
                hr = UISetValueConflictsFromListView(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_CONFLICT_VALUES_VIEW), CURRENTUXDATABASE.sczName, &(CURRENTUXDATABASE.pcplConflictProductList[pUX->GetSelectedConflictProductIndex()]), RESOLUTION_LOCAL);
                ExitOnFailure(hr, "Failed to update value resolution state");
            }
            break;
        case BROWSE_CONTROL_ACCEPT_MINE_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                ::EnterCriticalSection(&UXDATABASE(dwIndex).cs);
                fCsEntered = TRUE;
                hr = UISetProductConflictsFromListView(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_SINGLE_DB_CONFLICTS_VIEW), CURRENTUXDATABASE.sczName, CURRENTUXDATABASE.pcplConflictProductList, CURRENTUXDATABASE.dwConflictProductCount, RESOLUTION_LOCAL);
                ExitOnFailure(hr, "Failed to update product resolution state");
            }
            break;
        case BROWSE_CONTROL_SINGLE_PRODUCT_ACCEPT_OTHER_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                ::EnterCriticalSection(&UXDATABASE(dwIndex).cs);
                fCsEntered = TRUE;
                hr = UISetValueConflictsFromListView(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_CONFLICT_VALUES_VIEW), CURRENTUXDATABASE.sczName, &(CURRENTUXDATABASE.pcplConflictProductList[pUX->GetSelectedConflictProductIndex()]), RESOLUTION_REMOTE);
                ExitOnFailure(hr, "Failed to update value resolution state");
            }
            break;
        case BROWSE_CONTROL_ACCEPT_OTHER_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                ::EnterCriticalSection(&UXDATABASE(dwIndex).cs);
                fCsEntered = TRUE;
                hr = UISetProductConflictsFromListView(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_SINGLE_DB_CONFLICTS_VIEW), CURRENTUXDATABASE.sczName, CURRENTUXDATABASE.pcplConflictProductList, CURRENTUXDATABASE.dwConflictProductCount, RESOLUTION_REMOTE);
                ExitOnFailure(hr, "Failed while switching main state");
            }
            break;
        case BROWSE_CONTROL_PRODUCT_LIST_PRODUCT_FORGET_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                ::EnterCriticalSection(&UXDATABASE(dwIndex).cs);
                fCsEntered = TRUE;
                hr = UIForgetProductsFromListView(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_PRODUCT_LIST_VIEW), CURRENTUXDATABASE.cdb, CURRENTUXDATABASE.productEnum.cehItems);
                ExitOnFailure(hr, "Failed to forget products from list view");

                hr = pUX->EnumerateProducts(pUX->m_dwDatabaseIndex);
                ExitOnFailure(hr, "Failed to enumerate products");
            }
            break;
        case BROWSE_CONTROL_PRODUCT_LIST_BACK_BUTTON: __fallthrough;
        case BROWSE_CONTROL_SINGLE_VALUE_HISTORY_BACK_BUTTON: __fallthrough;
        case BROWSE_CONTROL_SINGLE_DB_BACK_BUTTON: __fallthrough;
        case BROWSE_CONTROL_SINGLE_PRODUCT_BACK_BUTTON: __fallthrough;
        case BROWSE_CONTROL_SET_EXTERNAL_CANCEL_BUTTON: __fallthrough;
        case BROWSE_CONTROL_CANCEL_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                hr = pUX->SetPreviousScreen();
            }
            break;
        case BROWSE_CONTROL_VIEW_VALUE_HISTORY_BUTTON:
            dwIndex = pUX->GetSelectedValueIndex();
            if (BN_CLICKED == HIWORD(wParam) && DWORD_MAX != dwIndex)
            {
                ::EnterCriticalSection(&CURRENTUXDATABASE.cs);
                fCsEntered = TRUE;
                // Ignore failure (this is expected in certain race conditions), just pretend the button was never hit
                hrTemp = CfgEnumReadString(CURRENTUXDATABASE.valueEnum.cehItems, dwIndex, ENUM_DATA_VALUENAME, &wzText);
                if (SUCCEEDED(hrTemp))
                {
                    hr = StrAllocString(&CURRENTUXDATABASE.sczValueName, wzText, 0);
                    ExitOnFailure(hr, "Failed to copy value name");
                }
                ::LeaveCriticalSection(&CURRENTUXDATABASE.cs);
                fCsEntered = FALSE;

                if (FAILED(hrTemp))
                {
                    LogStringLine(REPORT_STANDARD, "Failed to go to value history screen due to failure to read enum with error 0x%X", hrTemp);
                }
                else
                {
                    if (BROWSE_TAB_MAIN == pUX->m_tab)
                    {
                        hr = pUX->SetMainState(BROWSE_MAIN_STATE_SINGLEVALUEHISTORY);
                        ExitOnFailure(hr, "Failed while switching main state to single value history screen");
                    }
                    else if (BROWSE_TAB_OTHERDATABASES == pUX->m_tab)
                    {
                        hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEVALUEHISTORY);
                        ExitOnFailure(hr, "Failed while switching other databases state to single value history screen");
                    }

                    hr = pUX->EnumerateValueHistory(pUX->m_dwDatabaseIndex, TRUE);
                    ExitOnFailure(hr, "Failed to enumerate value history");
                }
            }
            break;
        case BROWSE_CONTROL_VIEW_MY_VALUE_HISTORY_BUTTON:
            if (BN_CLICKED == HIWORD(wParam) && DWORD_MAX != pUX->GetSelectedConflictValueIndex())
            {
                hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_VIEW_MY_VALUE_CONFLICTS);
                ExitOnFailure(hr, "Failed while switching other database state to my conflicts value screen");
            }
            break;
        case BROWSE_CONTROL_VIEW_OTHER_VALUE_HISTORY_BUTTON:
            if (BN_CLICKED == HIWORD(wParam) && DWORD_MAX != pUX->GetSelectedConflictValueIndex())
            {
                hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_VIEW_OTHER_VALUE_CONFLICTS);
                ExitOnFailure(hr, "Failed while switching other database state to other conflicts value screen");
            }
            break;
        case BROWSE_CONTROL_NEW_VALUE_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.cdSetValueType = VALUE_INVALID;
                CURRENTUXDATABASE.fNewValue = TRUE;

                if (BROWSE_TAB_MAIN == pUX->m_tab)
                {
                    hr = pUX->SetMainState(BROWSE_MAIN_STATE_SETVALUE);
                    ExitOnFailure(hr, "Failed while switching main state to new value screen");
                }
                else if (BROWSE_TAB_OTHERDATABASES == pUX->m_tab)
                {
                    hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_SETVALUE);
                    ExitOnFailure(hr, "Failed while switching other databases state to new value screen");
                }
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.fNewValue = FALSE;

                if (BROWSE_TAB_MAIN == pUX->m_tab)
                {
                    hr = pUX->SetMainState(BROWSE_MAIN_STATE_SETVALUE);
                    ExitOnFailure(hr, "Failed while switching main state to set value screen");
                }
                else if (BROWSE_TAB_OTHERDATABASES == pUX->m_tab)
                {
                    hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_SETVALUE);
                    ExitOnFailure(hr, "Failed while switching other databases state to set value screen");
                }
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_CANCEL_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                hr = pUX->SetPreviousScreen();
                ExitOnFailure(hr, "Failed while switching to previous screen");
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_SAVE_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                if (DWORD_MAX == pUX->GetSelectedValueIndex())
                {
                    hr = ThemeGetTextControl(pUX->m_pTheme, BROWSE_CONTROL_SET_VALUE_NAME_EDITBOX, &sczTemp1);
                    ExitOnFailure(hr, "Failed to get value in name editbox");

                    wzText = sczTemp1;
                }
                else
                {
                    ::EnterCriticalSection(&CURRENTUXDATABASE.cs);
                    fCsEntered = TRUE;
                    hrTemp = CfgEnumReadString(CURRENTUXDATABASE.valueEnum.cehItems, pUX->GetSelectedValueIndex(), ENUM_DATA_VALUENAME, &wzText);
                    if (SUCCEEDED(hrTemp))
                    {
                        hr = StrAllocString(&sczTemp1, wzText, 0);
                        ExitOnFailure(hr, "Failed to copy value name");
                    }
                    ::LeaveCriticalSection(&CURRENTUXDATABASE.cs);
                    fCsEntered = FALSE;
                    ExitOnFailure(hr, "Failed to read value name from enumeration");
                }

                if (VALUE_BLOB == CURRENTUXDATABASE.cdSetValueType)
                {
                    hr = ThemeGetTextControl(pUX->m_pTheme, BROWSE_CONTROL_SET_VALUE_PATH_EDITBOX, &sczTemp2);
                    ExitOnFailure(hr, "Failed to get value in set value editbox");
                }
                else
                {
                    hr = ThemeGetTextControl(pUX->m_pTheme, BROWSE_CONTROL_SET_VALUE_EDITBOX, &sczTemp2);
                    ExitOnFailure(hr, "Failed to get value in set value editbox");
                }

                if (VALUE_BLOB == CURRENTUXDATABASE.cdSetValueType)
                {
                    hr = SendStringPair(pUX->m_dwWorkThreadId, WM_BROWSE_SET_FILE, pUX->m_dwDatabaseIndex, sczTemp1, sczTemp2);
                    ExitOnFailure(hr, "Failed to send set file command to worker thread");
                }
                if (VALUE_STRING == CURRENTUXDATABASE.cdSetValueType)
                {
                    hr = SendStringPair(pUX->m_dwWorkThreadId, WM_BROWSE_SET_STRING, pUX->m_dwDatabaseIndex, sczTemp1, sczTemp2);
                    ExitOnFailure(hr, "Failed to send set string command to worker thread");
                }
                else if (VALUE_DWORD == CURRENTUXDATABASE.cdSetValueType)
                {
                    dwTemp1 = wcstoul(sczTemp2, NULL, 10);

                    if (ULONG_MAX == dwTemp1)
                    {
                        ExitWithLastError(hr, "Failed to convert string to dword");
                    }

                    hr = SendDwordString(pUX->m_dwWorkThreadId, WM_BROWSE_SET_DWORD, pUX->m_dwDatabaseIndex, dwTemp1, sczTemp1);
                    ExitOnFailure(hr, "Failed to send set dword command to worker thread");
                }
                else if (VALUE_QWORD == CURRENTUXDATABASE.cdSetValueType)
                {
                    qwTemp = _wcstoui64(sczTemp2, NULL, 10);

                    if (_UI64_MAX == qwTemp)
                    {
                        ExitWithLastError(hr, "Failed to convert string to qword");
                    }

                    hr = SendQwordString(pUX->m_dwWorkThreadId, WM_BROWSE_SET_QWORD, pUX->m_dwDatabaseIndex, qwTemp, sczTemp1);
                    ExitOnFailure(hr, "Failed to send set qword command to worker thread");
                }
                else if (VALUE_BOOL == CURRENTUXDATABASE.cdSetValueType)
                {
                    hr = SendDwordString(pUX->m_dwWorkThreadId, WM_BROWSE_SET_BOOL, pUX->m_dwDatabaseIndex, static_cast<BOOL>(ThemeIsControlChecked(pUX->m_pTheme, BROWSE_CONTROL_SET_VALUE_CHECKBOX)), sczTemp1);
                    ExitOnFailure(hr, "Failed to send set bool command to worker thread");
                }
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_FILE_TYPE:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.cdSetValueType = VALUE_BLOB;

                pUX->RefreshTypeRadioButton();
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_DWORD_TYPE:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.cdSetValueType = VALUE_DWORD;

                pUX->RefreshTypeRadioButton();
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_QWORD_TYPE:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.cdSetValueType = VALUE_QWORD;

                pUX->RefreshTypeRadioButton();
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_STRING_TYPE:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.cdSetValueType = VALUE_STRING;

                pUX->RefreshTypeRadioButton();
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_BOOL_TYPE:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.cdSetValueType = VALUE_BOOL;

                pUX->RefreshTypeRadioButton();
            }
            break;
        case BROWSE_CONTROL_SINGLE_DATABASE_SYNC_BUTTON:
            if (BN_CLICKED != HIWORD(wParam) || CURRENTUXDATABASE.fSyncing || CURRENTUXDATABASE.fImportingLegacyManifest)
            {
                ExitFunction1(hr = S_OK);
            }

            if (0 < CURRENTUXDATABASE.dwConflictProductCount)
            {
                CURRENTUXDATABASE.fResolving = TRUE;
                CURRENTUXDATABASE.hrResolveResult = S_OK;

                if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_RESOLVE, static_cast<WPARAM>(pUX->m_dwDatabaseIndex), 0))
                {
                    ExitWithLastError(hr, "Failed to send message to worker thread to resolve conflicts");
                }
            }
            else
            {
                CURRENTUXDATABASE.fSyncing = TRUE;
                CURRENTUXDATABASE.hrSyncResult = S_OK;

                if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_SYNC, static_cast<WPARAM>(pUX->m_dwDatabaseIndex), 0))
                {
                    ExitWithLastError(hr, "Failed to send message to worker thread to sync database");
                }
            }

            hr = pUX->RefreshSingleDatabaseConflictList(pUX->m_dwDatabaseIndex);
            ExitOnFailure(hr, "Failed to refresh single database conflict display list");
            break;
        case BROWSE_CONTROL_READ_LEGACY_SETTINGS_BUTTON:
            if (BN_CLICKED != HIWORD(wParam) || CURRENTUXDATABASE.fSyncing || CURRENTUXDATABASE.fReadingLegacySettings)
            {
                ExitFunction1(hr = S_OK);
            }

            CURRENTUXDATABASE.fReadingLegacySettings = TRUE;
            CURRENTUXDATABASE.hrReadLegacySettingsResult = S_OK;

            hr = pUX->RefreshProductList(pUX->m_dwLocalDatabaseIndex);
            ExitOnFailure(hr, "Failed to update product list");

            if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_READ_LEGACY_SETTINGS, static_cast<WPARAM>(pUX->m_dwDatabaseIndex), 0))
            {
                ExitWithLastError(hr, "Failed to send message to worker thread to read legacy settings");
            }

            break;
        case BROWSE_CONTROL_IMPORT_LEGACY_MANIFEST_BUTTON:
            if (BN_CLICKED != HIWORD(wParam) || !(CURRENTUXDATABASE.fInitialized) || CURRENTUXDATABASE.fImportingLegacyManifest)
            {
                ExitFunction1(hr = S_OK);
            }

            // Prompt to change the source location.
            ofn.lStructSize = sizeof(ofn);
            ofn.hwndOwner = hWnd;
            ofn.lpstrFile = CURRENTUXDATABASE.wzLegacyManifestPath;
            ofn.nMaxFile = countof(CURRENTUXDATABASE.wzLegacyManifestPath);
            ofn.lpstrFilter = L"User Data Manifest Files\0*.udm\0";
            ofn.nFilterIndex = 1;
            ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;
            ofn.lpstrTitle = L"Legacy Manifest File";

            if (::GetOpenFileNameW(&ofn))
            {
                ThemeControlEnable(pUX->m_pTheme, BROWSE_CONTROL_SINGLE_DATABASE_SYNC_BUTTON, FALSE);
                CURRENTUXDATABASE.fImportingLegacyManifest = TRUE;

                hr = StrAllocString(&sczTemp1, CURRENTUXDATABASE.wzLegacyManifestPath, 0);
                ExitOnFailure(hr, "Failed to allocate copy of string from COPYDATA message");

                if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_IMPORT_LEGACY_MANIFEST, reinterpret_cast<WPARAM>(sczTemp1), 0))
                {
                    ExitWithLastError(hr, "Failed to send message to worker thread to import legacy manifest");
                }
                sczTemp1 = NULL;
            }
            else
            {
                // Do nothing
            }

            ofn.lpstrFile = NULL;
            break;
        case BROWSE_CONTROL_SET_EXTERNAL_BROWSE_BUTTON:
            if (BN_CLICKED != HIWORD(wParam) || !(CURRENTUXDATABASE.fInitialized))
            {
                ExitFunction1(hr = S_OK);
            }

            // Prompt to change the source location.
            ofn.lStructSize = sizeof(ofn);
            ofn.hwndOwner = hWnd;
            hr = StrAlloc(&ofn.lpstrFile, NUM_FILE_SELECTION_DIALOG_CHARACTERS);
            ExitOnFailure(hr, "Failed to allocate space for file selection dialog while importing file");
            ofn.nMaxFile = NUM_FILE_SELECTION_DIALOG_CHARACTERS;
            ofn.lpstrFilter = L"All Files\0*.*\0";
            ofn.nFilterIndex = 1;
            ofn.Flags = 0;
            ofn.lpstrTitle = L"Set External Database Path";

            if (::GetOpenFileNameW(&ofn))
            {
                hr = ThemeSetTextControl(pUX->m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_PATH_EDITBOX, ofn.lpstrFile);
                ExitOnFailure(hr, "Failed to set text of external database path editbox to user's browse dialog selection");
            }
            else
            {
                // Do nothing
            }
            break;
        case BROWSE_CONTROL_SET_VALUE_BROWSE_BUTTON:
            if (BN_CLICKED != HIWORD(wParam) || !(CURRENTUXDATABASE.fInitialized))
            {
                ExitFunction1(hr = S_OK);
            }

            // Prompt to change the source location.
            ofn.lStructSize = sizeof(ofn);
            ofn.hwndOwner = hWnd;
            hr = StrAlloc(&ofn.lpstrFile, NUM_FILE_SELECTION_DIALOG_CHARACTERS);
            ExitOnFailure(hr, "Failed to allocate space for file selection dialog while importing file");
            ofn.nMaxFile = NUM_FILE_SELECTION_DIALOG_CHARACTERS;
            ofn.lpstrFilter = L"All Files\0*.*\0";
            ofn.nFilterIndex = 1;
            ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;
            ofn.lpstrTitle = L"Import File";

            if (::GetOpenFileNameW(&ofn))
            {
                hr = ThemeSetTextControl(pUX->m_pTheme, BROWSE_CONTROL_SET_VALUE_PATH_EDITBOX, ofn.lpstrFile);
                ExitOnFailure(hr, "Failed to set text of file path editbox to user's browse dialog selection");
            }
            else
            {
                // Do nothing
            }
            break;
        case BROWSE_CONTROL_SINGLE_VALUE_HISTORY_EXPORT_FILE_BUTTON:
            if (BN_CLICKED != HIWORD(wParam) || !(CURRENTUXDATABASE.fInitialized))
            {
                ExitFunction1(hr = S_OK);
            }

            dwTemp1 = pUX->GetSelectedValueHistoryIndex();

            if (DWORD_MAX == dwTemp1)
            {
                ExitFunction1(hr = S_OK);
            }

            ReleaseNullStr(sczTemp1);
            hr = UIExportFile(hWnd, &sczTemp1);
            ExitOnFailure(hr, "Failed to request export path from user");

            if (NULL != sczTemp1)
            {
                hr = SendDwordString(pUX->m_dwWorkThreadId, WM_BROWSE_EXPORT_FILE_FROM_HISTORY, pUX->m_dwDatabaseIndex, dwTemp1, sczTemp1);
                ExitOnFailure(hr, "Failed to send WM_BROWSE_EXPORT_FILE_FROM_HISTORY message to worker thread");
            }
            break;
        case BROWSE_CONTROL_EXPORT_FILE_BUTTON:
            if (BN_CLICKED != HIWORD(wParam) || !(CURRENTUXDATABASE.fInitialized))
            {
                ExitFunction1(hr = S_OK);
            }

            ReleaseNullStr(sczTemp1);
            hr = UIExportFile(hWnd, &sczTemp1);
            ExitOnFailure(hr, "Failed to request export path from user");

            if (NULL != sczTemp1)
            {
                ::EnterCriticalSection(&CURRENTUXDATABASE.cs);
                fCsEntered = TRUE;
                hrTemp = CfgEnumReadString(CURRENTUXDATABASE.valueEnum.cehItems, pUX->GetSelectedValueIndex(), ENUM_DATA_VALUENAME, &wzText);
                if (SUCCEEDED(hrTemp))
                {
                    hr = SendStringPair(pUX->m_dwWorkThreadId, WM_BROWSE_EXPORT_FILE, pUX->m_dwDatabaseIndex, wzText, sczTemp1);
                    ExitOnFailure(hr, "Failed to send WM_BROWSE_EXPORT_FILE message to worker thread");
                }
                else
                {
                    LogStringLine(REPORT_STANDARD, "Failed to export file due to failure to read enum with error 0x%X", hrTemp);
                }
                ::LeaveCriticalSection(&CURRENTUXDATABASE.cs);
                fCsEntered = FALSE;
            }
            break;
        case BROWSE_CONTROL_OTHERDATABASES_SET_EXTERNAL_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                pUX->m_fAdding = TRUE;

                hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_STATE_SET_EXTERNAL);
                ExitOnFailure(hr, "Failed to move to add external database screen");
            }
            break;
        case BROWSE_CONTROL_OTHERDATABASES_MODIFY_EXTERNAL_BUTTON:
            if (BN_CLICKED == HIWORD(wParam) && DWORD_MAX != pUX->m_dwDatabaseIndex && DATABASE_REMOTE == CURRENTUXDATABASE.dtType)
            {
                pUX->m_fAdding = FALSE;

                hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_STATE_SET_EXTERNAL);
                ExitOnFailure(hr, "Failed to move to modify external database screen");
            }
            break;
        case BROWSE_CONTROL_OTHERDATABASES_SYNC_BUTTON:
            if (BN_CLICKED != HIWORD(wParam) || CURRENTUXDATABASE.fSyncing || CURRENTUXDATABASE.fImportingLegacyManifest)
            {
                ExitFunction1(hr = S_OK);
            }

            // Docs don't indicate any way for it to return failure
            dwTemp1 = ::SendMessageW(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_OTHERDATABASES_VIEW), LVM_GETITEMCOUNT, 0, 0);

            for (DWORD i = 0; i < dwTemp1; ++i)
            {
                ZeroMemory(&lvItem, sizeof(lvItem));
                lvItem.mask = LVIF_PARAM;
                lvItem.iItem = i;

                if (!::SendMessageW(::GetDlgItem(pUX->m_hWnd, BROWSE_CONTROL_OTHERDATABASES_VIEW), LVM_GETITEM, 0, reinterpret_cast<LPARAM>(&lvItem)))
                {
                    ExitWithLastError(hr, "Failed to get item lparam from list view to set product conflicts");
                }

                // Index in database list
                dwIndex = lvItem.lParam;

                // If the checkbox is checked, sync it
                if (UXDATABASE(dwIndex).fChecked)
                {
                    // If this database is not initialized / in an error state, don't try to sync it
                    if (!UtilReadyToSync(pUX->m_pbdlDatabaseList->rgDatabases + dwIndex))
                    {
                        continue;
                    }

                    // if we have conflicts to resolve, resolve those first
                    if (NULL != UXDATABASE(dwIndex).pcplConflictProductList)
                    {
                        UXDATABASE(dwIndex).fResolving = TRUE;
                        UXDATABASE(dwIndex).hrResolveResult = S_OK;

                        if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_RESOLVE, static_cast<LPARAM>(dwIndex), 0))
                        {
                            ExitWithLastError(hr, "Failed to send message to worker thread to resolve database");
                        }
                    }
                    else
                    {
                        UXDATABASE(dwIndex).fSyncing = TRUE;
                        UXDATABASE(dwIndex).hrSyncResult = S_OK;

                        if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_SYNC, static_cast<LPARAM>(dwIndex), 0))
                        {
                            ExitWithLastError(hr, "Failed to send message to worker thread to sync database");
                        }
                    }
                }
            }

            hr = pUX->RefreshOtherDatabaseList();
            ExitOnFailure(hr, "Failed to refresh other database display list");
            break;
        case BROWSE_CONTROL_OTHERDATABASES_VIEW_DATABASE_CONFLICTS:
            if (BN_CLICKED == HIWORD(wParam) && DWORD_MAX != pUX->m_dwDatabaseIndex)
            {
                hr = pUX->SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_CONFLICTS);
                ExitOnFailure(hr, "Failed to move to single other database conflicts screen");
            }
            break;
        case BROWSE_CONTROL_SET_EXTERNAL_OK_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                if (pUX->m_fAdding)
                {
                    hr = ThemeGetTextControl(pUX->m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_PATH_EDITBOX, &sczTemp1);
                    ExitOnFailure(hr, "Failed to get path string");

                    hr = ThemeGetTextControl(pUX->m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_NAME_EDITBOX, &sczTemp2);
                    ExitOnFailure(hr, "Failed to get name string");

                    // If they didn't provide both a path and friendly name, do nothing
                    if (wcslen(sczTemp1) == 0 || wcslen(sczTemp2) == 0)
                    {
                        ExitFunction();
                    }

                    hr = UtilGrowDatabaseList(pUX->m_pbdlDatabaseList, &dwIndex);
                    ExitOnFailure(hr, "Failed to grow database list");

                    fTemp1 = ThemeIsControlChecked(pUX->m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_CREATE_NEW_CHECKBOX);

                    UXDATABASE(dwIndex).sczPath = sczTemp1;
                    sczTemp1 = NULL;
                    UXDATABASE(dwIndex).sczName = sczTemp2;
                    sczTemp2 = NULL;

                    UXDATABASE(dwIndex).dtType = DATABASE_REMOTE;
                }
                else
                {
                    dwIndex = pUX->m_dwDatabaseIndex;
                }

                fTemp2 = ThemeIsControlChecked(pUX->m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_REMEMBER_CHECKBOX);

                // If their remember preference changed, mark us down to send a command to apply it
                if (fTemp2 != UXDATABASE(dwIndex).fRemember)
                {
                    UXDATABASE(dwIndex).fRemembering = fTemp2;
                    UXDATABASE(dwIndex).fForgetting = !fTemp2;
                }

                UXDATABASE(dwIndex).fRemember = fTemp2;
                UXDATABASE(dwIndex).fSyncByDefault = ThemeIsControlChecked(pUX->m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_SYNC_BY_DEFAULT_CHECKBOX);
                UXDATABASE(dwIndex).fChecked = UXDATABASE(dwIndex).fSyncByDefault;

                hr = pUX->RefreshOtherDatabaseList();
                ExitOnFailure(hr, "Failed to refresh other database listview");

                if (pUX->m_fAdding)
                {
                    if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, fTemp1 ? WM_BROWSE_CREATE_REMOTE : WM_BROWSE_OPEN_REMOTE, static_cast<LPARAM>(dwIndex), 0))
                    {
                        ExitWithLastError(hr, "Failed to send message to worker thread to create/open remote database");
                    }
                }
                else if (UXDATABASE(dwIndex).fRemembering)
                {
                    if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_REMEMBER, static_cast<WPARAM>(dwIndex), 0))
                    {
                        ExitWithLastError(hr, "Failed to send message to worker thread to remember database");
                    }
                }
                else if (UXDATABASE(dwIndex).fForgetting)
                {
                    if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_FORGET, static_cast<WPARAM>(dwIndex), 0))
                    {
                        ExitWithLastError(hr, "Failed to send message to worker thread to forget database");
                    }
                }

                hr = pUX->SetPreviousScreen();
                ExitOnFailure(hr, "Failed to move back to previous screen");
            }
            break;
        case BROWSE_CONTROL_SET_EXTERNAL_RECONNECT_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.fInitializing = TRUE;

                hr = pUX->RefreshOtherDatabaseList();
                ExitOnFailure(hr, "Failed to refresh database list");

                if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_OPEN_REMOTE, static_cast<LPARAM>(pUX->m_dwOtherDatabaseIndex), 0))
                {
                    ExitWithLastError(hr, "Failed to send message to worker thread to reconnect to remote database (index %u, name %ls, path %ls)", pUX->m_dwOtherDatabaseIndex, CURRENTUXDATABASE.sczName, CURRENTUXDATABASE.sczPath);
                }

                hr = pUX->SetPreviousScreen();
                ExitOnFailure(hr, "Failed to move back to previous screen");
            }

            break;
        case BROWSE_CONTROL_SET_EXTERNAL_DISCONNECT_BUTTON:
            if (BN_CLICKED == HIWORD(wParam))
            {
                CURRENTUXDATABASE.fInitialized = FALSE;
                CURRENTUXDATABASE.fUninitializing = TRUE;

                hr = pUX->RefreshOtherDatabaseList();
                ExitOnFailure(hr, "Failed to refresh database list");

                if (!::PostThreadMessageW(pUX->m_dwWorkThreadId, WM_BROWSE_DISCONNECT, static_cast<LPARAM>(pUX->m_dwOtherDatabaseIndex), 0))
                {
                    ExitWithLastError(hr, "Failed to send message to worker thread to disconnect remote database (index %u, name %ls, path %ls)", pUX->m_dwOtherDatabaseIndex, CURRENTUXDATABASE.sczName, CURRENTUXDATABASE.sczPath);
                }

                hr = pUX->SetPreviousScreen();
                ExitOnFailure(hr, "Failed to move back to previous screen");
            }

            break;
        }

        ExitFunction();
    }
LExit:
    if (fCsEntered)
    {
        ::LeaveCriticalSection(&UXDATABASE(dwIndex).cs);
    }
    ReleaseStr(ofn.lpstrFile);
    ReleaseStr(sczTemp1);
    ReleaseStr(sczTemp2);
    ReleaseMem(rgdwDwords);
    if (FAILED(hr))
    {
        pUX->Bomb(hr);
        return -1;
    }

    if (pUX)
    {
        return ThemeDefWindowProc(pUX->m_pTheme, hWnd, uMsg, wParam, lParam);
    }
    else
    {
        return ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
    }
}


//
// OnCreate - finishes loading the theme and sets up our UI to the initial page
//
BOOL BrowseWindow::OnCreate(
    __in HWND hWnd
    )
{
    HRESULT hr = S_OK;
    m_hWnd = hWnd;

    hr = ThemeLoadControls(m_pTheme, hWnd, vrgInitControls, countof(vrgInitControls));
    ExitOnFailure(hr, "Failed to load theme controls.");

    C_ASSERT(BROWSE_PAGE_COUNT == countof(vrgwzPageNames));
    C_ASSERT(countof(m_rgdwPageIds) == countof(vrgwzPageNames));

    ThemeGetPageIds(m_pTheme, vrgwzPageNames, m_rgdwPageIds, countof(m_rgdwPageIds));

    // Okay, we're ready for packages now.
    hr = SetMainState(BROWSE_MAIN_STATE_PRODUCTLIST);
    ExitOnFailure(hr, "Failed while switching main state");

LExit:
    if (FAILED(hr))
    {
        Bomb(hr);
    }

    return SUCCEEDED(hr);
}


HRESULT BrowseWindow::SetTab(
    __in BROWSE_TAB tab
    )
{
    HRESULT hr = S_OK;
    DWORD dwOldPageId = 0;

    // Don't do any work if we're already at the right tab
    if (tab == m_tab)
    {
        ExitFunction1(hr = S_OK);
    }

    // Make the current page invisible
    if (BROWSE_TAB_MAIN == m_tab)
    {
        DeterminePageIdMain(m_mainState, &dwOldPageId);
        ThemeShowPage(m_pTheme, dwOldPageId, SW_HIDE);
    }
    else if (BROWSE_TAB_OTHERDATABASES == m_tab)
    {
        DeterminePageIdOtherDatabases(m_otherDatabasesState, &dwOldPageId);
        ThemeShowPage(m_pTheme, dwOldPageId, SW_HIDE);
    }

    m_tab = tab;

    // Make the new page visible, refreshing any updated data if necessary
    if (BROWSE_TAB_MAIN == tab)
    {
        hr = SetDatabaseIndex(0);
        ExitOnFailure(hr, "Failed to set database index to 0");

        hr = SetMainState(m_mainState);
        ExitOnFailure(hr, "Failed while switching main state");
    }
    else if (BROWSE_TAB_OTHERDATABASES == tab)
    {
        hr = SetDatabaseIndex(m_dwOtherDatabaseIndex);
        ExitOnFailure(hr, "Failed to set database index to %u", m_dwOtherDatabaseIndex);

        hr = SetOtherDatabasesState(m_otherDatabasesState);
        ExitOnFailure(hr, "Failed while switching 'other databases' state");
    }

LExit:
    return hr;
}


//
// SetMainState
//
HRESULT BrowseWindow::SetMainState(
    __in BROWSE_MAIN_STATE state
    )
{
    HRESULT hr = S_OK;
    DWORD dwNewPageId = 0;
    DWORD dwOldPageId = 0;

    Trace(REPORT_STANDARD, "BrowseWindow::SetMainState() changing from state %d to %d", m_mainState, state);

    if (state != m_mainState)
    {
        DeterminePageIdMain(m_mainState, &dwOldPageId);
        ThemeShowPage(m_pTheme, dwOldPageId, SW_HIDE);
    }
    DeterminePageIdMain(state, &dwNewPageId);
    ThemeShowPage(m_pTheme, dwNewPageId, SW_SHOW);

    m_mainState = state;

    if (BROWSE_MAIN_STATE_PRODUCTLIST == state)
    {
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_PRODUCT_LIST_BACK_BUTTON, FALSE);
        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_NEW_VALUE_BUTTON, TRUE);
        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_VALUE_BUTTON, TRUE);
        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_EXPORT_FILE_BUTTON, TRUE);

        hr = RefreshProductList(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh product list");
    }
    else if (BROWSE_MAIN_STATE_SINGLEPRODUCT == state)
    {
        hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SINGLE_PRODUCT_NAME_TEXT, CURRENTDATABASE.sczCurrentProductDisplayName);
        ExitOnFailure(hr, "Failed to write to product name control for single product screen");

        hr = RefreshValueList(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh value list for main single product screen");
    }
    else if (BROWSE_MAIN_STATE_SETVALUE == state)
    {
        hr = OpenSetValueScreen();
        ExitOnFailure(hr, "Failed to setup set value screen");
    }
    else if (BROWSE_MAIN_STATE_SINGLEVALUEHISTORY == state)
    {
        CURRENTDATABASE.vhmValueHistoryMode = HISTORY_NORMAL;

        hr = RefreshValueHistoryList(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh value history listview");
    }

    ::SetFocus(m_hWnd);

LExit:
    return hr;
}


HRESULT BrowseWindow::SetOtherDatabasesState(
    __in BROWSE_OTHERDATABASES_STATE state
    )
{
    HRESULT hr = S_OK;
    DWORD dwNewPageId = 0;
    DWORD dwOldPageId = 0;

    Trace(REPORT_STANDARD, "BrowseWindow::SetOtherDatabasesState() changing from state %d to %d", m_otherDatabasesState, state);

    if (state != m_otherDatabasesState)
    {
        DeterminePageIdOtherDatabases(m_otherDatabasesState, &dwOldPageId);
        ThemeShowPage(m_pTheme, dwOldPageId, SW_HIDE);
    }
    DeterminePageIdOtherDatabases(state, &dwNewPageId);
    ThemeShowPage(m_pTheme, dwNewPageId, SW_SHOW);

    m_otherDatabasesState = state;

    if (BROWSE_OTHERDATABASES_SINGLE_DATABASE_PRODUCTLIST == state)
    {
        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_NEW_VALUE_BUTTON, TRUE);
        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_VALUE_BUTTON, TRUE);
        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_DELETE_SETTINGS_BUTTON, TRUE);
        ThemeControlEnable(m_pTheme, BROWSE_CONTROL_EXPORT_FILE_BUTTON, TRUE);

        hr = EnumerateProducts(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to send message to enumerate products");
    }
    else if (BROWSE_OTHERDATABASES_STATE_SET_EXTERNAL == state)
    {
        if (m_fAdding)
        {
            ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_RECONNECT_BUTTON, FALSE);
            ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_DISCONNECT_BUTTON, FALSE);

            ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_CREATE_NEW_CHECKBOX, TRUE);
            ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_PATH_EDITBOX, TRUE);
            ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_NAME_EDITBOX, TRUE);

            hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_PATH_EDITBOX, L"");
            ExitOnFailure(hr, "Failed to clear text on path Editbox");

            hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_NAME_EDITBOX, L"");
            ExitOnFailure(hr, "Failed to clear text on name Editbox");

            ::SendMessageW(::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_EXTERNAL_CREATE_NEW_CHECKBOX), BM_SETCHECK, BST_UNCHECKED, 0);
            ::SendMessageW(::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_EXTERNAL_SYNC_BY_DEFAULT_CHECKBOX), BM_SETCHECK, BST_CHECKED, 0);
            ::SendMessageW(::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_EXTERNAL_REMEMBER_CHECKBOX), BM_SETCHECK, BST_CHECKED, 0);
        }
        else
        {
            // Show reconnect button if we're disconnected, or disconnect button if we're connected
            ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_RECONNECT_BUTTON, !CURRENTDATABASE.fInitialized);
            ThemeShowControl(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_DISCONNECT_BUTTON, CURRENTDATABASE.fInitialized);

            // Can't 'create new' when modifying a database
            ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_CREATE_NEW_CHECKBOX, FALSE);

            // can't modify path of a database you're modifying properties for
            ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_PATH_EDITBOX, FALSE);
            hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_PATH_EDITBOX, CURRENTDATABASE.sczPath);
            ExitOnFailure(hr, "Failed to clear text on path Editbox");

            // can't modify name either, as it's currently the ID
            ThemeControlEnable(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_NAME_EDITBOX, FALSE);
            hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SET_EXTERNAL_NAME_EDITBOX, CURRENTDATABASE.sczName);
            ExitOnFailure(hr, "Failed to clear text on name Editbox");

            ::SendMessageW(::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_EXTERNAL_CREATE_NEW_CHECKBOX), BM_SETCHECK, BST_UNCHECKED, 0);
            ::SendMessageW(::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_EXTERNAL_SYNC_BY_DEFAULT_CHECKBOX), BM_SETCHECK, CURRENTDATABASE.fSyncByDefault ? BST_CHECKED : BST_UNCHECKED, 0);
            ::SendMessageW(::GetDlgItem(m_hWnd, BROWSE_CONTROL_SET_EXTERNAL_REMEMBER_CHECKBOX), BM_SETCHECK, CURRENTDATABASE.fRemember ? BST_CHECKED : BST_UNCHECKED, 0);
        }
    }
    else if (BROWSE_OTHERDATABASES_SINGLE_PRODUCT_CONFLICTS == state)
    {
        hr = RefreshSingleProductValuesConflictList(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh single product conflicting values list");
    }
    else if (BROWSE_OTHERDATABASES_SINGLE_DATABASE_CONFLICTS == state)
    {
        hr = RefreshSingleDatabaseConflictList(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh single database conflict list");
    }
    else if (BROWSE_OTHERDATABASES_VIEW_MY_VALUE_CONFLICTS == state)
    {
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SINGLE_VALUE_HISTORY_EXPORT_FILE_BUTTON, FALSE);

        CURRENTDATABASE.vhmValueHistoryMode = HISTORY_LOCAL_CONFLICTS;

        hr = RefreshValueHistoryList(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh value history list");
    }
    else if (BROWSE_OTHERDATABASES_VIEW_OTHER_VALUE_CONFLICTS == state)
    {
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SINGLE_VALUE_HISTORY_EXPORT_FILE_BUTTON, FALSE);

        CURRENTDATABASE.vhmValueHistoryMode = HISTORY_REMOTE_CONFLICTS;

        hr = RefreshValueHistoryList(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh value history list");
    }
    else if (BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEPRODUCT == state)
    {
        hr = ThemeSetTextControl(m_pTheme, BROWSE_CONTROL_SINGLE_PRODUCT_NAME_TEXT, CURRENTDATABASE.sczCurrentProductDisplayName);
        ExitOnFailure(hr, "Failed to write to product name control for other databases single product screen");

        hr = RefreshValueList(m_dwDatabaseIndex);
        ExitOnFailure(hr, "Failed to refresh value list for other databases single product screen");
    }
    else if (BROWSE_OTHERDATABASES_SINGLE_DATABASE_SETVALUE == state)
    {
        hr = OpenSetValueScreen();
        ExitOnFailure(hr, "Failed to setup set value screen");
    }
    else if (BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEVALUEHISTORY == state)
    {
        ThemeShowControl(m_pTheme, BROWSE_CONTROL_SINGLE_VALUE_HISTORY_EXPORT_FILE_BUTTON, TRUE);
    }

    ::SetFocus(m_hWnd);

LExit:
    return hr;
}

HRESULT BrowseWindow::SetPreviousScreen()
{
    HRESULT hr = S_OK;

    switch (m_tab)
    {
    case BROWSE_TAB_MAIN:
        switch (m_mainState)
        {
        case BROWSE_MAIN_STATE_SINGLEVALUEHISTORY:
            hr = SetMainState(BROWSE_MAIN_STATE_SINGLEPRODUCT);
            break;
        case BROWSE_MAIN_STATE_SETVALUE:
            hr = SetMainState(BROWSE_MAIN_STATE_SINGLEPRODUCT);
            break;
        case BROWSE_MAIN_STATE_SINGLEPRODUCT:
            hr = SetMainState(BROWSE_MAIN_STATE_PRODUCTLIST);
            break;
        }
        break;

    case BROWSE_TAB_OTHERDATABASES:
        switch (m_otherDatabasesState)
        {
        case BROWSE_OTHERDATABASES_STATE_SET_EXTERNAL:
            hr = SetOtherDatabasesState(BROWSE_OTHERDATABASES_STATE_MAIN);
            break;
        case BROWSE_OTHERDATABASES_SINGLE_DATABASE_CONFLICTS:
            hr = SetOtherDatabasesState(BROWSE_OTHERDATABASES_STATE_MAIN);
            break;
        case BROWSE_OTHERDATABASES_SINGLE_PRODUCT_CONFLICTS:
            hr = SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_CONFLICTS);
            break;
        case BROWSE_OTHERDATABASES_VIEW_MY_VALUE_CONFLICTS: __fallthrough;
        case BROWSE_OTHERDATABASES_VIEW_OTHER_VALUE_CONFLICTS: __fallthrough;
            hr = SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_PRODUCT_CONFLICTS);
            break;

        case BROWSE_OTHERDATABASES_SINGLE_DATABASE_PRODUCTLIST:
            hr = SetOtherDatabasesState(BROWSE_OTHERDATABASES_STATE_MAIN);
            break;
        case BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEVALUEHISTORY:
            hr = SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEPRODUCT);
            break;
        case BROWSE_OTHERDATABASES_SINGLE_DATABASE_SETVALUE:
            hr = SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEPRODUCT);
            break;
        case BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEPRODUCT:
            hr = SetOtherDatabasesState(BROWSE_OTHERDATABASES_SINGLE_DATABASE_PRODUCTLIST);
            break;
        }
        break;
    }

    return hr;
}

void BrowseWindow::DeterminePageIdMain(
    __in BROWSE_MAIN_STATE state,
    __out DWORD* pdwPageId
    ) const
{
    switch (state)
    {
    case BROWSE_MAIN_STATE_PRODUCTLIST:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_PRODUCTLIST];
        break;
    case BROWSE_MAIN_STATE_SINGLEPRODUCT:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_SINGLEPRODUCT];
        break;
    case BROWSE_MAIN_STATE_SETVALUE:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_SETVALUE];
        break;
    case BROWSE_MAIN_STATE_SINGLEVALUEHISTORY:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_SINGLEVALUEHISTORY];
        break;
    }
}

void BrowseWindow::DeterminePageIdOtherDatabases(
    __in BROWSE_OTHERDATABASES_STATE state,
    __out DWORD* pdwPageId
    )
{
    switch (state)
    {
    case BROWSE_OTHERDATABASES_STATE_MAIN:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_OTHER_DATABASE_LIST];
        break;
    case BROWSE_OTHERDATABASES_STATE_SET_EXTERNAL:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_SET_EXTERNAL_DATABASE];
        break;
    case BROWSE_OTHERDATABASES_SINGLE_DATABASE_CONFLICTS:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_CONFLICTS_PRODUCTLIST];
        break;
    case BROWSE_OTHERDATABASES_SINGLE_PRODUCT_CONFLICTS:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_CONFLICTS_SINGLEPRODUCT];
        break;
    case BROWSE_OTHERDATABASES_VIEW_MY_VALUE_CONFLICTS: __fallthrough;
    case BROWSE_OTHERDATABASES_VIEW_OTHER_VALUE_CONFLICTS: __fallthrough;
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_SINGLEVALUEHISTORY];
        break;

    // These mimic the main states, but show the same data for another database
    case BROWSE_OTHERDATABASES_SINGLE_DATABASE_PRODUCTLIST:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_PRODUCTLIST];
        break;
    case BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEPRODUCT:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_SINGLEPRODUCT];
        break;
    case BROWSE_OTHERDATABASES_SINGLE_DATABASE_SETVALUE:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_SETVALUE];
        break;
    case BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEVALUEHISTORY:
        *pdwPageId = m_rgdwPageIds[BROWSE_PAGE_SINGLEVALUEHISTORY];
        break;
    }
}


//
// Constructor - intitialize member variables.
//
BrowseWindow::BrowseWindow(
    __in HMODULE hModule,
    __in BROWSE_DATABASE_LIST *pbdlDatabaseList
    )
{
    m_hUiThread = NULL;
    m_dwWorkThreadId = 0;

    m_hModule = hModule;

    m_pTheme = NULL;
    m_pLoc = NULL;
    m_fRegistered = FALSE;
    m_hWnd = NULL;
    m_hMenu = NULL;
    m_fVisible = FALSE;

    m_mainState = BROWSE_MAIN_STATE_PRODUCTLIST;
    m_otherDatabasesState = BROWSE_OTHERDATABASES_STATE_MAIN;
    m_tab = BROWSE_TAB_MAIN;

    m_sczLanguage = NULL;

    m_dwLocalDatabaseIndex = 0;

    // Open other database functionality
    m_dwDatabaseIndex = 0;
    m_dwOtherDatabaseIndex = 0;
    ZeroMemory(m_wzOtherDatabaseLocation, sizeof(m_wzOtherDatabaseLocation));
    m_pbdlDatabaseList = pbdlDatabaseList;
    m_fBackgroundThreadResumed = FALSE;

    m_fAdding = FALSE;
}


//
// Destructor - release member variables.
//
BrowseWindow::~BrowseWindow()
{
    DestroyMainWindow();

    ReleaseTheme(m_pTheme);
    if (m_pLoc)
    {
        LocFree(m_pLoc);
        m_pLoc = NULL;
    }
}
