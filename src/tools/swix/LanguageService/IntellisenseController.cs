// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace WixToolset.Simplified.LanguageService
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("Swix Intellisense Controller")]
    [ContentType(TypeConstants.Content)]
    internal class IntellisenseControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new IntellisenseController(this, textView, subjectBuffers);
        }
    }

    internal class IntellisenseController : IIntellisenseController
    {
        private IntellisenseControllerProvider provider;
        private ITextView textView;
        private IList<ITextBuffer> subjectBuffers;
        private IQuickInfoSession session;

        public IntellisenseController(IntellisenseControllerProvider provider, ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            this.provider = provider;
            this.textView = textView;
            this.subjectBuffers = subjectBuffers;

            this.textView.MouseHover += TextView_MouseHover;
        }

        void TextView_MouseHover(object sender, MouseHoverEventArgs e)
        {
            // find the mouse position by mapping down to the subject buffer
            SnapshotPoint? point = this.textView.BufferGraph.MapDownToFirstMatch
                 (new SnapshotPoint(this.textView.TextSnapshot, e.Position),
                PointTrackingMode.Positive,
                snapshot => this.subjectBuffers.Contains(snapshot.TextBuffer),
                PositionAffinity.Predecessor);

            if (point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position, PointTrackingMode.Positive);

                if (!this.provider.QuickInfoBroker.IsQuickInfoActive(this.textView))
                {
                    this.session = this.provider.QuickInfoBroker.TriggerQuickInfo(this.textView, triggerPoint, true);
                }
            }
        }

        public void Detach(ITextView textView)
        {
            if (this.textView == textView)
            {
                this.textView.MouseHover -= TextView_MouseHover;
                this.textView = null;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
        }
    }
}
