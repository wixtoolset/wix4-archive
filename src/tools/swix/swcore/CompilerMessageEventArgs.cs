// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified
{
    using System;
    using WixToolset.Simplified.ParserCore;
    using WixToolset.Simplified.Lexicon;

    /// <summary>
    /// Delegate for sending compiler messages.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    internal delegate void CompilerMessageDelegate(CompilerMessageEventArgs e);

    /// <summary>
    /// Compiler message event arguments.
    /// </summary>
    public class CompilerMessageEventArgs : EventArgs
    {
        public CompilerMessageEventArgs(CompilerMessage message, PackageItem item)
            : this(message, item.LineNumber)
        {
        }

        internal CompilerMessageEventArgs(CompilerMessage message, string sourceFile, Range range)
            : this(
                message,
                sourceFile == null ? null : sourceFile,
                range == null ? 0 : range.Start.Line,
                range == null ? 0 : range.Start.Column,
                range == null ? 0 : range.End.Column)
        {
        }

        internal CompilerMessageEventArgs(CompilerMessage message, FileLineNumber fileLineNumber)
            : this(
                message,
                fileLineNumber == null ? null : fileLineNumber.SourceFile,
                fileLineNumber == null ? 0 : fileLineNumber.LineNumber,
                fileLineNumber == null ? 0 : fileLineNumber.LinePosition)
        {
        }

        public CompilerMessageEventArgs(CompilerMessage message, string fileName, int lineNumber, int linePosition, int? linePositionEnd = null)
        {
            this.Message = message;
            this.FileName = fileName;
            this.LineNumber = lineNumber;
            this.LinePosition = linePosition;
            this.LinePositionEnd = linePositionEnd;
        }

        public CompilerMessage Message { get; private set; }
        public string FileName { get; private set; }
        public int LineNumber { get; private set; }
        public int LinePosition { get; private set; }
        public int? LinePositionEnd { get; private set; }
    }
}
