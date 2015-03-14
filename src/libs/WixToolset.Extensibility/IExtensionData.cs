//-------------------------------------------------------------------------------------------------
// <copyright file="IExtensionData.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    /// <summary>
    /// Interface extensions implement to provide data.
    /// </summary>
    public interface IExtensionData
    {
        /// <summary>
        /// Gets the table definitions for this extension.
        /// </summary>
        /// <value>Table definisions for this extension or null if there are no table definitions.</value>
        TableDefinitionCollection TableDefinitions { get; }

        /// <summary>
        /// Gets the optional default culture.
        /// </summary>
        /// <value>The optional default culture.</value>
        string DefaultCulture { get; }

        /// <summary>
        /// Gets the library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <param name="allowIncompleteSections">Whether a WixMissingTableDefinitionException should be thrown if a section has a table without a table definition.</param>
        /// <returns>The library for this extension or null if there is no library.</returns>
        Library GetLibrary(TableDefinitionCollection tableDefinitions, bool allowIncompleteSections);
    }
}
