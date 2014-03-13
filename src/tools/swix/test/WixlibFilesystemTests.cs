//-------------------------------------------------------------------------------------------------
// <copyright file="WixlibFilesystemTests.cs" company="Outercurve Foundation">
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
    public class WixlibFilesystemTests
    {
        [Fact]
        public void SharedFolder_File()
        {
            IEnumerable<string> files = LoadSourceFiles(@"Data\wixlib.filesystem\sharedfolder_file");
            string output = CreateOutputFolder("test_wixlib_filesystem_sharedfolder_file", "sharedfolder_file", ".wixlib");

            var swc = new SwcTool("neutral", "en-us", null, @"Data\content");
            swc.Compile(files, output);
            Assert.Empty(swc.Errors);

            var diff = WixLibUtility.Diff(@"Data\wixlib.filesystem\sharedfolder_file\Expected-Wixlib.xml", output);
            Assert.Empty(diff);
        }

        [Fact]
        public void SharedFolder_Parent()
        {
            IEnumerable<string> files = LoadSourceFiles("Data\\wixlib.filesystem\\sharedfolder_parent");
            string output = CreateOutputFolder("test_wixlib_filesystem_sharedfolder_parent", "sharedfolder_parent", ".wixlib");

            var swc = new SwcTool("neutral", "en-us", null, @"Data\content");
            swc.Compile(files, output);
            Assert.Empty(swc.Errors);

            var diff = WixLibUtility.Diff("Data\\wixlib.filesystem\\sharedfolder_parent\\Expected-Wixlib.xml", output);
            Assert.Empty(diff);
        }

        [Fact]
        public void SharedFolder_Ref()
        {
            IEnumerable<string> files = LoadSourceFiles("Data\\wixlib.filesystem\\sharedfolder_ref");
            string output = CreateOutputFolder("test_wixlib_filesystem_sharedfolder_ref", "sharedfolder_ref", ".wixlib");

            var swc = new SwcTool("neutral", "en-us", null, @"Data\content");
            swc.Compile(files, output);
            Assert.Empty(swc.Errors);

            var diff = WixLibUtility.Diff("Data\\wixlib.filesystem\\sharedfolder_ref\\Expected-Wixlib.xml", output);
            Assert.Empty(diff);
        }

        [Fact]
        public void SharedFolder_TwoGroups()
        {
            IEnumerable<string> files = LoadSourceFiles(@"Data\wixlib.filesystem\sharedfolder_twogroups");
            string output = CreateOutputFolder("test_wixlib_filesystem_sharedfolder_twogroups", "sharedfolder_twogroups", ".wixlib");

            var swc = new SwcTool("neutral", "en-us", null, @"Data\content");
            swc.Compile(files, output);
            Assert.Empty(swc.Errors);

            var diff = WixLibUtility.Diff(@"Data\wixlib.filesystem\sharedfolder_twogroups\Expected-Wixlib.xml", output);
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
