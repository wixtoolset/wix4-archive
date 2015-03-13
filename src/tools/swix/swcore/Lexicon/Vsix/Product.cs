//-------------------------------------------------------------------------------------------------
// <copyright file="Product.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Vsix
{
    using System;
    using System.Collections.Generic;
    using Lexicon = WixToolset.Simplified.Lexicon;

    public static class Product
    {
        private static Dictionary<Lexicon.Prerequisite, string> PrerequisiteEdition = new Dictionary<Lexicon.Prerequisite, string>();

        public static void SetEdition(Lexicon.Prerequisite prerequisite, string edition)
        {
            Product.PrerequisiteEdition.Add(prerequisite, edition);
        }

        public static string GetEdition(Lexicon.Prerequisite prerequisite)
        {
            string edition = null;
            if (Product.PrerequisiteEdition.TryGetValue(prerequisite, out edition))
            {
            }

            return edition;
        }
    }
}
