//-------------------------------------------------------------------------------------------------
// <copyright file="ThreadingModelType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
