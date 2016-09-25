// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Burn
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Win32;
    using WixTest.Verifiers;
    using WixToolset.Bootstrapper;
    using Xunit;

    public class TransactionTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs a bundle with an MSI transaction and then remove it.")]
        [RuntimeTest]
        public void Burn_InstallUninstall()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A").Build().Output;
            string packageB = new PackageBuilder(this, "B").Build().Output;
            string packageC = new PackageBuilder(this, "C").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", Path.GetDirectoryName(packageA));
            bindPaths.Add("packageB", Path.GetDirectoryName(packageB));
            bindPaths.Add("packageC", Path.GetDirectoryName(packageC));

            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            string packageASourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            string packageBSourceCodeInstalled = this.GetTestInstallFolder(@"B\B.wxs");
            string packageCSourceCodeInstalled = this.GetTestInstallFolder(@"C\C.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A payload should not be there on test start: ", packageASourceCodeInstalled));
            Assert.False(File.Exists(packageBSourceCodeInstalled), String.Concat("Package B payload should not be there on test start: ", packageBSourceCodeInstalled));
            Assert.False(File.Exists(packageCSourceCodeInstalled), String.Concat("Package C payload should not be there on test start: ", packageCSourceCodeInstalled));

            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));
            Assert.True(File.Exists(packageBSourceCodeInstalled), String.Concat("Should have found Package B payload installed at: ", packageBSourceCodeInstalled));
            Assert.True(File.Exists(packageCSourceCodeInstalled), String.Concat("Should have found Package C payload installed at: ", packageCSourceCodeInstalled));


            // Uninstall everything.
            install.Uninstall();

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageASourceCodeInstalled));
            Assert.False(File.Exists(packageBSourceCodeInstalled), String.Concat("Package B payload should have been removed by uninstall from: ", packageBSourceCodeInstalled));
            Assert.False(File.Exists(packageCSourceCodeInstalled), String.Concat("Package C payload should have been removed by uninstall from: ", packageCSourceCodeInstalled));

            this.Complete();
        }

        /// <summary>
        /// Installs 2 bundles:
        ///   bundleC- installs package E
        ///   bundleB- installes packages A, B, D
        ///     package B performs a major upgrade of package E
        ///     package D fails
        ///     Thus, rolling back the transaction should reinstall package E
        /// </summary>
        [NamedFact]
        [Priority(2)]
        [Description("Installs a bundle with an MSI transaction that should fail.")]
        [RuntimeTest]
        public void Burn_InstallRollback()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A").Build().Output;
            string packageB = new PackageBuilder(this, "B").Build().Output;
            string packageD = new PackageBuilder(this, "D").Build().Output;
            string packageE = new PackageBuilder(this, "E").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bundleBbindPaths = new Dictionary<string, string>();
            bundleBbindPaths.Add("packageA", Path.GetDirectoryName(packageA));
            bundleBbindPaths.Add("packageB", Path.GetDirectoryName(packageB));
            bundleBbindPaths.Add("packageD", Path.GetDirectoryName(packageD));

            Dictionary<string, string> bundleCbindPaths = new Dictionary<string, string>();
            bundleCbindPaths.Add("packageE", Path.GetDirectoryName(packageE));

            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bundleBbindPaths, Extensions = Extensions }.Build().Output;
            string bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bundleCbindPaths, Extensions = Extensions }.Build().Output;

            string packageASourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            string packageBSourceCodeInstalled = this.GetTestInstallFolder(@"B\B.wxs");
            string packageDSourceCodeInstalled = this.GetTestInstallFolder(@"D\D.wxs");
            string packageESourceCodeInstalled = this.GetTestInstallFolder(@"E\E.wxs");

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A payload should not be there on test start: ", packageASourceCodeInstalled));
            Assert.False(File.Exists(packageBSourceCodeInstalled), String.Concat("Package B payload should not be there on test start: ", packageBSourceCodeInstalled));
            Assert.False(File.Exists(packageDSourceCodeInstalled), String.Concat("Package D payload should not be there on test start: ", packageDSourceCodeInstalled));
            Assert.False(File.Exists(packageESourceCodeInstalled), String.Concat("Package E payload should not be there on test start: ", packageESourceCodeInstalled));

            BundleInstaller installC = new BundleInstaller(this, bundleC).Install();

            // Source file should be installed
            Assert.True(File.Exists(packageESourceCodeInstalled), String.Concat("Should have found Package E payload installed at: ", packageESourceCodeInstalled));

            // Source file should *not* be installed
            Assert.False(File.Exists(packageASourceCodeInstalled), String.Concat("Package A payload should not be there on test start: ", packageASourceCodeInstalled));
            Assert.False(File.Exists(packageBSourceCodeInstalled), String.Concat("Package B payload should not be there on test start: ", packageBSourceCodeInstalled));
            Assert.False(File.Exists(packageDSourceCodeInstalled), String.Concat("Package D payload should not be there on test start: ", packageDSourceCodeInstalled));

            // Bundle B should fail
            BundleInstaller installB = new BundleInstaller(this, bundleB).Install(1603);
           
            // Source file should be installed
            Assert.True(File.Exists(packageASourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageASourceCodeInstalled));
            Assert.True(File.Exists(packageESourceCodeInstalled), String.Concat("Should have found Package E payload that survived a failed major upgrade installed at: ", packageESourceCodeInstalled));

            // Source file should *not* be installed
            Assert.False(File.Exists(packageBSourceCodeInstalled), String.Concat("Package B payload should not be there on test start: ", packageBSourceCodeInstalled));
            Assert.False(File.Exists(packageDSourceCodeInstalled), String.Concat("Package D payload should not be there on test start: ", packageDSourceCodeInstalled));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Builds a bundle with an MSI transaction that has a x64 package after a x86 package.")]
        [RuntimeTest]
        public void Burn_x64Afterx86()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A").Build().Output;
            string packageB = new PackageBuilder(this, "B").Build().Output;
            string packageF = new PackageBuilder(this, "F").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", Path.GetDirectoryName(packageA));
            bindPaths.Add("packageB", Path.GetDirectoryName(packageB));
            bindPaths.Add("packageF", Path.GetDirectoryName(packageF));

            // Shoud ld fail build, x64 after x86 in transaction
            new BundleBuilder(this, "BundleD") { BindPaths = bindPaths, Extensions = Extensions, ExpectedLightExitCode = 390 }.Build();

            this.Complete();
        }
    }
}
