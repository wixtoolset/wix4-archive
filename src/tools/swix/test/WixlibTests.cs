//-------------------------------------------------------------------------------------------------
// <copyright file="WixlibTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using WixToolset.Simplified.Test.Utility;
using Xunit;

namespace WixToolset.Simplified.Test
{
    public class WixlibTests
    {
        [Fact]
        public void Simple()
        {
            var diff = WixLibUtility.Diff("Data\\wixlib\\simple\\Expected-Wixlib.xml", "Data\\wixlib\\simple\\Expected-Wixlib.xml");
            Assert.Empty(diff);
        }

        [Fact]
        public void Condition()
        {
            IEnumerable<string> files = LoadSourceFiles("Data\\wixlib\\condition");
            string output = CreateOutputFolder("test_condition", "condition", ".wixlib");

            var swc = new SwcTool("neutral", "en-us", null, @"Data\content");
            swc.Compile(files, output);
            Assert.Empty(swc.Errors);

            var diff = WixLibUtility.Diff("Data\\wixlib\\condition\\Expected-Wixlib.xml", output);
            Assert.Empty(diff);
        }

        [Fact]
        public void FileSearch()
        {
            IEnumerable<string> files = LoadSourceFiles("Data\\wixlib\\filesearch");
            string output = CreateOutputFolder("test_filesearch", "filesearch", ".wixlib");

            var swc = new SwcTool("neutral", "en-us", null, @"Data\content");
            swc.Compile(files, output);
            Assert.Empty(swc.Errors);

            var diff = WixLibUtility.Diff("Data\\wixlib\\filesearch\\Expected-Wixlib.xml", output);
            Assert.Empty(diff);
        }

        [Fact]
        public void Inproc()
        {
            IEnumerable<string> files = LoadSourceFiles("Data\\wixlib\\inproc");
            string output = CreateOutputFolder("test_inproc", "inproc", ".wixlib");

            var swc = new SwcTool("neutral", "en-us", null, @"Data\content");
            swc.Compile(files, output);
            Assert.Empty(swc.Errors);

            var diff = WixLibUtility.Diff("Data\\wixlib\\inproc\\Expected-Wixlib.xml", output);
            Assert.Empty(diff);
        }

        [Fact]
        public void Ngen()
        {
            IEnumerable<string> files = LoadSourceFiles("Data\\wixlib\\ngen");
            string output = CreateOutputFolder("test_ngen", "ngen", ".wixlib");

            var swc = new SwcTool("neutral", "en-us", null, @"Data\content");
            swc.Compile(files, output);
            Assert.Empty(swc.Errors);

            var diff = WixLibUtility.Diff("Data\\wixlib\\ngen\\Expected-Wixlib.xml", output);
            Assert.Empty(diff);
        }

        private static IEnumerable<string> LoadSourceFiles(string folder)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(folder, "*.swr"));
            files.AddRange(Directory.GetFiles(folder, "*.swx"));

            return files;
        }

        private string CreateOutputFolder(string folder, string prefix, string extension)
        {
            string outputFolder = Path.Combine(folder, prefix);
            string output = Path.Combine(outputFolder, String.Concat(prefix, extension));
            if (Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.Delete(outputFolder, true);
                }
                catch (IOException)
                {
                }
            }

            Directory.CreateDirectory(outputFolder);
            return output;
        }
    }
}
