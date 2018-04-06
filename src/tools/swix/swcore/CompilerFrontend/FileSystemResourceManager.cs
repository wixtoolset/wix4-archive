// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using WixToolset.Simplified.Lexicon;
    using IO = System.IO;

    internal class FileSystemResourceManager
    {
        internal static readonly char[] DirectorySplitChars = new char[] { IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar };

        private string defaultFolderId;
        private Dictionary<string, Resource> resolvedPaths;

        public FileSystemResourceManager(string defaultFolderId)
        {
            this.defaultFolderId = defaultFolderId;

            this.resolvedPaths = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
        }

        public void ResolveResources(FrontendCompiler context, List<FileSystemResource> resolves)
        {
            if (resolves.Count == 0)
            {
                return;
            }

            this.ResolveParentFolders(context, resolves);

            List<FileSystemResource> implicitPaths = new List<FileSystemResource>();
            foreach (FileSystemResource r in new List<FileSystemResource>(resolves))
            {
                if (!r.Deleted)
                {
                    this.ResolvePath(context, r);

                    // If the resource had a relative path it may still need to create some implicit folders
                    // so add it to the list.
                    if (!String.IsNullOrEmpty(r.ParentRelativePathFromName))
                    {
                        implicitPaths.Add(r);
                    }
                }
            }

            this.CreateImplicitFolders(context, implicitPaths);
        }

        public bool TryFindResourceByPath(FrontendCompiler context, string path, out Resource resource)
        {
            Folder defaultRootFolder = this.GetDefaultRoot(context);

            string parent;
            string child;
            string[] idPath = path.Split(new char[] { ':' }, 2);
            if (1 == idPath.Length)
            {
                parent = defaultRootFolder.Path;
                child = idPath[0];
            }
            else
            {
                PackageItem item;
                if (context.TryGetItemById(idPath[0], out item))
                {
                    parent = ((FileSystemResource)item).Path;
                }
                else
                {
                    parent = idPath[0];
                }

                child = idPath[1];
            }

            if (child.StartsWith("\\", StringComparison.Ordinal))
            {
                child = child.Substring(1);
            }

            path = IO.Path.Combine(parent, child);
            return this.resolvedPaths.TryGetValue(path, out resource);
        }

        private void ResolveParentFolders(FrontendCompiler context, List<FileSystemResource> resolves)
        {
            Folder defaultRootFolder = this.GetDefaultRoot(context);

            foreach (FileSystemResource r in new List<FileSystemResource>(resolves))
            {
                if (!String.IsNullOrEmpty(r.ParentFolderIdFromName))
                {
                    PackageItem item;
                    Folder newParentFolder = null;
                    if (context.TryGetItemById(r.ParentFolderIdFromName, out item))
                    {
                        newParentFolder = item as Folder;
                        if (newParentFolder == null)
                        {
                            context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.InvalidFolderReference(r.ParentFolderIdFromName, item.GetType().Name), r.LineNumber));
                        }
                    }
                    else // did not find a matching id.
                    {
                        // TODO: come up with a better error message for this.
                        context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.InvalidFolderReference(r.ParentFolderIdFromName, "not found"), r.LineNumber));
                    }

                    if (newParentFolder != null)
                    {
                        // If this is a "no name" folder then reparent all the contents to this new parent
                        if (r is Folder && String.IsNullOrEmpty(r.Name))
                        {
                            this.ReparentFolderChildren(context, (Folder)r, newParentFolder);
                            resolves.Remove(r); // folder is deleted, be sure not to try to continue to process it.
                        }
                        else // simply point our folder to this new parent.
                        {
                            r.ReparentFolder(newParentFolder);
                        }
                    }
                }
                else if (r.ParentFolder == null && !(r is Folder && ((Folder)r).External)) // if we didn't get a parent folder yet and this is not an external folder reference, default to the application folder.
                {
                    r.ReparentFolder(defaultRootFolder);
                }
            }
        }

        private Folder GetDefaultRoot(FrontendCompiler context)
        {
            PackageItem item;
            if (!context.TryGetItemById(this.defaultFolderId, out item))
            {
                CompilerException.ThrowInternalError("Failed to locate default root folder with Id: '{0}'.", this.defaultFolderId);
            }

            Folder folder = item as Folder;
            if (folder == null)
            {
                CompilerException.ThrowInternalError("Unexpected default root item type. Ensure Id: '{0}' resolves to a Folder instead of a: '{1}'.", this.defaultFolderId, item);
            }

            return folder;
        }

        private string ResolvePath(FrontendCompiler context, FileSystemResource r)
        {
            if (String.IsNullOrEmpty(r.Path))
            {
                // If there is a parent folder resolve it's path.
                string parentPath = String.Empty;
                if (r.ParentFolder != null)
                {
                    parentPath = ResolvePath(context, r.ParentFolder); // recurse.
                }

                string path = IO.Path.Combine(parentPath, r.ParentRelativePathFromName ?? String.Empty, r.Name ?? String.Empty);
                Debug.Assert((r is Folder && path.EndsWith("\\", StringComparison.Ordinal)) || (r is File && !path.EndsWith("\\", StringComparison.Ordinal)));

                r.SetPath(path);
            }

            // If in the process of calculating this resource's path we reparented the folder (which deletes the
            // resource) then don't check for a conflicting resource because this resource would lose anyway.
            if (!r.Deleted)
            {
                Resource conflictingResource;
                if (this.resolvedPaths.TryGetValue(r.Path, out conflictingResource))
                {
                    if (conflictingResource == r)
                    {
                        // We found ourself so don't do anything.
                    }
                    else if (conflictingResource is Folder && r is Folder) // folders are special because the can be implicitly created.
                    {
                        // If our resource has an id that makes it a better candidate for the path.
                        if (!String.IsNullOrEmpty(r.Id))
                        {
                            // The conflicting resource cannot also have an Id or the backend compiler will be all confusimicated.
                            if (!String.IsNullOrEmpty(conflictingResource.Id))
                            {
                                // TODO: change this to an error message instead of an internal compiler error.
                                CompilerException.ThrowInternalError("Two named folders refer to the same path. That is not supported.");
                            }

                            this.ReparentFolderChildren(context, (Folder)conflictingResource, (Folder)r);

                            this.resolvedPaths[r.Path] = r; // this resource now owns the path.
                        }
                        else // the conflicting resource either has an Id or was here first so it's a better parent.
                        {
                            this.ReparentFolderChildren(context, (Folder)r, (Folder)conflictingResource);
                        }
                    }
                    else
                    {
                        // TODO: change this to an error message instead of an internal compiler error.
                        CompilerException.ThrowInternalError("Two files or a file and a folder ended up with the same path. That is not allowed.");
                    }
                }
                else // no one owns this path yet so take it over.
                {
                    Debug.Assert(r != r.ParentFolder);
                    this.resolvedPaths.Add(r.Path, r);
                }
            }

            return r.Path;
        }

        private void CreateImplicitFolders(FrontendCompiler context, List<FileSystemResource> implicitPaths)
        {
            implicitPaths.Sort((x, y) => x.Path.CompareTo(y.Path));

            foreach (FileSystemResource r in implicitPaths)
            {
                string path = String.Empty;
                Folder parentFolder = null;

                string[] splitPath = r.Path.Split(FileSystemResourceManager.DirectorySplitChars, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < splitPath.Length - 1; ++i)
                {
                    Folder folder = null;
                    Resource found = null;

                    path = IO.Path.Combine(path, splitPath[i]) + "\\";
                    if (path.EndsWith(":\\", StringComparison.Ordinal))
                    {
                        PackageItem item;
                        if (context.TryGetItemById(path.Substring(0, path.Length - 2), out item))
                        {
                            folder = (Folder)item;
                        }
                    }
                    else if (this.resolvedPaths.TryGetValue(path, out found))
                    {
                        folder = found as Folder;
                        if (folder == null)
                        {
                            CompilerException.ThrowInternalError("Failed to resolve path to Folder. The path: '{0}' should resolve to a Folder instead of: '{1}'.", path, found);
                        }
                        else if (folder.ParentFolder != parentFolder)
                        {
                            CompilerException.ThrowInternalError("Found Folder.ParentFolder does not match expected parent Folder. Ensure path: '{0}' is correctly rooted to Folder/@Id='{1}'.", path, folder.Id);
                        }
                    }
                    else // need to create the implicit folder.
                    {
                        folder = new Folder();
                        folder.Group = r.Group;
                        folder.Name = splitPath[i];
                        parentFolder.Items.Add(folder);
                        context.AddItem(r.LineNumber, folder);

                        // Since the folder was just created, we need to jump start its "resolving" since all
                        // of the other resources have already started resolve.
                        folder.ResolveGroup(context);
                        folder.BeginResolve(context);
                        this.ResolvePath(context, folder);
                    }

                    parentFolder = folder;
                }

                if (r.ParentFolder != parentFolder)
                {
                    r.ReparentFolder(parentFolder);
                }
            }
        }

        private void ReparentFolderChildren(FrontendCompiler context, Folder deleteFolder, Folder replacementFolder)
        {
            Debug.Assert(replacementFolder != deleteFolder);
            if (!String.IsNullOrEmpty(deleteFolder.Path) && !deleteFolder.Path.Equals(replacementFolder.Path, StringComparison.OrdinalIgnoreCase))
            {
                CompilerException.ThrowInternalError("Reparenting to folder with different path. That should not be possible.");
            }

            foreach (FileSystemResource child in new List<FileSystemResource>(deleteFolder.Items))
            {
                child.ReparentFolder(replacementFolder);
            }
            Debug.Assert(deleteFolder.Items.Count == 0);

            if (deleteFolder.ParentFolder != null)
            {
                deleteFolder.ParentFolder.Items.Remove(deleteFolder);
            }

            context.RemoveItem(deleteFolder);
        }
    }
}
