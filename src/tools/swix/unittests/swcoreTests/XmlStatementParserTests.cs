// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Linq;
using WixToolset.Simplified.CompilerFrontend.Parser;
using WixToolset.Simplified.ParserCore;
using Xunit;

namespace WixToolset.Simplified.UnitTest.Swcore
{
    public class XmlStatementParserTests
    {
        TokenComparer<ParserTokenType> tokenComparer;
        StatementComparer<StatementType, ParserTokenType> statementComparer;

        public XmlStatementParserTests()
        {
            this.tokenComparer = new TokenComparer<ParserTokenType>(compareOffsets: false);
            this.statementComparer = new StatementComparer<StatementType, ParserTokenType>(this.tokenComparer);
        }

        [Fact]
        public void SimpleObject()
        {
            this.TestParser(
                "<File />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "File")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void SimpleObjectStart()
        {
            this.TestParser(
                "<File>",
                Statement(
                    StatementType.ObjectStart,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "File")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void SimpleObjectEnd()
        {
            this.TestParser(
                "</File>",
                Statement(
                    StatementType.ObjectEnd,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.Object, "File")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void SimplePropertyValue()
        {
            this.TestParser(
                "<Package Name=something />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "Package")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "Name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "something")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void QuotedPropertyValue()
        {
            this.TestParser(
                "<Package Name=\"something\" />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "Package")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "Name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.PropertyValue, "something")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void QuotedComplexPropertyValue()
        {
            // CN=Microsoft Corporate Root Authority,OU=ITG,O=Microsoft,L=Redmond,S=WA,C=US
            this.TestParser(
                "<Package Publisher=\"CN=Microsoft,C=US\" />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "Package")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "Publisher")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.PropertyValue, "CN=Microsoft,C=US")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void BareComplexPropertyValue()
        {
            this.TestParser(
                "<FileType Extension=.tax />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "FileType")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "Extension")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, ".tax")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }
        [Fact]
        public void SimpleComment()
        {
            this.TestParser(
                "<!-- comment -->",
                Statement(
                    StatementType.Comment,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Comment, "!")
                    .Token(ParserTokenType.Comment, "--")
                    .Token(ParserTokenType.Comment, " comment ")
                    .Token(ParserTokenType.Comment, "--")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void QuotedHexPropertyValue()
        {
            this.TestParser(
                "<Color White=\"#FFFFFF\" />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "Color")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "White")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.PropertyValue, "#FFFFFF")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        // REVIEW: The XML format doesn't support default values?
        ////[Fact]
        ////public void SimpleDefaultPropertyValue()
        ////{
        ////    this.TestParser(
        ////        "<Package Something />",
        ////        Statement(
        ////            StatementType.Object,
        ////            Token(ParserTokenType.LeftAngle, "<")
        ////            .Token(ParserTokenType.Object, "Package")
        ////            .Token(ParserTokenType.Whitespace, " ")
        ////            .Token(ParserTokenType.PropertyValue, "Something")
        ////            .Token(ParserTokenType.Whitespace, " ")
        ////            .Token(ParserTokenType.Slash, "/")
        ////            .Token(ParserTokenType.RightAngle, ">")
        ////            )
        ////        );
        ////}

        [Fact]
        public void ObjectWithNamespace()
        {
            this.TestParser(
                "<appx:Capability />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.NamespacePrefix, "appx")
                    .Token(ParserTokenType.Colon, ":")
                    .Token(ParserTokenType.Object, "Capability")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void AttachedProperty()
        {
            this.TestParser(
                "<File Condition.Install=true />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "File")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.AttachedPropertyObject, "Condition")
                    .Token(ParserTokenType.Period, ".")
                    .Token(ParserTokenType.PropertyName, "Install")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "true")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }

        [Fact]
        public void AttachedPropertyWithNamespace()
        {
            this.TestParser(
                "<File msi:Condition.Install=true />",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.LeftAngle, "<")
                    .Token(ParserTokenType.Object, "File")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.NamespacePrefix, "msi")
                    .Token(ParserTokenType.Colon, ":")
                    .Token(ParserTokenType.AttachedPropertyObject, "Condition")
                    .Token(ParserTokenType.Period, ".")
                    .Token(ParserTokenType.PropertyName, "Install")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "true")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.Slash, "/")
                    .Token(ParserTokenType.RightAngle, ">")
                    )
                );
        }


        ////[Fact]
        ////public void TwoTrivialObjects()
        ////{
        ////    this.TestParser(
        ////        "one\ntwo",
        ////        Statement(
        ////            StatementType.Object,
        ////            Token(ParserTokenType.Object, "one")
        ////            .Token(ParserTokenType.Whitespace, "\n")
        ////            )
        ////        .Statement(
        ////            StatementType.Object,
        ////            Token(ParserTokenType.Object, "two")
        ////            )
        ////        );
        ////}

        ////[Fact]
        ////public void TwoObjects()
        ////{
        ////    this.TestParser(
        ////        "one value\ntwo name=value",
        ////        Statement(
        ////            StatementType.Object,
        ////            Token(ParserTokenType.Object, "one")
        ////            .Token(ParserTokenType.Whitespace, " ")
        ////            .Token(ParserTokenType.PropertyValue, "value")
        ////            .Token(ParserTokenType.Whitespace, "\n")
        ////            )
        ////        .Statement(
        ////            StatementType.Object,
        ////            Token(ParserTokenType.Object, "two")
        ////            .Token(ParserTokenType.Whitespace, " ")
        ////            .Token(ParserTokenType.PropertyName, "name")
        ////            .Token(ParserTokenType.Equals, "=")
        ////            .Token(ParserTokenType.PropertyValue, "value")
        ////            )
        ////        );
        ////}

        ////[Fact]
        ////public void TwoObjectsWithIndent()
        ////{
        ////    this.TestParser(
        ////        "one value\n\ttwo name=value",
        ////        Statement(
        ////            StatementType.Object,
        ////            Token(ParserTokenType.Object, "one")
        ////            .Token(ParserTokenType.Whitespace, " ")
        ////            .Token(ParserTokenType.PropertyValue, "value")
        ////            .Token(ParserTokenType.Whitespace, "\n")
        ////            )
        ////        .Statement(
        ////            StatementType.Object,
        ////            Token(ParserTokenType.Whitespace, "\t")
        ////            .Token(ParserTokenType.Object, "two")
        ////            .Token(ParserTokenType.Whitespace, " ")
        ////            .Token(ParserTokenType.PropertyName, "name")
        ////            .Token(ParserTokenType.Equals, "=")
        ////            .Token(ParserTokenType.PropertyValue, "value")
        ////            )
        ////        );
        ////}

        ////[Fact]
        ////public void Continuation()
        ////{
        ////    this.TestParser(
        ////        "folder\n\tname=value",
        ////        Statement(
        ////            StatementType.Object,
        ////            Token(ParserTokenType.Object, "folder")
        ////            .Token(ParserTokenType.Whitespace, "\n")
        ////            .Token(ParserTokenType.Whitespace, "\t")
        ////            .Token(ParserTokenType.PropertyName, "name")
        ////            .Token(ParserTokenType.Equals, "=")
        ////            .Token(ParserTokenType.PropertyValue, "value")
        ////        ));
        ////}

        ////[Fact]
        ////public void UseStatement()
        ////{
        ////    this.TestParser(
        ////        "use appx",
        ////        Statement(
        ////            StatementType.Use,
        ////            Token(ParserTokenType.UseKeyword, "use")
        ////            .Token(ParserTokenType.Whitespace, " ")
        ////            .Token(ParserTokenType.NamespacePrefixDeclaration, "appx")
        ////            )
        ////        );
        ////}

        ////        [Fact]
        ////        public void WixlibFileSearchTest()
        ////        {
        ////            this.TestParser(
        ////                @"use msi
        ////
        ////msi.fileSearch id=fsC component=c00d5a59-1bd1-4f6b-9b2b-27e0ae64020b
        ////
        ////msi.fileSearch id=fsRK registry=HKLM\Search\Registry\Key\",
        ////                Statement(
        ////                ////    StatementType.Use,
        ////                ////    Token(ParserTokenType.UseKeyword, "use")
        ////                ////    .Token(ParserTokenType.Whitespace, " ")
        ////                ////    .Token(ParserTokenType.NamespacePrefixDeclaration, "msi")
        ////                ////    .Token(ParserTokenType.Whitespace, "\r\n")
        ////                ////    )
        ////                ////.Statement(
        ////                    StatementType.Ignorable,
        ////                    Token(ParserTokenType.Whitespace, "\r\n")
        ////                    )
        ////                .Statement(
        ////                    StatementType.Object,
        ////                    Token(ParserTokenType.NamespacePrefix, "msi")
        ////                    .Token(ParserTokenType.Period, ".")
        ////                    .Token(ParserTokenType.Object, "fileSearch")
        ////                    .Token(ParserTokenType.Whitespace, " ")
        ////                    .Token(ParserTokenType.PropertyName, "id")
        ////                    .Token(ParserTokenType.Equals, "=")
        ////                    .Token(ParserTokenType.PropertyValue, "fsC")
        ////                    .Token(ParserTokenType.Whitespace, " ")
        ////                    .Token(ParserTokenType.PropertyName, "component")
        ////                    .Token(ParserTokenType.Equals, "=")
        ////                    .Token(ParserTokenType.PropertyValue, "c00d5a59-1bd1-4f6b-9b2b-27e0ae64020b")
        ////                    .Token(ParserTokenType.Whitespace, "\r\n")
        ////                    )
        ////                .Statement(
        ////                    StatementType.Ignorable,
        ////                    Token(ParserTokenType.Whitespace, "\r\n")
        ////                    )
        ////                .Statement(
        ////                    StatementType.Object,
        ////                    Token(ParserTokenType.NamespacePrefix, "msi")
        ////                    .Token(ParserTokenType.Period, ".")
        ////                    .Token(ParserTokenType.Object, "fileSearch")
        ////                    .Token(ParserTokenType.Whitespace, " ")
        ////                    .Token(ParserTokenType.PropertyName, "id")
        ////                    .Token(ParserTokenType.Equals, "=")
        ////                    .Token(ParserTokenType.PropertyValue, "fsRK")
        ////                    .Token(ParserTokenType.Whitespace, " ")
        ////                    .Token(ParserTokenType.PropertyName, "registry")
        ////                    .Token(ParserTokenType.Equals, "=")
        ////                    .Token(ParserTokenType.PropertyValue, "HKLM\\Search\\Registry\\Key\\")
        ////                    )
        ////                );
        ////        }


        private static StatementListBuilder<StatementType, ParserTokenType> Statement(StatementType type, TokenListBuilder<ParserTokenType> tokens)
        {
            var builder = new StatementListBuilder<StatementType, ParserTokenType>();
            return builder.Statement(type, tokens);
        }

        private static TokenListBuilder<ParserTokenType> Token(ParserTokenType type, string value)
        {
            var builder = new TokenListBuilder<ParserTokenType>();
            return builder.Token(type, value);
        }

        private void TestParser(string source, StatementListBuilder<StatementType, ParserTokenType> expected)
        {
            StringTextProvider textProvider = new StringTextProvider(source);
            var actual = XmlStatementParser.ParseStatements(new Position(0, 0, 0), textProvider);

            Assert.True(expected.SequenceEqual(actual, this.statementComparer));
        }
    }
}
