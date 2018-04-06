// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Cab;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public class LightCommandLine
    {
        public LightCommandLine()
        {
            this.ShowLogo = true;
            this.Tidy = true;

            this.CubeFiles = new List<string>();
            this.SuppressIces = new List<string>();
            this.Ices = new List<string>();
            this.BindPaths = new List<BindPath>();
            this.Extensions = new List<string>();
            this.Files = new List<string>();
            this.LocalizationFiles = new List<string>();
            this.Variables = new Dictionary<string, string>();
        }

        public string PdbFile { get; private set; }

        public CompressionLevel? DefaultCompressionLevel { get; set; }

        public bool SuppressAclReset { get; private set; }

        public bool SuppressLayout { get; private set; }

        public bool SuppressWixPdb { get; private set; }

        public bool SuppressValidation { get; private set; }

        public string OutputsFile { get; private set; }

        public string BuiltOutputsFile { get; private set; }

        public string WixprojectFile { get; private set; }

        public string ContentsFile { get; private set; }

        public List<string> Ices { get; private set; }

        public string CabCachePath { get; private set; }

        public int CabbingThreadCount { get; private set; }

        public List<string> CubeFiles { get; private set; }

        public List<string> SuppressIces { get; private set; }

        public bool ShowLogo { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowPedanticMessages { get; private set; }

        public bool SuppressLocalization { get; private set; }

        public bool SuppressVersionCheck { get; private set; }

        public string[] Cultures { get; private set; }

        public string OutputFile { get; private set; }

        public bool OutputXml { get; private set; }

        public List<BindPath> BindPaths { get; private set; }

        public List<string> Extensions { get; private set; }

        public List<string> Files { get; private set; }

        public List<string> LocalizationFiles { get; private set; }

        public bool Tidy { get; private set; }

        public string UnreferencedSymbolsFile { get; private set; }

        public IDictionary<string, string> Variables { get; private set; }

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
                    if (parameter.Equals("b", StringComparison.Ordinal))
                    {
                        BindPath bindPath = CommandLine.GetBindPath(parameter, args, ++i);
                        if (null == bindPath)
                        {
                            break;
                        }

                        this.BindPaths.Add(bindPath);
                    }
                    else if (parameter.StartsWith("cultures:", StringComparison.Ordinal))
                    {
                        string culturesString = arg.Substring(10).ToLower(CultureInfo.InvariantCulture);

                        // When null is used treat it as if cultures wasn't specified.
                        // This is needed for batching over the light task when using MSBuild which doesn't
                        // support empty items
                        if (culturesString.Equals("null", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Cultures = null;
                        }
                        else
                        {
                            this.Cultures = culturesString.Split(';', ',');

                            for (int c = 0; c < this.Cultures.Length; ++c)
                            {
                                // Neutral is different from null. For neutral we still want to do WXL filtering.
                                // Set the culture to the empty string = identifier for the invariant culture
                                if (this.Cultures[c].Equals("neutral", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.Cultures[c] = String.Empty;
                                }
                            }
                        }
                    }
                    else if (parameter.StartsWith("dcl:", StringComparison.Ordinal))
                    {
                        string defaultCompressionLevel = arg.Substring(5);

                        if (String.IsNullOrEmpty(defaultCompressionLevel))
                        {
                            break;
                        }

                        this.DefaultCompressionLevel = WixCreateCab.CompressionLevelFromString(defaultCompressionLevel);
                    }
                    else if (parameter.StartsWith("d", StringComparison.Ordinal))
                    {
                        parameter = arg.Substring(2);
                        string[] value = parameter.Split("=".ToCharArray(), 2);

                        string preexisting;
                        if (1 == value.Length)
                        {
                            Messaging.Instance.OnMessage(WixErrors.ExpectedWixVariableValue(value[0]));
                        }
                        else if (this.Variables.TryGetValue(value[0], out preexisting))
                        {
                            Messaging.Instance.OnMessage(WixErrors.WixVariableCollision(null, value[0]));
                        }
                        else
                        {
                            this.Variables.Add(value[0], value[1]);
                        }
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
                    else if (parameter.Equals("loc", StringComparison.Ordinal))
                    {
                        string locFile = CommandLine.GetFile(parameter, args, ++i);
                        if (String.IsNullOrEmpty(locFile))
                        {
                            break;
                        }

                        this.LocalizationFiles.Add(locFile);
                    }
                    else if (parameter.Equals("nologo", StringComparison.Ordinal))
                    {
                        this.ShowLogo = false;
                    }
                    else if (parameter.Equals("notidy", StringComparison.Ordinal))
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
                    else if (parameter.Equals("pedantic", StringComparison.Ordinal))
                    {
                        this.ShowPedanticMessages = true;
                    }
                    else if (parameter.Equals("sloc", StringComparison.Ordinal))
                    {
                        this.SuppressLocalization = true;
                    }
                    else if (parameter.Equals("usf", StringComparison.Ordinal))
                    {
                        this.UnreferencedSymbolsFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.UnreferencedSymbolsFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("xo", StringComparison.Ordinal))
                    {
                        this.OutputXml = true;
                    }
                    else if (parameter.Equals("cc", StringComparison.Ordinal))
                    {
                        this.CabCachePath = CommandLine.GetDirectory(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.CabCachePath))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("ct", StringComparison.Ordinal))
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalCabbingThreadCount(String.Empty));
                            break;
                        }

                        int ct = 0;
                        if (!Int32.TryParse(args[i], out ct) || 0 >= ct)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalCabbingThreadCount(args[i]));
                            break;
                        }

                        this.CabbingThreadCount = ct;
                        Messaging.Instance.OnMessage(WixVerboses.SetCabbingThreadCount(this.CabbingThreadCount.ToString()));
                    }
                    else if (parameter.Equals("cub", StringComparison.Ordinal))
                    {
                        string cubeFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(cubeFile))
                        {
                            break;
                        }

                        this.CubeFiles.Add(cubeFile);
                    }
                    else if (parameter.Equals("eav", StringComparison.Ordinal))
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DeprecatedCommandLineSwitch(arg));
                    }
                    else if (parameter.StartsWith("ice:", StringComparison.Ordinal))
                    {
                        this.Ices.Add(parameter.Substring(4));
                    }
                    else if (parameter.Equals("contentsfile", StringComparison.Ordinal))
                    {
                        this.ContentsFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.ContentsFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("outputsfile", StringComparison.Ordinal))
                    {
                        this.OutputsFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.OutputsFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("builtoutputsfile", StringComparison.Ordinal))
                    {
                        this.BuiltOutputsFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.BuiltOutputsFile))
                        {
                            break;
                        }
                    }
                    else if (parameter.Equals("wixprojectfile", StringComparison.Ordinal))
                    {
                        this.WixprojectFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.WixprojectFile))
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
                    else if (parameter.StartsWith("sice:", StringComparison.Ordinal))
                    {
                        this.SuppressIces.Add(parameter.Substring(5));
                    }
                    else if (parameter.Equals("sl", StringComparison.Ordinal))
                    {
                        this.SuppressLayout = true;
                    }
                    else if (parameter.Equals("spdb", StringComparison.Ordinal))
                    {
                        this.SuppressWixPdb = true;
                    }
                    else if (parameter.Equals("sacl", StringComparison.Ordinal))
                    {
                        this.SuppressAclReset = true;
                    }
                    else if (parameter.Equals("sval", StringComparison.Ordinal))
                    {
                        this.SuppressValidation = true;
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

                // After the linker tells us what the output type actually is, we'll change the ".wix" to the correct extension.
                this.OutputFile = Path.ChangeExtension(Path.GetFileName(this.Files[0]), ".wix");

                // Add the directories of the input files as unnamed bind paths.
                foreach (string file in this.Files)
                {
                    BindPath bindPath = new BindPath(Path.GetDirectoryName(Path.GetFullPath(file)));
                    this.BindPaths.Add(bindPath);
                }
            }

            if (!this.SuppressWixPdb && String.IsNullOrEmpty(this.PdbFile) && !String.IsNullOrEmpty(this.OutputFile))
            {
                this.PdbFile = Path.ChangeExtension(this.OutputFile, ".wixpdb");
            }

            return unprocessed.ToArray();
        }
    }
}
