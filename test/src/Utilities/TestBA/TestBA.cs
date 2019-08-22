// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.BA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using WixToolset.Bootstrapper;
    using Microsoft.Win32;

    /// <summary>
    /// A minimal UX used for testing.
    /// </summary>
    public class TestBA : BootstrapperApplication
    {
        private const string BurnBundleVersionVariable = "WixBundleVersion";

        private LaunchAction action;
        private ManualResetEvent wait;
        private int result;

        private string updateBundlePath;

        private int sleepDuringCache;
        private int cancelCacheAtProgress;
        private int sleepDuringExecute;
        private int cancelExecuteAtProgress;
        private int retryExecuteFilesInUse;

        /// <summary>
        /// Initializes test user experience.
        /// </summary>
        public TestBA()
        {
            this.wait = new ManualResetEvent(false);
        }

        /// <summary>
        /// Get the version of the install.
        /// </summary>
        public Version Version { get; private set; }

        /// <summary>
        /// Indicates if DetectUpdate found a newer version to update.
        /// </summary>
        private bool UpdateAvailable { get; set; }

        /// <summary>
        /// UI Thread entry point for TestUX.
        /// </summary>
        protected override void Run()
        {
            this.action = this.Command.Action;
            this.TestVariables();

            this.Version = this.Engine.VersionVariables[BurnBundleVersionVariable];
            this.Log("Version: {0}", this.Version);

            List<string> verifyArguments = this.ReadVerifyArguments();

            string[] args = this.Command.GetCommandLineArgs();
            foreach (string arg in args)
            {
                // If we're not in the update already, process the updatebundle.
                if (this.Command.Relation != RelationType.Update && arg.StartsWith("-updatebundle:", StringComparison.OrdinalIgnoreCase))
                {
                    this.updateBundlePath = arg.Substring(14);
                    FileInfo info = new FileInfo(this.updateBundlePath);
                    this.Engine.SetUpdate(this.updateBundlePath, null, info.Length, UpdateHashType.None, null);
                    this.UpdateAvailable = true;
                    this.action = LaunchAction.UpdateReplaceEmbedded;
                }
                else if (this.Command.Relation != RelationType.Update && arg.StartsWith("-checkupdate", StringComparison.OrdinalIgnoreCase))
                {
                    this.action = LaunchAction.UpdateReplace;
                }

                verifyArguments.Remove(arg);
            }
            this.Log("Action: {0}", this.action);

            // If there are any verification arguments left, error out.
            if (0 < verifyArguments.Count)
            {
                foreach (string expectedArg in verifyArguments)
                {
                    this.Log("Failure. Expected command-line to have argument: {0}", expectedArg);
                }

                this.Engine.Quit(-1);
                return;
            }

            int redetectCount = 0;
            string redetect = this.ReadPackageAction(null, "RedetectCount");
            if (String.IsNullOrEmpty(redetect) || !Int32.TryParse(redetect, out redetectCount))
            {
                redetectCount = 0;
            }

            do
            {
                this.Engine.Detect();
                this.Log("Completed detection phase: {0} re-runs remaining", redetectCount);
            } while (0 < redetectCount--);

            this.wait.WaitOne();
            this.Engine.Quit(this.result & 0xFFFF); // return plain old Win32 error, not HRESULT.
        }

        protected override void OnDetectUpdateBegin(DetectUpdateBeginEventArgs args)
        {
            this.Log("OnDetectUpdateBegin");
            if ((LaunchAction.UpdateReplaceEmbedded == this.action)|(LaunchAction.UpdateReplace == this.action))
            {
                args.Skip = false;
            }
        }

        protected override void OnDetectUpdate(DetectUpdateEventArgs e)
        {
            // The list of updates is sorted in descending version, so the first callback should be the largest update available.
            // This update should be either larger than ours (so we are out of date), the same as ours (so we are current)
            // or smaller than ours (we have a private build). If we really wanted to, we could leave the e.StopProcessingUpdates alone and
            // enumerate all of the updates.
            this.Log(String.Format("Potential update v{0} from '{1}'; current version: v{2}", e.Version, e.UpdateLocation, this.Version));
            if (e.Version > this.Version)
            {
                this.Log(String.Format("Selected update v{0}", e.Version));
                this.Engine.SetUpdate(null, e.UpdateLocation, e.Size, UpdateHashType.None, null);
                this.UpdateAvailable = true;
            }
            else
            {
                this.UpdateAvailable = false;
            }
            e.StopProcessingUpdates = true;
        }

        protected override void OnDetectUpdateComplete(DetectUpdateCompleteEventArgs e)
        {
            this.Log("OnDetectUpdateComplete");

            // Failed to process an update, allow the existing bundle to still install.
            if (!Hresult.Succeeded(e.Status))
            {
                this.Log(String.Format("Failed to locate an update, status of 0x{0:X8}, updates disabled.", e.Status));
                e.IgnoreError = true; // But continue on...
            }
        }

        protected override void OnDetectComplete(DetectCompleteEventArgs args)
        {
            this.result = args.Status;

            if (Hresult.Succeeded(this.result) && (this.UpdateAvailable | (!((LaunchAction.UpdateReplaceEmbedded == this.action) | (LaunchAction.UpdateReplace == this.action)))))
            {                
                this.Engine.Plan(this.action);
            }
            else
            {
                this.wait.Set();
            }
        }

        protected override void OnPlanPackageBegin(PlanPackageBeginEventArgs args)
        {
            RequestState state;
            string action = ReadPackageAction(args.PackageId, "Requested");
            if (TryParseEnum<RequestState>(action, out state))
            {
                args.State = state;
            }
        }

        protected override void OnPlanTargetMsiPackage(PlanTargetMsiPackageEventArgs args)
        {
            RequestState state;
            string action = ReadPackageAction(args.PackageId, "Requested");
            if (TryParseEnum<RequestState>(action, out state))
            {
                args.State = state;
            }
        }

        protected override void OnPlanMsiFeature(PlanMsiFeatureEventArgs args)
        {
            FeatureState state;
            string action = ReadFeatureAction(args.PackageId, args.FeatureId, "Requested");
            if (TryParseEnum<FeatureState>(action, out state))
            {
                args.State = state;
            }
        }

        protected override void OnPlanComplete(PlanCompleteEventArgs args)
        {
            this.result = args.Status;
            if (Hresult.Succeeded(this.result))
            {
                this.Engine.Apply(IntPtr.Zero);
            }
            else
            {
                this.wait.Set();
            }
        }

        protected override void OnCachePackageBegin(CachePackageBeginEventArgs args)
        {
            this.Log("OnCachePackageBegin() - package: {0}, payloads to cache: {1}", args.PackageId, args.CachePayloads);

            string slowProgress = ReadPackageAction(args.PackageId, "SlowCache");
            if (String.IsNullOrEmpty(slowProgress) || !Int32.TryParse(slowProgress, out this.sleepDuringCache))
            {
                this.sleepDuringCache = 0;
            }

            string cancelCache = ReadPackageAction(args.PackageId, "CancelCacheAtProgress");
            if (String.IsNullOrEmpty(cancelCache) || !Int32.TryParse(cancelCache, out this.cancelCacheAtProgress))
            {
                this.cancelCacheAtProgress = -1;
            }
        }

        protected override void OnCacheAcquireProgress(CacheAcquireProgressEventArgs args)
        {
            this.Log("OnCacheAcquireProgress() - container/package: {0}, payload: {1}, progress: {2}, total: {3}, overall progress: {4}%", args.PackageOrContainerId, args.PayloadId, args.Progress, args.Total, args.OverallPercentage);

            if (this.cancelCacheAtProgress > 0 && this.cancelCacheAtProgress <= args.Progress)
            {
                args.Cancel = true;
            }
            else if (this.sleepDuringCache > 0)
            {
                Thread.Sleep(this.sleepDuringCache);
            }
        }

        protected override void OnExecutePackageBegin(ExecutePackageBeginEventArgs args)
        {
            this.Log("OnExecutePackageBegin() - package: {0}, rollback: {1}", args.PackageId, !args.ShouldExecute);

            string slowProgress = ReadPackageAction(args.PackageId, "SlowExecute");
            if (String.IsNullOrEmpty(slowProgress) || !Int32.TryParse(slowProgress, out this.sleepDuringExecute))
            {
                this.sleepDuringExecute = 0;
            }

            string cancelExecute = ReadPackageAction(args.PackageId, "CancelExecuteAtProgress");
            if (String.IsNullOrEmpty(cancelExecute) || !Int32.TryParse(cancelExecute, out this.cancelExecuteAtProgress))
            {
                this.cancelExecuteAtProgress = -1;
            }

            string retryBeforeCancel = ReadPackageAction(args.PackageId, "RetryExecuteFilesInUse");
            if (String.IsNullOrEmpty(retryBeforeCancel) || !Int32.TryParse(retryBeforeCancel, out this.retryExecuteFilesInUse))
            {
                this.retryExecuteFilesInUse = 0;
            }
        }

        protected override void OnExecuteFilesInUse(ExecuteFilesInUseEventArgs args)
        {
            this.Log("OnExecuteFilesInUse() - package: {0}, retries remaining: {1}, data: {2}", args.PackageId, this.retryExecuteFilesInUse, String.Join(", ", args.Files.ToArray()));

            if (this.retryExecuteFilesInUse > 0)
            {
                --this.retryExecuteFilesInUse;
                args.Result = Result.Retry;
            }
            else
            {
                args.Result = Result.Abort;
            }
        }

        protected override void OnExecuteProgress(ExecuteProgressEventArgs args)
        {
            this.Log("OnExecuteProgress() - package: {0}, progress: {1}%, overall progress: {2}%", args.PackageId, args.ProgressPercentage, args.OverallPercentage);

            if (this.cancelExecuteAtProgress > 0 && this.cancelExecuteAtProgress <= args.ProgressPercentage)
            {
                args.Cancel = true;
            }
            else if (this.sleepDuringExecute > 0)
            {
                Thread.Sleep(this.sleepDuringExecute);
            }
        }

        protected override void OnExecutePatchTarget(ExecutePatchTargetEventArgs args)
        {
            this.Log("OnExecutePatchTarget - Patch Package: {0}, Target Product Code: {1}", args.PackageId, args.TargetProductCode);
        }

        protected override void OnProgress(ProgressEventArgs args)
        {
            this.Log("OnProgress() - progress: {0}%, overall progress: {1}%", args.ProgressPercentage, args.OverallPercentage);
            if (this.Command.Display == Display.Embedded)
            {
                Engine.SendEmbeddedProgress(args.ProgressPercentage, args.OverallPercentage);
            }
        }

        protected override void OnResolveSource(ResolveSourceEventArgs args)
        {
            if (!String.IsNullOrEmpty(args.DownloadSource))
            {
                args.Action = BOOTSTRAPPER_RESOLVESOURCE_ACTION.Download;
            }
        }

        protected override void OnApplyComplete(ApplyCompleteEventArgs args)
        {
            // Output what the privileges are now.
            this.Log("After elevation: WixBundleElevated = {0}", this.Engine.NumericVariables["WixBundleElevated"]);

            this.result = args.Status;
            this.wait.Set();
        }

        protected override void OnSystemShutdown(SystemShutdownEventArgs args)
        {
            // Always prevent shutdown.
            this.Log("Disallowed system request to shut down the bootstrapper application.");
            args.Cancel = true;

            this.wait.Set();
        }

        private void TestVariables()
        {
            // First make sure we can check and get standard variables of each type.
            {
                string value = null;
                if (this.Engine.StringVariables.Contains("WindowsFolder"))
                {
                    value = this.Engine.StringVariables["WindowsFolder"];
                    this.Engine.Log(LogLevel.Verbose, "TEST: Successfully retrieved a string variable: WindowsFolder");
                }
                else
                {
                    throw new Exception("Engine did not define a standard string variable: WindowsFolder");
                }
            }

            {
                long value = 0;
                if (this.Engine.NumericVariables.Contains("NTProductType"))
                {
                    value = this.Engine.NumericVariables["NTProductType"];
                    this.Engine.Log(LogLevel.Verbose, "TEST: Successfully retrieved a numeric variable: NTProductType");
                }
                else
                {
                    throw new Exception("Engine did not define a standard numeric variable: NTProductType");
                }
            }

            {
                Version value = new Version();
                if (this.Engine.VersionVariables.Contains("VersionMsi"))
                {
                    value = this.Engine.VersionVariables["VersionMsi"];
                    this.Engine.Log(LogLevel.Verbose, "TEST: Successfully retrieved a version variable: VersionMsi");
                }
                else
                {
                    throw new Exception("Engine did not define a standard version variable: VersionMsi");
                }
            }

            // Now validate that Contians returns false for non-existant variables of each type.
            if (this.Engine.StringVariables.Contains("TestStringVariableShouldNotExist"))
            {
                throw new Exception("Engine defined a string variable that should not exist: TestStringVariableShouldNotExist");
            }
            else
            {
                this.Engine.Log(LogLevel.Verbose, "TEST: Successfully checked for non-existent string variable: TestStringVariableShouldNotExist");
            }

            if (this.Engine.NumericVariables.Contains("TestNumericVariableShouldNotExist"))
            {
                throw new Exception("Engine defined a numeric variable that should not exist: TestNumericVariableShouldNotExist");
            }
            else
            {
                this.Engine.Log(LogLevel.Verbose, "TEST: Successfully checked for non-existent numeric variable: TestNumericVariableShouldNotExist");
            }

            if (this.Engine.VersionVariables.Contains("TestVersionVariableShouldNotExist"))
            {
                throw new Exception("Engine defined a version variable that should not exist: TestVersionVariableShouldNotExist");
            }
            else
            {
                this.Engine.Log(LogLevel.Verbose, "TEST: Successfully checked for non-existent version variable: TestVersionVariableShouldNotExist");
            }

            // Output what the initially run privileges were.
            this.Engine.Log(LogLevel.Verbose, String.Format("TEST: WixBundleElevated = {0}", this.Engine.NumericVariables["WixBundleElevated"]));
        }

        private void Log(string format, params object[] args)
        {
            string relation = this.Command.Relation != RelationType.None ? String.Concat(" (", this.Command.Relation.ToString().ToLowerInvariant(), ")") : String.Empty;
            string message = String.Format(format, args);

            this.Engine.Log(LogLevel.Standard, String.Concat("TESTBA", relation, ": ", message));
        }

        private List<string> ReadVerifyArguments()
        {
            string testName = this.Engine.StringVariables["TestName"];
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\TestBAControl\{0}", testName)))
            {
                string verifyArguments = testKey == null ? null : testKey.GetValue("VerifyArguments") as string;
                return verifyArguments == null ? new List<string>() : new List<string>(verifyArguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        private string ReadPackageAction(string packageId, string state)
        {
            string testName = this.Engine.StringVariables["TestName"];
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\TestBAControl\{0}\{1}", testName, String.IsNullOrEmpty(packageId) ? String.Empty : packageId)))
            {
                return testKey == null ? null : testKey.GetValue(state) as string;
            }
        }

        private string ReadFeatureAction(string packageId, string featureId, string state)
        {
            string testName = this.Engine.StringVariables["TestName"];
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\TestBAControl\{0}\{1}", testName, packageId)))
            {
                string registryName = String.Concat(featureId, state);
                return testKey == null ? null : testKey.GetValue(registryName) as string;
            }
        }

        private bool TryParseEnum<T>(string value, out T t)
        {
            try
            {
                t = (T)Enum.Parse(typeof(T), value, true);
                return true;
            }
            catch (ArgumentException) { }
            catch (OverflowException) { }

            t = default(T);
            return false;
        }
    }
}
