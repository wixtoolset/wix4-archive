// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Linq;
using WixToolset.Simplified.CompilerFrontend.Parser;
using WixToolset.Simplified.ParserCore;
using Xunit;

namespace WixToolset.Simplified.UnitTest.Swcore
{
    public class RtypeLexerTests
    {
        TokenComparer<LexerTokenType> comparer = new TokenComparer<LexerTokenType>();

        public RtypeLexerTests()
        {
        }

        [Fact]
        public void SimpleObject()
        {
            this.TestLexer(
                "file",
                Token(LexerTokenType.Identifier, "file")
                );
        }

        [Fact]
        public void SimplePropertyValue()
        {
            this.TestLexer(
                "package name=something",
                Token(LexerTokenType.Identifier, "package")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "name")
                    .Token(LexerTokenType.Equals, "=")
                    .Token(LexerTokenType.Identifier, "something")
                );
        }

        [Fact]
        public void QuotedPropertyValue()
        {
            this.TestLexer(
                "package name=\"something\"",
                Token(LexerTokenType.Identifier, "package")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "name")
                    .Token(LexerTokenType.Equals, "=")
                    .Token(LexerTokenType.DoubleQuote, "\"")
                    .Token(LexerTokenType.Identifier, "something")
                    .Token(LexerTokenType.DoubleQuote, "\"")
                );
        }

        [Fact]
        public void BareComplexPropertyValue()
        {
            this.TestLexer(
                "fileType extension=.tax",
                Token(LexerTokenType.Identifier, "fileType")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "extension")
                    .Token(LexerTokenType.Equals, "=")
                    .Token(LexerTokenType.Period, ".")
                    .Token(LexerTokenType.Identifier, "tax")
                );
        }

        [Fact]
        public void SimpleDefaultPropertyValue()
        {
            this.TestLexer(
                "package something",
                Token(LexerTokenType.Identifier, "package")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "something")
                );
        }

        [Fact]
        public void ObjectWithNamespace()
        {
            this.TestLexer(
                "appx.capability",
                Token(LexerTokenType.Identifier, "appx")
                    .Token(LexerTokenType.Period, ".")
                    .Token(LexerTokenType.Identifier, "capability")
                );
        }

        [Fact]
        public void SimpleComment()
        {
            this.TestLexer(
                "# comment",
                Token(LexerTokenType.Hash, "#")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "comment")
                );
        }

        [Fact]
        public void QuotedHexValue()
        {
            this.TestLexer(
                "color white=\"#FFFFFF\"",
                Token(LexerTokenType.Identifier, "color")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "white")
                    .Token(LexerTokenType.Equals, "=")
                    .Token(LexerTokenType.DoubleQuote, "\"")
                    .Token(LexerTokenType.Hash, "#")
                    .Token(LexerTokenType.Identifier, "FFFFFF")
                    .Token(LexerTokenType.DoubleQuote, "\"")
                );
        }

        [Fact]
        public void UseStatement()
        {
            this.TestLexer(
                "use appx",
                Token(LexerTokenType.UseKeyword, "use")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "appx")
                );
        }

        private static TokenListBuilder<LexerTokenType> Token(LexerTokenType type, string value)
        {
            var builder = new TokenListBuilder<LexerTokenType>();
            return builder.Token(type, value);
        }

        private void TestLexer(string source, TokenListBuilder<LexerTokenType> expected)
        {
            var actual = RtypeLexer.LexTokens(source, new Position(0, 0, 0));
            Assert.True(expected.SequenceEqual(actual, this.comparer));
        }
    }
}
