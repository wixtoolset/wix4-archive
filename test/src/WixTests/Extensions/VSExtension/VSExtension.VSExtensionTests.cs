// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.VSExtension
{
    using System;
    using System.IO;
    using WixTest;
    using Xunit;

    /// <summary>
    /// NetFX extension VSSetup element tests
    /// </summary>
    public class VSExtensionTests : WixTests, IUseFixture<VSExtensionFixture>
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\VSExtension\VSExtensionTests");

        private VSExtensionFixture fixture;

        public VSExtensionTests()
        {
        }

        public void SetFixture(VSExtensionFixture fixture)
        {
            this.fixture = fixture;
        }

        [NamedFact]
        [Description("Verify that the project templates are installed to the correct folder on install.")]
        [Priority(2)]
        [RuntimeTest]
        public void VS90InstallVSTemplates_Install()
        {
            string sourceFile = Path.Combine(VSExtensionTests.TestDataDirectory, @"VS90InstallVSTemplates.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixVSExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(File.Exists(this.fixture.OutputFileName), "devenv.exe was not called");
            string actualParamters = File.ReadAllText(this.fixture.OutputFileName).Trim();
            string expectedParamters = "/InstallVSTemplates";
            Assert.True(actualParamters.ToLowerInvariant().Equals(expectedParamters.ToLowerInvariant()), String.Format("devenv.exe was not called with the expected parameters. Actual: '{0}'. Expected '{1}'.", actualParamters, expectedParamters));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [NamedFact]
        [Description("Verify that the files are installed to the correct folder on install.")]
        [Priority(2)]
        [RuntimeTest]
        public void VSSetup_Install()
        {
            string sourceFile = Path.Combine(VSExtensionTests.TestDataDirectory, @"VS90Setup.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixVSExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(File.Exists(this.fixture.OutputFileName), "devenv.exe was not called");
            string actualParamters = File.ReadAllText(this.fixture.OutputFileName).Trim();
            string expectedParamters = "/setup";
            Assert.True(actualParamters.ToLowerInvariant().Equals(expectedParamters.ToLowerInvariant()), String.Format("devenv.exe was not called with the expected parameters. Actual: '{0}'. Expected '{1}'.", actualParamters, expectedParamters));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        protected override void TestUninitialize()
        {
            File.Delete(this.fixture.OutputFileName);

            base.TestUninitialize();
        }
    }
}
