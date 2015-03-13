//-------------------------------------------------------------------------------------------------
// <copyright file="CompilerConstants.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;

    /// <summary>
    /// Constants used by compiler.
    /// </summary>
    public class CompilerConstants
    {
        public const int IntegerNotSet = int.MinValue;
        public const int IllegalInteger = int.MinValue + 1;
        public const long LongNotSet = long.MinValue;
        public const long IllegalLong = long.MinValue + 1;
        public const string IllegalGuid = "IllegalGuid";
        public static readonly Version IllegalVersion = new Version(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);
    }
}
