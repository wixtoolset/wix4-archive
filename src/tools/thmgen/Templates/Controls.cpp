// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "{2}"

static THEME_ASSIGN_CONTROL_ID vrgInitControls [] = {{
{1}
}};

HRESULT Load{0}Theme(
    __in THEME* pTheme,
    __in HWND hwndParent
    )
{{
    HRESULT hr = S_OK;

    hr = ThemeLoadControls(pTheme, hwndParent, vrgInitControls, countof(vrgInitControls));
    ExitOnFailure(hr, "Failed to load {0} theme controls.");

    ThemeGetPageIds(pTheme, vrgwzPageNames, vrgdwPageIds, countof(vrgdwPageIds));
    C_ASSERT(countof(vrgdwPageIds) == countof(vrgwzPageNames));

LExit:
    return hr;
}}

