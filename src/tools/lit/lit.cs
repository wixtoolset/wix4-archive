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
    /// Main entry point for library tool.
    /// </summary>
    public sealed class Lit
    {
        LitCommandLine commandLine;
        private IEnumerable<IExtensionData> extensionData;
        private IEnumerable<IBinderFileManager> fileManagers;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Commandline arguments for lit.</param>
        /// <returns>Returns non-zero error code in the case of an error.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("LIT", "lit.exe").Display += AppCommon.ConsoleDisplayMessage;

            Lit lit = new Lit();
            return lit.Execute(args);
        }

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
                        Console.WriteLine(LitStrings.HelpMessage);
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
            this.commandLine = new LitCommandLine();
            string[] unparsed = this.commandLine.Parse(args);
            if (Messaging.Instance.EncounteredError)
            {
                return unparsed;
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
                unparsed = dce.ParseCommandLine(unparsed);
            }

            // File resolution command line processing.
            this.fileManagers = extensionManager.Create<IBinderFileManager>();
            if (this.fileManagers.Any())
            {
                foreach (IExtensionCommandLine fme in this.fileManagers.Where(e => e is IExtensionCommandLine).Cast<IExtensionCommandLine>())
                {
                    fme.MessageHandler = Messaging.Instance;
                    unparsed = fme.ParseCommandLine(unparsed);
                }
            }
            else // there are no extension file managers so add the default one.
            {
                List<IBinderFileManager> defaultBinderFileManager = new List<IBinderFileManager>();
                defaultBinderFileManager.Add(new BinderFileManager());

                this.fileManagers = defaultBinderFileManager;
            }

            return commandLine.ParsePostExtensions(unparsed);
        }

        /// <summary>
        /// Create the library.
        /// </summary>
        private void Run()
        {
            // Create the librarian and add the extension data.
            Librarian librarian = new Librarian();

            foreach (IExtensionData data in this.extensionData)
            {
                librarian.AddExtensionData(data);
            }

            // Add the sections to the librarian
            List<Section> sections = new List<Section>();
            foreach (string file in this.commandLine.Files)
            {
                string inputFile = Path.GetFullPath(file);
                FileFormat format = FileStructure.GuessFileFormatFromExtension(Path.GetExtension(inputFile));
                bool retry;
                do
                {
                    retry = false;

                    try
                    {
                        switch (format)
                        {
                            case FileFormat.Wixobj:
                                Intermediate intermediate = Intermediate.Load(inputFile, librarian.TableDefinitions, this.commandLine.SuppressVersionCheck);
                                sections.AddRange(intermediate.Sections);
                                break;

                            default:
                                Library loadedLibrary = Library.Load(inputFile, librarian.TableDefinitions, this.commandLine.SuppressVersionCheck);
                                sections.AddRange(loadedLibrary.Sections);
                                break;
                        }
                    }
                    catch (WixUnexpectedFileFormatException e)
                    {
                        format = e.FileFormat;
                        retry = (FileFormat.Wixobj == format || FileFormat.Wixlib == format); // .wixobj and .wixout are supported by lit.
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
            Library library = librarian.Combine(sections);

            // Add any localization files and save the library output if an error did not occur
            if (null != library)
            {
                foreach (string localizationFile in this.commandLine.LocalizationFiles)
                {
                    Localization localization = Localizer.ParseLocalizationFile(localizationFile, librarian.TableDefinitions);
                    if (null != localization)
                    {
                        library.AddLocalization(localization);
                    }
                }

                // If there was an error adding localization files, then bail.
                if (Messaging.Instance.EncounteredError)
                {
                    return;
                }

                LibraryBinaryFileResolver resolver = null;
                if (this.commandLine.BindFiles)
                {
                    resolver = new LibraryBinaryFileResolver();
                    resolver.FileManagers = this.fileManagers;
                    resolver.VariableResolver = new WixVariableResolver();

                    BinderFileManagerCore core = new BinderFileManagerCore();
                    core.AddBindPaths(this.commandLine.BindPaths, BindStage.Normal);

                    foreach (IBinderFileManager fileManager in resolver.FileManagers)
                    {
                        fileManager.Core = core;
                    }
                }

                library.Save(this.commandLine.OutputFile, resolver);
            }
        }

        /// <summary>
        /// File resolution mechanism to create binary library.
        /// </summary>
        private class LibraryBinaryFileResolver : ILibraryBinaryFileResolver
        {
            public IEnumerable<IBinderFileManager> FileManagers { get; set; }

            public WixVariableResolver VariableResolver { get; set; }

            public string Resolve(SourceLineNumber sourceLineNumber, string table, string path)
            {
                string resolvedPath = this.VariableResolver.ResolveVariables(sourceLineNumber, path, false);
                foreach (IBinderFileManager fileManager in this.FileManagers)
                {
                    string finalPath = fileManager.ResolveFile(resolvedPath, table, sourceLineNumber, BindStage.Normal);
                    if (!String.IsNullOrEmpty(finalPath))
                    {
                        return finalPath;
                    }
                }

                return null;
            }
        }
    }
}
