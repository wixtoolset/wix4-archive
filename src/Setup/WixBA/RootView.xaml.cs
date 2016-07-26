// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.UX
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;

    /// <summary>
    /// Interaction logic for View.xaml
    /// </summary>
    public partial class RootView : Window
    {
        /// <summary>
        /// Creates the view populated with it's model.
        /// </summary>
        /// <param name="viewModel">Model for the view.</param>
        public RootView(RootViewModel viewModel)
        {
            this.DataContext = viewModel;

            this.Loaded += (sender, e) => WixBA.Model.Engine.CloseSplashScreen();
            this.Closed += (sender, e) => this.Dispatcher.InvokeShutdown(); // shutdown dispatcher when the window is closed.

            this.InitializeComponent();

            viewModel.ViewWindowHandle = new WindowInteropHelper(this).EnsureHandle();
            WindowProperties.Instance.CheckBackgroundBrightness();
        }

        /// <summary>
        /// Allows the user to drag the window around by grabbing the background rectangle.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Background_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
 
        /// <summary>
        /// If user clicks on a no-input control (i.e. a TextBlock, etc.) route event to here to avoid  InvalidOperationException
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RootView_IgnoreClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

    }
}
