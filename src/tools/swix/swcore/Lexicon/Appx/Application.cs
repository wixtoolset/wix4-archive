//-------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using System.Collections.Generic;
    using Lexicon = WixToolset.Simplified.Lexicon;

    public static class Application
    {
        private static Dictionary<Lexicon.Application, bool> AppToasts = new Dictionary<Lexicon.Application, bool>();

        public static void SetToastCapable(Lexicon.Application application, bool toast)
        {
            Application.AppToasts.Add(application, toast);
        }

        public static bool? GetToastCapable(Lexicon.Application application)
        {
            bool toast;
            if (Application.AppToasts.TryGetValue(application, out toast))
            {
                return toast;
            }

            return null;
        }
    }
}
