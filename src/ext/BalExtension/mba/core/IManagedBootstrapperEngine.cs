//-------------------------------------------------------------------------------------------------
// <copyright file="IManagedBootstrapperEngine.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Bootstrapper
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Text;

    public interface IManagedBootstrapperEngine
    {
        IVariables<long> NumericVariables { get; }
        int PackageCount { get; }
        IVariables<SecureString> SecureStringVariables { get; }
        IVariables<string> StringVariables { get; }
        IVariables<Version> VersionVariables { get; }

        void Apply(IntPtr hwndParent);
        void CloseSplashScreen();
        void Detect();
        void Detect(IntPtr hwndParent);
        bool Elevate(IntPtr hwndParent);
        string EscapeString(string input);
        bool EvaluateCondition(string condition);
        string FormatString(string format);
        void LaunchApprovedExe(IntPtr hwndParent, string approvedExeForElevationId, string arguments);
        void LaunchApprovedExe(IntPtr hwndParent, string approvedExeForElevationId, string arguments, int waitForInputIdleTimeout);
        void Log(LogLevel level, string message);
        void Plan(LaunchAction action);
        void SetUpdate(string localSource, string downloadSource, long size, UpdateHashType hashType, byte[] hash);
        void SetLocalSource(string packageOrContainerId, string payloadId, string path);
        void SetDownloadSource(string packageOrContainerId, string payloadId, string url, string user, string password);
        int SendEmbeddedError(int errorCode, string message, int uiHint);
        int SendEmbeddedProgress(int progressPercentage, int overallPercentage);
        void Quit(int exitCode);
    }
}
