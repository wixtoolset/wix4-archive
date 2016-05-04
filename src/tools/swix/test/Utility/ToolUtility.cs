// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WixToolset.Simplified.Test.Utility
{
    /// <summary>
    /// Utilities for working with tools.
    /// </summary>
    internal static class ToolUtility
    {
        private static readonly Regex WixErrorMessage = new Regex(@"^(.*: )?error [^:\d]*(?<errorNumber>\d*).*:.*$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex WixWarningMessage = new Regex(@"^.*: warning [^:\d]*(?<warningNumber>\d*).*:.*$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Get the unexpected errors and warnings from an List<string> of output strings.
        /// </summary>
        /// <param name="output">The output strings.</param>
        /// <param name="expectedErrors">The expected errors, semicolon delimited.</param>
        /// <param name="expectedWarnings">The expected warnings, semicolon delimited.</param>
        /// <returns>The unexpected warnings and errors.</returns>
        public static List<string> GetErrors(List<string> output, string expectedErrors, string expectedWarnings)
        {
            Dictionary<int, string> expectedErrorNumbers = new Dictionary<int, string>();
            Dictionary<int, string> expectedWarningNumbers = new Dictionary<int, string>();
            List<string> errors = new List<string>();

            if (expectedErrors.Length > 0)
            {
                foreach (string error in expectedErrors.Split(';'))
                {
                    int errorNumber = Convert.ToInt32(error, CultureInfo.InvariantCulture);

                    expectedErrorNumbers.Add(errorNumber, null);
                }
            }

            if (expectedWarnings.Length > 0)
            {
                foreach (string warning in expectedWarnings.Split(';'))
                {
                    int warningNumber = Convert.ToInt32(warning, CultureInfo.InvariantCulture);

                    expectedWarningNumbers.Add(warningNumber, null);
                }
            }

            bool treatAllLinesAsErrors = false;
            foreach (string line in output)
            {
                if (treatAllLinesAsErrors)
                {
                    errors.Add(line);
                }
                else
                {
                    Match errorMatch = WixErrorMessage.Match(line);
                    Match warningMatch = WixWarningMessage.Match(line);

                    if (errorMatch.Success)
                    {
                        int errorNumber = 0;
                        Int32.TryParse(errorMatch.Groups["errorNumber"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out errorNumber);

                        // error number 1 is special because it includes a stack trace which much be kept in the error output
                        if (errorNumber == 1)
                        {
                            treatAllLinesAsErrors = true;
                        }

                        if (expectedErrorNumbers.ContainsKey(errorNumber))
                        {
                            expectedErrorNumbers[errorNumber] = String.Empty;
                        }
                        else
                        {
                            errors.Add(line);
                        }
                    }
                    else if (line.StartsWith("Unhandled Exception:")) // .NET error
                    {
                        errors.Add(line);
                    }

                    if (warningMatch.Success)
                    {
                        int warningNumber = 0;
                        Int32.TryParse(warningMatch.Groups["warningNumber"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out warningNumber);

                        if (expectedWarningNumbers.ContainsKey(warningNumber))
                        {
                            expectedWarningNumbers[warningNumber] = String.Empty;
                        }
                        else
                        {
                            errors.Add(line);
                        }
                    }
                }
            }

            foreach (KeyValuePair<int, string> entry in expectedErrorNumbers)
            {
                if (entry.Value == null)
                {
                    errors.Add(String.Format(CultureInfo.InvariantCulture, "Expected error {0} not found.", entry.Key));
                }
            }

            foreach (KeyValuePair<int, string> entry in expectedWarningNumbers)
            {
                if (entry.Value == null)
                {
                    errors.Add(String.Format(CultureInfo.InvariantCulture, "Expected warning {0} not found.", entry.Key));
                }
            }

            return errors;
        }

        /// <summary>
        /// Run a tool with the given file name and command line.
        /// </summary>
        /// <param name="toolFile">The tool's file name.</param>
        /// <param name="commandLine">The command line.</param>
        /// <returns>An List<string> of output strings.</returns>
        public static List<string> RunTool(string toolFile, string commandLine)
        {
            // The returnCode variable doesn't get used but it must be created to pass as an argument
            int returnCode;
            return ToolUtility.RunTool(toolFile, commandLine, out returnCode);
        }

        /// <summary>
        /// Run a tool with the given file name and command line.
        /// </summary>
        /// <param name="toolFile">The tool's file name.</param>
        /// <param name="commandLine">The command line.</param>
        /// <param name="returnCode">Store the return code of the process.</param>
        /// <returns>An List<string> of output strings.</returns>
        public static List<string> RunTool(string toolFile, string commandLine, out int returnCode)
        {
            // Expand environment variables
            toolFile = Environment.ExpandEnvironmentVariables(toolFile);
            commandLine = Environment.ExpandEnvironmentVariables(commandLine);

            List<string> output = new List<string>();
            Process process = null;

            // The returnCode must get initialized outside of the try block
            returnCode = 0;

            try
            {
                process = new Process();
                process.StartInfo.FileName = toolFile;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                // run the tool
                output.Add(String.Format(CultureInfo.InvariantCulture, "Command: {0} {1}", toolFile, commandLine));
                process.StartInfo.Arguments = commandLine;
                process.Start();

                string line;
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    output.Add(line);
                }

                // WiX tools log all output to stdout but .NET may put error output in stderr
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    output.Add(line);
                }

                process.WaitForExit();
                returnCode = process.ExitCode;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return output;
        }
    }
}
