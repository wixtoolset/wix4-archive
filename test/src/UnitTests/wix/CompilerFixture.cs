// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.WixUnitTest
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset;
    using WixToolset.Data;
    using Xunit;

    public class CompilerFixture : WixUnitTestBaseFixture
    {
        [Fact]
        public void CanCompile()
        {
            XDocument d = XDocument.Load(@"testdata\simple.wxs");
            Compiler c = new Compiler();

            Intermediate i = c.Compile(d);
            Assert.NotNull(i);
        }

        [Fact]
        public void CanCompileYesNoAttribute()
        {
            XDocument d = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Fragment><Property Id='SECURE' Secure='yes' /></Fragment></Wix>");
            Compiler c = new Compiler();

            Intermediate i = c.Compile(d);
            Assert.NotNull(i);
        }

        [Fact]
        public void CanCompileErrorElement()
        {
            XDocument d = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Fragment><UI Id='Test'><Error Id='42'>Magic number.</Error><Error Id='43'>No magic number.</Error></UI></Fragment><Fragment Id='foo'></Fragment></Wix>");
            Preprocessor p = new Preprocessor();
            var s = p.Process(d.CreateReader(), new Dictionary<string, string>());
            Compiler c = new Compiler();

            Intermediate i = c.Compile(s);
            Assert.NotNull(i);
        }

        [Fact]
        public void CanCompileCustomAcitonElement()
        {
            XDocument d = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'><Fragment><CustomAction Id='Foo' Error='Test message.' /><InstallExecuteSequence><Custom Action='Foo' After='Bar' /></InstallExecuteSequence></Fragment></Wix>");
            Preprocessor p = new Preprocessor();
            var s = p.Process(d.CreateReader(), new Dictionary<string, string>());
            Compiler c = new Compiler();

            Intermediate i = c.Compile(s);
            Assert.NotNull(i);
        }

        [Fact]
        public void CanCompileExtensionNamespaces()
        {
            XDocument d = XDocument.Parse("<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs' xmlns:swid='http://wixtoolset.org/schemas/v4/wxs/tag'><Fragment><Component Directory='InstallFolder'><File Source='file.exe' /></Component></Fragment></Wix>");
            Preprocessor p = new Preprocessor();
            var s = p.Process(d.CreateReader(), new Dictionary<string, string>());
            Compiler c = new Compiler();

            Intermediate i = c.Compile(s);
            Assert.NotNull(i);
        }
    }
}
