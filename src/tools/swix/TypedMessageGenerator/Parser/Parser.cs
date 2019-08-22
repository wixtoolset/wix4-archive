// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.TypedMessageGenerator.Parser
{
    internal enum ParserTokenType
    {
        Unknown,
        Whitespace,
        Comment,
        TypeKeyword,
        MessageTypeDefinition,
        MessageTypeRange,
        MessageType,
        MessageName,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        Comma,
        Colon,
        ReplacementName,
        ReplacementType,
        ReplacementPosition,
        ReplacementAlignment,
        ReplacementFormat,
        Escape,
        Value,  // catch-all for sequences that aren't part of the above!
    }

    internal class MsgsTokenInfo : ITokenTypeInfo<ParserTokenType, LexerTokenType>
    {
        public bool IsIgnorable(ParserTokenType parserTokenType)
        {
            switch (parserTokenType)
            {
                case ParserTokenType.Whitespace:
                case ParserTokenType.Comment:
                    return true;
            }
            
            return false;
        }

        public bool StatementBuilderCanConsumeOnFailedExpect(LexerTokenType lexerTokenType)
        {
            switch (lexerTokenType)
            {
                case LexerTokenType.Newline:
                case LexerTokenType.Comment:
                    return false;
            }

            return true;
        }
    }


    internal class Parser
    {
        public Parser(string fileName)
        {
            this.FileName = fileName;
        }

        public string FileName { get; private set; }

        public MessageData MessageData { get; private set; }
        public List<Error> Errors { get; private set; }

        public MessageData Parse(string input)
        {
            using (StringReader reader = new StringReader(input))
            {
                return this.Parse(reader);
            }
        }

        private class ReaderTextProvider : ITextProvider, IDisposable
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

        public MessageData Parse(TextReader reader)
        {
            this.MessageData = new MessageData(this.FileName);
            this.Errors = new List<Error>();

            using (ReaderTextProvider textProvider = new ReaderTextProvider(reader))
            {
                var statements = StatementParser.ParseStatements(new Position(0, 0, 0), textProvider);
                Message currentMessage = null;

                foreach (var statement in statements)
                {
                    if (statement.HasError)
                    {
                        // Propagate the error... do we bother with the line otherwise?
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
                        continue;
                    }

                    IList<Token<ParserTokenType>> tokens = statement.Tokens;

                    switch (statement.StatementType)
                    {
                        case StatementType.MessageTypeDefiniton:
                            Token<ParserTokenType> messageTypeName = tokens.ElementAtOrDefault(1);
                            Token<ParserTokenType> rangeStart = tokens.ElementAtOrDefault(2);
                            Token<ParserTokenType> rangeEnd = tokens.ElementAtOrDefault(3);

                            System.Diagnostics.Debug.Assert(messageTypeName.TokenType == ParserTokenType.MessageTypeDefinition);
                            System.Diagnostics.Debug.Assert(rangeStart.TokenType == ParserTokenType.MessageTypeRange);
                            System.Diagnostics.Debug.Assert(rangeEnd.TokenType == ParserTokenType.MessageTypeRange);

                            this.MessageData.Types.Add(new MessageType(messageTypeName.Value, int.Parse(rangeStart.Value), int.Parse(rangeEnd.Value)));
                            break;

                        case StatementType.Message:
                            Token<ParserTokenType> messageTypeToken = tokens.ElementAtOrDefault(0);
                            Token<ParserTokenType> messageName = tokens.ElementAtOrDefault(1);
                            Token<ParserTokenType> messageIdToken = tokens.ElementAtOrDefault(2);

                            System.Diagnostics.Debug.Assert(messageTypeToken.TokenType == ParserTokenType.MessageType);
                            System.Diagnostics.Debug.Assert(messageName.TokenType == ParserTokenType.MessageName);
                            System.Diagnostics.Debug.Assert(messageIdToken == null || messageIdToken.TokenType == ParserTokenType.MessageTypeRange);

                            // Find the message type...
                            MessageType messageType = this.MessageData.Types.FirstOrDefault(t => string.Equals(t.Name, messageTypeToken.Value, StringComparison.Ordinal));
                            if (messageType == null)
                            {
                                this.AddError(
                                    messageTypeToken,
                                    "Unknown message type '{0}'. Message types must be defined before use.",
                                    messageTypeToken.Value);
                            }

                            int messageId = -1;

                            if (messageIdToken != null)
                            {
                                messageId = int.Parse(messageIdToken.Value);
                            }

                            Message existing = this.MessageData.Messages.FirstOrDefault(
                                                m => string.Equals(m.Name, messageName.Value, StringComparison.OrdinalIgnoreCase));

                            if (existing != null)
                            {
                                this.AddError(
                                    messageName,
                                    "There is already a message with name '{0}'. Message names must differ by more than just case.",
                                    existing.Name);
                            }


                            // create a new message...
                            currentMessage = new Message(statement.Range.Start.Line, messageType, messageName.Value, messageId);

                            if (statement.HasError)
                            {
                                string errors = string.Join(", ", statement.AllTokens.SelectMany(t => t.Errors).Select(e => e.Message));
                                currentMessage.Error = errors;
                                // What about earlier this.AddError?
                            }

                            this.MessageData.Messages.Add(currentMessage);
                            break;

                        case StatementType.MessageInstance:
                            if (currentMessage == null)
                            {
                                this.AddError(statement, "You must define a message before any instances.");
                                break;
                            }

                            StringBuilder originalMessage = new StringBuilder();
                            StringBuilder processedMessage = new StringBuilder();
                            List<ParamData> paramList = new List<ParamData>();
                            int tokenIndex = 0;

                            while (tokenIndex < tokens.Count)
                            {
                                Token<ParserTokenType> token = tokens[tokenIndex];
                                switch (token.TokenType)
                                {
                                    case ParserTokenType.Value:
                                        originalMessage.Append(token.Value);
                                        processedMessage.Append(token.Value);
                                        ++tokenIndex;
                                        break;

                                    case ParserTokenType.Escape:
                                        originalMessage.Append(token.Value);
                                        string raw = null;
                                        switch (token.Value[1])
                                        {
                                            case 'n':
                                                raw = "\n";
                                                break;
                                            case 'r':
                                                raw = "\r";
                                                break;
                                            case 't':
                                                raw = "\t";
                                                break;
                                            case 'v':
                                                raw = "\v";
                                                break;
                                            case '\\':
                                                raw = "\\";
                                                break;
                                            case '{':
                                                raw = "{{"; // TODO: if there end up being no replacements, we need to remove the doubled braces!
                                                break;
                                            case '}':
                                                raw = "}}"; // TODO: if there end up being no replacements, we need to remove the doubled braces!
                                                break;
                                            default:
                                                this.AddError(
                                                    token,
                                                    "Unrecognized escape sequence '{0}'.",
                                                    token.Value);
                                                break;
                                        }

                                        if (raw != null)
                                        {
                                            processedMessage.Append(raw);
                                        }

                                        ++tokenIndex;
                                        break;

                                    case ParserTokenType.LeftBrace:
                                        // Collect the replacement group...
                                        ++tokenIndex;
                                        List<Token<ParserTokenType>> replacementTokens = tokens.Skip(tokenIndex).TakeWhile(t => t.TokenType != ParserTokenType.RightBrace).ToList();
                                        tokenIndex += replacementTokens.Count + 1; // skip replacement tokens and the right brace

                                        // Before parsing, we aggregate all of the original values up to the
                                        // close brace.  It's just easier/cleaner that way.
                                        originalMessage.Append("{");
                                        foreach (Token<ParserTokenType> tempToken in replacementTokens)
                                        {
                                            originalMessage.Append(tempToken.Value);
                                        }
                                        originalMessage.Append("}");

                                        // Now get the tokens...
                                        Token<ParserTokenType> replacementType = replacementTokens.FirstOrDefault(t => t.TokenType == ParserTokenType.ReplacementType);
                                        Token<ParserTokenType> replacementPosition = replacementTokens.FirstOrDefault(t => t.TokenType == ParserTokenType.ReplacementPosition);
                                        Token<ParserTokenType> replacementName = replacementTokens.FirstOrDefault(t => t.TokenType == ParserTokenType.ReplacementName);
                                        Token<ParserTokenType> replacementAlignment = replacementTokens.FirstOrDefault(t => t.TokenType == ParserTokenType.ReplacementAlignment);
                                        Token<ParserTokenType> replacementFormat = replacementTokens.FirstOrDefault(t => t.TokenType == ParserTokenType.ReplacementFormat);

                                        if (replacementName != null)
                                        {
                                            // Now add the parsed replacement parameter info and update the strings...
                                            int paramIndex = paramList.FindIndex(p => string.Equals(p.Name, replacementName.Value, StringComparison.Ordinal));

                                            if (paramIndex == -1)
                                            {
                                                Type type;

                                                string paramType = replacementType != null ? replacementType.Value : "string";

                                                // Type.GetType() only finds proper .NET type names, the C# aliases like "int" and "string" aren't
                                                // understood.  We do our best to find what the author is asking for...
                                                switch (paramType)
                                                {
                                                    case "int":
                                                        type = typeof(int);
                                                        break;
                                                    case "uint":
                                                        type = typeof(uint);
                                                        break;
                                                    case "string":
                                                        type = typeof(string);
                                                        break;
                                                    case "object":
                                                        type = typeof(object);
                                                        break;
                                                    default:
                                                        type = Type.GetType(paramType, false, true);
                                                        break;
                                                }

                                                if (type == null)
                                                {
                                                    // Couldn't find it?  Try prefixing "System."!
                                                    type = Type.GetType(string.Concat("System.", paramType), false, true);
                                                }

                                                if (type == null)
                                                {
                                                    this.AddError(
                                                        replacementType,
                                                        "Could not parse parameter type '{0}'.",
                                                        replacementType.Value);
                                                    type = typeof(object);
                                                }

                                                // Get the position (if specified)...
                                                int position = -1;

                                                if (replacementPosition != null)
                                                {
                                                    position = int.Parse(replacementPosition.Value);

                                                    if (paramList.Any(p => p.Position == position))
                                                    {
                                                        this.AddError(
                                                            replacementPosition,
                                                            "Replacement parameter position '{0}' is already in use.",
                                                            replacementPosition.Value);

                                                        position = -1;
                                                    }
                                                }

                                                paramList.Add(new ParamData(replacementName.Value, type, position));
                                                paramIndex = paramList.Count - 1;
                                            }
                                            else
                                            {
                                                // TODO:? Verify type/position match, if specified?
                                            }

                                            // Update the reformatted string (pass 1).  We have to make 2 passes because we
                                            // don't necessarily know the parameter order until they've all been read!
                                            processedMessage.Append("{");
                                            processedMessage.Append(replacementName.Value);
                                            if (replacementAlignment != null)
                                            {
                                                processedMessage.Append(",");
                                                processedMessage.Append(replacementAlignment.Value);
                                            }
                                            if (replacementFormat != null)
                                            {
                                                processedMessage.Append(":");
                                                processedMessage.Append(replacementFormat.Value);
                                            }
                                            processedMessage.Append("}");
                                        }
                                        break;
                                }
                            }

                            // Create the instance...
                            // Determine the final parameter order, now that we have all of the parameters.
                            int paramCount = paramList.Count;
                            for (int position = 0; position < paramCount; ++position)
                            {
                                if (paramList.FirstOrDefault(p => p.Position == position) == null)
                                {
                                    // Update the position of the first auto-numbered parameter.
                                    var param = paramList.FirstOrDefault(p => p.Position == -1);
                                    if (param != null)
                                    {
                                        param.Position = position;
                                    }
                                    else
                                    {
                                        this.AddError(
                                            statement.Range,
                                            "There is no parameter {0}!",
                                            position);
                                    }
                                }
                            }

                            // Replace the parameter names with their final position, longest parameter name first, to ensure
                            // we don't make partial replacements!
                            paramList.OrderByDescending(p => p.Name.Length).ForEach(p =>
                            {
                                processedMessage.Replace(string.Concat("{", p.Name), string.Concat("{", p.Position.ToString()));
                            });

                            // And finally, if there were no replacements, un-double any braces we might have created from
                            // the "\{" and "\}" escapes...
                            if (paramList.Count == 0)
                            {
                                processedMessage.Replace("{{", "{");
                                processedMessage.Replace("}}", "}");
                            }

                            Instance instance = new Instance(
                                statement.Range.Start.Line,
                                originalMessage.ToString(),
                                processedMessage.ToString(),
                                paramList.OrderBy(p => p.Position).Select(p => new Tuple<string, Type>(p.Name, p.Type)).ToList());

                            if (statement.HasError)
                            {
                                string errors = string.Join(", ", statement.AllTokens.SelectMany(t => t.Errors).Select(e => e.Message));
                                instance.Error = errors;
                            }

                            currentMessage.Instances.Add(instance);
                            break;

                        case StatementType.Ignorable:
                            // No-op!
                            break;

                        case StatementType.Unknown:
                        // Error!
                        ////this.AddError(
                        ////    this.currentToken,
                        ////    "Unexpected {0}: '{1}'.",
                        ////    this.currentToken.TokenType,
                        ////    this.currentToken.Value);
                        default:
                            break;
                    }
                }
            }

            // Name all the instances...
            foreach (Message message in this.MessageData.Messages)
            {
                message.NameInstances();
            }

            return this.MessageData;
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

        private class ParamData
        {
            public ParamData(string name, Type type, int position = -1)
            {
                this.Name = name;
                this.Type = type;
                this.Position = position;
            }

            public string Name { get; private set; }
            public Type Type { get; private set; }
            public int Position { get; set; }
        }
    }
}
