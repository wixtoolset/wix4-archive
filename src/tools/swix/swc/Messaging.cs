// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified
{
    using System;
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Internal messaging system to appropriately route messages to command-line or MSBuild logger.
    /// </summary>
    internal class Messaging
    {
        private TaskLoggingHelper logger;

        /// <summary>
        /// Creates a command-line based messaging object.
        /// </summary>
        public Messaging()
        {
            this.MessageDelegate = this.OnMessageToCommandline;
        }

        /// <summary>
        /// Creates an MSBuild base messaging object.
        /// </summary>
        /// <param name="task"></param>
        public Messaging(Task task)
        {
            this.logger = task.Log;
            this.MessageDelegate = this.OnMessageToMSBuild;
        }

        /// <summary>
        /// Flag specifying whether an error message was encountered.
        /// </summary>
        public bool Errored { get; private set; }

        /// <summary>
        /// Flag specifying whether to display verbose messages.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Delegate handling messages.
        /// </summary>
        public EventHandler<CompilerMessageEventArgs> MessageDelegate { get; private set; }

        public void OnError(object sender, string format, params object[] details)
        {
            if (this.MessageDelegate != null)
            {
                CompilerMessageEventArgs e = new CompilerMessageEventArgs(CompilerMessage.InternalError(String.Format(CultureInfo.InvariantCulture, format, details)), null, 0, 0);
                this.MessageDelegate(sender, e);
            }
        }

        // NOTE: 'id' is ignored!
        public void OnError(object sender, int id, string fileName, string format, params object[] details)
        {
            if (this.MessageDelegate != null)
            {
                CompilerMessageEventArgs e = new CompilerMessageEventArgs(CompilerMessage.InternalError(String.Format(CultureInfo.InvariantCulture, format, details)), fileName, 0, 0);
                this.MessageDelegate(sender, e);
            }
        }

        private void OnMessageToCommandline(object sender, CompilerMessageEventArgs e)
        {
            if (!this.Verbose && e.Message.Type == CompilerMessage.CompilerMessageType.Verbose)
            {
                return;
            }

            string fileName = e.FileName;
            if (e.LineNumber > 0)
            {
                if (e.LinePosition > 0)
                {
                    fileName = String.Format(CultureInfo.InvariantCulture, "{0}({1},{2})", e.FileName, e.LineNumber, e.LinePosition);
                }
                else
                {
                    fileName = String.Format(CultureInfo.InvariantCulture, "{0}({1})", e.FileName, e.LineNumber);
                }
            }

            string messageType = e.Message.Type.ToString().ToLowerInvariant();
            System.IO.TextWriter stream = null;
            switch (e.Message.Type)
            {
            case CompilerMessage.CompilerMessageType.Error:
            case CompilerMessage.CompilerMessageType.Warning:
                stream = Console.Error;
                break;
            default:
                stream = Console.Out;
                break;
            }

            stream.WriteLine(String.IsNullOrEmpty(fileName) ?
                             "{1} {2}{3:0000}: {4}" :
                             "{0} : {1} {2}{3:0000}: {4}",
                             fileName, messageType, "SWIX", e.Message.Id, e.Message.Message);

            if (e.Message.Type == CompilerMessage.CompilerMessageType.Error)
            {
                this.Errored = true;
            }
        }

        private void OnMessageToMSBuild(object sender, CompilerMessageEventArgs e)
        {
            switch (e.Message.Type)
            {
                case CompilerMessage.CompilerMessageType.Error:
                    this.logger.LogError(e.Message.Message);
                    //this.logger.LogError("SWIX", e.Id, null, e.FileName, e.LineNumber, e.LinePosition, 0, 0, e.Details);
                    this.Errored = true;
                    break;

                case CompilerMessage.CompilerMessageType.Warning:
                    this.logger.LogWarning("SWIX", e.Message.Id, null, e.FileName, e.LineNumber, e.LinePosition, 0, e.LinePositionEnd, e.Message.Message);
                    break;

                case CompilerMessage.CompilerMessageType.Information:
                    this.logger.LogMessage(MessageImportance.Normal, e.Message.Message);
                    break;

                case CompilerMessage.CompilerMessageType.Verbose:
                    this.logger.LogMessage(MessageImportance.Low, e.Message.Message);
                    break;
            }
        }
    }
}
