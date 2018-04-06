// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.CompilerFrontend.Parser
{
    public class XmlTokenInfo : ITokenTypeInfo<ParserTokenType, LexerTokenType>
    {
        public bool IsIgnorable(ParserTokenType parserTokenType)
        {
            switch (parserTokenType)
            {
                case ParserTokenType.Whitespace:
                case ParserTokenType.Comment:
                ////case ParserTokenType.Equals:        // We *could* ignore the parser token, because all we really need to know is PropertyName/PropertyValue...
                case ParserTokenType.DoubleQuote:
                case ParserTokenType.SingleQuote:
                case ParserTokenType.Period:
                case ParserTokenType.Colon:
                case ParserTokenType.LeftAngle:
                case ParserTokenType.RightAngle:
                case ParserTokenType.Slash:
                    return true;
            }

            return false;
        }

        public bool StatementBuilderCanConsumeOnFailedExpect(LexerTokenType lexerTokenType)
        {
            ////switch (lexerTokenType)
            ////{
            ////    case LexerTokenType.Newline:
            ////        ////case LexerTokenType.Hash:
            ////        return false;
            ////}

            return true;
        }
    }

    public class XmlParser
    {
        public XmlParser(string fileName)
        {
            this.FileName = fileName;
        }

        public string FileName { get; private set; }

        public StatementNode RootStatementNode { get; private set; }
        public List<Error> Errors { get; private set; }

        public StatementNode Parse(string input)
        {
            using (StringReader reader = new StringReader(input))
            {
                return this.Parse(reader);
            }
        }

        public StatementNode Parse(TextReader reader)
        {
            ////var swixTokens = new Token<ParserTokenType>[] { new Token<ParserTokenType>(ParserTokenType.Object, "swix", new Range(new Position(-1, -1, -1), 0)) };
            ////this.RootStatementNode = new StatementNode(-1, null, new Statement<StatementType, ParserTokenType>(StatementType.Object, swixTokens, swixTokens));
            this.Errors = new List<Error>();

            using (ReaderTextProvider textProvider = new ReaderTextProvider(reader))
            {
                var statements = XmlStatementParser.ParseStatements(new Position(0, 0, 0), textProvider);

                Stack<StatementNode> parentStack = new Stack<StatementNode>();

                ////parentStack.Push(this.RootStatementNode);

                foreach (var statement in statements)
                {
                    if (statement.HasError)
                    {
                        // Propagate the error...
                        foreach (var token in statement.Tokens)
                        {
                            if (token.Errors != null)
                            {
                                foreach (var error in token.Errors)
                                {
                                    this.AddError(error.Range, error.Message);
                                }
                            }
                        }
                    }

                    if (statement.StatementType == StatementType.Ignorable ||
                        statement.StatementType == StatementType.Comment)
                    {
                        // TODO: Should we continue to pass along the ignorable statements?
                        continue;
                    }

                    if (statement.StatementType == StatementType.Object ||
                        statement.StatementType == StatementType.ObjectStart)
                    {
                        StatementNode parent = null;
                        int indent = 0;

                        if (parentStack.Count > 0)
                        {
                            parent = parentStack.Peek();
                            indent = parent.Indent + 1;
                        }

                        var node = new StatementNode(indent, parent, statement);

                        if (this.RootStatementNode == null)
                        {
                            this.RootStatementNode = node;
                        }

                        if (parent != null)
                        {
                            parent.Add(node);
                        }

                        if (statement.StatementType == StatementType.ObjectStart)
                        {
                            parentStack.Push(node);
                        }
                    }
                    else if (statement.StatementType == StatementType.ObjectEnd)
                    {
                        // Should we push the close statement to the end of
                        // the children?  I think so!
                        var parent = parentStack.Pop();
                        while (parent != null && !ObjectEndMatches(parent.Statement, statement))
                        {
                            parent = parentStack.Pop();
                        }

                        if (parent != null)
                        {
                            // push the close statement...
                            var node = new StatementNode(parent.Indent + 1, parent, statement);
                            parent.Add(node);
                        }

                        // Flag an error?
                        ////// If we over-pop, push the root node back on...
                        ////if (parentStack.Count == 0)
                        ////{
                        ////    parentStack.Push(this.RootStatementNode);
                        ////}
                    }
                }
            }

            return this.RootStatementNode;
        }

        private static bool ObjectEndMatches(
            Statement<StatementType, ParserTokenType> openStatement,
            Statement<StatementType, ParserTokenType> closeStatement)
        {
            // check optional namespace and object name
            var openTokens = openStatement.Tokens.Take(2).ToList();
            var closeTokens = openStatement.Tokens.Take(2).ToList();

            if (openTokens.Count >= 1 && closeTokens.Count >= 1)
            {
                if (openTokens[0].TokenType == closeTokens[0].TokenType &&
                    string.Equals(openTokens[0].Value, closeTokens[0].Value))
                {
                    if (openTokens[0].TokenType == ParserTokenType.Object)
                    {
                        return true;
                    }

                    if (openTokens[0].TokenType == ParserTokenType.NamespacePrefix &&
                        (openTokens.Count >= 2 && closeTokens.Count >= 2) &&
                        openTokens[1].TokenType == ParserTokenType.Object &&
                        openTokens[1].TokenType == closeTokens[1].TokenType &&
                        string.Equals(openTokens[1].Value, closeTokens[1].Value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void AddError(IRangeProvider rangeProvider, string messageFormat, params object[] args)
        {
            this.AddError(rangeProvider, string.Format(messageFormat, args));
        }

        private void AddError(IRangeProvider rangeProvider, string message)
        {
            System.Diagnostics.Debug.Assert(rangeProvider != null);
            this.Errors.Add(new Error(this.FileName, rangeProvider.Range, message));
        }

        private void AddErrorAfter(IRangeProvider rangeProvider, string messageFormat, params object[] args)
        {
            this.AddErrorAfter(rangeProvider, string.Format(messageFormat, args));
        }

        private void AddErrorAfter(IRangeProvider rangeProvider, string message)
        {
            System.Diagnostics.Debug.Assert(rangeProvider != null);
            this.AddError(new Range(rangeProvider.Range.End, 0), message);
        }
    }
}
