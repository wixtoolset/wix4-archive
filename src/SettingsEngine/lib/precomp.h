#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#define ExitTrace LogErrorString

#include <windows.h>
#include <stdlib.h>
#include <stddef.h>
#include <strsafe.h>
#include <intsafe.h>
#include <shlobj.h>
#include <sqlce_oledb.h>
#include <Shlwapi.h>

#include "dutil.h"
#include "aclutil.h"
#include "cabcutil.h"
#include "cabutil.h"
#include "cryputil.h"
#include "dictutil.h"
#include "dirutil.h"
#include "pathutil.h"
#include "fileutil.h"
#include "guidutil.h"
#include "iniutil.h"
#include "logutil.h"
#include "memutil.h"
#include "monutil.h"
#include "osutil.h"
#include "regutil.h"
#include "strutil.h"
#include "sceutil.h"
#include "timeutil.h"
#include "uncutil.h"
#include "xmlutil.h"

#include "inc\cfgapi.h"
#include "inc\cfgleg.h"
#include "inc\cfgadmin.h"
#include "inc\cfgrmote.h"

#include "database.h"
#include "testhook.h"
#include "handle.h"
#include "dblist.h"
#include "dispname.h"
#include "manifest.h"
#include "parse.h"
#include "mapdata.h"
#include "drspcial.h"
#include "product.h"
#include "detect.h"
#include "filter.h"
#include "util.h"
#include "value.h"
#include "enum.h"
#include "compress.h"
#include "stream.h"
#include "inifile.h"
#include "legsync.h"
#include "conflict.h"
#include "drdfault.h"
#include "rgdfault.h"
#include "rgspcial.h"
#include "backgrnd.h"
#include "guidlist.h"
