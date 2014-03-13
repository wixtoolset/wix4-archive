//-------------------------------------------------------------------------------------------------
// <copyright file="ExtensionManagerFixture.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
