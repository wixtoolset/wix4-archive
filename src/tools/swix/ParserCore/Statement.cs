// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixToolset.Simplified.ParserCore
{
    // Statement *could* be derived from Token<>, as it has a type and range... consider it
    // a "meta-token" or "aggregated token", perhaps.
    [System.Diagnostics.DebuggerDisplay("{StatementType} {Range}")]
    public class Statement<StatementT, TokenT> : IRangeProvider
        where StatementT : struct   // the closest we can get to "T : enum"
        where TokenT : struct       // the closest we can get to "T : enum"
    {
        public Statement(
            StatementT statementType,
            IEnumerable<Token<TokenT>> significantTokens,
            IEnumerable<Token<TokenT>> allTokens)
        {
            this.StatementType = statementType;
            this.Tokens = new List<Token<TokenT>>(significantTokens);
            this.AllTokens = new List<Token<TokenT>>(allTokens);

            // Make a statement range be *all* of the tokens, not just the
            // non-ignorable ones!
            var start = this.AllTokens.First().Range.Start;
            var end = this.AllTokens.Last().Range.End;

            this.Range = new Range(start, end);

            this.HasError = this.AllTokens.Any(t => t.Errors != null && t.Errors.Any());
        }

        public StatementT StatementType { get; private set; }
        public IList<Token<TokenT>> Tokens { get; private set; }
        public Range Range { get; private set; }
        public bool HasError { get; private set; }

        // By default, most caller will want 'Tokens', which includes only the significant ones.
        // If you need "insignificant" tokens (whitespace, comments), use 'AllTokens'.
        public IList<Token<TokenT>> AllTokens { get; private set; }

        // Don't really need this, as the public Range member is sufficient.
        ////Range IRangeProvider.Range
        ////{
        ////    get { return this.Range; }
        ////}

        public override string ToString()
        {
            return string.Format("{0} {1}", this.StatementType, this.Range);
        }
    }
}
