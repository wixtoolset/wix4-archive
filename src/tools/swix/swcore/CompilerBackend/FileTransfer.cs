// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerBackend
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class FileTransfer
    {
        /// <summary>
        /// Use Create method.
        /// </summary>
        private FileTransfer()
        {
        }

        /// <summary>Source path to file.</summary>
        public string Source { get; private set; }

        /// <summary>Destination path for file.</summary>
        public string Destination { get; private set; }

        /// <summary>Flag if file should be moved (optimal).</summary>
        public bool Move { get; private set; }

        /// <summary>Optional source line number where the file transfer orginated.</summary>
        public FileLineNumber LineNumber { get; private set; }

        /// <summary>Optional type of file this transfer is moving or copying.</summary>
        public string Type { get; private set; }

        /// <summary>Flag if file transfer is redundant (copy on top of itself).</summary>
        public bool Redundant { get; private set; }

        /// <summary>
        /// Creates a file transfer object.
        /// </summary>
        /// <param name="lineNumber">Optional line number for item that required the file transfer.</param>
        /// <param name="source">Source path.</param>
        /// <param name="destination">Destination path.</param>
        /// <param name="type">Optional type of file trnasfer.</param>
        /// <param name="move">Flag if file should be moved or copied</param>
        /// <returns>File transfer object.</returns>
        public static FileTransfer Create(FileLineNumber lineNumber, string source, string destination, string type, bool move)
        {
            FileTransfer transfer = new FileTransfer();
            transfer.LineNumber = lineNumber;
            transfer.Type = type;
            transfer.Move = move;

            try
            {
                transfer.Source = Path.GetFullPath(source);
            }
            catch (ArgumentException)
            {
                throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.InvalidFileName(source), lineNumber));
            }
            catch (PathTooLongException)
            {
                throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.PathTooLong(source), lineNumber));
            }

            try
            {
                transfer.Destination = Path.GetFullPath(destination);
            }
            catch (ArgumentException)
            {
                throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.InvalidFileName(destination), lineNumber));
            }
            catch (PathTooLongException)
            {
                throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.PathTooLong(destination), lineNumber));
            }

            transfer.Redundant = String.Equals(transfer.Source, transfer.Destination, StringComparison.OrdinalIgnoreCase);
            return transfer;
        }

        /// <summary>
        /// Copy or move a single transfer.
        /// </summary>
        /// <param name="backend">Compiler backend that generated the transfers.</param>
        /// <param name="transfer">File transfers to execute.</param>
        public static void ExecuteTransfer(BackendCompiler backend, FileTransfer transfer)
        {
            if (transfer.Redundant)
            {
                // TODO: log that we tried to transfer a redundant file?
                return;
            }

            bool retry = false;
            do
            {
                try
                {
                    if (transfer.Move)
                    {
                        backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.MoveFile(transfer.Source, transfer.Destination), transfer.LineNumber));
                        File.Move(transfer.Source, transfer.Destination);
                    }
                    else
                    {
                        backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.CopyFile(transfer.Source, transfer.Destination), transfer.LineNumber));
                        File.Copy(transfer.Source, transfer.Destination, true);
                    }

                    retry = false;
                }
                catch (FileNotFoundException e)
                {
                    throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.FileNotFound(transfer.Source), transfer.LineNumber), e);
                }
                catch (DirectoryNotFoundException dnfe)
                {
                    // if we already retried, give up
                    if (retry)
                    {
                        throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.CannotTransferFile(transfer.Source, transfer.Destination), transfer.LineNumber), dnfe);
                    }

                    string directory = Path.GetDirectoryName(transfer.Destination);
                    Directory.CreateDirectory(directory);
                    retry = true;
                }
                catch (UnauthorizedAccessException uae)
                {
                    // if we already retried, give up
                    if (retry)
                    {
                        throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.CannotTransferFile(transfer.Source, transfer.Destination), transfer.LineNumber), uae);
                    }

                    FileTransfer.RemoveDestination(backend, transfer);
                    retry = true;
                }
                catch (IOException e)
                {
                    // if we already retried, give up
                    if (retry)
                    {
                        throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.CannotTransferFile(transfer.Source, transfer.Destination), transfer.LineNumber), e);
                    }

                    FileTransfer.RemoveDestination(backend, transfer);
                    retry = true;
                }
            } while (retry);
        }

        /// <summary>
        /// Process a set of transfers.
        /// </summary>
        /// <param name="backend">Compiler backend that generated the transfers.</param>
        /// <param name="transfers">Set of file transfers to execute.</param>
        public static void ExecuteTransfers(BackendCompiler backend, IEnumerable<FileTransfer> transfers)
        {
            foreach (FileTransfer transfer in transfers)
            {
                FileTransfer.ExecuteTransfer(backend, transfer);
            }
        }

        /// <summary>
        /// Removes the destination for a file transfer.
        /// </summary>
        /// <param name="backend">Compiler backend that generated the transfer.</param>
        /// <param name="transfers">File transfers destination to remove.</param>
        private static void RemoveDestination(BackendCompiler backend, FileTransfer transfer)
        {
            if (File.Exists(transfer.Destination))
            {
                backend.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RemovingDestinationFile(transfer.Destination), transfer.LineNumber));

                // try to ensure the file is not read-only
                FileAttributes attributes = File.GetAttributes(transfer.Destination);
                try
                {
                    File.SetAttributes(transfer.Destination, attributes & ~FileAttributes.ReadOnly);
                }
                catch (ArgumentException ae) // thrown for unauthorized access errors
                {
                    throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.CannotTransferFile(transfer.Source, transfer.Destination), transfer.LineNumber), ae);
                }

                // try to delete the file
                try
                {
                    File.Delete(transfer.Destination);
                }
                catch (IOException ioe)
                {
                    throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.CannotTransferFile(transfer.Source, transfer.Destination), transfer.LineNumber), ioe);
                }
            }
            else // no idea what just happened, bail
            {
                //throw;
            }
        }
    }
}
