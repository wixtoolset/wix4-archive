// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Interface that defines an object is a parent of package items.
    /// </summary>
    public interface IParentItem
    {
        IEnumerable EnumItems { get; }
    }

    /// <summary>
    /// Interface that defines an object is a parent of a particular type of package items.
    /// </summary>
    /// <typeparam name="T">PackageItem type for child items.</typeparam>
    public interface IParentItem<T> : IParentItem where T : IPackageItem
    {
        ICollection<T> Items { get; }
    }
}
