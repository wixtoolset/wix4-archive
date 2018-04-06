// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.TypedMessageGenerator.Parser
{
    internal enum StatementType
    {
        Unknown,
        MessageTypeDefiniton,
        Message,
        MessageInstance,
        Ignorable,      // Contains only whitespace and/or comments (?)
    }

    internal class StatementParser
    {
        public static IEnumerable<Statement<StatementType, ParserTokenType>> ParseStatements(Position start, ITextProvider textProvider)
        {
            TokenBuffer<LexerTokenType> tokenBuffer = new TokenBuffer<LexerTokenType>(Lexer.LexTokens(start, textProvider));
            MsgsTokenInfo parserTokenInfo = new MsgsTokenInfo();

            while (tokenBuffer.CurrentToken != null)
            {
                var statement = new StatementBuilder<StatementType, ParserTokenType, LexerTokenType>(tokenBuffer, parserTokenInfo);
                bool readToEndOfLine = true;

                switch (tokenBuffer.CurrentTokenType)
                {
                    case LexerTokenType.Newline:
                        statement.As(StatementType.Ignorable, ParserTokenType.Whitespace);
                        // *Don't* ReadToEndOfLine(), because we've already read it!
                        readToEndOfLine = false;
                        break;

                    case LexerTokenType.Comment:
                        statement.As(StatementType.Ignorable, ParserTokenType.Comment);
                        break;

                    case LexerTokenType.TypeKeyword:
                        statement.As(StatementType.MessageTypeDefiniton, ParserTokenType.TypeKeyword)
                            .Expect(LexerTokenType.Whitespace, ParserTokenType.Whitespace)
                            .Expect(LexerTokenType.Identifier, ParserTokenType.MessageTypeDefinition)
                            .Expect(LexerTokenType.Whitespace, ParserTokenType.Whitespace)
                            .Expect(LexerTokenType.Number, ParserTokenType.MessageTypeRange)
                            .Expect(LexerTokenType.Whitespace, ParserTokenType.Whitespace)
                            .Expect(LexerTokenType.Number, ParserTokenType.MessageTypeRange);
                        break;

                    case LexerTokenType.Identifier:
                        // A leading identifier indicates a new message
                        statement.As(StatementType.Message, ParserTokenType.MessageType)
                            .Expect(LexerTokenType.Whitespace, ParserTokenType.Whitespace)
                            .Expect(LexerTokenType.Identifier, ParserTokenType.MessageName)
                            .Accept(LexerTokenType.Whitespace, ParserTokenType.Whitespace)
                            .Accept(LexerTokenType.Number, ParserTokenType.MessageTypeRange);
                        break;

                    case LexerTokenType.Whitespace:
                        // Whitespace is either a message instance, or blank or a comment... we have to look
                        // ahead to see what it is.
                        Token<LexerTokenType> firstToken;
                        tokenBuffer.Accept(LexerTokenType.Whitespace, out firstToken);

                        if (tokenBuffer.Is(LexerTokenType.Newline) || tokenBuffer.Is(LexerTokenType.Comment))
                        {
                            statement.As(StatementType.Ignorable, ParserTokenType.Whitespace, firstToken);
                        }
                        else
                        {
                            // Beginning of a message instance...
                            statement.As(StatementType.MessageInstance, ParserTokenType.Whitespace, firstToken);

                            // Loop through the remaining tokens on this line, interpreting the replacements and escapes...
                            while (tokenBuffer.CurrentToken != null &&
                                !tokenBuffer.Is(LexerTokenType.Newline) &&
                                !tokenBuffer.Is(LexerTokenType.Comment))
                            {
                                switch (tokenBuffer.CurrentTokenType)
                                {
                                    default:    // identifiers, numbers, etc. get treated as values
                                    case LexerTokenType.Value:
                                        statement.AcceptAny(ParserTokenType.Value);
                                        break;

                                    case LexerTokenType.Escape:
                                        statement.Expect(LexerTokenType.Escape, ParserTokenType.Escape);
                                        break;

                                    case LexerTokenType.LeftBrace:
                                        // parse the replacement format...
                                        statement.Accept(LexerTokenType.LeftBrace, ParserTokenType.LeftBrace)
                                            .Accept(LexerTokenType.LeftBracket, ParserTokenType.LeftBracket,
                                                ifBracket =>
                                                {
                                                    return ifBracket.Accept(LexerTokenType.Identifier, ParserTokenType.ReplacementType,
                                                        ifType =>
                                                        {
                                                            return ifType.Accept(LexerTokenType.Comma, ParserTokenType.Comma,
                                                                ifComma => ifComma.Expect(LexerTokenType.Number, ParserTokenType.ReplacementPosition));
                                                        },
                                                        ifNoType =>
                                                        {
                                                            return ifNoType.Expect(LexerTokenType.Number, ParserTokenType.ReplacementPosition);
                                                        })
                                                        .Expect(LexerTokenType.RightBracket, ParserTokenType.RightBracket);
                                                })
                                            .Expect(LexerTokenType.Identifier, ParserTokenType.ReplacementName)
                                            .Accept(LexerTokenType.Comma, ParserTokenType.Comma,
                                                ifComma => ifComma.Expect(LexerTokenType.Number, ParserTokenType.ReplacementAlignment))
                                            .Accept(LexerTokenType.Colon, ParserTokenType.Colon,
                                                ifColon => ifColon.AggregateWhileNot(LexerTokenType.RightBrace, ParserTokenType.ReplacementFormat))
                                            .Expect(LexerTokenType.RightBrace, ParserTokenType.RightBrace);
                                        break;

                                    case LexerTokenType.RightBrace: // needs to be escaped!
                                        // Unknown, unexpected token!
                                        // Note that we have to double the '}', because this is used in a string.Format call!
                                        statement.Unexpected("Unescaped right-brace. This must be escaped (\\}}) for use in the message.");
                                        break;
                                }

                                // We re-enable the statement so that we can keep consuming tokens...
                                statement.Enable();
                            }
                        }
                        break;

                    default:
                        // Unknown, unexpected token!
                        statement.As(StatementType.Unknown, ParserTokenType.Unknown);
                        break;
                }

                if (readToEndOfLine)
                {
                    statement.ReadToEndOfLine(tokenBuffer);
                }

                yield return statement.ToStatement();
            }
        }
    }

    internal static class StatementExtensions
    {
        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> ReadToEndOfLine(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement,
            TokenBuffer<LexerTokenType> tokenBuffer)
        {
            statement.Enable()
                .Accept(LexerTokenType.Whitespace, ParserTokenType.Whitespace)
                .Accept(LexerTokenType.Comment, ParserTokenType.Comment)
                .AggregateWhileNot(LexerTokenType.Newline, ParserTokenType.Unknown);

            if (tokenBuffer.CurrentToken != null)
            {
                statement.Enable()
                    .Expect(LexerTokenType.Newline, ParserTokenType.Whitespace);
            }

            return statement;
        }
    }
}
