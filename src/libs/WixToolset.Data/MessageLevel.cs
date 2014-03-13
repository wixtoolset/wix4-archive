//-------------------------------------------------------------------------------------------------
// <copyright file="MessageLevel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Message handling class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    /// <summary>
    /// Enum for message to display.
    /// </summary>
    public enum MessageLevel
    {
        /// <summary>Display nothing.</summary>
        Nothing,

        /// <summary>Display verbose information.</summary>
        Verbose,

        /// <summary>Display information.</summary>
        Information,

        /// <summary>Display warning.</summary>
        Warning,

        /// <summary>Display error.</summary>
        Error,
    }
}
