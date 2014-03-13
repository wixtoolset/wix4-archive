//-------------------------------------------------------------------------------------------------
// <copyright file="ClassAttribute.cs" company="Outercurve Foundation">
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
    /// Attribute on a class.
    /// </summary>
    public class ClassAttribute
    {
        public string Name { get; set; }

        public ClassAttributeType Type { get; set; }

        public string Value { get; set; }
    }
}
