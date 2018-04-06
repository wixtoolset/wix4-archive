// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
