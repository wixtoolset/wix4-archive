// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Linq;
using WixToolset.Simplified.CompilerFrontend.Parser;
using Xunit;

namespace WixToolset.Simplified.UnitTest.Swcore
{
    public class XmlStatementNodeTests
    {
        StatementNodeComparer statementNodeComparer;

        public XmlStatementNodeTests()
        {
            var tokenComparer = new TokenComparer<ParserTokenType>(compareOffsets: false);
            var statementComparer = new StatementComparer<StatementType, ParserTokenType>(tokenComparer);

            this.statementNodeComparer = new StatementNodeComparer(statementComparer);
        }

        [Fact]
        public void SingleNode()
        {
            this.TestParser(
                "<File />",
                StatementNode(
                    0,
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "File")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                ));
        }

        [Fact]
        public void SingleNodeWithNewline()
        {
            this.TestParser(
                "<File \r\n/>",
                StatementNode(
                    0,
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "File")
                    .Token(ParserTokenType.Whitespace, " \r\n")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                ));
        }

        [Fact]
        public void SingleNodeWithNewlineAttributes()
        {
            this.TestParser(
                "<File Id=\"myFile\"\r\n  Name=\"theFile.txt\" />",
                StatementNode(
                    0,
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "File")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "Id")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.PropertyValue, "myFile")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.Whitespace, "\r\n  ")
                    .Token(ParserTokenType.PropertyName, "Name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.PropertyValue, "theFile.txt")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                ));
        }

        [Fact]
        public void SingleElementOpenClose()
        {
            this.TestParser(
                "<Folder></Folder>",
                StatementNode(
                    0,
                    StatementType.ObjectStart,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "Folder")
                    .Token(ParserTokenType.RightAngle, ">"),
                    StatementNode(
                        1,
                        StatementType.ObjectEnd,
                        Token(ParserTokenType.LeftAngle, "<")
                        .Token(ParserTokenType.Slash, "/")
                        .Token(ParserTokenType.Object, "Folder")
                        .Token(ParserTokenType.RightAngle, ">")
                    )
                ));
        }


        [Fact]
        public void SimpleTree()
        {
            this.TestParser(
                "<Folder>\n\t<File/>\n</Folder>",
                StatementNode(
                    0,
                    StatementType.ObjectStart,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "Folder")
                    .Token(ParserTokenType.RightAngle, ">"),
                    StatementNode(
                        1,
                        StatementType.Object,
                        Token(ParserTokenType.LeftAngle, "<")
                        .Token(ParserTokenType.Object, "File")
                        .Token(ParserTokenType.Slash, "/")
                        .Token(ParserTokenType.RightAngle, ">")
                    )
                    .StatementNode(
                        1,
                        StatementType.ObjectEnd,
                        Token(ParserTokenType.LeftAngle, "<")
                        .Token(ParserTokenType.Slash, "/")
                        .Token(ParserTokenType.Object, "Folder")
                        .Token(ParserTokenType.RightAngle, ">")
                    )
                ));
        }


        private static StatementNodeBuilder StatementNode(int indent, StatementType type, TokenListBuilder<ParserTokenType> tokens, StatementNodeBuilder children = null)
        {
            var builder = new StatementNodeBuilder();
            return builder.StatementNode(indent, type, tokens, children);
        }

        private static TokenListBuilder<ParserTokenType> Token(ParserTokenType type, string value)
        {
            var builder = new TokenListBuilder<ParserTokenType>();
            return builder.Token(type, value);
        }

        private void TestParser(string source, StatementNodeBuilder expected)
        {
            // When parsing, the tree always starts with a magical "swix" node...
            ////var swixNode = new StatementNodeBuilder();
            ////swixNode.StatementNode(-1, StatementType.Object, Token(ParserTokenType.Object, "swix"), expected);

            var parser = new XmlParser("test");
            var actual = parser.Parse(source);

            Assert.Equal(expected.First(), actual, this.statementNodeComparer);
        }
    }
}
