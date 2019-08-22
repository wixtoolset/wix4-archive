// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// Parse command line for pyro.
    /// </summary>
    public class PyroCommandLine
    {
        public PyroCommandLine()
        {
            this.ShowLogo = true;
            this.Tidy = true;

            this.Extensions = new List<string>();
        }

        public bool Delta { get; private set; }

        public bool ShowLogo { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowPedanticMessages { get; private set; }

        public bool Tidy { get; private set; }

        public string CabCachePath { get; private set; }

        public string OutputFile { get; private set; }

        public string PdbFile { get; private set; }

        public List<PatchTransform> PatchTransforms { get; private set; }

        public List<string> Extensions { get; private set; }

        public string InputFile { get; private set; }

        public bool SuppressWixPdb { get; set; }

        public bool SuppressAclReset { get; private set; }

        public List<BindPath> TargetBindPaths { get; private set; }

        public List<BindPath> UpdatedBindPaths { get; private set; }

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
                    if ("bt" == parameter)
                    {
                        BindPath bindPath = CommandLine.GetBindPath(parameter, args, ++i);
                        if (null == bindPath)
                        {
                            break;
                        }

                        this.TargetBindPaths.Add(bindPath);
                    }
                    else if ("bu" == parameter)
                    {
                        BindPath bindPath = CommandLine.GetBindPath(parameter, args, ++i);
                        if (null == bindPath)
                        {
                            break;
                        }

                        this.UpdatedBindPaths.Add(bindPath);
                    }
                    else if (parameter.Equals("cc", StringComparison.Ordinal))
                    {
                        this.CabCachePath = CommandLine.GetDirectory(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.CabCachePath))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("delta", StringComparison.Ordinal))
                    {
                        this.Delta = true;
                    }
                    else if (parameter.Equals("ext", StringComparison.Ordinal))
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            break;
                        }

                        this.Extensions.Add(args[i]);
                    }
                    else if ("nologo" == parameter)
                    {
                        this.ShowLogo = false;
                    }
                    else if ("notidy" == parameter)
                    {
                        this.Tidy = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.OutputFile = CommandLine.GetFile(parameter, args, ++i);
                        if (String.IsNullOrEmpty(this.OutputFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("pdbout", StringComparison.Ordinal))
                    {
                        this.PdbFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.PdbFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("reusecab", StringComparison.Ordinal))
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DeprecatedCommandLineSwitch(arg, "-cc"));
                    }
                    else if (parameter.Equals("spdb", StringComparison.Ordinal))
                    {
                        this.SuppressWixPdb = true;
                    }
                    else if (parameter.Equals("sacl", StringComparison.Ordinal))
                    {
                        this.SuppressAclReset = true;
                    }
                    else if (parameter.Equals("t", StringComparison.Ordinal))
                    {
                        string baseline;
                        string transformPath;

                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.BaselineRequired());
                            break;
                        }

                        baseline = args[i];

                        transformPath = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(transformPath))
                        {
                            break;
                        }

                        // Verify the transform hasn't been added already.
                        if (this.PatchTransforms.Any(t => t.TransformPath.Equals(transformPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            Messaging.Instance.OnMessage(WixErrors.DuplicateTransform(transformPath));
                            break;
                        }

                        this.PatchTransforms.Add(new PatchTransform(transformPath, baseline));
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
                    if (String.IsNullOrEmpty(this.InputFile))
                    {
                        this.InputFile = CommandLine.VerifyPath(arg);
                        if (String.IsNullOrEmpty(this.InputFile))
                        {
                            break;
                        }
                    }
                    else
                    {
                        unprocessed.Add(arg);
                    }
                }
            }

            if (String.IsNullOrEmpty(this.InputFile))
            {
                this.ShowHelp = true;
            }
            else if (String.IsNullOrEmpty(this.OutputFile))
            {
                this.OutputFile = Path.ChangeExtension(Path.GetFileName(this.InputFile), ".msp");
            }

            if (!this.SuppressWixPdb && String.IsNullOrEmpty(this.PdbFile) && !String.IsNullOrEmpty(this.OutputFile))
            {
                this.PdbFile = Path.ChangeExtension(this.OutputFile, ".wixpdb");
            }

            return unprocessed.ToArray();
        }
    }
}
