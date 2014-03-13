//-------------------------------------------------------------------------------------------------
// <copyright file="CompilerException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

//-------------------------------------------------------------------------------------------------
// <copyright file="CompilerFileManager.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified
{
    using System;

    /// <summary>
    /// All known exceptions in the compiler will throw this.
    /// </summary>
    public class CompilerException : Exception
    {
        public CompilerMessageEventArgs MessageEventArgs { get; private set; }

        public CompilerException(CompilerMessageEventArgs mea) :
            this(mea, null)
        {
        }

        public CompilerException(CompilerMessageEventArgs mea, Exception innerException)
            : base(mea.Message.Message, innerException)
        {
            this.MessageEventArgs = mea;
        }

        /// <summary>
        /// Internal method to throw a consistent exception for internal compiler failures.
        /// </summary>
        /// <param name="format">Message to add to exception.</param>
        /// <param name="details">Additional details to format into message.</param>
        /// <remarks>It's odd to have this on CompilerException when it throws an InvalidOperationException,
        /// but that's because the original implementation was on CompilerMessage, which is now a generated
        /// class.  In any case, it may make more sense to have the method here.</remarks>
        internal static void ThrowInternalError(string format, params object[] details)
        {
            throw new InvalidOperationException(String.Concat("Internal compiler failure: ", String.Format(format, details)));
        }
    }
}
