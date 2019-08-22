// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.VSExtension
{
    using Microsoft.Win32;
    using System;
    using System.IO;

    public class VSExtensionFixture : IDisposable
    {
        private static readonly string DevenvRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS\";
        private static readonly string DevenvRegistryValueName = @"EnvironmentPath";

        private string devenvOriginalLocation;

        public string OutputFileName { get; private set; }

        public VSExtensionFixture()
        {
            // Create a new command file.
            string commandFileName = Path.Combine(Path.GetTempPath(), "stubdevenv.cmd");
            this.OutputFileName = Utilities.FileUtilities.GetUniqueFileName();
            File.WriteAllText(commandFileName, string.Format("echo %* > {0}", this.OutputFileName));

            // Backup the original devenv.exe registry key first.
            devenvOriginalLocation = (string)Registry.GetValue(VSExtensionFixture.DevenvRegistryKey, VSExtensionFixture.DevenvRegistryValueName, string.Empty);

            // Replace the devenv.exe registry key with the new command file.
            Registry.SetValue(VSExtensionFixture.DevenvRegistryKey, VSExtensionFixture.DevenvRegistryValueName, commandFileName);
        }

        public void Dispose()
        {
            // Replace the devenv.exe registry key with the original file.
            Registry.SetValue(VSExtensionFixture.DevenvRegistryKey, VSExtensionFixture.DevenvRegistryValueName, devenvOriginalLocation);
        }
    }
}
