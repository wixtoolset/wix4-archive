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

            viewModel.Dispatcher = this.Dispatcher;
            viewModel.ViewWindowHandle = new WindowInteropHelper(this).EnsureHandle();
            WindowProperties.Instance.CheckBackgroundBrightness();
        }

        /// <summary>
        /// Event is fired when the window is closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RootViewModel rvm = this.DataContext as RootViewModel;
            if ((null != rvm) && (InstallationState.Applying == rvm.InstallState))
            {
                rvm.Canceled = true;
                // defer closing until the engine has canceled processing, then user wwill get a Close button.
                e.Cancel = true;
            }
        }
    }
}
