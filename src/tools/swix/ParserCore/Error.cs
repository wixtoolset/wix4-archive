// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixToolset.Simplified.ParserCore
{
    public class Error
    {
        public Error(string filename, IRangeProvider rangeProvider, string message)
        {
            System.Diagnostics.Debug.Assert(rangeProvider != null);
            this.Filename = filename;
            this.Range = rangeProvider.Range;
            this.Message = message;
        }

        public Error(IRangeProvider rangeProvider, string message)
            : this(null, rangeProvider, message)
        {
        }

        public string Filename { get; private set; }
        public Range Range { get; private set; }
        public string Message { get; private set; }
    }
}
