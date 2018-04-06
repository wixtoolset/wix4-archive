// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using WixToolset.Simplified.CompilerFrontend.Parser;
using WixToolset.Simplified.ParserCore;
using Xunit;

namespace WixToolset.Simplified.UnitTest.Swcore
{
    public class RtypeStatementParserTests
    {
        TokenComparer<ParserTokenType> tokenComparer;
        StatementComparer<StatementType, ParserTokenType> statementComparer;

        public RtypeStatementParserTests()
        {
            this.tokenComparer = new TokenComparer<ParserTokenType>(compareOffsets: false);
            this.statementComparer = new StatementComparer<StatementType, ParserTokenType>(this.tokenComparer);
        }

        [Fact]
        public void SimpleObject()
        {
            this.TestParser(
                "file",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "file")
                    )
                );
        }

        [Fact]
        public void SimplePropertyValue()
        {
            this.TestParser(
                "package name=something",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "package")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "something")
                    )
                );
        }

        [Fact]
        public void QuotedPropertyValue()
        {
            this.TestParser(
                "package name=\"something\"",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "package")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.PropertyValue, "something")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    )
                );
        }

        [Fact]
        public void QuotedComplexPropertyValue()
        {
            // CN=Microsoft Corporate Root Authority,OU=ITG,O=Microsoft,L=Redmond,S=WA,C=US
            this.TestParser(
                "package publisher=\"CN=Microsoft,C=US\"",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "package")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "publisher")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.PropertyValue, "CN=Microsoft,C=US")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    )
                );
        }

        [Fact]
        public void BareComplexPropertyValue()
        {
            this.TestParser(
                "fileType extension=.tax",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "fileType")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "extension")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, ".tax")
                    )
                );
        }
        [Fact]
        public void SimpleComment()
        {
            this.TestParser(
                "# comment",
                Statement(
                    StatementType.Ignorable,
                    Token(ParserTokenType.Comment, "# comment")
                    )
                );
        }

        [Fact]
        public void QuotedHexPropertyValue()
        {
            this.TestParser(
                "color white=\"#FFFFFF\"",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "color")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "white")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    .Token(ParserTokenType.PropertyValue, "#FFFFFF")
                    .Token(ParserTokenType.DoubleQuote, "\"")
                    )
                );
        }

        [Fact]
        public void SimpleDefaultPropertyValue()
        {
            this.TestParser(
                "package something",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "package")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyValue, "something")
                    )
                );
        }

        [Fact]
        public void ObjectWithNamespace()
        {
            this.TestParser(
                "appx.capability",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.NamespacePrefix, "appx")
                    .Token(ParserTokenType.Period, ".")
                    .Token(ParserTokenType.Object, "capability")
                    )
                );
        }

        [Fact]
        public void AttachedProperty()
        {
            this.TestParser(
                "file condition.install=true",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "file")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.AttachedPropertyObject, "condition")
                    .Token(ParserTokenType.Period, ".")
                    .Token(ParserTokenType.PropertyName, "install")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "true")
                    )
                );
        }

        [Fact]
        public void AttachedPropertyWithNamespace()
        {
            this.TestParser(
                "file msi.condition.install=true",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "file")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.NamespacePrefix, "msi")
                    .Token(ParserTokenType.Period, ".")
                    .Token(ParserTokenType.AttachedPropertyObject, "condition")
                    .Token(ParserTokenType.Period, ".")
                    .Token(ParserTokenType.PropertyName, "install")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "true")
                    )
                );
        }


        [Fact]
        public void TwoTrivialObjects()
        {
            this.TestParser(
                "one\ntwo",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "one")
                    .Token(ParserTokenType.Whitespace, "\n")
                    )
                .Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "two")
                    )
                );
        }

        [Fact]
        public void TwoObjects()
        {
            this.TestParser(
                "one value\ntwo name=value",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "one")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyValue, "value")
                    .Token(ParserTokenType.Whitespace, "\n")
                    )
                .Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "two")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "value")
                    )
                );
        }

        [Fact]
        public void TwoObjectsWithIndent()
        {
            this.TestParser(
                "one value\n\ttwo name=value",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "one")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyValue, "value")
                    .Token(ParserTokenType.Whitespace, "\n")
                    )
                .Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Whitespace, "\t")
                    .Token(ParserTokenType.Object, "two")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "value")
                    )
                );
        }

        [Fact]
        public void Continuation()
        {
            this.TestParser(
                "folder\n\tname=value",
                Statement(
                    StatementType.Object,
                    Token(ParserTokenType.Object, "folder")
                    .Token(ParserTokenType.Whitespace, "\n")
                    .Token(ParserTokenType.Whitespace, "\t")
                    .Token(ParserTokenType.PropertyName, "name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "value")
                ));
        }

        [Fact]
        public void UseStatement()
        {
            this.TestParser(
                "use appx",
                Statement(
                    StatementType.Use,
                    Token(ParserTokenType.UseKeyword, "use")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.NamespacePrefixDeclaration, "appx")
                    )
                );
        }

        [Fact]
        public void WixlibFileSearchTest()
        {
            this.TestParser(
                @"use msi

msi.fileSearch id=fsC component=c00d5a59-1bd1-4f6b-9b2b-27e0ae64020b

msi.fileSearch id=fsRK registry=HKLM\Search\Registry\Key\",
                Statement(
                    StatementType.Use,
                    Token(ParserTokenType.UseKeyword, "use")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.NamespacePrefixDeclaration, "msi")
                    .Token(ParserTokenType.Whitespace, "\r\n")
                    )
                .Statement(
                    StatementType.Ignorable,
                    Token(ParserTokenType.Whitespace, "\r\n")
                    )
                .Statement(
                    StatementType.Object,
                    Token(ParserTokenType.NamespacePrefix, "msi")
                    .Token(ParserTokenType.Period, ".")
                    .Token(ParserTokenType.Object, "fileSearch")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "id")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "fsC")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "component")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "c00d5a59-1bd1-4f6b-9b2b-27e0ae64020b")
                    .Token(ParserTokenType.Whitespace, "\r\n")
                    )
                .Statement(
                    StatementType.Ignorable,
                    Token(ParserTokenType.Whitespace, "\r\n")
                    )
                .Statement(
                    StatementType.Object,
                    Token(ParserTokenType.NamespacePrefix, "msi")
                    .Token(ParserTokenType.Period, ".")
                    .Token(ParserTokenType.Object, "fileSearch")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "id")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "fsRK")
                    .Token(ParserTokenType.Whitespace, " ")
                    .Token(ParserTokenType.PropertyName, "registry")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "HKLM\\Search\\Registry\\Key\\")
                    )
                );
        }


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
            var actual = RtypeStatementParser.ParseStatements(new Position(0, 0, 0), textProvider);

            Assert.True(expected.SequenceEqual(actual, this.statementComparer));
        }
    }

    class StringTextProvider : ITextProvider
    {
        string value;

        public StringTextProvider(string text)
        {
            this.value = text;
        }

        public bool TryGetText(Position pos, out string text, out Range range)
        {
            text = null;
            range = null;

            if (pos.Offset != 0)
            {
                return false;
            }

            range = new Range(pos, this.value.Length);
            text = this.value;

            return true;
        }
    }

    class StatementListBuilder<StatementT, TokenT> : IEnumerable<Statement<StatementT, TokenT>>
        where StatementT : struct   // the closest we can get to "T : enum"
        where TokenT : struct       // the closest we can get to "T : enum"
    {
        List<Statement<StatementT, TokenT>> statements = new List<Statement<StatementT, TokenT>>();
        Position currentPosition = new Position(0, 0, 0);

        public StatementListBuilder<StatementT, TokenT> Statement(StatementT type, TokenListBuilder<TokenT> tokens)
        {
            Statement<StatementT, TokenT> statement = new Statement<StatementT, TokenT>(type, tokens, tokens);
            this.statements.Add(statement);
            return this;
        }

        public IEnumerator<Statement<StatementT, TokenT>> GetEnumerator()
        {
            return this.statements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    class StatementComparer<StatementT, TokenT> : IEqualityComparer<Statement<StatementT, TokenT>>
        where StatementT : struct   // the closest we can get to "T : enum"
        where TokenT : struct       // the closest we can get to "T : enum"
    {
        TokenComparer<TokenT> tokenComparer;

        public StatementComparer(TokenComparer<TokenT> tokenComparer)
        {
            this.tokenComparer = tokenComparer;
        }

        public bool Equals(Statement<StatementT, TokenT> x, Statement<StatementT, TokenT> y)
        {
            Assert.Equal(x.StatementType, y.StatementType);

            Assert.Equal(x.AllTokens.Count, y.AllTokens.Count);
            bool sequence = x.AllTokens.SequenceEqual(y.AllTokens, this.tokenComparer);
            Assert.True(sequence);

            return object.Equals(x.StatementType, y.StatementType) && sequence;
        }

        public int GetHashCode(Statement<StatementT, TokenT> statement)
        {
            return statement.AllTokens.Count << 8 /*| (int)(statement.StatementType)*/;
        }
    }

}
