// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixToolset.Simplified.ParserCore
{
    // TODO: ?  extend to allow "push back" of any number of tokens?
    internal class TokenBuffer<T>
        where T : struct   // as close as we can get to "T : enum"
    {
        private IEnumerator<Token<T>> enumerator;
        private List<Token<T>> lookAheadBuffer = new List<Token<T>>();

        public TokenBuffer(IEnumerable<Token<T>> tokens)
        {
            this.CurrentTokenType = default(T);
            this.CurrentValue = string.Empty;
            this.CurrentRange = new Range(new Position(0, 0, 0), 0);

            this.enumerator = new EnumeratorWrapper<Token<T>>(tokens.GetEnumerator());
            this.GetNextToken();
        }

        public Token<T> CurrentToken { get; private set; }
        public T CurrentTokenType { get; private set; }
        public string CurrentValue { get; private set; }
        public Range CurrentRange { get; private set; }

        private void GetNextToken()
        {
            Token<T> nextToken = null;

            // If we have a lookahead buffer, item zero is the token we're moving past...
            if (this.lookAheadBuffer.Any())
            {
                this.lookAheadBuffer.RemoveAt(0);

                // We either have another look-ahead to consume, or we're finally
                // up to the real enumerator.
                if (this.lookAheadBuffer.Any())
                {
                    nextToken = this.lookAheadBuffer[0];
                }
                else
                {
                    nextToken = this.enumerator.Current;
                }
            }
            else if (this.enumerator.MoveNext())
            {
                nextToken = this.enumerator.Current;
            }

            this.SetCurrentToken(nextToken);
        }

        private void SetCurrentToken(Token<T> nextToken)
        {
            if (nextToken != null)
            {
                this.CurrentToken = nextToken;
                this.CurrentTokenType = this.CurrentToken.TokenType;
                this.CurrentValue = this.CurrentToken.Value;
                this.CurrentRange = this.CurrentToken.Range;
            }
            else if (this.CurrentToken != null)
            {
                this.CurrentToken = null;
                this.CurrentTokenType = default(T);
                this.CurrentValue = string.Empty;
                this.CurrentRange = new Range(this.CurrentRange.End, 0);
            }
        }

        // distance zero is the current token...
        public T LookAhead(int distance, T typeIfNull)
        {
            var token = this.LookAhead(distance);
            return token != null ? token.TokenType : typeIfNull;
        }

        // distance zero is the current token...
        public Token<T> LookAhead(int distance)
        {
            if (distance < 0)
            {
                throw new ArgumentOutOfRangeException("distance", "cannot be negative!");
            }

            if (this.lookAheadBuffer.Count > distance)
            {
                return this.lookAheadBuffer[distance];
            }

            // We will pull all pending tokens *except* for the one we're looking for to the look-ahead buffer.
            // So if we've already buffered one, and are looking ahead one, we don't need to buffer any
            // more... the one we're looking for is the enumerator's current token.
            int tokensToBuffer = distance - this.lookAheadBuffer.Count;

            if (tokensToBuffer > 0)
            {
                for (int i = 0; i < tokensToBuffer; ++i)
                {
                    // Buffer the current enumerator token, and then advance.
                    this.lookAheadBuffer.Add(this.enumerator.Current);

                    // We blindly move ahead, even if that means pushing
                    // null tokens on our look-ahead buffer.
                    this.enumerator.MoveNext();
                }
            }

            return this.enumerator.Current;
        }

        // Allows client to "un-consume" a token, which becomes the current token.
        public void PushBack(Token<T> token)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            // It doesn't matter whether the existing current token is from the
            // enumerator or the look-ahead buffer... we can just push onto the
            // beginning of the look-ahead buffer and update the current values.
            this.lookAheadBuffer.Insert(0, token);
            this.SetCurrentToken(this.lookAheadBuffer[0]);
        }

        // Allows client to "un-consume" a set of tokens, and update the current token
        public void PushBack(IEnumerable<Token<T>> tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException("tokens");
            }

            // The token order is maintained, such that the first token in the argument
            // becomes the current token, the second token is next, and so on.
            if (tokens.Any())
            {
                this.lookAheadBuffer.InsertRange(0, tokens);
                this.SetCurrentToken(this.lookAheadBuffer[0]);
            }
        }

        public bool Is(T tokenType)
        {
            if (this.CurrentToken != null)
            {
                return this.CurrentTokenType.Equals(tokenType);
            }

            return false;
        }

        public bool AcceptAny(out Token<T> token)
        {
            token = null;

            if (this.CurrentToken != null)
            {
                token = this.CurrentToken;

                this.GetNextToken();
                return true;
            }

            return false;
        }

        // Returns 'true' if the current token type matches the provided one.  The token is set even
        // if Accept returns 'false' so that an error message can make use of the actual token found.
        public bool Accept(T tokenType, out Token<T> token, bool returnTokenEvenWhenNotMatched = false)
        {
            token = null;

            if (this.CurrentToken != null)
            {
                if (this.CurrentTokenType.Equals(tokenType))
                {
                    token = this.CurrentToken;
                    this.GetNextToken();
                    return true;
                }

                if (returnTokenEvenWhenNotMatched)
                {
                    token = this.CurrentToken;
                }
            }

            return false;
        }
    }

    // Ensures that Current return null/default at end of enumeration.
    internal class EnumeratorWrapper<T> : IEnumerator<T>
    {
        private IEnumerator<T> inner;
        private bool atEnd = false;

        public EnumeratorWrapper(IEnumerator<T> inner)
        {
            this.inner = inner;
        }

        public T Current
        {
            get
            {
                if (this.atEnd)
                {
                    return default(T);
                }

                return this.inner.Current;
            }
        }

        public void Dispose()
        {
            this.inner.Dispose();
            this.inner = null;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return this.Current; }
        }

        public bool MoveNext()
        {
            this.atEnd = !this.inner.MoveNext();
            return !this.atEnd;
        }

        public void Reset()
        {
            this.inner.Reset();
            this.atEnd = false;
        }
    }
}
