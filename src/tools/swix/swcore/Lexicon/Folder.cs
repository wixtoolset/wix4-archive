// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Folder in the file system.
    /// </summary>
    [DefaultCollectionProperty("Items")]
    [TypeConverter(typeof(IdTypeConverter))]
    public class Folder : FileSystemResource, IParentItem<FileSystemResource>
    {
        public Folder()
        {
            this.Items = new PackageItemCollection<FileSystemResource>(this);
        }

        /// <summary>
        /// Creates an external folder reference.
        /// </summary>
        /// <param name="system">Creates a system external folder.</param>
        /// <param name="id">Id for the Folder and serves as the base of the name.</param>
        internal Folder(bool system, string id)
            : this()
        {
            this.System = system;
            this.Id = id;
            this.External = true;
        }

        /// <summary>
        /// Define a folder that is defined external to this compile.
        /// </summary>
        public bool External { get; set; }

        public ICollection<FileSystemResource> Items { get; private set; }

        public IEnumerable EnumItems { get { return this.Items; } }

        // Note that this (or the File override) could be in FileSystemResource, but neither seems
        // "more fundamental" than the other.  These can be refactored if/when other classes derive
        // from FileSystemResource.
        public override IEnumerable<string> GetReferenceNames()
        {
            // See comment in PackageItem.GetReferenceNames() about the current design,
            // and other possible future refactoring.
            if (!String.IsNullOrEmpty(this.Id))
            {
                // Folder references always end with backslash, at least canonically.
                yield return String.Concat(this.Id, ":\\");
            }

            if (this.ParentFolder != null && !String.IsNullOrEmpty(this.Name) && this != this.ParentFolder)
            {
                foreach (string parentName in this.ParentFolder.GetReferenceNames())
                {
                    // Folder references always end with backslash, at least canonically.
                    yield return String.Concat(parentName, this.Name, this.Name.EndsWith("\\", StringComparison.Ordinal) ? String.Empty : "\\");
                }
            }
        }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            if (this.External)
            {
                // TODO: error if id is empty.
                // TODO: error if name is *not* empty.
                // TODO: error if parent is a folder.
                this.SetPath(String.Concat(this.Id, ":\\"));
            }
            else if (!String.IsNullOrEmpty(this.Name) && !this.Name.EndsWith("\\", StringComparison.Ordinal))
            {
                this.Name += "\\";
            }

            base.OnResolveBegin(context);
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
        }
    }
}
