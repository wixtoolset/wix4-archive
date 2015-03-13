//-----------------------------------------------------------------------
// <copyright file="UtilExtension.WixShellExecTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension WixShellExec tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Util extension WixShellExec element tests
    /// </summary>
    public class WixShellExecTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\WixShellExecTests");
     
        [NamedFact]
        [Description("Verify that WixShellExec executes the expected command.")]
        [Priority(2)]
        [RuntimeTest]
        public void WixShellExec_Install()
        {
            string sourceFile = Path.Combine(WixShellExecTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"WixTestFolder\out.txt");
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(File.Exists(fileName), String.Format("Command was not executed. File '{0}' does not exist.", fileName));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
