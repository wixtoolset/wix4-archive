// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified
{
    using System;

    internal class FileLineNumber
    {
        public FileLineNumber(string sourceFile, int lineNumber, int linePosition)
        {
            this.SourceFile = sourceFile;
            this.LineNumber = lineNumber;
            this.LinePosition = linePosition;
        }

        public int LineNumber { get; set; }

        public int LinePosition { get; set; }

        public string SourceFile { get; set; }
    }
}
