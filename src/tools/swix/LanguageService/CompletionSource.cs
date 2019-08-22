// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using WixToolset.Simplified.Lexicon;
using WixToolset.Simplified.ParserCore;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using WixToolset.Simplified.CompilerFrontend.Parser;
using WixToolset.Simplified.CompilerFrontend;
using System.Diagnostics;

namespace WixToolset.Simplified.LanguageService
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(TypeConstants.Content)]
    [Name("Swix Completion")]
    internal class CompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal IGlyphService GlyphService { get; set; }

        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new CompletionSource(this, textBuffer);
        }
    }

    internal class CompletionSource : ICompletionSource
    {
        private CompletionSourceProvider sourceProvider;
        private ITextBuffer textBuffer;
        private ParserLanguage language;
        private ObjectInstanceTypeCache typeCache;
        private string prefixSeparator;

        public CompletionSource(CompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            this.sourceProvider = sourceProvider;
            this.textBuffer = textBuffer;

            // Determine the file language (rtype or xml)...
            this.language = textBuffer.ContentType.GetParserLanguage();
            this.prefixSeparator = this.language == ParserLanguage.Xml ? ":" : ".";

            // TODO: can the type-cache be shared (put into the CompletionSourceProvider, perhaps?)
            this.typeCache = new ObjectInstanceTypeCache();
            this.typeCache.AddAssembly(typeof(ParserTokenType).Assembly);
            this.typeCache.PreloadCache();
        }

        private System.Windows.Media.ImageSource GetGlyph(StandardGlyphGroup group, StandardGlyphItem item)
        {
            return this.sourceProvider.GlyphService.GetGlyph(group, item);
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            ObjectTree objectTree = new ObjectTree(this.language, this.textBuffer.CurrentSnapshot);

            // In the case of virtual spaces, VS sends SnapshotPoint of the last physical
            // location in the line.  Unfortunately, we really need to know the virtual
            // location!  As far as I know, there's no way to actually get this.  Sadly,
            // that means that we might provide the wrong context.
            SnapshotPoint point = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot).Value;
            Position pos = point.ToSwixPosition();

            StatementNode currentNode;
            StatementNode parentNode;
            if (!objectTree.TryFindContextNodes(pos, out currentNode, out parentNode))
            {
                return;
            }

            // Get compilation context for value completions (IDs, file names, etc.)
            IEnumerable<PackageItem> contextItems = SourceFileCompiler.Instance.GetCompilationContext(this.textBuffer);

            List<Completion> completions = this.CreateCompletions(objectTree, currentNode, parentNode, null, pos, contextItems);

            ITrackingSpan applicableSpan = this.FindApplicableSpan(session);

            completionSets.Add(new CompletionSet(
                "Swix Terms",    // the non-localized title of the tab
                "Swix Terms",    // the display title of the tab
                applicableSpan,
                completions,
                null));
        }

        private ITrackingSpan FindApplicableSpan(ICompletionSession session)
        {
            // We eventually want to use an ITextStructureNavigator to expand the current point, but
            // first we have to teach it what out structure is.  For now, we just understand the Rtype
            // syntax directly.
            ////ITextStructureNavigator navigator = this.sourceProvider.NavigatorService.GetTextStructureNavigator(this.textBuffer);

            ITextSnapshot snapshot = session.TextView.TextSnapshot;
            ITrackingPoint triggerPoint = session.GetTriggerPoint(session.TextView.TextBuffer);

            // Look left and right until we're at a contextual break.
            SnapshotPoint initialPoint = triggerPoint.GetPoint(snapshot);
            SnapshotPoint leftPoint = initialPoint;
            SnapshotPoint rightPoint = initialPoint;

            ITextSnapshotLine line = leftPoint.GetContainingLine();

            // look left...
            if (leftPoint > line.Start)
            {
                leftPoint -= 1;
                while (leftPoint > line.Start && CompletionSource.IsTokenTermCharacter(leftPoint.GetChar()))
                {
                    leftPoint -= 1;
                }

                // If we bailed because of the character, advance back to the right.
                if (!CompletionSource.IsTokenTermCharacter(leftPoint.GetChar()))
                {
                    leftPoint += 1;
                }
            }

            // look right...
            // In theory, we might need to include spaces that are inside a quoted region, but we don't know if we've
            // just added the start-quote and might over-extend the selection.  It's safer to be conservative even if
            // it means leaving cruft in the buffer... it's easier for the user to delete the extra than to recover it if
            // we removed it.
            while (rightPoint < line.End && CompletionSource.IsTokenTermCharacter(rightPoint.GetChar()))
            {
                rightPoint += 1;
            }

            ITrackingSpan applicableSpan = snapshot.CreateTrackingSpan(
                leftPoint,
                rightPoint - leftPoint,
                SpanTrackingMode.EdgeInclusive);

            ////System.Diagnostics.Debug.WriteLine("**FindApplicableSpan: final span={0} ('{1}')", applicableSpan, applicableSpan.GetText(leftPoint.Snapshot));

            return applicableSpan;
        }

        // When finding applicable spans, these characters are included.
        internal static bool IsTokenTermCharacter(char ch)
        {
            // This matches the Lexer's behavior.
            // TODO: Actually user the Lexer to parse the line for us, for guaranteed consistency!
            return !char.IsWhiteSpace(ch) && ch != '=' && ch != '#';
        }

        private bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }


        private List<Completion> CreateCompletions(ObjectTree objectTree, StatementNode currentNode, StatementNode parentNode, string filterText, Position position, IEnumerable<PackageItem> contextItems)
        {
            List<Completion> completions = new List<Completion>();

            // Figure out what kind of completion we need...
            bool addObjectCompletions = false;
            bool addMemberCompletions = false;
            bool addValueCompletions = false;
            string currentText = string.Empty;
            Token<ParserTokenType> token = null;

            if (currentNode == null)
            {
                addObjectCompletions = true;
            }
            else
            {
                // Determine the completions based on the node!
                token = currentNode.Statement.AllTokens.FirstOrDefault(t => t.Range.End >= position);

                SpewDiagnostics("----- Finding completions for statement: [{0}]", currentNode.Statement);
                SpewDiagnostics("----- Position: {0}", position);
                SpewDiagnostics("----- Token: {0}", token);

                var tokenType = ParserTokenType.Unknown;

                if (token == null)
                {
                    // Need to find a surrogate token?
                }
                else
                {
                    if (token.Range.End < position)
                    {
                        // we're actually *after* that token, so figure out what that means...
                    }

                    tokenType = token.TokenType;
                    currentText = token.Value;
                }

                switch (tokenType)
                {
                    ////case ParserTokenType.Unknown:
                    ////    break;
                    case ParserTokenType.Whitespace:
                        // Depends on what the previous/next tokens are...
                        addMemberCompletions = true;
                        break;
                    ////case ParserTokenType.Comment:
                    ////    break;
                    case ParserTokenType.UseKeyword:
                        break;
                    case ParserTokenType.Object:
                        addObjectCompletions = true;
                        break;
                    case ParserTokenType.PropertyName:
                        addMemberCompletions = true;
                        break;
                    case ParserTokenType.Equals:
                        addValueCompletions = true;
                        break;
                    case ParserTokenType.PropertyValue:
                        addValueCompletions = true;
                        break;
                    case ParserTokenType.DoubleQuote:
                        addValueCompletions = true;
                        break;
                    case ParserTokenType.SingleQuote:
                        addValueCompletions = true;
                        break;
                    case ParserTokenType.Period:
                        break;
                    case ParserTokenType.NamespacePrefix:
                        break;
                    case ParserTokenType.NamespacePrefixDeclaration:
                        break;
                    case ParserTokenType.NamespaceDeclaration:
                        break;
                    case ParserTokenType.AttachedPropertyObject:
                        break;
                    default:
                        break;
                }
            }

            if (addObjectCompletions)
            {
                this.AddObjectCompletions(objectTree, parentNode, completions);
            }

            if (addMemberCompletions)
            {
                this.AddMemberCompletions(objectTree, currentNode, currentText, completions);
            }

            if (addValueCompletions)
            {
                this.AddValueCompletions(objectTree, currentNode, token, contextItems, completions);
            }

            // sort the completions...
            completions.Sort((a, b) =>
            {
                return a.DisplayText.CompareTo(b.DisplayText);
            });

            return completions;
        }

        private ObjectInstanceType GetTypeFromObjectStatement(StatementNode node)
        {
            if (node == null || 
                (node.Statement.StatementType != StatementType.Object &&
                node.Statement.StatementType != StatementType.ObjectStart))
            {
                return null;
            }

            int prefixIndex = 0;
            int objectIndex = 0;
            string prefix = string.Empty;
            string typeName = null;

            var parentToken = node.Statement.Tokens.ElementAt(prefixIndex);
            if (parentToken.TokenType == ParserTokenType.NamespacePrefix)
            {
                prefix = parentToken.Value;
                objectIndex = 1;
            }

            parentToken = node.Statement.Tokens.ElementAt(objectIndex);
            typeName = parentToken.Value;

            string clrNamespace;
            if (!this.typeCache.TryGetNamespaceByPrefix(prefix, out clrNamespace))
            {
                return null;
            }

            ObjectInstanceType oit;
            if (!this.typeCache.TryGetObjectInstanceType(clrNamespace, typeName, out oit))
            {
                return null;
            }

            return oit;
        }

        private void AddObjectCompletions(ObjectTree objectTree, StatementNode parentNode, List<Completion> completions)
        {
            SpewDiagnostics("----- Adding Objects for {0}", parentNode.Statement);

            var parentOit = this.GetTypeFromObjectStatement(parentNode);

            if (parentOit == null)
            {
                return;
            }

            // Add all of the types that are allowable as children of the parent object.
            foreach (var oit in this.typeCache.GetTypes().Where(t => t.Type.IsOrDerivesFrom(parentOit.GetChildrenTypes())))
            {
                var type = oit.Type;

                // Check for [Obsolete] objects...
                object[] attributes = type.GetCustomAttributes(typeof(ObsoleteAttribute), true);

                // Some attributes have a enabled/disabled flag, but [Obsolete] is always set if present.
                if (attributes == null || attributes.Length == 0)
                {
                    var prefix = this.typeCache.GetPrefixForClrNamespace(type.Namespace);

                    if (!string.IsNullOrEmpty(prefix))
                    {
                        prefix = string.Concat(prefix, this.prefixSeparator);
                    }

                    string objectName = this.FormatName(type.Name);
                    string replacement = string.Concat(prefix, objectName);

                    completions.Add(new Completion(
                        replacement,
                        string.Concat(replacement, " "), // added space makes continued completions worthwhile...
                        string.Format("{0}\nobject", type.Name),
                        this.GetGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic),
                        ""));
                }
            }
        }

        private void AddMemberCompletions(ObjectTree objectTree, StatementNode currentNode, string currentText, List<Completion> completions)
        {
            SpewDiagnostics("----- Adding Members for {0}", currentNode.Statement);

            var oit = this.GetTypeFromObjectStatement(currentNode);

            if (oit == null)
            {
                return;
            }

            // We want to add all the members that are publically writable, and aren't already used
            // on the object *unless* it's the current token context.
            var propsUsed = currentNode.Statement.Tokens.Where(t => t.TokenType == ParserTokenType.PropertyName);
            var propsToExclude = propsUsed.Select(t => t.Value).Where(s => !string.Equals(s, currentText)).ToList();

            foreach (var prop in oit.Properties)
            {
                // Check for [Obsolete] members...
                object[] attributes = prop.PropertyInfo.GetCustomAttributes(typeof(ObsoleteAttribute), true);

                // Some attributes have a enabled/disabled flag, but [Obsolete] is always set if present.
                if (attributes == null || attributes.Length == 0)
                {
                    string propName = this.FormatName(prop.Name);

                    if (propsToExclude.Contains(propName))
                    {
                        continue;
                    }

                    completions.Add(new Completion(
                        propName,
                        string.Concat(propName, "="),
                        string.Format("{0}.{1}\nattribute", oit.Type.Name, prop.Name),
                        this.GetGlyph(StandardGlyphGroup.GlyphGroupField, StandardGlyphItem.GlyphItemPublic),
                        ""));
                }
            }

            // In addition to any properties on the object itself, we also need to list any
            // attached properties that work for this object.
            foreach (var prop in this.typeCache.GetAttachedProperties())
            {
                var prefix = this.typeCache.GetPrefixForClrNamespace(prop.Type.Namespace);

                if (!string.IsNullOrEmpty(prefix))
                {
                    prefix = string.Concat(prefix, this.prefixSeparator);
                }

                var attachedObjectName = this.FormatName(prop.Type.Name);

                foreach (var setter in prop.Setters)
                {
                    // TODO: Check for [Obsolete] members...
                    ////object[] attributes = setter.GetCustomAttributes(typeof(ObsoleteAttribute), true);

                    ////// Some attributes have a enabled/disabled flag, but [Obsolete] is always set if present.
                    ////if (attributes == null || attributes.Length == 0)
                    ////{
                    if (oit.Type.IsOrDerivesFrom(setter.ObjectType))
                    {
                        var propName = this.FormatName(setter.Name);
                        var replacement = string.Concat(prefix, attachedObjectName, ".", propName);

                        completions.Add(new Completion(
                            replacement,
                            string.Concat(replacement, "="),
                            string.Format("{0}.{1}\nextension attribute\nfrom \"{2}\"", prop.Type.Name, setter.Name, prop.Type.Namespace),
                            this.GetGlyph(StandardGlyphGroup.GlyphExtensionMethod, StandardGlyphItem.GlyphItemPublic),
                            ""));
                    }
                    ////}
                }
            }
        }

        private void AddValueCompletions(ObjectTree objectTree, StatementNode currentNode, Token<ParserTokenType> token, IEnumerable<PackageItem> contextItems, List<Completion> completions)
        {
            if (token == null)
            {
                return;
            }

            SpewDiagnostics("----- Adding Values for {0}", currentNode.Statement);

            var oit = this.GetTypeFromObjectStatement(currentNode);

            if (oit == null)
            {
                return;
            }

            // Determine the type of the property or attached property we need...
            // Look at the tokens prior to the context token.
            var tokens = currentNode.Statement.AllTokens;
            var lookup = tokens.Take(tokens.IndexOf(token)).Reverse().ToList();

            var propToken = lookup.FirstOrDefault(t => t.TokenType == ParserTokenType.PropertyName);

            if (propToken == null)
            {
                return;
            }

            string propName = propToken.Value;
            string attachedObjectName = string.Empty;
            string prefix = string.Empty;

            var propIndex = lookup.IndexOf(propToken);
            var attachedObjectToken = lookup.Skip(propIndex).ElementAtOrDefault(2);

            if (attachedObjectToken != null && attachedObjectToken.TokenType == ParserTokenType.AttachedPropertyObject)
            {
                attachedObjectName = attachedObjectToken.Value;

                var prefixToken = lookup.Skip(propIndex).ElementAtOrDefault(4);

                if (prefixToken != null && prefixToken.TokenType == ParserTokenType.NamespacePrefix)
                {
                    prefix = prefixToken.Value;
                }
            }

            Type type = null;
            if (string.IsNullOrEmpty(attachedObjectName))
            {
                // normal property...
                PropertyInstanceType prop;
                if (oit.TryGetProperty(propName, out prop))
                {
                    type = prop.ValueType;
                }
            }
            else
            {
                // attached property...
                string clrNamespace;
                AttachedPropertyObjectType attachedObj;
                AttachedPropertySetterType setter;
                if (this.typeCache.TryGetNamespaceByPrefix(prefix, out clrNamespace) &&
                    this.typeCache.TryGetAttachedPropertyType(clrNamespace, attachedObjectName, out attachedObj) &&
                    attachedObj.TryGetPropertySetter(propName, out setter))
                {
                    type = setter.ValueType;
                }
            }

            if (type == null)
            {
                return;
            }

            if (type == typeof(string))
            {
                // We don't show any completions for strings... we can't!
            }
            else if (type.IsValueType)
            {
                if (type.IsEnum)
                {
                    foreach (string name in type.GetEnumNames())
                    {
                        string valueName = this.FormatName(name);

                        completions.Add(new Completion(
                            valueName,
                            valueName,
                            string.Format("{0}\n{1} enumeration value", name, type.Name),
                            this.GetGlyph(StandardGlyphGroup.GlyphGroupValueType, StandardGlyphItem.GlyphItemPublic),
                            ""));
                    }
                }
                else
                {
                    // simple type...
                    if (type == typeof(bool))
                    {
                        completions.Add(new Completion("true", "true", null, this.GetGlyph(StandardGlyphGroup.GlyphGroupValueType, StandardGlyphItem.GlyphItemPublic), ""));
                        completions.Add(new Completion("false", "false", null, this.GetGlyph(StandardGlyphGroup.GlyphGroupValueType, StandardGlyphItem.GlyphItemPublic), ""));
                    }
                    else
                    {
                        // ?? non-class value types / strings??
                        // We have to have more than one placeholder, or else VS will auto-fill for us!
                        string info = string.Format("TODO: Determine values for {0}", type.FullName);
                        completions.Add(new Completion("fake 1", "???", info, this.GetGlyph(StandardGlyphGroup.GlyphGroupValueType, StandardGlyphItem.GlyphItemPublic), ""));
                        completions.Add(new Completion("fake 2", "???", info, this.GetGlyph(StandardGlyphGroup.GlyphGroupValueType, StandardGlyphItem.GlyphItemPublic), ""));
                    }
                }
            }
            else
            {
                // Handle other classes/interfaces...
                // Find all the context items that could fulfill this type...
                List<string> usedValues = new List<string>();
                foreach (PackageItem item in contextItems)
                {
                    var itemType = item.GetType();

                    if (itemType.IsOrDerivesFrom(type))
                    {
                        // By convention, we know that the first entry is the shortest possible reference,
                        // and the last one is the longest/most-explicit.
                        List<string> referenceNames = item.GetReferenceNames().ToList();

                        // While editing, it's possible to end up with incomplete items that have no names
                        // at all.  In that case, just skip them!
                        if (referenceNames.Count == 0)
                        {
                            continue;
                        }

                        string shortest = referenceNames[0];
                        string longest = referenceNames[referenceNames.Count - 1];

                        string info = string.Format(
                            "Reference to {0} object\nshortest reference: {1}\nlongest reference: {2}",
                            this.FormatName(itemType.Name),
                            shortest,
                            longest);

                        foreach (string referenceName in referenceNames)
                        {
                            if (!usedValues.Contains(referenceName))
                            {
                                usedValues.Add(referenceName);

                                // Add quotes if there are any non-token characters in the value.
                                string insertionText = referenceName;
                                if (!referenceName.All(c => CompletionSource.IsTokenTermCharacter(c) && c != '"'))
                                {
                                    insertionText = string.Concat('"', referenceName, '"');
                                }

                                completions.Add(new Completion(
                                    referenceName,
                                    insertionText,
                                    info,
                                    this.GetGlyph(StandardGlyphGroup.GlyphGroupValueType, StandardGlyphItem.GlyphItemPublic),
                                    ""));
                            }
                        }
                    }
                }
            }
        }


        // Assumes 'name' has PascalCasing already.
        private string FormatName(string name)
        {
            Debug.Assert(char.IsUpper(name[0]));
            return (this.language == ParserLanguage.Rtype) ? name.ToRtypeName() : name;
        }

        [Conditional("SPEW_COMPLETION_DIAGNOSTICS")]
        private void SpewDiagnostics(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        [Conditional("SPEW_COMPLETION_DIAGNOSTICS")]
        private void SpewDiagnostics(string messageFormat, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(messageFormat, args);
        }

        [Conditional("SPEW_COMPLETION_DIAGNOSTICS")]
        private void SpewDiagnostics(IEnumerable<string> messages)
        {
            foreach (string message in messages)
            {
                SpewDiagnostics(message);
            }
        }
    }
}
