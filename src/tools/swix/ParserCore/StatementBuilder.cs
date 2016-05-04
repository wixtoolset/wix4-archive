// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixToolset.Simplified.ParserCore
{
    /// <summary>
    /// Helper class for aggregating lexer tokens into statements of parser tokens.
    /// </summary>
    /// <typeparam name="TStatement">The type (enum) allowed for statements.</typeparam>
    /// <typeparam name="TParserToken">The type (enum) of parse tokens inside the statements.</typeparam>
    /// <typeparam name="TLexerToken">The type (enum) of incoming tokens from the lexer.</typeparam>
    [System.Diagnostics.DebuggerDisplay("{TStatementype} {Range}")]
    internal class StatementBuilder<TStatement, TParserToken, TLexerToken> : IRangeProvider
        where TStatement : struct       // the closest we can get to "T : enum"
        where TParserToken : struct     // the closest we can get to "T : enum"
        where TLexerToken : struct      // the closest we can get to "T : enum"
    {
        bool chainEnabled = true;

        public StatementBuilder(
            TokenBuffer<TLexerToken> tokenBuffer,
            ITokenTypeInfo<TParserToken, TLexerToken> tokenInfo,
            TParserToken unknownTParserTokenType = default(TParserToken),
            TStatement TStatementype = default(TStatement))
        {
            this.TokenBuffer = tokenBuffer;
            this.TStatementype = TStatementype;
            this.TokenInfo = tokenInfo;
            this.UnknownTParserTokenType = unknownTParserTokenType;
            this.Tokens = new List<Token<TParserToken>>();
        }

        public TokenBuffer<TLexerToken> TokenBuffer { get; private set; }
        public ITokenTypeInfo<TParserToken, TLexerToken> TokenInfo { get; private set; }
        public TParserToken UnknownTParserTokenType { get; private set; }
        public TStatement TStatementype { get; private set; }
        public Range Range { get; private set; }
        public IList<Token<TParserToken>> Tokens { get; private set; }
        public bool HasError { get; private set; }

        // Don't really need this, as the public Range member is sufficient.
        ////Range IRangeProvider.Range
        ////{
        ////    get { return this.Range; }
        ////}

        private void Add(Token<TParserToken> token)
        {
            Range range = this.Range ?? token.Range;
            this.Tokens.Add(token);
            this.Range = new Range(range.Start, token.Range.End);
        }

        public Statement<TStatement, TParserToken> ToStatement()
        {
            var significantTokens = this.Tokens.Where(t => !this.TokenInfo.IsIgnorable(t.TokenType));
            return new Statement<TStatement, TParserToken>(this.TStatementype, significantTokens, this.Tokens);
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> As(TStatement TStatementype)
        {
            this.TStatementype = TStatementype;
            return this;
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> As(TStatement TStatementype, TParserToken parserTokenType)
        {
            this.TStatementype = TStatementype;
            // Auto-accept the current token...
            return this.AcceptAny(parserTokenType);
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> As(TStatement TStatementype, TParserToken parserTokenType, Token<TLexerToken> lexerToken)
        {
            this.TStatementype = TStatementype;
            // Include the provided token...
            if (lexerToken != null)
            {
                this.Add(new Token<TParserToken>(parserTokenType, lexerToken));
            }

            return this;
        }

        // With 'errorMessage' provided, it's treated as an error...
        public StatementBuilder<TStatement, TParserToken, TLexerToken> AcceptAny(TParserToken parserTokenType, string errorMessage = null)
        {
            if (this.chainEnabled)
            {
                Token<TLexerToken> lexerToken;
                if (this.TokenBuffer.AcceptAny(out lexerToken))
                {
                    var token = new Token<TParserToken>(parserTokenType, lexerToken);

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        // always provide actual type, actual value, and new type as possible message arguments
                        token.AddError(errorMessage, lexerToken.TokenType, lexerToken.Value, parserTokenType);

                        this.chainEnabled = false;
                        this.HasError = true;
                    }

                    this.Add(token);
                }

                // TODO: If there are no more tokens, but an error message was provided, what does that mean?
                // is it an error, or is it specifically okay, because there were no more tokens?
            }

            return this;
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> Accept(TLexerToken lexerTokenType, TParserToken parserTokenType, out bool matched)
        {
            matched = false;
            if (this.chainEnabled)
            {
                Token<TLexerToken> lexerToken;
                if (this.TokenBuffer.Accept(lexerTokenType, out lexerToken))
                {
                    this.Add(new Token<TParserToken>(parserTokenType, lexerToken));
                    matched = true;
                }
            }

            return this;
        }

        // This version of Accept requires passing the statement through.  It's architecturally nice, but
        // means anonymous delegates have to define the Func<> parameter and also return.
        public StatementBuilder<TStatement, TParserToken, TLexerToken> Accept(
            TLexerToken lexerTokenType,
            TParserToken parserTokenType,
            Func<StatementBuilder<TStatement, TParserToken, TLexerToken>, StatementBuilder<TStatement, TParserToken, TLexerToken>> ifAccepted = null,
            Func<StatementBuilder<TStatement, TParserToken, TLexerToken>, StatementBuilder<TStatement, TParserToken, TLexerToken>> ifNotAccepted = null)
        {
            if (this.chainEnabled)
            {
                bool matched;
                this.Accept(lexerTokenType, parserTokenType, out matched);
                if (matched && ifAccepted != null)
                {
                    return ifAccepted(this);
                }
                else if (!matched && ifNotAccepted != null)
                {
                    return ifNotAccepted(this);
                }
            }

            return this;
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> Expect(TLexerToken lexerTokenType, TParserToken parserTokenType)
        {
            bool matched;
            return this.Expect(lexerTokenType, parserTokenType, out matched);
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> Expect(TLexerToken lexerTokenType, TParserToken parserTokenType, out bool matched)
        {
            matched = false;

            if (this.chainEnabled)
            {
                this.Accept(lexerTokenType, parserTokenType, out matched);
                if (!matched)
                {
                    Token<TParserToken> badToken = null;

                    // TODO: Never consume on error?  (*but* inject an error token to the statement?)

                    // KNOWS ABOUT COMMENT/NEWLINE?
                    // TODO: We might actually be able to get rid of the "StatementBuilderCanConsumeOnFailedExpect", if
                    // only because the parser needs to be the one to understand about newlines and when they're okay.
                    // Or, maybe that just means that our current users return false for all Lexer tokens.
                    if (this.TokenBuffer.CurrentToken != null &&
                        this.TokenInfo.StatementBuilderCanConsumeOnFailedExpect(this.TokenBuffer.CurrentTokenType))
                    {
                        Token<TLexerToken> lexerToken;
                        this.TokenBuffer.AcceptAny(out lexerToken);
                        badToken = new Token<TParserToken>(this.UnknownTParserTokenType, lexerToken);
                        badToken.AddError(
                            "Unexpected token: {0} ('{1}'), expected {2}",
                            lexerToken.TokenType,
                            lexerToken.Value,
                            lexerTokenType);
                    }
                    else
                    {
                        // Don't consume the token, but do copy it for the error...
                        badToken = new Token<TParserToken>(this.UnknownTParserTokenType, this.TokenBuffer.CurrentValue, this.TokenBuffer.CurrentRange);
                        badToken.AddError(
                            "Unexpected token: {0} ('{1}'), expected {2}",
                            this.TokenBuffer.CurrentTokenType,
                            this.TokenBuffer.CurrentValue,
                            lexerTokenType);
                    }

                    if (badToken != null)
                    {
                        this.Add(badToken);
                    }

                    this.HasError = true;
                    this.chainEnabled = false;
                }
            }

            return this;
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> AggregateWhile(
            TLexerToken lexerTokenType,
            TParserToken parserTokenType)
        {
            return this.AggregateWhile(l => l.Equals(lexerTokenType), parserTokenType);
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> AggregateWhileNot(
            TLexerToken lexerTokenType,
            TParserToken parserTokenType)
        {
            return this.AggregateWhile(l => !l.Equals(lexerTokenType), parserTokenType);
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> AggregateWhile(
            Predicate<TLexerToken> lexerTokenTypePredicate,
            TParserToken parserTokenType,
            bool expectNonEmpty = false)
        {
            if (this.chainEnabled)
            {
                Position parserTokenStart = null;
                Position parserTokenEnd = null;
                string parserTokenValue = null;

                while (this.TokenBuffer.CurrentToken != null && lexerTokenTypePredicate(this.TokenBuffer.CurrentTokenType))
                {
                    Token<TLexerToken> lexerToken;
                    if (this.TokenBuffer.AcceptAny(out lexerToken))
                    {
                        if (parserTokenStart == null)
                        {
                            parserTokenStart = lexerToken.Range.Start;
                        }

                        parserTokenEnd = lexerToken.Range.End;

                        if (parserTokenValue == null)
                        {
                            parserTokenValue = lexerToken.Value;
                        }
                        else
                        {
                            parserTokenValue = string.Concat(parserTokenValue, lexerToken.Value);
                        }
                    }
                }

                if (parserTokenStart != null && parserTokenEnd != null && parserTokenValue != null)
                {
                    this.Add(new Token<TParserToken>(parserTokenType, parserTokenValue, new Range(parserTokenStart, parserTokenEnd)));
                }
                else if (expectNonEmpty)
                {
                    // Generate error on the next token...
                    this.AcceptAny(parserTokenType, string.Format("Expected {0} value", parserTokenType));
                }
            }

            return this;
        }

        public StatementBuilder<TStatement, TParserToken, TLexerToken> Unexpected(string errorMessage)
        {
            this.AcceptAny(this.UnknownTParserTokenType, errorMessage);
            this.HasError = true;

            return this;
        }

        ////public StatementBuilder<TStatement, TParserToken, TLexerToken> UnknownUntil(TLexerToken lexerTokenType, TParserToken parserTokenType)
        ////{
        ////    return this.AggregateWhileNot(lexerTokenType, this.UnknownTParserTokenType)
        ////        .Expect(lexerTokenType, parserTokenType);

        ////    ////if (this.chainEnabled)
        ////    ////{
        ////    ////    while (this.tokenBuffer.CurrentToken != null && !this.tokenBuffer.Is(lexerTokenType))
        ////    ////    {
        ////    ////        // It wasn't what we expected, now accept it anyway...
        ////    ////        this.Unexpected();
        ////    ////    }

        ////    ////    if (this.tokenBuffer.CurrentToken != null)
        ////    ////    {
        ////    ////        this.Expect(lexerTokenType, parserTokenType);
        ////    ////    }
        ////    ////}

        ////    ////return this;
        ////}

        public StatementBuilder<TStatement, TParserToken, TLexerToken> Enable()
        {
            this.chainEnabled = true;
            return this;
        }
    }
}
