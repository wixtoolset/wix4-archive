// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using WixToolset.Simplified.Lexicon.Appx;

namespace WixToolset.Simplified.CompilerBackend.Appx
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Simplified.Lexicon;
    using AppxLexicon = WixToolset.Simplified.Lexicon.Appx;

    internal class ManifestItem : IComparable
    {
        public ManifestItem(string key, PackageItem item, XElement xml)
        {
            this.Key = key;
            this.Item = item;
            this.Xml = xml;
        }

        public string Key { get; set; }

        public PackageItem Item { get; set; }

        public Application ParentApplication { get; set; }

        public XElement Xml { get; set; }

        public int CompareTo(object obj)
        {
            Type thisType = this.GetType();
            Type objType = obj.GetType();

            // Equivalent types get sorted by their key.
            if (thisType == objType)
            {
                ManifestItem objItem = (ManifestItem)obj;
                if (this.Item is AppxLexicon.ContentUri && objItem.Item is AppxLexicon.ContentUri)
                {
                    AppxLexicon.ContentUri thisContentUri = (AppxLexicon.ContentUri)this.Item;
                    AppxLexicon.ContentUri objContentUri = (AppxLexicon.ContentUri)objItem.Item;
                    if (thisContentUri.Rule != objContentUri.Rule)
                    {
                        return (thisContentUri.Rule == ContentUriRule.include) ? -1 : 1;
                    }
                }

                return this.Key.CompareTo((objItem).Key);
            }
            else if (thisType == typeof(Package))
            {
                return -1;
            }
            else if (objType == typeof(Package))
            {
                return 1;
            }
            else if (thisType == typeof(Application))
            {
                return -1;
            }
            else if (objType == typeof(Application))
            {
                return 1;
            }
            else // not a special type so sort by type name.
            {
                return thisType.FullName.CompareTo(objType.FullName);
            }
        }

        public override bool Equals(object obj)
        {
            return this.Key.Equals(((ManifestItem)obj).Key, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return this.Key.GetHashCode();
        }
    }
}
