// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.CompilerFrontend.Parser
{
    public enum StatementType
    {
        Unknown,
        Ignorable,      // Contains only whitespace and/or comments.
        Comment,
        Object,
        Use,            // rtype only
        ObjectStart,    // xml only // If we create fake ObjectEnd on self-close tags, we could avoid needing the ObjectStart token...
        ObjectEnd,      // xml only
    }

    public static class RtypeStatementParser
    {
        // Assumes that 'start' is a reasonable place from which to start parsing... that is, 
        // it's not the second physical line in a logical continuation line.
        public static IEnumerable<Statement<StatementType, ParserTokenType>> ParseStatements(Position start, ITextProvider textProvider)
        {
            TokenBuffer<LexerTokenType> tokenBuffer = new TokenBuffer<LexerTokenType>(RtypeLexer.LexTokens(start, textProvider));
            RTypeTokenInfo parserTokenInfo = new RTypeTokenInfo();

            while (tokenBuffer.CurrentToken != null)
            {
                var statement = new StatementBuilder<StatementType, ParserTokenType, LexerTokenType>(tokenBuffer, parserTokenInfo);

                // Many statements can start with leading whitespace, we save that off first so that
                // the switch below is easier...
                Token<LexerTokenType> leadingWhitespace;
                tokenBuffer.Accept(LexerTokenType.Whitespace, out leadingWhitespace);

                switch (tokenBuffer.CurrentTokenType)
                {
                    case LexerTokenType.Newline:
                    case LexerTokenType.Hash:   // comment
                        statement.As(StatementType.Ignorable, ParserTokenType.Whitespace, leadingWhitespace)
                            .ReadToEndOfLine();
                        break;

                    case LexerTokenType.UseKeyword:
                        // Should we disallow leading whitespace for the use statement?
                        statement.As(StatementType.Use, ParserTokenType.Whitespace, leadingWhitespace)
                            .Expect(LexerTokenType.UseKeyword, ParserTokenType.UseKeyword)
                            .Accept(LexerTokenType.Whitespace, ParserTokenType.Whitespace)
                            .Expect(LexerTokenType.Identifier, ParserTokenType.NamespacePrefixDeclaration)
                            .ReadToEndOfLine();
                        break;

                    case LexerTokenType.Identifier:
                        // A leading identifier indicates a new message
                        statement.As(StatementType.Object, ParserTokenType.Whitespace, leadingWhitespace)
                            .ExpectRtypeObjectName();

                        // Now, we allow "name=value" and comments and newlines... we have to be smart about
                        // understanding when it's a continuation line and when it's a new object.  We do this by 
                        // looking for an "equals" after the identifier.
                        bool keepConsuming = true;
                        bool beginningOfLine = false;
                        bool canHaveDefaultPropertyValue = true;
                        while (keepConsuming && tokenBuffer.CurrentToken != null)
                        {
                            if (beginningOfLine)
                            {
                                // Temporarily eat any whitespace so we can check the text...
                                tokenBuffer.Accept(LexerTokenType.Whitespace, out leadingWhitespace);
                                var looksLikeProperty = tokenBuffer.LooksLikePropertyAssignment();

                                if (leadingWhitespace != null)
                                {
                                    tokenBuffer.PushBack(leadingWhitespace);
                                }

                                if (!looksLikeProperty)
                                {
                                    keepConsuming = false;
                                    break;
                                }
                            }

                            beginningOfLine = false;

                            // Because whitespace, comments, and newlines can happen often, it's easier
                            // to go ahead and account for them first...
                            statement.AcceptTrailingComment();

                            if (tokenBuffer.Is(LexerTokenType.Newline))
                            {
                                statement.Expect(LexerTokenType.Newline, ParserTokenType.Whitespace);
                                beginningOfLine = true;
                                canHaveDefaultPropertyValue = false;
                                continue;
                            }

                            // If it's allowable to have the default property value, we need to check to see if the token after this
                            // one is an equals or not... if it's not, we assume this is an (optionally) quoted value.
                            if (tokenBuffer.CurrentToken != null && canHaveDefaultPropertyValue && !tokenBuffer.LooksLikePropertyAssignment())
                            {
                                statement.ExpectRtypePropertyValue(ParserTokenType.PropertyValue);
                                canHaveDefaultPropertyValue = false;
                                continue;
                            }

                            while (!statement.HasError && tokenBuffer.CurrentToken != null && !tokenBuffer.Is(LexerTokenType.Newline))
                            {
                                // Read a "Name=Value" pair...
                                statement.ExpectRtypePropertyName()
                                    .Expect(LexerTokenType.Equals, ParserTokenType.Equals)
                                    .ExpectRtypePropertyValue(ParserTokenType.PropertyValue)
                                    .AcceptTrailingComment();
                            }

                            if (statement.HasError)
                            {
                                statement.ReadToEndOfLine();
                                keepConsuming = false;
                            }
                        }
                        break;

                    default:
                        // Unknown, unexpected token!
                        statement.As(StatementType.Unknown, ParserTokenType.Whitespace, leadingWhitespace)
                            .ReadToEndOfLine();
                        break;
                }

                yield return statement.ToStatement();
            }
        }
    }

    internal static partial class StatementExtensions
    {
        // Object names can be:
        //    name
        //    prefix.name
        // Property names can be:
        //    name
        //    attachedPropertyObject.name
        //    prefix.attachedPropertyObject.name
        public static bool LooksLikeCompoundName(this TokenBuffer<LexerTokenType> tokenBuffer, out int length)
        {
            length = 0;

            var tokenType0 = tokenBuffer.LookAhead(0, LexerTokenType.Unknown);
            var tokenType1 = tokenBuffer.LookAhead(1, LexerTokenType.Unknown);
            var tokenType2 = tokenBuffer.LookAhead(2, LexerTokenType.Unknown);
            var tokenType3 = tokenBuffer.LookAhead(3, LexerTokenType.Unknown);
            var tokenType4 = tokenBuffer.LookAhead(4, LexerTokenType.Unknown);

            if (tokenType0 == LexerTokenType.Identifier)
            {
                length = 1;

                if (tokenType1 == LexerTokenType.Period && tokenType2 == LexerTokenType.Identifier)
                {
                    length = 3;

                    if (tokenType3 == LexerTokenType.Period && tokenType4 == LexerTokenType.Identifier)
                    {
                        length = 5;
                    }

                }

                return true;
            }

            return false;
        }

        // See if the name is followed by an equals....
        public static bool LooksLikePropertyAssignment(this TokenBuffer<LexerTokenType> tokenBuffer)
        {
            int nameLength;
            if (tokenBuffer.LooksLikeCompoundName(out nameLength))
            {
                var tokenTypeFollowing = tokenBuffer.LookAhead(nameLength, LexerTokenType.Unknown);

                if (tokenTypeFollowing == LexerTokenType.Equals)
                {
                    return true;
                }
            }

            return false;
        }


        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> ExpectRtypeObjectName(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement)
        {
            int nameLength;
            if (statement.TokenBuffer.LooksLikeCompoundName(out nameLength))
            {
                if (nameLength >= 3)
                {
                    statement.Expect(LexerTokenType.Identifier, ParserTokenType.NamespacePrefix)
                        .Expect(LexerTokenType.Period, ParserTokenType.Period);
                }
            }

            return statement.Expect(LexerTokenType.Identifier, ParserTokenType.Object);
        }

        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> ExpectRtypePropertyName(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement)
        {
            int nameLength;
            if (statement.TokenBuffer.LooksLikeCompoundName(out nameLength))
            {
                if (nameLength >= 5)
                {
                    statement.Expect(LexerTokenType.Identifier, ParserTokenType.NamespacePrefix)
                        .Expect(LexerTokenType.Period, ParserTokenType.Period);
                }

                if (nameLength >= 3)
                {
                    statement.Expect(LexerTokenType.Identifier, ParserTokenType.AttachedPropertyObject)
                        .Expect(LexerTokenType.Period, ParserTokenType.Period);
                }
            }

            return statement.Expect(LexerTokenType.Identifier, ParserTokenType.PropertyName);
        }

        // Expects a "quoted value", but doesn't necessarily require the quotes... should that be
        // an explicit ''allowNoQuotes parameter for clarity?
        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> ExpectRtypePropertyValue(
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
                            // Aggregate until whitespace, comment (hash), newline...
                            return ifNoSingle.AggregateWhile(l =>
                                    l != LexerTokenType.Whitespace &&
                                    l != LexerTokenType.Hash &&
                                    l != LexerTokenType.Newline,
                                parserTokenType,
                                expectNonEmpty: true);
                        });
                });
        }

        // Accepts whitespace and comment.
        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> AcceptTrailingComment(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement)
        {
            statement.Accept(LexerTokenType.Whitespace, ParserTokenType.Whitespace);
            if (statement.TokenBuffer.Is(LexerTokenType.Hash))
            {
                statement.AggregateWhileNot(LexerTokenType.Newline, ParserTokenType.Comment);
            }

            // Note that we *don't* eat the trailing Newline, because callers may need to know about it
            // for some line-based parsing logic.
            return statement;
        }

        public static StatementBuilder<StatementType, ParserTokenType, LexerTokenType> ReadToEndOfLine(
            this StatementBuilder<StatementType, ParserTokenType, LexerTokenType> statement)
        {
            // TODO: Flag unknown token (AggregateWhileNot) here, or let consumer deal with it?
            statement.Enable()
                .AcceptTrailingComment()
                .AggregateWhileNot(LexerTokenType.Newline, ParserTokenType.Unknown);

            if (statement.TokenBuffer.CurrentToken != null)
            {
                statement.Enable()
                    .Expect(LexerTokenType.Newline, ParserTokenType.Whitespace);
            }

            return statement;
        }
    }
}
