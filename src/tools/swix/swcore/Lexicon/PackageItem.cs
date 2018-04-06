// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.ComponentModel;
    using System.Windows.Markup;
    using System.Collections.Generic;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Base class for all items that can be parsed.
    /// </summary>
    [RuntimeNameProperty("Id")]
    [DefaultPropertyNameAttribute("Id")]
    public abstract class PackageItem : IPackageItem
    {
        private bool resolved;
        private bool resolving;
        private bool resolvedGroup;

        /// <summary>
        /// Optional unique identifier for the item.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Optional parent package item in the document.
        /// </summary>
        public PackageItem Parent { get; internal set; }

        /// <summary>
        /// Container holding this package item.
        /// </summary>
        [TypeConverter(typeof(IdTypeConverter))]
        public Group Group { get; set; }

        /// <summary>
        /// Specifies whether this item is an internal system item or user defined.
        /// </summary>
        public bool System { get; internal set; }

        /// <summary>
        /// Returns a list of names by which this item can be referenced.
        /// </summary>
        /// <returns>A list of names by which this item can be referenced.</returns>
        public virtual IEnumerable<string> GetReferenceNames()
        {
            // We've discussed having a fixed base implementation with virtual (or member) values
            // for ID pre- and post-fixes, and then using a virtual for the non-id values.  At present,
            // however, the only real implementers are the base (here) and FileSystemResource,
            // Folder, and File.  To keep things simple, each one currently re-implements the ID
            // handling as needed.
            if (!String.IsNullOrEmpty(this.Id))
            {
                yield return String.Concat(this.Id, ":");
            }

            // Overrides will make use of other reference-able fields in order to construct references.
            // In all cases, they should return the "shortest" reference first (i.e. the ID), and then provide
            // longer and longer variations.  For an example, see the Folder and File implementations.
        }

        internal FileLineNumber LineNumber { get; set; }

        internal Dictionary<string, DelayedItemLookup> DelayedLookup = new Dictionary<string, DelayedItemLookup>();

        internal bool Deleted { get; set; }

        /// <summary>
        /// Called when the item resolution is beginning. Default implementation does nothing.
        /// </summary>
        /// <param name="context">Frontend compiler context to aid in resolution.</param>
        protected virtual void OnResolveBegin(FrontendCompiler context)
        {
        }

        /// <summary>
        /// Called when the item resolution is being completed. Default implementation resolves any remaining delayed lookups.
        /// </summary>
        /// <param name="context">Frontend compiler context to aid in resolution.</param>
        protected virtual void OnResolveEnd(FrontendCompiler context)
        {
        }

        /// <summary>
        /// Called to verify that this item and all the objects it refers to are not deleted.
        /// </summary>
        protected virtual void OnVerifyResolvedConsistency()
        {
        }

        internal Group ResolveGroup(FrontendCompiler context)
        {
            if (!this.resolvedGroup)
            {
                // See if the group was explicitly provided.
                DelayedItemLookup groupLookup;
                if (this.DelayedLookup.TryGetValue("Group", out groupLookup))
                {
                    PackageItem item = groupLookup.Resolve(context, true);
                    if (item != null && !(item is Group))
                    {
                        this.Group = new Group(false, groupLookup.Lookup, this);
                        context.AddItem(this.LineNumber, this.Group);
                    }

                    this.DelayedLookup.Remove("Group"); // remove the delayed look up so we don't try to look it up again in the future.
                }

                // If a group wasn't explicitly provided, try to default to the group from the parent.
                if (this.Group == null)
                {
                    if (this.Parent != null)
                    {
                        this.Group = this.Parent.ResolveGroup(context);
                        if (this.Group != null && this.Group != this.Parent)
                        {
                            this.Group.Items.Add(this);
                        }
                    }
                }
                else if (this.Parent is Group)
                {
                    // TODO: send warning message that we overrode the implicit (via parent) group with an explicit group.
                }

                this.resolvedGroup = true;
            }

            return (this is Group) ? (Group)this : this.Group;
        }

        internal bool BeginResolve(FrontendCompiler context)
        {
            bool began = false;
            if (!this.resolved && !this.resolving)
            {
                began = true;
                this.resolving = true; // mark the resource resolving to prevent infinite loops.

                this.OnResolveBegin(context);
            }

            return began;
        }

        internal void EndResolve(FrontendCompiler context)
        {
            if (this.resolving)
            {
                // Process all the delayed lookup items first.
                foreach (DelayedItemLookup lookup in this.DelayedLookup.Values)
                {
                    lookup.Resolve(context);
                }

                this.OnResolveEnd(context);

                this.resolved = true;
                this.resolving = false;
            }
        }

        internal void VerifyResolvedConsistency()
        {
            if (this.Deleted)
            {
                CompilerException.ThrowInternalError("Internal resolved consistency violation: Item was deleted.");
            }

            if (this.Parent != null && this.Parent.Deleted)
            {
                CompilerException.ThrowInternalError("Internal resolved consistency violation: Parent was deleted.");
            }

            if (this.Group != null && this.Group.Deleted)
            {
                CompilerException.ThrowInternalError("Internal resolved consistency violation: Group was deleted.");
            }

            if (!this.resolved)
            {
                CompilerException.ThrowInternalError("Internal resolved consistency violation: Item was not resolved.");
            }

            if (this.resolving)
            {
                CompilerException.ThrowInternalError("Internal resolved consistency violation: Item was still resolving.");
            }

            if (!this.resolvedGroup)
            {
                CompilerException.ThrowInternalError("Internal resolved consistency violation: Item did not resolve its group.");
            }

            this.OnVerifyResolvedConsistency();
        }
    }
}
