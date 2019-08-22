// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using WixTest.Utilities;

    /// <summary>
    /// Fixture class for WiX tests.
    /// </summary>
    public class WixTestFixture : IDisposable
    {        
        // The name of the environment variable that stores the MSBuild directory.
        public static readonly string EnvWixTestMSBuildDirectory = "WixTestMSBuildDirectory";

        // The name of the environment variable that stores the WiX build output directory.
        public static readonly string EnvWixBuildPathDirectory = "WixBuildPathDirectory";

        // The name of the environment variable that stores the WiX bin directory.
        public static readonly string EnvWixToolsPath = "WixToolsPath";

        // The name of the environment variable that stores the wix.targets path.
        public static readonly string EnvWixTargetsPath = "WixTargetsPath";

        // The name of the environment variable that stores the WixTasks.dll path.
        public static readonly string EnvWixTasksPath = "WixTasksPath";

        // The name of the environment variable that stores the WIX_ROOT path.
        public static readonly string EnvWixRootPath = "WIX_ROOT";

        private static readonly string projectDirectory;
        private static readonly string seed;

        private static string originalWixRootValue;
        private static int references = 0;

        /// <summary>
        /// Directory containing wix.proj.
        /// </summary>
        public static string ProjectDirectory
        {
            get
            {
                return projectDirectory;
            }
        }

        /// <summary>
        /// Date time stamp.
        /// </summary>
        public static string Seed
        {
            get
            {
                return seed;
            }
        }

        /// <summary>
        /// Initializes the test base class.
        /// </summary>
        public WixTestFixture()
        {
            // Ideally this logic would be in an AssemblyInitialization method, but Xunit doesn't have that feature.
            if (1 == Interlocked.Increment(ref references))
            {
                WixTestFixture.originalWixRootValue = Environment.GetEnvironmentVariable(WixTestFixture.EnvWixRootPath);
                Environment.SetEnvironmentVariable(WixTestFixture.EnvWixRootPath, WixTestFixture.projectDirectory);
            }
        }

        public void Dispose()
        {
            // Ideally this logic would be in an AssemblyUninitialization method, but Xunit doesn't have that feature.
            if (0 == Interlocked.Decrement(ref WixTestFixture.references))
            {
                Environment.SetEnvironmentVariable(WixTestFixture.EnvWixRootPath, WixTestFixture.originalWixRootValue);
            }
        }
        
        /// <summary>
        /// Initialize static variables and settings.
        /// </summary>
        static WixTestFixture()
        {
            WixTestFixture.seed = DateTime.Now.ToString("yyyy-MM-ddTHH.mm.ss");
            WixTestFixture.projectDirectory = FileUtilities.GetDirectoryNameOfFileAbove("wix.proj");

            Settings.Seed = WixTestFixture.seed;

            // Best effort to locate MSBuild.
            IEnumerable<string> msbuildDirectories = new string[]
            {
                Environment.GetEnvironmentVariable(WixTestFixture.EnvWixTestMSBuildDirectory),
                Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), @"Microsoft.NET\Framework\v4.0.30319"),
                Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), @"Microsoft.NET\Framework\v3.5"),
            };

            foreach (string msbuildDirectory in msbuildDirectories)
            {
                if (!String.IsNullOrEmpty(msbuildDirectory) && Directory.Exists(msbuildDirectory))
                {
                    Settings.MSBuildDirectory = msbuildDirectory;
                    break;
                }
            }

            // Set the directory for the build output.
            Settings.WixBuildDirectory = Environment.GetEnvironmentVariable(WixTestFixture.EnvWixBuildPathDirectory) ?? Environment.CurrentDirectory;
            Settings.WixToolsDirectory = Environment.GetEnvironmentVariable(WixTestFixture.EnvWixToolsPath) ?? Environment.CurrentDirectory;

            // Set the locations of wix.targets and wixtasks.dll using the build output as default.
            string path = Environment.GetEnvironmentVariable(WixTestFixture.EnvWixTargetsPath);
            if (String.IsNullOrEmpty(path))
            {
                path = Path.Combine(Settings.WixToolsDirectory, "wix.targets");
            }

            if (File.Exists(path))
            {
                Settings.WixTargetsPath = path;
            }
            else
            {
                Console.WriteLine("The environment variable '{0}' was not set. The location for wix.targets will not be explicitly specified to MSBuild.", WixTestFixture.EnvWixTargetsPath);
            }

            path = Environment.GetEnvironmentVariable(WixTestFixture.EnvWixTasksPath);
            if (String.IsNullOrEmpty(path))
            {
                path = Path.Combine(Settings.WixToolsDirectory, "WixTasks.dll");
            }

            if (File.Exists(path))
            {
                Settings.WixTasksPath = path;
            }
            else
            {
                Console.WriteLine("The environment variable '{0}' was not set. The location for WixTasks.dll will not be explicitly specified to MSBuild.", WixTestFixture.EnvWixTasksPath);
            }
        }
    }
}
