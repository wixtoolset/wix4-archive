//-------------------------------------------------------------------------------------------------
// <copyright file="WixExtensionReferenceNodeProperties.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixExtensionReferenceNodeProperties class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.VisualStudio
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Package;

    /// <summary>
    /// Represents Wixlib reference node properties.
    /// </summary>
    /// <remarks>This class must be public and marked as ComVisible in order for the DispatchWrapper to work correctly.</remarks>
    [CLSCompliant(false)]
    [ComVisible(true)]
    [Guid("F6396F1D-2E76-428b-92DA-F089C2A78370")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1409:ComVisibleTypesShouldBeCreatable")]
    public class WixExtensionReferenceNodeProperties : WixReferenceNodeProperties
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixExtensionReferenceNodeProperties"/> class.
        /// </summary>
        /// <param name="node">The node that contains the properties to expose via the Property Browser.</param>
        public WixExtensionReferenceNodeProperties(WixExtensionReferenceNode node)
            : base(node)
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the version of the WiX extension file.
        /// </summary>
        /// <value>The version of the WiX extension file.</value>
        [Browsable(true)]
        [WixLocalizedDescription("WixExtensionReferenceVersionDescription")]
        [WixLocalizedDisplayName("WixExtensionReferenceVersionDisplayName")]
        public Version Version
        {
            get
            {
                Debug.Assert(this.Node != null, "The associated hierarchy node has not been initialized");

                return ((WixExtensionReferenceNode)this.Node).Version;
            }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Returns the name that is displayed in the right hand side of the Properties window drop-down combo box.
        /// </summary>
        /// <returns>The class name of the object, or null if the class does not have a name.</returns>
        public override string GetClassName()
        {
            return WixStrings.WixExtensionReferenceProperties;
        }
    }
}
