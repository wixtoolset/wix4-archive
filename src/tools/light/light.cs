// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The main entry point for light.
    /// </summary>
    public sealed class Light
    {
        LightCommandLine commandLine;
        private IEnumerable<IExtensionData> extensionData;
        private IEnumerable<IBinderExtension> binderExtensions;
        private IEnumerable<IBinderFileManager> fileManagers;

        /// <summary>
        /// The main entry point for light.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("LGHT", "light.exe").Display += AppCommon.ConsoleDisplayMessage;

            Light light = new Light();
            return light.Execute(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Execute(string[] args)
        {
            try
            {
                string[] unparsed = this.ParseCommandLineAndLoadExtensions(args);

                if (!Messaging.Instance.EncounteredError)
                {
                    if (this.commandLine.ShowLogo)
                    {
                        AppCommon.DisplayToolHeader();
                    }

                    if (this.commandLine.ShowHelp)
                    {
                        this.PrintHelp();
                        AppCommon.DisplayToolFooter();
                    }
                    else
                    {
                        foreach (string arg in unparsed)
                        {
                            Messaging.Instance.OnMessage(WixWarnings.UnsupportedCommandLineArgument(arg));
                        }

                        this.Run();
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
        /// Parse command line and load all the extensions.
        /// </summary>
        /// <param name="args">Command line arguments to be parsed.</param>
        private string[] ParseCommandLineAndLoadExtensions(string[] args)
        {
            this.commandLine = new LightCommandLine();
            string[] unprocessed = this.commandLine.Parse(args);
            if (Messaging.Instance.EncounteredError)
            {
                return unprocessed;
            }

            // Load extensions.
            ExtensionManager extensionManager = new ExtensionManager();
            foreach (string extension in this.commandLine.Extensions)
            {
                extensionManager.Load(extension);
            }

            // Extension data command line processing.
            this.extensionData = extensionManager.Create<IExtensionData>();
            foreach (IExtensionCommandLine dce in this.extensionData.Where(e => e is IExtensionCommandLine).Cast<IExtensionCommandLine>())
            {
                dce.MessageHandler = Messaging.Instance;
                unprocessed = dce.ParseCommandLine(unprocessed);
            }

            // Binder extensions command line processing.
            this.binderExtensions = extensionManager.Create<IBinderExtension>();
            foreach (IExtensionCommandLine bce in this.binderExtensions.Where(e => e is IExtensionCommandLine).Cast<IExtensionCommandLine>())
            {
                bce.MessageHandler = Messaging.Instance;
                unprocessed = bce.ParseCommandLine(unprocessed);
            }

            // File resolution command line processing.
            this.fileManagers = extensionManager.Create<IBinderFileManager>();
            if (this.fileManagers.Any())
            {
                foreach (IExtensionCommandLine fme in this.fileManagers.Where(e => e is IExtensionCommandLine).Cast<IExtensionCommandLine>())
                {
                    fme.MessageHandler = Messaging.Instance;
                    unprocessed = fme.ParseCommandLine(unprocessed);
                }
            }
            else // there are no extension file managers so add the default one.
            {
                List<IBinderFileManager> defaultBinderFileManager = new List<IBinderFileManager>();
                defaultBinderFileManager.Add(new BinderFileManager());

                this.fileManagers = defaultBinderFileManager;
            }

            return commandLine.ParsePostExtensions(unprocessed);
        }

        private void Run()
        {
            // Initialize the variable resolver from the command line.
            WixVariableResolver wixVariableResolver = new WixVariableResolver();
            foreach (var wixVar in this.commandLine.Variables)
            {
                wixVariableResolver.AddVariable(wixVar.Key, wixVar.Value);
            }

            // Initialize the linker from the command line.
            Linker linker = new Linker();
            linker.UnreferencedSymbolsFile = this.commandLine.UnreferencedSymbolsFile;
            linker.ShowPedanticMessages = this.commandLine.ShowPedanticMessages;
            linker.WixVariableResolver = wixVariableResolver;

            foreach (IExtensionData data in this.extensionData)
            {
                linker.AddExtensionData(data);
            }

            // Initialize the binder from the command line.
            WixToolset.Binder binder = new WixToolset.Binder();
            binder.CabCachePath = this.commandLine.CabCachePath;
            binder.ContentsFile = this.commandLine.ContentsFile;
            binder.BuiltOutputsFile = this.commandLine.BuiltOutputsFile;
            binder.OutputsFile = this.commandLine.OutputsFile;
            binder.WixprojectFile = this.commandLine.WixprojectFile;
            binder.BindPaths.AddRange(this.commandLine.BindPaths);
            binder.CabbingThreadCount = this.commandLine.CabbingThreadCount;
            if (this.commandLine.DefaultCompressionLevel.HasValue)
            {
                binder.DefaultCompressionLevel = this.commandLine.DefaultCompressionLevel.Value;
            }
            binder.Ices.AddRange(this.commandLine.Ices);
            binder.SuppressIces.AddRange(this.commandLine.SuppressIces);
            binder.SuppressAclReset = this.commandLine.SuppressAclReset;
            binder.SuppressLayout = this.commandLine.SuppressLayout;
            binder.SuppressValidation = this.commandLine.SuppressValidation;
            binder.PdbFile = this.commandLine.SuppressWixPdb ? null : this.commandLine.PdbFile;
            binder.TempFilesLocation = AppCommon.GetTempLocation();
            binder.WixVariableResolver = wixVariableResolver;

            foreach (IBinderExtension extension in this.binderExtensions)
            {
                binder.AddExtension(extension);
            }

            foreach (IBinderFileManager fileManager in this.fileManagers)
            {
                binder.AddExtension(fileManager);
            }

            // Initialize the localizer.
            Localizer localizer = this.InitializeLocalization(linker.TableDefinitions);
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            wixVariableResolver.Localizer = localizer;
            linker.Localizer = localizer;
            binder.Localizer = localizer;

            // Loop through all the believed object files.
            List<Section> sections = new List<Section>();
            Output output = null;
            foreach (string inputFile in this.commandLine.Files)
            {
                string inputFileFullPath = Path.GetFullPath(inputFile);
                FileFormat format = FileStructure.GuessFileFormatFromExtension(Path.GetExtension(inputFileFullPath));
                bool retry;
                do
                {
                    retry = false;

                    try
                    {
                        switch (format)
                        {
                            case FileFormat.Wixobj:
                                Intermediate intermediate = Intermediate.Load(inputFileFullPath, linker.TableDefinitions, this.commandLine.SuppressVersionCheck);
                                sections.AddRange(intermediate.Sections);
                                break;

                            case FileFormat.Wixlib:
                                Library library = Library.Load(inputFileFullPath, linker.TableDefinitions, this.commandLine.SuppressVersionCheck);
                                AddLibraryLocalizationsToLocalizer(library, this.commandLine.Cultures, localizer);
                                sections.AddRange(library.Sections);
                                break;

                            default:
                                output = Output.Load(inputFileFullPath, this.commandLine.SuppressVersionCheck);
                                break;
                        }
                    }
                    catch (WixUnexpectedFileFormatException e)
                    {
                        format = e.FileFormat;
                        retry = (FileFormat.Wixobj == format || FileFormat.Wixlib == format || FileFormat.Wixout == format); // .wixobj, .wixout and .wixout are supported by light.
                        if (!retry)
                        {
                            Messaging.Instance.OnMessage(e.Error);
                        }
                    }
                } while (retry);
            }

            // Stop processing if any errors were found loading object files.
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // and now for the fun part
            if (null == output)
            {
                OutputType expectedOutputType = OutputType.Unknown;
                if (!String.IsNullOrEmpty(this.commandLine.OutputFile))
                {
                    expectedOutputType = Output.GetOutputType(Path.GetExtension(this.commandLine.OutputFile));
                }

                output = linker.Link(sections, expectedOutputType);

                // If an error occurred during linking, stop processing.
                if (null == output)
                {
                    return;
                }
            }
            else if (0 != sections.Count)
            {
                throw new InvalidOperationException(LightStrings.EXP_CannotLinkObjFilesWithOutpuFile);
            }

            bool tidy = true; // clean up after ourselves by default.
            try
            {
                // only output the xml if its a patch build or user specfied to only output wixout
                string outputFile = this.commandLine.OutputFile;
                string outputExtension = Path.GetExtension(outputFile);
                if (this.commandLine.OutputXml || OutputType.Patch == output.Type)
                {
                    if (String.IsNullOrEmpty(outputExtension) || outputExtension.Equals(".wix", StringComparison.Ordinal))
                    {
                        outputExtension = (OutputType.Patch == output.Type) ? ".wixmsp" : ".wixout";
                        outputFile = Path.ChangeExtension(outputFile, outputExtension);
                    }

                    output.Save(outputFile);
                }
                else // finish creating the MSI/MSM
                {
                    if (String.IsNullOrEmpty(outputExtension) || outputExtension.Equals(".wix", StringComparison.Ordinal))
                    {
                        outputExtension = Output.GetExtension(output.Type);
                        outputFile = Path.ChangeExtension(outputFile, outputExtension);
                    }

                    binder.Bind(output, outputFile);
                }
            }
            catch (WixException we) // keep files around for debugging IDT issues.
            {
                if (we is WixInvalidIdtException)
                {
                    tidy = false;
                }

                throw;
            }
            catch (Exception) // keep files around for debugging unexpected exceptions.
            {
                tidy = false;
                throw;
            }
            finally
            {
                if (null != binder)
                {
                    binder.Cleanup(tidy);
                }
            }

            return;
        }

        private Localizer InitializeLocalization(TableDefinitionCollection tableDefinitions)
        {
            Localizer localizer = null;

            // Instantiate the localizer and load any localization files.
            if (!this.commandLine.SuppressLocalization || 0 < this.commandLine.LocalizationFiles.Count || null != this.commandLine.Cultures || !this.commandLine.OutputXml)
            {
                List<Localization> localizations = new List<Localization>();

                // Load each localization file.
                foreach (string localizationFile in this.commandLine.LocalizationFiles)
                {
                    Localization localization = Localizer.ParseLocalizationFile(localizationFile, tableDefinitions);
                    if (null != localization)
                    {
                        localizations.Add(localization);
                    }
                }

                localizer = new Localizer();
                if (null != this.commandLine.Cultures)
                {
                    // Alocalizations in order specified in cultures.
                    foreach (string culture in this.commandLine.Cultures)
                    {
                        foreach (Localization localization in localizations)
                        {
                            if (culture.Equals(localization.Culture, StringComparison.OrdinalIgnoreCase))
                            {
                                localizer.AddLocalization(localization);
                            }
                        }
                    }
                }
                else // no cultures specified, so try neutral culture and if none of those add all loc files.
                {
                    bool neutralFound = false;
                    foreach (Localization localization in localizations)
                    {
                        if (String.IsNullOrEmpty(localization.Culture))
                        {
                            // If a neutral wxl was provided use it.
                            localizer.AddLocalization(localization);
                            neutralFound = true;
                        }
                    }

                    if (!neutralFound)
                    {
                        // No cultures were specified and no neutral wxl are available, include all of the loc files.
                        foreach (Localization localization in localizations)
                        {
                            localizer.AddLocalization(localization);
                        }
                    }
                }

                // Load localizations provided by extensions with data.
                foreach (IExtensionData data in this.extensionData)
                {
                    Library library = data.GetLibrary(tableDefinitions);
                    if (null != library)
                    {
                        // Load the extension's default culture if it provides one and no cultures were specified.
                        string[] extensionCultures = this.commandLine.Cultures;
                        if (null == extensionCultures && null != data.DefaultCulture)
                        {
                            extensionCultures = new string[] { data.DefaultCulture };
                        }

                        AddLibraryLocalizationsToLocalizer(library, extensionCultures, localizer);
                    }
                }
            }

            return localizer;
        }

        private void AddLibraryLocalizationsToLocalizer(Library library, string[] cultures, Localizer localizer)
        {
            foreach (Localization localization in library.GetLocalizations(cultures))
            {
                localizer.AddLocalization(localization);
            }
        }

        /// <summary>
        /// Prints usage help.
        /// </summary>
        private void PrintHelp()
        {
            string lightArgs = LightStrings.CommandLineArguments;

            Console.WriteLine(String.Format(LightStrings.HelpMessage, lightArgs));
        }
    }
}
