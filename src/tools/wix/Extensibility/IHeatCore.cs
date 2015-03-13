//-------------------------------------------------------------------------------------------------
// <copyright file="IHeatCore.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibilty
{
    using WixToolset.Data;

    /// <summary>
    /// The WiX Toolset Harvester application core.
    /// </summary>
    public interface IHeatCore
    {
        /// <summary>
        /// Gets whether the mutator core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        bool EncounteredError { get; }

        /// <summary>
        /// Gets the harvester.
        /// </summary>
        /// <value>The harvester.</value>
        Harvester Harvester { get; }

        /// <summary>
        /// Gets the mutator.
        /// </summary>
        /// <value>The mutator.</value>
        Mutator Mutator { get; }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        void OnMessage(MessageEventArgs mea);
    }
}
