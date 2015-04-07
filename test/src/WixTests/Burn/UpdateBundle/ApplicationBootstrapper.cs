//-----------------------------------------------------------------------
// <copyright file="ApplicationBootstrapper.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains ApplicationBootstrapper to test update bundles in Burn.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Burn.UpdateBundle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Nancy;
    using Nancy.Bootstrapper;
    using Nancy.Conventions;
    using Nancy.Diagnostics;
    using Nancy.TinyIoc;

    public class ApplicationBootstrapper : Nancy.DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            StaticConfiguration.EnableRequestTracing = true;
        }

        protected override IRootPathProvider RootPathProvider
        {
            get { return new RootPathProvider(); }
        }

        /* If you are having problems getting nancy working locally, uncomment this to get to the default _Nancy web management interface */
        protected override DiagnosticsConfiguration DiagnosticsConfiguration
        {
            get { return new DiagnosticsConfiguration { Password = @"wix" }; }
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feed/1.0", "/1.0/FeedBv1.0.xml"));
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feed/2.0", "/2.0/FeedBv1.1.xml"));
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/1.0/BundleB.exe", "/1.0/BundleB.exe"));
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/2.0/BundleB.exe", "/2.0/BundleB.exe"));
            base.ConfigureConventions(nancyConventions);
        }
    }
}
