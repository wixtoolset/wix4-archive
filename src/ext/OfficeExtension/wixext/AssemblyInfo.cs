//-------------------------------------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The assembly information for the WiX Toolset Office Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

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
