//-------------------------------------------------------------------------------------------------
// <copyright file="InspectorCore.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Core facilities for inspector extensions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// Core facilities for inspector extensions.
    /// </summary>
    internal sealed class InspectorCore : IInspectorCore
    {
        /// <summary>
        /// Gets whether an error occured.
        /// </summary>
        /// <value>Whether an error occured.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
        }

        /// <summary>
        /// Logs a message to the log handler.
        /// </summary>
        /// <param name="e">The <see cref="MessageEventArgs"/> that contains information to log.</param>
        public void OnMessage(MessageEventArgs e)
        {
            Messaging.Instance.OnMessage(e);
        }
    }
}
