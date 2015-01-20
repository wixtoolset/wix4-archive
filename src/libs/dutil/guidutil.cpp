//-------------------------------------------------------------------------------------------------
// <copyright file="guidutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

extern "C" HRESULT DAPI GuidCreate(
    _Out_z_cap_c_(GUID_STRING_LENGTH) WCHAR* wzGuid
    )
{
    HRESULT hr = S_OK;
    UUID guid = { };

    hr = HRESULT_FROM_RPC(::UuidCreate(&guid));
    ExitOnFailure(hr, "UuidCreate failed.");

    if (!::StringFromGUID2(guid, wzGuid, GUID_STRING_LENGTH))
    {
        hr = E_OUTOFMEMORY;
        ExitOnRootFailure(hr, "Failed to convert guid into string.");
    }

LExit:
    return hr;
}

extern "C" HRESULT DAPI GuidCreateScz(
    __deref_out_z LPWSTR* psczGuid
    )
{
    HRESULT hr = S_OK;

    hr = StrAlloc(psczGuid, GUID_STRING_LENGTH);
    ExitOnFailure(hr, "Failed to allocate space for guid");

    hr = GuidCreate(*psczGuid);
    ExitOnFailure(hr, "Failed to create new guid.");

LExit:
    return hr;
}
