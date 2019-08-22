// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace WixToolset.Simplified.Test.Utility
{
    internal class SwcTool
    {
        private readonly string architecture;
        private readonly string language;
        private readonly string type;
        private readonly string searchPath;

        public SwcTool()
        {
        }

        public SwcTool(string architecture, string language, string type, string searchPath)
        {
            this.architecture = architecture;
            this.language = language;
            this.type = type;
            this.searchPath = searchPath;
        }

        public string CommandLine { get; private set; }

        public List<string> Output { get; private set; }

        public List<string> Errors { get; private set; }

        public SwcTool Help()
        {
            this.Output = ToolUtility.RunTool("swc.exe", "-?");
            this.Errors = ToolUtility.GetErrors(this.Output, String.Empty, String.Empty);
            return this;
        }

        public SwcTool Compile(IEnumerable<string> sourceFiles, string outputPath)
        {
            StringBuilder cmdline = new StringBuilder();
            cmdline.AppendFormat("-out {0}", outputPath);

            if (!String.IsNullOrEmpty(this.architecture))
            {
                cmdline.AppendFormat(" -arch {0}", this.architecture);
            }

            if (!String.IsNullOrEmpty(this.language))
            {
                cmdline.AppendFormat(" -lang {0}", this.language);
            }

            if (!String.IsNullOrEmpty(this.type))
            {
                cmdline.AppendFormat(" -type {0}", this.type);
            }

            if (!String.IsNullOrEmpty(this.searchPath))
            {
                cmdline.AppendFormat(" -sp {0}", this.searchPath);
            }

            foreach (string sourceFile in sourceFiles)
            {
                cmdline.AppendFormat(" {0}", sourceFile);
            }

            this.CommandLine = cmdline.ToString();
            this.Output = ToolUtility.RunTool("swc.exe", this.CommandLine);
            this.Errors = ToolUtility.GetErrors(this.Output, String.Empty, String.Empty);
            return this;
        }
    }
}
