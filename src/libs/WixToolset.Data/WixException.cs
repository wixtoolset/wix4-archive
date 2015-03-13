//-------------------------------------------------------------------------------------------------
// <copyright file="WixException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    using System;

    /// <summary>
    /// Base class for all WiX exceptions.
    /// </summary>
    [Serializable]
    public class WixException : Exception
    {
        private MessageEventArgs error;

        /// <summary>
        /// Instantiate a new WixException with a given WixError.
        /// </summary>
        /// <param name="error">The localized error information.</param>
        public WixException(MessageEventArgs error)
            : this(error, null)
        {
        }

        /// <summary>
        /// Instantiate a new WixException with a given WixError.
        /// </summary>
        /// <param name="error">The localized error information.</param>
        /// <param name="exception">Original exception.</param>
        public WixException(MessageEventArgs error, Exception exception) :
            base(error.GenerateMessageString(), exception)
        {
            this.error = error;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>The error message.</value>
        public MessageEventArgs Error
        {
            get { return this.error; }
        }
    }
}
