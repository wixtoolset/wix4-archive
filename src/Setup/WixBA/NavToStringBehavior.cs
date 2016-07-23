using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WixToolset.UX
{
    class NavToStringBehavior
    {
        public static readonly DependencyProperty HtmlDocProperty =
            DependencyProperty.RegisterAttached("HtmlDoc", typeof(string), typeof(NavToStringBehavior), new PropertyMetadata(OnHtmlDocChanged));

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
