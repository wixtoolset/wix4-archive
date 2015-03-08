//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Precompiled header for DUtil integration tests.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#include <windows.h>
#include <strsafe.h>
#include <ShlObj.h>

// Include error.h before dutil.h
#include "error.h"
#include <dutil.h>

#include <dictutil.h>
#include <dirutil.h>
#include <fileutil.h>
#include <guidutil.h>
#include <iniutil.h>
#include <memutil.h>
#include <pathutil.h>
#include <strutil.h>
#include <monutil.h>
#include <regutil.h>
#include <varutil.h>
#include <condutil.h>

#include "VarHelpers.h"

#pragma managed
#include <vcclr.h>
