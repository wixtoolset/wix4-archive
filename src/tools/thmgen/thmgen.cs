//-------------------------------------------------------------------------------------------------
// <copyright file="thmgen.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The ThmUtil data generator application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml.Linq;
    using WixToolset.Data;

    /// <summary>
    /// The main entry point for ThmGen.
    /// </summary>
    public sealed class ThmGen
    {
        private ThmGenCommandLine commandLine;
        private XNamespace NameSpace = "http://wixtoolset.org/schemas/v4/thmutil";

        /// <summary>
        /// The main entry point for ThmGen.
        /// </summary>
        /// <param name="args">Command-line arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("THMG", "thmgen.exe").Display += AppCommon.ConsoleDisplayMessage;

            ThmGen thmgen = new ThmGen();
            return thmgen.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Command-line arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            try
            {
                this.commandLine = new ThmGenCommandLine();
                string[] unprocessed = commandLine.Parse(args);

                if (!Messaging.Instance.EncounteredError)
                {
                    if (this.commandLine.ShowLogo)
                    {
                        AppCommon.DisplayToolHeader();
                    }

                    if (this.commandLine.ShowHelp)
                    {
                        Console.WriteLine(ThmGenStrings.HelpMessage);
                        AppCommon.DisplayToolFooter();
                    }
                    else
                    {
                        foreach (string arg in unprocessed)
                        {
                            Messaging.Instance.OnMessage(WixWarnings.UnsupportedCommandLineArgument(arg));
                        }

                        this.GenerateThemeHelpers();
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

        private void GenerateThemeHelpers()
        {
            var outputHeaderFile = Path.ChangeExtension(this.commandLine.OutputFile ?? "theme.h", ".h");
            var outputModuleFile = Path.ChangeExtension(outputHeaderFile, ".cpp");
            var prefix = this.commandLine.Prefix ?? String.Empty;
            var pch = this.commandLine.PchName ?? "stdafx.h";

            // load the theme file and find all the controls and pages with a Name attribute
            var doc = XDocument.Load(this.commandLine.InputFile);
            var named = doc.Descendants().Elements().Where(xelem => xelem.Attribute("Name") != null);
            var controls = named.Where(xelem => xelem.Name != this.NameSpace + "Page");
            var pages = named.Where(xelem => xelem.Name == this.NameSpace + "Page");

            // theme.h
            var templateControlsDeclaration = GetTemplateFromResource("WixToolset.Tools.Templates.Controls.h");
            var templatePagesDeclaration = GetTemplateFromResource("WixToolset.Tools.Templates.Pages.h");

            var controlsDeclaration = String.Join(",\n", GetDeclaration(controls));
            var pagesDeclaration = String.Join(",\n", GetDeclaration(pages));

            File.WriteAllText(outputHeaderFile, String.Format(templateControlsDeclaration, prefix, controlsDeclaration));
            File.AppendAllText(outputHeaderFile, String.Format(templatePagesDeclaration, prefix, pagesDeclaration));

            // theme.cpp
            var templateControlsInitialization = GetTemplateFromResource("WixToolset.Tools.Templates.Controls.cpp");
            var templatePagesInitializations = GetTemplateFromResource("WixToolset.Tools.Templates.Pages.cpp");

            var controlsInitialization = String.Join(",\n", GetControlsInitialization(controls, prefix));
            var pagesInitialization = String.Join(",\n", GetPagesInitialization(pages));

            File.WriteAllText(outputModuleFile, String.Format(templateControlsInitialization, prefix, controlsInitialization, pch));
            File.AppendAllText(outputModuleFile, String.Format(templatePagesInitializations, prefix, pagesInitialization));
        }

        private static string GetTemplateFromResource(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static IEnumerable<string> GetDeclaration(IEnumerable<XElement> elems)
        {
            return elems.Select(elem => elem.Attribute("Name").Value);
        }

        private static IEnumerable<string> GetPagesInitialization(IEnumerable<XElement> pages)
        {
            return pages.Select(page =>
            {
                var name = page.Attribute("Name").Value;
                return String.Format("L\"{0}\"", name);
            });
        }

        private static IEnumerable<string> GetControlsInitialization(IEnumerable<XElement> controls, string prefix)
        {
            return controls.Select(control =>
            {
                var name = control.Attribute("Name").Value;
                return String.Format("{{ {0}Controls::{1}, L\"{1}\" }}", prefix, name);
            });
        }
    }
}
