// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.WixUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using WixToolset;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using Xunit;

    public class InlineDirectorySyntaxFixture : WixUnitTestBaseFixture
    {
        [Fact]
        public void CreateProtectedDirectoryFromInlineSyntax()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='CreateProtectedDirectoryFromInlineSyntax' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='BinFolder'/></Product>" +
                                            @"<Fragment><Directory Id='protected BinFolder' Name='TARGETDIR:\foo\bar\bin\' /></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            Output output = linker.Link(intermediate.Sections, OutputType.Product);

            RowIndexedList<Row> directoryRows = new RowIndexedList<Row>(output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Directory")).SelectMany(d => d.Rows));
            Assert.Equal(4, directoryRows.Count);

            Row binFolder = directoryRows.Get("BinFolder");
            Row barFolder = directoryRows.Get((string)binFolder[1]);
            Row fooFolder = directoryRows.Get((string)barFolder[1]);
            Row targetDir = directoryRows.Get((string)fooFolder[1]);

            Assert.Equal(AccessModifier.Protected, binFolder.Access);
            Assert.Equal(AccessModifier.Private, barFolder.Access);
            Assert.Equal(AccessModifier.Private, fooFolder.Access);
            Assert.Equal(AccessModifier.Public, targetDir.Access);

            Assert.Equal("bin", binFolder[2]);
            Assert.Equal("bar", barFolder[2]);
            Assert.Equal("foo", fooFolder[2]);
            Assert.Equal("SourceDir", targetDir[2]);
        }

        [Fact]
        public void ComponentUsesInlineDirectorySyntax()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='ComponentUsesInlineDirectorySyntax' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><Feature Id='Feature1'><ComponentRef Id='Component1'/></Feature></Product>" +
                                            @"<Fragment><Component Id='protected Component1' Directory='BinFolder:\comp\' /></Fragment>" +
                                            @"<Fragment><Directory Id='protected BinFolder' Name='TARGETDIR:\foo\bar\bin\' /></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            Output output = linker.Link(intermediate.Sections, OutputType.Product);

            RowIndexedList<Row> directoryRows = new RowIndexedList<Row>(output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Directory")).SelectMany(d => d.Rows));
            ComponentRow componentRow = output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Component")).SelectMany(c => c.Rows).Cast<ComponentRow>().Single();
            Row compFolder = directoryRows.Get(componentRow.Directory);

            Assert.NotNull(compFolder);
            Assert.Equal(AccessModifier.Private, compFolder.Access);
            Assert.Equal("BinFolder", compFolder[1]);
            Assert.Equal("comp", compFolder[2]);
        }

        [Fact]
        public void InlineDirectorySyntaxCollapses()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='InlineDirectorySyntaxCollapses' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='Bin1Folder'/><DirectoryRef Id='Bin2Folder'/></Product>" +
                                            @"<Fragment><Directory Id='protected Bin1Folder' Name='TARGETDIR:\foo\bar\bin1\' /></Fragment>" +
                                            @"<Fragment><Directory Id='internal Bin2Folder' Name='TARGETDIR:\foo\bar\bin2\' /></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            Output output = linker.Link(intermediate.Sections, OutputType.Product);

            RowIndexedList<Row> directoryRows = new RowIndexedList<Row>(output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Directory")).SelectMany(d => d.Rows));
            Assert.Equal(7, directoryRows.Count);

            Row bin1Folder = directoryRows.Get("Bin1Folder");
            Row bar1Folder = directoryRows.Get((string)bin1Folder[1]);
            Row foo1Folder = directoryRows.Get((string)bar1Folder[1]);
            Row targetDir = directoryRows.Get((string)foo1Folder[1]);

            Row bin2Folder = directoryRows.Get("Bin2Folder");
            Row bar2Folder = directoryRows.Get((string)bin2Folder[1]);
            Row foo2Folder = directoryRows.Get((string)bar2Folder[1]);

            Assert.Equal(AccessModifier.Protected, bin1Folder.Access);
            Assert.Equal(AccessModifier.Private, bar1Folder.Access);
            Assert.Equal(AccessModifier.Private, foo1Folder.Access);
            Assert.Equal(AccessModifier.Public, targetDir.Access);

            Assert.Equal(AccessModifier.Internal, bin2Folder.Access);
            Assert.Equal(AccessModifier.Private, bar2Folder.Access);
            Assert.Equal(AccessModifier.Private, foo2Folder.Access);
            Assert.Equal(targetDir, directoryRows.Get((string)foo2Folder[1]));

            // Primary keys should be the same for the inline directories.
            Assert.Equal(bar1Folder[0], bar2Folder[0]);
            Assert.Equal(foo1Folder[0], foo2Folder[0]);

            // One and only one of the same inline directories should be marked redundant.
            foreach (Row duplicate in directoryRows.Duplicates)
            {
                Row nonDupe = directoryRows.Get((string)duplicate[0]);
                Assert.True(nonDupe.Redundant ^ duplicate.Redundant);
                Assert.True(nonDupe.IsIdentical(duplicate));
            }
        }

        [Fact]
        public void RootedInlineDirectorySyntaxCanBeNested()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='RootedInlineDirectorySyntaxCanBeNested' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='BinFolder'/></Product>" +
                                            @"<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='protected BinFolder' Name='ProgramFilesFolder:\foo\bar\bin\' /></DirectoryRef></Fragment>" +
                                            @"<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='ProgramFilesFolder' Name='PFiles' /></DirectoryRef></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            Output output = linker.Link(intermediate.Sections, OutputType.Product);

            RowIndexedList<Row> directoryRows = new RowIndexedList<Row>(output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Directory")).SelectMany(d => d.Rows));
            Assert.Equal(5, directoryRows.Count);

            Row binFolder = directoryRows.Get("BinFolder");
            Row barFolder = directoryRows.Get((string)binFolder[1]);
            Row fooFolder = directoryRows.Get((string)barFolder[1]);
            Row programFilesFolder = directoryRows.Get((string)fooFolder[1]);
            Row targetDir = directoryRows.Get((string)programFilesFolder[1]);

            Assert.Equal(AccessModifier.Protected, binFolder.Access);
            Assert.Equal(AccessModifier.Private, barFolder.Access);
            Assert.Equal(AccessModifier.Private, fooFolder.Access);
            Assert.Equal(AccessModifier.Public, programFilesFolder.Access);
            Assert.Equal(AccessModifier.Public, targetDir.Access);
        }

        [Fact]
        public void NonRootedInlineDirectorySyntaxCanBeNested()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='NonRootedInlineDirectorySyntaxCanBeNested' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='BinFolder'/></Product>" +
                                            @"<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='protected BinFolder' Name='foo\bar\bin\' /></DirectoryRef></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            Output output = linker.Link(intermediate.Sections, OutputType.Product);

            RowIndexedList<Row> directoryRows = new RowIndexedList<Row>(output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Directory")).SelectMany(d => d.Rows));
            Assert.Equal(4, directoryRows.Count);

            Row binFolder = directoryRows.Get("BinFolder");
            Row barFolder = directoryRows.Get((string)binFolder[1]);
            Row fooFolder = directoryRows.Get((string)barFolder[1]);
            Row targetDir = directoryRows.Get((string)fooFolder[1]);

            Assert.Equal(AccessModifier.Protected, binFolder.Access);
            Assert.Equal(AccessModifier.Private, barFolder.Access);
            Assert.Equal(AccessModifier.Private, fooFolder.Access);
            Assert.Equal(AccessModifier.Public, targetDir.Access);
        }
    }
}
