// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
