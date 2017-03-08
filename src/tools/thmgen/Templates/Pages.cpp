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

