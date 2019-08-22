// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class PackageItemCollection<T> : ICollection<T>, ICollection where T : IPackageItem
    {
        private PackageItem parent;
        private List<T> items;

        public PackageItemCollection(PackageItem parent)
        {
            this.parent = parent;
            this.items = new List<T>();
        }

        public int Count
        {
            get { return this.items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(T item)
        {
            if (this.parent != null)
            {
                PackageItem i = item as PackageItem;
                if (i != null && i.Parent == null)
                {
                    i.Parent = this.parent;
                }
            }

            this.items.Add(item);
        }

        public void Clear()
        {
            this.items.Clear();
        }

        public bool Contains(T item)
        {
            return this.items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.items.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return this.items.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        public void CopyTo(Array array, int index)
        {
            this.items.CopyTo((T[])array, index);
        }
    }
}
