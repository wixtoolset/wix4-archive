// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.UX
{
    using System.Windows;
    using System.Windows.Controls;

    class BrowserProperties
    {
        public static readonly DependencyProperty HtmlDocProperty =
            DependencyProperty.RegisterAttached("HtmlDoc", typeof(string), typeof(BrowserProperties), new PropertyMetadata(OnHtmlDocChanged));

        public static string GetHtmlDoc(DependencyObject dependencyObject)
        {
            return (string)dependencyObject.GetValue(HtmlDocProperty);
        }

        public static void SetHtmlDoc(DependencyObject dependencyObject, string htmldoc)
        {
            dependencyObject.SetValue(HtmlDocProperty, htmldoc);
        }

        private static void OnHtmlDocChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var webBrowser = (WebBrowser)d;
            webBrowser.NavigateToString((string)e.NewValue);
        }
    }
}
