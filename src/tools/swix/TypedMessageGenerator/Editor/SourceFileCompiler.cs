// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using WixToolset.Simplified.ParserCore;
using WixToolset.Simplified.TypedMessageGenerator.Parser;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Tagging;

namespace WixToolset.Simplified.TypedMessageGenerator.Editor
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [Name("TypedMessage Compiler")]
    [ContentType(TypeConstants.Content)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class SourceFileCompilerProvider : IWpfTextViewCreationListener
    {
        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            SourceFileCompiler.Instance.SetImports(this.TextDocumentFactoryService, this.ServiceProvider);

            // Tell the source file compiler about the new view...
            SourceFileCompiler.Instance.AddTextView(textView);
        }
    }

    /// <summary>
    ///  Harvests TypedMessage data from source files.
    /// </summary>
    internal class SourceFileCompiler
    {
        internal const int BackgroundCompileDelay = 1000; // milliseconds to wait before kicking off background-compile.

        private static Lazy<SourceFileCompiler> Singleton = new Lazy<SourceFileCompiler>(() => new SourceFileCompiler(), true);

        public static SourceFileCompiler Instance { get { return Singleton.Value; } }

        public ITextDocumentFactoryService TextDocumentFactoryService { get; private set; }
        public IServiceProvider ServiceProvider { get; private set; }
        public ErrorListProvider ErrorListProvider { get; private set; }

        public void SetImports(ITextDocumentFactoryService textDocumentFactoryService, IServiceProvider serviceProvider)
        {
            if (this.TextDocumentFactoryService == null)
            {
                this.TextDocumentFactoryService = textDocumentFactoryService;
            }

            if (this.ServiceProvider == null)
            {
                this.ServiceProvider = serviceProvider;
            }

            if (this.ErrorListProvider == null)
            {
                this.ErrorListProvider = new ErrorListProvider(this.ServiceProvider);
                this.ErrorListProvider.ProviderGuid = new Guid("A3924CF5-DCFA-457B-B6C7-F4D630DADFA2");
                this.ErrorListProvider.ProviderName = "TypedMessage Errors";
            }
        }

        // list of files we manage
        private List<SourceFileSet> sourceFileSets = new List<SourceFileSet>();

        public void AddTextView(ITextView textView)
        {
            ITextDocument textDocument;
            if (!this.TextDocumentFactoryService.TryGetTextDocument(textView.TextBuffer, out textDocument))
            {
                throw new Exception("Could not find ITextDocument from textView!");
            }

            string fullPath = Path.GetFullPath(textDocument.FilePath);

            SourceFileSet sourceFileSet = this.FindSourceFileSet(fullPath, true);

            sourceFileSet.AttachTextView(fullPath, textView);
            sourceFileSet.CompileAsync();
        }

        // Gets the "context" for a file.
        public MessageData GetCompilationContext(ITextBuffer buffer)
        {
            ITextDocument textDocument;
            if (!this.TextDocumentFactoryService.TryGetTextDocument(buffer, out textDocument))
            {
                throw new Exception("Unable to find ITextDocument for buffer!");
            }

            return this.GetCompilationContext(textDocument.FilePath);
        }

        private MessageData GetCompilationContext(string file)
        {
            string fullPath = Path.GetFullPath(file);

            SourceFileSet sourceFileSet = this.FindSourceFileSet(fullPath, true);

            // Note that we *don't* compile here... we just use the existing items from our background compile.
            return sourceFileSet.CompilationContext;
        }

        // Returns the set of files (full paths) that constitute a compilation context for
        // a given file.
        private IEnumerable<string> GetContextFiles(string file)
        {
            string fullPath = Path.GetFullPath(file);

            // Getting all the files in the directory sort of works, but also breaks some cases.
            // For now, we only do single-file compilation, until we get a real project system
            // that can tell us all of the TypedMessage files.
            yield return fullPath;
        }

        public SourceFileSet FindSourceFileSet(string file)
        {
            return this.FindSourceFileSet(file, false);
        }

        private SourceFileSet FindSourceFileSet(string file, bool createIfMissing)
        {
            // Find the SourceFileSet that has a given file...
            SourceFileSet sourceFileSet = this.sourceFileSets.FirstOrDefault(set => set.HasSourceFile(file));

            if (sourceFileSet == null && createIfMissing)
            {
                // The crazy try/catch/try/finally is to keep StyleCop from thinking we're going to missing
                // calling Dispose on an exception.
                sourceFileSet = new SourceFileSet(this, file);
                try
                {
                    sourceFileSet.AddSourceFiles(this.GetContextFiles(file));
                    this.sourceFileSets.Add(sourceFileSet);

                    sourceFileSet.SetClosed += SourceFileSet_SetClosed;
                    sourceFileSet.CompileComplete += SourceFileSet_CompileComplete;
                }
                catch (Exception)
                {
                    try
                    {
                        sourceFileSet.SetClosed -= SourceFileSet_SetClosed;
                        sourceFileSet.CompileComplete -= SourceFileSet_CompileComplete;
                        this.sourceFileSets.Remove(sourceFileSet);
                    }
                    finally
                    {
                        sourceFileSet.Dispose();
                    }
                }
            }

            return sourceFileSet;
        }

        public SourceFileData FindSourceFileData(string file)
        {
            SourceFileSet sourceFileSet = this.FindSourceFileSet(file);
            if (sourceFileSet != null)
            {
                return sourceFileSet.FindSourceFile(file);
            }
            return null;
        }

        public SourceFileData FindSourceFileData(ITextBuffer textBuffer)
        {
            ITextDocument textDocument;
            if (!this.TextDocumentFactoryService.TryGetTextDocument(textBuffer, out textDocument))
            {
                throw new Exception("Could not find ITextDocument from textView!");
            }

            string fullPath = Path.GetFullPath(textDocument.FilePath);

            return this.FindSourceFileData(fullPath);
        }


        void SourceFileSet_SetClosed(object sender, EventArgs e)
        {
            SourceFileSet sourceFileSet = sender as SourceFileSet;
            sourceFileSet.SetClosed -= SourceFileSet_SetClosed;
            sourceFileSet.CompileComplete -= SourceFileSet_CompileComplete;
            this.sourceFileSets.Remove(sourceFileSet);
            sourceFileSet.Dispose();
            this.UpdateErrorList();
        }

        void SourceFileSet_CompileComplete(object sender, EventArgs e)
        {
            this.UpdateErrorList();
        }

        // This has to live in the compiler, not in the error tagger, because we
        // don't want to update the list multiple times when we get to having more
        // than TypedMessage file in a project opened.  Also, having it here allows for
        // multiple sets to be handled concurrently, which we need since we
        // *don't* have TypedMessage projects yet! (?)
        private void UpdateErrorList()
        {
            SourceFileCompiler.Instance.ErrorListProvider.Tasks.Clear();

            foreach (SourceFileSet sourceFileSet in this.sourceFileSets)
            {
                foreach (CompilerMessage message in sourceFileSet.Messages)
                {
                    SourceFileCompiler.Instance.ErrorListProvider.Tasks.Add(message);
                }
            }
        }

        ////void Task_Navigate(object sender, EventArgs e)
        ////{
        ////    Task task = sender as Task;

        ////    VsShellUtilities.OpenDocument(SourceFileCompiler.Instance.ServiceProvider, task.Document);
        ////    SourceFileData data = SourceFileCompiler.Instance.FindSourceFileData(task.Document);

        ////    if (data != null && data.TextView != null)
        ////    {
        ////        SnapshotSpan span = data.TextView.TextSnapshot.CreateSpan(task.Line, task.Column, task.Column + 1);
        ////        data.TextView.Caret.MoveTo(span.Start, PositionAffinity.Successor);
        ////        data.TextView.ViewScroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.ShowStart);
        ////    }
        ////}
    }

    internal class SourceFileSet : IDisposable
    {
        public SourceFileSet(SourceFileCompiler compiler, string id)
        {
            this.Compiler = compiler;
            this.Id = id;
            this.CompilationContext = new MessageData(null);
        }

        private List<SourceFileData> files = new List<SourceFileData>();

        public bool IsEmpty { get { return !this.files.Any(); } }

        public SourceFileCompiler Compiler { get; private set; }
        public string Id { get; private set; }

        public bool NeedsCompilation
        {
            get
            {
                return this.files.Any(file => file.NeedsCompilation);
            }
        }

        public MessageData CompilationContext { get; private set; }

        private List<CompilerMessage> messages = new List<CompilerMessage>();
        public IEnumerable<CompilerMessage> Messages
        {
            get
            {
                return this.messages;

                // If we want to hide the XAML write errors, use this instead:
                ////CompilerMessage writeError = CompilerMessage.XamlWriteError(string.Empty);
                ////return this.messages.Where(message => message.Message.Id != writeError.Id);
            }
        }

        private System.Threading.Timer compileAsyncTimer;

        public void CompileAsync(bool immediate = false)
        {
            if (this.compileAsyncTimer == null)
            {
                this.compileAsyncTimer = new System.Threading.Timer(
                    this.CompileAsyncTimerCallback,
                    null,
                    System.Threading.Timeout.Infinite,
                    0);
            }

            if (immediate)
            {
                this.CompileAsyncTimerCallback(null);
            }
            else
            {
                // touch the timer to extend the timeout...
                this.compileAsyncTimer.Change(SourceFileCompiler.BackgroundCompileDelay, 0);
            }
        }

        private void CompileAsyncTimerCallback(object state)
        {
            // turn off the timer!
            this.compileAsyncTimer.Change(System.Threading.Timeout.Infinite, 0);
            System.Threading.Tasks.Task.Factory.StartNew(this.Compile);
        }

        object compileLock = new object();

        public void Compile()
        {
#if !PreventCompilation
            if (!this.NeedsCompilation)
            {
                return;
            }

            lock (compileLock)
            {
                List<CompilerMessage> compilerMessages = new List<CompilerMessage>();

                try
                {
                    // Parse all the files in the set, then ... ?
                    foreach (var file in this.files)
                    {
                        file.Parse();
                        compilerMessages.AddRange(file.ParseMessages);
                    }

                    // We happen to know that we *really* only have one file, so we pass its
                    // results through as our own...
                    foreach (var file in this.files)
                    {
                        this.CompilationContext = file.CompilationContext;
                    }
                }
                catch (Exception /*ex*/)
                {
                    // create/add message here?
                }
                finally
                {
                    // update the messages...
                    this.messages = compilerMessages;

                    this.OnCompileComplete();
                }
            }
#endif // PreventCompilation
        }

        public event EventHandler CompileComplete;

        private void OnCompileComplete()
        {
            EventHandler handler = this.CompileComplete;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }


        public void AddSourceFiles(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                SourceFileData data = new SourceFileData(this, file);
                this.files.Add(data);
            }
        }

        public void AttachTextView(string path, ITextView textView)
        {
            SourceFileData data = this.FindSourceFile(path, false);
            data.AttachTextView(textView);
            data.ViewClosed += SourceFileData_ViewClosed;
        }

        void SourceFileData_ViewClosed(object sender, EventArgs e)
        {
            SourceFileData data = sender as SourceFileData;
            data.ViewClosed -= SourceFileData_ViewClosed;

            // If there are no more source files with views, we can kill the entire set.
            if (this.files.All(d => d.TextView == null))
            {
                this.files.Clear();
                ////this.Items.Clear();
                this.messages.Clear();

                this.OnSetClosed();
            }
        }

        public event EventHandler SetClosed;

        private void OnSetClosed()
        {
            EventHandler handler = this.SetClosed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public bool HasSourceFile(string file)
        {
            return (this.FindSourceFile(file, false) != null);
        }

        public SourceFileData FindSourceFile(string file)
        {
            return this.FindSourceFile(file, false);
        }

        private SourceFileData FindSourceFile(string file, bool createIfMissing)
        {
            SourceFileData data = this.files.FirstOrDefault(d => string.Equals(d.Path, file, StringComparison.OrdinalIgnoreCase));

            if (data == null && createIfMissing)
            {
                data = new SourceFileData(this, file);
                this.files.Add(data);
            }

            return data;
        }

        private bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                if (this.compileAsyncTimer != null)
                {
                    this.compileAsyncTimer.Dispose();
                }

                GC.SuppressFinalize(this);
                disposed = true;
            }
        }
    }

    internal class SourceFileData
    {
        private Parser.Parser parser;

        public SourceFileData(SourceFileSet sourceFileSet, string file)
        {
            this.SourceFileSet = sourceFileSet;
            this.Path = file;
            this.TextView = null;
            this.NeedsCompilation = true;

            this.parser = new Parser.Parser(this.Path);
        }

        public SourceFileSet SourceFileSet { get; private set; }

        public string Path { get; private set; }

        public ITextView TextView { get; set; }

        public bool NeedsCompilation { get; private set; }

        public void AttachTextView(ITextView textView)
        {
            if (this.TextView != textView)
            {
                this.TextView = textView;

                this.TextView.Closed += this.TextView_Closed;
                this.TextView.TextBuffer.PostChanged += TextBuffer_PostChanged;
            }
        }

        void TextView_Closed(object sender, EventArgs e)
        {
            this.TextView.Closed -= this.TextView_Closed;
            this.TextView.TextBuffer.PostChanged -= TextBuffer_PostChanged;

            this.TextView = null;

            this.OnViewClosed();
        }

        void TextBuffer_PostChanged(object sender, EventArgs e)
        {
            this.NeedsCompilation = true;
            this.SourceFileSet.CompileAsync();
        }

        public event EventHandler ViewClosed;

        private void OnViewClosed()
        {
            EventHandler handler = this.ViewClosed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void Parse()
        {
            if (this.TextView != null)
            {
                using (TextSnapshotToTextReader reader = new TextSnapshotToTextReader(this.TextView.TextBuffer.CurrentSnapshot))
                {
                    this.parser.Parse(reader);
                }
            }
            ////else
            ////{
            ////    frontend.Parse(this.Path);
            ////}

            this.NeedsCompilation = false;
        }

        public MessageData CompilationContext
        {
            get
            {
                return this.parser.MessageData;
            }
        }

        public IEnumerable<CompilerMessage> ParseMessages
        {
            get
            {
                return this.parser.Errors.Select(p => new CompilerMessage(this.SourceFileSet.Compiler.ServiceProvider, p, this.Path));
            }
        }

        public IEnumerable<CompilerMessage> AllMessages
        {
            get
            {
                return this.SourceFileSet.Messages.Where(message => string.Equals(message.Document, this.Path, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    // TODO: move these somewhere...
    internal static class CompilerExtensions
    {
        internal static Span AsSpan(this Range range)
        {
            return new Span(range.Start.Offset, range.Length);
        }

        internal static SnapshotSpan CreateSpan(this ITextSnapshot snapshot, int lineNumber, int columnStart, int columnEnd)
        {
            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(lineNumber);

            SnapshotSpan span = new SnapshotSpan(
                    line.Start + columnStart,
                    columnEnd - columnStart);

            return span;
        }

        internal static SnapshotSpan CreateSpan(this ITextSnapshot snapshot, Range range)
        {
            SnapshotSpan span = new SnapshotSpan(
                new SnapshotPoint(snapshot, range.Start.Offset),
                new SnapshotPoint(snapshot, range.End.Offset));

            return span;
        }

        internal static SnapshotSpan CreateSpan(this ITextSnapshot snapshot, CompilerMessage message)
        {
            return snapshot.CreateSpan(message.Range);
        }

        internal static SnapshotSpan CreateSpan(this ITextSnapshot snapshot, IRangeProvider rangeProvider)
        {
            return snapshot.CreateSpan(rangeProvider.Range);
        }

        internal static ITrackingSpan CreateTrackingSpan(this ITextSnapshot snapshot, CompilerMessage message, SpanTrackingMode trackingMode = SpanTrackingMode.EdgeExclusive)
        {
            SnapshotSpan span = snapshot.CreateSpan(message);
            return snapshot.CreateTrackingSpan(span, trackingMode);
        }

        internal static ITrackingSpan CreateTrackingSpan(this ITextSnapshot snapshot, IRangeProvider rangeProvider, SpanTrackingMode trackingMode = SpanTrackingMode.EdgeExclusive)
        {
            SnapshotSpan span = snapshot.CreateSpan(rangeProvider);
            return snapshot.CreateTrackingSpan(span, trackingMode);
        }
    }

    internal class RangeErrorTask : ErrorTask, IErrorTag
    {
        private IServiceProvider serviceProvider;

        public RangeErrorTask(IServiceProvider serviceProvider, Error error, string filename = null)
        {
            this.serviceProvider = serviceProvider;

            this.Document = filename ?? error.Filename;
            this.Range = error.Range;
            this.Line = this.Range.Start.Line;
            this.Column = this.Range.Start.Column;
            this.Text = error.Message;

            this.Category = TaskCategory.CodeSense;
            this.ErrorCategory = TaskErrorCategory.Error;

            // TODO: Get IVsHierarchy for project mapping
        }

        public Range Range { get; private set; }

        protected override void OnNavigate(EventArgs e)
        {
            ////base.OnNavigate(e);

            if (string.IsNullOrEmpty(this.Document))
            {
                return;
            }

            // We know the document is open, don't we?  This just forces it to the foreground/activates it...
            // Sadly, we get the old IVsTextView interface instead of the new ITextView one...
            IVsUIHierarchy hierarchy;
            uint item;
            IVsWindowFrame frame;
            Microsoft.VisualStudio.TextManager.Interop.IVsTextView legacyTextView;

            VsShellUtilities.OpenDocument(
                this.serviceProvider,
                this.Document,
                VSConstants.LOGVIEWID.Any_guid,
                out hierarchy,
                out item,
                out frame,
                out legacyTextView);

            var legacySpan = new Microsoft.VisualStudio.TextManager.Interop.TextSpan();
            legacySpan.iStartLine = this.Range.Start.Line;
            legacySpan.iStartIndex = this.Range.Start.Column;
            legacySpan.iEndLine = this.Range.End.Line;
            legacySpan.iEndIndex = this.Range.End.Column;

            legacyTextView.SetCaretPos(legacySpan.iEndLine, legacySpan.iEndIndex);
            legacyTextView.SetSelection(legacySpan.iStartLine, legacySpan.iStartIndex, legacySpan.iEndLine, legacySpan.iEndIndex);
            legacyTextView.EnsureSpanVisible(legacySpan);
        }

        #region IErrorTag Members

        // We don't use "compiler error", "warning", or "???"...
        public string ErrorType
        {
            get { return "syntax error"; }
        }

        public object ToolTipContent
        {
            get { return this.Text; }
        }

        #endregion
    }

    internal class CompilerMessage : RangeErrorTask
    {
        public CompilerMessage(IServiceProvider serviceProvider, Error error, string filename = null)
            : base(serviceProvider, error, filename)
        {
        }

        ////public CompilerMessage(string fileName, Range range, string message)
        ////{
        ////    this.FileName = fileName;
        ////    this.Range = range;
        ////    this.Message = message;
        ////}

        ////public string FileName { get; private set; }
        ////public Range Range { get; private set; }
        ////public string Message { get; private set; }

        ////public TaskCategory TaskCategory
        ////{
        ////    get
        ////    {
        ////        TaskCategory category = TaskCategory.CodeSense;
        ////        ////TaskCategory category = TaskCategory.All;

        ////        ////switch (message.Type)
        ////        ////{
        ////        ////    case CompilerMessage.CompilerMessageType.LexerError:
        ////        ////        category = TaskCategory.CodeSense;
        ////        ////        break;
        ////        ////    case CompilerMessage.CompilerMessageType.Error:
        ////        ////    case CompilerMessage.CompilerMessageType.Warning:
        ////        ////    case CompilerMessage.CompilerMessageType.Information:
        ////        ////    case CompilerMessage.CompilerMessageType.Verbose:
        ////        ////        category = TaskCategory.BuildCompile;
        ////        ////        break;
        ////        ////}

        ////        return category;
        ////    }
        ////}

        ////public TaskErrorCategory TaskErrorCategory
        ////{
        ////    get
        ////    {
        ////        TaskErrorCategory category = TaskErrorCategory.Error;
        ////        ////TaskErrorCategory category = TaskErrorCategory.Message;

        ////        ////switch (message.Type)
        ////        ////{
        ////        ////    case CompilerMessage.CompilerMessageType.LexerError:
        ////        ////    case CompilerMessage.CompilerMessageType.Error:
        ////        ////    default:
        ////        ////        category = TaskErrorCategory.Error;
        ////        ////        break;

        ////        ////    case CompilerMessage.CompilerMessageType.Warning:
        ////        ////        category = TaskErrorCategory.Warning;
        ////        ////        break;

        ////        ////    case CompilerMessage.CompilerMessageType.Information:
        ////        ////    case CompilerMessage.CompilerMessageType.Verbose:
        ////        ////        category = TaskErrorCategory.Message;
        ////        ////        break;
        ////        ////}

        ////        return category;
        ////    }
        ////}

        ////public ErrorTask ToErrorTask()
        ////{
        ////    ErrorTask task = new ErrorTask();
        ////    task.Document = this.FileName;
        ////    task.Line = this.Range.Start.Line;
        ////    task.Column = this.Range.Start.Column;
        ////    task.Text = this.Message;

        ////    // inline?
        ////    task.Category = this.TaskCategory;
        ////    task.ErrorCategory = this.TaskErrorCategory;

        ////    return task;
        ////}
    }
}
