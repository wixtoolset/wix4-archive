// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified
{
    /// <summary>
    /// Type of the resulting package.
    /// </summary>
    public enum PackageType
    {
        Unknown,
        Appx,
        Nuget,
        Msi,
        Vsix,
        Wixlib,
    }
}
