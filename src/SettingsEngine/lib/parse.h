#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

HRESULT ParseManifest(
    __in_z LPCWSTR wzFileContents,
    __out LEGACY_PRODUCT *pProduct
    );

#ifdef __cplusplus
}
#endif
