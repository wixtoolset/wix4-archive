// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerFrontend.Parser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixToolset.Simplified;
    using WixToolset.Simplified.ParserCore;

    /// <summary>
    /// Xml-type lexer.
    /// </summary>
    public static class XmlLexer
    {
        private static char[] Newlines = new char[] { '\n', '\r' };

        public static IEnumerable<Token<LexerTokenType>> LexTokens(Position start, ITextProvider textProvider)
        {
            System.Diagnostics.Debug.Assert(textProvider != null);

            Position pos = start;
            string text;
            Range range;

            // So that the caller's implementation of 'tryGetMoreText' doesn't have to buffer and be
            // smart about token boundaries, we detect when we're about to yield the last token
            // in the current buffer, and instead try to get more text and re-lex.
            Token<LexerTokenType> lastToken = null;
            while (textProvider.TryGetText(pos, out text, out range))
            {
                if (lastToken != null)
                {
                    text = string.Concat(lastToken.Value, text);
                    range = new Range(lastToken.Range.Start, range.End);
                }

                foreach (Token<LexerTokenType> token in XmlLexer.LexTokens(text, range.Start))
                {
                    if (token.Range.End.Offset != range.End.Offset)
                    {
                        lastToken = null;
                        yield return token;
                    }
                    else
                    {
                        lastToken = token;
                    }
                }

                pos = range.End;
            }

            // If we fell off the end with a token, make sure the caller gets it!
            if (lastToken != null)
            {
                yield return lastToken;
            }
        }

        public static IEnumerable<Token<LexerTokenType>> LexTokens(string text, Position startPosition)
        {
            Position tokenStart = startPosition;
            bool hadNewline = false;

            int currentPos = 0;
            while (currentPos < text.Length)
            {
                char currentChar = text[currentPos];
                var followingChars = text.Skip(currentPos + 1).TakeWhile(c => !Newlines.Contains(c));
                LexerTokenType tokenType = LexerTokenType.None;
                int length = 0;

                hadNewline = false;

                // XML doesn't care about newlines, but we need them for line-counting.
                if (currentChar == '\r' || currentChar == '\n')
                {
                    tokenType = LexerTokenType.Newline;
                    // Take all consecutive newline characters, or just the immediate CRLF? ... just the next one.
                    length = (currentChar == '\r' && text.Skip(currentPos + 1).FirstOrDefault() == '\n') ? 2 : 1;
                    hadNewline = true;
                }
                else if (char.IsWhiteSpace(currentChar))
                {
                    // return all of the consecutive whitespace...
                    tokenType = LexerTokenType.Whitespace;
                    length = 1 + followingChars.TakeWhile(c => char.IsWhiteSpace(c)).Count();
                }
                else if (currentChar == '<')
                {
                    tokenType = LexerTokenType.LeftAngle;
                    length = 1;
                }
                else if (currentChar == '>')
                {
                    tokenType = LexerTokenType.RightAngle;
                    length = 1;
                }
                else if (currentChar == '?')
                {
                    tokenType = LexerTokenType.Question;
                    length = 1;
                }
                else if (currentChar == '/')
                {
                    tokenType = LexerTokenType.Slash;
                    length = 1;
                }
                else if (currentChar == '!')
                {
                    tokenType = LexerTokenType.Exclamation;
                    length = 1;
                }
                else if (currentChar == '-' &&
                    currentPos + 1 < text.Length &&
                    text[currentPos + 1] == '-')
                {
                    tokenType = LexerTokenType.DoubleDash;
                    length = 2;
                }
                else if (currentChar == ':')
                {
                    tokenType = LexerTokenType.Colon;
                    length = 1;
                }
                else if (currentChar == '.')
                {
                    tokenType = LexerTokenType.Period;
                    length = 1;
                }
                else if (currentChar == '=')
                {
                    tokenType = LexerTokenType.Equals;
                    length = 1;
                }
                else if (currentChar == '"')
                {
                    tokenType = LexerTokenType.DoubleQuote;
                    length = 1;
                }
                else if (currentChar == '\'')
                {
                    tokenType = LexerTokenType.SingleQuote;
                    length = 1;
                }
                else if (char.IsLetter(currentChar))
                {
                    tokenType = LexerTokenType.Identifier;
                    length = 1 + followingChars.TakeWhile(c => char.IsLetterOrDigit(c) || c == '_').Count();
                }
                else if (char.IsDigit(currentChar))
                {
                    tokenType = LexerTokenType.Number;
                    length = 1 + followingChars.TakeWhile(c => char.IsDigit(c)).Count();
                }
                else
                {
                    tokenType = LexerTokenType.Value;
                    //length = 1 + followingChars.TakeWhile(c => !ValueTokenEnders.Contains(c)).Count();
                    length = 1;
                }

                if (length <= 0)
                {
                    throw new Exception("didn't eat any characters!");
                }

                Range tokenRange = new Range(tokenStart, length);

                yield return new Token<LexerTokenType>(
                    tokenType,
                    text.Substring(currentPos, length),
                    tokenRange);

                currentPos += length;
                tokenStart = tokenRange.End;

                // After newlines, keep the offset, but bump the line and reset the column
                if (hadNewline)
                {
                    tokenStart = new Position(tokenStart.Offset, tokenStart.Line + 1, 0);
                }
            }
        }
    }
}
