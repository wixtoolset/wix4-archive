#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define NUM_FILE_SELECTION_DIALOG_CHARACTERS MAX_PATH

const LPCWSTR BROWSE_WINDOW_CLASS = L"Browse";

const LPCWSTR wzBrowserProductId = L"WiX_Cfg";
const LPCWSTR wzBrowserVersion = L"1.0.0.0";
const LPCWSTR wzBrowserPublicKey = L"B77A5C561934E089";

const LPCWSTR BROWSER_SETTING_SHOW_UNINSTALLED_PRODUCTS = L"Browser:\\ShowUninstalledProducts";
const LPCWSTR BROWSER_SETTING_SHOW_DELETED_VALUES = L"Browser:\\ShowDeletedValues";

enum BROWSE_TAB
{
    BROWSE_TAB_MAIN,
    BROWSE_TAB_OTHERDATABASES
};

enum BROWSE_MAIN_STATE
{
    BROWSE_MAIN_STATE_PRODUCTLIST,
    BROWSE_MAIN_STATE_SINGLEPRODUCT,
    BROWSE_MAIN_STATE_SETVALUE,
    BROWSE_MAIN_STATE_SINGLEVALUEHISTORY
};

enum BROWSE_OTHERDATABASES_STATE
{
    BROWSE_OTHERDATABASES_STATE_MAIN,
    BROWSE_OTHERDATABASES_STATE_SET_EXTERNAL,

    BROWSE_OTHERDATABASES_SINGLE_DATABASE_CONFLICTS,
    BROWSE_OTHERDATABASES_SINGLE_PRODUCT_CONFLICTS,
    BROWSE_OTHERDATABASES_VIEW_MY_VALUE_CONFLICTS,
    BROWSE_OTHERDATABASES_VIEW_OTHER_VALUE_CONFLICTS,

    BROWSE_OTHERDATABASES_SINGLE_DATABASE_PRODUCTLIST,
    BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEPRODUCT,
    BROWSE_OTHERDATABASES_SINGLE_DATABASE_SETVALUE,
    BROWSE_OTHERDATABASES_SINGLE_DATABASE_SINGLEVALUEHISTORY,
};

// This enum must be kept in the same order as the vrgwzPageNames array.
enum BROWSE_PAGE
{
    BROWSE_PAGE_PRODUCTLIST,
    BROWSE_PAGE_SINGLEPRODUCT,
    BROWSE_PAGE_SETVALUE,
    BROWSE_PAGE_SINGLEVALUEHISTORY,
    BROWSE_PAGE_OTHER_DATABASE_LIST,
    BROWSE_PAGE_SET_EXTERNAL_DATABASE,
    BROWSE_PAGE_CONFLICTS_PRODUCTLIST,
    BROWSE_PAGE_CONFLICTS_SINGLEPRODUCT,
    BROWSE_PAGE_COUNT
};

// This array must be kept in the same order as the BROWSE_PAGE enum.
__declspec(selectany) LPCWSTR vrgwzPageNames[] = {
    L"ProductList",
    L"SingleProduct",
    L"SetValue",
    L"SingleValueHistory",
    L"OtherDatabaseList",
    L"SetExternalDatabase",
    L"ConflictsProductList",
    L"SingleProductConflicts"
};

enum BROWSE_CONTROL
{
    // Global
    BROWSE_CONTROL_MAIN_TAB_CONTROL,

    // Product List screen
    BROWSE_CONTROL_PRODUCT_LIST_DATABASE_NAME_TEXT,
    BROWSE_CONTROL_PRODUCT_LIST_VIEW,
    BROWSE_CONTROL_PRODUCT_LIST_SHOW_UNINSTALLED_PRODUCTS_CHECKBOX,
    BROWSE_CONTROL_PRODUCT_LIST_BACK_BUTTON,
    BROWSE_CONTROL_PRODUCT_LIST_PRODUCT_FORGET_BUTTON,

    // Single product screen
    BROWSE_CONTROL_SINGLE_PRODUCT_DATABASE_NAME_TEXT,
    BROWSE_CONTROL_SINGLE_PRODUCT_NAME_TEXT,
    BROWSE_CONTROL_VALUE_LIST_VIEW,
    BROWSE_CONTROL_PRODUCT_LIST_SHOW_DELETED_VALUES_CHECKBOX,
    BROWSE_CONTROL_SINGLE_PRODUCT_BACK_BUTTON,
    BROWSE_CONTROL_NEW_VALUE_BUTTON,
    BROWSE_CONTROL_SET_VALUE_BUTTON,
    BROWSE_CONTROL_DELETE_SETTINGS_BUTTON,
    BROWSE_CONTROL_VIEW_VALUE_HISTORY_BUTTON,
    BROWSE_CONTROL_EXPORT_FILE_BUTTON,

    // Set value screen
    BROWSE_CONTROL_SET_VALUE_DATABASE_NAME_TEXT,
    BROWSE_CONTROL_SET_VALUE_NAME,
    BROWSE_CONTROL_SET_VALUE_NAME_EDITBOX,
    BROWSE_CONTROL_SET_VALUE_CANCEL_BUTTON,
    BROWSE_CONTROL_SET_VALUE_SAVE_BUTTON,
    BROWSE_CONTROL_SET_VALUE_FILE_TYPE,
    BROWSE_CONTROL_SET_VALUE_PATH_TEXT,
    BROWSE_CONTROL_SET_VALUE_PATH_EDITBOX,
    BROWSE_CONTROL_SET_VALUE_BROWSE_BUTTON,
    BROWSE_CONTROL_SET_VALUE_DWORD_TYPE,
    BROWSE_CONTROL_SET_VALUE_QWORD_TYPE,
    BROWSE_CONTROL_SET_VALUE_STRING_TYPE,
    BROWSE_CONTROL_SET_VALUE_BOOL_TYPE,
    BROWSE_CONTROL_SET_VALUE_EDITBOX,
    BROWSE_CONTROL_SET_VALUE_CHECKBOX,
    BROWSE_CONTROL_SET_VALUE_ERROR_TEXT,

    // Single value history screen
    BROWSE_CONTROL_SINGLE_VALUE_HISTORY_DATABASE_NAME_TEXT,
    BROWSE_CONTROL_SINGLE_VALUE_HISTORY_PRODUCTNAME,
    BROWSE_CONTROL_SINGLE_VALUE_HISTORY_VALUENAME,
    BROWSE_CONTROL_SINGLE_VALUE_HISTORY_LIST_VIEW,
    BROWSE_CONTROL_SINGLE_VALUE_HISTORY_BACK_BUTTON,
    BROWSE_CONTROL_SINGLE_VALUE_HISTORY_EXPORT_FILE_BUTTON,

    // Other databases main screen (database list)
    BROWSE_CONTROL_OTHERDATABASES_VIEW,
    BROWSE_CONTROL_OTHERDATABASES_SYNC_BUTTON,
    BROWSE_CONTROL_OTHERDATABASES_VIEW_DATABASE_CONFLICTS,
    BROWSE_CONTROL_OTHERDATABASES_SET_EXTERNAL_BUTTON,
    BROWSE_CONTROL_OTHERDATABASES_MODIFY_EXTERNAL_BUTTON,

    // Add external database screen
    BROWSE_CONTROL_SET_EXTERNAL_PATH_EDITBOX,
    BROWSE_CONTROL_SET_EXTERNAL_NAME_EDITBOX,
    BROWSE_CONTROL_SET_EXTERNAL_CREATE_NEW_CHECKBOX,
    BROWSE_CONTROL_SET_EXTERNAL_SYNC_BY_DEFAULT_CHECKBOX,
    BROWSE_CONTROL_SET_EXTERNAL_REMEMBER_CHECKBOX,
    BROWSE_CONTROL_SET_EXTERNAL_BROWSE_BUTTON,
    BROWSE_CONTROL_SET_EXTERNAL_CANCEL_BUTTON,
    BROWSE_CONTROL_SET_EXTERNAL_OK_BUTTON,
    BROWSE_CONTROL_SET_EXTERNAL_RECONNECT_BUTTON,
    BROWSE_CONTROL_SET_EXTERNAL_DISCONNECT_BUTTON,

    // Single (other) database product list
    BROWSE_CONTROL_SINGLE_DB_CONFLICTS_DATABASE_NAME_TEXT,
    BROWSE_CONTROL_SINGLE_DB_CONFLICTS_VIEW,
    BROWSE_CONTROL_SINGLE_DB_BACK_BUTTON,
    BROWSE_CONTROL_SINGLE_DATABASE_SYNC_BUTTON,
    BROWSE_CONTROL_READ_LEGACY_SETTINGS_BUTTON,
    BROWSE_CONTROL_IMPORT_LEGACY_MANIFEST_BUTTON,
    BROWSE_CONTROL_ACCEPT_MINE_BUTTON,
    BROWSE_CONTROL_ACCEPT_OTHER_BUTTON,

    // Other database single product screen
    BROWSE_CONTROL_CONFLICT_VALUES_DATABASE_NAME_TEXT,
    BROWSE_CONTROL_CONFLICT_VALUES_PRODUCT_NAME_TEXT,
    BROWSE_CONTROL_CONFLICT_VALUES_VIEW,
    BROWSE_CONTROL_VIEW_MY_VALUE_HISTORY_BUTTON,
    BROWSE_CONTROL_VIEW_OTHER_VALUE_HISTORY_BUTTON,
    BROWSE_CONTROL_SINGLE_PRODUCT_ACCEPT_MINE_BUTTON,
    BROWSE_CONTROL_SINGLE_PRODUCT_ACCEPT_OTHER_BUTTON,
    BROWSE_CONTROL_CANCEL_BUTTON
};

__declspec(selectany) THEME_ASSIGN_CONTROL_ID vrgInitControls[] = {
    { BROWSE_CONTROL_MAIN_TAB_CONTROL, L"MainTabControl" },

    { BROWSE_CONTROL_PRODUCT_LIST_DATABASE_NAME_TEXT, L"ProductListDatabaseName" },
    { BROWSE_CONTROL_PRODUCT_LIST_VIEW, L"ProductListView" },
    { BROWSE_CONTROL_PRODUCT_LIST_SHOW_UNINSTALLED_PRODUCTS_CHECKBOX, L"ShowUninstalledProductsCheckBox" },
    { BROWSE_CONTROL_PRODUCT_LIST_BACK_BUTTON, L"ProductListBackButton" },
    { BROWSE_CONTROL_PRODUCT_LIST_PRODUCT_FORGET_BUTTON, L"ForgetProductButton" },
    { BROWSE_CONTROL_READ_LEGACY_SETTINGS_BUTTON, L"ReadLegacySettingsButton" },
    { BROWSE_CONTROL_IMPORT_LEGACY_MANIFEST_BUTTON, L"ImportLegacyManifestButton" },

    { BROWSE_CONTROL_SINGLE_PRODUCT_DATABASE_NAME_TEXT, L"SingleProductDatabaseName" },
    { BROWSE_CONTROL_SINGLE_PRODUCT_NAME_TEXT, L"SingleProductNameText" },
    { BROWSE_CONTROL_VALUE_LIST_VIEW, L"ValueListView" },
    { BROWSE_CONTROL_PRODUCT_LIST_SHOW_DELETED_VALUES_CHECKBOX, L"ShowDeletedValuesCheckBox" },
    { BROWSE_CONTROL_SINGLE_PRODUCT_BACK_BUTTON, L"SingleProductBackButton" },
    { BROWSE_CONTROL_NEW_VALUE_BUTTON, L"NewValueButton" },
    { BROWSE_CONTROL_SET_VALUE_BUTTON, L"SetValueButton" },
    { BROWSE_CONTROL_DELETE_SETTINGS_BUTTON, L"DeleteSettingsButton" },
    { BROWSE_CONTROL_VIEW_VALUE_HISTORY_BUTTON, L"ViewValueHistoryButton" },
    { BROWSE_CONTROL_EXPORT_FILE_BUTTON, L"ExportFileButton" },
    
    // Set value screen
    { BROWSE_CONTROL_SET_VALUE_DATABASE_NAME_TEXT, L"SetValueDatabaseName" },
    { BROWSE_CONTROL_SET_VALUE_NAME, L"SetValueName" },
    { BROWSE_CONTROL_SET_VALUE_NAME_EDITBOX, L"SetValueNameEditBox" },
    { BROWSE_CONTROL_SET_VALUE_CANCEL_BUTTON, L"SetValueCancelButton" },
    { BROWSE_CONTROL_SET_VALUE_SAVE_BUTTON, L"SetValueSaveButton" },
    { BROWSE_CONTROL_SET_VALUE_FILE_TYPE, L"SetValueFileType" },
    { BROWSE_CONTROL_SET_VALUE_PATH_TEXT, L"SetValuePathText" },
    { BROWSE_CONTROL_SET_VALUE_PATH_EDITBOX, L"SetValuePathEditBox" },
    { BROWSE_CONTROL_SET_VALUE_BROWSE_BUTTON, L"SetValueBrowseButton" },
    { BROWSE_CONTROL_SET_VALUE_DWORD_TYPE, L"SetValueDwordType" },
    { BROWSE_CONTROL_SET_VALUE_QWORD_TYPE, L"SetValueQwordType" },
    { BROWSE_CONTROL_SET_VALUE_STRING_TYPE, L"SetValueStringType" },
    { BROWSE_CONTROL_SET_VALUE_BOOL_TYPE, L"SetValueBoolType" },
    { BROWSE_CONTROL_SET_VALUE_EDITBOX, L"SetValueEditBox" },
    { BROWSE_CONTROL_SET_VALUE_CHECKBOX, L"SetValueCheckBox" },
    { BROWSE_CONTROL_SET_VALUE_ERROR_TEXT, L"SetValueErrorText" },

    // Single value history screen
    { BROWSE_CONTROL_SINGLE_VALUE_HISTORY_DATABASE_NAME_TEXT, L"SingleValueHistoryDatabaseName" },
    { BROWSE_CONTROL_SINGLE_VALUE_HISTORY_PRODUCTNAME, L"SingleValueHistoryProductName" },
    { BROWSE_CONTROL_SINGLE_VALUE_HISTORY_VALUENAME, L"SingleValueHistoryValueName" },
    { BROWSE_CONTROL_SINGLE_VALUE_HISTORY_LIST_VIEW, L"SingleValueHistoryView" },
    { BROWSE_CONTROL_SINGLE_VALUE_HISTORY_BACK_BUTTON, L"SingleValueHistoryBackButton" },
    { BROWSE_CONTROL_SINGLE_VALUE_HISTORY_EXPORT_FILE_BUTTON, L"SingleFileHistoryExportFileButton" },

    // Other databases main screen (database list)
    { BROWSE_CONTROL_OTHERDATABASES_VIEW, L"OtherDatabasesMainScreenView" },
    { BROWSE_CONTROL_OTHERDATABASES_SYNC_BUTTON, L"OtherDatabasesMainScreenSyncButton" },
    { BROWSE_CONTROL_OTHERDATABASES_VIEW_DATABASE_CONFLICTS, L"OtherDatabasesMainScreenViewConflicts" },
    { BROWSE_CONTROL_OTHERDATABASES_SET_EXTERNAL_BUTTON, L"OtherDatabasesMainScreenAddExternalButton" },
    { BROWSE_CONTROL_OTHERDATABASES_MODIFY_EXTERNAL_BUTTON, L"OtherDatabasesMainScreenModifyExternalButton" },

    // Set external database screen
    { BROWSE_CONTROL_SET_EXTERNAL_PATH_EDITBOX, L"SetExternalDatabasePathEditbox" },
    { BROWSE_CONTROL_SET_EXTERNAL_NAME_EDITBOX, L"SetExternalDatabaseNameEditbox" },
    { BROWSE_CONTROL_SET_EXTERNAL_CREATE_NEW_CHECKBOX, L"SetExternalDatabaseCreateNewCheckBox" },
    { BROWSE_CONTROL_SET_EXTERNAL_SYNC_BY_DEFAULT_CHECKBOX, L"SetExternalDatabaseSyncByDefaultCheckBox" },
    { BROWSE_CONTROL_SET_EXTERNAL_REMEMBER_CHECKBOX, L"SetExternalDatabaseRememberCheckBox" },
    { BROWSE_CONTROL_SET_EXTERNAL_BROWSE_BUTTON, L"SetExternalDatabaseBrowseButton" },
    { BROWSE_CONTROL_SET_EXTERNAL_CANCEL_BUTTON, L"SetExternalDatabaseCancelButton" },
    { BROWSE_CONTROL_SET_EXTERNAL_OK_BUTTON, L"SetExternalDatabaseOKButton" },
    { BROWSE_CONTROL_SET_EXTERNAL_RECONNECT_BUTTON, L"SetExternalDatabaseReconnectButton" },
    { BROWSE_CONTROL_SET_EXTERNAL_DISCONNECT_BUTTON, L"SetExternalDatabaseDisconnectButton" },

    // Single (other) database product list
    { BROWSE_CONTROL_SINGLE_DB_CONFLICTS_DATABASE_NAME_TEXT, L"SingleDbConflictsDatabaseName" },
    { BROWSE_CONTROL_SINGLE_DB_CONFLICTS_VIEW, L"SingleDbConflictsView" },
    { BROWSE_CONTROL_SINGLE_DB_BACK_BUTTON, L"SingleDbConflictsBackButton" },
    { BROWSE_CONTROL_SINGLE_DATABASE_SYNC_BUTTON, L"SingleDbConflictsSyncButton" },
    { BROWSE_CONTROL_ACCEPT_MINE_BUTTON, L"SingleDbConflictsAcceptMine" },
    { BROWSE_CONTROL_ACCEPT_OTHER_BUTTON, L"SingleDbConflictsAcceptOther" },

    // Single product conflict screen
    { BROWSE_CONTROL_CONFLICT_VALUES_DATABASE_NAME_TEXT, L"SingleProductConflictsDatabaseName" },
    { BROWSE_CONTROL_CONFLICT_VALUES_PRODUCT_NAME_TEXT, L"SingleProductConflictsNameText" },
    { BROWSE_CONTROL_CONFLICT_VALUES_VIEW, L"SingleProductConflictsValuesView" },
    { BROWSE_CONTROL_VIEW_MY_VALUE_HISTORY_BUTTON, L"SingleProductConflictsMyValueHistoryButton" },
    { BROWSE_CONTROL_VIEW_OTHER_VALUE_HISTORY_BUTTON, L"SingleProductConflictsOtherValueHistoryButton" },
    { BROWSE_CONTROL_SINGLE_PRODUCT_ACCEPT_MINE_BUTTON, L"SingleProductConflictsAcceptMine" },
    { BROWSE_CONTROL_SINGLE_PRODUCT_ACCEPT_OTHER_BUTTON, L"SingleProductConflictsAcceptOther" },
    { BROWSE_CONTROL_CANCEL_BUTTON, L"SingleProductConflictsCancelButton" }
};

class BrowseWindow
{
public:
    BrowseWindow(
        __in HMODULE hModule,
        __in BROWSE_DATABASE_LIST *pbdlDatabaseList
        );
    ~BrowseWindow();

    STDMETHODIMP Initialize(
        __out DWORD *pdwUIThread
        );
    STDMETHODIMP_(void) Uninitialize();
    void Bomb(
        HRESULT hr
        );

    // These functions interact directly with the database, and thus should not be called from a UI thread,
    // but instead from the worker thread. Use messages WM_BROWSE_READ_SETTINGS and WM_BROWSE_PERSIST_SETTINGS
    // to tell worker thread to call them
    HRESULT ReadSettings();
    HRESULT PersistSettings();

    HRESULT EnumerateProducts(DWORD dwIndex);
    HRESULT EnumerateDatabases(DWORD dwIndex);
    HRESULT EnumerateValues(DWORD dwIndex, BOOL fDifferentProduct);
    HRESULT EnumerateValueHistory(DWORD dwIndex, BOOL fDifferentValue);

    DWORD GetSelectedValueIndex();
    DWORD GetSelectedValueHistoryIndex();
    DWORD GetSelectedConflictProductIndex();
    DWORD GetSelectedConflictValueIndex();

    HRESULT SetDatabaseIndex(DWORD dwIndex);

    // Specific Screen helper functions
    HRESULT OpenSetValueScreen();

    // Listview refresh functions
    HRESULT RefreshProductList(DWORD dwDatabaseIndex);
    HRESULT RefreshValueList(DWORD dwDatabaseIndex);
    HRESULT RefreshValueHistoryList(DWORD dwDatabaseIndex);

    HRESULT RefreshSingleDatabaseConflictList(DWORD dwDatabaseIndex);
    HRESULT RefreshSingleProductValuesConflictList(DWORD dwDatabaseIndex);

    HRESULT RefreshOtherDatabaseList();

    void RefreshTypeRadioButton();

    // Shows / hides controls for specific screens based on state member variables
    void RefreshSetValueScreenVisibility();

private:
    HRESULT SetSelectedProduct(
        __in DWORD dwIndex
        );
    static DWORD WINAPI BrowseWindow::UiThreadProc(
        __in_bcount(sizeof(BrowseWindow)) LPVOID pvContext
        );
    HRESULT CreateMainWindow();
    void DestroyMainWindow();
    static LRESULT CALLBACK WndProc(
        __in HWND hWnd,
        __in UINT uMsg,
        __in WPARAM wParam,
        __in LPARAM lParam
        );
    BOOL OnCreate(
        __in HWND hWnd
        );
    HRESULT SetTab(
        __in BROWSE_TAB tab
        );
    HRESULT SetMainState(
        __in BROWSE_MAIN_STATE state
        );
    HRESULT SetOtherDatabasesState(
        __in BROWSE_OTHERDATABASES_STATE state
        );
    HRESULT SetPreviousScreen();
    void DeterminePageIdMain(
        __in BROWSE_MAIN_STATE state,
        __out DWORD* pdwPageId
        ) const;
    void DeterminePageIdOtherDatabases(
        __in BROWSE_OTHERDATABASES_STATE state,
        __out DWORD* pdwPageId
        );

    HANDLE m_hUiThread;
    DWORD m_dwWorkThreadId;

    HMODULE m_hModule;

    THEME* m_pTheme;
    WIX_LOCALIZATION* m_pLoc;
    DWORD m_rgdwPageIds[countof(vrgwzPageNames)];
    BOOL m_fRegistered;
    HWND m_hWnd;
    HMENU m_hMenu;
    BOOL m_fVisible;

    BROWSE_MAIN_STATE m_mainState;
    BROWSE_OTHERDATABASES_STATE m_otherDatabasesState;
    BROWSE_TAB m_tab;

    LPWSTR m_sczLanguage;

    DWORD m_dwLocalDatabaseIndex;

    // Open other database functionality
    DWORD m_dwDatabaseIndex;
    DWORD m_dwOtherDatabaseIndex;
    WCHAR m_wzOtherDatabaseLocation[MAX_PATH];
    BROWSE_DATABASE_LIST *m_pbdlDatabaseList;
    BOOL m_fBackgroundThreadResumed;

    // Add / modify database in list
    BOOL m_fAdding;

    // Browser settings
    BOOL m_fShowUninstalledProducts;
    BOOL m_fShowDeletedValues;
};

#ifdef __cplusplus
}
#endif
