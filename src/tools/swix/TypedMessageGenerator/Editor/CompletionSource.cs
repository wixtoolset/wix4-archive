// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using WixToolset.Simplified.TypedMessageGenerator.Parser;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace WixToolset.Simplified.TypedMessageGenerator.Editor
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(TypeConstants.Content)]
    [Name("TypedMessage Completion")]
    internal class CompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal IGlyphService GlyphService { get; set; }

        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new CompletionSource(this, textBuffer);
        }
    }

    internal class CompletionSource : ICompletionSource
    {
        private CompletionSourceProvider sourceProvider;
        private ITextBuffer textBuffer;
        private IClassifier classifier;

        public CompletionSource(CompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            this.sourceProvider = sourceProvider;
            this.textBuffer = textBuffer;
            this.classifier = this.sourceProvider.ClassifierAggregatorService.GetClassifier(this.textBuffer);
        }

        private System.Windows.Media.ImageSource GetGlyph(StandardGlyphGroup group, StandardGlyphItem item)
        {
            return this.sourceProvider.GlyphService.GetGlyph(group, item);
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            SnapshotPoint point = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot).Value;

            // Find the span we're in...
            IList<ClassificationSpan> spans = this.classifier.GetClassificationSpans(point.GetContainingLine().Extent);
            ClassificationSpan span = spans.FirstOrDefault(s => s.Span.Contains(point - 1));

            // Get other context...
            MessageData context = SourceFileCompiler.Instance.GetCompilationContext(this.textBuffer);

            // only complete message types?
            List<Completion> completions = this.CreateCompletions(null, span, point, context);

            ITrackingSpan applicableSpan = this.FindApplicableSpan(session, span, point);

            completionSets.Add(new CompletionSet(
                "TypedMessage Terms",    // the non-localized title of the tab
                "TypedMessage Terms",    // the display title of the tab
                applicableSpan,
                completions,
                null));
        }

        private ITrackingSpan FindApplicableSpan(ICompletionSession session, ClassificationSpan span, SnapshotPoint point)
        {
            // We eventually want to use an ITextStructureNavigator to expand the current point, but
            // first we have to teach it what out structure is.  For now, we just understand the Rtype
            // syntax directly.
            ////ITextStructureNavigator navigator = this.sourceProvider.NavigatorService.GetTextStructureNavigator(this.textBuffer);

            ITextSnapshot snapshot = session.TextView.TextSnapshot;

            if (span != null)
            {
                return snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeInclusive);
            }

            return snapshot.CreateTrackingSpan(point, 0, SpanTrackingMode.EdgeInclusive);
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

        private List<Completion> CreateCompletions(string filterText, ClassificationSpan span, SnapshotPoint point, MessageData context)
        {
            List<Completion> completions = new List<Completion>();

            // TODO: figure out which kind of completions to offer...
            if ((span != null && span.ClassificationType.IsOfType(TypeConstants.MessageType)) ||
                (span == null && point.Position == point.GetContainingLine().Start))
            {
                AddMessageTypeCompletions(context, completions);
            }

            return completions;
        }

        private void AddMessageTypeCompletions(MessageData context, List<Completion> completions)
        {
            string prefix = string.Empty;

            foreach (MessageType type in context.Types)
            {
                string typeName = type.Name;
                string replacement = string.Concat(prefix, typeName);

                completions.Add(new Completion(
                    replacement,
                    string.Concat(replacement, " "), // added space makes continued completions worthwhile...
                    string.Format("{0}", type.Name),
                    this.GetGlyph(StandardGlyphGroup.GlyphGroupClass, StandardGlyphItem.GlyphItemPublic),
                    ""));
            }

            // We could also be adding a new type, so include "type"...
            completions.Add(new Completion(
                "type",
                string.Concat("type", " "), // added space makes continued completions worthwhile...
                string.Format("{0}", "type"),
                this.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic),
                ""));
        }
    }
}
