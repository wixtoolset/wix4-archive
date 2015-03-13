﻿//-------------------------------------------------------------------------------------------------
// <copyright file="WixColorUserControl.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixColorUserControl class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.VisualStudio.Controls
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Data;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// A control of which all children are being colored
    /// according to the VS colors servcie
    /// </summary>
    public partial class WixColorUserControl : UserControl
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Constrcutor for the control
        /// </summary>
        public WixColorUserControl()
        {
            this.InitializeComponent();
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Override to return our specialized controls colletion which allows us 
        /// to control the colors of the controls as they're being added.
        /// </summary>
        /// <returns>A collection that will contain all controls on the control</returns>
        protected override ControlCollection CreateControlsInstance()
        {
            return new WixColorUserControlCollection(this);
        }

        /// <summary>
        /// Resets the control colors when the system colors change
        /// </summary>
        /// <param name="e">The object representing the event   </param>
        protected override void OnSystemColorsChanged(EventArgs e)
        {
            base.OnSystemColorsChanged(e);

            // This sets the background control for all this control and all of its children
            this.BackColor = WixHelperMethods.GetVsColor(WixHelperMethods.Vs2010Color.VSCOLOR_BUTTONFACE);

            // The forecolor has to be set explicitly for each control
            WixHelperMethods.SetControlTreeColors(this);
        }

        /// <summary>
        /// A collection of child controls on the WixColorUserControl
        /// which sets the proper colors on which added control
        /// </summary>
        internal class WixColorUserControlCollection : Control.ControlCollection
        {
            // =========================================================================================
            // Constructors
            // =========================================================================================

            /// <summary>
            /// Constructor for the collection of child controls
            /// </summary>
            /// <param name="owner">The parent control of the collection</param>
            internal WixColorUserControlCollection(Control owner)
                : base(owner)
            {
            }

            // =========================================================================================
            // Methods
            // =========================================================================================

            /// <summary>
            /// Adds a control to the collection of child controls
            /// </summary>
            /// <param name="value">Control to be added</param>
            public override void Add(Control value)
            {
                base.Add(value);
                WixHelperMethods.SetControlTreeColors(value);
            }
        }
    }   
}
