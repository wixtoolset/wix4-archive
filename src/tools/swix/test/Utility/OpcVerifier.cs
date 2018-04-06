// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WixToolset.Dtf.Compression.Zip;
using Xunit;

namespace WixToolset.Simplified.Test.Utility
{
    /// <summary>
    /// Base class used to verify Open Package Convention (OPC) file formats.
    /// </summary>
    internal abstract class OpcVerifier
    {
        private static readonly char[] SplitSemicolons = new char[] { ';' };

        public string Architecture { get; set; }

        public string Language { get; set; }

        public string SearchPath { get; set; }

        public IDictionary<string, string> VerifyXml { get; set; }

        public ISet<string> Files { get; set; }

        public ISet<string> SourceFiles { get; set; }

        virtual public OpcVerifier Load(string folder, string[] expectedFiles)
        {
            this.VerifyXml = new Dictionary<string, string>();

            this.Files = new SortedSet<string>(LoadExpectedFiles(Path.Combine(folder, "Expected-Files.txt")), StringComparer.OrdinalIgnoreCase);

            this.SourceFiles = new SortedSet<string>(LoadSourceFiles(folder), StringComparer.OrdinalIgnoreCase);

            foreach (var expectedFile in expectedFiles)
            {
                this.Files.Add(expectedFile);

                string value;
                if (TryNormalizeXmlFile(Path.Combine(folder, String.Concat("Expected-", expectedFile)), out value))
                {
                    this.VerifyXml.Add(expectedFile, value);
                }
            }

            return this;
        }

        virtual public OpcVerifier Verify(string folder, string prefix, string extension)
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

            var swc = new SwcTool(this.Architecture ?? "neutral", this.Language ?? "en-us", null, this.SearchPath ?? @"Data\content");
            swc.Compile(this.SourceFiles, output);
            Assert.Empty(swc.Errors);

            string uncompressedFolder = Path.Combine(outputFolder, "uncompressed");
            Uncompress(output, uncompressedFolder);

            foreach (string file in this.VerifyXml.Keys)
            {
                string path = Path.Combine(uncompressedFolder, file);
                if (File.Exists(path))
                {
                    string normal = XmlUtility.NormalizeDocument(path);
                    Assert.Equal(this.VerifyXml[file], normal, StringComparer.Ordinal);
                }
            }

            string[] extractedFiles = Directory.GetFiles(uncompressedFolder, "*.*", SearchOption.AllDirectories);
            SortedSet<string> missing = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            SortedSet<string> added = new SortedSet<string>(extractedFiles, StringComparer.OrdinalIgnoreCase);

            foreach (string file in this.Files)
            {
                missing.Add(Path.Combine(uncompressedFolder, file));
            }

            added.ExceptWith(missing);
            missing.ExceptWith(extractedFiles);
            Assert.Empty(added);
            Assert.Empty(missing);

            return this;
        }

        private static IEnumerable<string> LoadExpectedFiles(string path)
        {
            List<string> files = new List<string>();
            if (File.Exists(path))
            {
                files.AddRange(File.ReadAllLines(path).SelectMany(line => line.Split(SplitSemicolons, StringSplitOptions.RemoveEmptyEntries)));
            }

            return files;
        }

        private static IEnumerable<string> LoadSourceFiles(string folder)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(folder, "*.swr"));
            files.AddRange(Directory.GetFiles(folder, "*.swx"));

            return files;
        }

        private static bool TryNormalizeXmlFile(string path, out string content)
        {
            content = null;

            if (File.Exists(path))
            {
                content = XmlUtility.NormalizeDocument(path);
            }

            return content != null;
        }

        private static void Uncompress(string path, string output)
        {
            ZipInfo z = new ZipInfo(path);
            z.Unpack(output);
        }
    }
}
