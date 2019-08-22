// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
