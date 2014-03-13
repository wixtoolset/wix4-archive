//-------------------------------------------------------------------------------------------------
// <copyright file="ConsoleMessageHandler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Message handler for console.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace WixToolset
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// Message handler for console applications.
    /// </summary>
    public class ConsoleMessageHandler : IMessageHandler
    {
        private const int SuccessErrorNumber = 0;

        private string shortAppName;
        private string longAppName;

        /// <summary>
        /// Create a new console message handler.
        /// </summary>
        /// <param name="shortAppName">Short application name; usually 4 uppercase characters.</param>
        /// <param name="longAppName">Long application name; usually the executable name.</param>
        public ConsoleMessageHandler(string shortAppName, string longAppName)
        {
            this.shortAppName = shortAppName;
            this.longAppName = longAppName;
        }

        /// <summary>
        /// Gets the last error code encountered by the message handler.
        /// </summary>
        /// <value>The exit code for the process.</value>
        public int LastErrorNumber
        {
            get { return Messaging.Instance.LastErrorNumber; }
        }

        /// <summary>
        /// Display a message to the console.
        /// </summary>
        /// <param name="sender">Sender of the message.</param>
        /// <param name="mea">Arguments for the message event.</param>
        public virtual void Display(object sender, MessageEventArgs mea)
        {
            string message = mea.GenerateMessageString(this.shortAppName, this.longAppName);
            if (null != message)
            {
#if DEBUG
                Debugger.Log((int)mea.Level, this.shortAppName, string.Concat(message, "\n"));
#endif
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Implements IMessageHandler to display error messages.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            this.Display(this, mea);
        }
    }
}
