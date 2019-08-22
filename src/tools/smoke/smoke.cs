// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using WixToolset.Data;

    /// <summary>
    /// The main entry point for Smoke.
    /// </summary>
    public sealed class Smoke
    {
        private const string msm = ".msm";
        private const string msi = ".msi";
        private const string msp = ".msp";

        private bool addDefault;
        private StringCollection extensionList;
        private StringCollection ices;
        private StringCollection inputFiles;
        private StringCollection invalidArgs;
        private string pdbPath;
        private bool showHelp;
        private bool showLogo;
        private StringCollection suppressICEs;
        private bool tidy;
        private Validator validator;

        /// <summary>
        /// Instantiate a new Smoke class.
        /// </summary>
        private Smoke()
        {
            this.extensionList = new StringCollection();
            this.ices = new StringCollection();
            this.inputFiles = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.addDefault = true;
            this.showLogo = true;
            this.suppressICEs = new StringCollection();
            this.tidy = true;
            this.validator = new Validator();
        }

        /// <summary>
        /// The main entry point for smoke.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("SMOK", "smoke.exe").Display += AppCommon.ConsoleDisplayMessage;

            Smoke smoke = new Smoke();
            return smoke.Run(args);
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

                if (0 == this.inputFiles.Count)
                {
                    this.showHelp = true;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(SmokeStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return Messaging.Instance.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    Messaging.Instance.OnMessage(WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                string tempFilesLocation = AppCommon.GetTempLocation();
                validator.TempFilesLocation = tempFilesLocation;

                // TODO: rename ValidatorExtensions to "ValidatorFilterExtension" or something like that. Actually,
                //       revisit all of this as we try to build a more generic validation system around ICEs.
                //
                // load any extensions
                //bool validatorExtensionLoaded = false;
                //foreach (string extension in this.extensionList)
                //{
                //    WixExtension wixExtension = WixExtension.Load(extension);

                //    ValidatorExtension validatorExtension = wixExtension.ValidatorExtension;
                //    if (null != validatorExtension)
                //    {
                //        if (validatorExtensionLoaded)
                //        {
                //            throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, SmokeStrings.EXP_CannotLoadLinkerExtension, validatorExtension.GetType().ToString(), validator.Extension.ToString()), "ext");
                //        }

                //        validator.Extension = validatorExtension;
                //        validatorExtensionLoaded = true;
                //    }
                //}

                // disable ICE33 and ICE66 by default
                this.suppressICEs.Add("ICE33");
                this.suppressICEs.Add("ICE66");

                // set the ICEs
                string[] iceArray = new string[this.ices.Count];
                this.ices.CopyTo(iceArray, 0);
                validator.ICEs = iceArray;

                // set the suppressed ICEs
                string[] suppressICEArray = new string[this.suppressICEs.Count];
                this.suppressICEs.CopyTo(suppressICEArray, 0);
                validator.SuppressedICEs = suppressICEArray;

                // Load the pdb and assign the Output to the validator
                if (null != pdbPath)
                {
                    string pdbFullPath = Path.GetFullPath(pdbPath);
                    Pdb pdb = Pdb.Load(pdbFullPath, false);
                    this.validator.Output = pdb.Output;
                }

                foreach (string inputFile in this.inputFiles)
                {
                    // set the default cube file
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    string appDirectory = Path.GetDirectoryName(assembly.Location);

                    if (this.addDefault)
                    {
                        switch (Path.GetExtension(inputFile).ToLower(CultureInfo.InvariantCulture))
                        {
                            case msm:
                                validator.AddCubeFile(Path.Combine(appDirectory, "mergemod.cub"));
                                break;
                            case msi:
                                validator.AddCubeFile(Path.Combine(appDirectory, "darice.cub"));
                                break;
                            default:
                                throw new WixException(WixErrors.UnexpectedFileExtension(inputFile, ".msi, .msm"));
                        }
                    }

                    // print friendly message saying what file is being validated
                    Console.WriteLine(Path.GetFileName(inputFile));
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    try
                    {
                        validator.Validate(Path.GetFullPath(inputFile));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Messaging.Instance.OnMessage(WixErrors.UnauthorizedAccess(Path.GetFullPath(inputFile)));
                    }
                    finally
                    {
                        stopwatch.Stop();
                        Messaging.Instance.OnMessage(WixVerboses.ValidatedDatabase(stopwatch.ElapsedMilliseconds));

                        if (this.tidy)
                        {
                            if (!AppCommon.DeleteDirectory(tempFilesLocation, Messaging.Instance))
                            {
                                Console.Error.WriteLine(SmokeStrings.WAR_FailedToDeleteTempDir, tempFilesLocation);
                            }
                        }
                        else
                        {
                            Console.WriteLine(SmokeStrings.INF_TempDirLocatedAt, tempFilesLocation);
                        }
                    }
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

                // skip blank arguments
                if (null == arg || 0 == arg.Length)
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);

                    if ("cub" == parameter)
                    {
                        string cubeFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(cubeFile))
                        {
                            return;
                        }

                        this.validator.AddCubeFile(cubeFile);
                    }
                    else if ("ext" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            return;
                        }

                        this.extensionList.Add(args[i]);
                    }
                    else if (parameter.StartsWith("ice:"))
                    {
                        this.ices.Add(parameter.Substring(4));
                    }
                    else if ("pdb" == parameter)
                    {
                        this.pdbPath = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.pdbPath))
                        {
                            return;
                        }
                    }
                    else if ("nodefault" == parameter)
                    {
                        this.addDefault = false;
                    }
                    else if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("notidy" == parameter)
                    {
                        this.tidy = false;
                    }
                    else if (parameter.StartsWith("sice:"))
                    {
                        this.suppressICEs.Add(parameter.Substring(5));
                    }
                    else if ("swall" == parameter)
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DeprecatedCommandLineSwitch("swall", "sw"));
                        Messaging.Instance.SuppressAllWarnings = true;
                    }
                    else if (parameter.StartsWith("sw"))
                    {
                        string paramArg = parameter.Substring(2);
                        try
                        {
                            if (0 == paramArg.Length)
                            {
                                Messaging.Instance.SuppressAllWarnings = true;
                            }
                            else
                            {
                                int suppressWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                                if (0 >= suppressWarning)
                                {
                                    Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                                }

                                Messaging.Instance.SuppressWarningMessage(suppressWarning);
                            }
                        }
                        catch (FormatException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                        }
                        catch (OverflowException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                        }
                    }
                    else if ("wxall" == parameter)
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DeprecatedCommandLineSwitch("wxall", "wx"));
                        Messaging.Instance.WarningsAsError = true;
                    }
                    else if (parameter.StartsWith("wx"))
                    {
                        string paramArg = parameter.Substring(2);
                        try
                        {
                            if (0 == paramArg.Length)
                            {
                                Messaging.Instance.WarningsAsError = true;
                            }
                            else
                            {
                                int elevateWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                                if (0 >= elevateWarning)
                                {
                                    Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                                }

                                Messaging.Instance.ElevateWarningMessage(elevateWarning);
                            }
                        }
                        catch (FormatException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                        }
                        catch (OverflowException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
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
                    // Verify the file extension is an expected value
                    if (IsValidFileExtension(arg))
                    {
                        this.inputFiles.AddRange(CommandLine.GetFiles(arg, "Source"));
                    }
                }
            }
        }

        /// <summary>
        /// Examines the file extension to determine if it is for a supported setup file extension.
        /// MSP file extensions are not currently supported and are flagged as invalid.
        /// </summary>
        /// <param name="searchPath">Search path to find files in.</param>
        /// <returns></returns>
        private bool IsValidFileExtension(string searchPath)
        {
            bool isFileValid = false;
            string extension = null;

            try
            {
                extension = Path.GetExtension(searchPath).ToLower(CultureInfo.InvariantCulture);
            }
            catch (ArgumentException)
            {
                // The path contains one or more invalid characters.
                Messaging.Instance.OnMessage(WixErrors.SmokeMalformedPath());
                // Can not continue further validation of the filename because an invalid character exists in the path.
                // GetExtension threw an ArgumentException before it extracted the extension so we don't know if a valid 
                // file extension is present.  
                //
                // Example input: "|\Setup.msi" or if a control character is present such as ctrl-o "^O\Setup.msi"
                //
                // Either example string would cause Path.GetExtension() to throw an ArgumentException and return null.
                // If we continued validating, the null returned by Path.GetExtension() would be flagged as unknown
                // even though the file extension in the examples given is valid.
                return false;
            }

            if (String.IsNullOrEmpty(extension))
            {
                // Display the unknown extension message if the file extension isn't present.
                Messaging.Instance.OnMessage(WixErrors.SmokeUnknownFileExtension());
                // Do not continue validating the file extension because there is no file extension to examine.
                return false;
            }

            switch (extension)
            {
                case msm:
                case msi:
                    // The file extension found is supported.
                    isFileValid = true;
                    break;
                case msp:
                    // The file extension found is not currently supported.
                    Messaging.Instance.OnMessage(WixErrors.SmokeUnsupportedFileExtension());
                    break;
                default:
                    // The file extension was not recognized and is not supported.
                    Messaging.Instance.OnMessage(WixErrors.SmokeUnknownFileExtension());
                    break;
            }

            return isFileValid;
        }
    }
}
