//-------------------------------------------------------------------------------------------------
// <copyright file="value.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Internal utility functions for dealing with user value data in Cfg API
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


#ifdef __cplusplus
extern "C" {
#endif

#define ReleaseCfgValue(x) { ValueFree(&x); }
#define ReleaseNullCfgValue(x) { ValueFree(&x); ZeroMemory(&x, sizeof(x)); }

enum CFG_BLOB_TYPE
{
    CFG_BLOB_INVALID = 0,
    CFG_BLOB_POINTER,
    CFG_BLOB_DB_STREAM
};

// Represents a value in memory
struct CONFIG_VALUE
{
    CONFIG_VALUETYPE cvType;
    SYSTEMTIME stWhen;
    LPWSTR sczBy;
    BOOL fReleaseBy;

    union
    {
        struct
        {
            CFG_BLOB_TYPE cbType;
            SIZE_T cbValue;
            BYTE rgbHash[CFG_HASH_LEN];

            union
            {
                struct
                {
                    const BYTE* pbValue;

                    // Whether the memory is "owned" (was allocated) by this value
                    BOOL fRelease;
                } pointer;
                struct
                {
                    DWORD dwContentID;
                    CFGDB_STRUCT *pcdb;
                } dbstream;
            };
        } blob;
        struct
        {
            LPWSTR sczValue;

            // Whether to release the memory when the variant is freed
            BOOL fRelease;
        } string;
        struct
        {
            DWORD dwValue;
        } dword;
        struct
        {
            DWORD64 qwValue;
        } qword;
        struct
        {
            BOOL fValue;
        } boolean;
    };
};

// Compares two values for equality. Ignores source of the value ('when' and 'by')
HRESULT ValueCompare(
    __in const CONFIG_VALUE *pcvValue1,
    __in const CONFIG_VALUE *pcvValue2,
    __out BOOL *pfResult
    );
HRESULT ValueCopy(
    __in CONFIG_VALUE *pcvInput,
    __out CONFIG_VALUE *pcvOutput
    );
HRESULT ValueSetDelete(
    __in_opt const SYSTEMTIME *pst,
    __in_z_opt LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    );
HRESULT ValueSetBlob(
    __in const BYTE* pbValue,
    __in SIZE_T cbValue,
    __in BOOL fCopy,
    __in_opt const SYSTEMTIME *pst,
    __in_z_opt LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    );
HRESULT ValueSetBlobDbStream(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwContentID,
    __in_opt const SYSTEMTIME *pst,
    __in_z_opt LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    );
HRESULT ValueSetString(
    __in_z LPCWSTR wzValue,
    __in BOOL fCopy,
    __in_opt const SYSTEMTIME *pst,
    __in_z_opt LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    );
HRESULT ValueSetDword(
    __in DWORD dwValue,
    __in_opt const SYSTEMTIME *pst,
    __in_z_opt LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    );
HRESULT ValueSetQword(
    __in DWORD64 qwValue,
    __in_opt const SYSTEMTIME *pst,
    __in_z_opt LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    );
HRESULT ValueSetBool(
    __in BOOL fValue,
    __in_opt const SYSTEMTIME *pst,
    __in_z_opt LPCWSTR wzBy,
    __deref_out CONFIG_VALUE *pcvValue
    );
HRESULT ValueWrite(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzName,
    __in CONFIG_VALUE *pcvValue,
    __in BOOL fIgnoreSameValue
    );
// Reads a value into memory from a database. Can read from either value index or value index history table.
HRESULT ValueRead(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE sceValueRow,
    __deref_out CONFIG_VALUE *pcvValue
    );
HRESULT ValueReadHistory(
    __in CFGDB_STRUCT *pcdb,
    __in SCE_ROW_HANDLE sceValueHistoryRow,
    __deref_out CONFIG_VALUE *pcvValue
    );
// Checks if the value in sceRow1 has an identical corresponding current value in pcdb2
// If they do have identical values but have different timestamps, copies the newer timestamped
// to the database with the older value, so their latest history entry exactly matches
HRESULT ValueMatch(
    __in_z LPCWSTR sczName,
    __in CFGDB_STRUCT *pcdb1,
    __in CFGDB_STRUCT *pcdb2,
    __in SCE_ROW_HANDLE sceRow1,
    __out BOOL *pfResult
    );
void ValueFree(
    __inout CONFIG_VALUE * pcvValue
    );
HRESULT ValueFindRow(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzValueName,
    __out SCE_ROW_HANDLE *pRowHandle
    );
HRESULT ValueFindHistoryRow(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __in_z LPCWSTR wzValueName,
    __in const SYSTEMTIME *pWhen,
    __in LPCWSTR wzBy,
    __out SCE_ROW_HANDLE *pRowHandle
    );
HRESULT ValueForget(
    __in CFGDB_STRUCT *pcdb,
    __in DWORD dwAppID,
    __inout SCE_ROW_HANDLE *psceValueRow
    );

#ifdef __cplusplus
}
#endif
