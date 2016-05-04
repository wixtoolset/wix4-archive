// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public class LitCommandLine
    {
        public LitCommandLine()
        {
            this.ShowLogo = true;
            this.BindPaths = new List<BindPath>();
            this.Extensions = new List<string>();
            this.Files = new List<string>();
            this.LocalizationFiles = new List<string>();
        }

        public bool ShowLogo { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowPedanticMessages { get; private set; }

        public bool SuppressVersionCheck { get; private set; }

        public bool BindFiles { get; private set; }

        public string OutputFile { get; private set; }

        public List<BindPath> BindPaths { get; private set; }

        public List<string> Extensions { get; private set; }

        public List<string> Files { get; private set; }

        public List<string> LocalizationFiles { get; private set; }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        public string[] Parse(string[] args)
        {
            List<string> unprocessed = new List<string>();

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (String.IsNullOrEmpty(arg)) // skip blank arguments
                {
                    continue;
                }

                if (1 == arg.Length) // treat '-' and '@' as filenames when by themselves.
                {
                    unprocessed.Add(arg);
                }
                else if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);
                    if ("b" == parameter)
                    {
                        BindPath bindPath = CommandLine.GetBindPath(parameter, args, ++i);
                        if (null == bindPath)
                        {
                            break;
                        }

                        this.BindPaths.Add(bindPath);
                    }
                    else if ("bf" == parameter)
                    {
                        this.BindFiles = true;
                    }
                    else if ("ext" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            break;
                        }

                        this.Extensions.Add(args[i]);
                    }
                    else if ("loc" == parameter)
                    {
                        string locFile = CommandLine.GetFile(parameter, args, ++i);
                        if (String.IsNullOrEmpty(locFile))
                        {
                            break;
                        }

                        this.LocalizationFiles.Add(locFile);
                    }
                    else if ("nologo" == parameter)
                    {
                        this.ShowLogo = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.OutputFile = CommandLine.GetFile(parameter, args, ++i);
                        if (String.IsNullOrEmpty(this.OutputFile))
                        {
                            break;
                        }
                    }
                    else if ("pedantic" == parameter)
                    {
                        this.ShowPedanticMessages = true;
                    }
                    else if ("sv" == parameter)
                    {
                        this.SuppressVersionCheck = true;
                    }
                    else if (parameter.StartsWith("sw", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        if (0 == paramArg.Length)
                        {
                            Messaging.Instance.SuppressAllWarnings = true;
                        }
                        else
                        {
                            int suppressWarning = 0;
                            if (!Int32.TryParse(paramArg, out suppressWarning) || 0 >= suppressWarning)
                            {
                                Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                            }
                            else
                            {
                                Messaging.Instance.SuppressWarningMessage(suppressWarning);
                            }
                        }
                    }
                    else if (parameter.StartsWith("wx", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        if (0 == paramArg.Length)
                        {
                            Messaging.Instance.WarningsAsError = true;
                        }
                        else
                        {
                            int elevateWarning = 0;
                            if (!Int32.TryParse(paramArg, out elevateWarning) || 0 >= elevateWarning)
                            {
                                Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                            }
                            else
                            {
                                Messaging.Instance.ElevateWarningMessage(elevateWarning);
                            }
                        }
                    }
                    else if ("v" == parameter)
                    {
                        Messaging.Instance.ShowVerboseMessages = true;
                    }
                    else if ("?" == parameter || "help" == parameter)
                    {
                        this.ShowHelp = true;
                        break;
                    }
                    else
                    {
                        unprocessed.Add(arg);
                    }
                }
                else if ('@' == arg[0])
                {
                    string[] parsedArgs = CommandLineResponseFile.Parse(arg.Substring(1));
                    string[] unparsedArgs = this.Parse(parsedArgs);
                    unprocessed.AddRange(unparsedArgs);
                }
                else
                {
                    unprocessed.Add(arg);
                }
            }

            return unprocessed.ToArray();
        }

        public string[] ParsePostExtensions(string[] remaining)
        {
            List<string> unprocessed = new List<string>();

            for (int i = 0; i < remaining.Length; ++i)
            {
                string arg = remaining[i];
                if (String.IsNullOrEmpty(arg)) // skip blank arguments
                {
                    continue;
                }

                if (1 < arg.Length && ('-' == arg[0] || '/' == arg[0]))
                {
                    unprocessed.Add(arg);
                }
                else
                {
                    this.Files.AddRange(CommandLine.GetFiles(arg, "Source"));
                }
            }

            if (0 == this.Files.Count)
            {
                this.ShowHelp = true;
            }
            else if (String.IsNullOrEmpty(this.OutputFile))
            {
                if (1 < this.Files.Count)
                {
                    Messaging.Instance.OnMessage(WixErrors.MustSpecifyOutputWithMoreThanOneInput());
                }

                this.OutputFile = Path.ChangeExtension(Path.GetFileName(this.Files[0]), ".wixlib");
            }

            // Add the directories of the input files as unnamed bind paths.
            foreach (string file in this.Files)
            {
                BindPath bindPath = new BindPath(Path.GetDirectoryName(Path.GetFullPath(file)));
                this.BindPaths.Add(bindPath);
            }

            return unprocessed.ToArray();
        }
    }
}
