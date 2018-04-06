// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;
    using IO = System.IO;

    /// <summary>
    /// File in the file system.
    /// </summary>
    [DefaultCollectionProperty("Items")]
    [TypeConverter(typeof(IdTypeConverter))]
    public class File : FileSystemResource, IParentItem<ITargetFile>, ITargetFile, IFileReference
    {
        /// <summary>
        /// Creates a new File object.
        /// </summary>
        /// <remarks>This constructor is typically only used by XAML.</remarks>
        public File()
        {
            this.Items = new PackageItemCollection<ITargetFile>(this);
        }

        /// <summary>
        /// Creates a new File object.
        /// </summary>
        /// <param name="source">Path to file on disk.</param>
        public File(string source) :
            this(source, null)
        {
        }

        /// <summary>
        /// Creates a new File object with a specific name.
        /// </summary>
        /// <param name="source">Path to file on disk.</param>
        /// <param name="name">Relative path to place file.</param>
        public File(string source, string name) : 
            this()
        {
            this.Source = source;
            this.Name = name;
        }

        /// <summary>
        /// Gets or sets the file that is companion to this file.
        /// </summary>
        [TypeConverter(typeof(IdTypeConverter))]
        public File Companion { get; set; }

        /// <summary>
        /// Path to file on disk.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Child package items that target this file.
        /// </summary>
        public ICollection<ITargetFile> Items { get; private set; }

        /// <summary>
        /// Enumerator over child items.
        /// </summary>
        public IEnumerable EnumItems { get { return this.Items; } }

        /// <summary>
        /// Gets the file targeted by this item.
        /// </summary>
        /// <returns>The Companion property.</returns>
        public File GetTargetedFile()
        {
            return this.Companion;
        }

        /// <summary>
        /// Returns this object as a package item.
        /// </summary>
        /// <returns>this</returns>
        public PackageItem GetPackageItem()
        {
            return this;
        }

        // Note that this (or the Folder override) could be in FileSystemResource, but neither seems
        // "more fundamental" than the other.  These can be refactored if/when other classes derive
        // from FileSystemResource.
        public override IEnumerable<string> GetReferenceNames()
        {
            // See comment in PackageItem.GetReferenceNames() about the current design,
            // and other possible future refactoring.
            if (!String.IsNullOrEmpty(this.Id))
            {
                yield return String.Concat(this.Id, ":");
            }

            if (this.ParentFolder != null && !String.IsNullOrEmpty(this.Name) /*&& this != this.ParentFolder*/)
            {
                foreach (string parentName in this.ParentFolder.GetReferenceNames())
                {
                    yield return String.Concat(parentName, this.Name);
                }
            }
        }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            if (String.IsNullOrEmpty(this.Name) && String.IsNullOrEmpty(this.Source))
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.FileMissingNameAndSource(), this));
            }
            else if (String.IsNullOrEmpty(this.Name) || this.Name.EndsWith("\\", StringComparison.Ordinal))
            {
                this.Name = IO.Path.Combine(this.Name ?? String.Empty, IO.Path.GetFileName(this.Source));
            }

            // TODO: error if name ends with "\".

            base.OnResolveBegin(context);

            // TODO: error if name is empty.

            if (String.IsNullOrEmpty(this.Source))
            {
                // TODO: walk the parent tree to see if a source is available.

                this.Source = IO.Path.Combine(this.ParentRelativePathFromName ?? String.Empty, this.Name);
            }
            else if (this.Source.EndsWith("\\", StringComparison.Ordinal))
            {
                this.Source = IO.Path.Combine(this.Source, this.ParentRelativePathFromName ?? String.Empty, this.Name);
            }
        }

        protected override void OnResolveEnd(FrontendCompiler context)
        {
            base.OnResolveEnd(context);

            // If the File wasn't explicitly set, default it to the parent if the parent is a file.
            if (this.Companion == null)
            {
                this.Companion = this.Parent as File;
            }
            else if (this.Parent is File)
            {
                // TODO: display a warning that we overrode the implicit (via parent) file with an explicit file reference.
            }
        }

        protected override void OnVerifyResolvedConsistency()
        {
            base.OnVerifyResolvedConsistency();

            foreach (PackageItem item in this.Items)
            {
                if (item.Deleted)
                {
                    CompilerException.ThrowInternalError("Internal resolved consistency violation: Item contained in Folder was deleted.");
                }
            }

            if (this.Companion != null && this.Companion.Deleted)
            {
                CompilerException.ThrowInternalError("Internal resolved consistency violation: File Companion was deleted.");
            }
        }
    }
}
