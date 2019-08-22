// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Lux
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Runtime.InteropServices;
    using WixToolset.Data;

    /// <summary>
    /// The main entry point for Lux
    /// </summary>
    public sealed class Lux
    {
        private StringCollection extensionList = new StringCollection();
        private List<string> inputFiles = new List<string>();
        private List<string> invalidArgs = new List<string>();
        private string outputFile;
        private bool showLogo = true;
        private bool showHelp;

        /// <summary>
        /// Prevents a default instance of the Lux class from being created.
        /// </summary>
        private Lux()
        {
        }

        /// <summary>
        /// The main entry point for Lux.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("LUX", "lux.exe").Display += AppCommon.ConsoleDisplayMessage;

            Lux lux = new Lux();
            return lux.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            try
            {
                // parse the command line
                this.ParseCommandLine(args);

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (Messaging.Instance.EncounteredError)
                {
                    return Messaging.Instance.LastErrorNumber;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(LuxStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return Messaging.Instance.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    Messaging.Instance.OnMessage(WixWarnings.UnsupportedCommandLineArgument(parameter));
                }

                this.invalidArgs = null;

                // gotta have something to do
                if (0 == this.inputFiles.Count || String.IsNullOrEmpty(this.outputFile))
                {
                    Console.WriteLine(LuxStrings.HelpMessage);
                    Messaging.Instance.OnMessage(LuxBuildErrors.MalfunctionNeedInput());
                    return Messaging.Instance.LastErrorNumber;
                }

                if (String.IsNullOrEmpty(Path.GetExtension(this.outputFile)))
                {
                    this.outputFile = Path.ChangeExtension(this.outputFile, ".wxs");
                }

                // get extensions from lux.exe.config
                AppCommon.ReadConfiguration(this.extensionList);

                Generator.Generate(this.extensionList, this.inputFiles, this.outputFile);
            }
            catch (WixException we)
            {
                Messaging.Instance.OnMessage(we.Error);
            }
            catch (Exception e)
            {
                Messaging.Instance.OnMessage(WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            return Messaging.Instance.LastErrorNumber;
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        private void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (null == arg || 0 == arg.Length)
                {
                    // skip blank arguments
                    continue;
                }

                if (1 == arg.Length)
                {
                    // treat '-' and '@' as filenames when by themselves.
                    this.inputFiles.AddRange(CommandLine.GetFiles(arg, "Source"));
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);
                    if ("ext" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            return;
                        }

                        this.extensionList.Add(args[i]);
                    }
                    else if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        string path = CommandLine.GetFileOrDirectory(parameter, args, ++i);

                        if (String.IsNullOrEmpty(path))
                        {
                            return;
                        }
                        else
                        {
                            this.outputFile = path;
                        }
                    }
                    else if ("v" == parameter)
                    {
                        Messaging.Instance.ShowVerboseMessages = true;
                    }
                    else if ("?" == parameter || "help" == parameter)
                    {
                        this.showHelp = true;
                        return;
                    }
                    else
                    {
                        this.invalidArgs.Add(parameter);
                    }
                }
                else if ('@' == arg[0])
                {
                    this.ParseCommandLine(CommandLineResponseFile.Parse(arg.Substring(1)));
                }
                else
                {
                    this.inputFiles.AddRange(CommandLine.GetFiles(arg, "Source"));
                }
            }

            return;
        }
    }
}
