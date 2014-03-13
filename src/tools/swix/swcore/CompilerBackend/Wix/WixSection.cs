//-------------------------------------------------------------------------------------------------
// <copyright file="WixSection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
