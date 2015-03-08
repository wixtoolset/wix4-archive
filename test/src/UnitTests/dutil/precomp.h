//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Precompiled header for DUtil unit tests.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#define WIN32_NO_STATUS
#include <windows.h>
#undef WIN32_NO_STATUS

#include <intsafe.h>
#include <ntstatus.h>
#include <strsafe.h>
#include <ShlObj.h>

#include <dutil.h>
#include <cryputil.h>
#include <fileutil.h>
#include <memutil.h>
#include <strutil.h>
#include <vrntutil.h>

#include "CrypUtilHelper.h"
#include "VrntUtilHelper.h"

#pragma managed
#include <vcclr.h>
