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
    /// The main entry point for Nit
    /// </summary>
    public sealed class Nit
    {
        private List<string> inputFiles = new List<string>();
        private List<string> invalidArgs = new List<string>();
        private bool showLogo = true;
        private bool showHelp;

        /// <summary>
        /// Prevents a default instance of the Nit class from being created.
        /// </summary>
        private Nit()
        {
        }

        /// <summary>
        /// The main entry point for Nit.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("NIT", "nit.exe").Display += AppCommon.ConsoleDisplayMessage;

            Nit nit = new Nit();
            return nit.Run(args);
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

                Messaging.Instance.ShowVerboseMessages = true; // always verbose, to show passed tests

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
                    Console.WriteLine(NitStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return Messaging.Instance.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    Messaging.Instance.OnMessage(WixWarnings.UnsupportedCommandLineArgument(parameter));
                }

                this.invalidArgs = null;

                // gotta have something to do
                if (0 == this.inputFiles.Count)
                {
                    Console.WriteLine(NitStrings.HelpMessage);
                    Messaging.Instance.OnMessage(NitErrors.MalfunctionNeedInput());
                    return Messaging.Instance.LastErrorNumber;
                }

                // run tests and report results
                TestRunner runner = new TestRunner();
                runner.InputFiles = this.inputFiles;

                int failures = 0;
                int passes = 0;
                runner.RunTests(out passes, out failures);

                if (0 < failures)
                {
                    Messaging.Instance.OnMessage(NitErrors.TotalTestFailures(failures, passes));
                    return Messaging.Instance.LastErrorNumber;
                }
                else
                {
                    Messaging.Instance.OnMessage(NitVerboses.OneHundredPercent(passes));
                }
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
                    if ("nologo" == parameter)
                    {
                        this.showLogo = false;
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
