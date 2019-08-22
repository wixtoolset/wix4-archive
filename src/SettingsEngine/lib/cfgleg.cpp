// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

volatile static DWORD s_dwRefCount = 0;
volatile static BOOL vfComInitialized = FALSE;
static BOOL s_fAdminAccess = FALSE;
static CFGDB_STRUCT s_cdb = { };

HRESULT CFGAPI CfgLegacyReadLatest(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    BOOL fLocked = FALSE;

    if (pcdb->fRemote)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle while reading latest");
    fLocked = TRUE;

    hr = LegacyPull(pcdb);
    ExitOnFailure(hr, "Failed to pull legacy settings");

    hr = BackgroundSyncRemotes(pcdb);
    ExitOnFailure(hr, "Failed to sync remotes");

LExit:
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }

    return hr;
}

HRESULT CfgLegacyImportProductFromXMLFile(
    __in_bcount(CFGDB_HANDLE_BYTES) CFGDB_HANDLE cdHandle,
    __in_z LPCWSTR wzXmlFilePath
    )
{
    HRESULT hr = S_OK;
    CFGDB_STRUCT *pcdb = static_cast<CFGDB_STRUCT *>(cdHandle);
    LPWSTR sczManifestValueName = NULL;
    LPWSTR sczContent = NULL;
    LEGACY_PRODUCT product = { };
    BOOL fInSceTransaction = FALSE;
    BOOL fLocked = FALSE;
    CONFIG_VALUE cvValue = { };

    hr = LogStringLine(REPORT_STANDARD, "Importing new legacy manifest from path %ls by explicit user request", wzXmlFilePath);
    ExitOnFailure(hr, "Failed to log line");

    hr = FileToString(wzXmlFilePath, &sczContent, NULL);
    ExitOnFailure(hr, "Failed to load string out of file contents from file at path: %ls", wzXmlFilePath);

    hr = ParseManifest(sczContent, &product);
    ExitOnFailure(hr, "Failed to parse XML manifest file from path: %ls", wzXmlFilePath);

    hr = HandleLock(pcdb);
    ExitOnFailure(hr, "Failed to lock handle while importing legacy manifest");
    fLocked = TRUE;

    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction");
    fInSceTransaction = TRUE;

    hr = ProductGetLegacyManifestValueName(product.sczProductId, &sczManifestValueName);
    ExitOnFailure(hr, "Failed to get legacy manifest value name");

    hr = ValueSetBlob(reinterpret_cast<BYTE *>(sczContent), lstrlenW(sczContent) * sizeof(WCHAR), FALSE, NULL, pcdb->sczGuid, &cvValue);
    ExitOnFailure(hr, "Failed to set manifest contents as string value in memory");

    hr = ValueWrite(pcdb, pcdb->dwCfgAppID, sczManifestValueName, &cvValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to write manifest contents to database");

    hr = ProductSet(pcdb, product.sczProductId, wzLegacyVersion, wzLegacyPublicKey, FALSE, NULL);
    ExitOnFailure(hr, "Failed to set legacy product to product ID: %ls", product.sczProductId);

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    fInSceTransaction = FALSE;

LExit:
    if (fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
    }
    if (fLocked)
    {
        HandleUnlock(pcdb);
    }
    ReleaseCfgValue(cvValue);
    ManifestFreeProductStruct(&product);
    ReleaseStr(sczManifestValueName);
    ReleaseStr(sczContent);

    return hr;
}

