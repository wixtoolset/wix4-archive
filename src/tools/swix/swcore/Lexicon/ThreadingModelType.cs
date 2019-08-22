// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    /// <summary>
    /// Threading model for inproc and out of proc servers.
    /// </summary>
    public enum ThreadingModelType
    {
        sta,
        mta,
        both,

        /// <summary>
        /// Neutral threading model.
        /// </summary>
        /// <remarks>Only supported when creating Windows Installer based packages. AppX packages treat this like "Both".</remarks>
        neutral,
    }
}
