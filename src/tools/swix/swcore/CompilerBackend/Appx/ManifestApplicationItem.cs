// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Simplified.Lexicon;
    using AppxLexicon = WixToolset.Simplified.Lexicon.Appx;

    /// <summary>
    /// Manifest application item.
    /// </summary>
    internal class ManifestApplicationItem : ManifestItem
    {
        private XElement extensions;
        private XElement contentUriRules;
        private XElement initialRotationPreferences;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">Unique identifier for manifest item.</param>
        /// <param name="item">Package item.</param>
        /// <param name="xml">XML that represents the manifest applicaion item.</param>
        public ManifestApplicationItem(string key, PackageItem item, XElement xml) :
            base(key, item, xml)
        {
        }

        /// <summary>
        /// XML to contain content URI rules.
        /// </summary>
        public XElement ContentUriRules
        {
            get
            {
                if (this.contentUriRules == null)
                {
                    this.contentUriRules = new XElement(AppxManifest.AppxNamespace + "ApplicationContentUriRules");
                }

                return this.contentUriRules;
            }
        }

        /// <summary>
        /// XML to contain extensions.
        /// </summary>
        public XElement Extensions
        {
            get
            {
                if (this.extensions == null)
                {
                    this.extensions = new XElement(AppxManifest.AppxNamespace + "Extensions");
                }

                return this.extensions;
            }
        }

        /// <summary>
        /// Gets and sets the manifest item for the application lock screen.
        /// </summary>
        public ManifestItem LockScreen { get; set; }

        /// <summary>
        /// Gets and sets the manifest item for the application orientation restrictions.
        /// </summary>
        public XElement InitialRotationPreferences
        {
            get
            {
                if (this.initialRotationPreferences == null)
                {
                    this.initialRotationPreferences = new XElement(AppxManifest.AppxNamespace + "InitialRotationPreference");
                }

                return this.initialRotationPreferences;
            }
        }

        /// <summary>
        /// Gets and sets the nanifest item for the application splash screen.
        /// </summary>
        public ManifestItem SplashScreen { get; set; }

        /// <summary>
        /// Gets and sets the manifest item for application tile.
        /// </summary>
        public ManifestItem Tile { get; set; }

        /// <summary>
        /// Finishes the manifest applcation XML.
        /// </summary>
        /// <param name="backend">Compiler backend used for sending messages.</param>
        public void Finish(BackendCompiler backend)
        {
            // Visual elements is required.
            XElement xVisualElements = new XElement(AppxManifest.AppxNamespace + "VisualElements");
            this.Xml.Add(xVisualElements);

            if (this.Tile != null)
            {
                Application application = (Application)this.Item;
                AppxLexicon.Tile tile = (AppxLexicon.Tile)this.Tile.Item;

                // Display name is required.
                if (!String.IsNullOrEmpty(application.DisplayName))
                {
                    xVisualElements.Add(new XAttribute("DisplayName", application.DisplayName));
                }
                else
                {
                    backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Application", "DisplayName"), this.Item));
                }

                // Description is required.
                if (!String.IsNullOrEmpty(application.Description))
                {
                    xVisualElements.Add(new XAttribute("Description", application.Description));
                }
                else
                {
                    backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Application", "Description"), this.Item));
                }

                // Image is required.
                string logo = tile.Image == null ? null : tile.Image.NonqualifiedName;
                if (!String.IsNullOrEmpty(logo))
                {
                    xVisualElements.Add(new XAttribute("Logo", logo));
                }
                else
                {
                    backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Tile", "Image"), this.Item));
                }

                // Small image is required.
                string smallLogo = tile.SmallImage == null ? null : tile.SmallImage.NonqualifiedName;
                if (!String.IsNullOrEmpty(smallLogo))
                {
                    xVisualElements.Add(new XAttribute("SmallLogo", smallLogo));
                }
                else
                {
                    backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("Tile", "SmallImage"), this.Item));
                }

                // Foreground is required.
                xVisualElements.Add(new XAttribute("ForegroundText", tile.Foreground));

                // Background is required.
                string color = AppxManifest.GetColor(tile.Background);
                if (!String.IsNullOrEmpty(color))
                {
                    xVisualElements.Add(new XAttribute("BackgroundColor", color));
                }
                else
                {
                    backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.InvalidAttributeValue("Tile", "Background", tile.Background), tile));
                }

                // Toast capable is optional.
                bool? toast = AppxLexicon.Application.GetToastCapable(application);
                if (toast.HasValue && toast.Value)
                {
                    xVisualElements.Add(new XAttribute("ToastCapable", true));
                }

                // Default tile is optional.
                string wideLogo = (tile.WideImage == null) ? null : tile.WideImage.NonqualifiedName;
                if (!String.IsNullOrEmpty(wideLogo) || !String.IsNullOrEmpty(tile.ShortName) || tile.ShowName != AppxLexicon.TileShowName.Invalid)
                {
                    XElement xDefautTile = new XElement(AppxManifest.AppxNamespace + "DefaultTile");

                    if (!String.IsNullOrEmpty(tile.ShortName))
                    {
                        xDefautTile.Add(new XAttribute("ShortName", tile.ShortName));
                    }

                    if (tile.ShowName != AppxLexicon.TileShowName.Invalid)
                    {
                        if (tile.ShowName != AppxLexicon.TileShowName.Invalid)
                        {
                            xDefautTile.Add(new XAttribute("ShowName", tile.ShowName));
                        }
                    }

                    if (!String.IsNullOrEmpty(wideLogo))
                    {
                        xDefautTile.Add(new XAttribute("WideLogo", wideLogo));
                    }

                    xVisualElements.Add(xDefautTile);
                }
            }
            else
            {
                backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredElement("Application", "Tile"), this.Item));
            }

            // Lock screen is optional.
            if (this.LockScreen != null && this.LockScreen.Xml != null && this.LockScreen.Xml.HasAttributes)
            {
                xVisualElements.Add(this.LockScreen.Xml);
            }

            // Splash screen is required.
            if (this.SplashScreen != null && this.SplashScreen.Xml != null && this.SplashScreen.Xml.HasAttributes)
            {
                xVisualElements.Add(this.SplashScreen.Xml);
            }
            else
            {
                backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredElement("Application", "SplashScreen"), this.Item));
            }

            // Orientation preferences are optional.
            if (this.initialRotationPreferences != null && this.initialRotationPreferences.HasElements)
            {
                xVisualElements.Add(initialRotationPreferences);
            }

            // Extensions are optional.
            if (this.extensions != null && this.extensions.HasElements)
            {
                this.Xml.Add(this.extensions);
            }

            // Content URI rules are optional.
            if (this.contentUriRules != null && this.contentUriRules.HasElements)
            {
                this.Xml.Add(this.contentUriRules);
            }
        }

        //private string StripRootFolderReference(string path)
        //{
        //    string[] idPath = path.Split(new char[] { ':' }, 2);
        //    if (!idPath[0].Equals("ApplicationFolder"))
        //    {
        //        // TOOD: send warning that we are ignoring all other roots and we always put files in ApplicationFolder
        //    }

        //    return idPath[1].Substring(1); // skip the backslash.
        //}
    }
}
