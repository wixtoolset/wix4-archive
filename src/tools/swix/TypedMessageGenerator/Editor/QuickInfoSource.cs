// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using System.Text;

namespace WixToolset.Simplified.TypedMessageGenerator.Editor
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("TypedMessage QuickInfo Provider")]
    [ContentType(TypeConstants.Content)]
    [Order(Before = "default")]
    internal class QuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new QuickInfoSource(this, textBuffer);
        }
    }

    internal class QuickInfoSource : IQuickInfoSource
    {
        private QuickInfoSourceProvider provider;
        private ITextBuffer textBuffer;
        private IClassifier classifier;

        public QuickInfoSource(QuickInfoSourceProvider provider, ITextBuffer textBuffer)
        {
            this.provider = provider;
            this.textBuffer = textBuffer;
            this.classifier = this.provider.ClassifierAggregatorService.GetClassifier(this.textBuffer);
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            // Map the trigger point down to our buffer.
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue)
            {
                return;
            }

            SnapshotPoint point = subjectTriggerPoint.Value;

            // Find the span we're in...
            IList<ClassificationSpan> spans = this.classifier.GetClassificationSpans(point.GetContainingLine().Extent);
            ClassificationSpan span = spans.FirstOrDefault(s => s.Span.Contains(point));

            if (span == null)
            {
                return;
            }

            // If we're over a replacement block, expand the span to include the entire replacement...
            if (IsReplacementSpan(span))
            {
                // expand to find entire replacement block...
                int initialSpan = spans.IndexOf(span);
                int firstSpan = initialSpan;
                int lastSpan = initialSpan;

                while (firstSpan > 0 && !spans[firstSpan].ClassificationType.IsOfType(TypeConstants.ReplacementStart))
                {
                    --firstSpan;
                }

                while (lastSpan < spans.Count - 1 && !spans[lastSpan].ClassificationType.IsOfType(TypeConstants.ReplacementEnd))
                {
                    ++lastSpan;
                }

                SnapshotSpan replacementSpan = new SnapshotSpan(
                    spans[firstSpan].Span.Start,
                    spans[lastSpan].Span.End);

                applicableToSpan = point.Snapshot.CreateTrackingSpan(replacementSpan, SpanTrackingMode.EdgeInclusive);

                // Programmatically build up the expected syntax...
                var replacementSpans = spans.Skip(firstSpan - 1).Take(lastSpan - firstSpan + 1);
                bool hasType = replacementSpans.Any(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementType));
                bool hasPosition = replacementSpans.Any(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementPosition));
                bool hasAlignment = replacementSpans.Any(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementAlignment));
                bool hasFormat = replacementSpans.Any(s => s.ClassificationType.IsOfType(TypeConstants.ReplacementFormat));

                StringBuilder builder = new StringBuilder();

                builder.Append("{");

                if (hasType || hasPosition)
                {
                    builder.Append("[");
                    if (hasType)
                    {
                        builder.Append("type");
                    }
                    if (hasPosition)
                    {
                        if (hasType)
                        {
                            builder.Append(",");
                        }
                        builder.Append("position");
                    }
                    builder.Append("]");
                }

                builder.Append("name");

                if (hasAlignment)
                {
                    builder.Append(",alignment");
                }

                if (hasFormat)
                {
                    builder.Append(":format");
                }

                builder.Append("}");

                //// See QuickInfoWithLink class (venus\editors\common\web\quickinfo) for not-just-text implementation.

                // 4 mutually exclusive attributes means 16 possible formats...
                quickInfoContent.Add(string.Format(
                    "replacement {0}  (+ 15 overloads)\nProvides type-safe replacement parameter.",
                    builder.ToString()));
            }
            else if (span.ClassificationType.IsOfType(TypeConstants.Keyword))
            {
                applicableToSpan = point.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeInclusive);
                quickInfoContent.Add("Keyword: type");
            }
            else if (span.ClassificationType.IsOfType(TypeConstants.MessageTypeDefinition))
            {
                applicableToSpan = point.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeInclusive);
                quickInfoContent.Add(string.Format("Message type definition: {0}", span.Span.GetText()));
            }
            else if (span.ClassificationType.IsOfType(TypeConstants.MessageType))
            {
                applicableToSpan = point.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeInclusive);
                quickInfoContent.Add(string.Format("Message type: {0}", span.Span.GetText()));
            }
            else if (span.ClassificationType.IsOfType(TypeConstants.MessageName))
            {
                applicableToSpan = point.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeInclusive);
                quickInfoContent.Add(string.Format("Message name: {0}", span.Span.GetText()));
            }
            else if (span.ClassificationType.IsOfType(TypeConstants.Escape))
            {
                applicableToSpan = point.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeInclusive);
                quickInfoContent.Add(string.Format("String escape: {0}", span.Span.GetText()));
            }
        }

        private static bool IsReplacementSpan(ClassificationSpan span)
        {
            return span.ClassificationType.IsOfType(TypeConstants.ReplacementStart) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementEnd) || 
                span.ClassificationType.IsOfType(TypeConstants.ReplacementDelimiter) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementName) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementType) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementPosition) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementAlignment) ||
                span.ClassificationType.IsOfType(TypeConstants.ReplacementFormat);
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
    }
}
