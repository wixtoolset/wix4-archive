// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Markup;

[assembly: AssemblyTitle("Simplified WiX Toolset Core")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

// Map XML namespaces to prefixes to CLR namespaces.
[assembly: XmlnsPrefix("http://wixtoolset.org/schemas/v4/swx", "")]
[assembly: XmlnsPrefix("http://wixtoolset.org/schemas/v4/swx/appx", "appx")]
[assembly: XmlnsPrefix("http://wixtoolset.org/schemas/v4/swx/msi", "msi")]
[assembly: XmlnsPrefix("http://wixtoolset.org/schemas/v4/swx/vsix", "vsix")]
[assembly: XmlnsPrefix("http://wixtoolset.org/schemas/v4/swx/nuget", "nuget")]

[assembly: XmlnsDefinition("http://wixtoolset.org/schemas/v4/swx", "WixToolset.Simplified.Lexicon")]
[assembly: XmlnsDefinition("http://wixtoolset.org/schemas/v4/swx/appx", "WixToolset.Simplified.Lexicon.Appx")]
[assembly: XmlnsDefinition("http://wixtoolset.org/schemas/v4/swx/msi", "WixToolset.Simplified.Lexicon.Msi")]
[assembly: XmlnsDefinition("http://wixtoolset.org/schemas/v4/swx/vsix", "WixToolset.Simplified.Lexicon.Vsix")]
[assembly: XmlnsDefinition("http://wixtoolset.org/schemas/v4/swx/nuget", "WixToolset.Simplified.Lexicon.Nuget")]

// Expose our internal types to the unit test assembly.
//[assembly: InternalsVisibleTo("WixToolset.Simplified.UnitTest.Swcore")]
