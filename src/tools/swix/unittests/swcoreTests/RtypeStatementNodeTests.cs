// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using WixToolset.Simplified.CompilerFrontend.Parser;
using WixToolset.Simplified.ParserCore;
using Xunit;

namespace WixToolset.Simplified.UnitTest.Swcore
{
    public class RtypeStatementNodeTests
    {
        StatementNodeComparer statementNodeComparer;

        public RtypeStatementNodeTests()
        {
            var tokenComparer = new TokenComparer<ParserTokenType>(compareOffsets: false);
            var statementComparer = new StatementComparer<StatementType, ParserTokenType>(tokenComparer);

            this.statementNodeComparer = new StatementNodeComparer(statementComparer);
        }

        [Fact]
        public void SingleNode()
        {
            this.TestParser(
                "file",
                StatementNode(
                    0,
                    StatementType.Object,
                    Token(ParserTokenType.Object, "file")
                ));
        }

        [Fact]
        public void SimpleTree()
        {
            this.TestParser(
                "folder\n\tfile",
                StatementNode(
                    0,
                    StatementType.Object,
                    Token(ParserTokenType.Object, "folder")
                    .Token(ParserTokenType.Whitespace, "\n"),
                    StatementNode(
                        1,
                        StatementType.Object,
                        Token(ParserTokenType.Whitespace, "\t")
                        .Token(ParserTokenType.Object, "file")
                    )
                ));
        }

        [Fact]
        public void Continuation()
        {
            this.TestParser(
                "folder\n\tname=value",
                StatementNode(
                    0,
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
        public void ContinuationWithSimpleTree()
        {
            this.TestParser(
                "folder\n\tname=value\n\tfile",
                StatementNode(
                    0,
                    StatementType.Object,
                    Token(ParserTokenType.Object, "folder")
                    .Token(ParserTokenType.Whitespace, "\n")
                    .Token(ParserTokenType.Whitespace, "\t")
                    .Token(ParserTokenType.PropertyName, "name")
                    .Token(ParserTokenType.Equals, "=")
                    .Token(ParserTokenType.PropertyValue, "value")
                    .Token(ParserTokenType.Whitespace, "\n"),
                    StatementNode(
                        1,
                        StatementType.Object,
                        Token(ParserTokenType.Whitespace, "\t")
                        .Token(ParserTokenType.Object, "file")
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
            var swixNode = new StatementNodeBuilder();
            swixNode.StatementNode(-1, StatementType.Object, Token(ParserTokenType.Object, "swix"), expected);

            RtypeParser parser = new RtypeParser("test");
            var actual = parser.Parse(source);

            Assert.Equal(swixNode.First(), actual, this.statementNodeComparer);
        }
    }

    class StatementNodeBuilder : IEnumerable<StatementNode>
    {
        List<StatementNode> statementNodes = new List<StatementNode>();

        public StatementNodeBuilder StatementNode(int indent, StatementType type, TokenListBuilder<ParserTokenType> tokens, StatementNodeBuilder children = null)
        {
            var statement = new Statement<StatementType, ParserTokenType>(type, tokens, tokens);
            var statementNode = new StatementNode(indent, null, statement);

            this.statementNodes.Add(statementNode);

            if (children != null)
            {
                foreach (var child in children)
                {
                    statementNode.Add(child);
                }
            }

            return this;
        }

        public IEnumerator<StatementNode> GetEnumerator()
        {
            return this.statementNodes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }


    class StatementNodeComparer : IEqualityComparer<StatementNode>
    {
        StatementComparer<StatementType, ParserTokenType> statementComparer;

        public StatementNodeComparer(StatementComparer<StatementType, ParserTokenType> statementComparer)
        {
            this.statementComparer = statementComparer;
        }

        public bool Equals(StatementNode x, StatementNode y)
        {
            Assert.Equal(x.Indent, y.Indent);
            Assert.Equal(x.Statement, y.Statement, this.statementComparer);

            Assert.Equal(x.Children.Count, y.Children.Count);
            bool sequence = x.Children.SequenceEqual(y.Children, this);
            Assert.True(sequence);

            return x.Indent == y.Indent &&
                this.statementComparer.Equals(x.Statement, y.Statement) &&
                sequence;
        }

        public int GetHashCode(StatementNode statementNode)
        {
            return statementNode.Indent << 16 | (int)(statementNode.Statement.StatementType);
        }
    }


}
