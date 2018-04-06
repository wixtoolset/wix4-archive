// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.CompilerFrontend.Parser
{
    public static class XmlStatementParser
    {
        // Assumes that 'start' is a reasonable place from which to start parsing... that is, 
        // it's not inside a tag.
        public static IEnumerable<Statement<StatementType, ParserTokenType>> ParseStatements(Position start, ITextProvider textProvider)
        {
            TokenBuffer<LexerTokenType> tokenBuffer = new TokenBuffer<LexerTokenType>(XmlLexer.LexTokens(start, textProvider));
            XmlTokenInfo parserTokenInfo = new XmlTokenInfo();

            while (tokenBuffer.CurrentToken != null)
            {
                var statement = new StatementBuilder<StatementType, ParserTokenType, LexerTokenType>(tokenBuffer, parserTokenInfo);

                switch (tokenBuffer.CurrentTokenType)
                {
                    case LexerTokenType.Whitespace:
                    case LexerTokenType.Newline:
                        statement.As(StatementType.Ignorable).AcceptAllWhitespace();
                        break;

                    case LexerTokenType.LeftAngle:
                        // Once we've seen a left angle bracket, it's either a comment or an
                        // object open or close.
                        statement.Expect(LexerTokenType.LeftAngle, ParserTokenType.LeftAngle)
                            .Accept(LexerTokenType.Exclamation, ParserTokenType.Comment,
                                isComment =>
                                {
                                    return isComment.As(StatementType.Comment)
                                        .Expect(LexerTokenType.DoubleDash, ParserTokenType.Comment)
                                        .AggregateWhileNot(LexerTokenType.DoubleDash, ParserTokenType.Comment)
                                        .Expect(LexerTokenType.DoubleDash, ParserTokenType.Comment)
                                        .Expect(LexerTokenType.RightAngle, ParserTokenType.RightAngle);
                                },
                                isNotComment =>
                                {
                                    return isNotComment.Accept(LexerTokenType.Question, ParserTokenType.Unknown,
                                        isProcessing =>
                                        {
                                            return isProcessing.As(StatementType.Ignorable)
                                                .AggregateWhile(l =>
                                                        l != LexerTokenType.Question &&
                                                        l != LexerTokenType.RightAngle,
                                                    ParserTokenType.Unknown,
                                                    expectNonEmpty: true)
                                                .Expect(LexerTokenType.Question, ParserTokenType.Unknown)
                                                .Expect(LexerTokenType.RightAngle, ParserTokenType.RightAngle);
                                        },
                                        isNotProcessing =>
                                        {
                                            return isNotProcessing.Accept(LexerTokenType.Slash, ParserTokenType.Slash,
                                                isCloseTag =>
                                                {
                                                    return isCloseTag.As(StatementType.ObjectEnd)
                                                        .AcceptAllWhitespace()
                                                        .ExpectXmlObjectName()
                                                        .AcceptAllWhitespace()
                                                        .Expect(LexerTokenType.RightAngle, ParserTokenType.RightAngle);
                                                },
                                                isOpenTag =>
                                                {
                                                    isOpenTag.As(StatementType.ObjectStart)
                                                        .AcceptAllWhitespace()
                                                        .ExpectXmlObjectName()
                                                        .AcceptAllWhitespace();

                                                    while (!isOpenTag.HasError && isOpenTag.TokenBuffer.CurrentTokenType == LexerTokenType.Identifier)
                                                    {
                                                        isOpenTag.ExpectXmlPropertyName()
                                                            .AcceptAllWhitespace()
                                                            .Expect(LexerTokenType.Equals, ParserTokenType.Equals)
                                                            .AcceptAllWhitespace()
                                                            .ExpectXmlPropertyValue(ParserTokenType.PropertyValue)
                                                            .AcceptAllWhitespace();
                                                    }

                                                    return isOpenTag.Accept(LexerTokenType.Slash, ParserTokenType.Slash,
                                                        isSelfClose =>
                                                        {
                                                            // If there's a trailing '/' inside the tag, it's self-closing.
                                                            return isSelfClose.As(StatementType.Object);
                                                        })
                                                        .Expect(LexerTokenType.RightAngle, ParserTokenType.RightAngle);
                                                });
                                        });
                                });
                        break;

                    default:
                        // Unknown, unexpected token!
                        statement.As(StatementType.Unknown, ParserTokenType.Unknown)
                            .AggregateWhileNot(LexerTokenType.LeftAngle, ParserTokenType.Unknown);
                        break;
                }

                yield return statement.ToStatement();
            }
        }
    }

    internal static partial class StatementExtensions
    {
        public static bool LooksLikeQualifiedName(
            this TokenBuffer<LexerTokenType> tokenBuffer,
            LexerTokenType qualifierDelimeter)
        {
            var tokenType0 = tokenBuffer.LookAhead(0, LexerTokenType.Unknown);
            var tokenType1 = tokenBuffer.LookAhead(1, LexerTokenType.Unknown);

            return (tokenType0 == LexerTokenType.Identifier &&
                tokenType1 == qualifierDelimeter);
        }

        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> AcceptNameQualifier(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement,
            LexerTokenType qualifierDelimeter,
            ParserTokenType identifierType,
            ParserTokenType qualifierType)
        {
            if (statement.TokenBuffer.LooksLikeQualifiedName(qualifierDelimeter))
            {
                statement.Expect(LexerTokenType.Identifier, identifierType)
                    .Expect(qualifierDelimeter, qualifierType);
            }

            return statement;
        }

        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> ExpectXmlObjectName(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement)
        {
            return statement.AcceptNameQualifier(LexerTokenType.Colon, ParserTokenType.NamespacePrefix, ParserTokenType.Colon)
                .Expect(LexerTokenType.Identifier, ParserTokenType.Object);
        }

        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> ExpectXmlPropertyName(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement)
        {
            return statement.AcceptNameQualifier(LexerTokenType.Colon, ParserTokenType.NamespacePrefix, ParserTokenType.Colon)
                .AcceptNameQualifier(LexerTokenType.Period, ParserTokenType.AttachedPropertyObject, ParserTokenType.Period)
                .Expect(LexerTokenType.Identifier, ParserTokenType.PropertyName);
        }

        // Expects a "quoted value", but doesn't necessarily require the quotes... should that be
        // an explicit ''allowNoQuotes parameter for clarity?
        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> ExpectXmlPropertyValue(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement,
            ParserTokenType parserTokenType)
        {
            return statement.Accept(LexerTokenType.DoubleQuote, ParserTokenType.DoubleQuote,
                ifDoubleQuote =>
                {
                    return ifDoubleQuote.AggregateWhileNot(LexerTokenType.DoubleQuote, parserTokenType)
                        .Expect(LexerTokenType.DoubleQuote, ParserTokenType.DoubleQuote);
                },
                ifNoDoubleQuote =>
                {
                    return ifNoDoubleQuote.Accept(LexerTokenType.SingleQuote, ParserTokenType.SingleQuote,
                        ifSingleQuote =>
                        {
                            return ifSingleQuote.AggregateWhileNot(LexerTokenType.SingleQuote, parserTokenType)
                                .Expect(LexerTokenType.SingleQuote, ParserTokenType.SingleQuote);
                        },
                        ifNoSingle =>
                        {
                            // Aggregate until whitespace or newline...
                            return ifNoSingle.AggregateWhile(l =>
                                    l != LexerTokenType.Whitespace &&
                                    l != LexerTokenType.Newline,
                                parserTokenType,
                                expectNonEmpty: true);
                        });
                });
        }

        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> AcceptAllWhitespace(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement)
        {
            return statement.AggregateWhile(
                lt => lt.Equals(LexerTokenType.Whitespace) || lt.Equals(LexerTokenType.Newline),
                ParserTokenType.Whitespace);
        }
    }
}
