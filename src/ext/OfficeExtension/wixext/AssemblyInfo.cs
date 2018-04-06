// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using WixToolset;
using WixToolset.Extensions;

[assembly: AssemblyTitle("WiX Toolset Office Extension")]
[assembly: AssemblyDescription("WiX Toolset Office Extension")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(false)]
[assembly: ComVisible(false)]
[assembly: AssemblyDefaultClickThroughConsoleAttribute(typeof(OfficeAddinClickThroughConsole))]
[assembly: AssemblyDefaultClickThroughUIAttribute(typeof(OfficeAddinClickThroughUI))]
