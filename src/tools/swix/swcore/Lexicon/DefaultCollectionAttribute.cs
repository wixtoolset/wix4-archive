//-------------------------------------------------------------------------------------------------
// <copyright file="DefaultCollectionAttribute.cs" company="Outercurve Foundation">
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
    /// Represents a custom attribute for declaring the default collection in a class.
    /// </summary>
    [AttributeUsageAttribute(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DefaultCollectionPropertyAttribute : Attribute
    {
        private readonly string collection;

        /// <summary>
        /// Instantiate a new DefaultCollectionAttribute.
        /// </summary>
        /// <param name="collectionName">The name of the default collection in a class.</param>
        public DefaultCollectionPropertyAttribute(string collectionName)
        {
            this.collection = collectionName;
        }

        /// <summary>
        /// Gets the name of the default collection in a class.
        /// </summary>
        /// <value>The name of the default collection in a class.</value>
        public string Collection
        {
            get { return this.collection; }
        }
    }
}
