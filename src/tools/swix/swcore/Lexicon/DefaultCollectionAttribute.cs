// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
