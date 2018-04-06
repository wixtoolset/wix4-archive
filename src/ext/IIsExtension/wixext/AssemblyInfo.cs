// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using WixToolset.Extensions;
using WixToolset.Tools;

[assembly: AssemblyTitle("WiX Toolset IIS Extension")]
[assembly: AssemblyDescription("WiX Toolset Internet Information Services Extension")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyDefaultHeatExtension(typeof(IIsHeatExtension))]
