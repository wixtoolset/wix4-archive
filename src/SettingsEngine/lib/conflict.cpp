// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT ConflictResolve(
    __in CFGDB_STRUCT *pcdbRemote,
    __in CONFLICT_PRODUCT *cpProduct,
    __in DWORD dwValueIndex
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzValueName = NULL;
    RESOLUTION_CHOICE rcChoice = cpProduct->rgrcValueChoices[dwValueIndex];
    CFG_ENUMERATION *pEnum;

    hr = CfgEnumReadString(cpProduct->rgcesValueEnumLocal[dwValueIndex], 0, ENUM_DATA_VALUENAME, &wzValueName);
    ExitOnFailure(hr, "Failed to read value name from enum");

    if (RESOLUTION_LOCAL == rcChoice)
    {
        pEnum = static_cast<CFG_ENUMERATION *>(cpProduct->rgcesValueEnumLocal[dwValueIndex]);

        hr = ValueTransferFromHistory(pcdbRemote, pEnum, 0, pcdbRemote->pcdbLocal);
        ExitOnFailure(hr, "Failed to resolve value history (chose local) for value %ls", wzValueName);
    }
    else if (RESOLUTION_REMOTE == rcChoice)
    {
        pEnum = static_cast<CFG_ENUMERATION *>(cpProduct->rgcesValueEnumRemote[dwValueIndex]);

        hr = ValueTransferFromHistory(pcdbRemote->pcdbLocal, pEnum, 0, pcdbRemote);
        ExitOnFailure(hr, "Failed to resolve value history (chose remote) for value %ls", wzValueName);
    }
    
LExit:
    return hr;
}

HRESULT ConflictGetList(
    __in const CFG_ENUMERATION *pcesInEnum1,
    __in DWORD dwCount1,
    __in const CFG_ENUMERATION *pcesInEnum2,
    __in DWORD dwCount2,
    __deref_out_bcount_opt(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *pcehOutHandle1,
    __out DWORD *pdwOutCount1,
    __deref_out_bcount_opt(CFG_ENUMERATION_HANDLE_BYTES) CFG_ENUMERATION_HANDLE *pcehOutHandle2,
    __out DWORD *pdwOutCount2
    )
{
    HRESULT hr = S_OK;
    DWORD i;
    DWORD dwFoundIndex = 0;
    DWORD dwStartCopyIndex = 0;

    *pdwOutCount1 = 0;
    *pdwOutCount2 = 0;

    if (ENUMERATION_VALUE_HISTORY != pcesInEnum1->enumType || ENUMERATION_VALUE_HISTORY != pcesInEnum2->enumType)
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "ConflictGetList() requires both enumerations to be of type ENUMERATION_VALUE_HISTORY");
     }

    // we subtract 1 because it's a 0-based array, and 1 more because we already checked the last index of the array
    // when we checked for subsumation
    dwStartCopyIndex = 0;
    for (i = dwCount1 - 2; i != DWORD_MAX; --i)
    {
        hr = EnumFindValueInHistory(pcesInEnum2, dwCount2, pcesInEnum1->valueHistory.rgcValues + i, &dwFoundIndex);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
            continue;
        }
        ExitOnFailure(hr, "Failed to search in enum 2");

        dwStartCopyIndex = i + 1;
        break;
    }

    // Now create pcehOutHandle1 with all the members of pcesInEnum1 after index dwFoundIndex
    hr = EnumCopy(pcesInEnum1, dwCount1, dwStartCopyIndex, reinterpret_cast<CFG_ENUMERATION **>(pcehOutHandle1), pdwOutCount1);
    ExitOnFailure(hr, "Failed to copy enumeration struct 1");

    // we subtract 1 because it's a 0-based array, and 1 more because we already checked the last index of the array
    // when we checked for subsumation
    dwStartCopyIndex = 0;
    for (i = dwCount2 - 2; i != DWORD_MAX; --i)
    {
        hr = EnumFindValueInHistory(pcesInEnum1, dwCount1, pcesInEnum2->valueHistory.rgcValues + i, &dwFoundIndex);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
            continue;
        }
        ExitOnFailure(hr, "Failed to search in enum 1");

        dwStartCopyIndex = i + 1;
        break;
    }

    // Now create pcehOutHandle1 with all the members of pcesInEnum1 after index dwFoundIndex
    hr = EnumCopy(pcesInEnum2, dwCount2, dwStartCopyIndex, reinterpret_cast<CFG_ENUMERATION **>(pcehOutHandle2), pdwOutCount2);
    ExitOnFailure(hr, "Failed to copy enumeration struct 2");

LExit:
    if (FAILED(hr))
    {
        EnumFree(static_cast<CFG_ENUMERATION *>(*pcehOutHandle1));
        *pcehOutHandle1 = NULL;
        EnumFree(static_cast<CFG_ENUMERATION *>(*pcehOutHandle2));
        *pcehOutHandle2 = NULL;
    }

    return hr;
}
