// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified.Lexicon.Appx
{
    using System;
    using WixToolset.Simplified.CompilerFrontend;

    /// <summary>
    /// Additional tasks for a background task.
    /// </summary>
    public class AdditionalTask
    {
        public BackgroundTaskType Name { get; set; }

        internal void Verify(FrontendCompiler context, PackageItem parentItem)
        {
            if (this.Name == BackgroundTaskType.Invalid)
            {
                context.OnMessage(new CompilerMessageEventArgs(CompilerMessage.RequiredAttribute("AdditionalTask", "Name"), parentItem));
            }
        }
    }
}
