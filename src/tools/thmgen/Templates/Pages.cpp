// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

static LPCWSTR vrgwzPageNames[] = {{
{1}
}};

static DWORD vrgdwPageIds[countof(vrgwzPageNames)] = {{ }};

void Show{0}Page(
    __in THEME* pTheme,
    __in Pages page
    )
{{
    if (pTheme->dwCurrentPageId)
    {{
        ThemeShowPage(pTheme, pTheme->dwCurrentPageId, SW_HIDE);
    }}

    ThemeShowPage(pTheme, vrgdwPageIds[page], SW_SHOW);
}}

