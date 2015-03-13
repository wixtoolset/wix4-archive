#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="uncutil.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Header for UNC helper functions. These aren't just simple path string parser / manipulators
//    (those belong in pathutil) these functions actually require win32 libraries to
//    do heavier work related to UNC shares.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifdef __cplusplus
extern "C" {
#endif

/*******************************************************************
 UncConvertFromMountedDrive - Converts the string in-place from a
                mounted drive path to a UNC path
*******************************************************************/
DAPI_(HRESULT) UncConvertFromMountedDrive(
    __inout LPWSTR *psczUNCPath,
    __in LPCWSTR sczMountedDrivePath
    );

#ifdef __cplusplus
}
#endif
