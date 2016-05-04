// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend.Wix
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    internal class WixSection
    {
        private Dictionary<string, XElement> tables;

        /// <summary>
        /// Creates an 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type"></param>
        public WixSection(string id, string type, FileLineNumber typeStartLineNumber)
        {
            this.Id = id;
            this.tables = new Dictionary<string, XElement>();
            this.Xml = new XElement(WixBackendCompilerServices.WixobjNamespace + "section",
                new XAttribute("id", id),
                new XAttribute("type", type));

            XElement row = WixBackendCompilerServices.GenerateRow(this, "WixFragment", typeStartLineNumber,
                id);
        }

        public string Id { get; private set; }

        public XElement Xml { get; private set; }

        public XElement GetTable(string name)
        {
            XElement table;
            if (!this.tables.TryGetValue(name, out table))
            {
                table = new XElement(WixBackendCompilerServices.WixobjNamespace + "table",
                    new XAttribute("name", name));

                this.Xml.Add(table);
                this.tables.Add(name, table);
            }

            return table;
        }
    }
}
