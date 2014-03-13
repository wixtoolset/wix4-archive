//-------------------------------------------------------------------------------------------------
// <copyright file="TokenListBuilder.cs" company="Outercurve Foundation">
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
