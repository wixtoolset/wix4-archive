// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
