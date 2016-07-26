// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.UX
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    public class WindowProperties : DependencyObject
    {
        public static readonly DependencyProperty IsLightBackgroundProperty = DependencyProperty.Register(
            "IsLightBackground", typeof(bool), typeof(WindowProperties), new PropertyMetadata( false ));

        private static WindowProperties _instance;

        public static WindowProperties Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WindowProperties();
                }
                return _instance;
            }
        }


        public Boolean IsLightBackground
        {
            get { return (bool)GetValue(IsLightBackgroundProperty); }
            private set { SetValue(IsLightBackgroundProperty, value); }
        }

        public void CheckBackgroundBrightness()
        {
            SolidColorBrush windowbrush = System.Windows.SystemColors.WindowBrush;
            System.Drawing.Color dcolor = System.Drawing.Color.FromArgb(windowbrush.Color.A, windowbrush.Color.R, windowbrush.Color.G, windowbrush.Color.B);

            var brightness = dcolor.GetBrightness();
            // Test for 'Lightness' at an arbitray point, approaching 1.0 (White).           
            if (0.7 < brightness)
            {
                WindowProperties.Instance.IsLightBackground = true;
            }
            else
            {
                WindowProperties.Instance.IsLightBackground = false;
            }
        }
    }
}
