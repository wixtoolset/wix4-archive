// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// Helper function to resize a CFG_ENUMERATION_STRUCT (and its members)
HRESULT EnumResize(
    __inout CFG_ENUMERATION *pcesEnum,
    __in DWORD dwNewSize
    )
{
    HRESULT hr = S_OK;
    size_t cbPointerSize = 0;
    size_t cbConfigValueSize = 0;
    size_t cbBoolSize = 0;
    size_t cbDwordSize = 0;

    // If we're shrinking, release strings before we actually resize the arrays, losing the pointers
    if (dwNewSize < pcesEnum->dwNumValues)
    {
        switch (pcesEnum->enumType)
        {
        case ENUMERATION_VALUES:
            for (DWORD i = dwNewSize; i < pcesEnum->dwNumValues; ++i)
            {
                ReleaseStr(pcesEnum->values.rgsczName[i]);
                ReleaseCfgValue(pcesEnum->values.rgcValues[i]);
            }
            break;

        case ENUMERATION_PRODUCTS:
            for (DWORD i = dwNewSize; i < pcesEnum->dwNumValues; ++i)
            {
                ReleaseStr(pcesEnum->products.rgsczName[i]);
                ReleaseStr(pcesEnum->products.rgsczVersion[i]);
                ReleaseStr(pcesEnum->products.rgsczPublicKey[i]);
                ReleaseDisplayNameArray(pcesEnum->products.rgrgDisplayNames[i], pcesEnum->products.rgcDisplayNames[i]);
            }
            break;

        case ENUMERATION_VALUE_HISTORY:
            for (DWORD i = dwNewSize; i < pcesEnum->dwNumValues; ++i)
            {
                ReleaseCfgValue(pcesEnum->valueHistory.rgcValues[i]);
                ReleaseStr(pcesEnum->valueHistory.rgsczDbReferences[i]);
            }
            break;

        case ENUMERATION_DATABASE_LIST:
            for (DWORD i = dwNewSize; i < pcesEnum->dwNumValues; ++i)
            {
                ReleaseStr(pcesEnum->databaseList.rgsczFriendlyName[i]);
                ReleaseStr(pcesEnum->databaseList.rgsczPath[i]);
            }
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unexpected enumeration type encountered while resizing enumeration struct to 0 size");
        }
    }

    // If new size is zero, 
    if (0 == dwNewSize)
    {
        switch (pcesEnum->enumType)
        {
        case ENUMERATION_VALUES:
            ReleaseNullMem(pcesEnum->values.rgsczName);
            ReleaseNullMem(pcesEnum->values.rgcValues);
            break;

        case ENUMERATION_PRODUCTS:
            ReleaseNullMem(pcesEnum->products.rgsczName);
            ReleaseNullMem(pcesEnum->products.rgsczVersion);
            ReleaseNullMem(pcesEnum->products.rgsczPublicKey);
            ReleaseNullMem(pcesEnum->products.rgrgDisplayNames);
            ReleaseNullMem(pcesEnum->products.rgcDisplayNames);
            ReleaseNullMem(pcesEnum->products.rgfRegistered);
            break;

        case ENUMERATION_VALUE_HISTORY:
            ReleaseNullMem(pcesEnum->valueHistory.rgcValues);
            ReleaseNullMem(pcesEnum->valueHistory.rgsczDbReferences);
            break;

        case ENUMERATION_DATABASE_LIST:
            ReleaseNullMem(pcesEnum->databaseList.rgsczFriendlyName);
            ReleaseNullMem(pcesEnum->databaseList.rgfSyncByDefault);
            ReleaseNullMem(pcesEnum->databaseList.rgsczPath);
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unexpected enumeration type encountered while resizing enumeration struct to 0 size");
        }
        ExitFunction1(hr = S_OK);
    }

    hr = ::SizeTMult(dwNewSize, sizeof(void *), &(cbPointerSize));
    ExitOnFailure(hr, "Maximum allocation of datatype array exceeded (pointer).");

    hr = ::SizeTMult(dwNewSize, sizeof(CONFIG_VALUE), &(cbConfigValueSize));
    ExitOnFailure(hr, "Maximum allocation of datatype array exceeded (CONFIG_VALUE).");

    hr = ::SizeTMult(dwNewSize, sizeof(BOOL), &(cbBoolSize));
    ExitOnFailure(hr, "Maximum allocation of datatype array exceeded (BOOL).");

    hr = ::SizeTMult(dwNewSize, sizeof(DWORD), &(cbDwordSize));
    ExitOnFailure(hr, "Maximum allocation of datatype array exceeded (DWORD).");

    // If it's a new struct, call memalloc
    if (0 == pcesEnum->dwMaxValues)
    {
        switch (pcesEnum->enumType)
        {
        case ENUMERATION_VALUES:
            pcesEnum->values.rgsczName = static_cast<LPWSTR *>(MemAlloc(cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->values.rgsczName, hr, E_OUTOFMEMORY, "Failed to allocate name array for value type Cfg Enumeration Struct");

            pcesEnum->values.rgcValues = static_cast<CONFIG_VALUE *>(MemAlloc(cbConfigValueSize, TRUE));
            ExitOnNull(pcesEnum->values.rgcValues, hr, E_OUTOFMEMORY, "Failed to allocate value array for value type Cfg Enumeration Struct");
            break;
        case ENUMERATION_PRODUCTS:
            pcesEnum->products.rgsczName = static_cast<LPWSTR *>(MemAlloc(cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->products.rgsczName, hr, E_OUTOFMEMORY, "Failed to allocate name array for product type Cfg Enumeration Struct");

            pcesEnum->products.rgsczVersion = static_cast<LPWSTR *>(MemAlloc(cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->products.rgsczVersion, hr, E_OUTOFMEMORY, "Failed to allocate version array for product type Cfg Enumeration Struct");

            pcesEnum->products.rgsczPublicKey = static_cast<LPWSTR *>(MemAlloc(cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->products.rgsczPublicKey, hr, E_OUTOFMEMORY, "Failed to allocate public key array for product type Cfg Enumeration Struct");

            pcesEnum->products.rgrgDisplayNames = static_cast<DISPLAY_NAME **>(MemAlloc(cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->products.rgrgDisplayNames, hr, E_OUTOFMEMORY, "Failed to allocate display name array of arrays for product type Cfg Enumeration Struct");

            pcesEnum->products.rgcDisplayNames = static_cast<DWORD *>(MemAlloc(cbDwordSize, TRUE));
            ExitOnNull(pcesEnum->products.rgrgDisplayNames, hr, E_OUTOFMEMORY, "Failed to allocate display name array of counts for product type Cfg Enumeration Struct");

            pcesEnum->products.rgfRegistered = static_cast<BOOL *>(MemAlloc(cbBoolSize, TRUE));
            ExitOnNull(pcesEnum->products.rgfRegistered, hr, E_OUTOFMEMORY, "Failed to allocate registered flag array for product type Cfg Enumeration Struct");
            break;
        case ENUMERATION_VALUE_HISTORY:
            pcesEnum->valueHistory.rgcValues = static_cast<CONFIG_VALUE *>(MemAlloc(cbConfigValueSize, TRUE));
            ExitOnNull(pcesEnum->valueHistory.rgcValues, hr, E_OUTOFMEMORY, "Failed to allocate value array for value history type Cfg Enumeration Struct");

            pcesEnum->valueHistory.rgsczDbReferences = static_cast<LPWSTR *>(MemAlloc(cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->valueHistory.rgsczDbReferences, hr, E_OUTOFMEMORY, "Failed to allocate database references array for value history type Cfg Enumeration Struct");
            break;
        case ENUMERATION_DATABASE_LIST:
            pcesEnum->databaseList.rgsczFriendlyName = static_cast<LPWSTR *>(MemAlloc(cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->databaseList.rgsczFriendlyName, hr, E_OUTOFMEMORY, "Failed to allocate friendly name array for database list type Cfg Enumeration Struct");

            pcesEnum->databaseList.rgfSyncByDefault = static_cast<BOOL *>(MemAlloc(cbBoolSize, TRUE));
            ExitOnNull(pcesEnum->databaseList.rgfSyncByDefault, hr, E_OUTOFMEMORY, "Failed to allocate sync by default flag array for database list type Cfg Enumeration Struct");

            pcesEnum->databaseList.rgsczPath = static_cast<LPWSTR *>(MemAlloc(cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->databaseList.rgsczPath, hr, E_OUTOFMEMORY, "Failed to allocate path array for database list type Cfg Enumeration Struct");
            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unexpected enumeration type encountered while initially allocating enumeration struct to %u size", dwNewSize);
        }
    }
    else // else MemReAlloc
    {
        switch (pcesEnum->enumType)
        {
        case ENUMERATION_VALUES:
            pcesEnum->values.rgsczName = static_cast<LPWSTR *>(MemReAlloc(pcesEnum->values.rgsczName, cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->values.rgsczName, hr, E_OUTOFMEMORY, "Failed to reallocate name array for value type Cfg Enumeration Struct");

            pcesEnum->values.rgcValues = static_cast<CONFIG_VALUE *>(MemReAlloc(pcesEnum->values.rgcValues, cbConfigValueSize, TRUE));
            ExitOnNull(pcesEnum->values.rgcValues, hr, E_OUTOFMEMORY, "Failed to reallocate type array for value type Cfg Enumeration Struct");
            break;
        case ENUMERATION_PRODUCTS:
            pcesEnum->products.rgsczName = static_cast<LPWSTR *>(MemReAlloc(pcesEnum->products.rgsczName, cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->products.rgsczName, hr, E_OUTOFMEMORY, "Failed to reallocate name array for product type Cfg Enumeration Struct");

            pcesEnum->products.rgsczVersion = static_cast<LPWSTR *>(MemReAlloc(pcesEnum->products.rgsczVersion, cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->products.rgsczVersion, hr, E_OUTOFMEMORY, "Failed to reallocate version array for product type Cfg Enumeration Struct");

            pcesEnum->products.rgsczPublicKey = static_cast<LPWSTR *>(MemReAlloc(pcesEnum->products.rgsczPublicKey, cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->products.rgsczPublicKey, hr, E_OUTOFMEMORY, "Failed to reallocate public key array for product type Cfg Enumeration Struct");

            pcesEnum->products.rgrgDisplayNames = static_cast<DISPLAY_NAME **>(MemReAlloc(pcesEnum->products.rgrgDisplayNames, cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->products.rgrgDisplayNames, hr, E_OUTOFMEMORY, "Failed to reallocate display name array of arrays for product type Cfg Enumeration Struct");

            pcesEnum->products.rgcDisplayNames = static_cast<DWORD *>(MemReAlloc(pcesEnum->products.rgcDisplayNames, cbDwordSize, TRUE));
            ExitOnNull(pcesEnum->products.rgrgDisplayNames, hr, E_OUTOFMEMORY, "Failed to reallocate display name array of counts for product type Cfg Enumeration Struct");

            pcesEnum->products.rgfRegistered = static_cast<BOOL *>(MemReAlloc(pcesEnum->products.rgfRegistered, cbBoolSize, TRUE));
            ExitOnNull(pcesEnum->products.rgfRegistered, hr, E_OUTOFMEMORY, "Failed to allocate registered flag array for product type Cfg Enumeration Struct");
            break;
        case ENUMERATION_VALUE_HISTORY:
            pcesEnum->valueHistory.rgcValues = static_cast<CONFIG_VALUE *>(MemReAlloc(pcesEnum->valueHistory.rgcValues, cbConfigValueSize, TRUE));
            ExitOnNull(pcesEnum->valueHistory.rgcValues, hr, E_OUTOFMEMORY, "Failed to reallocate type array for value history type Cfg Enumeration Struct");

            pcesEnum->valueHistory.rgsczDbReferences = static_cast<LPWSTR *>(MemReAlloc(pcesEnum->valueHistory.rgsczDbReferences, cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->valueHistory.rgsczDbReferences, hr, E_OUTOFMEMORY, "Failed to reallocate database references array for value history type Cfg Enumeration Struct");
            break;
        case ENUMERATION_DATABASE_LIST:
            pcesEnum->databaseList.rgsczFriendlyName = static_cast<LPWSTR *>(MemReAlloc(pcesEnum->databaseList.rgsczFriendlyName, cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->databaseList.rgsczFriendlyName, hr, E_OUTOFMEMORY, "Failed to reallocate friendly name array for database list type Cfg Enumeration Struct");

            pcesEnum->databaseList.rgfSyncByDefault = static_cast<BOOL *>(MemReAlloc(pcesEnum->databaseList.rgfSyncByDefault, cbBoolSize, TRUE));
            ExitOnNull(pcesEnum->databaseList.rgfSyncByDefault, hr, E_OUTOFMEMORY, "Failed to reallocate sync by default flag array for database list type Cfg Enumeration Struct");

            pcesEnum->databaseList.rgsczPath = static_cast<LPWSTR *>(MemReAlloc(pcesEnum->databaseList.rgsczPath, cbPointerSize, TRUE));
            ExitOnNull(pcesEnum->databaseList.rgsczPath, hr, E_OUTOFMEMORY, "Failed to reallocate path array for database list type Cfg Enumeration Struct");
            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unexpected enumeration type encountered while initially allocating enumeration struct to %u size", dwNewSize);
        }
    }

    // If everything succeeded, record the new size
    pcesEnum->dwMaxValues = dwNewSize;

LExit:
    return hr;
}

HRESULT EnumCopy(
    __in const CFG_ENUMERATION *pcesInput,
    __in DWORD dwCount,
    __in DWORD dwStartIndex,
    __deref_out_opt CFG_ENUMERATION **ppcesEnumOut,
    __out DWORD *pdwOutputCount
    )
{
    HRESULT hr = S_OK;
    DWORD i;
    DWORD dwNewCount = dwCount - dwStartIndex;

    *ppcesEnumOut = static_cast<CFG_ENUMERATION *>(MemAlloc(sizeof(CFG_ENUMERATION), TRUE));
    ExitOnNull(*ppcesEnumOut, hr, E_OUTOFMEMORY, "Failed to allocate memory for output enumeration struct");

    (*ppcesEnumOut)->enumType = pcesInput->enumType;
    hr = EnumResize(*ppcesEnumOut, dwNewCount);
    ExitOnFailure(hr, "Failed to resize enumeration struct while copying enumeration struct");
    (*ppcesEnumOut)->dwNumValues = dwNewCount;

    switch (pcesInput->enumType)
    {
    case ENUMERATION_VALUE_HISTORY:
        if (NULL != pcesInput->valueHistory.sczName)
        {
            hr = StrAllocString(&((*ppcesEnumOut)->valueHistory.sczName), pcesInput->valueHistory.sczName, 0);
            ExitOnFailure(hr, "Failed to copy name of enumeration over");
        }

        for (i = 0; i < dwNewCount; ++i)
        {
            hr = ValueCopy(&pcesInput->valueHistory.rgcValues[dwStartIndex+i], &(*ppcesEnumOut)->valueHistory.rgcValues[i]);
            ExitOnFailure(hr, "Failed to copy value at index %u while copying value history enum", i);
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "EnumCopy received enumType %d, this function only supports ENUMERATION_VALUE_HISTORY", pcesInput->enumType);
        break;
    }

    *pdwOutputCount = dwNewCount;

LExit:
    return hr;
}

HRESULT EnumValues(
    __in CFGDB_STRUCT *pcdb,
    __in CONFIG_VALUETYPE cvType,
    __deref_out CFG_ENUMERATION **ppcesEnumOut,
    __out_opt DWORD *pdwCount
    )
{
    HRESULT hr = S_OK;
    CONFIG_VALUE cvValue = { };
    SCE_QUERY_RESULTS_HANDLE sqrhResults = NULL;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRow = NULL;

    // Allocate the Enumeration struct and its members
    CFG_ENUMERATION *pcesEnum = static_cast<CFG_ENUMERATION *>(MemAlloc(sizeof(CFG_ENUMERATION), TRUE));
    ExitOnNull(pcesEnum, hr, E_OUTOFMEMORY, "Failed to allocate Cfg Enumeration Struct");

    pcesEnum->enumType = ENUMERATION_VALUES;

    hr = EnumResize(pcesEnum, 64);
    ExitOnFailure(hr, "Failed to resize enumeration struct immediately after its creation");

    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_TABLE, 0, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into value table");

    hr = SceSetQueryColumnDword(sqhHandle, pcdb->dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", pcdb->dwAppID);

    hr = SceRunQueryRange(&sqhHandle, &sqrhResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query");

    hr = SceGetNextResultRow(sqrhResults, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next result row");

        hr = ValueRead(pcdb, sceRow, &cvValue);
        ExitOnFailure(hr, "Failed to read value from row while enumerating values");

        // Only add the type if it's in our bitmask
        if (0 != (cvType & cvValue.cvType))
        {
            if (pcesEnum->dwNumValues >= pcesEnum->dwMaxValues)
            {
                DWORD dwNewSize = pcesEnum->dwMaxValues * 2;

                hr = EnumResize(pcesEnum, dwNewSize);
                ExitOnFailure(hr, "Failed to resize enumeration struct");
            }
                
            // Effectively move the struct into the enum
            memcpy(&pcesEnum->values.rgcValues[pcesEnum->dwNumValues], &cvValue, sizeof(cvValue));
            ZeroMemory(&cvValue, sizeof(cvValue));

            // Read the name of the value
            hr = SceGetColumnString(sceRow, VALUE_COMMON_NAME, &pcesEnum->values.rgsczName[pcesEnum->dwNumValues]);
            ExitOnFailure(hr, "Failed to read name from value for enum");

            ++pcesEnum->dwNumValues;
        }
        else
        {
            ReleaseNullCfgValue(cvValue);
        }

        ReleaseNullSceRow(sceRow);
        hr = SceGetNextResultRow(sqrhResults, &sceRow);
    }
    hr = S_OK;

    if (pcesEnum->dwNumValues < pcesEnum->dwMaxValues)
    {
        // Now that we'll no longer be adding to the struct, free any unneeded space we allocated
        hr = EnumResize(pcesEnum, pcesEnum->dwNumValues);
        ExitOnFailure(hr, "Failed to free unneeded memory from enumeration struct");
    }

    if (NULL != pdwCount)
    {
        *pdwCount = pcesEnum->dwNumValues;
    }

    if (0 < pcesEnum->dwNumValues && NULL != ppcesEnumOut)
    {
        *ppcesEnumOut = pcesEnum;
        pcesEnum = NULL;
    }

LExit:
    ReleaseCfgValue(cvValue);
    EnumFree(pcesEnum);
    ReleaseSceQuery(sqhHandle);
    ReleaseSceQueryResults(sqrhResults);
    ReleaseSceRow(sceRow);

    return hr;
}

// Helper function to free an enumeration struct
void EnumFree(
    __in_opt CFG_ENUMERATION *pcesEnum
    )
{
    if (NULL == pcesEnum)
    {
        return;
    }

    EnumResize(pcesEnum, 0);

    ReleaseMem(pcesEnum);
}

HRESULT EnumPastValues(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzName,
    __deref_opt_out CFG_ENUMERATION **ppcesEnumOut,
    __out_opt DWORD *pdwCount
    )
{
    HRESULT hr = S_OK;
    CONFIG_VALUE cvValue = { };
    SCE_QUERY_RESULTS_HANDLE sqrhResults = NULL;
    SCE_QUERY_HANDLE sqhHandle = NULL;
    SCE_ROW_HANDLE sceRow = NULL;
    LPWSTR sczDbReferences = NULL;

    // Allocate the Enumeration struct and its members
    CFG_ENUMERATION *pcesEnum = static_cast<CFG_ENUMERATION *>(MemAlloc(sizeof(CFG_ENUMERATION), TRUE));
    ExitOnNull(pcesEnum, hr, E_OUTOFMEMORY, "Failed to allocate Cfg Enumeration Struct");

    pcesEnum->enumType = ENUMERATION_VALUE_HISTORY;

    hr = EnumResize(pcesEnum, 64);
    ExitOnFailure(hr, "Failed to resize enumeration struct immediately after its creation");

    hr = SceBeginQuery(pcdb->psceDb, VALUE_INDEX_HISTORY_TABLE, 1, &sqhHandle);
    ExitOnFailure(hr, "Failed to begin query into value table");

    hr = SceSetQueryColumnDword(sqhHandle, pcdb->dwAppID);
    ExitOnFailure(hr, "Failed to set query column dword to: %u", pcdb->dwAppID);

    hr = SceSetQueryColumnString(sqhHandle, wzName);
    ExitOnFailure(hr, "Failed to set query column string to: %ls", wzName);

    hr = SceRunQueryRange(&sqhHandle, &sqrhResults);
    if (E_NOTFOUND == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to run query");

    hr = StrAllocString(&(pcesEnum->valueHistory.sczName), wzName, 0);
    ExitOnFailure(hr, "Failed to allocate copy of name of value");

    hr = SceGetNextResultRow(sqrhResults, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get next result row");

        hr = ValueRead(pcdb, sceRow, &cvValue);
        ExitOnFailure(hr, "Failed to read value from row while enumerating values");

        hr = SceGetColumnString(sceRow, VALUE_HISTORY_DB_REFERENCES, &sczDbReferences);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to get database references");

        if (pcesEnum->dwNumValues >= pcesEnum->dwMaxValues)
        {
            DWORD dwNewSize = pcesEnum->dwMaxValues * 2;

            hr = EnumResize(pcesEnum, dwNewSize);
            ExitOnFailure(hr, "Failed to resize enumeration struct");
        }

        // Effectively move the struct into the enum
        memcpy(&pcesEnum->valueHistory.rgcValues[pcesEnum->dwNumValues], &cvValue, sizeof(cvValue));
        ZeroMemory(&cvValue, sizeof(cvValue));

        pcesEnum->valueHistory.rgsczDbReferences[pcesEnum->dwNumValues] = sczDbReferences;
        sczDbReferences = NULL;

        ++pcesEnum->dwNumValues;

        ReleaseNullSceRow(sceRow);
        hr = SceGetNextResultRow(sqrhResults, &sceRow);
    }
    hr = S_OK;

    if (pcesEnum->dwNumValues < pcesEnum->dwMaxValues)
    {
        // Now that we'll no longer be adding to the struct, free any unneeded space we allocated
        hr = EnumResize(pcesEnum, pcesEnum->dwNumValues);
        ExitOnFailure(hr, "Failed to free unneeded memory from enumeration struct");
    }

    if (NULL != pdwCount)
    {
        *pdwCount = pcesEnum->dwNumValues;
    }

    if (0 < pcesEnum->dwNumValues && NULL != ppcesEnumOut)
    {
        *ppcesEnumOut = pcesEnum;
        pcesEnum = NULL;
    }

LExit:
    ReleaseCfgValue(cvValue);
    ReleaseStr(sczDbReferences);
    EnumFree(pcesEnum);
    ReleaseSceQuery(sqhHandle);
    ReleaseSceQueryResults(sqrhResults);
    ReleaseSceRow(sceRow);

    return hr;
}

HRESULT EnumDatabaseList(
    __in CFGDB_STRUCT *pcdb,
    __deref_opt_out CFG_ENUMERATION **ppcesEnumOut,
    __out_opt DWORD *pcCount
    )
{
    HRESULT hr = S_OK;
    SCE_ROW_HANDLE sceRow = NULL;

    // Allocate the Enumeration struct and its members
    CFG_ENUMERATION *pcesEnum = static_cast<CFG_ENUMERATION *>(MemAlloc(sizeof(CFG_ENUMERATION), TRUE));
    ExitOnNull(pcesEnum, hr, E_OUTOFMEMORY, "Failed to allocate Cfg Enumeration Struct");

    pcesEnum->enumType = ENUMERATION_DATABASE_LIST;

    hr = EnumResize(pcesEnum, 64);
    ExitOnFailure(hr, "Failed to resize enumeration struct immediately after its creation");

    hr = SceGetFirstRow(pcdb->psceDb, DATABASE_INDEX_TABLE, &sceRow);
    while (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get row from table: %u", DATABASE_INDEX_TABLE);

        hr = SceGetColumnString(sceRow, DATABASE_INDEX_FRIENDLY_NAME, &(pcesEnum->databaseList.rgsczFriendlyName[pcesEnum->dwNumValues]));
        ExitOnFailure(hr, "Failed to get friendly name from database index table");

        hr = SceGetColumnBool(sceRow, DATABASE_INDEX_SYNC_BY_DEFAULT, &(pcesEnum->databaseList.rgfSyncByDefault[pcesEnum->dwNumValues]));
        ExitOnFailure(hr, "Failed to get 'sync by default' flag from database index table");

        hr = SceGetColumnString(sceRow, DATABASE_INDEX_PATH, &(pcesEnum->databaseList.rgsczPath[pcesEnum->dwNumValues]));
        ExitOnFailure(hr, "Failed to get path from database index table");

        ++pcesEnum->dwNumValues;

        ReleaseNullSceRow(sceRow);
        hr = SceGetNextRow(pcdb->psceDb, DATABASE_INDEX_TABLE, &sceRow);
    }
    hr = S_OK;

    if (pcesEnum->dwNumValues < pcesEnum->dwMaxValues)
    {
        // Now that we'll no longer be adding to the struct, free any unneeded space we allocated
        hr = EnumResize(pcesEnum, pcesEnum->dwNumValues);
        ExitOnFailure(hr, "Failed to free unneeded memory from enumeration struct");
    }

    if (NULL != pcCount)
    {
        *pcCount = pcesEnum->dwNumValues;
    }

    if (0 < pcesEnum->dwNumValues && NULL != ppcesEnumOut)
    {
        *ppcesEnumOut = pcesEnum;
        pcesEnum = NULL;
    }

LExit:
    EnumFree(pcesEnum);
    ReleaseSceRow(sceRow);

    return hr;
}

HRESULT EnumFindValueInHistory(
    __in const CFG_ENUMERATION *pceSearchEnum,
    __in DWORD dwCount,
    __in const CONFIG_VALUE *pValue,
    __out_opt DWORD *pdwIndex
    )
{
    HRESULT hr = S_OK;
    BOOL fResult;

    if (ENUMERATION_VALUE_HISTORY != pceSearchEnum->enumType)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "EnumFindValueInHistory() requires an enumerations of type ENUMERATION_VALUE_HISTORY");
    }

    for (DWORD i = 0; i < dwCount; ++i)
    {
        fResult = FALSE;
        hr = ValueCompare(&pceSearchEnum->valueHistory.rgcValues[i], pValue, TRUE, &fResult);
        ExitOnFailure(hr, "Failed to compare two values from history enums");

        if (fResult)
        {
            *pdwIndex = i;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

HRESULT EnumWriteValue(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzName,
    __in const CFG_ENUMERATION *pceEnum,
    __in DWORD dwEnumIndex,
    __in_opt CFGDB_STRUCT *pcdbReferencedBy
    )
{
    HRESULT hr = S_OK;

    if (ENUMERATION_VALUES == pceEnum->enumType)
    {
        hr = ValueWrite(pcdb, pcdb->dwAppID, wzName, &pceEnum->values.rgcValues[dwEnumIndex], FALSE, pcdbReferencedBy);
        ExitOnFailure(hr, "Failed to set value from value enum");
    }
    else if (ENUMERATION_VALUE_HISTORY == pceEnum->enumType)
    {
        hr = ValueWrite(pcdb, pcdb->dwAppID, wzName, &pceEnum->valueHistory.rgcValues[dwEnumIndex], FALSE, pcdbReferencedBy);
        ExitOnFailure(hr, "Failed to set value from value history enum");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "EnumWriteValue() only supports input enums of type ENUMERATION_VALUES or ENUMERATION_VALUE_HISTORY");
    }

LExit:
    return hr;
}

