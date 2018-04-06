// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
