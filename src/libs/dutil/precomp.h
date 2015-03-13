#pragma once
//-------------------------------------------------------------------------------------------------
// <copyright file="precomp.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    dutil precompiled header.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0500
#endif

#ifndef _WIN32_MSI
#define _WIN32_MSI 200
#endif

#define JET_VERSION 0x0501

#include <WinSock2.h>
#include <windows.h>
#include <windowsx.h>
#include <intsafe.h>
#include <strsafe.h>
#include <wininet.h>
#include <msi.h>
#include <msiquery.h>
#include <psapi.h>
#include <shlobj.h>
#include <shlwapi.h>
#include <gdiplus.h>
#include <Tlhelp32.h>
#include <lm.h>
#include <Iads.h>
#include <activeds.h>
#include <richedit.h>
#include <stddef.h>
#include <esent.h>
#include <ahadmin.h>
#include <SRRestorePtAPI.h>
#include <userenv.h>
#include <WinIoCtl.h>
#include <wtsapi32.h>
#include <wuapi.h>
#include <commctrl.h>
#include <dbt.h>

#include "dutil.h"
#include "aclutil.h"
#include "atomutil.h"
#include "buffutil.h"
#include "butil.h"
#include "cabcutil.h"
#include "cabutil.h"
#include "conutil.h"
#include "cryputil.h"
#include "eseutil.h"
#include "dirutil.h"
#include "dlutil.h"
#include "fileutil.h"
#include "guidutil.h"
#include "gdiputil.h"
#include "dictutil.h"
#include "inetutil.h"
#include "iniutil.h"
#include "jsonutil.h"
#include "locutil.h"
#include "logutil.h"
#include "memutil.h"  // NOTE: almost everying is inlined so there is a small .cpp file
//#include "metautil.h" - see metautil.cpp why this *must* be commented out
#include "monutil.h"
#include "osutil.h"
#include "pathutil.h"
#include "perfutil.h"
#include "polcutil.h"
#include "procutil.h"
#include "regutil.h"
#include "resrutil.h"
#include "reswutil.h"
#include "rmutil.h"
#include "rssutil.h"
#include "apuputil.h" // NOTE: this must come after atomutil.h and rssutil.h since it uses them.
#include "shelutil.h"
//#include "sqlutil.h" - see sqlutil.cpp why this *must* be commented out
#include "srputil.h"
#include "strutil.h"
#include "timeutil.h"
#include "timeutil.h"
#include "thmutil.h"
#include "uncutil.h"
#include "uriutil.h"
#include "userutil.h"
#include "varutil.h"
#include "condutil.h" // NOTE: This must come after varutil.h since it uses it.
#include "wiutil.h"
#include "wuautil.h"
#include <comutil.h>  // This header is needed for msxml2.h to compile correctly
#include <msxml2.h>   // This file is needed to include xmlutil.h
#include "xmlutil.h"

