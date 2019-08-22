// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using Xunit;
    using Xunit.Sdk;

    /// <summary>
    /// Base class for WiX tests.
    /// </summary>
    //Xunit v2 version
    //public abstract class WixTestBase : ITestClass, IClassFixture<WixTestFixture>
    public abstract class WixTestBase : ITestClass, IUseFixture<WixTestFixture>
    {
        /// <summary>
        /// Common extensions for building packages and bundles.
        /// </summary>
        protected static readonly string[] Extensions = new string[]
        {
            "WixBalExtension",
            "WixDependencyExtension",
            "WixIIsExtension",
            "WixTagExtension",
            "WixUtilExtension",
        };

        private bool cleanArtifacts;
        private WixTestFixture testFixture;
        private Stack<string> currentDirectories = new Stack<string>();

        /// <summary>
        /// Initializes the test base class.
        /// </summary>
        //Xunit v2 version. This means that all subclasses will also need to have a WixTestFixture parameter and pass it along.
        //public WixTestBase(WixTestFixture testFixture)
        public WixTestBase()
        {
        }

        /// <summary>
        /// A list of test artifacts for the current test.
        /// </summary>
        public List<FileSystemInfo> TestArtifacts { get; private set; }

        /// <summary>
        /// The test context for the current test.
        /// </summary>
        public WixTestContext TestContext { get; private set; }

        /// <summary>
        /// Called by a test case to indicate the test is completed and test artifacts can be cleaned up.
        /// </summary>
        protected void Complete()
        {
            this.cleanArtifacts = true;
        }

        /// <summary>
        /// Gets the test install directory for the current test.
        /// </summary>
        /// <param name="additionalPath">Additional subdirectories under the test install directory.</param>
        /// <returns>Full path to the test install directory.</returns>
        /// <remarks>
        /// The package or bundle must install into [ProgramFilesFolder]\~Test WiX\[TestName]\([Additional]).
        /// </remarks>
        protected string GetTestInstallFolder(string additionalPath = null)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "~Test WiX", this.TestContext.TestName, additionalPath ?? String.Empty);
        }

        /// <summary>
        /// Gets the test registry key for the current test.
        /// </summary>
        /// <param name="additionalPath">Additional subkeys under the test registry key.</param>
        /// <returns>Full path to the test registry key.</returns>
        /// <remarks>
        /// The package must write into HKLM\Software\WiX\Tests\[TestName]\([Additional]).
        /// </remarks>
        protected RegistryKey GetTestRegistryRoot(string additionalPath = null)
        {
            string key = String.Format(@"Software\WiX\Tests\{0}\{1}", this.TestContext.TestName, additionalPath ?? String.Empty);
            return Registry.LocalMachine.OpenSubKey(key, true);
        }

        void IUseFixture<WixTestFixture>.SetFixture(WixTestFixture testFixture)
        {
            // IUseFixture<T> is removed in Xunit v2.
            // It has been replaced with IClassFixture<T>.
            // The T object will be injected into the constructor, which means this method needs to move into the constructor.
            // Because Xunit creates a new instance of the test class for every test method, class level initialization should be done in the constructor of the Fixture class.
            // Class level uninitialization should be done in the Dispose method of the Fixture class.
            this.testFixture = testFixture;
        }

        /// <summary>
        /// Initializes a single test case.
        /// </summary>
        protected virtual void TestInitialize()
        {
        }

        /// <summary>
        /// Uninitializes a single test case.
        /// </summary>
        protected virtual void TestUninitialize()
        {
        }

        /// <summary>
        /// Initializes a single test case.
        /// </summary>
        public virtual void TestInitialize(string testNamespace, string testClass, string testMethodName)
        {
            WixTestContext context = new WixTestContext();
            context.Seed = WixTestFixture.Seed;
            context.TestName = testMethodName;
            context.TestDirectory = Path.Combine(Path.GetTempPath(), "wix_tests", context.Seed, context.TestName);

            Directory.CreateDirectory(context.TestDirectory);

            // Make sure we can resolve to our test data directory.
            string path = Environment.GetEnvironmentVariable(WixTestFixture.EnvWixRootPath) ?? WixTestFixture.ProjectDirectory;
            if (!String.IsNullOrEmpty(path))
            {
                path = Path.Combine(path, @"test\data\");
            }
            else
            {
                throw new InvalidOperationException(String.Format("The {0} environment variable is not defined. The current test case cannot continue.", WixTestFixture.EnvWixRootPath));
            }

            // Always store the root test data directory for those tests that need it.
            context.DataDirectory = path;

            // Special handling for the WixTest project's tests.
            if (testNamespace.StartsWith("WixTest.Tests."))
            {
                path = Path.Combine(path, testNamespace.Substring("WixTest.Tests.".Length).Replace('.', '\\'), testClass);
            }

            context.TestDataDirectory = path;

            this.TestArtifacts = new List<FileSystemInfo>();
            this.TestArtifacts.Add(new DirectoryInfo(context.TestDirectory));

            // Keep track of the current directory stack and change to the current test directory.
            this.currentDirectories.Push(Directory.GetCurrentDirectory());
            Directory.SetCurrentDirectory(context.TestDirectory);

            this.TestContext = context;

            this.TestInitialize();
        }

        /// <summary>
        /// Uninitializes a single test case.
        /// </summary>
        public virtual void TestUninitialize(MethodResult result)
        {
            this.TestUninitialize();

            BundleBuilder.CleanupByUninstalling();
            PackageBuilder.CleanupByUninstalling();
            MSIExec.UninstallAllInstalledProducts();

            MsiVerifier.Reset();

            this.ResetRegistry();
            this.ResetDirectory();

            if (this.cleanArtifacts)
            {
                foreach (FileSystemInfo artifact in this.TestArtifacts)
                {
                    if (artifact.Exists)
                    {
                        try
                        {
                            DirectoryInfo dir = artifact as DirectoryInfo;
                            if (null != dir)
                            {
                                dir.Delete(true);
                            }
                            else
                            {
                                artifact.Delete();
                            }
                        }
                        catch
                        {
                            Debug.WriteLine(String.Format("Failed to delete '{0}'.", artifact.FullName));
                        }
                    }
                }
            }
        }

        private void ResetDirectory()
        {
            if (0 < this.currentDirectories.Count)
            {
                string path = this.currentDirectories.Pop();
                if (!String.IsNullOrEmpty(path))
                {
                    Directory.SetCurrentDirectory(path);
                }
            }
        }

        private void ResetRegistry()
        {
            if (null != this.TestContext)
            {
                string key = String.Format(@"Software\WiX\Tests\{0}", this.TestContext.TestName);
                Registry.LocalMachine.DeleteSubKeyTree(key, false);
            }

            Registry.LocalMachine.DeleteSubKeyTree(@"Software\WiX\Tests\TestBAControl", false);
        }
    }
}
