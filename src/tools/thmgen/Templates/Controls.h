// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

enum {0}Controls
{{
// skip low-numbered control ids that overlap Windows controls ids like IDOK and IDCANCEL
{1}
}};

HRESULT Load{0}Theme(
    __in THEME* pTheme,
    __in HWND hwndParent
    );

