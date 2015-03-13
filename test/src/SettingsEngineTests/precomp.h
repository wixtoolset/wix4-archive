//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Precompiled header for Cfg API tests.
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#include <windows.h>
#include <strsafe.h>
#include <ShlObj.h>

#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>

// Include error.h before dutil.h
#include "error.h"
#include <dutil.h>

#include <memutil.h>
#include <dirutil.h>
#include <dictutil.h>
#include <fileutil.h>
#include <iniutil.h>
#include <pathutil.h>
#include <regutil.h>
#include <strutil.h>

#include "testhook.h"
#include "cfgadmin.h"
#include "cfgapi.h"
#include "cfgleg.h"
#include "cfgrmote.h"

#include "SettingsEngineTest.h"

#pragma managed
#include <vcclr.h>
