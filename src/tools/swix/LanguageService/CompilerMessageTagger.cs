// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace WixToolset.Simplified.LanguageService
{
    [Export(typeof(IViewTaggerProvider))]
    [Name("Swix Compiler Message Tagger")]
    [ContentType(TypeConstants.Content)]
    [TagType(typeof(ErrorTag))]
    internal class CompilerMessageTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            SourceFileCompiler.Instance.SetImports(this.TextDocumentFactoryService, this.ServiceProvider);

            // Tell the source file compiler about the new view...
            SourceFileCompiler.Instance.AddTextView(textView);

            ITextDocument textDocument;
            if (!this.TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out textDocument))
            {
                throw new Exception("Could not find ITextDocument from textView!");
            }

            string fullPath = Path.GetFullPath(textDocument.FilePath);

            return new CompilerMessageTagger(textView, fullPath) as ITagger<T>;
        }
    }

    internal class CompilerMessageTagger : SimpleTagger<ErrorTag>
    {
        private SourceFileData sourceFileData;

        public string FileName { get; private set; }
        public ITextView textView { get; private set; }

        public CompilerMessageTagger(ITextView textView, string fullPath)
            : base(textView.TextBuffer)
        {
            this.textView = textView;
            this.FileName = fullPath;

            this.sourceFileData = SourceFileCompiler.Instance.FindSourceFileData(fullPath);

            this.sourceFileData.SourceFileSet.SetClosed += SourceFileSet_SetClosed;
            this.sourceFileData.SourceFileSet.CompileComplete += SourceFileSet_CompileComplete;
        }

        void SourceFileSet_SetClosed(object sender, EventArgs e)
        {
            this.sourceFileData.SourceFileSet.SetClosed -= SourceFileSet_SetClosed;
            this.sourceFileData.SourceFileSet.CompileComplete -= SourceFileSet_CompileComplete;
        }

        void SourceFileSet_CompileComplete(object sender, EventArgs e)
        {
            // Update all the tags!
            this.RemoveTagSpans(span => true);

            foreach (CompilerMessageEventArgs message in this.sourceFileData.Messages)
            {
                string errorType = "other error";

                switch (message.Message.Type)
                {
                    case CompilerMessage.CompilerMessageType.LexerError:
                        errorType = "syntax error";
                        break;
                    case CompilerMessage.CompilerMessageType.Error:
                        errorType = "compiler error";
                        break;
                    case CompilerMessage.CompilerMessageType.Warning:
                        errorType = "warning";
                        break;
                    case CompilerMessage.CompilerMessageType.Information:
                    case CompilerMessage.CompilerMessageType.Verbose:
                    default:
                        errorType = null;
                        break;
                }

                if (errorType != null)
                {
                     ErrorTag tag = new ErrorTag(errorType, message.Message.Message);
                    this.CreateTagSpan(this.textView.TextSnapshot.CreateTrackingSpanFromSwix(message), tag);
                }
            }
        }
    }
}
