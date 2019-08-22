// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsCfgProductQuery =
    L"SELECT `Name`, `Version`, `PublicKey`, `Component_` FROM `WixCfgProducts`";
enum eCfgProductQuery { ceqName = 1, ceqVersion, ceqPublicKey, ceqComponent };

/******************************************************************
 SchedCfgProducts - immediate custom action worker to 
   register and remove cfg products.

********************************************************************/
static UINT SchedCfgHandleProducts(
    __in MSIHANDLE hInstall,
    __in WCA_TODO todoSched
    )
{
    //AssertSz(FALSE, "debug SchedCfgHandleProducts here");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    int cCfgProducts = 0;
    BOOL fAlreadyExists;
    BOOL fAdminInitialized = FALSE;
    CFGDB_HANDLE cdhAdmin = NULL;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    LPWSTR sczCustomActionData = NULL;
    LPWSTR sczName = NULL;
    LPWSTR sczVersion = NULL;
    LPWSTR sczPublicKey = NULL;
    LPWSTR sczComponent = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "SchedCfgHandleProducts");
    ExitOnFailure(hr, "failed to initialize");

    // anything to do?
    if (S_OK != WcaTableExists(L"WixCfgProducts"))
    {
        WcaLog(LOGMSG_STANDARD, "WixCfgProducts table doesn't exist, so there are no cfg products to configure.");
        ExitFunction();
    }

    hr = CfgAdminInitialize(&cdhAdmin, FALSE);
    ExitOnFailure(hr, "failed to initialize global database");
    fAdminInitialized = TRUE;

    // query and loop through all the cfg products
    hr = WcaOpenExecuteView(vcsCfgProductQuery, &hView);
    ExitOnFailure(hr, "failed to open view on WixCfgProducts table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        fAlreadyExists = FALSE;

        hr = WcaGetRecordString(hRec, ceqName, &sczName);
        ExitOnFailure(hr, "failed to get cfg product name");

        hr = WcaGetRecordString(hRec, ceqVersion, &sczVersion);
        ExitOnFailure(hr, "failed to get cfg product version");

        hr = WcaGetRecordString(hRec, ceqPublicKey, &sczPublicKey);
        ExitOnFailure(hr, "failed to get cfg public key");

        hr = WcaGetRecordString(hRec, ceqComponent, &sczComponent);
        ExitOnFailure(hr, "failed to get cfg component");

        // figure out what we're doing for this product, treating reinstall the same as install
        WCA_TODO todoComponent = WcaGetComponentToDo(sczComponent);
        if ((WCA_TODO_REINSTALL == todoComponent ? WCA_TODO_INSTALL : todoComponent) != todoSched)
        {
            WcaLog(LOGMSG_STANDARD, "Component '%ls' action state (%d) doesn't match request (%d)", sczComponent, todoComponent, todoSched);
            continue;
        }

        hr = CfgAdminIsProductRegistered(cdhAdmin, sczName, sczVersion, sczPublicKey, &fAlreadyExists);
        ExitOnFailure(hr, "Failed to check if product is already registered in admin context");

        // Don't do work that doesn't need done. This is important so we only do things that
        // actually change state, because the same list (with opposite install action) is used in case of rollback
        if (fAlreadyExists && WCA_TODO_INSTALL == todoSched)
        {
            continue; // If we were going to install it but it already exists, skip it
        }
        else if (!fAlreadyExists && WCA_TODO_UNINSTALL == todoSched)
        {
            continue; // If we were going to uninstall it but it doesn't exist, skip it
        }

        // name :: version :: publickey
        ++cCfgProducts;
        hr = WcaWriteIntegerToCaData(todoComponent, &sczCustomActionData);
        ExitOnFailure(hr, "failed to write product action to custom action data");

        hr = WcaWriteStringToCaData(sczName, &sczCustomActionData);
        ExitOnFailure(hr, "failed to write product name to custom action data");

        hr = WcaWriteStringToCaData(sczVersion, &sczCustomActionData);
        ExitOnFailure(hr, "failed to write product version to custom action data");

        hr = WcaWriteStringToCaData(sczPublicKey, &sczCustomActionData);
        ExitOnFailure(hr, "failed to write product public key to custom action data");
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    } 
    ExitOnFailure(hr, "failure occured while processing WixCfgProducts table");

    // schedule ExecCfgProducts if there's anything to do
    if (sczCustomActionData && *sczCustomActionData)
    {
        WcaLog(LOGMSG_STANDARD, "Scheduling cfg product (%ls)", sczCustomActionData);

        if (WCA_TODO_INSTALL == todoSched)
        {
            hr = WcaDoDeferredAction(L"WixRollbackCfgProductsInstall", sczCustomActionData, cCfgProducts * COST_CFG_UNREGISTER);
            ExitOnFailure(hr, "failed to schedule cfg install products rollback");            
            hr = WcaDoDeferredAction(L"WixExecCfgProductsInstall", sczCustomActionData, cCfgProducts * COST_CFG_REGISTER);
            ExitOnFailure(hr, "failed to schedule cfg install products execution");
        }
        else
        {
            hr = WcaDoDeferredAction(L"WixRollbackCfgProductsUninstall", sczCustomActionData, cCfgProducts * COST_CFG_REGISTER);
            ExitOnFailure(hr, "failed to schedule cfg uninstall products rollback");    
            hr = WcaDoDeferredAction(L"WixExecCfgProductsUninstall", sczCustomActionData, cCfgProducts * COST_CFG_UNREGISTER);
            ExitOnFailure(hr, "failed to schedule cfg uninstall products execution");
        }
    }
    else
    {
        WcaLog(LOGMSG_STANDARD, "No cfg products scheduled");
    }

    fAdminInitialized = FALSE;
    hr = CfgAdminUninitialize(cdhAdmin);
    ExitOnFailure(hr, "Failed to uninitialize global database");

LExit:
    if (fAdminInitialized)
    {
        CfgAdminUninitialize(cdhAdmin); // we failed above - so just try to shutdown the database, but ignore errors here
    }

    ReleaseStr(sczCustomActionData);
    ReleaseStr(sczName);
    ReleaseStr(sczVersion);
    ReleaseStr(sczPublicKey);
    ReleaseStr(sczComponent);

    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}

/******************************************************************
 SchedCfgProductsInstall - immediate custom action entry
   point to register cfg products.

********************************************************************/
extern "C" UINT __stdcall SchedCfgProductsInstall(
    __in MSIHANDLE hInstall
    )
{
    return SchedCfgHandleProducts(hInstall, WCA_TODO_INSTALL);
}

/******************************************************************
 SchedCfgProductsUninstall - immediate custom action entry
   point to remove cfg products.

********************************************************************/
extern "C" UINT __stdcall SchedCfgProductsUninstall(
    __in MSIHANDLE hInstall
    )
{
    return SchedCfgHandleProducts(hInstall, WCA_TODO_UNINSTALL);
}

/******************************************************************
 ExecCfgProducts - deferred custom action entry point to 
   register and remove cfg products.

********************************************************************/
extern "C" UINT __stdcall ExecCfgProducts(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug ExecCfgProducts here");

    HRESULT hr = S_OK;
    LPWSTR sczName = NULL;
    LPWSTR sczVersion = NULL;
    LPWSTR sczPublicKey = NULL;
    LPWSTR sczCustomActionData = NULL;
    LPWSTR wz = NULL;
    CFGDB_HANDLE cdhAdmin = NULL;
    BOOL fAdminInitialized = FALSE;
    int iTodo = 0;

    // initialize
    hr = WcaInitialize(hInstall, "ExecCfgProducts");
    ExitOnFailure(hr, "failed to initialize ExecCfgProducts");

    hr = CfgAdminInitialize(&cdhAdmin, TRUE);
    ExitOnFailure(hr, "failed to initialize global database");
    fAdminInitialized = TRUE;

    hr = WcaGetProperty( L"CustomActionData", &sczCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");
    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", sczCustomActionData);

    // loop through all the passed in data
    wz = sczCustomActionData;
    while (wz && *wz)
    {
        // extract the custom action data and if rolling back, swap INSTALL and UNINSTALL
        hr = WcaReadIntegerFromCaData(&wz, &iTodo);
        ExitOnFailure(hr, "failed to read todo from custom action data");

        // Reverse operations for rollback
        if (::MsiGetMode(hInstall, MSIRUNMODE_ROLLBACK))
        {
            if (WCA_TODO_INSTALL == iTodo)
            {
                iTodo = WCA_TODO_UNINSTALL;
            }
            else if (WCA_TODO_UNINSTALL == iTodo)
            {
                iTodo = WCA_TODO_INSTALL;
            }
        }

        hr = WcaReadStringFromCaData(&wz, &sczName);
        ExitOnFailure(hr, "failed to read name from custom action data");

        hr = WcaReadStringFromCaData(&wz, &sczVersion);
        ExitOnFailure(hr, "failed to read version from custom action data");

        hr = WcaReadStringFromCaData(&wz, &sczPublicKey);
        ExitOnFailure(hr, "failed to read public key from custom action data");

        switch (iTodo)
        {
        case WCA_TODO_INSTALL:
        case WCA_TODO_REINSTALL:
            WcaLog(LOGMSG_STANDARD, "Installing cfg product %ls version %ls, public key %ls", sczName, sczVersion, sczPublicKey);
            hr = CfgAdminRegisterProduct(cdhAdmin, sczName, sczVersion, sczPublicKey);
            ExitOnFailure(hr, "failed to install cfg product: name '%ls' version %ls, public key %ls", sczName, sczVersion, sczPublicKey);
            break;

        case WCA_TODO_UNINSTALL:
            WcaLog(LOGMSG_STANDARD, "Uninstalling cfg product %ls version %ls, public key %d", sczName, sczVersion, sczPublicKey);
            hr = CfgAdminUnregisterProduct(cdhAdmin, sczName, sczVersion, sczPublicKey);
            ExitOnFailure(hr, "failed to remove cfg product: name '%ls' version %ls, public key %ls", sczName, sczVersion, sczPublicKey);
            break;
        }
    }

    fAdminInitialized = FALSE;
    hr = CfgAdminUninitialize(cdhAdmin);
    ExitOnFailure(hr, "Failed to uninitialize global database");

LExit:
    if (fAdminInitialized)
    {
        CfgAdminUninitialize(cdhAdmin); // we failed above - so just try to shutdown the database, but ignore errors here
    }

    ReleaseStr(sczCustomActionData);
    ReleaseStr(sczName);
    ReleaseStr(sczVersion);
    ReleaseStr(sczPublicKey);

    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}
