// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.
// Some of this code was posted by Samuel Jack (and he credits Todd White) at: http://stackoverflow.com/questions/263551/databind-the-source-property-of-the-webbrows
// Code from here: https://thomasfreudenberg.com/archive/2010/08/01/binding-webbrowser-content-in-wpf/  covered by: CC BY 4.0 at: https://creativecommons.org/licenses/by/4.0/ was modified and integrated.

namespace WixToolset.UX
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    public static class WebBrowserBehaviors
    {
        public static readonly DependencyProperty BindableSourceProperty =
            DependencyProperty.RegisterAttached("BindableSource", typeof(object), typeof(WebBrowserBehaviors), new UIPropertyMetadata(null, BindableSourcePropertyChanged));

        public static readonly DependencyProperty BodyProperty =
            DependencyProperty.RegisterAttached("Body", typeof(string), typeof(WebBrowserBehaviors), new PropertyMetadata(OnBodyChanged));

        public static string GetBody(DependencyObject dependencyObject)
        {
            return (string)dependencyObject.GetValue(BodyProperty);
        }

        public static void SetBody(DependencyObject dependencyObject, string body)
        {
            dependencyObject.SetValue(BodyProperty, body);
        }

        private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var webBrowser = (WebBrowser)d;
            webBrowser.NavigateToString((string)e.NewValue);
        }
  
        public static object GetBindableSource(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableSourceProperty);
        }

        public static void SetBindableSource(DependencyObject obj, object value)
        {
            obj.SetValue(BindableSourceProperty, value);
        }

        public static void BindableSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser browser = o as WebBrowser;
            if (browser == null) return;

            Uri uri = null;

            if (e.NewValue is string)
            {
                var uriString = e.NewValue as string;
                uri = string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString);
            }
            else if (e.NewValue is Uri)
            {
                uri = e.NewValue as Uri;
            }

            browser.Source = uri;
        }
    }
}
