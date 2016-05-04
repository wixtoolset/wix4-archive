// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace WixToolset.Simplified.LanguageService
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("Swix Command Filter")]
    [ContentType(TypeConstants.Content)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class CommandFilterProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
            {
                return;
            }

            textView.Properties.GetOrCreateSingletonProperty(() => new CommandFilter(textViewAdapter, textView, this));
        }
    }

    // Provides command support for Swix ITextViews, including commenting, and Intellisense hooks.
    internal class CommandFilter : IOleCommandTarget, IDisposable
    {
        private IOleCommandTarget nextCommandHandler;
        private IVsTextView textViewAdapter;
        private ITextView textView;
        private CommandFilterProvider provider;
        private ICompletionSession session;
        private Dictionary<CommandID, CommandHandler> commandHandlers = new Dictionary<CommandID, CommandHandler>();

        internal CommandFilter(IVsTextView textViewAdapter, ITextView textView, CommandFilterProvider provider)
        {
            this.textViewAdapter = textViewAdapter;
            this.textView = textView;
            this.provider = provider;

            // wire up our command handlers...
            CommandHandler[] handlers = new CommandHandler[] {
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.COMMENTBLOCK, this.CommentSelection),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK, this.UncommentSelection),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.COMMENT_BLOCK, this.CommentSelection),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK, this.UncommentSelection),

                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.COMPLETEWORD, this.AutoComplete),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.SHOWMEMBERLIST, this.AutoComplete),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.AUTOCOMPLETE, this.AutoComplete),

                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.TYPECHAR, this.HandleTyping),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.RETURN, this.HandleTyping),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.TAB, this.HandleTyping),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.BACKSPACE, this.HandleTyping),
                new CommandHandler(VSConstants.VSStd2K, (int)VSConstants.VSStd2KCmdID.DELETE, this.HandleTyping),
            
            };

            this.commandHandlers = handlers.ToDictionary(h => h.Command);

            // add the command to the command chain
            this.textViewAdapter.AddCommandFilter(this, out this.nextCommandHandler);
        }

        private bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                this.textViewAdapter.RemoveCommandFilter(this);
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }

        public int QueryStatus(ref Guid commandGroup, uint commandCount, OLECMD[] commands, IntPtr commandText)
        {
            // Get the default chained status.
            int ret = this.nextCommandHandler.QueryStatus(ref commandGroup, commandCount, commands, commandText);

            // See if we specifically support the command.
            for (uint cmdIndex = 0; cmdIndex < commandCount; ++cmdIndex)
            {
                CommandID command = new CommandID(commandGroup, (int)commands[cmdIndex].cmdID);
                CommandHandler handler;
                if (this.commandHandlers.TryGetValue(command, out handler))
                {
                    if (handler.QueryStatus != null)
                    {
                        commands[cmdIndex].cmdf = (uint)handler.QueryStatus(command);
                        // TODO: possibly change return value if we need to?
                    }
                    else
                    {
                        // We default to supported and enabled if no QueryStatus handler is provided.
                        commands[cmdIndex].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_ENABLED;
                    }
                }
            }

            return ret;
        }

        public int Exec(ref Guid commandGroup, uint commandId, uint commandOptions, IntPtr pvaIn, IntPtr pvaOut)
        {
            CommandID command = new CommandID(commandGroup, (int)commandId);
            CommandHandler handler;
            if (this.commandHandlers.TryGetValue(command, out handler))
            {
                // Do we need to double-check the status?  We shouldn't need to!
                if (handler.Invoke != null)
                {
                    return handler.Invoke(command, commandOptions, pvaIn, pvaOut);
                }
            }

            // If we didn't handle it, pass the command along...
            return this.nextCommandHandler.Exec(ref commandGroup, commandId, commandOptions, pvaIn, pvaOut);
        }

        private int PassThrough(CommandID command, uint options, IntPtr pvIn, IntPtr pvOut)
        {
            return this.nextCommandHandler.Exec(command.Guid, (uint)command.ID, options, pvIn, pvOut);
        }

        private int CommentSelection(CommandID command, uint options, IntPtr pvIn, IntPtr pvOut)
        {
            // Add the comment character to every line in the range...
            ITextEdit edit = this.textView.TextBuffer.CreateEdit();
            int start = edit.Snapshot.GetLineNumberFromPosition(this.textView.Selection.Start.Position);
            int end = edit.Snapshot.GetLineNumberFromPosition(this.textView.Selection.End.Position);

            List<int> linesToSkip = new List<int>();
            int smallestWhitespace = int.MaxValue;

            foreach (ITextSnapshotLine line in edit.Snapshot.Lines.Where(l => l.LineNumber >= start && l.LineNumber <= end))
            {
                string text = line.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    int leadingWhitespace = text.TakeWhile(c => char.IsWhiteSpace(c)).Count();
                    smallestWhitespace = Math.Min(smallestWhitespace, leadingWhitespace);
                }
                else
                {
                    linesToSkip.Add(line.LineNumber);
                }
            }

            foreach (ITextSnapshotLine line in edit.Snapshot.Lines.Where(l => l.LineNumber >= start && l.LineNumber <= end))
            {
                if (!linesToSkip.Contains(line.LineNumber))
                {
                    edit.Insert(line.Start + smallestWhitespace, "#");
                }
            }

            edit.Apply();

            return VSConstants.S_OK;
        }

        private int UncommentSelection(CommandID command, uint options, IntPtr pvIn, IntPtr pvOut)
        {
            // Remove the first leading comment character from every line in the range...
            ITextEdit edit = this.textView.TextBuffer.CreateEdit();
            int start = edit.Snapshot.GetLineNumberFromPosition(this.textView.Selection.Start.Position);
            int end = edit.Snapshot.GetLineNumberFromPosition(this.textView.Selection.End.Position);

            foreach (ITextSnapshotLine line in edit.Snapshot.Lines.Where(l => l.LineNumber >= start && l.LineNumber <= end))
            {
                string text = line.GetText();
                int leadingWhitespace = text.TakeWhile(c => char.IsWhiteSpace(c)).Count();
                if ((text.Length > leadingWhitespace) && (text[leadingWhitespace] == '#'))
                {
                    edit.Delete(line.Start + leadingWhitespace, 1);
                }
            }

            edit.Apply();

            return VSConstants.S_OK;
        }

        private int AutoComplete(CommandID command, uint options, IntPtr pvIn, IntPtr pvOut)
        {
            // If there is no active session, bring up completion
            if (this.session == null || this.session.IsDismissed)
            {
                this.TriggerCompletion();
            }

            return VSConstants.S_OK;
        }

        private int HandleTyping(CommandID command, uint options, IntPtr pvIn, IntPtr pvOut)
        {
            // If we're in an automation function, *don't* trigger anything as a result of typing!
            if (VsShellUtilities.IsInAutomationFunction(this.provider.ServiceProvider))
            {
                return this.PassThrough(command, options, pvIn, pvOut);
            }

            System.Diagnostics.Debug.Assert(command.Guid == VSConstants.VSStd2K);
            System.Diagnostics.Debug.Assert(command.ID == (int)VSConstants.VSStd2KCmdID.TYPECHAR ||
                                            command.ID == (int)VSConstants.VSStd2KCmdID.RETURN ||
                                            command.ID == (int)VSConstants.VSStd2KCmdID.TAB ||
                                            command.ID == (int)VSConstants.VSStd2KCmdID.BACKSPACE ||
                                            command.ID == (int)VSConstants.VSStd2KCmdID.DELETE);

            VSConstants.VSStd2KCmdID commandId = (VSConstants.VSStd2KCmdID)command.ID;
            char typedChar = char.MinValue;

            // Make sure the input is a typed character before getting it.
            if (commandId == VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvIn);
            }

            // Check for a commit character, and possibly commit a selection.
            if (commandId == VSConstants.VSStd2KCmdID.RETURN ||
                commandId == VSConstants.VSStd2KCmdID.TAB ||
                (commandId == VSConstants.VSStd2KCmdID.TYPECHAR && !CompletionSource.IsTokenTermCharacter(typedChar)))
            {
                if (this.session != null && !this.session.IsDismissed)
                {
                    // if the selection is fully selected, commit the current session
                    if (this.session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        ITrackingSpan applicableSpan = this.session.SelectedCompletionSet.ApplicableTo;
                        this.session.Commit();

                        // If we ended with whitespace or '=', trigger another session!
                        char ch = (applicableSpan.GetEndPoint(applicableSpan.TextBuffer.CurrentSnapshot) - 1).GetChar();
                        if (char.IsWhiteSpace(ch) || ch == '=')
                        {
                            this.TriggerCompletion();
                        }

                        return VSConstants.S_OK;    // don't add the commit character to the buffer
                    }
                    else
                    {
                        // if there is no selection, dismiss the session
                        this.session.Dismiss();
                    }
                }
            }

            // pass the command on through to the buffer to allow typing...
            int ret = this.PassThrough(command, options, pvIn, pvOut);

            if (commandId == VSConstants.VSStd2KCmdID.TYPECHAR && !typedChar.Equals(char.MinValue))
            {
                // If there is no active session, bring up completion
                if (this.session == null || this.session.IsDismissed)
                {
                    this.TriggerCompletion();
                }

                if (this.session != null && !this.session.IsDismissed)
                {
                    // the completion session is already active, so just filter
                    this.session.Filter();
                }
            }
            // else if BACKSPACE or DELETE, redo the filter...
            else if (commandId == VSConstants.VSStd2KCmdID.BACKSPACE ||
                    commandId == VSConstants.VSStd2KCmdID.DELETE)
            {
                if (this.session != null && !this.session.IsDismissed)
                {
                    this.session.Filter();
                }
            }

            return ret;
        }

        private bool TriggerCompletion()
        {
            // the caret must be in a non-projection location 
            SnapshotPoint? caretPoint = this.textView.Caret.Position.Point.GetPoint(
                textBuffer => (!textBuffer.ContentType.IsOfType("projection")),
                PositionAffinity.Predecessor);

            if (!caretPoint.HasValue)
            {
                return false;
            }

            // Don't trigger completion if we're in a comment!
            // We use a cheap version of this, rather than checking classification spans.
            ITextSnapshotLine line = caretPoint.Value.GetContainingLine();
            int commentIndex = line.GetText().IndexOf('#');
            if (commentIndex >= 0 && caretPoint.Value.Position > line.Start + commentIndex)
            {
                return false;
            }

            this.session = this.provider.CompletionBroker.CreateCompletionSession(
                this.textView,
                caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
                true);

            //subscribe to the Dismissed event on the session 
            this.session.Dismissed += this.OnSessionDismissed;
            this.session.Start();

            return true;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            this.session.Dismissed -= this.OnSessionDismissed;
            this.session = null;
        }
    }

    internal class CommandHandler
    {
        public CommandHandler(
            Guid commandGroup,
            int commandId,
            Func<CommandID, uint, IntPtr, IntPtr, int> invoke)
            : this(new CommandID(commandGroup, commandId), null, invoke)
        {
        }

        public CommandHandler(
            Guid commandGroup,
            int commandId,
            Func<CommandID, OLECMDF> queryStatus,
            Func<CommandID, uint, IntPtr, IntPtr, int> invoke)
            : this(new CommandID(commandGroup, commandId), queryStatus, invoke)
        {
        }

        public CommandHandler(
            CommandID command,
            Func<CommandID, OLECMDF> queryStatus,
            Func<CommandID, uint, IntPtr, IntPtr, int> invoke)
        {
            this.Command = command;
            this.QueryStatus = queryStatus;
            this.Invoke = invoke;
        }

        public CommandID Command { get; private set; }
        public Func<CommandID, OLECMDF> QueryStatus { get; private set; }
        public Func<CommandID, uint, IntPtr, IntPtr, int> Invoke { get; private set; }
    }
}
