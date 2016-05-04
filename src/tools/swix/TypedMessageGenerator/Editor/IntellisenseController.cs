// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.Windows.Input;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;

namespace WixToolset.Simplified.TypedMessageGenerator.Editor
{
    // INFO: { KeyProcessor f:cs p:env p:vb p:platform -p:platform\applications\samples -p:platform\language\test -p:platform\language\samples -p:platform\text\test p:venus }
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("TypedMessage Intellisense Controller")]
    [ContentType(TypeConstants.Content)]
    [Export(typeof(IKeyProcessorProvider))]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Order(Before = "DefaultKeyProcessor")]
    [Order(Before = "intellisense key binding processor")]
    internal class IntellisenseControllerProvider : IIntellisenseControllerProvider, IKeyProcessorProvider ////, IHasBrokersForFilter
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;

        [Import]
        public IQuickInfoBroker QuickInfoBroker { get; set; }

        [Import]
        public ICompletionBroker CompletionBroker { get; set; }

        [Import]
        public ISignatureHelpBroker SignatureHelpBroker { get; set; }

        ////[Import]
        ////public SVsServiceProvider ServiceProvider { get; set; }

        ////[Import]
        ////internal ITextBufferFactoryService TextBufferFactory { get; set; }

        ////[Import]
        ////internal IIntellisenseSessionStackMapService IntellisenseSessionStackMapService { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() =>
                new IntellisenseController(this, textView /*, subjectBuffers*/));
        }

        public KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
        {
            return wpfTextView.Properties.GetOrCreateSingletonProperty(() =>
                new IntellisenseController(this, wpfTextView /*, null */));
        }
    }

    internal class IntellisenseController : KeyProcessor, IIntellisenseController
    {
        private IntellisenseControllerProvider provider;
        private ITextView textView;
        ////private IList<ITextBuffer> subjectBuffers;

        ////public IntellisenseController(IntellisenseControllerProvider provider, ITextView textView, IList<ITextBuffer> subjectBuffers)
        public IntellisenseController(IntellisenseControllerProvider provider, ITextView textView)
        {
            this.provider = provider;
            this.textView = textView;
            ////this.subjectBuffers = subjectBuffers;

            this.textView.MouseHover += TextView_MouseHover;

            ////this.textView.Properties.GetOrCreateSingletonProperty(() => new CommandFilter(textViewAdapter, textView, this.provider));
        }

        // Should this not be a MouseProcessor?  See env\editor\pkg\impl\urlclickmouseprocessor.cs
        void TextView_MouseHover(object sender, MouseHoverEventArgs e)
        {
            // find the mouse position by mapping down to the subject buffer
            SnapshotPoint? point = this.textView.BufferGraph.MapDownToFirstMatch(
                new SnapshotPoint(this.textView.TextSnapshot, e.Position),
                PointTrackingMode.Positive,
                ////snapshot => this.subjectBuffers.Contains(snapshot.TextBuffer),
                snapshot => this.textView.TextBuffer == snapshot.TextBuffer,
                PositionAffinity.Predecessor);

            if (point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position, PointTrackingMode.Positive);

                if (!this.provider.QuickInfoBroker.IsQuickInfoActive(this.textView))
                {
                    this.provider.QuickInfoBroker.TriggerQuickInfo(this.textView, triggerPoint, true);
                }
            }
        }

        public void Detach(ITextView textView)
        {
            if (this.textView == textView)
            {
                this.textView.MouseHover -= TextView_MouseHover;

                ////this.textView.Properties.RemoveProperty(typeof(CommandFilter));
                ////CommandFilter filter = this.textView.Properties.GetProperty<CommandFilter>(typeof(CommandFilter));
                ////filter.Dispose();

                this.textView = null;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            ////??
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
            ////??
        }

        #region KeyProcessor overrides

        public override bool IsInterestedInHandledEvents { get { return true; } }
 
        public override void PreviewKeyDown(KeyEventArgs args)
        {
            if (args.Key == Key.Left || args.Key == Key.Right)
            {
                // As we move left or right, we need to re-compute the "current" parameter.
                // functionality isn't baked in to the ISignature interface, unfortunately.
                this.ComputeCurrentParameter();
            }

            if (this.provider.CompletionBroker.IsCompletionActive(this.textView))
            {
                // Return and Tab aren't getting here!
                if (((args.Key == Key.Space || args.Key == Key.Tab || args.Key == Key.Return) &&
                    args.KeyboardDevice.Modifiers == ModifierKeys.None) ||
                    (args.Key == Key.OemPeriod || args.Key == Key.OemSemicolon))
                {
                    foreach (var session in this.provider.CompletionBroker.GetSessions(this.textView))
                    {
                        session.Commit();
                    }

                    ////// Let the space key pass through for typing...
                    ////args.Handled = args.Key != Key.Space;
                    args.Handled = true;
                }
            }

            // Treat Ctrl+Space as "show completion"
            if (args.Key == Key.Space && args.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                this.TriggerCompletion();
                args.Handled = true;
            }

            // Treat Ctrl+Shift+Space as "show signature help"
            if (args.Key == Key.Space && args.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                this.TriggerSignatureHelp();
                args.Handled = true;
            }

            if (!args.Handled)
            {
                base.KeyDown(args);
            }
        }

        public override void TextInput(TextCompositionEventArgs args)
        {
            base.TextInput(args);

            if (args.Text.Length == 1)
            {
                char c = args.Text[0];

                switch (c)
                {
                        // For '{' and any of the internal replacement delimiters, try triggering signature help...
                    case '{':
                    case '[':
                    case ']':
                    case ',':
                    case ':':
                        this.TriggerSignatureHelp();
                        break;

                    case '}':
                        this.DismissSignatureHelp();
                        break;
                }
            }

            this.Match();
            this.ComputeCurrentParameter();
        }

        #endregion // KeyProcessor overrides

        private void TriggerCompletion()
        {
            if (!this.provider.CompletionBroker.IsCompletionActive(this.textView))
            {
                this.provider.CompletionBroker.TriggerCompletion(this.textView);
            }
        }

        private void TriggerSignatureHelp()
        {
            if (!this.provider.SignatureHelpBroker.IsSignatureHelpActive(this.textView))
            {
                this.provider.SignatureHelpBroker.TriggerSignatureHelp(this.textView);
                this.Match();
                this.ComputeCurrentParameter();
            }
        }

        private void DismissSignatureHelp()
        {
            if (this.provider.SignatureHelpBroker.IsSignatureHelpActive(this.textView))
            {
                this.provider.SignatureHelpBroker.DismissAllSessions(this.textView);
            }
        }

        private void Match()
        {
            if (this.provider.SignatureHelpBroker.IsSignatureHelpActive(this.textView))
            {
                foreach (var session in this.provider.SignatureHelpBroker.GetSessions(this.textView))
                {
                    session.Match();
                }
            }
        }

        private void ComputeCurrentParameter()
        {
            if (this.provider.SignatureHelpBroker.IsSignatureHelpActive(this.textView))
            {
                foreach (var session in this.provider.SignatureHelpBroker.GetSessions(this.textView))
                {
                    // Re-compute the "current" parameter.  This functionality isn't baked in
                    // to the ISignature interface, unfortunately.
                    ReplacementSignature replacementSignature = session.SelectedSignature as ReplacementSignature;
                    if (replacementSignature != null)
                    {
                        replacementSignature.ComputeCurrentParameter(this.textView.Caret.Position.BufferPosition);
                    }
                }
            }
        }

    }
}
