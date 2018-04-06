// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using WixToolset.Simplified.ParserCore;
using Xunit;

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
