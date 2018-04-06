// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT ProductDbToMachine(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
   );
static HRESULT DeleteEmptyRegistryKeyChildren(
    __in DWORD dwRoot,
    __in_z LPCWSTR wzSubKey
    );
static HRESULT DeleteEmptyRegistryKeys(
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    );
static HRESULT DeleteEmptyDirectoryChildren(
    __in_z LPCWSTR wzPath
    );
static HRESULT DeleteEmptyDirectory(
    __in LEGACY_FILE_TYPE fileType,
    __in_z LPCWSTR wzPath
    );
static HRESULT DeleteEmptyDirectories(
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    );
static HRESULT UpdateProductRegistrationState(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzName,
    __in_z LPCWSTR wzVersion,
    __in_z_opt LPCWSTR wzPublicKey
    );
static HRESULT ReadDirWriteLegacyDbHelp(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_FILE *pFile,
    __in_z LPCWSTR wzSubDir
    );
static HRESULT ReadDirWriteLegacyDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_FILE *pFile,
    __in_z LPCWSTR wzSubDir
    );
static HRESULT ReadFileWriteLegacyDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_FILE *pFile,
    __in_z LPCWSTR wzFilePath,
    __in BOOL fVirtualStoreCheck
    );
static HRESULT ReadRegKeyWriteLegacyDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_REGISTRY_KEY *pRegKey,
    __in_z LPCWSTR wzSubKey
    );
static HRESULT ReadRegValueWriteLegacyDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in HKEY hkKey,
    __in_z LPCWSTR wzRegKey,
    __in_z LPCWSTR wzValueName,
    __in DWORD dwType
    );

HRESULT LegacySyncInitializeSession(
    __in BOOL fWriteBackToMachine,
    __in BOOL fDetect,
    __out LEGACY_SYNC_SESSION *pSyncSession
    )
{
    HRESULT hr = S_OK;

    pSyncSession->fWriteBackToMachine = fWriteBackToMachine;
    pSyncSession->fDetect = fDetect;

    if (fDetect)
    {
        hr = DetectGetArpProducts(&pSyncSession->arpProducts);
        ExitOnFailure(hr, "Failed to detect products from ARP");

        hr = DetectGetExeProducts(&pSyncSession->exeProducts);
        ExitOnFailure(hr, "Failed to detect EXE products");
    }

LExit:
    return hr;
}

HRESULT LegacySyncSetProduct(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_SYNC_SESSION *pSyncSession,
    __in LPCWSTR wzName
    )
{
    HRESULT hr = S_OK;
    LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession = &pSyncSession->syncProductSession;
    LEGACY_FILE *pFile = NULL;
    LEGACY_FILE_SPECIAL *pFileSpecial = NULL;
    LEGACY_INI_FILE *pIniFile = NULL;
    CONFIG_VALUE cvManifestContents = { };
    CONFIG_VALUE cvManifestConvertedToBlob = { };
    SCE_ROW_HANDLE sceManifestValueRow = NULL;
    LPWSTR sczManifestValueName = NULL;
    BOOL fWasRegistered = FALSE;
    BYTE *pbManifestBuffer = NULL;
    SIZE_T iManifestBuffer = 0;
    LPWSTR sczBlobManifestAsString = NULL;

    if (pSyncSession->fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
        pSyncSession->fInSceTransaction = FALSE;
    }

    hr = ProductGetLegacyManifestValueName(wzName, &sczManifestValueName);
    ExitOnFailure(hr, "Failed to get legacy manifest value name");

    ManifestFreeProductStruct(&pSyncProductSession->product);
    ZeroMemory(&pSyncProductSession->product, sizeof(pSyncProductSession->product));

    for (DWORD i = 0; i < pSyncProductSession->cIniFiles; ++i)
    {
        IniFree(pSyncProductSession->rgIniFiles + i);
    }
    ReleaseNullMem(pSyncProductSession->rgIniFiles);
    pSyncProductSession->cIniFiles = 0;

    ReleaseNullDict(pSyncProductSession->shDictValuesSeen);
    ReleaseNullDict(pSyncProductSession->shIniFilesByNamespace);

    hr = DictCreateStringList(&pSyncProductSession->shDictValuesSeen, 0, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary of values seen");

    hr = DictCreateWithEmbeddedKey(&pSyncProductSession->shIniFilesByNamespace, 0, reinterpret_cast<void **>(&pSyncProductSession->rgIniFiles), offsetof(LEGACY_INI_FILE, sczNamespace), DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create ini file dictionary");

    hr = DictCreateWithEmbeddedKey(&pSyncProductSession->product.detect.shCachedDetectionPropertyValues, offsetof(LEGACY_CACHED_DETECTION_RESULT, sczPropertyName), reinterpret_cast<void **>(&pSyncProductSession->product.detect.rgCachedDetectionProperties), 0, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create cached detection property values dictionary");

    hr = ValueFindRow(pcdb, pcdb->dwCfgAppID, sczManifestValueName, &sceManifestValueRow);
    ExitOnFailure(hr, "Failed to find config value for legacy manifest (AppID: %u, Config Value named: %ls)", pcdb->dwCfgAppID, sczManifestValueName);

    hr = ValueRead(pcdb, sceManifestValueRow, &cvManifestContents);
    ExitOnFailure(hr, "Failed to read manifest contents");

    // TODO: someday remove this temporary conversion code when we feel confident nobody has old databases with old manifests laying around
    if (VALUE_STRING == cvManifestContents.cvType)
    {
        LogStringLine(REPORT_STANDARD, "Converting manifest value named %ls from string to blob value", sczManifestValueName);

        hr = ValueSetBlob(reinterpret_cast<BYTE *>(cvManifestContents.string.sczValue), lstrlenW(cvManifestContents.string.sczValue) * sizeof(WCHAR), FALSE, NULL, pcdb->sczGuid, &cvManifestConvertedToBlob);
        ExitOnFailure(hr, "Failed to set converted manifest value in memory");

        hr = ValueWrite(pcdb, pcdb->dwCfgAppID, sczManifestValueName, &cvManifestConvertedToBlob, TRUE, NULL);
        ExitOnFailure(hr, "Failed to set converted manifest blob: %ls", sczManifestValueName);

        ReleaseNullSceRow(sceManifestValueRow);
        ReleaseNullCfgValue(cvManifestContents);
        hr = ValueFindRow(pcdb, pcdb->dwCfgAppID, sczManifestValueName, &sceManifestValueRow);
        ExitOnFailure(hr, "Failed to find config value for legacy manifest after conversion (AppID: %u, Config Value named: %ls)", pcdb->dwCfgAppID, sczManifestValueName);

        hr = ValueRead(pcdb, sceManifestValueRow, &cvManifestContents);
        ExitOnFailure(hr, "Failed to read converted manifest contents");
    }

    if (VALUE_BLOB != cvManifestContents.cvType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Stored manifest value was not of type blob");
    }

    if (CFG_BLOB_DB_STREAM != cvManifestContents.blob.cbType)
    {
        hr = HRESULT_FROM_WIN32(ERROR_DATATYPE_MISMATCH);
        ExitOnFailure(hr, "Stored manifest blob was not a database stream");
    }

    hr = StreamRead(pcdb, cvManifestContents.blob.dbstream.dwContentID, NULL, &pbManifestBuffer, &iManifestBuffer);
    ExitOnFailure(hr, "Failed to get binary content of blob named: %ls, with content ID: %u", sczManifestValueName, pcdb->dwCfgAppID);

    hr = StrAllocString(&sczBlobManifestAsString, reinterpret_cast<LPWSTR>(pbManifestBuffer), iManifestBuffer / sizeof(WCHAR));
    ExitOnFailure(hr, "Failed to add null terminator to manifest blob");

    hr = ParseManifest(sczBlobManifestAsString, &pSyncProductSession->product);
    ExitOnFailure(hr, "Failed to parse manifest");

    hr = ProductSet(pcdb, wzName, wzLegacyVersion, wzLegacyPublicKey, FALSE, NULL);
    ExitOnFailure(hr, "Failed to set product");

    hr = ProductIsRegistered(pcdb, pcdb->sczProductName, wzLegacyVersion, wzLegacyPublicKey, &fWasRegistered);
    ExitOnFailure(hr, "Failed to check if product is registered");

    hr = DetectProduct(pcdb, !pSyncSession->fDetect, &pSyncSession->arpProducts, &pSyncSession->exeProducts, pSyncProductSession);
    ExitOnFailure(hr, "Failed to detect product with AppID: %u", pcdb->dwAppID);

    // Don't bother writing new registration state data to the database if detect is disabled
    if (pSyncSession->fDetect)
    {
        hr = UpdateProductRegistrationState(pcdb, pSyncProductSession, pcdb->sczProductName, wzLegacyVersion, wzLegacyPublicKey);
        ExitOnFailure(hr, "Failed to update product registration state");
    }

    hr = ProductIsRegistered(pcdb, pcdb->sczProductName, wzLegacyVersion, wzLegacyPublicKey, &pSyncProductSession->fRegistered);
    ExitOnFailure(hr, "Failed to check if product is registered");

    pSyncProductSession->fNewlyRegistered = (!fWasRegistered && pSyncProductSession->fRegistered);

    for (DWORD i = 0; i < pSyncProductSession->product.cFiles; ++i)
    {
        pFile = pSyncProductSession->product.rgFiles + i;

        hr = UtilExpandLegacyPath(pFile->sczLocation, &pSyncProductSession->product.detect, &pFile->sczExpandedPath);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }

        if (NULL != pFile->sczExpandedPath)
        {
            for (DWORD j = 0; j < pFile->cFileSpecials; ++j)
            {
                pFileSpecial = pFile->rgFileSpecials + j;

                if (0 < pFileSpecial->cIniInfo)
                {
                    hr = MemEnsureArraySize(reinterpret_cast<void **>(&pSyncProductSession->rgIniFiles), pSyncProductSession->cIniFiles + 1, sizeof(LEGACY_INI_FILE), 5);
                    ExitOnFailure(hr, "Failed to grow active IniFiles array");

                    pIniFile = pSyncProductSession->rgIniFiles + pSyncProductSession->cIniFiles;

                    hr = IniFileOpen(pFile, pFileSpecial, pFileSpecial->rgIniInfo, pIniFile);
                    ExitOnFailure(hr, "Failed to parse INI file");

                    hr = DictAddValue(pSyncProductSession->shIniFilesByNamespace, pIniFile);
                    ExitOnFailure(hr, "Failed to add INI file to dict for namespace: %ls", pIniFile->sczNamespace);

                    ++pSyncProductSession->cIniFiles;
                }
            }
        }
    }

    // IMPORTANT: Put all legacy database actions into a separate transaction
    // for each product. In the case of any kind of failure, it's OK to leave
    // changes written to local machine registry / file system, but not the
    // legacy database - they'll just appear as local machine changes on next
    // sync, and everything will be happy. The other way around is NOT happy,
    // because every sync would create another history entry, ad infinitum!
    hr = SceBeginTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to begin transaction");
    pSyncSession->fInSceTransaction = TRUE;

LExit:
    ReleaseSceRow(sceManifestValueRow);
    ReleaseCfgValue(cvManifestContents);
    ReleaseCfgValue(cvManifestConvertedToBlob);
    ReleaseStr(sczManifestValueName);
    ReleaseMem(pbManifestBuffer);
    ReleaseStr(sczBlobManifestAsString);

    return hr;
}

HRESULT LegacyProductMachineToDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    )
{
    HRESULT hr = S_OK;
    LEGACY_REGISTRY_KEY *pRegKey = NULL;
    LEGACY_FILE *pFile = NULL;
    DWORD dwExpandedPathLen = 0;

    for (DWORD i = 0; i < pSyncProductSession->product.cRegKeys; ++i)
    {
        pRegKey = pSyncProductSession->product.rgRegKeys + i;
        hr = ReadRegKeyWriteLegacyDb(pcdb, pSyncProductSession, pRegKey, pRegKey->sczKey);
        ExitOnFailure(hr, "Failed to write data to settings engine from registry key: %u, %ls", pRegKey->dwRoot, pRegKey->sczKey);
    }

    for (DWORD i = 0; i < pSyncProductSession->product.cFiles; ++i)
    {
        pFile = pSyncProductSession->product.rgFiles + i;
        if (NULL != pFile->sczExpandedPath)
        {
            ExitOnFailure(hr, "Failed to expand legacy directory paths for directory: %ls", pFile->sczLocation);

            dwExpandedPathLen = lstrlenW(pFile->sczExpandedPath);

            if (0 == dwExpandedPathLen)
            {
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Empty expanded path encountered while processing files for appID: %u", pcdb->dwAppID);
            }
            else if (LEGACY_FILE_DIRECTORY == pFile->legacyFileType)
            {
                hr = ReadDirWriteLegacyDb(pcdb, pSyncProductSession, pFile, pFile->sczExpandedPath);
                if (E_PATHNOTFOUND == hr)
                {
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to write files to settings engine from directory: %ls", pFile->sczExpandedPath);
            }
            else if (LEGACY_FILE_PLAIN == pFile->legacyFileType)
            {
                hr = ReadFileWriteLegacyDb(pcdb, pSyncProductSession, pFile, pFile->sczExpandedPath, TRUE);
                if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
                {
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to write individual file to settings engine from path: %ls", pFile->sczExpandedPath);
            }
        }
    }

    if (pSyncProductSession->fRegistered && !pSyncProductSession->fNewlyRegistered)
    {
        hr = LegacySyncPullDeletedValues(pcdb, pSyncProductSession);
        ExitOnFailure(hr, "Failed to check for deleted values");
    }

LExit:
    return hr;
}

HRESULT LegacySyncFinalizeProduct(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_SESSION *pSyncSession
    )
{
    HRESULT hr = S_OK;

    Assert(pSyncSession->fInSceTransaction);

    // If the product isn't registered, don't write anything back to disk for it
    if (pSyncSession->syncProductSession.fRegistered && pSyncSession->fWriteBackToMachine)
    {
        hr = ProductDbToMachine(pcdb, &pSyncSession->syncProductSession);
        ExitOnFailure(hr, "Failed to write individual product's legacy state from db to machine");
    }

    hr = SceCommitTransaction(pcdb->psceDb);
    ExitOnFailure(hr, "Failed to commit transaction");
    pSyncSession->fInSceTransaction = FALSE;

LExit:
    if (pSyncSession->fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
        pSyncSession->fInSceTransaction = FALSE;
    }

    return hr;
}

void LegacySyncUninitializeSession(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_SYNC_SESSION *pSyncSession
    )
{
    LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession = &pSyncSession->syncProductSession;

    if (pSyncSession->fInSceTransaction)
    {
        SceRollbackTransaction(pcdb->psceDb);
        pSyncSession->fInSceTransaction = FALSE;
    }

    for (DWORD i = 0; i < pSyncProductSession->cIniFiles; ++i)
    {
        IniFree(pSyncProductSession->rgIniFiles + i);
    }
    ReleaseNullMem(pSyncProductSession->rgIniFiles);

    ReleaseDict(pSyncProductSession->shDictValuesSeen);
    ReleaseDict(pSyncProductSession->shIniFilesByNamespace);

    DetectFreeArpProducts(&pSyncSession->arpProducts);
    DetectFreeExeProducts(&pSyncSession->exeProducts);

    ManifestFreeProductStruct(&pSyncProductSession->product);
    ZeroMemory(&pSyncProductSession->product, sizeof(pSyncProductSession->product));
}

HRESULT LegacyPull(
    __in CFGDB_STRUCT *pcdb
    )
{
    HRESULT hr = S_OK;
    LEGACY_SYNC_SESSION syncSession = { };
    SCE_ROW_HANDLE sceRow = NULL;

    hr = LegacySyncInitializeSession(FALSE, TRUE, &syncSession);
    ExitOnFailure(hr, "Failed to initialize legacy sync session");

    hr = SceGetFirstRow(pcdb->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to jump to beginning of table");
    }

    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to move to next record in PRODUCT_INDEX_TABLE table");

        hr = LegacyPullProduct(pcdb, &syncSession, sceRow);
        ExitOnFailure(hr, "Failed to pull individual product");

        ReleaseNullSceRow(sceRow);
        hr = SceGetNextRow(pcdb->psceDb, PRODUCT_INDEX_TABLE, &sceRow);
    }

    hr = S_OK;

LExit:
    LegacySyncUninitializeSession(pcdb, &syncSession);
    ReleaseSceRow(sceRow);

    return hr;
}

HRESULT LegacyPullProduct(
    __in CFGDB_STRUCT *pcdb,
    __inout LEGACY_SYNC_SESSION * pSyncSession,
    __in SCE_ROW_HANDLE sceProductRow
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczName = NULL;
    BOOL fLegacy = FALSE;

    hr = SceGetColumnBool(sceProductRow, PRODUCT_IS_LEGACY, &fLegacy);
    ExitOnFailure(hr, "Failed to get IsLegacy flag from product row");

    if (!fLegacy)
    {
        // Nothing to do for non-legacy products
        ExitFunction1(hr = S_OK);
    }

    hr = SceGetColumnString(sceProductRow, PRODUCT_NAME, &sczName);
    ExitOnFailure(hr, "Failed to get legacy product name");

    hr = LegacySyncSetProduct(pcdb, pSyncSession, sczName);
    ExitOnFailure(hr, "Failed to set product in legacy sync session");

    hr = LegacyProductMachineToDb(pcdb, &pSyncSession->syncProductSession);
    ExitOnFailure(hr, "Failed to read data from local machine and write into settings database for app");

    if (pSyncSession->syncProductSession.fRegistered && !pSyncSession->syncProductSession.fNewlyRegistered)
    {
        hr = LegacySyncPullDeletedValues(pcdb, &pSyncSession->syncProductSession);
        ExitOnFailure(hr, "Failed to check for deleted registry values");
    }

    hr = LegacySyncFinalizeProduct(pcdb, pSyncSession);
    ExitOnFailure(hr, "Failed to finalize product in legacy sync session");

LExit:
    ReleaseStr(sczName);

    return hr;
}

HRESULT LegacySyncPullDeletedValues(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_QUERY_RESULTS_HANDLE sqrhResults = NULL;
    SCE_ROW_HANDLE sceRow = NULL;
    CONFIG_VALUE cvExistingValue = { };
    CONFIG_VALUE cvNewValue = { };
    LPWSTR sczName = NULL;

    if (pcdb->dwAppID == pcdb->dwCfgAppID)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Error - tried to pull deleted values for dwCfgAppID!");
    }

    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into VALUE_INDEX_TABLE table");

    hr = SceSetQueryColumnDword(sqhHandle, pcdb->dwAppID);
    ExitOnFailure(hr, "Failed to set AppID for query");

    hr = SceRunQueryRange(&sqhHandle, &sqrhResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query into VALUE_INDEX_TABLE table for AppID: %u", pcdb->dwAppID);

    hr = SceGetNextResultRow(sqrhResults, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next result row from VALUE_INDEX_TABLE table");

        hr = SceGetColumnString(sceRow, VALUE_COMMON_NAME, &sczName);
        ExitOnFailure(hr, "Failed to get name from row while querying VALUE_INDEX_TABLE table");

        hr = DictKeyExists(pSyncProductSession->shDictValuesSeen, sczName);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;

            ReleaseNullCfgValue(cvExistingValue);
            hr = ValueRead(pcdb, sceRow, &cvExistingValue);
            ExitOnFailure(hr, "Failed to read value into memory while querying VALUE_INDEX_TABLE table");

            if (VALUE_DELETED != cvExistingValue.cvType)
            {
                hr = ValueSetDelete(NULL, pcdb->sczGuid, &cvNewValue);
                ExitOnFailure(hr, "Failed to set deleted value in memory");

                hr = ValueWrite(pcdb, pcdb->dwAppID, sczName, &cvNewValue, TRUE, NULL);
                ExitOnFailure(hr, "Failed to write deleted value to db: %ls", sczName);
            }
        }
        ExitOnFailure(hr, "Failed to check if registry value exists in reg values seen database");

        ReleaseNullSceRow(sceRow);
        hr = SceGetNextResultRow(sqrhResults, &sceRow);
    }

    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseSceRow(sceRow);
    ReleaseSceQuery(sqhHandle);
    ReleaseSceQueryResults(sqrhResults);
    ReleaseStr(sczName);
    ReleaseCfgValue(cvExistingValue);
    ReleaseCfgValue(cvNewValue);

    return hr;
}

static HRESULT ProductDbToMachine(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
   )
{
    HRESULT hr = S_OK;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_QUERY_RESULTS_HANDLE sqrhResults = NULL;
    SCE_ROW_HANDLE sceRow = NULL;
    BOOL fIgnore = FALSE;
    LPWSTR sczName = NULL;
    BOOL fHandled = FALSE;
    CONFIG_VALUE cvValue = { };

    // First handle all special values
    hr = RegSpecialsProductWrite(pcdb, pSyncProductSession);
    ExitOnFailure(hr, "Failed to write specially handled values back to registry");

    // Now handle all the regular values, minus exceptions from when we processed special values
    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into VALUE_INDEX_TABLE table");

    hr = SceSetQueryColumnDword(sqhHandle, pcdb->dwAppID);
    ExitOnFailure(hr, "Failed to set AppID for query");

    hr = SceRunQueryRange(&sqhHandle, &sqrhResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query into VALUE_INDEX_TABLE table for AppID: %u", pcdb->dwAppID);

    hr = SceGetNextResultRow(sqrhResults, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next results row from VALUE_INDEX_TABLE table");

        hr = SceGetColumnString(sceRow, VALUE_COMMON_NAME, &sczName);
        ExitOnFailure(hr, "Failed to get name from row while querying VALUE_INDEX_TABLE table");

        hr = FilterCheckValue(&pSyncProductSession->product, sczName, &fIgnore, NULL);
        ExitOnFailure(hr, "Failed to check if cfg setting should be ignored: %ls", sczName);

        if (!fIgnore)
        {
            fHandled = FALSE;

            hr = DictKeyExists(pSyncProductSession->product.shRegistrySpeciallyHandled, sczName);
            if (S_OK == hr)
            {
                // On a registry value exception, simply skip handling this value
                fHandled = TRUE;
            }
            else if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to check if registry value exists in reg value exceptions dictionary");

            ReleaseNullCfgValue(cvValue);
            hr = ValueRead(pcdb, sceRow, &cvValue);
            ExitOnFailure(hr, "Failed to read value %ls", sczName);

            if (!fHandled)
            {
                hr = RegDefaultWriteValue(&pSyncProductSession->product, sczName, &cvValue, &fHandled);
                ExitOnFailure(hr, "Failed to write value through registry default handler: %ls", sczName);
            }

            if (!fHandled)
            {
                hr = IniFileSetValue(pSyncProductSession, sczName, &cvValue, &fHandled);
                ExitOnFailure(hr, "Failed to write registry value through ini handler: %ls", sczName);
            }

            if (!fHandled)
            {
                hr = DirDefaultWriteFile(&pSyncProductSession->product, sczName, &cvValue, &fHandled);
                ExitOnFailure(hr, "Failed to write file through default handler: %ls", sczName);
            }
        }

        ReleaseNullSceRow(sceRow);
        hr = SceGetNextResultRow(sqrhResults, &sceRow);
    }
    hr = S_OK;
    ReleaseNullSceQueryResults(sqrhResults);

    for (DWORD i = 0; i < pSyncProductSession->cIniFiles; ++i)
    {
        hr = IniFileWrite(pSyncProductSession->rgIniFiles + i);
        ExitOnFailure(hr, "Failed to write INI file");
    }

    if (!pSyncProductSession->fRegistered)
    {
        hr = DeleteEmptyRegistryKeys(pSyncProductSession);
        ExitOnFailure(hr, "Failed to delete empty registry keys");

        hr = DeleteEmptyDirectories(pSyncProductSession);
        ExitOnFailure(hr, "Failed to delete empty directories");
    }

LExit:
    ReleaseStr(sczName);
    ReleaseSceQuery(sqhHandle);
    ReleaseSceQueryResults(sqrhResults);
    ReleaseSceRow(sceRow);
    ReleaseCfgValue(cvValue);

    return hr;
}

static HRESULT DeleteEmptyRegistryKeyChildren(
    __in DWORD dwRoot,
    __in_z LPCWSTR wzSubKey
    )
{
    HRESULT hr = S_OK;
    HKEY hkKey = NULL;
    DWORD dwIndex = 0;
    BOOL fNoValues = FALSE;
    LPWSTR sczValueName = NULL;
    LPWSTR sczSubkeyName = NULL;
    LPWSTR sczSubkeyPath = NULL;
    DWORD dwSubKeyPathLen = 0;

    hr = RegOpen(ManifestConvertToRootKey(dwRoot), wzSubKey, KEY_READ, &hkKey);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to open regkey: %ls", wzSubKey);

    if (E_NOMOREITEMS == RegValueEnum(hkKey, dwIndex, &sczValueName, NULL))
    {
        fNoValues = TRUE;
    }

    // Recurse and handle subkeys as well
    dwIndex = 0;
    while (E_NOMOREITEMS != (hr = RegKeyEnum(hkKey, dwIndex, &sczSubkeyName)))
    {
        ExitOnFailure(hr, "Failed to enumerate key %u", dwIndex);

        hr = StrAllocString(&sczSubkeyPath, wzSubKey, 0);
        ExitOnFailure(hr, "Failed to allocate copy of subkey name");

        dwSubKeyPathLen = lstrlenW(sczSubkeyPath);

        if (0 == dwSubKeyPathLen)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Encountered empty keyname while enumerating subkeys under key: %ls", wzSubKey);
        }
        else if (L'\\' != sczSubkeyPath[dwSubKeyPathLen - 1])
        {
            hr = StrAllocConcat(&sczSubkeyPath, L"\\", 1);
            ExitOnFailure(hr, "Failed to concatenate backslash to copy of regkey name");
        }

        hr = StrAllocConcat(&sczSubkeyPath, sczSubkeyName, 0);
        ExitOnFailure(hr, "Failed to concatenate subkey name to subkey path");

        hr = DeleteEmptyRegistryKeyChildren(dwRoot, sczSubkeyPath);
        // Increment and ignore the error if we didn't delete the subkey
        if (HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY) == hr)
        {
            ++dwIndex;
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to read regkey and write settings for root: %u, subkey: %ls", dwRoot, sczSubkeyPath);
    }

    // If there are no keys and no values under it, delete it
    if (fNoValues && 0 == dwIndex)
    {
        hr = RegDelete(ManifestConvertToRootKey(dwRoot), wzSubKey, REG_KEY_DEFAULT, FALSE);
        ExitOnFailure(hr, "Failed to delete registry key at root: %u, subkey: %ls", dwRoot, wzSubKey);

        ExitFunction1(hr = S_OK);
    }
    else
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY));
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczValueName);
    ReleaseStr(sczSubkeyName);
    ReleaseStr(sczSubkeyPath);

    return hr;
}

static HRESULT DeleteEmptyRegistryKeys(
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    )
{
    HRESULT hr = S_OK;
    const LEGACY_REGISTRY_KEY *rgRegKeys = pSyncProductSession->product.rgRegKeys;
    const DWORD cRegKeys = pSyncProductSession->product.cRegKeys;
    DWORD dwIndex = 0;
    LPWSTR pwcLastBackslash = NULL;
    LPWSTR sczParentKey = NULL;

    for (DWORD i = 0; i < cRegKeys; ++i)
    {
        hr = DeleteEmptyRegistryKeyChildren(rgRegKeys[i].dwRoot, rgRegKeys[i].sczKey);
        // This code is just an FYI that the key was not empty and so wasn't deleted. It's not an error, so ignore it.
        if (HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY) == hr)
        {
            hr = S_OK;
            continue;
        }
        ExitOnFailure(hr, "Failed to check for empty keys and delete them at root: %u, subkey: %ls", rgRegKeys[i].dwRoot, rgRegKeys[i].sczKey);

        hr = StrAllocString(&sczParentKey, rgRegKeys[i].sczKey, 0);
        ExitOnFailure(hr, "Failed to allocate copy of subkey");

        // Eliminate any trailing backslashes from the key first, if there are any
        dwIndex = lstrlenW(sczParentKey);
        if (0 == dwIndex)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unexpected empty parent key encountered while deleting empty registry keys");
        }

        --dwIndex; // Start at the last character of the string
        while (dwIndex > 0 && sczParentKey[dwIndex] == L'\\')
        {
            sczParentKey[dwIndex] = L'\0';
            --dwIndex;
        }

        if (0 == dwIndex)
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Parent key was entirely composed of backslashes!");
        }

        // Now delete any empty parent keys we see as well
        while (NULL != (pwcLastBackslash = wcsrchr(sczParentKey, L'\\')))
        {
            hr = RegDelete(ManifestConvertToRootKey(rgRegKeys[i].dwRoot), sczParentKey, REG_KEY_DEFAULT, FALSE);
            // This code is just an FYI that the key was not empty and so wasn't deleted. It's not an error, so ignore it.
            if (FAILED(hr))
            {
                LogErrorString(hr, "Failed to check for empty parent keys and delete them at root: %u, subkey: %ls", rgRegKeys[i].dwRoot, sczParentKey);
                hr = S_OK;
                break;
            }

            *pwcLastBackslash = L'\0';
        }
    }

LExit:
    ReleaseStr(sczParentKey);

    return hr;
}

static HRESULT DeleteEmptyDirectoryChildren(
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    WIN32_FIND_DATAW wfd = { };
    HANDLE hFind = NULL;
    LPWSTR sczSubDirWithWildcard = NULL;
    LPWSTR sczPath = NULL;

    hr = PathConcat(wzPath, L"*", &sczSubDirWithWildcard);
    ExitOnFailure(hr, "Failed to concatenate wildcard character to directory name for search");

    hFind = ::FindFirstFileW(sczSubDirWithWildcard, &wfd);
    if (INVALID_HANDLE_VALUE == hFind)
    {
        er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);
        if (E_PATHNOTFOUND == hr)
        {
            ExitFunction();
        }
        ExitWithLastError(hr, "Failed to find first file with query: %ls", sczSubDirWithWildcard);
    }

    do
    {
        // Safety / silence code analysis tools
        wfd.cFileName[MAX_PATH - 1] = L'\0';

        // Don't use "." or ".."
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wfd.cFileName, -1, L".", -1))
        {
            continue;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wfd.cFileName, -1, L"..", -1))
        {
            continue;
        }

        hr = PathConcat(wzPath, wfd.cFileName, &sczPath);
        ExitOnFailure(hr, "Failed to concat filename '%ls' to directory: %ls", wfd.cFileName, wzPath);

        // If we found a directory, recurse!
        if (wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
        {
            hr = PathBackslashTerminate(&sczPath);
            ExitOnFailure(hr, "Failed to ensure path is backslash terminated: %ls", sczPath);

            hr = DeleteEmptyDirectoryChildren(sczPath);
            if (HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY) == hr)
            {
                hr = S_OK;
            }
            else
            {
                ExitOnFailure(hr, "Failed to recurse to directory: %ls", sczPath);

                hr = DirEnsureDelete(sczPath, FALSE, FALSE);
                if (HRESULT_FROM_WIN32(ERROR_DIR_NOT_EMPTY) == hr)
                {
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to delete directory: %ls", sczPath);
            }
        }
        else
        {
            continue;
        }
    }
    while (::FindNextFileW(hFind, &wfd));

    er = ::GetLastError();
    if (ERROR_NO_MORE_FILES == er)
    {
        hr = S_OK;
    }
    else
    {
        ExitWithLastError(hr, "Failed while looping through files in directory: %ls", wzPath);
    }

LExit:
    // There was nothing to read, it's still success
    if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
    {
        hr = S_OK;
    }
    if (NULL != hFind)
    {
        FindClose(hFind);
    }
    ReleaseStr(sczSubDirWithWildcard);
    ReleaseStr(sczPath);

    return hr;
}

static HRESULT DeleteEmptyDirectory(
    __in LEGACY_FILE_TYPE fileType,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;

    LPWSTR sczParentDirectory = NULL;
    DWORD dwIndex = 0;
    LPWSTR pwcLastBackslash = NULL;

    // If it's an individual file and it exists, no point trying to delete any directories for it
    if (LEGACY_FILE_PLAIN == fileType)
    {
        if (FileExistsEx(wzPath, NULL))
        {
            ExitFunction1(hr = S_OK);
        }
    }
    else
    {
        // It's a directory, so delete children first
        hr = DeleteEmptyDirectoryChildren(wzPath);
        // This code is just an FYI that the directory was not empty and so wasn't deleted. It's not an error, so ignore it.
        if (FAILED(hr))
        {
            ExitFunction1(hr = S_OK);
        }
        ExitOnFailure(hr, "Failed to check for empty directories and delete them at path: %ls", wzPath);
    }

    hr = StrAllocString(&sczParentDirectory, wzPath, 0);
    ExitOnFailure(hr, "Failed to allocate copy of directory");

    // Eliminate any trailing backslashes from the directory first, if there are any
    dwIndex = lstrlenW(sczParentDirectory);
    if (0 == dwIndex)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Unexpected empty parent directory encountered while deleting empty directories");
    }

    --dwIndex; // Start at the last character of the string
    while (dwIndex > 0 && sczParentDirectory[dwIndex] == L'\\')
    {
        sczParentDirectory[dwIndex] = L'\0';
        --dwIndex;
    }

    if (0 == dwIndex)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Parent directory was entirely composed of backslashes!");
    }

    // Now delete any empty parent directories we see as well
    while (NULL != (pwcLastBackslash = wcsrchr(sczParentDirectory, L'\\')))
    {
        hr = DirEnsureDelete(sczParentDirectory, FALSE, FALSE);
        if (FAILED(hr))
        {
            LogErrorString(hr, "Failed to check for empty parent directories and delete them at directory: %ls", sczParentDirectory);
            hr = S_OK;
            break;
        }

        *pwcLastBackslash = L'\0';
    }

LExit:
    ReleaseStr(sczParentDirectory);

    return hr;
}

static HRESULT DeleteEmptyDirectories(
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession
    )
{
    HRESULT hr = S_OK;
    const LEGACY_FILE *rgFiles = pSyncProductSession->product.rgFiles;
    const DWORD cFiles = pSyncProductSession->product.cFiles;
    LPWSTR sczVirtualStorePath = NULL;

    for (DWORD i = 0; i < cFiles; ++i)
    {
        if (NULL == rgFiles[i].sczExpandedPath)
        {
            continue;
        }

        hr = DeleteEmptyDirectory(rgFiles[i].legacyFileType, rgFiles[i].sczExpandedPath);
        ExitOnFailure(hr, "Failed to scan for and delete empty directories for %ls", rgFiles[i].sczExpandedPath);

        // Delete the virtual store file as well
        hr = UtilConvertToVirtualStorePath(rgFiles[i].sczExpandedPath, &sczVirtualStorePath);
        ExitOnFailure(hr, "Failed to convert to virtualstore path: %ls", rgFiles[i].sczExpandedPath);

        hr = DeleteEmptyDirectory(rgFiles[i].legacyFileType, sczVirtualStorePath);
        ExitOnFailure(hr, "Falied to delete scan for and delete empty virtual store directories for %ls", sczVirtualStorePath);
    }

LExit:
    ReleaseStr(sczVirtualStorePath);

    return hr;
}

static HRESULT UpdateProductRegistrationState(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzName,
    __in_z LPCWSTR wzVersion,
    __in_z_opt LPCWSTR wzPublicKey
    )
{
    DISPLAY_NAME *rgDisplayNames = NULL;
    DWORD cDisplayNames = 0;
    BOOL fRegistered = FALSE;

    UNREFERENCED_PARAMETER(wzPublicKey);

    HRESULT hr = S_OK;

    // If we have one or more detects, use them to determine whether the product is registered or not
    if (0 == pSyncProductSession->product.detect.cDetects)
    {
        // Products that can't be detected always appear to be installed
        fRegistered = TRUE;
    }
    else if (0 < pSyncProductSession->product.detect.cDetects)
    {
        fRegistered = FALSE;
        for (DWORD i = 0; i < pSyncProductSession->product.detect.cDetects; ++i)
        {
            if (pSyncProductSession->product.detect.rgDetects[i].fFound)
            {
                fRegistered = TRUE;
                break;
            }
        }
    }

    hr = DisplayNameEnumerate(pcdb, pcdb->dwAppID, &rgDisplayNames, &cDisplayNames);
    ExitOnFailure(hr, "Failed to enumerate display names");

    // For performance, if they both have just one displayname and the LCIDs match, don't bother deleting and rewriting them all
    if (1 == pSyncProductSession->product.cDisplayNames && 1 == cDisplayNames && rgDisplayNames[0].dwLCID == pSyncProductSession->product.rgDisplayNames[0].dwLCID)
    {
        if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, rgDisplayNames[0].sczName, -1, pSyncProductSession->product.rgDisplayNames[0].sczName, -1))
        {
            hr = DisplayNamePersist(pcdb, pcdb->dwAppID, pSyncProductSession->product.rgDisplayNames[0].dwLCID, pSyncProductSession->product.rgDisplayNames[0].sczName);
            ExitOnFailure(hr, "Failed to persist display name %ls", pSyncProductSession->product.rgDisplayNames[0].sczName);
        }
    }
    else
    {
        // Otherwise delete them all, then write them all individually
        hr = DisplayNameRemoveAllForAppID(pcdb, pcdb->dwAppID);
        ExitOnFailure(hr, "Failed to remove all display names from AppID");

        for (DWORD i = 0; i < pSyncProductSession->product.cDisplayNames; ++i)
        {
            hr = DisplayNamePersist(pcdb, pcdb->dwAppID, pSyncProductSession->product.rgDisplayNames[i].dwLCID, pSyncProductSession->product.rgDisplayNames[i].sczName);
            ExitOnFailure(hr, "Failed to persist display name %ls at index %u", pSyncProductSession->product.rgDisplayNames[i].sczName, i);
        }
    }

    hr = ProductRegister(pcdb, wzName, wzVersion, wzLegacyPublicKey, fRegistered);
    ExitOnFailure(hr, "Failed to update product registration state for product: '%ls', '%ls', '%ls'", wzName, wzVersion, wzLegacyPublicKey);

LExit:
    ReleaseDisplayNameArray(rgDisplayNames, cDisplayNames);

    return hr;
}

static HRESULT ReadDirWriteLegacyDbHelp(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_FILE *pFile,
    __in_z LPCWSTR wzSubDir
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    WIN32_FIND_DATAW wfd = { };
    HANDLE hFind = NULL;
    LPWSTR sczSubDirWithWildcard = NULL;
    LPWSTR sczFileName = NULL;

    hr = PathConcat(wzSubDir, L"*", &sczSubDirWithWildcard);
    ExitOnFailure(hr, "Failed to concatenate wildcard character to directory name for search");

    hFind = ::FindFirstFileW(sczSubDirWithWildcard, &wfd);
    if (INVALID_HANDLE_VALUE == hFind)
    {
        er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);
        if (E_PATHNOTFOUND == hr)
        {
            ExitFunction();
        }
        ExitWithLastError(hr, "Failed to find first file with query: %ls", sczSubDirWithWildcard);
    }

    do
    {
        // Safety / silence code analysis tools
        wfd.cFileName[MAX_PATH - 1] = L'\0';

        // Don't use "." or ".."
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wfd.cFileName, -1, L".", -1))
        {
            continue;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, wfd.cFileName, -1, L"..", -1))
        {
            continue;
        }

        hr = PathConcat(wzSubDir, wfd.cFileName, &sczFileName);
        ExitOnFailure(hr, "Failed to concat filename '%ls' to directory: %ls", wfd.cFileName, wzSubDir);

        // If we found a directory, recurse!
        if (wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
        {
            hr = PathBackslashTerminate(&sczFileName);
            ExitOnFailure(hr, "Failed to ensure path is backslash terminated: %ls", sczFileName);

            hr = ReadDirWriteLegacyDb(pcdb, pSyncProductSession, pFile, sczFileName);
            ExitOnFailure(hr, "Failed to recurse to directory: %ls", sczFileName);
        }
        else
        {
            hr = ReadFileWriteLegacyDb(pcdb, pSyncProductSession, pFile, sczFileName, FALSE);
            ExitOnFailure(hr, "Failed while processing file: %ls", sczFileName);
        }
    }
    while (::FindNextFileW(hFind, &wfd));

    er = ::GetLastError();
    if (ERROR_NO_MORE_FILES == er)
    {
        hr = S_OK;
    }
    else
    {
        ExitWithLastError(hr, "Failed while looping through files in directory: %ls", wzSubDir);
    }

LExit:
    // There was nothing to read, it's still success
    if (HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
    {
        hr = S_OK;
    }
    if (INVALID_HANDLE_VALUE != hFind)
    {
        FindClose(hFind);
    }
    ReleaseStr(sczSubDirWithWildcard);
    ReleaseStr(sczFileName);

    return hr;
}

static HRESULT ReadRegValueWriteLegacyDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in const LEGACY_REGISTRY_KEY *pRegKey,
    __in HKEY hkKey,
    __in_z LPCWSTR wzRegKey,
    __in_z LPCWSTR wzValueName,
    __in DWORD dwType
    )
{
    HRESULT hr = S_OK;
    BOOL fContinueProcessing = FALSE;

    if (lstrlenW(pRegKey->sczKey) > lstrlenW(wzRegKey))
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Legacy registry key %ls was longer than the registry key sent to ReadRegValueWriteLegacyDb, %ls", pRegKey->sczKey, wzRegKey);
    }

    // Make the key reflect only the sub-portion under the original base key path
    wzRegKey += lstrlenW(pRegKey->sczKey);
    while (L'\\' == wzRegKey[0])
    {
        ++wzRegKey;
    }
    
    // Pass the value up to check for special handling first
    hr = RegSpecialValueRead(pcdb, pSyncProductSession, pRegKey, hkKey, wzRegKey, wzValueName, dwType, &fContinueProcessing);
    ExitOnFailure(hr, "Failed to appropriately check for and handle special registry value, subkey: %ls, value: %ls", wzRegKey, wzValueName);

    // If special handling tells us to avoid normal processing for this value, skip it
    if (!fContinueProcessing)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = RegDefaultReadValue(pcdb, pSyncProductSession, pRegKey->sczNamespace, hkKey, wzRegKey, wzValueName, dwType);
    ExitOnFailure(hr, "Failed to read registry with default handler valuetype: %u, key: %ls, named: %ls", dwType, wzRegKey, wzValueName);

LExit:
    return hr;
}

static HRESULT ReadRegKeyWriteLegacyDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_REGISTRY_KEY *pRegKey,
    __in_z LPCWSTR wzSubKey
    )
{
    HRESULT hr = S_OK;
    HKEY hkKey = NULL;
    DWORD dwType = 0;
    DWORD dwIndex = 0;
    LPWSTR sczValueName = NULL;
    LPWSTR sczSubkeyName = NULL;
    LPWSTR sczSubkeyPath = NULL;

    hr = RegOpen(ManifestConvertToRootKey(pRegKey->dwRoot), wzSubKey, KEY_READ, &hkKey);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to open regkey: %ls", wzSubKey);

    dwIndex = 0;
    while (E_NOMOREITEMS != (hr = RegValueEnum(hkKey, dwIndex, &sczValueName, &dwType)))
    {
        ExitOnFailure(hr, "Failed to enumerate value %u", dwIndex);

        hr = ReadRegValueWriteLegacyDb(pcdb, pSyncProductSession, pRegKey, hkKey, wzSubKey, sczValueName, dwType);
        ExitOnFailure(hr, "Failed to write registry value setting: %ls", sczValueName);

        ++dwIndex;
    }

    // Recurse and handle subkeys as well
    dwIndex = 0;
    while (E_NOMOREITEMS != (hr = RegKeyEnum(hkKey, dwIndex, &sczSubkeyName)))
    {
        ExitOnFailure(hr, "Failed to enumerate key %u", dwIndex);

        hr = StrAllocString(&sczSubkeyPath, wzSubKey, 0);
        ExitOnFailure(hr, "Failed to allocate copy of subkey name");

        hr = PathBackslashTerminate(&sczSubkeyPath);
        ExitOnFailure(hr, "Failed to ensure path is terminated with a backslash");

        hr = StrAllocConcat(&sczSubkeyPath, sczSubkeyName, 0);
        ExitOnFailure(hr, "Failed to concatenate subkey name to subkey path");

        hr = ReadRegKeyWriteLegacyDb(pcdb, pSyncProductSession, pRegKey, sczSubkeyPath);
        ExitOnFailure(hr, "Failed to read regkey and write settings for root: %u, subkey: %ls", pRegKey->dwRoot, sczSubkeyPath);

        ++dwIndex;
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczValueName);
    ReleaseStr(sczSubkeyName);
    ReleaseStr(sczSubkeyPath);

    return hr;
}

static HRESULT ReadFileWriteLegacyDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_FILE *pFile,
    __in_z LPCWSTR wzFilePath,
    __in BOOL fVirtualStoreCheck
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzSubPath = wzFilePath;
    LPWSTR sczVirtualStorePath = NULL;
    BOOL fContinueProcessing = FALSE;
    BOOL fWritePermission = TRUE;

    if (lstrlenW(pFile->sczExpandedPath) > lstrlenW(wzFilePath))
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Legacy file path %ls was longer than the file path sent to ReadFileWriteLegacyDb, %ls", pFile->sczExpandedPath, wzFilePath);
    }

    // Make the key reflect only the sub-portion under the original base key path
    wzSubPath += lstrlenW(pFile->sczExpandedPath);
    while (L'\\' == wzSubPath[0])
    {
        ++wzSubPath;
    }

    if (fVirtualStoreCheck)
    {
        hr = UtilTestWriteAccess(pcdb->hToken, wzFilePath);
        if (E_ACCESSDENIED == hr)
        {
            fWritePermission = FALSE;
            hr = S_OK;
        }
        else if (E_PATHNOTFOUND == hr)
        {
            ExitFunction();
        }
        ExitOnFailure(hr, "Failed to check for write access to directory of file: %ls", wzFilePath);

        if (!fWritePermission)
        {
            hr = UtilConvertToVirtualStorePath(wzFilePath, &sczVirtualStorePath);
            ExitOnFailure(hr, "Failed to convert file path to virtualstore path: %ls", wzFilePath);

            // Pass the value up to check for special handling first
            hr = DirSpecialFileRead(pcdb, pSyncProductSession, pFile, sczVirtualStorePath, wzSubPath, &fContinueProcessing);
            ExitOnFailure(hr, "Failed to appropriately check for and handle special directory file under virtualstore, file path: %ls", wzFilePath);

            // If special handling tells us to avoid normal processing for this file, skip it
            if (!fContinueProcessing)
            {
                ExitFunction1(hr = S_OK);
            }

            hr = DirDefaultReadFile(pcdb, pSyncProductSession, pFile->sczName, sczVirtualStorePath, NULL);
            ExitOnFailure(hr, "Failed to read virtualstore file with default handler, path: %ls", sczVirtualStorePath);

            // Check if DirDefaultReadFile found the file or not
            hr = DictKeyExists(pSyncProductSession->shDictValuesSeen, pFile->sczName);
            if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            else
            {
                ExitOnFailure(hr, "Failed to check if file was seen under virtual store path: %ls", pFile->sczName);

                // It saw the file, so let's not proceed to check the non-virtualstore path
                ExitFunction1(hr = S_OK);
            }
        }
    }

    // Pass the value up to check for special handling first
    hr = DirSpecialFileRead(pcdb, pSyncProductSession, pFile, wzFilePath, wzSubPath, &fContinueProcessing);
    ExitOnFailure(hr, "Failed to appropriately check for and handle special directory file, file path: %ls", wzFilePath);

    // If special handling tells us to avoid normal processing for this file, skip it
    if (!fContinueProcessing)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = DirDefaultReadFile(pcdb, pSyncProductSession, pFile->sczName, wzFilePath, wzSubPath);
    ExitOnFailure(hr, "Failed to read file with default handler, path: %ls", wzFilePath);

LExit:
    ReleaseStr(sczVirtualStorePath);

    return hr;
}

static HRESULT ReadDirWriteLegacyDb(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in LEGACY_FILE *pFile,
    __in_z LPCWSTR wzSubDir
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFullPathToVirtualDirectory = NULL;
    LPWSTR wzFullPathBackup = pFile->sczExpandedPath;
    BOOL fWritePermission = TRUE;

    hr = UtilTestWriteAccess(pcdb->hToken, wzSubDir);
    if (E_ACCESSDENIED == hr || E_PATHNOTFOUND == hr)
    {
        fWritePermission = FALSE;
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to check for write access to directory: %ls", wzSubDir);

    if (!fWritePermission)
    {
        // If we don't have write permission to the directory, prefer files in virtualstore
        hr = UtilConvertToVirtualStorePath(wzSubDir, &sczFullPathToVirtualDirectory);
        ExitOnFailure(hr, "Failed to get path equivalent under virtualstore for directory: %ls", sczFullPathToVirtualDirectory);

        // Make the LEGACY_FILE struct temporarily appear as though its natural base path was under virtualstore
        pFile->sczExpandedPath = sczFullPathToVirtualDirectory;
        hr = ReadDirWriteLegacyDbHelp(pcdb, pSyncProductSession, pFile, sczFullPathToVirtualDirectory);
        ExitOnFailure(hr, "Failed to read files out of virtual store subdirectory: %ls", sczFullPathToVirtualDirectory);

        // Restore it back so it isn't pointing to virtualstore anymore
        pFile->sczExpandedPath = wzFullPathBackup;
    }

    hr = ReadDirWriteLegacyDbHelp(pcdb, pSyncProductSession, pFile, wzSubDir);
    if (E_PATHNOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to read files out of subdirectory: %ls", wzSubDir);

LExit:
    ReleaseStr(sczFullPathToVirtualDirectory);
    pFile->sczExpandedPath = wzFullPathBackup;

    return hr;
}

