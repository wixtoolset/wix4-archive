//-------------------------------------------------------------------------------------------------
// <copyright file="condutilhelpers.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

//
// parse rules
//
// value                variable | literal | integer | version
// comparison-operator  < | > | <= | >= | = | <> | >< | << | >>
// term                 value | value comparison-operator value | ( expression )
// boolean-factor       term | NOT term
// boolean-term         boolean-factor | boolean-factor AND boolean-term
// expression           boolean-term | boolean-term OR expression
//


// constants

#define COMPARISON  0x00010000
#define INSENSITIVE 0x00020000

enum CONDUTIL_SYMBOL_TYPE
{
    // terminals
    CONDUTIL_SYMBOL_TYPE_NONE       =  0,
    CONDUTIL_SYMBOL_TYPE_END        =  1,
    CONDUTIL_SYMBOL_TYPE_OR         =  2,                               // OR
    CONDUTIL_SYMBOL_TYPE_AND        =  3,                               // AND
    CONDUTIL_SYMBOL_TYPE_NOT        =  4,                               // NOT
    CONDUTIL_SYMBOL_TYPE_LT         =  5 | COMPARISON,                  // <
    CONDUTIL_SYMBOL_TYPE_GT         =  6 | COMPARISON,                  // >
    CONDUTIL_SYMBOL_TYPE_LE         =  7 | COMPARISON,                  // <=
    CONDUTIL_SYMBOL_TYPE_GE         =  8 | COMPARISON,                  // >=
    CONDUTIL_SYMBOL_TYPE_EQ         =  9 | COMPARISON,                  // =
    CONDUTIL_SYMBOL_TYPE_NE         = 10 | COMPARISON,                  // <>
    CONDUTIL_SYMBOL_TYPE_BAND       = 11 | COMPARISON,                  // ><
    CONDUTIL_SYMBOL_TYPE_HIEQ       = 12 | COMPARISON,                  // <<
    CONDUTIL_SYMBOL_TYPE_LOEQ       = 13 | COMPARISON,                  // >>
    CONDUTIL_SYMBOL_TYPE_LT_I       =  5 | COMPARISON | INSENSITIVE,    // ~<
    CONDUTIL_SYMBOL_TYPE_GT_I       =  6 | COMPARISON | INSENSITIVE,    // ~>
    CONDUTIL_SYMBOL_TYPE_LE_I       =  7 | COMPARISON | INSENSITIVE,    // ~<=
    CONDUTIL_SYMBOL_TYPE_GE_I       =  8 | COMPARISON | INSENSITIVE,    // ~>=
    CONDUTIL_SYMBOL_TYPE_EQ_I       =  9 | COMPARISON | INSENSITIVE,    // ~=
    CONDUTIL_SYMBOL_TYPE_NE_I       = 10 | COMPARISON | INSENSITIVE,    // ~<>
    CONDUTIL_SYMBOL_TYPE_BAND_I     = 11 | COMPARISON | INSENSITIVE,    // ~><
    CONDUTIL_SYMBOL_TYPE_HIEQ_I     = 12 | COMPARISON | INSENSITIVE,    // ~<<
    CONDUTIL_SYMBOL_TYPE_LOEQ_I     = 13 | COMPARISON | INSENSITIVE,    // ~>>
    CONDUTIL_SYMBOL_TYPE_LPAREN     = 14,                               // (
    CONDUTIL_SYMBOL_TYPE_RPAREN     = 15,                               // )
    CONDUTIL_SYMBOL_TYPE_NUMBER     = 16,
    CONDUTIL_SYMBOL_TYPE_IDENTIFIER = 17,
    CONDUTIL_SYMBOL_TYPE_LITERAL    = 18,
    CONDUTIL_SYMBOL_TYPE_VERSION    = 19,
};


// structs

typedef struct _CondMockableFunctions
{
} CondMockableFunctions;

struct CONDUTIL_SYMBOL
{
    CONDUTIL_SYMBOL_TYPE Type;
    DWORD iPosition;
    VRNTUTIL_VARIANT_HANDLE Value;
};

struct CONDUTIL_PARSE_CONTEXT
{
    VARIABLES_HANDLE pVariables;
    LPCWSTR wzCondition;
    LPCWSTR wzRead;
    CONDUTIL_SYMBOL NextSymbol;
    BOOL fError;
};


// function declarations

static HRESULT CondEvaluateHelper(
    __in CondMockableFunctions* pFunctions,
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    );


// internal function declarations

static HRESULT ParseExpression(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    );
static HRESULT ParseBooleanTerm(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    );
static HRESULT ParseBooleanFactor(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    );
static HRESULT ParseTerm(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    );
static HRESULT ParseValue(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out VRNTUTIL_VARIANT_HANDLE pValue
    );
static HRESULT Expect(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __in CONDUTIL_SYMBOL_TYPE symbolType
    );
static HRESULT NextSymbol(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext
    );
static HRESULT CompareValues(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_SYMBOL_TYPE comparison,
    __in VRNTUTIL_VARIANT_HANDLE leftOperand,
    __in VRNTUTIL_VARIANT_HANDLE rightOperand,
    __out BOOL* pfResult
    );
static HRESULT CompareStringValues(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_SYMBOL_TYPE comparison,
    __in_z LPCWSTR wzLeftOperand,
    __in_z LPCWSTR wzRightOperand,
    __out BOOL* pfResult
    );
static HRESULT CompareIntegerValues(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_SYMBOL_TYPE comparison,
    __in LONGLONG llLeftOperand,
    __in LONGLONG llRightOperand,
    __out BOOL* pfResult
    );
static HRESULT CompareVersionValues(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_SYMBOL_TYPE comparison,
    __in DWORD64 qwLeftOperand,
    __in DWORD64 qwRightOperand,
    __out BOOL* pfResult
    );


// function definitions

static HRESULT CondEvaluateHelper(
    __in CondMockableFunctions* pFunctions,
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    CONDUTIL_PARSE_CONTEXT context = { };
    BOOL f = FALSE;

    context.pVariables = pVariables;
    context.wzCondition = wzCondition;
    context.wzRead = wzCondition;

    hr = NextSymbol(pFunctions, &context);
    ExitOnFailure(hr, "Failed to read next symbol.");

    hr = ParseExpression(pFunctions, &context, &f);
    ExitOnFailure(hr, "Failed to parse expression.");

    hr = Expect(pFunctions, &context, CONDUTIL_SYMBOL_TYPE_END);
    ExitOnFailure(hr, "Failed to expect end symbol.");

    //LogId(REPORT_VERBOSE, MSG_CONDITION_RESULT, wzCondition, LoggingTrueFalseToString(f));
    LogStringLine(REPORT_VERBOSE, "Condition '%ls' evaluates to %hs.", wzCondition, LogTrueFalseToString(f));

    *pf = f;
    hr = S_OK;

LExit:
    if (context.fError)
    {
        Assert(FAILED(hr));
        //LogErrorId(hr, MSG_FAILED_PARSE_CONDITION, wzCondition, NULL, NULL);
        LogErrorString(hr, "Failed to parse condition %ls.", wzCondition);
    }

    return hr;
}


// internal function definitions

/********************************************************************
ParseExpression - parses an expression.

********************************************************************/
static HRESULT ParseExpression(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    BOOL fFirst = FALSE;
    BOOL fSecond = FALSE;

    hr = ParseBooleanTerm(pFunctions, pContext, &fFirst);
    ExitOnFailure(hr, "Failed to parse boolean-term.");

    if (CONDUTIL_SYMBOL_TYPE_OR == pContext->NextSymbol.Type)
    {
        hr = NextSymbol(pFunctions, pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        hr = ParseExpression(pFunctions, pContext, &fSecond);
        ExitOnFailure(hr, "Failed to parse expression.");

        *pf = fFirst || fSecond;
    }
    else
    {
        *pf = fFirst;
    }

LExit:
    return hr;
}

/********************************************************************
ParseBooleanTerm - parses a boolean term.

********************************************************************/
static HRESULT ParseBooleanTerm(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    BOOL fFirst = FALSE;
    BOOL fSecond = FALSE;

    hr = ParseBooleanFactor(pFunctions, pContext, &fFirst);
    ExitOnFailure(hr, "Failed to parse boolean-factor.");

    if (CONDUTIL_SYMBOL_TYPE_AND == pContext->NextSymbol.Type)
    {
        hr = NextSymbol(pFunctions, pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        hr = ParseBooleanTerm(pFunctions, pContext, &fSecond);
        ExitOnFailure(hr, "Failed to parse boolean-term.");

        *pf = fFirst && fSecond;
    }
    else
    {
        *pf = fFirst;
    }

LExit:
    return hr;
}

/********************************************************************
ParseBooleanFactor - parses a boolean factor.

********************************************************************/
static HRESULT ParseBooleanFactor(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    BOOL fNot = FALSE;
    BOOL f = FALSE;

    if (CONDUTIL_SYMBOL_TYPE_NOT == pContext->NextSymbol.Type)
    {
        hr = NextSymbol(pFunctions, pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        fNot = TRUE;
    }

    hr = ParseTerm(pFunctions, pContext, &f);
    ExitOnFailure(hr, "Failed to parse term.");

    *pf = fNot ? !f : f;

LExit:
    return hr;
}

/********************************************************************
ParseTerm - parses a term.

********************************************************************/
static HRESULT ParseTerm(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_HANDLE firstValue = NULL;
    VRNTUTIL_VARIANT_HANDLE secondValue = NULL;
    VRNTUTIL_VARIANT_TYPE variantType = VRNTUTIL_VARIANT_TYPE_NONE;
    LONGLONG llValue = 0;
    LPWSTR sczValue = NULL;
    DWORD64 qwValue = 0;

    firstValue = MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, TRUE);
    ExitOnNull(firstValue, hr, E_OUTOFMEMORY, "Failed to alloc memory for firstValue.");

    secondValue = MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, TRUE);
    ExitOnNull(secondValue, hr, E_OUTOFMEMORY, "Failed to alloc memory for secondValue.");

    if (CONDUTIL_SYMBOL_TYPE_LPAREN == pContext->NextSymbol.Type)
    {
        hr = NextSymbol(pFunctions, pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        hr = ParseExpression(pFunctions, pContext, pf);
        ExitOnFailure(hr, "Failed to parse expression.");

        hr = Expect(pFunctions, pContext, CONDUTIL_SYMBOL_TYPE_RPAREN);
        ExitOnFailure(hr, "Failed to expect right parenthesis.");

        ExitFunction1(hr = S_OK);
    }

    hr = ParseValue(pFunctions, pContext, firstValue);
    ExitOnFailure(hr, "Failed to parse value.");

    if (COMPARISON & pContext->NextSymbol.Type)
    {
        CONDUTIL_SYMBOL_TYPE comparison = pContext->NextSymbol.Type;

        hr = NextSymbol(pFunctions, pContext);
        ExitOnFailure(hr, "Failed to read next symbol.");

        hr = ParseValue(pFunctions, pContext, secondValue);
        ExitOnFailure(hr, "Failed to parse value.");

        hr = CompareValues(pFunctions, comparison, firstValue, secondValue, pf);
        ExitOnFailure(hr, "Failed to compare value.");
    }
    else
    {
        hr = VrntGetType(firstValue, &variantType);
        ExitOnFailure(hr, "Failed to get the type of firstValue.");

        switch (variantType)
        {
        case VRNTUTIL_VARIANT_TYPE_NONE:
            *pf = FALSE;
            break;
        case VRNTUTIL_VARIANT_TYPE_STRING:
            hr = VrntGetString(firstValue, &sczValue);
            if (SUCCEEDED(hr))
            {
                *pf = sczValue && *sczValue;
            }
            break;
        case VRNTUTIL_VARIANT_TYPE_NUMERIC:
            hr = VrntGetNumeric(firstValue, &llValue);
            if (SUCCEEDED(hr))
            {
                *pf = 0 != llValue;
            }
            break;
        case VRNTUTIL_VARIANT_TYPE_VERSION:
            hr = VrntGetVersion(firstValue, &qwValue);
            if (SUCCEEDED(hr))
            {
                *pf = 0 != qwValue;
            }
            break;
        default:
            ExitFunction1(hr = E_UNEXPECTED);
        }
    }

LExit:
    if (firstValue)
    {
        VrntUninitialize(firstValue);
        ReleaseMem(firstValue);
    }

    if (secondValue)
    {
        VrntUninitialize(secondValue);
        ReleaseMem(secondValue);
    }

    SecureZeroMemory(&llValue, sizeof(llValue));
    SecureZeroMemory(&qwValue, sizeof(qwValue));
    StrSecureZeroFreeString(sczValue);

    return hr;
}

/********************************************************************
ParseValue - parses the value of the current symbol into pValue,
             then gets the next symbol.

********************************************************************/
static HRESULT ParseValue(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __out VRNTUTIL_VARIANT_HANDLE pValue
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_TYPE variantType = VRNTUTIL_VARIANT_TYPE_NONE;
    LPWSTR scz = NULL;

    switch (pContext->NextSymbol.Type)
    {
    case CONDUTIL_SYMBOL_TYPE_IDENTIFIER:
        hr = VrntGetType(pContext->NextSymbol.Value, &variantType);
        ExitOnFailure(hr, "Failed to get the symbol value's type.");

        Assert(VRNTUTIL_VARIANT_TYPE_STRING == variantType);

        hr = VrntGetString(pContext->NextSymbol.Value, &scz);
        ExitOnFailure(hr, "Failed to get the symbol's value.");

        // Find variable.
        hr = VarGetVariant(pContext->pVariables, scz, pValue);
        if (E_NOTFOUND != hr)
        {
            ExitOnRootFailure(hr, "Failed to get variable.");
        }

        break;

    case CONDUTIL_SYMBOL_TYPE_NUMBER: __fallthrough;
    case CONDUTIL_SYMBOL_TYPE_LITERAL: __fallthrough;
    case CONDUTIL_SYMBOL_TYPE_VERSION:
        hr = VrntCopy(pContext->NextSymbol.Value, pValue);
        ExitOnFailure(hr, "Failed to copy symbol's value.");

        VrntUninitialize(pContext->NextSymbol.Value);

        break;

    default:
        pContext->fError = TRUE;
        hr = E_INVALIDDATA;
        ExitOnRootFailure(hr, "Failed to parse condition '%ls' at position: %u", pContext->wzCondition, pContext->NextSymbol.iPosition);
    }

    // Get next symbol.
    hr = NextSymbol(pFunctions, pContext);
    ExitOnFailure(hr, "Failed to read next symbol.");

LExit:
    ReleaseStr(scz);

    return hr;
}

/********************************************************************
Expect - expects a symbol.

********************************************************************/
static HRESULT Expect(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_PARSE_CONTEXT* pContext,
    __in CONDUTIL_SYMBOL_TYPE symbolType
    )
{
    HRESULT hr = S_OK;

    if (pContext->NextSymbol.Type != symbolType)
    {
        pContext->fError = TRUE;
        hr = E_INVALIDDATA;
        ExitOnRootFailure(hr, "Failed to parse condition '%ls' at position: %u", pContext->wzCondition, pContext->NextSymbol.iPosition);
    }

    hr = NextSymbol(pFunctions, pContext);
    ExitOnFailure(hr, "Failed to read next symbol.");

LExit:
    return hr;
}

/********************************************************************
NextSymbol - finds the next symbol in an expression string.

********************************************************************/
static HRESULT NextSymbol(
    __in CondMockableFunctions* /*pFunctions*/,
    __in CONDUTIL_PARSE_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_HANDLE pVariant = NULL;
    WORD charType = 0;
    DWORD iPosition = 0;
    DWORD n = 0;
    DWORD64 qwValue = 0;

    // Free existing symbol.
    pVariant = pContext->NextSymbol.Value;
    if (!pVariant)
    {
        pVariant = MemAlloc(VRNTUTIL_VARIANT_HANDLE_BYTES, TRUE);
        ExitOnNull(pVariant, hr, E_OUTOFMEMORY, "Failed to allocate memory for NextSymbol's value.");
    }
    else
    {
        VrntUninitialize(pVariant);
    }
    memset(&pContext->NextSymbol, 0, sizeof(CONDUTIL_SYMBOL));
    pContext->NextSymbol.Value = pVariant;

    // Skip past blanks.
    while (L'\0' != pContext->wzRead[0])
    {
        ::GetStringTypeW(CT_CTYPE1, pContext->wzRead, 1, &charType);
        if (0 == (C1_BLANK & charType))
        {
            break; // no blank, done.
        }
        ++pContext->wzRead;
    }
    iPosition = (DWORD)(pContext->wzRead - pContext->wzCondition);

    // Read depending on first character type.
    switch (pContext->wzRead[0])
    {
    case L'\0':
        pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_END;
        break;
    case L'~':
        switch (pContext->wzRead[1])
        {
        case L'=':
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_EQ_I;
            n = 2;
            break;
        case L'>':
            switch (pContext->wzRead[2])
            {
            case '=':
                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_GE_I;
                n = 3;
                break;
            case L'>':
                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_LOEQ_I;
                n = 3;
                break;
            case L'<':
                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_BAND_I;
                n = 3;
                break;
            default:
                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_GT_I;
                n = 2;
            }
            break;
        case L'<':
            switch (pContext->wzRead[2])
            {
            case '=':
                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_LE_I;
                n = 3;
                break;
            case L'<':
                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_HIEQ_I;
                n = 3;
                break;
            case '>':
                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_NE_I;
                n = 3;
                break;
            default:
                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_LT_I;
                n = 2;
            }
            break;
        default: // error
            pContext->fError = TRUE;
            hr = E_INVALIDDATA;
            ExitOnRootFailure(hr, "Failed to parse condition \"%ls\". Unexpected '~' operator at position %d.", pContext->wzCondition, iPosition);
        }
        break;
    case L'>':
        switch (pContext->wzRead[1])
        {
        case L'=':
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_GE;
            n = 2;
            break;
        case L'>':
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_LOEQ;
            n = 2;
            break;
        case L'<':
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_BAND;
            n = 2;
            break;
        default:
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_GT;
            n = 1;
        }
        break;
    case L'<':
        switch (pContext->wzRead[1])
        {
        case L'=':
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_LE;
            n = 2;
            break;
        case L'<':
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_HIEQ;
            n = 2;
            break;
        case L'>':
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_NE;
            n = 2;
            break;
        default:
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_LT;
            n = 1;
        }
        break;
    case L'=':
        pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_EQ;
        n = 1;
        break;
    case L'(':
        pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_LPAREN;
        n = 1;
        break;
    case L')':
        pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_RPAREN;
        n = 1;
        break;
    case L'"': // literal
        do
        {
            ++n;
            if (L'\0' == pContext->wzRead[n])
            {
                // error
                pContext->fError = TRUE;
                hr = E_INVALIDDATA;
                ExitOnRootFailure(hr, "Failed to parse condition \"%ls\". Unterminated literal at position %d.", pContext->wzCondition, iPosition);
            }
        } while (L'"' != pContext->wzRead[n]);
        ++n; // terminating '"'

        pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_LITERAL;
        hr = VrntSetString(pContext->NextSymbol.Value, &pContext->wzRead[1], n - 2);
        ExitOnFailure(hr, "Failed to set symbol value.");
        break;
    default:
        if (C1_DIGIT & charType || L'-' == pContext->wzRead[0])
        {
            do
            {
                ++n;
                ::GetStringTypeW(CT_CTYPE1, &pContext->wzRead[n], 1, &charType);
                if (C1_ALPHA & charType || L'_' == pContext->wzRead[n])
                {
                    // Error, identifier cannot start with a digit.
                    pContext->fError = TRUE;
                    hr = E_INVALIDDATA;
                    ExitOnRootFailure(hr, "Failed to parse condition \"%ls\". Identifier cannot start at a digit, at position %d.", pContext->wzCondition, iPosition);
                }
            } while (C1_DIGIT & charType);

            // number
            pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_NUMBER;

            LONGLONG ll = 0;
            hr = StrStringToInt64(pContext->wzRead, n, &ll);
            if (FAILED(hr))
            {
                pContext->fError = TRUE;
                hr = E_INVALIDDATA;
                ExitOnRootFailure(hr, "Failed to parse condition \"%ls\". Constant too big, at position %d.", pContext->wzCondition, iPosition);
            }

            hr = VrntSetNumeric(pContext->NextSymbol.Value, ll);
            ExitOnFailure(hr, "Failed to set symbol value.");
        }
        else if (C1_ALPHA & charType || L'_' == pContext->wzRead[0])
        {
            ::GetStringTypeW(CT_CTYPE1, &pContext->wzRead[1], 1, &charType);
            if (L'v' == pContext->wzRead[0] && C1_DIGIT & charType)
            {
                // version
                DWORD cParts = 1;
                for (;;)
                {
                    ++n;
                    if (L'.' == pContext->wzRead[n])
                    {
                        ++cParts;
                        if (4 < cParts)
                        {
                            // Error, too many parts in version.
                            pContext->fError = TRUE;
                            hr = E_INVALIDDATA;
                            ExitOnRootFailure(hr, "Failed to parse condition \"%ls\". Version can have a maximum of 4 parts, at position %d.", pContext->wzCondition, iPosition);
                        }
                    }
                    else
                    {
                        ::GetStringTypeW(CT_CTYPE1, &pContext->wzRead[n], 1, &charType);
                        if (C1_DIGIT != (C1_DIGIT & charType))
                        {
                            break;
                        }
                    }
                }

                hr = FileVersionFromStringEx(&pContext->wzRead[1], n - 1, &qwValue);
                if (FAILED(hr))
                {
                    pContext->fError = TRUE;
                    hr = E_INVALIDDATA;
                    ExitOnRootFailure(hr, "Failed to parse condition \"%ls\". Invalid version format, at position %d.", pContext->wzCondition, iPosition);
                }

                hr = VrntSetVersion(pContext->NextSymbol.Value, qwValue);
                ExitOnFailure(hr, "Failed to set version variant.");

                pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_VERSION;
            }
            else
            {
                do
                {
                    ++n;
                    ::GetStringTypeW(CT_CTYPE1, &pContext->wzRead[n], 1, &charType);
                } while (C1_ALPHA & charType || C1_DIGIT & charType || L'_' == pContext->wzRead[n]);

                if (2 == n && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->wzRead, 2, L"OR", 2))
                {
                    // OR
                    pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_OR;
                }
                else if (3 == n && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->wzRead, 3, L"AND", 3))
                {
                    // AND
                    pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_AND;
                }
                else if (3 == n && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pContext->wzRead, 3, L"NOT", 3))
                {
                    // NOT
                    pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_NOT;
                }
                else
                {
                    // identifier
                    pContext->NextSymbol.Type = CONDUTIL_SYMBOL_TYPE_IDENTIFIER;
                    hr = VrntSetString(pContext->NextSymbol.Value, pContext->wzRead, n);
                    ExitOnFailure(hr, "Failed to set symbol value.");
                }
            }
        }
        else // error, unexpected character
        {
            pContext->fError = TRUE;
            hr = E_INVALIDDATA;
            ExitOnRootFailure(hr, "Failed to parse condition \"%ls\". Unexpected character at position %d.", pContext->wzCondition, iPosition);
        }
    }
    pContext->NextSymbol.iPosition = iPosition;
    pContext->wzRead += n;

LExit:
    return hr;
}

/********************************************************************
CompareValues - compares two variant values using a given comparison.

********************************************************************/
static HRESULT CompareValues(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_SYMBOL_TYPE comparison,
    __in VRNTUTIL_VARIANT_HANDLE leftOperand,
    __in VRNTUTIL_VARIANT_HANDLE rightOperand,
    __out BOOL* pfResult
    )
{
    HRESULT hr = S_OK;
    VRNTUTIL_VARIANT_TYPE leftType = VRNTUTIL_VARIANT_TYPE_NONE;
    VRNTUTIL_VARIANT_TYPE rightType = VRNTUTIL_VARIANT_TYPE_NONE;
    LONGLONG llLeft = 0;
    DWORD64 qwLeft = 0;
    LPWSTR sczLeft = NULL;
    LONGLONG llRight = 0;
    DWORD64 qwRight = 0;
    LPWSTR sczRight = NULL;

    hr = VrntGetType(leftOperand, &leftType);
    ExitOnFailure(hr, "Failed to get the left type.");

    hr = VrntGetType(rightOperand, &rightType);
    ExitOnFailure(hr, "Failed to get the right type.");

    // Get values to compare based on type.
    if (VRNTUTIL_VARIANT_TYPE_STRING == leftType && VRNTUTIL_VARIANT_TYPE_STRING == rightType)
    {
        hr = VrntGetString(leftOperand, &sczLeft);
        ExitOnFailure(hr, "Failed to get the left string.");

        hr = VrntGetString(rightOperand, &sczRight);
        ExitOnFailure(hr, "Failed to get the right string.");

        hr = CompareStringValues(pFunctions, comparison, sczLeft, sczRight, pfResult);
    }
    else if (VRNTUTIL_VARIANT_TYPE_NUMERIC == leftType && VRNTUTIL_VARIANT_TYPE_NUMERIC == rightType)
    {
        hr = VrntGetNumeric(leftOperand, &llLeft);
        ExitOnFailure(hr, "Failed to get the left numeric.");

        hr = VrntGetNumeric(rightOperand, &llRight);
        ExitOnFailure(hr, "Failed to get the right numeric.");

        hr = CompareIntegerValues(pFunctions, comparison, llLeft, llRight, pfResult);
    }
    else if (VRNTUTIL_VARIANT_TYPE_VERSION == leftType && VRNTUTIL_VARIANT_TYPE_VERSION == rightType)
    {
        hr = VrntGetVersion(leftOperand, &qwLeft);
        ExitOnFailure(hr, "Failed to get the left version.");

        hr = VrntGetVersion(rightOperand, &qwRight);
        ExitOnFailure(hr, "Failed to get the right version.");

        hr = CompareVersionValues(pFunctions, comparison, qwLeft, qwRight, pfResult);
    }
    else if (VRNTUTIL_VARIANT_TYPE_VERSION == leftType && VRNTUTIL_VARIANT_TYPE_STRING == rightType)
    {
        hr = VrntGetVersion(leftOperand, &qwLeft);
        ExitOnFailure(hr, "Failed to get the left version.");

        hr = VrntGetVersion(rightOperand, &qwRight);
        if (FAILED(hr))
        {
            if (DISP_E_TYPEMISMATCH != hr)
            {
                ExitOnFailure(hr, "Failed to get the right version.");
            }

            *pfResult = (CONDUTIL_SYMBOL_TYPE_NE == comparison);
            hr = S_OK;
        }
        else
        {
            hr = CompareVersionValues(pFunctions, comparison, qwLeft, qwRight, pfResult);
        }
    }
    else if (VRNTUTIL_VARIANT_TYPE_STRING == leftType && VRNTUTIL_VARIANT_TYPE_VERSION == rightType)
    {
        hr = VrntGetVersion(rightOperand, &qwRight);
        ExitOnFailure(hr, "Failed to get the right version.");

        hr = VrntGetVersion(leftOperand, &qwLeft);
        if (FAILED(hr))
        {
            if (DISP_E_TYPEMISMATCH != hr)
            {
                ExitOnFailure(hr, "Failed to get the left version.");
            }

            *pfResult = (CONDUTIL_SYMBOL_TYPE_NE == comparison);
            hr = S_OK;
        }
        else
        {
            hr = CompareVersionValues(pFunctions, comparison, qwLeft, qwRight, pfResult);
        }
    }
    else if (VRNTUTIL_VARIANT_TYPE_NUMERIC == leftType && VRNTUTIL_VARIANT_TYPE_STRING == rightType)
    {
        hr = VrntGetNumeric(leftOperand, &llLeft);
        ExitOnFailure(hr, "Failed to get the left numeric.");

        hr = VrntGetNumeric(rightOperand, &llRight);
        if (FAILED(hr))
        {
            if (DISP_E_TYPEMISMATCH != hr)
            {
                ExitOnFailure(hr, "Failed to get the right numeric.");
            }

            *pfResult = (CONDUTIL_SYMBOL_TYPE_NE == comparison);
            hr = S_OK;
        }
        else
        {
            hr = CompareIntegerValues(pFunctions, comparison, llLeft, llRight, pfResult);
        }
    }
    else if (VRNTUTIL_VARIANT_TYPE_STRING == leftType && VRNTUTIL_VARIANT_TYPE_NUMERIC == rightType)
    {
        hr = VrntGetNumeric(rightOperand, &llRight);
        ExitOnFailure(hr, "Failed to get the right numeric.");

        hr = VrntGetNumeric(leftOperand, &llLeft);
        if (FAILED(hr))
        {
            if (DISP_E_TYPEMISMATCH != hr)
            {
                ExitOnFailure(hr, "Failed to get the left numeric.");
            }

            *pfResult = (CONDUTIL_SYMBOL_TYPE_NE == comparison);
            hr = S_OK;
        }
        else
        {
            hr = CompareIntegerValues(pFunctions, comparison, llLeft, llRight, pfResult);
        }
    }
    else // not a combination that can be compared.
    {
        *pfResult = (CONDUTIL_SYMBOL_TYPE_NE == comparison);
    }

LExit:
    SecureZeroMemory(&qwLeft, sizeof(DWORD64));
    SecureZeroMemory(&llLeft, sizeof(LONGLONG));
    StrSecureZeroFreeString(sczLeft);
    SecureZeroMemory(&qwRight, sizeof(DWORD64));
    SecureZeroMemory(&llRight, sizeof(LONGLONG));
    StrSecureZeroFreeString(sczRight);

    return hr;
}

/********************************************************************
CompareStringValues - compares two string values using a given comparison.

********************************************************************/
static HRESULT CompareStringValues(
    __in CondMockableFunctions* pFunctions,
    __in CONDUTIL_SYMBOL_TYPE comparison,
    __in_z LPCWSTR wzLeftOperand,
    __in_z LPCWSTR wzRightOperand,
    __out BOOL* pfResult
    )
{
    HRESULT hr = S_OK;
    DWORD dwCompareString = (comparison & INSENSITIVE) ? NORM_IGNORECASE : 0;
    int cchLeft = lstrlenW(wzLeftOperand);
    int cchRight = lstrlenW(wzRightOperand);

    switch (comparison)
    {
    case CONDUTIL_SYMBOL_TYPE_LT:
    case CONDUTIL_SYMBOL_TYPE_GT:
    case CONDUTIL_SYMBOL_TYPE_LE:
    case CONDUTIL_SYMBOL_TYPE_GE:
    case CONDUTIL_SYMBOL_TYPE_EQ:
    case CONDUTIL_SYMBOL_TYPE_NE:
    case CONDUTIL_SYMBOL_TYPE_LT_I:
    case CONDUTIL_SYMBOL_TYPE_GT_I:
    case CONDUTIL_SYMBOL_TYPE_LE_I:
    case CONDUTIL_SYMBOL_TYPE_GE_I:
    case CONDUTIL_SYMBOL_TYPE_EQ_I:
    case CONDUTIL_SYMBOL_TYPE_NE_I:
    {
        int i = ::CompareStringW(LOCALE_INVARIANT, dwCompareString, wzLeftOperand, cchLeft, wzRightOperand, cchRight);
        hr = CompareIntegerValues(pFunctions, comparison, i, CSTR_EQUAL, pfResult);
    }
    break;
    case CONDUTIL_SYMBOL_TYPE_BAND:
    case CONDUTIL_SYMBOL_TYPE_BAND_I:
        // Test if left string contains right string.
        for (int i = 0; (i + cchRight) <= cchLeft; ++i)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, dwCompareString, wzLeftOperand + i, cchRight, wzRightOperand, cchRight))
            {
                *pfResult = TRUE;
                ExitFunction();
            }
        }
        *pfResult = FALSE;
        break;
    case CONDUTIL_SYMBOL_TYPE_HIEQ:
    case CONDUTIL_SYMBOL_TYPE_HIEQ_I:
        // Test if left string starts with right string.
        *pfResult = cchLeft >= cchRight && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, dwCompareString, wzLeftOperand, cchRight, wzRightOperand, cchRight);
        break;
    case CONDUTIL_SYMBOL_TYPE_LOEQ:
    case CONDUTIL_SYMBOL_TYPE_LOEQ_I:
        // Test if left string ends with right string.
        *pfResult = cchLeft >= cchRight && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, dwCompareString, wzLeftOperand + (cchLeft - cchRight), cchRight, wzRightOperand, cchRight);
        break;
    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}

/********************************************************************
CompareIntegerValues - compares two integer values using a given comparison.

********************************************************************/
static HRESULT CompareIntegerValues(
    __in CondMockableFunctions* /*pFunctions*/,
    __in CONDUTIL_SYMBOL_TYPE comparison,
    __in LONGLONG llLeftOperand,
    __in LONGLONG llRightOperand,
    __out BOOL* pfResult
    )
{
    HRESULT hr = S_OK;

    switch (comparison)
    {
    case CONDUTIL_SYMBOL_TYPE_LT: case CONDUTIL_SYMBOL_TYPE_LT_I: *pfResult = llLeftOperand <  llRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_GT: case CONDUTIL_SYMBOL_TYPE_GT_I: *pfResult = llLeftOperand >  llRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_LE: case CONDUTIL_SYMBOL_TYPE_LE_I: *pfResult = llLeftOperand <= llRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_GE: case CONDUTIL_SYMBOL_TYPE_GE_I: *pfResult = llLeftOperand >= llRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_EQ: case CONDUTIL_SYMBOL_TYPE_EQ_I: *pfResult = llLeftOperand == llRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_NE: case CONDUTIL_SYMBOL_TYPE_NE_I: *pfResult = llLeftOperand != llRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_BAND: case CONDUTIL_SYMBOL_TYPE_BAND_I: *pfResult = (llLeftOperand & llRightOperand) ? TRUE : FALSE; break;
    case CONDUTIL_SYMBOL_TYPE_HIEQ: case CONDUTIL_SYMBOL_TYPE_HIEQ_I: *pfResult = ((llLeftOperand >> 16) & 0xFFFF) == llRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_LOEQ: case CONDUTIL_SYMBOL_TYPE_LOEQ_I: *pfResult = (llLeftOperand & 0xFFFF) == llRightOperand; break;
    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}

/********************************************************************
CompareVersionValues - compares two quad-word version values using a given comparison.

********************************************************************/
static HRESULT CompareVersionValues(
    __in CondMockableFunctions* /*pFunctions*/,
    __in CONDUTIL_SYMBOL_TYPE comparison,
    __in DWORD64 qwLeftOperand,
    __in DWORD64 qwRightOperand,
    __out BOOL* pfResult
    )
{
    HRESULT hr = S_OK;

    switch (comparison)
    {
    case CONDUTIL_SYMBOL_TYPE_LT: *pfResult = qwLeftOperand <  qwRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_GT: *pfResult = qwLeftOperand >  qwRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_LE: *pfResult = qwLeftOperand <= qwRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_GE: *pfResult = qwLeftOperand >= qwRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_EQ: *pfResult = qwLeftOperand == qwRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_NE: *pfResult = qwLeftOperand != qwRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_BAND: *pfResult = (qwLeftOperand & qwRightOperand) ? TRUE : FALSE; break;
    case CONDUTIL_SYMBOL_TYPE_HIEQ: *pfResult = ((qwLeftOperand >> 16) & 0xFFFF) == qwRightOperand; break;
    case CONDUTIL_SYMBOL_TYPE_LOEQ: *pfResult = (qwLeftOperand & 0xFFFF) == qwRightOperand; break;
    default:
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}
