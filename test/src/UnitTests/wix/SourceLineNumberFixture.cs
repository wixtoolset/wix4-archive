//-------------------------------------------------------------------------------------------------
// <copyright file="SourceLineNumberFixture.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixTest.WixUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using WixToolset;
    using WixToolset.Data;
    using Xunit;

    public class SourceLineNumberFixture : WixUnitTestBaseFixture
    {
        [Fact]
        public void CanEncodeSingleLineNumber()
        {
            var source = new SourceLineNumber("a.wxs", 1);
            string encoded = source.GetEncoded();

            Assert.Equal("a.wxs*1", encoded);
        }

        [Fact]
        public void CanEncodeMultipleLineNumber()
        {
            var parent = new SourceLineNumber("a.wxs", 6);
            var source = new SourceLineNumber("b.wxi", 10) { Parent = parent };
            string encoded = source.GetEncoded();

            Assert.Equal("b.wxi*10|a.wxs*6", encoded);
        }

        [Fact]
        public void CanEncodeMultipleFiles()
        {
            var parent = new SourceLineNumber("parent.wxs");
            var source = new SourceLineNumber("included.wxi") { Parent = parent };
            string encoded = source.GetEncoded();

            Assert.Equal("included.wxi|parent.wxs", encoded);
        }

        [Fact]
        public void CanCheckSourceLineNumberEquality()
        {
            var a1 = new SourceLineNumber("a.wxs", 6);
            var a2 = new SourceLineNumber("a.wxs", 6);

            var b1 = new SourceLineNumber("b.wxs");
            var b2 = new SourceLineNumber("b.wxs");

            var c1 = new SourceLineNumber("c.wxs", 7) { Parent = a1 };
            var c2 = new SourceLineNumber("c.wxs", 7) { Parent = a2 };

            Assert.NotSame(a1, a2);
            Assert.NotSame(b1, b2);
            Assert.NotSame(c1, c2);
            Assert.True(a1.Equals(a2));
            Assert.Equal(a1.GetHashCode(), a2.GetHashCode());

            Assert.True(b1.Equals(b2));
            Assert.Equal(b1.GetHashCode(), b2.GetHashCode());

            Assert.True(c1.Equals(c2));
            Assert.Equal(c1.GetHashCode(), c2.GetHashCode());

            Assert.False(a1.Equals(b2));
            Assert.NotEqual(a1.GetHashCode(), b2.GetHashCode());

            Assert.False(b1.Equals(c2));
            Assert.NotEqual(b1.GetHashCode(), c2.GetHashCode());

            Assert.False(a1.Equals(c2));
            Assert.NotEqual(a1.GetHashCode(), c2.GetHashCode());
        }
    }
}
