//-------------------------------------------------------------------------------------------------
// <copyright file="TokenComparer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WixToolset.Simplified.ParserCore;
using Xunit;
using Xunit.Extensions;

namespace WixToolset.Simplified.UnitTest.Swcore
{
    class TokenComparer<T> : IEqualityComparer<Token<T>>
        where T : struct    // The closest we can get to "T : enum"
    {
        private bool compareOffsets;

        public TokenComparer(bool compareOffsets = true)
        {
            this.compareOffsets = compareOffsets;
        }

        public bool Equals(Token<T> x, Token<T> y)
        {
            Assert.Equal(x.TokenType, y.TokenType);
            Assert.Equal(x.Value, y.Value);

            if (this.compareOffsets)
            {
                Assert.Equal(x.Range.Start.Offset, y.Range.Start.Offset);
                Assert.Equal(x.Range.Length, y.Range.Length);
            }

            return object.Equals(x.TokenType, y.TokenType) &&
                string.Equals(x.Value, y.Value, StringComparison.Ordinal) &&
                (!this.compareOffsets ||
                    x.Range.Start.Offset == y.Range.Start.Offset &&
                    x.Range.Length == y.Range.Length);
        }

        public int GetHashCode(Token<T> token)
        {
            return token.Value.GetHashCode();
        }
    }
}
