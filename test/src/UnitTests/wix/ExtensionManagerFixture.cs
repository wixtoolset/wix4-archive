// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.WixUnitTest
{
    using System.Linq;
    using System.Reflection;
    using WixToolset;
    using WixToolset.Extensibility;
    using Xunit;

    public class ExtensionManagerFixture : WixUnitTestBaseFixture
    {
        [Fact]
        public void CanLoadCompilerExtension()
        {
            var m = new ExtensionManager();
            m.Load(Assembly.GetExecutingAssembly().Location);
            var e = m.Create<ICompilerExtension>();

            Assert.IsType<TestCompilerExtension>(e.Single());
        }

        [Fact]
        public void ExtensionsLoadOnce()
        {
            var m = new ExtensionManager();
            m.Load(Assembly.GetExecutingAssembly().Location);
            var e = m.Create<ICompilerExtension>();

            var ce = e.Single();
            var ceAgain = e.Single();
            Assert.Same(ce, ceAgain);
        }
    }

    /// <summary>
    /// Empty test compiler extension.
    /// </summary>
    public class TestCompilerExtension : CompilerExtension
    {
    }
}
