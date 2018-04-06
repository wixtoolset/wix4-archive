// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixToolset.Simplified.ParserCore
{
    public interface IRangeProvider
    {
        Range Range { get; }
    }

    // We need a bridge between line/column  (common for error messages and other command-line
    // diagnostic use), and stream- or buffer-position (common for interactive, VS use).  To do this
    // without relying on VS's classes, we define our own "Position" class, which includes both sets of
    // data.  Note that the line/column values are derivative, not normative, and that the buffer
    // position value is the normative one.
    [System.Diagnostics.DebuggerDisplay("@{Offset} ({Line},{Column})")]
    public class Position : IRangeProvider
    {
        public Position(int offset, int line, int column)
        {
            this.Offset = offset;
            this.Line = line;
            this.Column = column;
        }

        public int Offset { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public static Position operator +(Position pos, int delta)
        {
            return new Position(pos.Offset + delta, pos.Line, pos.Column + delta);
        }

        public static int operator -(Position pos, Position other)
        {
            return pos.Offset - other.Offset;
        }

        public static bool operator >(Position pos, Position other)
        {
            return pos.Offset > other.Offset;
        }

        public static bool operator <(Position pos, Position other)
        {
            return pos.Offset < other.Offset;
        }

        public static bool operator >=(Position pos, Position other)
        {
            return pos.Offset >= other.Offset;
        }

        public static bool operator <=(Position pos, Position other)
        {
            return pos.Offset <= other.Offset;
        }

        public override string ToString()
        {
            return string.Concat("@", this.Offset.ToString());
        }

        Range IRangeProvider.Range
        {
            get { return new Range(this, 0); }
        }
    }

    [System.Diagnostics.DebuggerDisplay("[{Start.Offset}..{End.Offset})")]
    public class Range : IRangeProvider
    {
        public Range(Position start, Position end)
        {
            this.Start = start;
            this.End = end;
        }

        public Range(Position start, int length)
        {
            this.Start = start;
            this.End = start + length;
        }

        public Position Start { get; private set; }
        public Position End { get; private set; }

        public int Length { get { return this.End.Offset - this.Start.Offset; } }

        public override string ToString()
        {
            return string.Concat("[", this.Start.Offset.ToString(), "..", this.End.Offset.ToString(), ")");
        }

        Range IRangeProvider.Range
        {
            get { return this; }
        }
    }
}
