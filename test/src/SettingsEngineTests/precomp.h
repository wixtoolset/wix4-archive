#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


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
