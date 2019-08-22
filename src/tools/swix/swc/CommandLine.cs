// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Command-line parsing.
    /// </summary>
    internal class CommandLine
    {
        private CommandLine()
        {
            this.Extensions = new List<FilePathTaskItem>();
            this.Files = new List<FilePathTaskItem>();
            this.Languages = new List<string>();
            this.PreprocessorDefines = new List<string>();
            this.References = new List<FilePathTaskItem>();
            this.SearchPaths = new List<FilePathTaskItem>();
        }

        /// <summary>
        /// Specifies whether to display help.
        /// </summary>
        public bool Help { get; private set; }

        /// <summary>
        /// Specifies whether to suppress logo.
        /// </summary>
        public bool NoLogo { get; private set; }

        /// <summary>
        /// Gets the list of extensions to load.
        /// </summary>
        public List<FilePathTaskItem> Extensions { get; private set; }

        /// <summary>
        /// Gets the list of files to compile.
        /// </summary>
        public List<FilePathTaskItem> Files { get; private set; }

        /// <summary>
        /// Gets the architecture to compile.
        /// </summary>
        public string Architecture { get; private set; }

        /// <summary>
        /// Gets the languages to compile.
        /// </summary>
        public List<string> Languages { get; private set; }

        /// <summary>
        /// Gets the output path.
        /// </summary>
        public FilePathTaskItem Output { get; private set; }

        /// <summary>
        /// Gets the preprocessor defines.
        /// </summary>
        public List<string> PreprocessorDefines { get; private set; }

        /// <summary>
        /// Reference files.
        /// </summary>
        public List<FilePathTaskItem> References { get; private set; }

        /// <summary>
        /// Gets the search paths.
        /// </summary>
        public List<FilePathTaskItem> SearchPaths { get; private set; }

        /// <summary>
        /// Gets the package type.
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Gets the verbose setting.
        /// </summary>
        public bool Verbose { get; private set; }

        /// <summary>
        /// Parses the command-line.
        /// </summary>
        /// <param name="args">Arguments from command-line.</param>
        /// <param name="messaging">Messaging object to send errors.</param>
        /// <param name="commandLine">Command line object created from command-line arguments</param>
        /// <returns>True if command-line is parsed, false if a failure was occurred.</returns>
        public static bool TryParseArguments(string[] args, Messaging messaging, out CommandLine commandLine)
        {
            bool success = true;

            commandLine = new CommandLine();

            for (int i = 0; i < args.Length; ++i)
            {
                if ('-' == args[i][0] || '/' == args[i][0])
                {
                    string arg = args[i].Substring(1).ToLowerInvariant();
                    if ("?" == arg || "help" == arg)
                    {
                        commandLine.Help = true;
                    }
                    else if ("arch" == arg || "architecture" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing architecture specification for '-architecture' option. Provide one of the following: x64, x86 or neutral.");
                            success = false;
                        }
                        else
                        {
                            commandLine.Architecture = args[i];
                        }
                    }
                    else if ("certificate" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing file specification for '-certificate' option.");
                            success = false;
                        }
                        else
                        {
                            //SimplifiedWixCompiler.certificatePath = Path.GetFullPath(args[i]);
                        }
                    }
                    else if ("d" == arg || "define" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing preprocessor definition for '-define' option. Provide a preprocessor definition in the form of: name or name=variable.");
                            success = false;
                        }
                        else
                        {
                            commandLine.PreprocessorDefines.Add(args[i]);
                        }
                    }
                    else if ("ext" == arg || "extension" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing reference specification for '-extension' option.");
                            success = false;
                        }
                        else
                        {
                            string[] extensionData = args[i].Split(new string[] { ";" }, 2, StringSplitOptions.RemoveEmptyEntries);
                            FilePathTaskItem extension = new FilePathTaskItem(extensionData[0]);
                            if (extensionData.Length > 1)
                            {
                                extension.SetMetadata("Data", extensionData[1]);
                            }

                            commandLine.Extensions.Add(extension);
                        }
                    }
                    else if ("lang" == arg || "language" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing file specification for '-language' option. Provide a valid language identifier such as: 1033 or en-US.");
                            success = false;
                        }
                        else
                        {
                            string[] languages = args[i].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            commandLine.Languages.AddRange(languages);
                        }
                    }
                    else if ("nologo" == arg)
                    {
                        commandLine.NoLogo = true;
                    }
                    else if ("o" == arg || "out" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing file specification for '-out' option.");
                            success = false;
                        }
                        else
                        {
                            string outputPath = Path.GetFullPath(args[i]);
                            commandLine.Output = new FilePathTaskItem(outputPath);
                        }
                    }
                    else if ("r" == arg || "reference" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing reference specification for '-reference' option.");
                            success = false;
                        }
                        else
                        {
                            commandLine.References.Add(new FilePathTaskItem(args[i]));
                        }
                    }
                    else if ("sp" == arg || "searchpath" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing reference specification for '-searchpath' option.");
                            success = false;
                        }
                        else
                        {
                            commandLine.SearchPaths.Add(new FilePathTaskItem(args[i]));
                        }
                    }
                    else if ("t" == arg || "type" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            messaging.OnError(null, "Missing type specification for '-type' option. Specify appx, msi, or wixlib.");
                            success = false;
                        }
                        else
                        {
                            commandLine.Type = args[i];
                        }
                    }
                    else if ("v" == arg || "verbose" == arg)
                    {
                        commandLine.Verbose = true;
                        messaging.Verbose = true;
                    }
                }
                else
                {
                    string[] file = args[i].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    string sourcePath = Path.GetFullPath(file[0]);
                    if (!System.IO.File.Exists(sourcePath))
                    {
                        messaging.OnError(null, "Source file '{0}' could not be found.", sourcePath);
                        success = false;
                    }
                    else
                    {
                        FilePathTaskItem item = new FilePathTaskItem(sourcePath);
                        if (file.Length > 1)
                        {
                            item.SetMetadata("TargetPath", file[1]);
                        }

                        commandLine.Files.Add(item);
                    }
                }
            }

            return success;
        }
    }
}
