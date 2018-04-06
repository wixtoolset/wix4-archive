// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WixToolset.Simplified.Test.Utility
{
    internal static class WixLibUtility
    {
        private static XNamespace WixLibNamespace = "http://wixtoolset.org/schmeas/wixlib";
        private static XNamespace WixObjectNamespace = "http://wixtoolset.org/schemas/v4/wixobj";

        public static IEnumerable<WixDiffResult> Diff(string expectedPath, string actualPath)
        {
            IDictionary<string, Section> expectedSections = LoadSections(expectedPath);
            IDictionary<string, Section> actualSections = LoadSections(actualPath);

            return Diff(expectedSections, actualSections);
        }

        private static IDictionary<string, Section> LoadSections(string path)
        {
            Dictionary<string, Section> sections = new Dictionary<string, Section>();

            XDocument xDoc = XDocument.Load(path);
            foreach (XElement xSection in xDoc.Document.Root.Descendants(WixObjectNamespace + "section"))
            {
                Section section = new Section() { Id = xSection.Attribute("id").Value, Type = xSection.Attribute("type").Value };
                sections.Add(section.Key, section);

                foreach (XElement xTable in xSection.Descendants(WixObjectNamespace + "table"))
                {
                    Table table = new Table() { Name = xTable.Attribute("name").Value };
                    section.Tables.Add(table.Key, table);

                    foreach (XElement xRow in xTable.Descendants(WixObjectNamespace + "row"))
                    {
                        IEnumerable<string> fields = from f in xRow.Elements(WixObjectNamespace + "field") select f.Value;
                        Row row = new Row() { TableName = table.Name, Fields = String.Join("/", fields.ToArray()), SourceLineNumber = xRow.Attribute("sourceLineNumber").Value };
                        table.Rows.Add(row);
                    }
                }
            }

            return sections;
        }

        private static IEnumerable<WixDiffResult> Diff(IDictionary<string, Section> expected, IDictionary<string, Section> actual)
        {
            List<WixDiffResult> results = new List<WixDiffResult>();

            SortedSet<string> sectionKeys = new SortedSet<string>(actual.Keys);
            SortedSet<Section> missingSections = new SortedSet<Section>(expected.Values);
            SortedSet<Section> addedSections = new SortedSet<Section>(actual.Values);

            sectionKeys.IntersectWith(expected.Keys);
            missingSections.ExceptWith(actual.Values);
            addedSections.ExceptWith(expected.Values);

            results.AddRange(from m in missingSections select new WixDiffResult() { Operation = WixDiffOperation.Remove, Type = m.Type, Value = m.Id });
            results.AddRange(from a in addedSections select new WixDiffResult() { Operation = WixDiffOperation.Add, Type = a.Type, Value = a.Id });

            foreach (string matchingKey in sectionKeys)
            {
                Section expectedSection = expected[matchingKey];
                Section actualSection = actual[matchingKey];

                SortedSet<Table> matchingTables = new SortedSet<Table>(expectedSection.Tables.Values);
                SortedSet<Table> missingTables = new SortedSet<Table>(expectedSection.Tables.Values);
                SortedSet<Table> addedTables = new SortedSet<Table>(actualSection.Tables.Values);

                matchingTables.IntersectWith(actualSection.Tables.Values);
                missingTables.ExceptWith(actualSection.Tables.Values);
                addedTables.ExceptWith(expectedSection.Tables.Values);

                results.AddRange(from m in missingTables select new WixDiffResult() { Operation = WixDiffOperation.Remove, Type = "table", Value = m.Key });
                results.AddRange(from a in addedTables select new WixDiffResult() { Operation = WixDiffOperation.Add, Type = "table", Value = a.Key });

                foreach (Table table in from t in matchingTables select t)
                {
                    IEnumerable<Row> expectedRows = expectedSection.Tables[table.Name].Rows;

                    SortedSet<Row> missingRows = new SortedSet<Row>(expectedRows);
                    SortedSet<Row> addedRows = new SortedSet<Row>(table.Rows);

                    missingRows.ExceptWith(table.Rows);
                    addedRows.ExceptWith(expectedRows);

                    results.AddRange(from m in missingRows select new WixDiffResult() { Operation = WixDiffOperation.Remove, SourceLineNumber = m.SourceLineNumber, Type = String.Format("row in {0}", table.Name), Value = m.Fields });
                    results.AddRange(from a in addedRows select new WixDiffResult() { Operation = WixDiffOperation.Add, SourceLineNumber = a.SourceLineNumber, Type = String.Format("row in {0}", table.Name), Value = a.Fields });
                }
            }

            return results;
        }

        private class Section : IComparable<Section>
        {
            public Section()
            {
                this.Tables = new Dictionary<string, Table>();
            }

            public string Key { get { return String.Concat(this.Type, ":", this.Id); } }

            public string Id { get; set; }

            public string Type { get; set; }

            public Dictionary<string, Table> Tables { get; private set; }

            public int CompareTo(Section other)
            {
                int result = this.Type.CompareTo(other.Type);
                if (result == 0)
                {
                    result = this.Id.CompareTo(other.Id);
                }

                return result;
            }
        }

        private class Table : IComparable<Table>
        {
            public Table()
            {
                this.Rows = new List<Row>();
            }

            public string Key { get { return this.Name; } }

            public string Name { get; set; }

            public List<Row> Rows { get; private set; }

            public int CompareTo(Table other)
            {
                int result = this.Name.CompareTo(other.Name);
                return result;
            }
        }

        private class Row : IComparable<Row>
        {
            public string Key { get { return String.Concat(this.TableName, "/", this.Fields, "@", this.SourceLineNumber); } }

            public string TableName { get; set; }

            public string Fields { get; set; }

            public string SourceLineNumber { get; set; }

            public int CompareTo(Row other)
            {
                int result = this.Fields.CompareTo(other.Fields);
                return result;
            }
        }
    }

    public enum WixDiffOperation
    {
        Add,
        Remove,
    }

    public class WixDiffResult
    {
        public WixDiffOperation Operation { get; set; }

        public string Type { get; set; }

        public string Value { get; set; }

        public string SourceLineNumber { get; set; }
    }
}
