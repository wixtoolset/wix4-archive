//-------------------------------------------------------------------------------------------------
// <copyright file="PreprocessorFixture.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Preprocessor of the WiX toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

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

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>", d.Declaration.ToString());
            Assert.Equal("<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">" + Environment.NewLine +
                         "  <Fragment>" + Environment.NewLine +
                         "    <ComponentGroup Id=\"ComponentGroup\" Directory=\"InstallFolder\">" + Environment.NewLine +
                         "      <Component>" + Environment.NewLine +
                         "        <File Source=\"file.ext\" />" + Environment.NewLine +
                         "      </Component>" + Environment.NewLine +
                         "    </ComponentGroup>" + Environment.NewLine +
                         "  </Fragment>" + Environment.NewLine +
                         "</Wix>", d.ToString());
            Assert.Equal(@"testdata\simple.wxs*5", s.QualifiedFileName);
        }

        [Fact]
        public void CanProcessIncludeDocument()
        {
            Preprocessor p = new Preprocessor();

            XDocument d = p.Process(@"testdata\parent.wxs", new Dictionary<string, string>());
            SourceLineNumber s = d.Descendants(WixNamespace + "Component").Single().Annotation<SourceLineNumber>();

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>", d.Declaration.ToString());
            Assert.Equal("<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">" + Environment.NewLine +
                         "  <Fragment>" + Environment.NewLine +
                         "    <ComponentGroup Id=\"ComponentGroup\">" + Environment.NewLine +
                         "      <Component Directory=\"InstallFolder\">" + Environment.NewLine +
                         "        <File Source=\"file.ext\" />" + Environment.NewLine +
                         "      </Component>" + Environment.NewLine +
                         "    </ComponentGroup>" + Environment.NewLine +
                         "  </Fragment>" + Environment.NewLine +
                         "</Wix>", d.ToString());
            Assert.Equal(@"testdata\include.wxi*3", s.QualifiedFileName);
        }

        [Fact]
        public void CanProcessDoubleFragmentDocument()
        {
            Preprocessor p = new Preprocessor();

            XDocument d = p.Process(@"testdata\double_fragment.wxs", new Dictionary<string, string>());
            SourceLineNumber s = Preprocessor.GetSourceLineNumbers(d.Descendants(WixNamespace + "Component").Single());

            string expected = "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">" + Environment.NewLine +
                              "  <Fragment>" + Environment.NewLine +
                              "    <ComponentGroup Id=\"ComponentGroup\">" + Environment.NewLine +
                              "      <ComponentRef Id=\"file.ext\" />" + Environment.NewLine +
                              "    </ComponentGroup>" + Environment.NewLine +
                              "  </Fragment>" + Environment.NewLine +
                              "  <Fragment>" + Environment.NewLine +
                              "    <Component Directory=\"InstallFolder\">" + Environment.NewLine +
                              "      <File Source=\"file.ext\" />" + Environment.NewLine +
                              "    </Component>" + Environment.NewLine +
                              "  </Fragment>" + Environment.NewLine +
                              "</Wix>";

            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-8\"?>", d.Declaration.ToString());
            Assert.Equal(expected, d.ToString());
            Assert.Equal(@"testdata\double_fragment.wxs*10", s.QualifiedFileName);
        }
    }
}
