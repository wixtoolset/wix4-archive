//-------------------------------------------------------------------------------------------------
// <copyright file="ExtensionData.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using WixToolset.Data;

    public abstract class ExtensionData : IExtensionData
    {
        /// <summary>
        /// Gets the optional table definitions for this extension.
        /// </summary>
        /// <value>Table definitions for this extension or null if there are no table definitions.</value>
        public virtual TableDefinitionCollection TableDefinitions
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional default culture.
        /// </summary>
        /// <value>The optional default culture.</value>
        public virtual string DefaultCulture
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <param name="allowIncompleteSections">Whether a WixMissingTableDefinitionException should be thrown if a section has a table without a table definition.</param>
        /// <returns>The library for this extension or null if there is no library.</returns>
        public virtual Library GetLibrary(TableDefinitionCollection tableDefinitions, bool allowIncompleteSections)
        {
            return null;
        }

        /// <summary>
        /// Help for loading a library from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The name of the embedded resource being requested.</param>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <param name="allowIncompleteSections">Whether a WixMissingTableDefinitionException should be thrown if a section has a table without a table definition.</param>
        /// <returns>The loaded library.</returns>
        protected static Library LoadLibraryHelper(Assembly assembly, string resourceName, TableDefinitionCollection tableDefinitions, bool allowIncompleteSections)
        {
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                UriBuilder uriBuilder = new UriBuilder(assembly.CodeBase);
                uriBuilder.Scheme = "embeddedresource";
                uriBuilder.Fragment = resourceName;

                return Library.Load(resourceStream, uriBuilder.Uri, tableDefinitions, false, allowIncompleteSections);
            }
        }

        /// <summary>
        /// Helper for loading table definitions from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The name of the embedded resource being requested.</param>
        /// <returns>The loaded table definitions.</returns>
        protected static TableDefinitionCollection LoadTableDefinitionHelper(Assembly assembly, string resourceName)
        {
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            using (XmlReader reader = XmlReader.Create(resourceStream))
            {
                return TableDefinitionCollection.Load(reader);
            }
        }
    }
}
