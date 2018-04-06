// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.TypedMessageGenerator.Parser
{
    internal enum LexerTokenType
    {
        None,
        Unknown,
        Whitespace,
        Newline,
        Comment,
        TypeKeyword,
        Identifier,
        Number,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Comma,
        Colon,
        Escape,
        Value,  // catch-all for sequences that aren't part of the above!
    }

    internal interface ITextProvider
    {
        bool TryGetText(Position pos, out string text, out Range range);
    }

    internal class Lexer
    {
        // ValueTokenEnders are characters that cause a Value token ("none of the above") to end,
        // so that some other kind of token can start.
        // TODO: make this a table to also use for token mapping?
        private static readonly char[] ValueTokenEnders = new char[] { ' ', '\t', '\v', '\r', '\n', '#', '{', '}', '\\' };
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

                foreach (Token<LexerTokenType> token in Lexer.LexTokens(text, range.Start))
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

                if (currentChar == '\r' || currentChar == '\n')
                {
                    tokenType = LexerTokenType.Newline;
                    // Take all consecutive newline characters, or just the immediate CRLF? ... just the next one.
                    length = (currentChar == '\r' && text.Skip(currentPos + 1).FirstOrDefault() == '\n') ? 2 : 1;
                    hadNewline = true;
                }
                else if (currentChar == '#')
                {
                    // The remainder of the line is comment!
                    tokenType = LexerTokenType.Comment;
                    length = 1 + followingChars.Count();
                }
                else if (currentChar == '{')
                {
                    tokenType = LexerTokenType.LeftBrace;
                    length = 1;
                }
                else if (currentChar == '}')
                {
                    tokenType = LexerTokenType.RightBrace;
                    length = 1;
                }
                else if (currentChar == '[')
                {
                    tokenType = LexerTokenType.LeftBracket;
                    length = 1;
                }
                else if (currentChar == ']')
                {
                    tokenType = LexerTokenType.RightBracket;
                    length = 1;
                }
                else if (currentChar == ',')
                {
                    tokenType = LexerTokenType.Comma;
                    length = 1;
                }
                else if (currentChar == ':')
                {
                    tokenType = LexerTokenType.Colon;
                    length = 1;
                }
                else if (currentChar == '\\')
                {
                    // Character after the backslash (if any!) is the raw...
                    tokenType = LexerTokenType.Escape;
                    length = 1 + followingChars.Take(1).Count();
                    // TODO: Flag as error if it's not known?
                }
                else if (char.IsWhiteSpace(currentChar))
                {
                    // return all of the consecutive whitespace...
                    tokenType = LexerTokenType.Whitespace;
                    length = 1 + followingChars.TakeWhile(c => char.IsWhiteSpace(c)).Count();
                }
                else if (currentChar == 't' &&
                    currentPos + 3 < text.Length &&
                    text[currentPos + 1] == 'y' &&
                    text[currentPos + 2] == 'p' &&
                    text[currentPos + 3] == 'e' &&
                    (currentPos + 4 >= text.Length || char.IsWhiteSpace(text[currentPos + 4])))
                {
                    tokenType = LexerTokenType.TypeKeyword;
                    length = 4;
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
                    length = 1 + followingChars.TakeWhile(c => !ValueTokenEnders.Contains(c)).Count();
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
