// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Simplified.CompilerFrontend;

    public enum BackgroundTaskType
    {
        Invalid,
        audio,
        controlChannel,
        pushNotification,
        systemEvent,
        timer,
    };

    /// <summary>
    /// The `appx.backgroundTask` element allows applications to continue running a
    /// process even when suspended. A background task may support multiple tasks by
    /// adding child [`appx.additionalTask`](#appxadditionaltask) elements.
    /// 
    /// Parent: `application`
    /// 
    /// Children: `appx.additionalTask`
    /// </summary>
    [DefaultCollectionProperty("AdditionalTasks")]
    public class BackgroundTask : ApplicationExtensionItem
    {
        public BackgroundTask()
        {
            this.AdditionalTasks = new List<AdditionalTask>();
        }

        /// <summary>
        /// `name` - specifies the task to run in the background. See the list below for
        /// valid values. For example, `name=audio`
        /// 
        ///   The following is a list of tasks supported by background tasks in AppX:
        ///     * `audio`
        ///     * `controlChannel`
        ///     * `pushNotification`
        ///     * `systemEvent`
        ///     * `timer`
        /// </summary>
        public BackgroundTaskType Name { get; set; }

        /// <summary>
        /// `serverName` (optional) - indicates the WinRT server instance for the
        /// background tasks and 3rd party classes are the same instance, thus ensuring
        /// only one instance of the server will exist at runtime regardless of how it was
        /// activated. For example, `serverName=BackgroundTaskExampleServer`
        /// </summary>
        public string ServerName { get; set; }

        public ICollection<AdditionalTask> AdditionalTasks { get; private set; }

        protected override void OnResolveBegin(FrontendCompiler context)
        {
            base.OnResolveBegin(context);

            if (this.Name == BackgroundTaskType.Invalid)
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("BackgroundTask", "Name"), this));
            }

            foreach (AdditionalTask at in this.AdditionalTasks)
            {
                at.Verify(context, this);
            }
        }
    }
}
