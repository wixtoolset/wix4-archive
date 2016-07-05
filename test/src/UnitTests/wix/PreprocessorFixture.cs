// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.WixUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using WixToolset;
    using WixToolset.Data;
    using Xunit;

    public class PreprocessorFixture : WixUnitTestBaseFixture
    {
        private XNamespace WixNamespace = "http://wixtoolset.org/schemas/v4/wxs";

        [Fact]
        public void CanProcessSimpleDocument()
        {
            Preprocessor p = new Preprocessor();

            XDocument d = p.Process(@"testdata\simple.wxs", new Dictionary<string, string>());
            SourceLineNumber s = Preprocessor.GetSourceLineNumbers(d.Descendants(WixNamespace + "Component").Single());

            string expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <ComponentGroup Id=\"ComponentGroup\" Directory=\"InstallFolder\">",
                "      <Component>",
                "        <File Source=\"file.ext\" />",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>");

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>", d.Declaration.ToString());
            Assert.Equal(expected, d.ToString());
            Assert.Equal(@"testdata\simple.wxs*8", s.QualifiedFileName);
        }

        [Fact]
        public void CanProcessIncludeDocument()
        {
            Preprocessor p = new Preprocessor();

            XDocument d = p.Process(@"testdata\parent.wxs", new Dictionary<string, string>());
            SourceLineNumber s = d.Descendants(WixNamespace + "Component").Single().Annotation<SourceLineNumber>();

            string expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <ComponentGroup Id=\"ComponentGroup\">",
                "      <Component Directory=\"InstallFolder\">",
                "        <File Source=\"file.ext\" />",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>");

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>", d.Declaration.ToString());
            Assert.Equal(expected, d.ToString());
            Assert.Equal(@"testdata\include.wxi*6", s.QualifiedFileName);
        }

        [Fact]
        public void CanProcessDoubleFragmentDocument()
        {
            Preprocessor p = new Preprocessor();

            XDocument d = p.Process(@"testdata\double_fragment.wxs", new Dictionary<string, string>());
            SourceLineNumber s = Preprocessor.GetSourceLineNumbers(d.Descendants(WixNamespace + "Component").Single());

            string expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <ComponentGroup Id=\"ComponentGroup\">",
                "      <ComponentRef Id=\"file.ext\" />",
                "    </ComponentGroup>",
                "  </Fragment>",
                "  <Fragment>",
                "    <Component Directory=\"InstallFolder\">",
                "      <File Source=\"file.ext\" />",
                "    </Component>",
                "  </Fragment>",
                "</Wix>");

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>", d.Declaration.ToString());
            Assert.Equal(expected, d.ToString());
            Assert.Equal(@"testdata\double_fragment.wxs*13", s.QualifiedFileName);
        }

        [Fact]
        public void CanProcessAutoVersionFunction()
        {
            Preprocessor p = new Preprocessor();

            DateTime now = DateTime.UtcNow;
            int build = (int)(now - new DateTime(2000, 1, 1)).TotalDays;
            int revision = (int)(now - new DateTime(now.Year, now.Month, now.Day)).TotalSeconds / 2;

            XDocument d = p.Process(@"testdata\func_autoversion.wxs", new Dictionary<string, string>());
            XElement fooElement = d.Descendants("Foo").Single();
            string barValue = fooElement.Attribute("Bar").Value;
            Version version = new Version(barValue);

            Assert.Equal(1, version.Major);
            Assert.Equal(2, version.Minor);
            Assert.Equal(build, version.Build);
            Assert.True(revision <= version.Revision);
        }
    }
}
