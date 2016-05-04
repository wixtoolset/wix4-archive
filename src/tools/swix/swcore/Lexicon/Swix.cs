// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.Collections.Generic;

    [DefaultCollectionPropertyAttribute("Items")]
    public class Swix
    {
        public Swix()
        {
            this.Items = new PackageItemCollection<PackageItem>(null);
        }

        internal Swix(params object[] items) :
            this()
        {
            foreach (PackageItem item in items)
            {
                ProcessItem(item);
            }
        }

        public ICollection<PackageItem> Items { get; private set; }

        internal FileLineNumber LineNumber { get; set; }

        private void ProcessItem(PackageItem item)
        {
            Group group = item as Group;
            if (group != null)
            {
                this.ProcessGroup((Group)item);
            }
            else
            {
                this.ProcessResource((Resource)item);
            }
        }

        /// <summary>
        /// Add group to the Swix root.
        /// </summary>
        /// <param name="group">Group to process.</param>
        private void ProcessGroup(Group group)
        {
            this.Items.Add(group);

            foreach (PackageItem item in group.Items)
            {
                ProcessItem(item);
            }
        }

        /// <summary>
        /// Add resource to the Swix root.
        /// </summary>
        /// <param name="resource">Resource to process.</param>
        private void ProcessResource(Resource resource)
        {
            this.Items.Add(resource);

            IParentItem parent = resource as IParentItem;
            if (parent != null)
            {
                foreach (PackageItem item in parent.EnumItems)
                {
                    ProcessItem(item);
                }
            }
        }
    }
}
