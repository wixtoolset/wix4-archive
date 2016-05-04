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

    public class IdentifierAccessModifiersFixture : WixUnitTestBaseFixture
    {
        [Fact]
        public void ImplicitPublicIsPublic()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Fragment><ComponentGroup Id='PublicGroup' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();

            Intermediate intermediate = compiler.Compile(src);
            var row = intermediate.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("WixComponentGroup")).SelectMany(t => t.Rows).Single();
            Assert.Equal("PublicGroup", row[0]);
            Assert.Equal(AccessModifier.Public, row.Access);
        }

        [Fact]
        public void ExplicitAccessModifiersValid()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Fragment><ComponentGroup Id='public PublicGroup' Directory='PrivateDirectory'>" +
                                            "<Component Id='internal InternalComponent'>" +
                                            "<File Id='protected ProtectedFile' Source='ignored'/></Component></ComponentGroup>" +
                                            "<DirectoryRef Id='TARGETDIR'><Directory Id='private PrivateDirectory'/></DirectoryRef></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();

            Intermediate intermediate = compiler.Compile(src);
            var tables = intermediate.Sections.SelectMany(sec => sec.Tables).ToDictionary(t => t.Name);
            var componentGroupRow = tables["WixComponentGroup"].Rows.Single();
            Assert.Equal("PublicGroup", componentGroupRow[0].ToString());
            Assert.Equal(AccessModifier.Public, componentGroupRow.Access);

            var componentRow = tables["Component"].Rows.Single();
            Assert.Equal("InternalComponent", componentRow[0].ToString());
            Assert.Equal(AccessModifier.Internal, componentRow.Access);

            var fileRow = tables["File"].Rows.Single();
            Assert.Equal("ProtectedFile", fileRow[0].ToString());
            Assert.Equal(AccessModifier.Protected, fileRow.Access);

            var directoryRow = tables["Directory"].Rows.Single();
            Assert.Equal("PrivateDirectory", directoryRow[0].ToString());
            Assert.Equal(AccessModifier.Private, directoryRow.Access);
        }

        [Fact]
        public void ProtectedLinksAcrossFragmentsButNotFiles()
        {
            XDocument doc1 = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='ProtectedLinksAcrossFragments' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='ProtectedDirectory'/></Product>" +
                                             "<Fragment><Directory Id='TARGETDIR' Name='SourceDir'><Directory Id='protected ProtectedDirectory' Name='protected'/></Directory></Fragment></Wix>");
            XDocument doc2 = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='protected ProtectedDirectory' Name='conflict'/></DirectoryRef></Fragment></Wix>");
            XDocument src1 = new Preprocessor().Process(doc1.CreateReader(), new Dictionary<string, string>());
            XDocument src2 = new Preprocessor().Process(doc2.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();
            List<Section> sections = new List<Section>();

            Intermediate intermediate = compiler.Compile(src1);
            sections.AddRange(intermediate.Sections);

            intermediate = compiler.Compile(src2);
            sections.AddRange(intermediate.Sections);

            Output output = linker.Link(sections, OutputType.Product);

            var directoryRows = output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Directory")).SelectMany(t => t.Rows).OrderBy(r => r[0]).ToArray();

            Assert.Equal(2, directoryRows.Length);

            Assert.Equal("ProtectedDirectory", directoryRows[0][0]);
            Assert.Equal(AccessModifier.Protected, directoryRows[0].Access);

            Assert.Equal("TARGETDIR", directoryRows[1][0]);
            Assert.Equal(AccessModifier.Public, directoryRows[1].Access);
        }

        [Fact]
        public void PrivateCannotLinkAcrossFragments()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='PrivateCannotLinkAcrossFragments' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='PrivateDirectory'/></Product>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir'><Directory Id='private PrivateDirectory' Name='private'/></Directory></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            WixException e = Assert.Throws<WixException>(() => linker.Link(intermediate.Sections, OutputType.Product));
            Assert.Equal(WixErrors.UnresolvedReference(null, null).Id, e.Error.Id);
        }

        [Fact]
        public void PrivateDuplicatesAvoidLinkError()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='PrivateDuplicatesAvoidLinkError' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'></Product>" +
                                            "<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='private PrivateDirectory' Name='noconflict1'/></DirectoryRef></Fragment>" +
                                            "<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='private PrivateDirectory' Name='noconflict2'/></DirectoryRef></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            Output output = linker.Link(intermediate.Sections, OutputType.Product);
            Assert.Empty(output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Directory")));
        }

        [Fact]
        public void ProtectedDuplicatesInSameFileCauseLinkError()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='ProtectedDuplicatesInSameFileCauseLinkError' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='ProtectedDirectory'/></Product>" +
                                            "<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='protected ProtectedDirectory' Name='conflict1'/></DirectoryRef></Fragment>" +
                                            "<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='protected ProtectedDirectory' Name='conflict2'/></DirectoryRef></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            WixException e = Assert.Throws<WixException>(() => linker.Link(intermediate.Sections, OutputType.Product));
            Assert.Equal(WixErrors.DuplicateSymbol(null, null).Id, e.Error.Id);
        }

        [Fact]
        public void InternalCannotLinkAcrossLibrary()
        {
            XDocument doc1 = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment>" +
                                             "<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='internal InternalDirectory' Name='hidden'/></DirectoryRef></Fragment></Wix>");
            XDocument doc2 = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='ProtectedLinksAcrossFragments' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='TARGETDIR'/><DirectoryRef Id='InternalDirectory'/></Product></Wix>");
            XDocument src1 = new Preprocessor().Process(doc1.CreateReader(), new Dictionary<string, string>());
            XDocument src2 = new Preprocessor().Process(doc2.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Librarian librarian = new Librarian();
            Linker linker = new Linker();
            List<Section> sections = new List<Section>();

            Intermediate intermediate = compiler.Compile(src1);
            Library library = librarian.Combine(intermediate.Sections);

            intermediate = compiler.Compile(src2);

            sections.AddRange(library.Sections);
            sections.AddRange(intermediate.Sections);

            WixException e = Assert.Throws<WixException>(() => linker.Link(sections, OutputType.Product));
            Assert.Equal(WixErrors.UnresolvedReference(null, null).Id, e.Error.Id);
        }
    }
}
