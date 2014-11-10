//-----------------------------------------------------------------------
// <copyright file="Burn.UpdateBundleTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains methods to test update bundles in Burn.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Burn
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Microsoft.Win32;
    using Nancy;
    using Nancy.Bootstrapper;
    using Nancy.Conventions;
    using Nancy.Diagnostics;
    using Nancy.Hosting.Self;
    using Nancy.Responses;
    using Nancy.TinyIoc;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using WixToolset.Dtf.WindowsInstaller;
    using Xunit;

    public class UpdateBundleTests : BurnTests
    {
        private const string V2 = "2.0.0.0";

        private WixTest.PackageBuilder packageA;
        private WixTest.PackageBuilder packageAv2;
        private WixTest.BundleBuilder bundleA;
        private WixTest.BundleBuilder bundleAv2;
        private WixTest.PackageBuilder packageB;
        private WixTest.PackageBuilder packageBv2;
        private WixTest.BundleBuilder bundleB;
        private WixTest.BundleBuilder bundleBv2;

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0.")]
        [RuntimeTest]
        public void Burn_InstallUpdatedBundle()
        {
            // Build the packages.
            string packageA1 = this.GetPackageA().Output;
            string packageA2 = this.GetPackageAv2().Output;

            // Build the bundles.
            string bundleA1 = this.GetBundleA().Output;
            string bundleA2 = this.GetBundleAv2().Output;

            // Install the v1 bundle.
            BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install(arguments: String.Concat("\"", "-updatebundle:", bundleA2, "\""));
            BundleInstaller installerA2 = new BundleInstaller(this, bundleA2);

            // Test that only the newest packages is installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall bundleA2.
            installerA2.Uninstall();

            // Test all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Av1.0 then does an update to bundle Av2.0 during modify.")]
        [RuntimeTest]
        public void Burn_UpdateInstalledBundle()
        {
            // Build the packages.
            string packageA1 = this.GetPackageA().Output;
            string packageA2 = this.GetPackageAv2().Output;

            // Build the bundles.
            string bundleA1 = this.GetBundleA().Output;
            string bundleA2 = this.GetBundleAv2().Output;

            // Install the v1 bundle.
            BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install();

            // Test that v1 was correctly installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA2));

            // Run the v1 bundle providing an update bundle.
            installerA1.Modify(arguments: String.Concat("\"", "-updatebundle:", bundleA2, "\""));

            // Test that only v2 packages is installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall v2.
            BundleInstaller installerA2 = new BundleInstaller(this, bundleA2).Uninstall();

            // Test all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0 and verifies arguments are passed through the whole way.")]
        [RuntimeTest]
        public void Burn_InstallUpdatedBundleVerifyArguments()
        {
            const string verifyArguments = "these arguments should exist";

            // Build the packages.
            string packageA1 = this.GetPackageA().Output;
            string packageA2 = this.GetPackageAv2().Output;

            // Build the bundles.
            string bundleA1 = this.GetBundleA().Output;
            string bundleA2 = this.GetBundleAv2().Output;

            this.SetBurnTestValue(BurnTests.TestValueVerifyArguments, verifyArguments);

            // Install the v1 bundle.
            BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install(arguments: String.Concat("\"", "-updatebundle:", bundleA2, "\" ", verifyArguments));
            BundleInstaller installerA2 = new BundleInstaller(this, bundleA2);

            // Test that only the newest packages is installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall bundleA2 without the verify arguments passed and expect failure code.
            installerA2.Uninstall(expectedExitCode: -1);

            // Remove the required arguments and uninstall again.
            this.SetBurnTestValue(BurnTests.TestValueVerifyArguments, null);
            installerA2.Uninstall();

            // Test all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0.  Verifies the OptionalUpdateRegistration Element is correct for both installs")]
        [RuntimeTest]
        public void Burn_InstallUpdatedBundleOptionalUpdateRegistration()
        {
            string v2Version = "2.0.0.0";

            // Build the packages.
            string packageAv1 = new PackageBuilder(this, "A").Build().Output;
            string packageAv2 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", v2Version } } }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPathsv1 = new Dictionary<string, string>() { { "packageA", packageAv1 } };
            Dictionary<string, string> bindPathsv2 = new Dictionary<string, string>() { { "packageA", packageAv2 } };

            // Build the bundles.
            string bundleAv1 = new BundleBuilder(this, "BundleA") { BindPaths = bindPathsv1, Extensions = Extensions }.Build().Output;
            string bundleAv2 = new BundleBuilder(this, "BundleA") { BindPaths = bindPathsv2, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", v2Version } } }.Build().Output;

            // Initialize with first bundle.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleAv1).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv1));

            // Make sure the OptionalUpdateRegistration exists.
            // SOFTWARE\[Manufacturer]\Updates\[ProductFamily]\[Name]
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.Equal("Y", key.GetValue("ThisVersionInstalled"));
                Assert.Equal("1.0.0.0", key.GetValue("PackageVersion"));
            }

            // Install second bundle which will major upgrade away v1.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            // Make sure the OptionalUpdateRegistration exists.
            // SOFTWARE\[Manufacturer]\Updates\[ProductFamily]\[Name]
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.Equal("Y", key.GetValue("ThisVersionInstalled"));
                Assert.Equal("2.0.0.0", key.GetValue("PackageVersion"));
            }

            // Uninstall the second bundle and everything should be gone.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            // Make sure the key is removed.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.Null(key);
            }
        }

        
        //public class MainModule : Nancy.NancyModule
        //{
        //    //static public WixTestContext TestContext { get; set; }

        //    public MainModule()
        //    {
        //        Get["/"] = x =>
        //        {
        //            return "Hello WiX!";
        //        };
        //        Get["/BundleB/feed"] = X =>
        //        {
        //            //return new GenericFileResponse(Path.Combine(MainModule.TestContext.TestDataDirectory, "feedB.xml"));
        //            return new GenericFileResponse("feedB.xml");
        //        };
        //        Get["/BundleB/BundleB.exe"] = X =>
        //        {
        //            //return new GenericFileResponse(Path.Combine(MainModule.TestContext.TestDirectory, "BundleB.exe"));
        //            return new GenericFileResponse("BundleB.exe");
        //        };
        //    }
        //}



        public class RootPathProvider : IRootPathProvider
        {
            static public string RootPath { get; set; }
            public string GetRootPath()
            {
                return RootPathProvider.RootPath;
            }
        }

        public class ApplicationBootstrapper : Nancy.DefaultNancyBootstrapper
        {
            //public enum UpdateFeedBehavior
            //{
            //    None,
            //    Invalid,
            //    Version1,
            //    Version2
            //}

            //private readonly UpdateFeedBehavior updateFeedBehavior;

            public ApplicationBootstrapper(/*UpdateFeedBehavior feedBehavior*/)
            {
                //this.updateFeedBehavior = feedBehavior;
            }

            protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
            {
                base.ApplicationStartup(container, pipelines);

                StaticConfiguration.EnableRequestTracing = true;
                StaticConfiguration.Caching.EnableRuntimeViewUpdates = true;
            }

            protected override IRootPathProvider RootPathProvider
            {
                get { return new RootPathProvider(); }
            }

            /* If you are having problems getting nancy working locally, uncomment this to get to the default _Nancy web management interface */
            protected override DiagnosticsConfiguration DiagnosticsConfiguration
            {
                get { return new DiagnosticsConfiguration { Password = @"password" }; }
            }

            protected override void ConfigureConventions(NancyConventions nancyConventions)
            {
                //switch(this.updateFeedBehavior)
                //{
                //    case UpdateFeedBehavior.None:
                //        break;
                //    case UpdateFeedBehavior.Invalid:
                //        nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feed", "/1.0/BundleB.exe"));
                //        break;
                //    case UpdateFeedBehavior.Version1:
                //        nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feed", "/1.0/FeedBv1.0.xml"));
                //        break;
                //    case UpdateFeedBehavior.Version2:
                //        nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feed", "/1.1/FeedBv1.1.xml"));
                //        break;
                //}
                //nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feedz/1.0", "/1.0/FeedBv1.0.xml"));
                //nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feedz/1.1", "/1.1/FeedBv1.1.xml"));

                nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/1.0/BundleB.exe", "/1.0/BundleB.exe"));
                nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/1.1/BundleB.exe", "/1.1/BundleB.exe"));
                base.ConfigureConventions(nancyConventions);
            }
        }


        public class FeedModule: Nancy.NancyModule
        {
            public enum UpdateFeedBehavior
            {
                None,
                Invalid,
                Version1,
                Version2
            }

            public static UpdateFeedBehavior FeedBehavior {get;set;}

            public FeedModule()
            {
                switch (FeedModule.FeedBehavior)
                {
                    case UpdateFeedBehavior.None:
                        break;
                    case UpdateFeedBehavior.Invalid:
                        Get["/BundleB/feed"] = x => {
                            //return new GenericFileResponse("1.0/BundleB.exe");
                            return Response.AsFile("1.0/BundleB.exe");
                        };
                        //nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feed", "/1.0/BundleB.exe"));
                        break;
                    case UpdateFeedBehavior.Version1:
                        //nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feed", "/1.0/FeedBv1.0.xml"));
                        Get["/BundleB/feed"] = x =>
                        {
                            //return new GenericFileResponse("1.0/FeedBv1.0.xml");
                            return Response.AsFile("1.0/FeedBv1.0.xml");
                        };
                        break;
                    case UpdateFeedBehavior.Version2:
                        //nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddFile("/BundleB/feed", "/1.1/FeedBv1.1.xml"));
                        Get["/BundleB/feed"] = x =>
                        {
                            return Response.AsFile("1.1/FeedBv1.1.xml");
                            //return new GenericFileResponse("1.1/FeedBv1.1.xml");
                        };
                        break;
                }
            }
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Bv1.0 then does an update to bundle Bv2.0 during modify.")]
        [RuntimeTest(NonPrivileged = true)]
        public void Burn_UpdateInstalledPerUserBundle()
        {
            // Build the packages.
            string packageB1 = this.GetPackageB().Output;
            string packageB2 = this.GetPackageBv2().Output;

            // Build the bundles.
            string bundleB1 = this.GetBundleB().Output;
            string bundleB2 = this.GetBundleBv2().Output;
            
            // Install the v1 bundle.
            BundleInstaller installerB1 = new BundleInstaller(this, bundleB1).Install();

            // Test that v1 was correctly installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB2));

            // Run the v1 bundle requesting an update bundle.

            installerB1.Modify(arguments: new string[]{"-checkupdate", "-expectNoUpdate"});
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB2));

            HostConfiguration hostConfigs = new HostConfiguration()
            {
                UrlReservations = new UrlReservations() { CreateAutomatically = true },
                AllowChunkedEncoding = false // https://github.com/NancyFx/Nancy/issues/1337
            };

            Directory.CreateDirectory(Path.Combine(this.TestContext.TestDirectory, "1.0"));
            Directory.CreateDirectory(Path.Combine(this.TestContext.TestDirectory, "1.1"));

            // Copy v1.0 artifacts to the TestDataDirectory
            File.Copy(bundleB1, Path.Combine(this.TestContext.TestDirectory, "1.0", Path.GetFileName(bundleB1)), true);
            File.Copy(Path.Combine(this.TestContext.TestDataDirectory, "FeedBv1.0.xml"), Path.Combine(this.TestContext.TestDirectory, "1.0", "FeedBv1.0.xml"), true);

            // Copy v1.1 artifacts to the TestDataDirectory
            File.Copy(bundleB2, Path.Combine(this.TestContext.TestDirectory, "1.1", Path.GetFileName(bundleB2)), true);
            File.Copy(Path.Combine(this.TestContext.TestDataDirectory, "FeedBv1.1.xml"), Path.Combine(this.TestContext.TestDirectory, "1.1", "FeedBv1.1.xml"), true);
            RootPathProvider.RootPath = this.TestContext.TestDirectory;

            FeedModule.FeedBehavior = FeedModule.UpdateFeedBehavior.None;
            // Verify bundle asking for update and getting a 404 doesn't update and doesn't modify state
            using (NancyHost nancyHost = new NancyHost(new Uri("http://localhost:9999/wix4/"), new ApplicationBootstrapper(), hostConfigs) { })
            {

                nancyHost.Start();

                // Run the v1 bundle providing an update bundle.
                installerB1.Modify(arguments: new string[] { "-checkupdate", "-expectNoUpdate" });

                // The modify -> update is asynchronous, so we need to wait until the real BundleB is done
                System.Diagnostics.Process[] childBundles = System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(bundleB2));
                foreach (var childBundle in childBundles)
                {
                    childBundle.WaitForExit();
                }
            }
            // Test that only v1 packages is installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB2));

            // Verify bundle asking for update and getting a current feed doesn't update and doesn't modify state
            FeedModule.FeedBehavior = FeedModule.UpdateFeedBehavior.Version1;
            using (NancyHost nancyHost = new NancyHost(new Uri("http://localhost:9999/wix4/"), new ApplicationBootstrapper(), hostConfigs) { })
            {

                nancyHost.Start();

                // Run the v1 bundle providing an update bundle.
                installerB1.Modify(arguments: new string[] { "-checkupdate"});

                // The modify -> update is asynchronous, so we need to wait until the real BundleB is done
                System.Diagnostics.Process[] childBundles = System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(bundleB2));
                foreach (var childBundle in childBundles)
                {
                    childBundle.WaitForExit();
                }
            }
            // Test that only v1 packages is installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB2));

            // Verify bundle asking for update and getting an updated feed updates
            FeedModule.FeedBehavior = FeedModule.UpdateFeedBehavior.Version2;
            using (NancyHost nancyHost = new NancyHost(new Uri("http://localhost:9999/wix4/"), new ApplicationBootstrapper(), hostConfigs) { })
            {

                nancyHost.Start();
                
                // Run the v1 bundle providing an update bundle.
                installerB1.Modify(arguments: "-checkupdate");

                // The modify -> update is asynchronous, so we need to wait until the real BundleB is done
                System.Diagnostics.Process[] childBundles = System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(bundleB2));
                foreach (var childBundle in childBundles)
                {
                    childBundle.WaitForExit();
                }
            }
            // Test that only v2 packages is installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB2));

            // Attempt to uninstall v2.
            BundleInstaller installerB2 = new BundleInstaller(this, bundleB2).Uninstall();

            // Test all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB2));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }


        private WixTest.PackageBuilder GetPackageA()
        {
            return (null != this.packageA) ? this.packageA : this.packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build();
        }

        private WixTest.PackageBuilder GetPackageAv2()
        {
            return (null != this.packageAv2) ? this.packageAv2 : this.packageAv2 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
        }

        private WixTest.BundleBuilder GetBundleA(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageA().Output;
                bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };
            }

            return (null != this.bundleA) ? this.bundleA : this.bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build();
        }

        private WixTest.BundleBuilder GetBundleAv2(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageAv2().Output;
                bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };
            }

            return (null != this.bundleAv2) ? this.bundleAv2 : this.bundleAv2 = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
        }

        private WixTest.PackageBuilder GetPackageB()
        {
            return (null != this.packageB) ? this.packageB : this.packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build();
        }

        private WixTest.PackageBuilder GetPackageBv2()
        {
            return (null != this.packageBv2) ? this.packageBv2 : this.packageBv2 = new PackageBuilder(this, "B") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
        }

        private WixTest.BundleBuilder GetBundleB(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageB().Output;
                bindPaths = new Dictionary<string, string>() { { "packageB", packageAPath } };
            }

            return (null != this.bundleB) ? this.bundleB : this.bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build();
        }

        private WixTest.BundleBuilder GetBundleBv2(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageBPath = this.GetPackageBv2().Output;
                bindPaths = new Dictionary<string, string>() { { "packageB", packageBPath } };
            }

            return (null != this.bundleBv2) ? this.bundleBv2 : this.bundleBv2 = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
        }
    }
}
