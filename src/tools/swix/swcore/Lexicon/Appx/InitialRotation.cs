// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    public enum RotationPreference
    {
        portrait,
        landscape,
        portraitFlipped,
        landscapeFlipped,
    }

    /// <summary>
    /// Defines the initial orientations desired by the application.
    /// </summary>
    public class InitialRotation : PackageItem
    {
        public RotationPreference Preference { get; set; }
    }
}
