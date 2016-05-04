// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuild.Tools.Md2html
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using MarkdownSharp;

    internal class Program
    {
        private static void Main(string[] args)
        {
            CommandLine commandLine;
            if (!CommandLine.TryParseArguments(args, out commandLine))
            {
                ShowHelp();
                Environment.Exit(1);
            }

            string text = String.Empty;

            if (!String.IsNullOrEmpty(commandLine.Header))
            {
                text = File.ReadAllText(commandLine.Header);
            }

            //var mdOptions = new MarkdownOptions { AutoHyperlink = options.AutoHyperlink, AutoNewlines = options.AutoNewLines, EmptyElementSuffix = options.OutputXHTML ? " />" : ">", EncodeProblemUrlCharacters = options.EncodeProblemUrlCharacters, LinkEmails = !options.DontLinkEmails, StrictBoldItalic = options.StrictBoldItalic };
            var mdOptions = new MarkdownOptions { EmptyElementSuffix = " />" };
            foreach (var fileName in commandLine.Files)
            {
                var markdown = new Markdown(mdOptions);
                text += markdown.Transform(File.ReadAllText(fileName));
            }

            if (!String.IsNullOrEmpty(commandLine.Footer))
            {
                text += File.ReadAllText(commandLine.Footer);
            }

            var preprocessor = new Preprocessor(commandLine.PreprocessorDefines);
            string[] lines = text.Replace("\r", String.Empty).Split(new char[] { '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; ++i)
            {
                lines[i] = preprocessor.Preprocess(i + 1, lines[i]);
            }

            text = String.Join(Environment.NewLine, lines);

            if (!String.IsNullOrEmpty(commandLine.Output))
            {
                string outputPath = Path.GetFullPath(commandLine.Output);

                string outputFolder = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                using (TextWriter output = new StreamWriter(outputPath))
                {
                    output.WriteLine(text);
                }
            }
            else
            {
                Console.Write(text);
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("md2html.exe [-?] [-header file] [-footer file] [-d variable=value] [-out file] file1 file2 ... fileN");
        }
    }

    /// <summary>
    /// Command-line parsing.
    /// </summary>
    internal class CommandLine
    {
        private CommandLine()
        {
            this.Files = new List<string>();
            this.PreprocessorDefines = new List<string>();
        }

        /// <summary>
        /// List of files to process.
        /// </summary>
        public List<string> Files { get; private set; }

        public List<string> PreprocessorDefines { get; private set; }

        public string Header { get; set; }

        public string Footer { get; set; }

        public string Output { get; set; }

        /// <summary>
        /// Parses the command-line.
        /// </summary>
        /// <param name="args">Arguments from command-line.</param>
        /// <param name="messaging">Messaging object to send errors.</param>
        /// <param name="commandLine">Command line object created from command-line arguments</param>
        /// <returns>True if command-line is parsed, false if a failure was occurred.</returns>
        public static bool TryParseArguments(string[] args, out CommandLine commandLine)
        {
            bool success = true;

            commandLine = new CommandLine();

            for (int i = 0; i < args.Length; ++i)
            {
                if ('-' == args[i][0] || '/' == args[i][0])
                {
                    string arg = args[i].Substring(1).ToLowerInvariant();
                    if ("?" == arg || "help" == arg)
                    {
                        return false;
                    }
                    else if ("d" == arg || "define" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine(String.Format("Missing preprocessor definition for '-define' option. Provide a preprocessor definition in the form of: name or name=variable."));
                            success = false;
                        }
                        else
                        {
                            commandLine.PreprocessorDefines.Add(args[i]);
                        }
                    }
                    else if ("f" == arg || "footer" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine(String.Format("Missing file specification for '-footer' option. Provide a valid path to a file."));
                            success = false;
                        }
                        else
                        {
                            string sourcePath = Path.GetFullPath(args[i]);
                            if (!System.IO.File.Exists(sourcePath))
                            {
                                Console.Error.WriteLine(String.Format("Footer file '{0}' could not be found.", sourcePath));
                                success = false;
                            }
                            else
                            {
                                commandLine.Footer = sourcePath;
                            }
                        }
                    }
                    else if ("h" == arg || "header" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine(String.Format("Missing file specification for '-header' option. Provide a valid path to a file."));
                            success = false;
                        }
                        else
                        {
                            string sourcePath = Path.GetFullPath(args[i]);
                            if (!System.IO.File.Exists(sourcePath))
                            {
                                Console.Error.WriteLine(String.Format("Header file '{0}' could not be found.", sourcePath));
                                success = false;
                            }
                            else
                            {
                                commandLine.Header = sourcePath;
                            }
                        }
                    }
                    else if ("o" == arg || "out" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine(String.Format("Missing file specification for '-out' option."));
                            success = false;
                        }
                        else
                        {
                            string outputPath = Path.GetFullPath(args[i]);
                            commandLine.Output = outputPath;
                        }
                    }
                }
                else
                {
                    string[] file = args[i].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    string sourcePath = Path.GetFullPath(file[0]);
                    if (!System.IO.File.Exists(sourcePath))
                    {
                        Console.Error.WriteLine(String.Format("Source file '{0}' could not be found.", sourcePath));
                        success = false;
                    }
                    else
                    {
                        commandLine.Files.Add(sourcePath);
                    }
                }
            }

            if (0 == commandLine.Files.Count)
            {
                Console.Error.WriteLine(String.Format("No inputs specified. Specify at least one file."));
                success = false;
            }

            return success;
        }
    }

    internal class Preprocessor
    {
        private static readonly Regex ParseVariables = new Regex(@"\$\([a-zA-Z_][a-zA-Z0-9_\-\.]*\)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        public Preprocessor(IEnumerable<string> defines)
        {
            this.Variables = new Dictionary<string, string>();
            foreach (string define in defines)
            {
                string[] defineSplit = define.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                this.Variables.Add(defineSplit[0], defineSplit.Length > 1 ? defineSplit[1] : null);
            }
        }

        public IDictionary<string, string> Variables { get; private set; }

        public string Preprocess(int lineNumber, string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                int replaceCount = 0;

                Match m = Preprocessor.ParseVariables.Match(text);
                while (m.Success)
                {
                    int offset = 0;
                    string beginning = text.Substring(0, m.Index);
                    string variableName = text.Substring(m.Index + 2, m.Length - 3);
                    string end = text.Substring(m.Index + m.Length);

                    // This is an arbitrary upper limit for variable replacements to prevent
                    // inifite loops.
                    if (replaceCount > 20)
                    {
                        Console.Error.WriteLine("Infinite loop in preprocessor variable: {0} on line: {1}, column: {2}", variableName, lineNumber, m.Index + 1);
                        break;
                    }

                    string variableValue;
                    if (this.Variables.TryGetValue(variableName, out variableValue))
                    {
                        text = String.Concat(beginning, variableValue, end);
                    }
                    else // skip the entire preprocess variable because we couldn't replace it.
                    {
                        Console.Error.WriteLine("Unknown preprocessor variable: {0} on line: {1}, column: {2}", variableName, lineNumber, m.Index + 1);
                        offset = m.Length;
                    }

                    m = Preprocessor.ParseVariables.Match(text, m.Index + offset);
                }
            }

            return text;
        }
    }
}
