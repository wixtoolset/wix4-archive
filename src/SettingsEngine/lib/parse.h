//-------------------------------------------------------------------------------------------------
// <copyright file="parse.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Legacy settings engine API parsing code (for legacy XML manifests)
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once


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
