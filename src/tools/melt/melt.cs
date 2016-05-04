// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Dtf.WindowsInstaller;
    using WixToolset.Dtf.WindowsInstaller.Package;

    using Wix = WixToolset.Data.Serialize;
    using WixToolset.Extensibility;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Entry point for the melter
    /// </summary>
    public sealed class Melt
    {
        private string exportBasePath;
        private StringCollection extensionList;
        private StringCollection invalidArgs;
        private string id;
        private string inputFile;
        private string inputPdbFile;
        private string outputFile;
        private OutputType outputType;
        private bool showHelp;
        private bool showLogo;
        private bool tidy;
        private bool suppressExtraction;

        /// <summary>
        /// Instantiate a new Melt class.
        /// </summary>
        private Melt()
        {
            this.extensionList = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.showLogo = true;
            this.tidy = true;
            this.id = null;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Arguments to decompiler.</param>
        /// <returns>0 if sucessful, otherwise 1.</returns>
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("MELT", "melt.exe").Display += AppCommon.ConsoleDisplayMessage;

            Melt melt = new Melt();
            return melt.Run(args);
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

                if (String.IsNullOrEmpty(this.inputFile) || String.IsNullOrEmpty(this.outputFile) || (OutputType.Product == this.outputType && String.IsNullOrEmpty(this.inputPdbFile)))
                {
                    this.showHelp = true;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(MeltStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return Messaging.Instance.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    Messaging.Instance.OnMessage(WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                if (null == this.exportBasePath)
                {
                    this.exportBasePath = System.IO.Path.GetDirectoryName(this.outputFile);
                }

                if (OutputType.Module == this.outputType)
                {
                    MeltModule();
                }
                else if (OutputType.Product == this.outputType)
                {
                    MeltProduct();
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
        /// Extracts files from a merge module and creates corresponding ComponentGroup WiX authoring.
        /// </summary>
        private void MeltModule()
        {
            Decompiler decompiler = null;
            Unbinder unbinder = null;
            Melter melter = null;

            try
            {
                // create the decompiler, unbinder, and melter
                decompiler = new Decompiler();
                unbinder = new Unbinder();
                melter = new Melter(decompiler, id);

                // read the configuration file (melt.exe.config)
                AppCommon.ReadConfiguration(this.extensionList);

                // load all extensions
                ExtensionManager extensionManager = new ExtensionManager();
                foreach (string extension in this.extensionList)
                {
                    extensionManager.Load(extension);
                }

                foreach (IDecompilerExtension extension in extensionManager.Create<IDecompilerExtension>())
                {
                    decompiler.AddExtension(extension);
                }

                foreach (IUnbinderExtension extension in extensionManager.Create<IUnbinderExtension>())
                {
                    unbinder.AddExtension(extension);
                }

                // set options
                decompiler.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");

                unbinder.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");
                unbinder.SuppressDemodularization = true;

                // print friendly message saying what file is being decompiled
                Console.WriteLine(Path.GetFileName(this.inputFile));

                // unbind
                Output output = unbinder.Unbind(this.inputFile, this.outputType, this.exportBasePath);

                if (null != output)
                {
                    Wix.Wix wix = melter.Melt(output);
                    if (null != wix)
                    {
                        XmlTextWriter writer = null;

                        try
                        {
                            writer = new XmlTextWriter(this.outputFile, System.Text.Encoding.UTF8);

                            writer.Indentation = 4;
                            writer.IndentChar = ' ';
                            writer.QuoteChar = '"';
                            writer.Formatting = Formatting.Indented;

                            writer.WriteStartDocument();
                            wix.OutputXml(writer);
                            writer.WriteEndDocument();
                        }
                        finally
                        {
                            if (null != writer)
                            {
                                writer.Close();
                            }
                        }
                    }
                }
            }
            finally
            {
                if (null != decompiler)
                {
                    if (this.tidy)
                    {
                        if (!decompiler.DeleteTempFiles())
                        {
                            Console.Error.WriteLine(MeltStrings.WAR_FailedToDeleteTempDir, decompiler.TempFilesLocation);
                        }
                    }
                    else
                    {
                        Console.WriteLine(MeltStrings.INF_TempDirLocatedAt, decompiler.TempFilesLocation);
                    }
                }

                if (null != unbinder)
                {
                    if (this.tidy)
                    {
                        if (!unbinder.DeleteTempFiles())
                        {
                            Console.Error.WriteLine(MeltStrings.WAR_FailedToDeleteTempDir, unbinder.TempFilesLocation);
                        }
                    }
                    else
                    {
                        Console.WriteLine(MeltStrings.INF_TempDirLocatedAt, unbinder.TempFilesLocation);
                    }
                }
            }
        }

        /// <summary>
        /// Checks to make sure that the debug symbols match up with the MSI.
        /// This is to help in ensuring that error 1642 does not inexplicably appear.
        /// </summary>
        /// <remarks>
        /// This is meant to assist with Bug # 4792
        /// http://wixtoolset.org/issues/4792/
        /// </remarks>
        /// <param name="package">
        /// The MSI currently being melted.
        /// </param>
        /// <param name="inputPdb">
        /// The debug symbols package being compared against the <paramref name="package"/>.
        /// </param>
        /// <returns></returns>
        private static bool ValidateMsiMatchesPdb(InstallPackage package, Pdb inputPdb)
        {
            string msiPackageCode = (string)package.Property["PackageCode"];

            foreach (Row pdbPropertyRow in inputPdb.Output.Tables["Property"].Rows)
            {
                if ("PackageCode" == (string)pdbPropertyRow.Fields[0].Data)
                {
                    string pdbPackageCode = (string)pdbPropertyRow.Fields[1].Data;
                    if (msiPackageCode != pdbPackageCode)
                    {
                        Console.WriteLine(MeltStrings.WAR_MismatchedPackageCode, msiPackageCode, pdbPackageCode);
                        return false;
                    }
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Extracts files from an MSI database and rewrites the paths embedded in the source .wixpdb to the output .wixpdb.
        /// </summary>
        private void MeltProduct()
        {
            // print friendly message saying what file is being decompiled
            Console.WriteLine("{0} / {1}", Path.GetFileName(this.inputFile), Path.GetFileName(this.inputPdbFile));

            Pdb inputPdb = Pdb.Load(this.inputPdbFile, true);

            // extract files from the .msi (unless suppressed) and get the path map of File ids to target paths
            string outputDirectory = this.exportBasePath ?? Environment.GetEnvironmentVariable("WIX_TEMP");
            IDictionary<string, string> paths = null;
            using (InstallPackage package = new InstallPackage(this.inputFile, DatabaseOpenMode.ReadOnly, null, outputDirectory))
            {
                // ignore failures as this is a new validation in v3.x
                ValidateMsiMatchesPdb(package, inputPdb);

                if (!this.suppressExtraction)
                {
                    package.ExtractFiles();
                }

                paths = package.Files.SourcePaths;
            }

            Table wixFileTable = inputPdb.Output.Tables["WixFile"];
            if (null != wixFileTable)
            {
                foreach (Row row in wixFileTable.Rows)
                {
                    WixFileRow fileRow = row as WixFileRow;
                    if (null != fileRow)
                    {
                        string newPath;
                        if (paths.TryGetValue(fileRow.File, out newPath))
                        {
                            fileRow.Source = Path.Combine(outputDirectory, newPath);
                        }
                    }
                }
            }

            inputPdb.Save(this.outputFile);
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
                if (null == arg || 0 == arg.Length) // skip blank arguments
                {
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
                    else if ("id" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            Messaging.Instance.OnMessage(WixErrors.ExpectedArgument(String.Concat("-", parameter)));
                            return;
                        }

                        this.id = args[i];
                    }
                    else if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("notidy" == parameter)
                    {
                        this.tidy = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.outputFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.outputFile))
                        {
                            return;
                        }
                    }
                    else if ("pdb" == parameter)
                    {
                        this.inputPdbFile = CommandLine.GetFile(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.inputPdbFile))
                        {
                            return;
                        }
                    }
                    else if ("sextract" == parameter)
                    {
                        this.suppressExtraction = true;
                    }
                    else if ("swall" == parameter)
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DeprecatedCommandLineSwitch("swall", "sw"));
                        Messaging.Instance.SuppressAllWarnings = true;
                    }
                    else if (parameter.StartsWith("sw", StringComparison.Ordinal))
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
                    else if (parameter.StartsWith("wx", StringComparison.Ordinal))
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
                    else if ("x" == parameter)
                    {
                        this.exportBasePath = CommandLine.GetDirectory(parameter, args, ++i);

                        if (String.IsNullOrEmpty(this.exportBasePath))
                        {
                            return;
                        }
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
                else
                {
                    if (null == this.inputFile)
                    {
                        this.inputFile = CommandLine.VerifyPath(arg);

                        if (String.IsNullOrEmpty(this.inputFile))
                        {
                            return;
                        }

                        // guess the output type based on the extension of the input file
                        if (OutputType.Unknown == this.outputType)
                        {
                            string extension = Path.GetExtension(this.inputFile);
                            this.outputType = Output.GetOutputType(extension);

                            if (OutputType.Unknown == this.outputType)
                            {
                                Messaging.Instance.OnMessage(WixErrors.UnexpectedFileExtension(extension, ".msm, .msi"));
                                return;
                            }
                        }
                    }
                    else if (null == this.outputFile)
                    {
                        this.outputFile = CommandLine.VerifyPath(arg);

                        if (String.IsNullOrEmpty(this.outputFile))
                        {
                            return;
                        }
                    }
                    else
                    {
                        Messaging.Instance.OnMessage(WixErrors.AdditionalArgumentUnexpected(arg));
                    }
                }
            }
        }
    }
}
