#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT FilterCheckValue(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzName,
    __out_opt BOOL *pfIgnore,
    __out_opt BOOL *pfShareWriteOnRead
    );

#ifdef __cplusplus
}
#endif
