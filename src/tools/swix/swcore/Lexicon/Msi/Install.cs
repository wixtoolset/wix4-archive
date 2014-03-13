//-------------------------------------------------------------------------------------------------
// <copyright file="Install.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Msi
{
    using System;
    using System.Collections.Generic;
    using Lexicon = WixToolset.Simplified.Lexicon;

    public static class Install
    {
        private static Dictionary<PackageItem, string> ItemCondition = new Dictionary<PackageItem, string>();

        public static void SetCondition(Lexicon.PackageItem item, string condition)
        {
            Install.ItemCondition.Add(item, condition);
        }

        public static string GetCondition(Lexicon.PackageItem item)
        {
            string condition = null;
            if (Install.ItemCondition.TryGetValue(item, out condition))
            {
            }

            return condition;
        }
    }
}
