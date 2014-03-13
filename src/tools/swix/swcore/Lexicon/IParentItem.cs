//-------------------------------------------------------------------------------------------------
// <copyright file="IParentItem.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
