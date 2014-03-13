//-------------------------------------------------------------------------------------------------
// <copyright file="enum.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Utility functions related to enumerations
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#ifdef __cplusplus
extern "C" {
#endif

enum ENUMERATION_TYPE
{
    ENUMERATION_INVALID = 0,
    ENUMERATION_VALUES,
    ENUMERATION_PRODUCTS,
    ENUMERATION_VALUE_HISTORY,
    ENUMERATION_DATABASE_LIST
};

struct CFG_ENUMERATION
{
    ENUMERATION_TYPE enumType;
    DWORD dwNumValues; // The number of values in the enumeration
    DWORD dwMaxValues; // The actual size of the dynamic arrays

    union
    {
        struct
        {
            LPWSTR *rgsczName;
            CONFIG_VALUE *rgcValues;
        } values;
        struct
        {
            LPWSTR *rgsczName;
            LPWSTR *rgsczVersion;
            LPWSTR *rgsczPublicKey;
            BOOL *rgfRegistered;
        } products;
        struct
        {
            LPWSTR sczName;
            CONFIG_VALUE *rgcValues;
        } valueHistory;
        struct
        {
            LPWSTR *rgsczFriendlyName;
            BOOL *rgfSyncByDefault;
            LPWSTR *rgsczPath;
        } databaseList;
    };
};

HRESULT EnumResize(
    __inout CFG_ENUMERATION *pcesEnum,
    __in DWORD dwNewSize
    );
HRESULT EnumCopy(
    __in const CFG_ENUMERATION *pcesInput,
    __in DWORD dwCount,
    __in DWORD dwStartIndex,
    __deref_out_opt CFG_ENUMERATION **ppcesEnumOut,
    __out DWORD *pdwOutputCount
    );
void EnumFree(
    __in_opt CFG_ENUMERATION *pcesEnum
    );
HRESULT EnumPastValues(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzName,
    __deref_opt_out CFG_ENUMERATION **ppcesEnumOut,
    __out_opt DWORD *pdwCount
    );
HRESULT EnumDatabaseList(
    __in CFGDB_STRUCT *pcdb,
    __deref_opt_out CFG_ENUMERATION **ppcesEnumOut,
    __out_opt DWORD *pcCount
    );
// Returns E_NOTFOUND if it wasn't found. If it was found, sets *pdwIndex to the index of the found value in pceSearchEnum
HRESULT EnumFindValueInHistory(
    __in const CFG_ENUMERATION *pceSearchEnum,
    __in DWORD dwCount,
    __in const CONFIG_VALUE *pValue,
    __out_opt DWORD *pdwIndex
    );
HRESULT EnumValues(
    __in CFGDB_STRUCT *pcdb,
    __in CONFIG_VALUETYPE cvType,
    __deref_out CFG_ENUMERATION **ppcesEnumOut,
    __out_opt DWORD *pdwCount
    );
HRESULT EnumWriteValue(
    __in CFGDB_STRUCT *pcdb,
    __in_z LPCWSTR wzName,
    __in const CFG_ENUMERATION *pceEnum,
    __in DWORD dwEnumIndex
    );

#ifdef __cplusplus
}
#endif
