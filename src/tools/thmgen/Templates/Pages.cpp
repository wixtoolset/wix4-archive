static LPCWSTR vrgwzPageNames[] = {{
{1}
}};

static DWORD vrgdwPageIds[countof(vrgwzPageNames)] = {{ }};
static Pages vpageCurrent = Pages::PageCount;

void Show{0}Page(
    __in THEME* pTheme,
    __in Pages page
    )
{{
    if (Pages::PageCount != vpageCurrent)
    {{
        ThemeShowPage(pTheme, vrgdwPageIds[vpageCurrent], SW_HIDE);
    }}

    vpageCurrent = page;
    ThemeShowPage(pTheme, vrgdwPageIds[page], SW_SHOW);
}}

Pages GetCurrent{0}Page()
{{
    return vpageCurrent;
}}
