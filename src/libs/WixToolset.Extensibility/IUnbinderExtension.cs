//-------------------------------------------------------------------------------------------------
// <copyright file="IUnbinderExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using System;
    using WixToolset.Data;

    /// <summary>
    /// Base class for creating an unbinder extension.
    /// </summary>
    public interface IUnbinderExtension
    {
        /// <summary>
        /// Called during the generation of sectionIds for an admin image.
        /// </summary>
        void GenerateSectionIds(Output output);
    }
}
