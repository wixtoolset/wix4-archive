// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT FilterCheckValue(
    __in LEGACY_PRODUCT *pProduct,
    __in_z LPCWSTR wzName,
    __out_opt BOOL *pfIgnore,
    __out_opt BOOL *pfShareWriteOnRead
    )
{
    HRESULT hr = S_OK;

    if (pfIgnore)
    {
        *pfIgnore = FALSE;
    }
    if (pfShareWriteOnRead)
    {
        *pfShareWriteOnRead = FALSE;
    }

    for (DWORD i = 0; i < pProduct->cFilters; ++i)
    {
        // If it matches an exact named filter, this is it (no checking further filters)
        if (NULL != pProduct->rgFilters[i].sczExactName && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pProduct->rgFilters[i].sczExactName, -1, wzName, -1))
        {
            if (pfIgnore)
            {
                *pfIgnore = pProduct->rgFilters[i].fIgnore;
            }
            if (pfShareWriteOnRead)
            {
                *pfShareWriteOnRead = pProduct->rgFilters[i].fShareWriteOnRead;
            }
            ExitFunction1(hr = S_OK);
        }
        // If it matches a prefix, filter it
        else if (NULL != pProduct->rgFilters[i].sczPrefix || NULL != pProduct->rgFilters[i].sczPostfix)
        {
            if (NULL != pProduct->rgFilters[i].sczPrefix && 
                (lstrlenW(wzName) < lstrlenW(pProduct->rgFilters[i].sczPrefix) || 
                CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pProduct->rgFilters[i].sczPrefix, lstrlenW(pProduct->rgFilters[i].sczPrefix), wzName, lstrlenW(pProduct->rgFilters[i].sczPrefix))))
            {
                continue;
            }
            if (NULL != pProduct->rgFilters[i].sczPostfix &&
                (lstrlenW(wzName) < lstrlenW(pProduct->rgFilters[i].sczPostfix) ||
                CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pProduct->rgFilters[i].sczPostfix, lstrlenW(pProduct->rgFilters[i].sczPostfix), wzName + lstrlenW(wzName) - lstrlenW(pProduct->rgFilters[i].sczPostfix), lstrlenW(pProduct->rgFilters[i].sczPostfix))))
            {
                continue;
            }

            if (pfIgnore)
            {
                *pfIgnore = pProduct->rgFilters[i].fIgnore;
            }
            if (pfShareWriteOnRead)
            {
                *pfShareWriteOnRead = pProduct->rgFilters[i].fShareWriteOnRead;
            }
            // Not an exact match, so keep checking further filters to allow one to override another
        }
    }
    
LExit:
    return hr;
}
