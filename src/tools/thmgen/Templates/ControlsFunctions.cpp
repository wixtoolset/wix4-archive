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

