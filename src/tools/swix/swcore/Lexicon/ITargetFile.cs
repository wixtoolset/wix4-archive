// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;

    /// <summary>
    /// Indicates an item can target a file object.
    /// </summary>
    public interface ITargetFile : IPackageItem
    {
        /// <summary>
        /// Gets the File targeted.
        /// </summary>
        /// <returns>File targeted.</returns>
        File GetTargetedFile();
    }
}
