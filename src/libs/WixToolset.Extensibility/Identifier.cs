//-------------------------------------------------------------------------------------------------
// <copyright file="Identifier.cs" company="Outercurve Foundation">
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
    /// Class to define the identifier and access for a row.
    /// </summary>
    public class Identifier
    {
        public static Identifier Invalid = new Identifier(null, AccessModifier.Private);

        public Identifier(string id, AccessModifier access)
        {
            this.Id = id;
            this.Access = access;
        }

        /// <summary>
        /// Access modifier for a row.
        /// </summary>
        public AccessModifier Access { get; private set; }

        /// <summary>
        /// Identifier for the row.
        /// </summary>
        public string Id { get; private set; }
    }
}
