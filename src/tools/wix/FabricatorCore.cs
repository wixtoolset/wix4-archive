//-------------------------------------------------------------------------------------------------
// <copyright file="FabricatorCore.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;
    using System.Reflection;
    using WixToolset.Data;

    /// <summary>
    /// The WiX Toolset Fabricator core.
    /// </summary>
    public class FabricatorCore
    {
        /// <summary>
        /// Gets whether the fabricator core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            Messaging.Instance.OnMessage(mea);
        }
    }
}
