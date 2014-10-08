//-------------------------------------------------------------------------------------------------
// <copyright file="IPreprocessorCore.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    public interface IPreprocessorCore : IMessageHandler
    {
        /// <summary>
        /// Gets or sets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        Platform CurrentPlatform { get; }

        /// <summary>
        /// Gets whether the core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        bool EncounteredError { get; }
    }
}
