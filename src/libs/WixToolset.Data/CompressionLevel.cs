//-------------------------------------------------------------------------------------------------
// <copyright file="CompressionLevel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{

    /// <summary>
    /// Compression level to use when creating cabinet.
    /// </summary>
    public enum CompressionLevel
    {
        /// <summary>Use no compression.</summary>
        None,

        /// <summary>Use low compression.</summary>
        Low,

        /// <summary>Use medium compression.</summary>
        Medium,

        /// <summary>Use high compression.</summary>
        High,

        /// <summary>Use ms-zip compression.</summary>
        Mszip
    }
}
