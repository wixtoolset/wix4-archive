//-------------------------------------------------------------------------------------------------
// <copyright file="HarvesterExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The base harvester extension.  Any of these methods can be overridden to change
// the behavior of the harvester.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The base harvester extension.  Any of these methods can be overridden to change
    /// the behavior of the harvester.
    /// </summary>
    public abstract class HarvesterExtension
    {
        /// <summary>
        /// Gets or sets the harvester core for the extension.
        /// </summary>
        /// <value>The harvester core for the extension.</value>
        public IHarvesterCore Core { get; set; }

        /// <summary>
        /// Harvest a WiX document.
        /// </summary>
        /// <param name="argument">The argument for harvesting.</param>
        /// <returns>The harvested Fragments.</returns>
        public abstract Wix.Fragment[] Harvest(string argument);
    }
}
