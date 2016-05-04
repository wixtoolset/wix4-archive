// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT RegDefaultReadValueBinary(
    __in CFGDB_STRUCT *pcdb,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in_z LPCWSTR wzCfgValueName
    );
static HRESULT RegDefaultReadValueString(
    __in CFGDB_STRUCT *pcdb,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in_z LPCWSTR wzCfgValueName
    );
static HRESULT RegDefaultReadValueDword(
    __in CFGDB_STRUCT *pcdb,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in_z LPCWSTR wzCfgValueName
    );
static HRESULT RegDefaultReadValueQword(
    __in CFGDB_STRUCT *pcdb,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in_z LPCWSTR wzCfgValueName
    );

extern "C" HRESULT RegDefaultReadValue(
    __in CFGDB_STRUCT *pcdb,
    __in LEGACY_SYNC_PRODUCT_SESSION *pSyncProductSession,
    __in_z LPCWSTR wzNamespace,
    __in HKEY hkKey,
    __in_z LPCWSTR wzRegKey,
    __in_z LPCWSTR wzValueName,
    __in DWORD dwRegType
    )
{
    HRESULT hr = S_OK;
    BOOL fIgnore = FALSE;
    LPWSTR sczCfgValueName = NULL;

    hr = MapRegValueToCfgName(wzNamespace, wzRegKey, wzValueName, &sczCfgValueName);
    ExitOnFailure(hr, "Failed to format default legacy value name from namespace: %ls, key: %ls, valuename: %ls", wzNamespace, wzRegKey, wzValueName);

    hr = FilterCheckValue(&pSyncProductSession->product, sczCfgValueName, &fIgnore, NULL);
    ExitOnFailure(hr, "Failed to check if cfg value should be ignored: %ls", sczCfgValueName);

    if (fIgnore)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = DictAddKey(pSyncProductSession->shDictValuesSeen, sczCfgValueName);
    ExitOnFailure(hr, "Failed to add to dictionary value: %ls", sczCfgValueName);

    switch (dwRegType)
    {
    case REG_BINARY:
        hr = RegDefaultReadValueBinary(pcdb, hkKey, wzValueName, sczCfgValueName);
        ExitOnFailure(hr, "Failed to handle binary value by default handler while reading from registry: %ls", wzValueName);
        break;

    case REG_SZ:
        hr = RegDefaultReadValueString(pcdb, hkKey, wzValueName, sczCfgValueName);
        ExitOnFailure(hr, "Failed to handle string value by default handler while reading from registry: %ls", wzValueName);
        break;

    case REG_DWORD:
        hr = RegDefaultReadValueDword(pcdb, hkKey, wzValueName, sczCfgValueName);
        ExitOnFailure(hr, "Failed to handle dword value by default handler while reading from registry: %ls", wzValueName);
        break;

    case REG_QWORD:
        hr = RegDefaultReadValueQword(pcdb, hkKey, wzValueName, sczCfgValueName);
        ExitOnFailure(hr, "Failed to handle qword value by default handler while reading from registry: %ls", wzValueName);
        break;

    default:
        // Ignore this value, it's unsupported
        ExitFunction1(hr = S_OK);
        break;
    }

LExit:
    ReleaseStr(sczCfgValueName);

    return hr;
}

extern "C" HRESULT RegDefaultWriteValue(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzName,
    __in const CONFIG_VALUE *pcvValue,
    __out BOOL *pfHandled
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    LPWSTR sczRegKey = NULL;
    LPWSTR sczRegValueName = NULL;
    BYTE *pbBuffer = NULL;
    SIZE_T cbBuffer = 0;
    BOOL fReleaseBuffer = FALSE;
    DWORD dwRoot = DWORD_MAX;
    HKEY hk = NULL;

    hr = MapCfgNameToRegValue(pProduct, wzName, &dwRoot, &sczRegKey, &sczRegValueName);
    if (E_INVALIDARG == hr)
    {
        *pfHandled = FALSE;
        // Not a regkey, so just ignore
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to convert value name to registry information");
    *pfHandled = TRUE;

    hr = RegOpen(ManifestConvertToRootKey(dwRoot), sczRegKey, KEY_SET_VALUE, &hk);
    if (E_FILENOTFOUND == hr)
    {
        hr = S_OK;

        // The key doesn't exist, so no need to proceed with deleting the value
        if (VALUE_DELETED == pcvValue->cvType)
        {
            ExitFunction1(hr = S_OK);
        }
        hr = RegCreate(ManifestConvertToRootKey(dwRoot), sczRegKey, KEY_SET_VALUE, &hk);
        ExitOnFailure(hr, "Failed to create regkey: %ls", sczRegKey);
    }
    ExitOnFailure(hr, "Failed to open regkey: %ls", sczRegKey);

    switch (pcvValue->cvType)
    {
    case VALUE_DELETED:
        hr = RegWriteString(hk, sczRegValueName, NULL);
        ExitOnFailure(hr, "Failed to delete existing value");
        break;

    case VALUE_BLOB:
        switch (pcvValue->blob.cbType)
        {
        case CFG_BLOB_POINTER:
            pbBuffer = const_cast<BYTE *>(pcvValue->blob.pointer.pbValue);
            cbBuffer = pcvValue->blob.cbValue;
            break;
        case CFG_BLOB_DB_STREAM:
            fReleaseBuffer = TRUE;
            hr = StreamRead(pcvValue->blob.dbstream.pcdb, pcvValue->blob.dbstream.dwContentID, NULL, &pbBuffer, &cbBuffer);
            ExitOnFailure(hr, "Failed to read stream from database while writing binary to the registry");
            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Invalid blob type encountered");
            break;
        }
        hr = RegWriteBinary(hk, sczRegValueName, pbBuffer, cbBuffer);
        ExitOnFailure(hr, "Failed to write binary value to registry");
        break;

    case VALUE_STRING:
        hr = RegWriteString(hk, sczRegValueName, pcvValue->string.sczValue);
        ExitOnFailure(hr, "Failed to write string to registry");
        break;

    case VALUE_DWORD:
        hr = RegWriteNumber(hk, sczRegValueName, pcvValue->dword.dwValue);
        ExitOnFailure(hr, "Failed to write dword to registry");
        break;

    case VALUE_QWORD:
        hr = RegWriteQword(hk, sczRegValueName, pcvValue->qword.qwValue);
        ExitOnFailure(hr, "Failed to write qword to registry");
        break;

    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    ReleaseRegKey(hk);
    ReleaseStr(sczValue);
    ReleaseStr(sczRegKey);
    ReleaseStr(sczRegValueName);
    if (fReleaseBuffer)
    {
        ReleaseMem(pbBuffer);
    }

    return hr;
}

// Static functions
static HRESULT RegDefaultReadValueBinary(
    __in CFGDB_STRUCT *pcdb,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in_z LPCWSTR wzCfgValueName
    )
{
    HRESULT hr = S_OK;
    BYTE *pbBuffer = NULL;
    SIZE_T cbBuffer = 0;
    CONFIG_VALUE cvNewValue = { };

    hr = RegReadBinary(hkKey, wzValueName, &pbBuffer, &cbBuffer);
    ExitOnFailure(hr, "Failed to read binary value from registry: %ls", wzValueName);

    hr = ValueSetBlob(pbBuffer, cbBuffer, FALSE, NULL, pcdb->sczGuid, &cvNewValue);
    ExitOnFailure(hr, "Failed to set string value %ls in memory", wzCfgValueName);

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzCfgValueName, &cvNewValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set value in db: %ls", wzCfgValueName);

LExit:
    ReleaseMem(pbBuffer);
    ReleaseCfgValue(cvNewValue);

    return hr;
}

static HRESULT RegDefaultReadValueString(
    __in CFGDB_STRUCT *pcdb,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in_z LPCWSTR wzCfgValueName
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczStringValue = NULL;
    CONFIG_VALUE cvNewValue = { };

    hr = RegReadString(hkKey, wzValueName, &sczStringValue);
    ExitOnFailure(hr, "Failed to read string value from registry: %ls", wzValueName);

    hr = ValueSetString(sczStringValue, FALSE, NULL, pcdb->sczGuid, &cvNewValue);
    ExitOnFailure(hr, "Failed to set string value %ls in memory", wzCfgValueName);

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzCfgValueName, &cvNewValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set value in db: %ls", wzCfgValueName);

LExit:
    ReleaseStr(sczStringValue);
    ReleaseCfgValue(cvNewValue);

    return hr;
}

static HRESULT RegDefaultReadValueDword(
    __in CFGDB_STRUCT *pcdb,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in_z LPCWSTR wzCfgValueName
    )
{
    HRESULT hr = S_OK;
    DWORD dwValue = 0;
    CONFIG_VALUE cvNewValue = { };

    hr = RegReadNumber(hkKey, wzValueName, &dwValue);
    ExitOnFailure(hr, "Failed to read dword value from registry: %ls", wzValueName);

    hr = ValueSetDword(dwValue, NULL, pcdb->sczGuid, &cvNewValue);
    ExitOnFailure(hr, "Failed to set string value %ls in memory", wzCfgValueName);

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzCfgValueName, &cvNewValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set value in db: %ls", wzCfgValueName);

LExit:
    ReleaseCfgValue(cvNewValue);

    return hr;
}

static HRESULT RegDefaultReadValueQword(
    __in CFGDB_STRUCT *pcdb,
    __in HKEY hkKey,
    __in_z LPCWSTR wzValueName,
    __in_z LPCWSTR wzCfgValueName
    )
{
    HRESULT hr = S_OK;
    DWORD64 qwValue = 0;
    CONFIG_VALUE cvNewValue = { };

    hr = RegReadQword(hkKey, wzValueName, &qwValue);
    ExitOnFailure(hr, "Failed to read qword value from registry: %ls", wzValueName);

    hr = ValueSetQword(qwValue, NULL, pcdb->sczGuid, &cvNewValue);
    ExitOnFailure(hr, "Failed to set string value %ls in memory", wzCfgValueName);

    hr = ValueWrite(pcdb, pcdb->dwAppID, wzCfgValueName, &cvNewValue, TRUE, NULL);
    ExitOnFailure(hr, "Failed to set value in db: %ls", wzCfgValueName);

LExit:
    ReleaseCfgValue(cvNewValue);

    return hr;
}
