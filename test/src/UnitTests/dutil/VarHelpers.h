//-------------------------------------------------------------------------------------------------
// <copyright file="VarHelpers.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once


namespace DutilTests
{

void VarSetStringHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzValue);
void VarSetNumericHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LONGLONG llValue);
void VarSetVersionHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, DWORD64 qwValue);
void VarGetStringHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzExpectedValue);
void VarGetNumericHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LONGLONG llExpectedValue);
void VarGetVersionHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, DWORD64 qwExpectedValue);
void VarGetFormattedHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzExpectedValue);
void VarFormatStringHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzIn, LPCWSTR wzExpectedValue);
void VarEscapeStringHelper(LPCWSTR wzIn, LPCWSTR wzExpectedValue);
bool EvaluateConditionHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzCondition);
bool EvaluateFailureConditionHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzCondition);

}
