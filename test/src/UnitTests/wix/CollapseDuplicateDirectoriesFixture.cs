// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.WixUnitTest
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using WixToolset;
    using WixToolset.Data;
    using Xunit;

    public class CollapseDuplicateDirectoriesFixture : WixUnitTestBaseFixture
    {
        [Fact]
        public void TableExt()
        {
            TableIndexedCollection tables = new TableIndexedCollection();

            Assert.Empty(tables["NotFound"].RowsAs<Row>());
        }

        [Fact]
        public void DuplicatePrivateDirectoriesCollapse()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='DuplicatePrivateDirectoriesCollapse' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='ChildDirectory1'/><DirectoryRef Id='ChildDirectory2'/></Product>" +
                                            "<Fragment><PropertyRef Id='Property1'/><DirectoryRef Id='TARGETDIR'><Directory Id='private DupeDirectory' Name='dupe'><Directory Id='ChildDirectory1' Name='child1'/></Directory></DirectoryRef></Fragment>" +
                                            "<Fragment><PropertyRef Id='Property2'/><DirectoryRef Id='TARGETDIR'><Directory Id='private DupeDirectory' Name='dupe'><Directory Id='ChildDirectory2' Name='child2'/></Directory></DirectoryRef></Fragment>" +
                                            "<Fragment><Property Id='Property1' Value='prop1' /></Fragment>" +
                                            "<Fragment><Property Id='Property2' Value='prop2' /></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            Output output = linker.Link(intermediate.Sections, OutputType.Product);

            var directoryRows = output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Directory")).SelectMany(d => d.Rows).ToList();
            var dupedRows = directoryRows.Where(r => r[0].Equals("DupeDirectory"));
            Assert.Equal(2, dupedRows.Count());
            Assert.Single(dupedRows.Where(r => r.Redundant));
            Assert.Single(dupedRows.Where(r => !r.Redundant));

            var propertyRows = output.Sections.SelectMany(sec => sec.Tables).Where(t => t.Name.Equals("Property")).SelectMany(p => p.Rows).ToList();
            Assert.Single(propertyRows.Where(r => r[0].Equals("Property1")));
            Assert.Single(propertyRows.Where(r => r[0].Equals("Property2")));
        }

        [Fact]
        public void DuplicateNonPrivateDirectoryErrors()
        {
            XDocument doc = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Product Id='*' Language='1033' Manufacturer='WixTests' Name='DuplicateNonPrivateDirectoryErrors' Version='1.0.0' UpgradeCode='12345678-1234-1234-1234-1234567890AB'><DirectoryRef Id='ChildDirectory1'/><DirectoryRef Id='ChildDirectory2'/></Product>" +
                                            "<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='private DupeDirectory' Name='dupe'><Directory Id='ChildDirectory1' Name='child1'/></Directory></DirectoryRef></Fragment>" +
                                            "<Fragment><DirectoryRef Id='TARGETDIR'><Directory Id='protected DupeDirectory' Name='dupe'><Directory Id='ChildDirectory2' Name='child2'/></Directory></DirectoryRef></Fragment>" +
                                            "<Fragment><Directory Id='TARGETDIR' Name='SourceDir' /></Fragment></Wix>");
            XDocument src = new Preprocessor().Process(doc.CreateReader(), new Dictionary<string, string>());
            Compiler compiler = new Compiler();
            Linker linker = new Linker();

            Intermediate intermediate = compiler.Compile(src);
            WixException e = Assert.Throws<WixException>(() => linker.Link(intermediate.Sections, OutputType.Product));
            Assert.Equal(WixErrors.DuplicateSymbol(null, null).Id, e.Error.Id);
        }
    }
}
