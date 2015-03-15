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

void VarSetStringTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzValue);
void VarSetNumericTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LONGLONG llValue);
void VarSetVersionTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, DWORD64 qwValue);
void VarGetStringTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzExpectedValue);
void VarGetNumericTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LONGLONG llExpectedValue);
void VarGetVersionTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, DWORD64 qwExpectedValue);
void VarGetFormattedTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzExpectedValue);
void VarFormatStringTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzIn, LPCWSTR wzExpectedValue);
void VarEscapeStringTestHelper(LPCWSTR wzIn, LPCWSTR wzExpectedValue);
bool EvaluateConditionTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzCondition);
bool EvaluateFailureConditionTestHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzCondition);

}
