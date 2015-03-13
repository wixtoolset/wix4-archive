﻿//-------------------------------------------------------------------------------------------------
// <copyright file="LocalizedControl.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    using System;

    public class LocalizedControl
    {
        public LocalizedControl(string dialog, string control, int x, int y, int width, int height, int attribs, string text)
        {
            this.Dialog = dialog;
            this.Control = control;
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.Attributes = attribs;
            this.Text = text;
        }

        public string Dialog { get; set; }

        public string Control { get; set; }

        public int X { get; private set; }

        public int Y { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Attributes { get; private set; }

        public string Text { get; private set; }

        /// <summary>
        /// Get key for a localized control.
        /// </summary>
        /// <returns>The localized control id.</returns>
        public string GetKey()
        {
            return LocalizedControl.GetKey(this.Dialog, this.Control);
        }

        /// <summary>
        /// Get key for a localized control.
        /// </summary>
        /// <param name="dialog">The optional id of the control's dialog.</param>
        /// <param name="control">The id of the control.</param>
        /// <returns>The localized control id.</returns>
        public static string GetKey(string dialog, string control)
        {
            return String.Concat(String.IsNullOrEmpty(dialog) ? String.Empty : dialog, "/", String.IsNullOrEmpty(control) ? String.Empty : control);
        }
    }
}
