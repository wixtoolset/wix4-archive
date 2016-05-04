// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using WixToolset.Simplified.CompilerFrontend;
using WixToolset.Simplified.Lexicon;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using WixToolset.Simplified.ParserCore;

namespace WixToolset.Simplified.LanguageService
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [Name("Swix Compiler")]
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
    ///  Harvests Swix data from source files.
    /// </summary>
    internal class SourceFileCompiler
    {
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
                this.ErrorListProvider.ProviderGuid = new Guid("280CD10E-529B-4234-B4A1-F21CB74BEAFD");
                this.ErrorListProvider.ProviderName = "Swix Errors";
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
        public IEnumerable<PackageItem> GetCompilationContext(ITextBuffer buffer)
        {
            ITextDocument textDocument;
            if (!this.TextDocumentFactoryService.TryGetTextDocument(buffer, out textDocument))
            {
                throw new Exception("Unable to find ITextDocument for buffer!");
            }

            return this.GetCompilationContext(textDocument.FilePath);
        }

        private IEnumerable<PackageItem> GetCompilationContext(string file)
        {
            string fullPath = Path.GetFullPath(file);

            SourceFileSet sourceFileSet = this.FindSourceFileSet(fullPath, true);

            // Note that we *don't* compile here... we just use the existing items from our background compile.
            return sourceFileSet.Items;
        }

        // Returns the set of files (full paths) that constitute a compilation context for
        // a given file.
        private IEnumerable<string> GetContextFiles(string file)
        {
            string fullPath = Path.GetFullPath(file);

            // Getting all the files in the directory sort of works, but also breaks some cases.
            // For now, we only do single-file compilation, until we get a real project system
            // that can tell us all of the Swix files.
            yield return fullPath;

            // This would return all .swr files in the same directory.
            ////foreach (string sourceFile in Directory.EnumerateFiles(Path.GetDirectoryName(fullPath), Swix.RtypeExtension, SearchOption.TopDirectoryOnly))
            ////{
            ////    yield return sourceFile;
            ////}
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
                sourceFileSet = new SourceFileSet(file);
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
        // than one file in a Swix project opened.  Also, having it here allows for
        // multiple sets to be handled concurrently, which we need since we
        // *don't* have Swix projects yet!
        private void UpdateErrorList()
        {
            SourceFileCompiler.Instance.ErrorListProvider.Tasks.Clear();

            foreach (SourceFileSet sourceFileSet in this.sourceFileSets)
            {
                foreach (CompilerMessageEventArgs message in sourceFileSet.Messages)
                {
                    ErrorTask task = new ErrorTask();
                    task.Category = TaskCategoryFromCompilerMessage(message.Message);
                    task.Document = message.FileName;
                    task.Line = message.LineNumber - 1;
                    task.Column = message.LinePosition - 1;
                    task.ErrorCategory = TaskErrorCategoryFromCompilerMessage(message.Message);
                    task.Text = message.Message.Message;

                    if (!string.IsNullOrWhiteSpace(message.FileName))
                    {
                        ////CompilerMessageEventArgs messageCurried = message;
                        task.Navigate += Task_Navigate;
                    }

                    SourceFileCompiler.Instance.ErrorListProvider.Tasks.Add(task);
                }
            }
        }

        void Task_Navigate(object sender, EventArgs e)
        {
            Task task = sender as Task;

            VsShellUtilities.OpenDocument(SourceFileCompiler.Instance.ServiceProvider, task.Document);
            SourceFileData data = SourceFileCompiler.Instance.FindSourceFileData(task.Document);

            if (data != null && data.TextView != null)
            {
                SnapshotSpan span = data.TextView.TextSnapshot.CreateSpan(task.Line, task.Column, task.Column + 1);
                data.TextView.Caret.MoveTo(span.Start, PositionAffinity.Successor);
                data.TextView.ViewScroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.ShowStart);
            }
        }

        private TaskCategory TaskCategoryFromCompilerMessage(CompilerMessage message)
        {
            TaskCategory category = TaskCategory.All;

            switch (message.Type)
            {
                case CompilerMessage.CompilerMessageType.LexerError:
                    category = TaskCategory.CodeSense;
                    break;
                case CompilerMessage.CompilerMessageType.Error:
                case CompilerMessage.CompilerMessageType.Warning:
                case CompilerMessage.CompilerMessageType.Information:
                case CompilerMessage.CompilerMessageType.Verbose:
                    category = TaskCategory.BuildCompile;
                    break;
            }

            return category;
        }

        private TaskErrorCategory TaskErrorCategoryFromCompilerMessage(CompilerMessage message)
        {
            TaskErrorCategory category = TaskErrorCategory.Message;

            switch (message.Type)
            {
                case CompilerMessage.CompilerMessageType.LexerError:
                case CompilerMessage.CompilerMessageType.Error:
                default:
                    category = TaskErrorCategory.Error;
                    break;

                case CompilerMessage.CompilerMessageType.Warning:
                    category = TaskErrorCategory.Warning;
                    break;

                case CompilerMessage.CompilerMessageType.Information:
                case CompilerMessage.CompilerMessageType.Verbose:
                    category = TaskErrorCategory.Message;
                    break;
            }

            return category;
        }
    }

    internal class SourceFileSet : IDisposable
    {
        public SourceFileSet(string id)
        {
            this.Id = id;
            this.Items = new List<PackageItem>();
        }

        private List<SourceFileData> files = new List<SourceFileData>();

        public bool IsEmpty { get { return !this.files.Any(); } }

        public string Id { get; private set; }

        public bool NeedsCompilation
        {
            get
            {
                return this.files.Any(file => file.NeedsCompilation);
            }
        }

        public List<PackageItem> Items { get; private set; }

        private List<CompilerMessageEventArgs> messages = new List<CompilerMessageEventArgs>();
        public IEnumerable<CompilerMessageEventArgs> Messages
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
                this.compileAsyncTimer.Change(500, 0);
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
            if (!this.NeedsCompilation)
            {
                return;
            }

            lock (compileLock)
            {
                FrontendCompiler frontend = new FrontendCompiler(PackageArchitecture.Unknown, null);

                List<CompilerMessageEventArgs> compilerMessages = new List<CompilerMessageEventArgs>();
                frontend.Messages += (s, e) => { compilerMessages.Add(e); };

                try
                {
                    // Parse all the files in the set, then resolve and harvest them.
                    this.files.ForEach(file =>
                    {
                        file.Parse(frontend);
                    });
                    frontend.Resolve();
                    frontend.Harvest();
                }
                catch (Exception ex)
                {
                    // create/add message here?
                    System.Diagnostics.Debug.WriteLine("compiler exception: {0}", ex.Message);
                }
                finally
                {
                    // update the messages...
                    this.messages = compilerMessages;

                    // GetIntermediates() returns an array of IEnumerable...
                    // we just want to flatten that to a single list.
                    var newItems = frontend.GetIntermediates().SelectMany(i => i.Items);
                    this.Items = new List<PackageItem>(newItems);

                    this.OnCompileComplete();
                }
            }
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
                this.Items.Clear();
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
        public SourceFileData(SourceFileSet sourceFileSet, string file)
        {
            this.SourceFileSet = sourceFileSet;
            this.Path = file;
            this.TextView = null;
            this.NeedsCompilation = true;
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

        public void Parse(FrontendCompiler frontend)
        {
            if (this.TextView != null)
            {
                using (TextSnapshotToTextReader reader = new TextSnapshotToTextReader(this.TextView.TextBuffer.CurrentSnapshot))
                {
                    frontend.Parse(reader, this.Path);
                }
            }
            else
            {
                frontend.Parse(this.Path);
            }

            this.NeedsCompilation = false;
        }

        public IEnumerable<CompilerMessageEventArgs> Messages
        {
            get
            {
                return this.SourceFileSet.Messages.Where(message => string.Equals(message.FileName, this.Path, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    internal static class CompilerExtensions
    {
        internal static SnapshotSpan CreateSpan(this ITextSnapshot snapshot, int lineNumber, int columnStart, int columnEnd)
        {
            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(lineNumber);

            SnapshotSpan span = new SnapshotSpan(
                    line.Start + columnStart,
                    columnEnd - columnStart);

            return span;
        }

        internal static SnapshotSpan CreateSpanFromSwix(this ITextSnapshot snapshot, int swixLine, int swixColumnStart, int? swixColumnEnd)
        {
            int lineNumber = swixLine;
            int columnStart = swixColumnStart;
            int columnEnd;

            if (swixColumnEnd.HasValue)
            {
                columnEnd = swixColumnEnd.Value;
            }
            else
            {
                // If there's no end value, we have to check for column -1 and end-of-file conditions.
                bool setColumnStart = false;
                if (columnStart == -1 || lineNumber >= snapshot.LineCount)
                {
                    lineNumber = Math.Min(lineNumber - 1, snapshot.LineCount - 1);
                    setColumnStart = true;
                }

                ITextSnapshotLine line = snapshot.GetLineFromLineNumber(lineNumber);

                if (setColumnStart)
                {
                    columnStart = line.Length - 1;

                }
                columnEnd = line.Length;
            }

            return snapshot.CreateSpan(lineNumber, columnStart, columnEnd);
        }

        internal static SnapshotSpan CreateSpanFromSwix(this ITextSnapshot snapshot, Position start, Position end)
        {
            return new SnapshotSpan(snapshot, start.Offset, end.Offset - start.Offset);
        }

        internal static SnapshotSpan CreateSpanFromSwix(this ITextSnapshot snapshot, Range range)
        {
            return new SnapshotSpan(snapshot, range.Start.Offset, range.Length);
        }

        internal static SnapshotSpan ToSnapshotSpan(this Range range, ITextSnapshot snapshot)
        {
            return snapshot.CreateSpanFromSwix(range);
        }

        internal static SnapshotSpan CreateSpanFromSwix(this ITextSnapshot snapshot, CompilerMessageEventArgs message)
        {
            return snapshot.CreateSpanFromSwix(message.LineNumber, message.LinePosition, message.LinePositionEnd);
        }

        internal static ITrackingSpan CreateTrackingSpanFromSwix(this ITextSnapshot snapshot, CompilerMessageEventArgs message, SpanTrackingMode trackingMode = SpanTrackingMode.EdgeExclusive)
        {
            SnapshotSpan span = snapshot.CreateSpanFromSwix(message);
            return snapshot.CreateTrackingSpan(span, trackingMode);
        }
    }
}
