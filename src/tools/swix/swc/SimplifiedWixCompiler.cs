// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Command-line executable and MSBuild Task that calls the WiX toolset simplified compiler.
    /// </summary>
    public class SimplifiedWixCompiler : Task
    {
        public string Architecture { get; set; }

        public ITaskItem[] Extensions { get; set; }

        public string[] Languages { get; set; }

        public ITaskItem[] References { get; set; }

        [Output]
        public ITaskItem OutputPath { get; set; }

        public string[] PreprocessorDefines { get; set; }

        public ITaskItem[] SearchPaths { get; set; }

        [Required]
        public ITaskItem[] SourcePaths { get; set; }

        public string Type { get; set; }

        /// <summary>
        /// Entry point when called from command-line.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>0 on success, non-zero on failure.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            Messaging messaging = new Messaging();
            CommandLine commandline;

            if (!CommandLine.TryParseArguments(args, messaging, out commandline))
            {
                return 1;
            }

            if (commandline.Help)
            {
                SimplifiedWixCompiler.ShowLogo();
                SimplifiedWixCompiler.ShowHelp();
                return 0;
            }

            if (!commandline.NoLogo)
            {
                SimplifiedWixCompiler.ShowLogo();
            }

            SimplifiedWixCompiler self = new SimplifiedWixCompiler();
            self.Architecture = commandline.Architecture;
            self.Extensions = commandline.Extensions.ToArray();
            self.Languages = commandline.Languages.ToArray();
            self.Type = commandline.Type;
            self.PreprocessorDefines = commandline.PreprocessorDefines.ToArray();
            self.SourcePaths = commandline.Files.ToArray();
            self.SearchPaths = commandline.SearchPaths.ToArray();
            self.OutputPath = commandline.Output;

            return self.Run(messaging) ? 0 : -1;
        }

        /// <summary>
        /// Entry point when called from MSBuild.
        /// </summary>
        /// <returns>True if successfully executed.</returns>
        public override bool Execute()
        {
            Messaging messaging = new Messaging(this);
            return this.Run(messaging);
        }

        private bool Run(Messaging messaging)
        {
            if (this.SourcePaths.Length == 0)
            {
                messaging.OnError(null, "No inputs specified. Specify at least one file.");
            }

            if (String.IsNullOrEmpty(this.Type))
            {
                if (this.OutputPath == null)
                {
                    messaging.OnError(this, "Package type cannot be inferred from output path. Explicitly specify PackageType in your MSBuild project or -type from the swc.exe command-line. Valid options are: appx, msi, nuget, vsix, or wixlib");
                }
                else
                {
                    this.Type = Path.GetExtension(this.OutputPath.ItemSpec);
                }
            }
            else if (this.OutputPath == null && this.SourcePaths.Length > 0)
            {
                string outputPath = Path.ChangeExtension(this.SourcePaths[0].ItemSpec, this.Type.ToLowerInvariant());
                this.OutputPath = new FilePathTaskItem(outputPath);
            }

            PackageArchitecture architecture = PackageArchitecture.Unknown;
            if (String.IsNullOrEmpty(this.Architecture))
            {
                messaging.OnError(this, "A package architecture must specified. Set the PackageArchitecture in your MSBuild project or -arch from the swc.exe command-line. Valid options are: arm, x64, x86 or neutral");
            }
            else if (!SimplifiedWixCompiler.TryConvertPackageArchitecture(this.Architecture, out architecture))
            {
                messaging.OnError(this, "Unknown architecture specified: {0}. Valid options are: arm, x64, x86 or neutral", this.Architecture);
            }

            List<CultureInfo> locales = new List<CultureInfo>();
            if (this.Languages != null)
            {
                foreach (string language in this.Languages)
                {
                    try
                    {
                        CultureInfo locale;

                        int lcid = 0;
                        if (Int32.TryParse(language, out lcid))
                        {
                            locale = new CultureInfo(lcid);
                        }
                        else
                        {
                            locale = new CultureInfo(language);
                        }

                        locales.Add(locale);
                    }
                    catch (CultureNotFoundException)
                    {
                        messaging.OnError(this, "Unknown language: {0}", language);
                    }
                }
            }

            if (String.IsNullOrEmpty(this.Type))
            {
                messaging.OnError(this, "A package type must specified. Use the PackageType in your MSBuild project or -type from the swc.exe command-line. Valid options are: appx, msi, nuget, vsix or wixlib");
            }

            if (!messaging.Errored)
            {
                SimplifiedCompiler compiler = new SimplifiedCompiler();
                compiler.Messages += messaging.MessageDelegate;

                if (this.SearchPaths != null)
                {
                    foreach (ITaskItem searchPath in this.SearchPaths)
                    {
                        compiler.SearchPaths.Add(searchPath.ItemSpec);
                    }
                }

                if (this.PreprocessorDefines != null)
                {
                    foreach (string define in this.PreprocessorDefines)
                    {
                        string[] defineSplit = define.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        compiler.PreprocessorDefines.Add(defineSplit[0], defineSplit.Length > 1 ? defineSplit[1] : null);
                    }
                }

                // Load the extensions.
                if (this.Extensions != null)
                {
                    SimplifiedWixCompiler.LoadExtensions(compiler, messaging, this.Extensions);
                }

                CompilerExtension outputExtension = compiler.Extensions.FirstOrDefault(e => e.HasBackendCompiler(this.Type));
                if (outputExtension != null)
                {
                    if (!messaging.Errored)
                    {
                        SimplifiedWixCompiler.LoadPackageSources(compiler, this.SourcePaths);
                        compiler.Compile(outputExtension, architecture, locales.ToArray(), this.OutputPath.ItemSpec);
                    }
                }
                else
                {
                    PackageType type = PackageType.Unknown;
                    if (!SimplifiedWixCompiler.TryConvertPackageType(this.Type, out type))
                    {
                        messaging.OnError(this, "Unknown package type specified: {0}. Valid options are: appx, msi, nuget, vsix or wixlib", this.Type);
                    }

                    if (type == PackageType.Appx && locales.Count == 0)
                    {
                        messaging.OnError(this, "AppX packages do not support language neutral packages. At least one language to be specified. Use the PackageLanguages property in your MSBuild project or -lang from the swc.exe command-line.");
                    }

                    // Finally, load the sources and compile!
                    if (!messaging.Errored)
                    {
                        SimplifiedWixCompiler.LoadPackageSources(compiler, this.SourcePaths);
                        compiler.Compile(type, architecture, locales.ToArray(), this.OutputPath.ItemSpec);
                    }
                }
            }

            return !messaging.Errored;
        }

        private static void ShowLogo()
        {
            Assembly executingAssembly = Assembly.GetCallingAssembly();
            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(executingAssembly.Location);

            Console.WriteLine("Simplified WiX Compiler version {0}", fileVersion.FileVersion);
            Console.WriteLine("Copyright (C) .NET Foundation and contributors. All rights reserved.");
            Console.WriteLine();
        }

        private static void ShowHelp()
        {
            //                          1         2         3         4         5         6         7         8
            //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine(" usage: swc.exe [options] sourceFile ... sourceFile");
            Console.WriteLine();
            Console.WriteLine("   -arch          architecture for package (required)");
            Console.WriteLine("                     values: neutral, arm, x86 or x64");
            Console.WriteLine("   -d name=value  define a preprocessor variable");
            Console.WriteLine("   -ext           specifies an extension to load");
            Console.WriteLine("   -lang          locale or LCID language for the package");
            Console.WriteLine("                     multiple languages are supported");
            Console.WriteLine("   -nologo        skip printing the header information");
            Console.WriteLine("   -o[ut]         specify output file (default: write to current directory)");
            Console.WriteLine("   -sp            ordered list of paths to find source files");
            Console.WriteLine("   -type          type of package to output (default: inferred from -out)");
            Console.WriteLine("                     values: appx, msi, nuget, vsix or wixlib");
            Console.WriteLine("   -? | -help     this help information");
            Console.WriteLine();
        }

        private static bool TryConvertPackageArchitecture(string architectureString, out PackageArchitecture architecture)
        {
            architecture = PackageArchitecture.Unknown;
            switch (architectureString.ToLowerInvariant())
            {
                case "arm":
                    architecture = PackageArchitecture.Arm;
                    break;

                case "amd64":
                case "x64":
                    architecture = PackageArchitecture.X64;
                    break;

                case "i386":
                case "x86":
                    architecture = PackageArchitecture.X86;
                    break;

                case "neutral":
                    architecture = PackageArchitecture.Neutral;
                    break;
            }

            return architecture != PackageArchitecture.Unknown;
        }

        private static bool TryConvertPackageType(string typeString, out PackageType type)
        {
            type = PackageType.Unknown;

            // If the looks like a file extension, skip the dot.
            if (typeString.StartsWith(".", StringComparison.Ordinal))
            {
                typeString = typeString.Substring(1);
            }

            switch (typeString.ToLowerInvariant())
            {
                case "appx":
                    type = PackageType.Appx;
                    break;

                case "nuget":
                case "nupkg":
                    type = PackageType.Nuget;
                    break;

                case "msi":
                    type = PackageType.Msi;
                    break;

                case "vsix":
                    type = PackageType.Vsix;
                    break;

                case "wixlib":
                    type = PackageType.Wixlib;
                    break;
            }

            return type != PackageType.Unknown;
        }

        private static void LoadExtensions(SimplifiedCompiler compiler, Messaging messaging, ITaskItem[] extensions)
        {
            foreach (ITaskItem extension in extensions)
            {
                string className = extension.GetMetadata("Class");
                string data = extension.GetMetadata("Data");

                // First, try the HintPath.
                string resolvedPath = extension.GetMetadata("HintPath");
                if (String.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath))
                {
                    // Try the item as a DLL.
                    resolvedPath = extension.ItemSpec;
                    if (Path.GetExtension(resolvedPath).Length == 0)
                    {
                        resolvedPath += ".dll";
                    }

                    if (!File.Exists(resolvedPath))
                    {
                        // Finally a file on disk wasn't found, so just set it to the extension name passed in.
                        resolvedPath = extension.ItemSpec;
                    }
                }

                try
                {
                    string extensionName = resolvedPath;
                    if (!String.IsNullOrEmpty(className))
                    {
                        extensionName = String.Concat(className, ", ", extensionName);
                    }

                    // Load the extension and feed it its arguments if any were provided.
                    CompilerExtension compilerExtension = CompilerExtension.Load(extensionName);
                    compilerExtension.Messages += messaging.MessageDelegate;
                    compilerExtension.Data = data;

                    // If the compiler file manager hasn't been set yet, set it to the extension's file manager.
                    if (compiler.FileManager == null)
                    {
                        compiler.FileManager = compilerExtension.FileManager;
                    }

                    compiler.AddExtension(compilerExtension);
                }
                catch (CompilerException e)
                {
                    messaging.OnError(null, "{1}", e.Message);
                }
            }
        }

        private static void LoadPackageSources(SimplifiedCompiler compiler, IEnumerable<ITaskItem> filePaths)
        {
            foreach (ITaskItem filePath in filePaths)
            {
                string fileExtension = Path.GetExtension(filePath.ItemSpec);
                if (String.Equals(".swx", fileExtension, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(".swr", fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    compiler.AddSourceFile(filePath.ItemSpec);
                }
                else
                {
                    string group = filePath.GetMetadata("Group");
                    string targetPath = filePath.GetMetadata("TargetPath");
                    compiler.AddFile(group, filePath.ItemSpec, targetPath);
                }
            }
        }
    }
}
