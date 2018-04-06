// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Linq;
using WixToolset.Simplified.CompilerFrontend.Parser;
using WixToolset.Simplified.ParserCore;
using Xunit;

namespace WixToolset.Simplified.UnitTest.Swcore
{
    public class XmlLexerTests
    {
        TokenComparer<LexerTokenType> comparer = new TokenComparer<LexerTokenType>();

        public XmlLexerTests()
        {
        }

        [Fact]
        public void SimpleObject()
        {
            this.TestLexer(
                "<File />",
                Token(LexerTokenType.LeftAngle, "<")
                    .Token(LexerTokenType.Identifier, "File")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Slash, "/")
                    .Token(LexerTokenType.RightAngle, ">")
                );
        }

        [Fact]
        public void SimplePropertyValue()
        {
            this.TestLexer(
                "<Package Name=something />",
                Token(LexerTokenType.LeftAngle, "<")
                    .Token(LexerTokenType.Identifier, "Package")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "Name")
                    .Token(LexerTokenType.Equals, "=")
                    .Token(LexerTokenType.Identifier, "something")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Slash, "/")
                    .Token(LexerTokenType.RightAngle, ">")
                );
        }

        [Fact]
        public void QuotedPropertyValue()
        {
            this.TestLexer(
                "<Package Name=\"something\" />",
                Token(LexerTokenType.LeftAngle, "<")
                    .Token(LexerTokenType.Identifier, "Package")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "Name")
                    .Token(LexerTokenType.Equals, "=")
                    .Token(LexerTokenType.DoubleQuote, "\"")
                    .Token(LexerTokenType.Identifier, "something")
                    .Token(LexerTokenType.DoubleQuote, "\"")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Slash, "/")
                    .Token(LexerTokenType.RightAngle, ">")
                );
        }

        [Fact]
        public void BareComplexPropertyValue()
        {
            this.TestLexer(
                "<FileType Extension=.tax />",
                Token(LexerTokenType.LeftAngle, "<")
                    .Token(LexerTokenType.Identifier, "FileType")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "Extension")
                    .Token(LexerTokenType.Equals, "=")
                    .Token(LexerTokenType.Period, ".")
                    .Token(LexerTokenType.Identifier, "tax")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Slash, "/")
                    .Token(LexerTokenType.RightAngle, ">")
                );
        }

        // REVIEW: XML format does not support default values?
        ////[Fact]
        ////public void SimpleDefaultPropertyValue()
        ////{
        ////    this.TestLexer(
        ////        "<Package Something />",
        ////        Token(LexerTokenType.LeftAngle, "<")
        ////            .Token(LexerTokenType.Identifier, "Package")
        ////            .Token(LexerTokenType.Whitespace, " ")
        ////            .Token(LexerTokenType.Identifier, "Something")
        ////            .Token(LexerTokenType.Whitespace, " ")
        ////            .Token(LexerTokenType.Slash, "/")
        ////            .Token(LexerTokenType.RightAngle, ">")
        ////        );
        ////}

        [Fact]
        public void ObjectWithNamespace()
        {
            this.TestLexer(
                "<appx:Capability />",
                Token(LexerTokenType.LeftAngle, "<")
                    .Token(LexerTokenType.Identifier, "appx")
                    .Token(LexerTokenType.Colon, ":")
                    .Token(LexerTokenType.Identifier, "Capability")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Slash, "/")
                    .Token(LexerTokenType.RightAngle, ">")
                );
        }

        [Fact]
        public void SimpleComment()
        {
            this.TestLexer(
                "<!-- comment -->",
                Token(LexerTokenType.LeftAngle, "<")
                    .Token(LexerTokenType.Exclamation, "!")
                    .Token(LexerTokenType.DoubleDash, "--")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "comment")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.DoubleDash, "--")
                    .Token(LexerTokenType.RightAngle, ">")
                );
        }

        [Fact]
        public void QuotedHexValue()
        {
            this.TestLexer(
                "<Color White=\"#FFFFFF\" />",
                Token(LexerTokenType.LeftAngle, "<")
                    .Token(LexerTokenType.Identifier, "Color")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Identifier, "White")
                    .Token(LexerTokenType.Equals, "=")
                    .Token(LexerTokenType.DoubleQuote, "\"")
                    .Token(LexerTokenType.Value, "#")
                    .Token(LexerTokenType.Identifier, "FFFFFF")
                    .Token(LexerTokenType.DoubleQuote, "\"")
                    .Token(LexerTokenType.Whitespace, " ")
                    .Token(LexerTokenType.Slash, "/")
                    .Token(LexerTokenType.RightAngle, ">")
                );
        }

        ////[Fact]
        ////public void UseStatement()
        ////{
        ////    this.TestLexer(
        ////        "use appx",
        ////        Token(LexerTokenType.UseKeyword, "use")
        ////            .Token(LexerTokenType.Whitespace, " ")
        ////            .Token(LexerTokenType.Identifier, "appx")
        ////        );
        ////}

        private static TokenListBuilder<LexerTokenType> Token(LexerTokenType type, string value)
        {
            var builder = new TokenListBuilder<LexerTokenType>();
            return builder.Token(type, value);
        }

        private void TestLexer(string source, TokenListBuilder<LexerTokenType> expected)
        {
            var actual = XmlLexer.LexTokens(source, new Position(0, 0, 0));
            Assert.True(expected.SequenceEqual(actual, this.comparer));
        }
    }
}
