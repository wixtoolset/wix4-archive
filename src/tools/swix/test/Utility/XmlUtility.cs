// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Xml;

namespace WixToolset.Simplified.Test.Utility
{
    internal static class XmlUtility
    {
        private static readonly char[] SplitSemicolons = new char[] { ';' };

        public static string NormalizeDocument(string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNode node = NormalizeXml(doc, doc);

            return node.OuterXml;
        }

        public static XmlNode NormalizeXml(XmlDocument doc, XmlNode node)
        {
            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    NormalizeXml(doc, child);
                }
            }

            // Sort the attributes since their order is irrelevant in XML and we don't want
            // to deal with false positives.
            if (node.Attributes != null)
            {
                string[] attributes = new string[(node.Attributes.Count)];
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    attributes[i] = string.Join(";",
                                                new string[]
                                                    {
                                                        node.Attributes[i].Prefix,
                                                        node.Attributes[i].LocalName,
                                                        node.Attributes[i].NamespaceURI,
                                                        node.Attributes[i].Value
                                                    });
                }

                Array.Sort(attributes);
                node.Attributes.RemoveAll();

                foreach (var attribute in attributes)
                {
                    string[] attrib = attribute.Split(SplitSemicolons, 4, StringSplitOptions.None);
                    XmlAttribute a = doc.CreateAttribute(attrib[0], attrib[1], attrib[2]);
                    a.Value = attrib[3];
                    node.Attributes.Append(a);
                }
            }

            return node;
        }
    }
}
