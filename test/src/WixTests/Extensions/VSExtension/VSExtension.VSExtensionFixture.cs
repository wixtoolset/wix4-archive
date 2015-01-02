//-----------------------------------------------------------------------
// <copyright file="VSExtension.VSExtensionFixture.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

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
