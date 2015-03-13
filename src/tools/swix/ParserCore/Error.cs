//-------------------------------------------------------------------------------------------------
// <copyright file="Error.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
