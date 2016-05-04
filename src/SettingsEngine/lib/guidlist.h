#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT GuidListEnsure(
    __in CFGDB_STRUCT *pcdb,
    __in LPCWSTR wzOtherGuid,
    __out LPWSTR *psczString
    );

#ifdef __cplusplus
}
#endif
