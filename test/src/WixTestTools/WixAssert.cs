//-----------------------------------------------------------------------
// <copyright file="WixAssert.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using Xunit;

    public class WixAssert : Assert
    {
        public static void Succeeded(int hr, string format, params object[] formatArgs)
        {
            if (0 > hr)
            {
                throw new SucceededException(hr, String.Format(format, formatArgs));
            }
        }
    }
}
