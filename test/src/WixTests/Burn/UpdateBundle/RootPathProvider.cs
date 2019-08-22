// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Burn.UpdateBundle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Nancy;

    public class RootPathProvider : IRootPathProvider
    {
        static public string RootPath { get; set; }
        public string GetRootPath()
        {
            return RootPathProvider.RootPath;
        }
    }
}
