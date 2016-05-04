// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Burn.UpdateBundle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Nancy;
    using Nancy.Responses;

    public class FeedModule : NancyModule
    {
        public enum UpdateFeedBehavior
        {
            None,
            Invalid,
            Version1,
            Version2
        }

        public static UpdateFeedBehavior FeedBehavior { get; set; }

        public FeedModule()
        {
            switch (FeedModule.FeedBehavior)
            {
                case UpdateFeedBehavior.None:
                    break;
                case UpdateFeedBehavior.Invalid:
                    Get["/BundleB/feed"] = x =>
                    {
                        return Response.AsFile("1.0/BundleB.exe");
                    };
                    break;
                case UpdateFeedBehavior.Version1:
                    Get["/BundleB/feed"] = x =>
                    {
                        return Response.AsFile("1.0/FeedBv1.0.xml");
                    };
                    break;
                case UpdateFeedBehavior.Version2:
                    Get["/BundleB/feed"] = x =>
                    {
                        return Response.AsFile("2.0/FeedBv2.0.xml");
                    };
                    break;
            }
        }
    }
}
