// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon
{
    using System;

    /// <summary>
    /// Attribute on a class.
    /// </summary>
    public class ClassAttribute
    {
        public string Name { get; set; }

        public ClassAttributeType Type { get; set; }

        public string Value { get; set; }
    }
}
