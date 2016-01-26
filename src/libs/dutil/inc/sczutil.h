#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="sczutil.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Header for string RAII class.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifdef __cplusplus
class PSCZ
{
public:
    PSCZ() : m_scz(NULL) { }

    ~PSCZ() { ReleaseNullStr(m_scz); }

    operator LPWSTR() { return m_scz; }

    operator LPCWSTR() { return m_scz; }

    operator bool() { return NULL != m_scz; }

    LPWSTR* operator &() { return &m_scz; }

    bool operator !() { return !m_scz; }

    WCHAR operator *() { return *m_scz; }

    LPWSTR Detach() { LPWSTR scz = m_scz; m_scz = NULL; return scz; }

private:
    LPWSTR m_scz;
};
#endif  //__cplusplus
