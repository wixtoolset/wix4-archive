//-----------------------------------------------------------------------
// <copyright file="FeedModule.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains FeedModule to test update bundles in Burn.
// </summary>
//-----------------------------------------------------------------------

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
