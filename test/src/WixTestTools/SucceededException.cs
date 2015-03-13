//-----------------------------------------------------------------------
// <copyright file="SucceededException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using Xunit.Sdk;

    public class SucceededException : AssertException
    {
        public SucceededException(int hr, string userMessage)
            : base(String.Format("WixAssert.Succeeded() Failure\r\n" +
                                 "HRESULT:  0x{0:X8}\r\n" +
                                 "Message:  {1}",
                                 hr, userMessage))
        {
        }
    }
}
