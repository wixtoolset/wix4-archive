//-------------------------------------------------------------------------------------------------
// <copyright file="Target.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
