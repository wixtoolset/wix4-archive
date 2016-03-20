//-------------------------------------------------------------------------------------------------
// <copyright file="ThmGenCommandLine.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using WixToolset.Data;

    /// <summary>
    /// Parse command line for ThmGen.
    /// </summary>
    public class ThmGenCommandLine
    {
        public ThmGenCommandLine()
        {
            this.ShowLogo = true;
        }

        public bool ShowLogo { get; private set; }

        public bool ShowHelp { get; private set; }

        public string OutputFile { get; private set; }

        public string InputFile { get; private set; }

        public string Prefix { get; private set; }

        public string HeaderName { get; private set; }

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
                    if ("nologo" == parameter)
                    {
                        this.ShowLogo = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        string path = CommandLine.GetFile(parameter, args, ++i);

                        if (!String.IsNullOrEmpty(path))
                        {
                            this.OutputFile = path;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ("?" == parameter || "help" == parameter)
                    {
                        this.ShowHelp = true;
                        break;
                    }
                    else if ("header" == parameter)
                    {
                        if (CommandLine.IsValidArg(args, ++i))
                        {
                            this.HeaderName = args[i];
                        }
                    }
                    else if ("prefix" == parameter)
                    {
                        if (CommandLine.IsValidArg(args, ++i))
                        {
                            this.Prefix = args[i];
                        }
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
                    var files = CommandLine.GetFiles(arg, "Source");
                    if (1 == files.Length)
                    {
                        this.InputFile = files[0];
                    }
                    else
                    {
                        unprocessed.AddRange(files);
                    }
                }
            }

            if (String.IsNullOrEmpty(this.InputFile))
            {
                this.ShowHelp = true;
            }

            return unprocessed.ToArray();
        }
    }
}
