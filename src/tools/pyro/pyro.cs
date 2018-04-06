// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The pyro patch builder application.
    /// </summary>
    public sealed class Pyro
    {
        private PyroCommandLine commandLine;
        private IEnumerable<IBinderExtension> binderExtensions;
        private IEnumerable<IBinderFileManager> fileManagers;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Arguments to pyro.</param>
        /// <returns>0 if sucessful, otherwise 1.</returns>
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("PYRO", "pyro.exe").Display += AppCommon.ConsoleDisplayMessage;

            Pyro pyro = new Pyro();
            return pyro.Execute(args);
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
                        Console.WriteLine(PyroStrings.HelpMessage);
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
            this.commandLine = new PyroCommandLine();
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

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        private void Run()
        {
            WixVariableResolver wixVariableResolver = new WixVariableResolver();

            // Initialize the binder from the command line.
            WixToolset.Binder binder = new WixToolset.Binder();
            binder.CabCachePath = this.commandLine.CabCachePath;
            //binder.DeltaBinaryPatch = this.commandLine.Delta;
            //binder.ContentsFile = this.commandLine.ContentsFile;
            //binder.BuiltOutputsFile = this.commandLine.BuiltOutputsFile;
            //binder.OutputsFile = this.commandLine.OutputsFile;
            //binder.WixprojectFile = this.commandLine.WixprojectFile;
            //binder.BindPaths.AddRange(this.commandLine.BindPaths);
            binder.TargetBindPaths.AddRange(this.commandLine.TargetBindPaths);
            binder.UpdatedBindPaths.AddRange(this.commandLine.UpdatedBindPaths);
            //binder.CabbingThreadCount = this.commandLine.CabbingThreadCount;
            //binder.DefaultCompressionLevel = this.commandLine.DefaultCompressionLevel;
            //binder.ExactAssemblyVersions = this.commandLine.ExactAssemblyVersions;
            binder.SuppressAclReset = this.commandLine.SuppressAclReset;
            //binder.SuppressLayout = this.commandLine.SuppressLayout;
            binder.SuppressValidation = true;
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

            // Create and configure the patch
            Patch patch = new Patch();
            patch.Load(this.commandLine.InputFile);
            patch.AttachTransforms(this.commandLine.PatchTransforms);

            bool tidy = true; // clean up after ourselves by default.
            try
            {
                // Bind the patch to an msp.
                binder.Bind(patch.PatchOutput, this.commandLine.OutputFile);
            }
            catch (WixException we)
            {
                if (we is WixInvalidIdtException)
                {
                    tidy = false;
                }

                throw;
            }
            catch (Exception)
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

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        private void OnMessage(MessageEventArgs mea)
        {
            Messaging.Instance.OnMessage(mea);
        }
    }
}
