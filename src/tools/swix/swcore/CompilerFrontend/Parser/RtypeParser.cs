// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.CompilerFrontend.Parser
{
    public enum ParserTokenType
    {
        Unknown,
        Whitespace,
        Comment,
        Object,
        PropertyName,
        Equals,
        PropertyValue,
        DoubleQuote,
        SingleQuote,
        Period,
        NamespacePrefix,
        NamespacePrefixDeclaration,
        NamespaceDeclaration,
        AttachedPropertyObject,
        UseKeyword,         // rtype only 
        LeftAngle,          // xml only
        RightAngle,         // xml only
        Colon,              // xml only
        Slash,              // xml only
    }

    public class RTypeTokenInfo : ITokenTypeInfo<ParserTokenType, LexerTokenType>
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
                    return true;
            }

            return false;
        }

        public bool StatementBuilderCanConsumeOnFailedExpect(LexerTokenType lexerTokenType)
        {
            switch (lexerTokenType)
            {
                case LexerTokenType.Newline:
                ////case LexerTokenType.Hash:
                    return false;
            }

            return true;
        }
    }

    public class StatementNode : IRangeProvider
    {
        public StatementNode(int indent, StatementNode parent, Statement<StatementType, ParserTokenType> statement)
        {
            this.Indent = indent;
            this.Parent = parent;
            this.Statement = statement;
            this.Children = new List<StatementNode>();
        }

        public int Indent { get; private set; }
        public StatementNode Parent { get; set; }
        public Statement<StatementType, ParserTokenType> Statement { get; private set; }
        public List<StatementNode> Children { get; private set; }

        public void Add(StatementNode childStatementNode)
        {
            this.Children.Add(childStatementNode);
        }

        public Range Range
        {
            get
            {
                Range range = this.Statement.Range;

                if (this.Children.Any())
                {
                    range = new Range(this.Statement.Range.Start, this.Children.Last().Range.End);
                }

                return range;
            }
        }
    }

    public class ReaderTextProvider : ITextProvider, IDisposable
    {
        private TextReader reader;

        public ReaderTextProvider(TextReader reader)
        {
            this.reader = reader;
        }

        public bool TryGetText(Position pos, out string text, out Range range)
        {
            // For now, we ignore the position, and assume it always increases from the last one...
            char[] buffer = new char[1024];
            int read = this.reader.Read(buffer, 0, buffer.Length);

            if (read > 0)
            {
                text = new string(buffer, 0, read);
                range = new Range(pos, text.Length);
                return true;
            }

            text = null;
            range = new Range(pos, 0);
            return false;
        }

        private bool disposed;
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.reader.Dispose();
                this.disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }

    public class RtypeParser
    {
        public RtypeParser(string fileName)
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
            var swixTokens = new Token<ParserTokenType>[] { new Token<ParserTokenType>(ParserTokenType.Object, "swix", new Range(new Position(-1, -1, -1), 0)) };
            this.RootStatementNode = new StatementNode(-1, null, new Statement<StatementType,ParserTokenType>(StatementType.Object, swixTokens, swixTokens));
            this.Errors = new List<Error>();

            using (ReaderTextProvider textProvider = new ReaderTextProvider(reader))
            {
                var statements = RtypeStatementParser.ParseStatements(new Position(0, 0, 0), textProvider);

                Stack<StatementNode> parentStack = new Stack<StatementNode>();
                StatementNode lastStatementNode = null;
                char requiredLeadingWhitespaceStyle = '\0';

                parentStack.Push(this.RootStatementNode);

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

                    if (statement.StatementType == StatementType.Ignorable)
                    {
                        // TODO: Should we continue to pass along the ignorable statements?
                        continue;
                    }

                    // Look at the leading whitespace (if any!) and decide if we're a child, a sibling, or
                    // if we need to pop the stack some...
                    int indent = 0;
                    Token<ParserTokenType> leadingWhitespace = statement.AllTokens
                        .Take(1)
                        .Where(t => t.TokenType == ParserTokenType.Whitespace)
                        .FirstOrDefault();

                    if (leadingWhitespace != null)
                    {
                        string whitespace = leadingWhitespace.Value;

                        // If we haven't decided whether we require leading tabs or spaces, just use the
                        // first character in this leading whitespace!

                        if (requiredLeadingWhitespaceStyle == '\0')
                        {
                            requiredLeadingWhitespaceStyle = whitespace[0];
                        }

                        if (!whitespace.All(c => c == requiredLeadingWhitespaceStyle))
                        {
                            this.AddError(leadingWhitespace, "Leading whitespace must be either all tabs or all spaces.");
                        }

                        indent = leadingWhitespace.Value.Length;
                    }

                    // Look at the indent of this statement, and decide where it goes...
                    if (lastStatementNode != null && indent > lastStatementNode.Indent)
                    {
                        parentStack.Push(lastStatementNode);
                    }
                    else if (lastStatementNode != null && indent < lastStatementNode.Indent)
                    {
                        // Pop the parent stack until we have a parent with less indentation...
                        while (parentStack.Peek().Indent >= indent)
                        {
                            parentStack.Pop();
                        }
                    }

                    // Now we've fixed up the parent stack, so we can add this statement to the
                    // proper parent...
                    lastStatementNode = new StatementNode(indent, parentStack.Peek(), statement);
                    parentStack.Peek().Add(lastStatementNode);
                }
            }

            return this.RootStatementNode;
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
