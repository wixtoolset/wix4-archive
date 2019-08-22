// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixToolset.Simplified.ParserCore
{
    // Token types are logically like enum values... but we need to be able to ask them questions
    // like "are you ignorable?", and potentially other call-backy things.  Therefore, we need a
    // typed interface.  What's more, lexer and parser tokens are tied together, but it's weird
    // to have separate classes.  Therefore, the token-info iinterface deals with a lexer/parser
    // token pair.
    interface ITokenTypeInfo<TParserToken, TLexerToken>
        where TParserToken : struct    // we'd really like to say "T : enum", but that's not allowed!
        where TLexerToken : struct    // we'd really like to say "T : enum", but that's not allowed!
    {
        bool IsIgnorable(TParserToken parserTokenType);
        bool StatementBuilderCanConsumeOnFailedExpect(TLexerToken lexerTokenType);
    }

    public class TokenBase : IRangeProvider
    {
        public TokenBase(string value, Range range)
        {
            this.Initialize(value, range);
        }

        public TokenBase(TokenBase token)
        {
            System.Diagnostics.Debug.Assert(token != null);
            this.Initialize(token.Value, token.Range);

            // copy errors?
            if (token.Errors != null)
            {
                this.Errors = new List<Error>(token.Errors);
            }
        }

        public string Value { get; private set; }
        public Range Range { get; private set; }
        public List<Error> Errors { get; private set; }

        private void Initialize(string value, Range range)
        {
            this.Value = value;
            this.Range = range;
        }

        public void AddError(string message)
        {
            if (this.Errors == null)
            {
                this.Errors = new List<Error>();
            }

            this.Errors.Add(new Error(this, message));
        }

        public void AddError(string messageFormat, params object[] args)
        {
            this.AddError(string.Format(messageFormat, args));
        }

        public void UpdateValue(string value)
        {
            this.Value = value;
        }

        // Don't really need this, as the public Range member is sufficient.
        ////Range IRangeProvider.Range
        ////{
        ////    get { return this.Range; }
        ////}
    }

    [System.Diagnostics.DebuggerDisplay("{TokenType} {Range} {Value}")]
    public class Token<T> : TokenBase
        where T : struct       // The closest we can get to "T : enum"
    {
        public Token(T tokenType, string value, Range range)
            : base(value, range)
        {
            this.TokenType = tokenType;
        }

        public Token(T tokenType, TokenBase token)
            : base(token)
        {
            this.TokenType = tokenType;
        }

        public T TokenType { get; private set; }

        public override string ToString()
        {
            if (this == null)
            {
                return "(null)";
            }

            return string.Format("{0} {1} {2}", this.TokenType, this.Range, this.Value);
        }
    }
}
