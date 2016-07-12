// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.
// This code was posted by bj0 at: http://stackoverflow.com/questions/2470685/how-do-you-disable-aero-snap-in-an-application
// Only used portions of this sample code needed to address WIXBUG #5262 (The DragMoveBehavior code was throwing an exception).

using System;
using System.Windows;
using System.Windows.Interactivity;

namespace WixToolset.UX
{
    /// <summary>
    /// behavior that makes a window/dialog not resizable while clicked.  this prevents
    /// the window from being snapped to the edge of the screen (AeroSnap).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NoSnapBehavior<T> : Behavior<T> where T : FrameworkElement
    {
        ResizeMode lastMode = ResizeMode.NoResize;
        protected override void OnAttached()
        {
            AssociatedObject.MouseLeftButtonDown += MouseDown;
            AssociatedObject.MouseLeftButtonUp += MouseUp;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= MouseDown;
            AssociatedObject.MouseLeftButtonUp -= MouseUp;
            base.OnDetaching();
        }

        /// <summary>
        /// make it so the window can be moved by dragging
        /// </summary>
        void MouseDown(object sender, EventArgs ea)
        {
            var win = Window.GetWindow(sender as T);
            if (win != null && win.ResizeMode != ResizeMode.NoResize)
            {
                lastMode = win.ResizeMode;
                win.ResizeMode = ResizeMode.NoResize;
                win.UpdateLayout();
            }
        }

        void MouseUp(object sender, EventArgs ea)
        {
            var win = Window.GetWindow(sender as T);
            if (win != null && win.ResizeMode != lastMode)
            {
                win.ResizeMode = lastMode;
                win.UpdateLayout();
            }
        }
    }

    public class WinNoSnapBehavior : NoSnapBehavior<Window> { }
}
