// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using WixToolset.Bootstrapper;

namespace WixToolset.UX
{
    public class ProgressViewModel : PropertyNotifyBase
    {
        private static readonly Regex TrimActionTimeFromMessage = new Regex(@"^\w+\s+\d+:\d+:\d+:\s+", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        private RootViewModel root;
        private Dictionary<string, int> executingPackageOrderIndex;

        private int progressPhases;
        private int progress;
        private int cacheProgress;
        private int executeProgress;
        private string package;
        private string message;

        public ProgressViewModel(RootViewModel root)
        {
            this.root = root;
            this.executingPackageOrderIndex = new Dictionary<string, int>();

            this.root.PropertyChanged += this.RootPropertyChanged;

            WixBA.Model.Bootstrapper.ExecutePackageBegin += this.ExecutePackageBegin;
            WixBA.Model.Bootstrapper.ExecutePackageComplete += this.ExecutePackageComplete;
            WixBA.Model.Bootstrapper.ExecuteProgress += this.ApplyExecuteProgress;
            WixBA.Model.Bootstrapper.PlanBegin += this.PlanBegin;
            WixBA.Model.Bootstrapper.PlanPackageComplete += this.PlanPackageComplete;
            WixBA.Model.Bootstrapper.ApplyBegin += this.ApplyBegin;
            WixBA.Model.Bootstrapper.Progress += this.ApplyProgress;
            WixBA.Model.Bootstrapper.CacheAcquireProgress += this.CacheAcquireProgress;
            WixBA.Model.Bootstrapper.CacheComplete += this.CacheComplete;
        }

        public bool ProgressEnabled
        {
            get { return this.root.InstallState == InstallationState.Applying; }
        }

        public int Progress
        {
            get
            {
                return this.progress;
            }

            set
            {
                if (this.progress != value)
                {
                    this.progress = value;
                    base.OnPropertyChanged("Progress");
                }
            }
        }

        public string Package
        {
            get
            {
                return this.package;
            }

            set
            {
                if (this.package != value)
                {
                    this.package = value;
                    base.OnPropertyChanged("Package");
                }
            }
        }

        public string Message
        {
            get
            {
                return this.message;
            }

            set
            {
                if (this.message != value)
                {
                    this.message = value;
                    base.OnPropertyChanged("Message");
                }
            }
        }

        void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ("InstallState" == e.PropertyName)
            {
                base.OnPropertyChanged("ProgressEnabled");
            }
        }

        private void PlanBegin(object sender, PlanBeginEventArgs e)
        {
            lock (this)
            {
                this.executingPackageOrderIndex.Clear();
            }
        }

        private void PlanPackageComplete(object sender, PlanPackageCompleteEventArgs e)
        {
            if (ActionState.None != e.Execute)
            {
                lock (this)
                {
                    Debug.Assert(!this.executingPackageOrderIndex.ContainsKey(e.PackageId));
                    this.executingPackageOrderIndex.Add(e.PackageId, this.executingPackageOrderIndex.Count);
                }
            }
        }

        private void ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs e)
        {
            lock (this)
            {
                this.Package = WixBA.Model.GetPackageName(e.PackageId);
                this.Message = String.Format("Processing: {0}", this.Package);
                e.Cancel = this.root.Canceled;
            }
        }

        private void ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs e)
        {
            lock (this)
            {   // avoid a stale display
                this.Message = String.Empty;
            }
        }

        private void ApplyBegin(object sender, ApplyBeginEventArgs e)
        {
            this.progressPhases = e.PhaseCount;
        }

        private void ApplyProgress(object sender, ProgressEventArgs e)
        {
            lock (this)
            {
                e.Cancel = this.root.Canceled;
            }
        }

        private void CacheAcquireProgress(object sender, CacheAcquireProgressEventArgs e)
        {
            lock (this)
            {
                this.cacheProgress = e.OverallPercentage;
                this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;
                e.Cancel = this.root.Canceled;
            }
        }

        private void CacheComplete(object sender, CacheCompleteEventArgs e)
        {
            lock (this)
            {
                this.cacheProgress = 100;
                this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;
            }
        }

        private void ApplyExecuteProgress(object sender, ExecuteProgressEventArgs e)
        {
            lock (this)
            {
                this.executeProgress = e.OverallPercentage;
                this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;

                if (WixBA.Model.Command.Display == Display.Embedded)
                {
                    WixBA.Model.Engine.SendEmbeddedProgress(e.ProgressPercentage, this.Progress);
                }

                e.Cancel = this.root.Canceled;
            }
        }
    }
}
