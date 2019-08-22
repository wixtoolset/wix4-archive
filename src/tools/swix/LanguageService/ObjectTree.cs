// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WixToolset.Simplified.CompilerFrontend.Parser;
using WixToolset.Simplified.Lexicon;
using WixToolset.Simplified.ParserCore;
using Microsoft.VisualStudio.Text;

namespace WixToolset.Simplified.LanguageService
{
    internal class ObjectTree
    {
        private ITextSnapshot snapshot;

        private ParserLanguage language;
        private StatementNode statementNodeRoot;
        private Dictionary<string, string> namespacePrefixes = new Dictionary<string, string>(); // namespace URI to prefix
        private Dictionary<string, string> prefixNamespaces = new Dictionary<string, string>(); // prefix to namespace URI

        public ObjectTree(ParserLanguage language, ITextSnapshot snapshot)
        {
            this.language = language;
            this.snapshot = snapshot;

            this.BuildObjectTree();
        }

        public Dictionary<string, string> NamespacePrefixes { get { return this.namespacePrefixes; } }
        public Dictionary<string, string> PrefixNamespaces { get { return this.prefixNamespaces; } }

        // Suppress the CA2202 message, because I'm pretty sure we can't double-Dispose the snapshotReader.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private void BuildObjectTree()
        {
            using (TextSnapshotToTextReader snapshotReader = new TextSnapshotToTextReader(this.snapshot))
            {
                switch (this.language)
                {
                    case ParserLanguage.Rtype:
                        var rtype = new RtypeParser(null);
                        this.statementNodeRoot = rtype.Parse(snapshotReader);
                        break;
                    case ParserLanguage.Xml:
                        var xml = new XmlParser(null);
                        this.statementNodeRoot = xml.Parse(snapshotReader);
                        break;
                    default:
                        break;
                }
            }
        }

        internal bool TryFindContextNodes(Position position, out StatementNode currentNode, out StatementNode parentNode)
        {
            currentNode = null;
            parentNode = null;

            if (this.statementNodeRoot == null)
            {
                return false;
            }

            currentNode = this.FindInnermostNode(this.statementNodeRoot, position);

            if (currentNode.Parent == null)
            {
                parentNode = currentNode;
                currentNode = null;
            }
            else
            {
                parentNode = currentNode.Parent;

                // For Rtype, determining the parent depends on indentation...
                if (this.language == ParserLanguage.Rtype)
                {
                    // We think we've found the parent node, but we may have to climb
                    // back up the tree if we're less indented than the current statement.
                    while (parentNode != null && position.Column <= parentNode.Indent)
                    {
                        parentNode = parentNode.Parent;
                    }

                    // We think we've found the current node, but if we're at the same
                    // or less indent, it *can't* be a continuation of the current
                    // statement.
                    if (position.Column <= currentNode.Indent)
                    {
                        currentNode = null;
                    }
                }
                else if (this.language == ParserLanguage.Xml)
                {
                    // For XML, we have to see if we're *after* the end of
                    // each statement... we might just be in the parent's content!
                    if (currentNode != null)
                    {
                        if (currentNode.Statement.AllTokens.Last().Range.End <= position)
                        {
                            currentNode = null;
                        }
                    }
                }
            }

            return true;
        }

        // The easiest way to implement the context finder is recursively...
        private StatementNode FindInnermostNode(StatementNode nodeParent, Position position)
        {
            if (nodeParent.Statement.Range.End >= position)
            {
                return nodeParent;
            }

            // Recurse into the last child (if any!) whose start is before the position.
            var child = nodeParent.Children.LastOrDefault(n => n.Range.Start <= position);

            if (child != null)
            {
                return this.FindInnermostNode(child, position);
            }

            // If there are no children that contain the position, then it is the parent...
            return nodeParent;
        }
    }


    internal static class VsInteropExtensions
    {
        public static Position ToSwixPosition(this SnapshotPoint point)
        {
            var line = point.GetContainingLine();
            Position pos = new Position(point.Position, line.LineNumber, point - line.Start);
            return pos;
        }
    }

    internal static class TypeExtensions
    {
        internal static bool IsOrDerivesFrom(this Type type, params Type[] baseTypes)
        {
            return baseTypes.Any(b => type.IsOrDerivesFrom(b));
        }

        internal static bool IsOrDerivesFrom(this Type type, Type baseType)
        {
            // Also look for interface implementation... XAML doesn't seem to include this...
            if (baseType.IsInterface)
            {
                // IsAssignableFrom() seems the easiest way to see if a type implements a particular interface.
                return baseType.IsAssignableFrom(type);
            }

            var testType = type;
            while (testType != null)
            {
                if (testType == baseType)
                {
                    return true;
                }

                testType = testType.BaseType;
            }

            return false;
        }

        internal static string ToRtypeName(this string name)
        {
            // TODO: is there a XAML way to find the expected serialization for this?
            System.Diagnostics.Debug.Assert(name.Length >= 2);
            return string.Concat(name[0].ToString().ToLowerInvariant(), name.Substring(1));
        }
    }
}
