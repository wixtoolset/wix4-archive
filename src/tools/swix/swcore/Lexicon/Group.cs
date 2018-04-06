// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Arbitrary grouping of package items.
    /// </summary>
    [DefaultCollectionProperty("Items")]
    public class Group : PackageItem
    {
        /// <summary>
        /// Creates an arbitrary grouping of package items.
        /// </summary>
        public Group()
        {
            this.Items = new PackageItemCollection<PackageItem>(this);
        }

        /// <summary>
        /// Creates an identifiable arbitrary grouping of package items.
        /// </summary>
        /// <param name="id">Identity for group.</param>
        /// <param name="items">Optional array of items to add to the group.</param>
        public Group(string id, params PackageItem[] items) :
            this(false, id, items)
        {
        }

        internal Group(bool system, string id, params PackageItem[] items) :
            this()
        {
            this.System = system;
            this.Id = id;

            foreach (PackageItem item in items)
            {
                this.Items.Add(item);
            }
        }

        /// <summary>
        /// List of items contained in this group.
        /// </summary>
        public ICollection<PackageItem> Items { get; private set; }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (String.IsNullOrEmpty(this.Id))
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.GroupMissingId(), this));
            }
        }

        protected override void OnVerifyResolvedConsistency()
        {
            base.OnVerifyResolvedConsistency();

            foreach (PackageItem item in this.Items)
            {
                if (item.Deleted)
                {
                    CompilerException.ThrowInternalError("Internal resolved consistency violation: Item contained in Group was deleted.");
                }
            }
        }
    }
}
