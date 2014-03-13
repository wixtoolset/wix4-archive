//-------------------------------------------------------------------------------------------------
// <copyright file="BinderFileManager.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The base binder file manager.  Any of these methods can be overridden to change
// the behavior of the binder.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.IO;
    using System.Runtime.InteropServices;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Extensibility;

    /// <summary>
    /// Base class for creating a binder file manager.
    /// </summary>
    public class BinderFileManager : IBinderFileManager
    {
        /// <summary>
        /// Gets or sets the file manager core.
        /// </summary>
        public IBinderFileManagerCore Core { get; set; }

        /// <summary>
        /// Compares two files to determine if they are equivalent.
        /// </summary>
        /// <param name="targetFile">The target file.</param>
        /// <param name="updatedFile">The updated file.</param>
        /// <returns>true if the files are equal; false otherwise.</returns>
        public virtual bool? CompareFiles(string targetFile, string updatedFile)
        {
            FileInfo targetFileInfo = new FileInfo(targetFile);
            FileInfo updatedFileInfo = new FileInfo(updatedFile);

            if (targetFileInfo.Length != updatedFileInfo.Length)
            {
                return false;
            }

            using (FileStream targetStream = File.OpenRead(targetFile))
            {
                using (FileStream updatedStream = File.OpenRead(updatedFile))
                {
                    if (targetStream.Length != updatedStream.Length)
                    {
                        return false;
                    }

                    // Using a larger buffer than the default buffer of 4 * 1024 used by FileStream.ReadByte improves performance.
                    // The buffer size is based on user feedback. Based on performance results, a better buffer size may be determined.
                    byte[] targetBuffer = new byte[16 * 1024];
                    byte[] updatedBuffer = new byte[16 * 1024];
                    int targetReadLength;
                    int updatedReadLength;
                    do
                    {
                        targetReadLength = targetStream.Read(targetBuffer, 0, targetBuffer.Length);
                        updatedReadLength = updatedStream.Read(updatedBuffer, 0, updatedBuffer.Length);
                        
                        if (targetReadLength != updatedReadLength)
                        {
                            return false;
                        }

                        for (int i = 0; i < targetReadLength; ++i)
                        {
                            if (targetBuffer[i] != updatedBuffer[i])
                            {
                                return false;
                            }
                        }

                    } while (0 < targetReadLength);
                }
            }

            return true;
        }

        /// <summary>
        /// Resolves the source path of a file.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public virtual string ResolveFile(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            if (String.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            if (BinderFileManager.CheckFileExists(source)) // if the file exists, we're good to go.
            {
                return source;
            }
            else if (Path.IsPathRooted(source)) // path is rooted so bindpaths won't help, bail since the file apparently doesn't exist.
            {
                return null;
            }
            else // not a rooted path so let's try applying all the different source resolution options.
            {
                const string bindPathOpenString = "!(bindpath.";

                string bindName = String.Empty;
                string path = source;
                string pathWithoutSourceDir = null;

                if (source.StartsWith(bindPathOpenString, StringComparison.Ordinal))
                {
                    int closeParen = source.IndexOf(')', bindPathOpenString.Length);
                    if (-1 != closeParen)
                    {
                        bindName = source.Substring(bindPathOpenString.Length, closeParen - bindPathOpenString.Length);
                        path = source.Substring(bindPathOpenString.Length + bindName.Length + 1); // +1 for the closing brace.
                        path = path.TrimStart('\\'); // remove starting '\\' char so the path doesn't look rooted.
                    }
                }
                else if (source.StartsWith("SourceDir\\", StringComparison.Ordinal) || source.StartsWith("SourceDir/", StringComparison.Ordinal))
                {
                    pathWithoutSourceDir = path.Substring(10);
                }

                var bindPaths = this.Core.GetBindPaths(bindStage, bindName);
                foreach (string bindPath in bindPaths)
                {
                    string filePath;
                    if (!String.IsNullOrEmpty(pathWithoutSourceDir))
                    {
                        filePath = Path.Combine(bindPath, pathWithoutSourceDir);
                        if (BinderFileManager.CheckFileExists(filePath))
                        {
                            return filePath;
                        }
                    }

                    filePath = Path.Combine(bindPath, path);
                    if (BinderFileManager.CheckFileExists(filePath))
                    {
                        return filePath;
                    }
                }
            }

            // Didn't find the file.
            return null;
        }

        /// <summary>
        /// Resolves the source path of a file related to another file's source.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="relatedSource">Source related to original source.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public virtual string ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage)
        {
            string resolvedSource = this.ResolveFile(source, type, sourceLineNumbers, bindStage);
            return Path.Combine(Path.GetDirectoryName(resolvedSource), relatedSource);
        }

        /// <summary>
        /// Resolves the source path of a cabinet file.
        /// </summary>
        /// <param name="cabinetPath">Default path to cabinet to generate.</param>
        /// <param name="fileRows">Collection of files in this cabinet.</param>
        /// <returns>The CabinetBuildOption and path to build the .  By default the cabinet is built and moved to its target location.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public virtual ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<FileRow> fileRows)
        {
            if (null == fileRows)
            {
                throw new ArgumentNullException("fileRows");
            }

            // By default cabinet should be built and moved to the suggested location.
            ResolvedCabinet resolved = new ResolvedCabinet() { BuildOption = CabinetBuildOption.BuildAndMove, Path = cabinetPath };

            // No special behavior specified, use the default.
            if (null == this.Core.CabCachePath && !this.Core.ReuseCabinets)
            {
                return resolved;
            }

            // If a cabinet cache path was provided, change the location for the cabinet
            // to be built to.
            if (null != this.Core.CabCachePath)
            {
                string cabinetName = Path.GetFileName(cabinetPath);
                resolved.Path = Path.Combine(this.Core.CabCachePath, cabinetName);
            }

            // If we still think we're going to reuse the cabinet check to see if the cabinet exists first.
            if (this.Core.ReuseCabinets)
            {
                bool cabinetValid = false;

                if (BinderFileManager.CheckFileExists(resolved.Path))
                {
                    // check to see if
                    // 1. any files are added or removed
                    // 2. order of files changed or names changed
                    // 3. modified time changed
                    cabinetValid = true;

                    // Need to force garbage collection of WixEnumerateCab to ensure the handle
                    // associated with it is closed before it is reused.
                    using (Cab.WixEnumerateCab wixEnumerateCab = new Cab.WixEnumerateCab())
                    {
                        ArrayList fileList = wixEnumerateCab.Enumerate(resolved.Path);

                        if (fileRows.Count() != fileList.Count)
                        {
                            cabinetValid = false;
                        }
                        else
                        {
                            int i = 0;
                            foreach (FileRow fileRow in fileRows)
                            {
                                // First check that the file identifiers match because that is quick and easy.
                                CabinetFileInfo cabFileInfo = fileList[i] as CabinetFileInfo;
                                cabinetValid = (cabFileInfo.FileId == fileRow.File);
                                if (cabinetValid)
                                {
                                    // Still valid so ensure the source time stamp hasn't changed. Thus we need
                                    // to convert the source file time stamp into a cabinet compatible data/time.
                                    DateTime sourceFileTime = File.GetLastWriteTime(fileRow.Source);
                                    ushort sourceCabDate;
                                    ushort sourceCabTime;

                                    Cab.Interop.CabInterop.DateTimeToCabDateAndTime(sourceFileTime, out sourceCabDate, out sourceCabTime);
                                    cabinetValid = (cabFileInfo.Date == sourceCabDate && cabFileInfo.Time == sourceCabTime);
                                }

                                if (!cabinetValid)
                                {
                                    break;
                                }

                                i++;
                            }
                        }
                    }
                }

                resolved.BuildOption = cabinetValid ? CabinetBuildOption.Copy : CabinetBuildOption.BuildAndCopy;
            }

            return resolved;
        }

        /// <summary>
        /// Resolve the layout path of a media.
        /// </summary>
        /// <param name="mediaRow">The media's row.</param>
        /// <param name="layoutDirectory">The layout directory for the setup image.</param>
        /// <returns>The layout path for the media.</returns>
        public virtual string ResolveMedia(MediaRow mediaRow, string layoutDirectory)
        {
            if (mediaRow == null)
            {
                throw new ArgumentNullException("mediaRow");
            }

            string mediaLayoutDirectory = mediaRow.Layout;

            if (null == mediaLayoutDirectory)
            {
                mediaLayoutDirectory = layoutDirectory;
            }
            else if (!Path.IsPathRooted(mediaLayoutDirectory))
            {
                mediaLayoutDirectory = Path.Combine(layoutDirectory, mediaLayoutDirectory);
            }

            return mediaLayoutDirectory;
        }

        /// <summary>
        /// Resolves the URL to a file.
        /// </summary>
        /// <param name="url">URL that may be a format string for the id and fileName.</param>
        /// <param name="packageId">Identity of the package (if payload is not part of a package) the URL points to. NULL if not part of a package.</param>
        /// <param name="payloadId">Identity of the payload the URL points to.</param>
        /// <param name="fileName">File name the URL points at.</param>
        /// <param name="fallbackUrl">Optional URL to use if the URL provided is empty.</param>
        /// <returns>An absolute URL or null if no URL is provided.</returns>
        public virtual string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            // If a URL was not specified but there is a fallback URL that has a format specifier in it
            // then use the fallback URL formatter for this URL.
            if (String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(fallbackUrl))
            {
                string formattedFallbackUrl = String.Format(fallbackUrl, packageId, payloadId, fileName);
                if (!String.Equals(fallbackUrl, formattedFallbackUrl, StringComparison.OrdinalIgnoreCase))
                {
                    url = fallbackUrl;
                }
            }

            if (!String.IsNullOrEmpty(url))
            {
                string formattedUrl = String.Format(url, packageId, payloadId, fileName);

                Uri canonicalUri;
                if (Uri.TryCreate(formattedUrl, UriKind.Absolute, out canonicalUri))
                {
                    url = canonicalUri.AbsoluteUri;
                }
                else
                {
                    url = null;
                }
            }

            return url;
        }

        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="source">The file to copy.</param>
        /// <param name="destination">The destination file.</param>
        /// <param name="overwrite">true if the destination file can be overwritten; otherwise, false.</param>
        public virtual bool CopyFile(string source, string destination, bool overwrite)
        {
            if (overwrite)
            {
                File.Delete(destination);
            }

            if (!CreateHardLink(destination, source, IntPtr.Zero))
            {
#if DEBUG
                int er = Marshal.GetLastWin32Error();
#endif

                File.Copy(source, destination, overwrite);
            }

            return true;
        }

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="source">The file to move.</param>
        /// <param name="destination">The destination file.</param>
        public virtual bool MoveFile(string source, string destination, bool overwrite)
        {
            if (overwrite)
            {
                File.Delete(destination);
            }

            File.Move(source, destination);
            return true;
        }

        /// <summary>
        /// Create patch if needed. This runs in the cabinet building thread.
        /// </summary>
        /// <param name="fileRow">The FileRow of the file to create the delta for.</param>
        /// <param name="retainRangeWarning">true if the retain ranges were ignored to mismatches.</param>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters")]
        public virtual void ResolvePatch(FileRow fileRow, out bool retainRangeWarning)
        {
            if (fileRow == null)
            {
                throw new ArgumentNullException("fileRow");
            }

            retainRangeWarning = false;
            if (this.Core.DeltaBinaryPatch && RowOperation.Modify == fileRow.Operation)
            {
                if (0 != (PatchAttributeType.IncludeWholeFile | fileRow.PatchAttributes))
                {
                    string deltaBase = Common.GenerateIdentifier("dlt", Common.GenerateGuid());
                    string deltaFile = Path.Combine(this.Core.TempFilesLocation, String.Concat(deltaBase, ".dpf"));
                    string headerFile = Path.Combine(this.Core.TempFilesLocation, String.Concat(deltaBase, ".phd"));
                    PatchAPI.PatchInterop.PatchSymbolFlagsType apiPatchingSymbolFlags = 0;
                    bool optimizePatchSizeForLargeFiles = false;

                    Table wixPatchIdTable = this.Core.Output.Tables["WixPatchId"];
                    if (null != wixPatchIdTable)
                    {
                        Row row = wixPatchIdTable.Rows[0];
                        if (null != row)
                        {
                            if (null != row[2])
                            {
                                optimizePatchSizeForLargeFiles = (1 == Convert.ToUInt32(row[2], CultureInfo.InvariantCulture));
                            }
                            if (null != row[3])
                            {
                                apiPatchingSymbolFlags = (PatchAPI.PatchInterop.PatchSymbolFlagsType)Convert.ToUInt32(row[3], CultureInfo.InvariantCulture);
                            }
                        }
                    }

                    if (PatchAPI.PatchInterop.CreateDelta(
                            deltaFile,
                            fileRow.Source,
                            fileRow.Symbols,
                            fileRow.RetainOffsets,
                            fileRow.PreviousSourceArray,
                            fileRow.PreviousSymbolsArray,
                            fileRow.PreviousIgnoreLengthsArray,
                            fileRow.PreviousIgnoreOffsetsArray,
                            fileRow.PreviousRetainLengthsArray,
                            fileRow.PreviousRetainOffsetsArray,
                            apiPatchingSymbolFlags,
                            optimizePatchSizeForLargeFiles,
                            out retainRangeWarning))
                    {
                        PatchAPI.PatchInterop.ExtractDeltaHeader(deltaFile, headerFile);
                        fileRow.Patch = headerFile;
                        fileRow.Source = deltaFile;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a path exists, and throws a well known error for invalid paths.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>True if path exists.</returns>
        private static bool CheckFileExists(string path)
        {
            try
            {
                return File.Exists(path);
            }
            catch (ArgumentException)
            {
                throw new WixException(WixErrors.IllegalCharactersInPath(path));
            }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
