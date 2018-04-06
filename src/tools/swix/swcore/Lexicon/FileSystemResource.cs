// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.ComponentModel;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Base class for all file system resources.
    /// </summary>
    [DefaultPropertyNameAttribute("Name")]
    public abstract class FileSystemResource : Resource
    {
        /// <summary>
        /// Name of file system resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Calculated parent folder.
        /// </summary>
        internal Folder ParentFolder { get; private set; }

        /// <summary>
        /// Internal value used to calculate parent folder.
        /// </summary>
        internal string ParentFolderIdFromName { get; private set; }

        /// <summary>
        /// Internal value used to calculate parent folder
        /// </summary>
        internal string ParentRelativePathFromName { get; private set; }

        internal void ReparentFolder(Folder newParentFolder)
        {
            // Ensure this new parent folder will not change our path if we already calculated it.
            if (!String.IsNullOrEmpty(this.Path) && !this.Path.StartsWith(newParentFolder.Path, StringComparison.OrdinalIgnoreCase))
            {
                CompilerException.ThrowInternalError("Reparenting folder would change already calculated path. That is not allowed.");
            }

            // If the parent was the parent folder, update the parent to point to the replacement folder.
            if (this.Parent == this.ParentFolder)
            {
                this.Parent = newParentFolder;
            }

            if (this.ParentFolder != null)
            {
                this.ParentFolder.Items.Remove(this);
            }

            this.ParentFolder = newParentFolder;
            newParentFolder.Items.Add(this);
        }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            this.ParentFolder = this.Parent as Folder;

            // If the resource has a name, break it down into it's constituent parts: [folderid:\][relative\path\to\][name[\]]
            if (!String.IsNullOrEmpty(this.Name))
            {
                // If the name starts with a parent folder identifier.
                int idSeparator = this.Name.IndexOf(':');
                if (idSeparator > -1)
                {
                    this.ParentFolderIdFromName = this.Name.Substring(0, idSeparator);
                    this.Name = this.Name.Substring(idSeparator + 1).TrimStart(FileSystemResourceManager.DirectorySplitChars);
                }

                // Do not include a trailing backslash in the search.
                int endOfName = this.Name.EndsWith("\\", StringComparison.Ordinal) ? this.Name.Length - 2 : this.Name.Length - 1;
                int lastBackslash = this.Name.LastIndexOfAny(FileSystemResourceManager.DirectorySplitChars, endOfName);
                if (lastBackslash > -1)
                {
                    this.ParentRelativePathFromName = this.Name.Substring(0, lastBackslash + 1);
                    this.Name = this.Name.Substring(lastBackslash + 1);
                }
            }

            if (this.ParentFolder != null && !String.IsNullOrEmpty(this.ParentFolderIdFromName))
            {
                // TODO: send error that Name attribute overrode ParentFolder string.
            }

            //FileSystemResourceManager manager = (FileSystemResourceManager)context.GetService(typeof(FileSystemResourceManager));
            //manager.AddResource(context, this);
        }

        protected override void OnVerifyResolvedConsistency()
        {
            base.OnVerifyResolvedConsistency();

            if (this.ParentFolder != null && this.ParentFolder.Deleted)
            {
                CompilerException.ThrowInternalError("Internal resolved consistency violation: File system ParentFolder was deleted.");
            }
        }
    }
}
