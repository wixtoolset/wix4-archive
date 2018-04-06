// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using WixToolset.Simplified.ParserCore;
using WixToolset.Simplified.TypedMessageGenerator.Parser;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace WixToolset.Simplified.TypedMessageGenerator.Editor
{
    [Export(typeof(IClassifierProvider))]
    [Name("TypedMessage Classifier")]
    [ContentType(TypeConstants.Content)]
    // REVIEW: Do we want to provide error tagging here (for CodeSense), or just let a background-compile
    // catch it?  In theory it's nice to do as one types, but it may be overkill.
    ////[Export(typeof(IViewTaggerProvider))]
    ////[TagType(typeof(ErrorTag))]
    internal class ClassifierProvider : IClassifierProvider ////, IViewTaggerProvider
    {
        /// <summary>
        /// Import the classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }


        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return this.EnsureClassifier(buffer);
        }

        ////public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        ////{
        ////    Classifier classifier = this.EnsureClassifier(buffer);
        ////    classifier.SetView(textView);
        ////    return classifier as ITagger<T>;
        ////}

        private Classifier EnsureClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty<Classifier>(
                () => new Classifier(this, buffer));
        }
    }

    /// <summary>
    /// Classifier for TypedMessage Msgs files.
    /// </summary>
    class Classifier : /*SimpleTagger<ErrorTag>,*/ IClassifier ////, IDisposable
    {
        private ClassifierProvider provider;
        ////private ErrorListProvider errorListProvider;
        ////private string filename;
        private ITextBuffer buffer;
        ////private ITextView textView;

        private IClassificationType commentType;
        private IClassificationType whitespaceType;
        private IClassificationType keywordType;
        private IClassificationType messageTypeDefinitionType;
        private IClassificationType messageTypeRangeType;
        private IClassificationType messageTypeType;
        private IClassificationType messageNameType;
        private IClassificationType stringType;
        private IClassificationType escapeType;
        private IClassificationType replacementDelimiterType;
        private IClassificationType replacementDelimiterStart;
        private IClassificationType replacementDelimiterEnd;
        private IClassificationType replacementNameType;
        private IClassificationType replacementTypeType;
        private IClassificationType replacementPositionType;
        private IClassificationType replacementAlignmentType;
        private IClassificationType replacementFormatType;

        internal Classifier(ClassifierProvider provider, ITextBuffer buffer)
            ////: base(buffer)
        {
            this.provider = provider;
            this.buffer = buffer;

            ////// Get the filename, if we can...
            ////ITextDocument textDocument;
            ////if (this.provider.TextDocumentFactoryService.TryGetTextDocument(this.buffer, out textDocument))
            ////{
            ////    this.filename = textDocument.FilePath;
            ////}

            ////// Create our own error list provider
            ////this.errorListProvider = new ErrorListProvider(this.provider.ServiceProvider);
            ////this.errorListProvider.ProviderGuid = Guid.NewGuid();
            ////this.errorListProvider.ProviderName = "TypedMessage CodeSense Errors";

            IClassificationTypeRegistryService registry = this.provider.ClassificationRegistry;

            this.commentType = registry.GetClassificationType(TypeConstants.Comment);
            this.whitespaceType = registry.GetClassificationType(TypeConstants.Whitespace);
            this.keywordType = registry.GetClassificationType(TypeConstants.Keyword);
            this.messageTypeDefinitionType = registry.GetClassificationType(TypeConstants.MessageTypeDefinition);
            this.messageTypeRangeType = registry.GetClassificationType(TypeConstants.MessageTypeRange);
            this.messageTypeType = registry.GetClassificationType(TypeConstants.MessageType);
            this.messageNameType = registry.GetClassificationType(TypeConstants.MessageName);
            this.stringType = registry.GetClassificationType(TypeConstants.String);
            this.escapeType = registry.GetClassificationType(TypeConstants.Escape);
            this.replacementDelimiterType = registry.GetClassificationType(TypeConstants.ReplacementDelimiter);
            this.replacementDelimiterStart = registry.GetClassificationType(TypeConstants.ReplacementStart);
            this.replacementDelimiterEnd = registry.GetClassificationType(TypeConstants.ReplacementEnd);
            this.replacementNameType = registry.GetClassificationType(TypeConstants.ReplacementName);
            this.replacementTypeType = registry.GetClassificationType(TypeConstants.ReplacementType);
            this.replacementPositionType = registry.GetClassificationType(TypeConstants.ReplacementPosition);
            this.replacementAlignmentType = registry.GetClassificationType(TypeConstants.ReplacementAlignment);
            this.replacementFormatType = registry.GetClassificationType(TypeConstants.ReplacementFormat);
        }

        ////public void SetView(ITextView textView)
        ////{
        ////    this.textView = textView;
        ////}

        const int UnknownLineNumber = -1;

        private class SnapshotTextProvider : ITextProvider
        {
            private ITextSnapshot snapshot;
            private SnapshotSpan spanLimit;

            public SnapshotTextProvider(ITextSnapshot snapshot, SnapshotSpan spanLimit)
            {
                this.snapshot = snapshot;
                this.spanLimit = spanLimit;
            }

            public bool TryGetText(Position pos, out string text, out Range range)
            {
                // If we're past the span being asked for, fail...
                if (pos.Offset >= this.spanLimit.End.Position)
                {
                    text = null;
                    range = new Range(pos, 0);
                    return false;
                }

                var line = this.snapshot.GetLineFromPosition(pos.Offset);
                text = line.GetTextIncludingLineBreak();

                // In theory, we might start in a different spot than we expected...
                // In practice, this doesn't happen.
                System.Diagnostics.Debug.Assert(line.Start.Position == pos.Offset);
                range = new Range(pos, text.Length);

                return true;
            }
        }

        /// <summary>
        /// Scans the given SnapshotSpan for potential matches for this classification.
        /// </summary>
        /// <param name="span">The span currently being classified</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List <ClassificationSpan> classifications = new List<ClassificationSpan>();

            // Extend the span to full lines, and tokenize/classify...
            var firstLine = span.Start.GetContainingLine();
            var lastLine = (span.End - 1).GetContainingLine();

            // Remove any errors in the span?
            ////this.RemoveTagSpans(s => span.Contains(s.Span.GetSpan(span.Snapshot)));
            ////var toRemove = this.errorListProvider.Tasks.OfType<Task>().Where(
            ////    t => firstLine.LineNumber <= t.Line && t.Line <= lastLine.LineNumber).ToList();
            ////foreach (var task in toRemove)
            ////{
            ////    this.errorListProvider.Tasks.Remove(task);
            ////}

            SnapshotTextProvider textProvider = new SnapshotTextProvider(span.Snapshot, span);

            var statements = StatementParser.ParseStatements(
                new Position(firstLine.Start.Position, firstLine.LineNumber, 0),
                textProvider);

            foreach (var statement in statements)
            {
                // If the statement ends before the requested span, ignore it completely.
                if (statement.AllTokens.Last().Range.End.Offset <= span.Start.Position)
                {
                    continue;
                }

                foreach (var token in statement.AllTokens)
                {
                    // Skip any tokens entirely before or after the span...
                    if (token.Range.End.Offset <= span.Start.Position)
                    {
                        continue;
                    }
                    else if (token.Range.Start.Offset >= span.End.Position)
                    {
                        break;
                    }

                    IClassificationType classification = null;

                    switch (token.TokenType)
                    {
                        case ParserTokenType.Unknown:
                            break;
                        case ParserTokenType.Whitespace:
                            classification = this.whitespaceType;
                            break;
                        case ParserTokenType.Comment:
                            classification = this.commentType;
                            break;
                        case ParserTokenType.TypeKeyword:
                            classification = this.keywordType;
                            break;
                        case ParserTokenType.MessageTypeDefinition:
                            classification = this.messageTypeDefinitionType;
                            break;
                        case ParserTokenType.MessageTypeRange:
                            classification = this.messageTypeRangeType;
                            break;
                        case ParserTokenType.MessageType:
                            classification = this.messageTypeType;
                            break;
                        case ParserTokenType.MessageName:
                            classification = this.messageNameType;
                            break;
                        case ParserTokenType.LeftBrace:
                            classification = this.replacementDelimiterStart;
                            break;
                        case ParserTokenType.RightBrace:
                            classification = this.replacementDelimiterEnd;
                            break;
                        case ParserTokenType.LeftBracket:
                        case ParserTokenType.RightBracket:
                        case ParserTokenType.Comma:
                        case ParserTokenType.Colon:
                            classification = this.replacementDelimiterType;
                            break;
                        case ParserTokenType.ReplacementName:
                            classification = this.replacementNameType;
                            break;
                        case ParserTokenType.ReplacementType:
                            classification = this.replacementTypeType;
                            break;
                        case ParserTokenType.ReplacementPosition:
                            classification = this.replacementPositionType;
                            break;
                        case ParserTokenType.ReplacementAlignment:
                            classification = this.replacementAlignmentType;
                            break;
                        case ParserTokenType.ReplacementFormat:
                            classification = this.replacementFormatType;
                            break;
                        case ParserTokenType.Value:
                            classification = this.stringType;
                            break;
                        case ParserTokenType.Escape:
                            classification = this.escapeType;
                            break;
                        default:
                            break;
                    }

                    if (classification != null)
                    {
                        SnapshotSpan? intersect = span.Intersection(token.Range.AsSpan());

                        if (intersect.HasValue)
                        {
                            classifications.Add(new ClassificationSpan(intersect.Value, classification));
                        }
                    }

                    // If there were any errors, pass those up...
                    ////if (token.Errors != null)
                    ////{
                    ////    foreach (Error error in token.Errors)
                    ////    {
                    ////        ErrorTag tag = new ErrorTag("syntax error", error.Message);
                    ////        this.CreateTagSpan(span.Snapshot.CreateTrackingSpan(token), tag);

                    ////        ErrorTask task = new ErrorTask();
                    ////        task.Document = this.filename;
                    ////        task.Line = error.Range.Start.Line;
                    ////        task.Column = error.Range.Start.Column;
                    ////        task.Text = error.Message;

                    ////        // inline?
                    ////        task.Category = TaskCategory.CodeSense;
                    ////        task.ErrorCategory = TaskErrorCategory.Error;

                    ////        // Why do we have to do this ourselves?
                    ////        if (!string.IsNullOrWhiteSpace(task.Document))
                    ////        {
                    ////            IVsUIHierarchy hierarchy;
                    ////            uint item;
                    ////            IVsWindowFrame frame;

                    ////            if (VsShellUtilities.IsDocumentOpen(
                    ////                this.provider.ServiceProvider,
                    ////                task.Document,
                    ////                VSConstants.LOGVIEWID.Any_guid,
                    ////                out hierarchy,
                    ////                out item,
                    ////                out frame))
                    ////            {
                    ////                task.HierarchyItem = hierarchy;
                    ////            }

                    ////            task.Navigate += Task_Navigate;
                    ////        }

                    ////        this.errorListProvider.Tasks.Add(task);
                    ////    }
                    ////}
                }

                // If the statement ends at or after the requested span, we're done!
                if (statement.AllTokens.Last().Range.End.Offset >= span.End.Position)
                {
                    break;
                }
            }

            return classifications;
        }

        ////private void Task_Navigate(object sender, EventArgs e)
        ////{
        ////    Task task = sender as Task;

        ////    // We know the document is open, don't we?  This just forces it to the foreground/activates it...
        ////    VsShellUtilities.OpenDocument(this.provider.ServiceProvider, task.Document);

        ////    if (this.textView != null)
        ////    {
        ////        SnapshotSpan span = this.textView.TextSnapshot.CreateSpan(task.Line, task.Column, task.Column + 1);
        ////        this.textView.Caret.MoveTo(span.Start, PositionAffinity.Successor);
        ////        this.textView.ViewScroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.ShowStart);
        ////    }
        ////}


#pragma warning disable 67
        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67

        ////#region IDisposable Members

        ////private bool disposed;
        ////public void Dispose()
        ////{
        ////    if (!this.disposed)
        ////    {
        ////        if (this.errorListProvider != null)
        ////        {
        ////            this.errorListProvider.Dispose();
        ////            this.errorListProvider = null;
        ////        }

        ////        this.disposed = true;
        ////        GC.SuppressFinalize(this);
        ////    }
        ////}

        ////#endregion
    }
}
