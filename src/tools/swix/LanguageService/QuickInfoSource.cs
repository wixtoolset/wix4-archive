// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace WixToolset.Simplified.LanguageService
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("Swix QuickInfo Provider")]
    [ContentType(TypeConstants.Content)]
    [Order(Before = "default")]
    internal class QuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new QuickInfoSource(textBuffer);
        }
    }

    internal class QuickInfoSource : IQuickInfoSource
    {
        ITextBuffer textBuffer;

        public QuickInfoSource(ITextBuffer textBuffer)
        {
            this.textBuffer = textBuffer;
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

            // Find the source file we're looking at.
            SourceFileData sourceFileData = SourceFileCompiler.Instance.FindSourceFileData(this.textBuffer);
            if (sourceFileData == null)
            {
                return;
            }

            // Find the line/position of the trigger...
            ITextSnapshotLine line = subjectTriggerPoint.Value.GetContainingLine();
            int column = subjectTriggerPoint.Value.Position - line.Start;

            // See if there's an error/message that intersects the trigger...
            CompilerMessageEventArgs message = sourceFileData.Messages.FirstOrDefault(m =>
                m.LineNumber -1 == line.LineNumber &&
                m.LinePosition -1 <= column &&
                m.LinePositionEnd > column);

            if (message == null)
            {
                return;
            }

            // If we found it, create the span and content!
            applicableToSpan = subjectTriggerPoint.Value.Snapshot.CreateTrackingSpanFromSwix(message);

            // For some reason, we sometimes get doubled (and more!) messages... we specifically check for this, just in case.
            if (quickInfoContent.Count > 0 || session.QuickInfoContent.Count > 0)
            {
                System.Diagnostics.Debug.Assert(false, "multiple quick info?");
                System.Diagnostics.Debugger.Break();
            }

            quickInfoContent.Add(message.Message.Message);
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
