// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.UnitTest.Swcore
{
    class TokenListBuilder<T> : IEnumerable<Token<T>>
        where T : struct    // The closest we can get to "T : enum"
    {
        List<Token<T>> tokens = new List<Token<T>>();
        Position currentPosition = new Position(0, 0, 0);

        public TokenListBuilder<T> Token(T type, string value)
        {
            Range range = new Range(this.currentPosition, value.Length);
            this.tokens.Add(new Token<T>(type, value, range));
            this.currentPosition = range.End;
            return this;
        }

        public IEnumerator<Token<T>> GetEnumerator()
        {
            return this.tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
