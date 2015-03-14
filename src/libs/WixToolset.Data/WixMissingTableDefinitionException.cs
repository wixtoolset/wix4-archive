//-------------------------------------------------------------------------------------------------
// <copyright file="WixMissingTableDefinitionException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Exception thrown when a table definition is missing.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Data
{
    using System;

    /// <summary>
    /// Exception thrown when a table definition is missing.
    /// </summary>
    [Serializable]
    public class WixMissingTableDefinitionException : WixException
    {
        /// <summary>
        /// Instantiate new WixMissingTableDefinitionException.
        /// </summary>
        /// <param name="tableName">Name of the missing table.</param>
        public WixMissingTableDefinitionException(string tableName)
            : base(WixDataErrors.MissingTableDefinition(tableName))
        {
            TableName = tableName;
        }

        public string TableName { get; private set; }
    }
}
