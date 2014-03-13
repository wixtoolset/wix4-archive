//-------------------------------------------------------------------------------------------------
// <copyright file="IPackageItem.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon
{
    using System;

    /// <summary>
    /// Interface that defines the core parts of a package item.
    /// </summary>
    /// <remarks>This interface exists to create a root for other interfaces that need to act like package items.</remarks>
    public interface IPackageItem
    {
        string Id { get; }

        Group Group { get; }

        PackageItem Parent { get; }

        bool System { get; }
    }
}
