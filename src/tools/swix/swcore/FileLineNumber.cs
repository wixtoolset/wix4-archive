//-------------------------------------------------------------------------------------------------
// <copyright file="FileLineNumber.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
