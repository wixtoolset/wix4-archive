//-------------------------------------------------------------------------------------------------
// <copyright file="AssemblyDefaultHeatExtensionAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Represents a custom attribute for declaring the type to use
// as the default heat extension in an assembly.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Tools
{
    using System;

    /// <summary>
    /// Represents a custom attribute for declaring the type to use
    /// as the default heat extension in an assembly.
    /// </summary>
    public class AssemblyDefaultHeatExtensionAttribute : Attribute
    {
        private readonly Type extensionType;

        /// <summary>
        /// Instantiate a new AssemblyDefaultHeatExtensionAttribute.
        /// </summary>
        /// <param name="extensionType">The type of the default heat extension in an assembly.</param>
        public AssemblyDefaultHeatExtensionAttribute(Type extensionType)
        {
            this.extensionType = extensionType;
        }

        /// <summary>
        /// Gets the type of the default heat extension in an assembly.
        /// </summary>
        /// <value>The type of the default heat extension in an assembly.</value>
        public Type ExtensionType
        {
            get { return this.extensionType; }
        }
    }
}
