// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Nuget
{
    using System;
    using System.Collections.Generic;
    using Lexicon = WixToolset.Simplified.Lexicon;

    public static class Target
    {
        private static Dictionary<Lexicon.Dependency, string> DependencyFramework = new Dictionary<Lexicon.Dependency, string>();
        private static Dictionary<Lexicon.Prerequisite, string> PrerequisiteFramework = new Dictionary<Lexicon.Prerequisite, string>();

        public static void SetFramework(Lexicon.Prerequisite prerequisite, string framework)
        {
            Target.PrerequisiteFramework.Add(prerequisite, framework);
        }

        public static string GetFramework(Lexicon.Prerequisite prerequisite)
        {
            string framework = null;
            if (Target.PrerequisiteFramework.TryGetValue(prerequisite, out framework))
            {
            }

            return framework;
        }
    }
}
