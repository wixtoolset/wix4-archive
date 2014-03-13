//-------------------------------------------------------------------------------------------------
// <copyright file="DefaultPropertyAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon
{
    using System;

    /// <summary>
    /// Represents a custom attribute for declaring the default property in a class.
    /// </summary>
    [AttributeUsageAttribute(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DefaultPropertyNameAttribute : Attribute
    {
        private readonly string property;

        /// <summary>
        /// Instantiate a new DefaultPropertyAttribute.
        /// </summary>
        /// <param name="propertyName">The name of the default property in a class.</param>
        public DefaultPropertyNameAttribute(string propertyName)
        {
            this.property = propertyName;
        }

        /// <summary>
        /// Gets the name of the default property in a class.
        /// </summary>
        /// <value>The name of the default property in a class.</value>
        public string Property
        {
            get { return this.property; }
        }
    }
}
