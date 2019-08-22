// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IVsGeneratorProgress = Microsoft.VisualStudio.Shell.Interop.IVsGeneratorProgress;
using IVsSingleFileGenerator = Microsoft.VisualStudio.Shell.Interop.IVsSingleFileGenerator;

namespace WixToolset.Simplified.TypedMessageGenerator
{
    internal abstract class MultiFileGenerator<TIter> : IVsSingleFileGenerator
    {
        // constructor
        public MultiFileGenerator()
        {
            this.DTE = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
        }

        protected EnvDTE.DTE DTE { get; private set; }
        protected EnvDTE.Project Project { get; private set; }
        protected EnvDTE.ProjectItem ProjectItem { get; private set; }
        protected string InputFileContents { get; private set; }
        protected string InputFilePath { get; private set; }
        protected string DefaultNamespace { get; private set; }

        // Interfaces that the sub-classes must implement...
        protected abstract IEnumerable<TIter> GetIterations();

        protected abstract string GetFileName(TIter tIter);

        protected abstract byte[] GenerateContent(TIter tIter);

        // Allow post-creation adjustment of the generated project sub-items...
        protected virtual void AdjustChildItem(TIter tIter, EnvDTE.ProjectItem childItem)
        {
            // nothing by default...
        }

        protected abstract string GetDefaultExtension();

        protected abstract byte[] GenerateSummaryContent();

        #region IVsSingleFileGenerator Members

        public int DefaultExtension(out string defaultExtension)
        {
            defaultExtension = this.GetDefaultExtension();
            return VSConstants.S_OK;
        }

        // The docs say you can't rely on the 'inputFilePath' value, but there's *no other way* to know the base file name or the project!
        public int Generate(
            string inputFilePath,
            string inputFileContents,
            string defaultNamespace,
            IntPtr[] outputFileContents,
            out uint outputByteCount,
            IVsGeneratorProgress progress)
        {
            outputByteCount = 0;
            List<string> generatedFiles = new List<string>();
            List<EnvDTE.ProjectItem> generatedItems = new List<EnvDTE.ProjectItem>();

            this.InputFilePath = inputFilePath;
            this.InputFileContents = inputFileContents;
            this.DefaultNamespace = defaultNamespace;

            this.GetProject();

            foreach (var iter in this.GetIterations())
            {
                try
                {
                    // Get the file for this iteration.
                    // TODO: correct for full/relative paths?
                    string file = this.GetFileName(iter);

                    // Keep track of generated files, we may need to add them to the project.
                    generatedFiles.Add(file);

                    // Create the file...
                    using (FileStream stream = File.Create(file))
                    {
                        byte[] content = this.GenerateContent(iter);

                        stream.Write(content, 0, content.Length);
                        ////stream.Close();
                    }

                    // Ensure the new item is a child of the input file...
                    EnvDTE.ProjectItem childItem = this.ProjectItem.ProjectItems.AddFromFile(file);
                    generatedItems.Add(childItem);
                    this.AdjustChildItem(iter, childItem);
                }
                catch (Exception ex)
                {
                    if (ErrorHandler.IsCriticalException(ex))
                    {
                        throw;
                    }
                }
            }

            // Delete any child items that aren't ours...
            foreach (EnvDTE.ProjectItem childItem in this.ProjectItem.ProjectItems)
            {
                if (!childItem.Name.EndsWith(this.GetDefaultExtension()) &&
                    !generatedItems.Contains(childItem))
                {
                    childItem.Delete();
                }
            }

            // Finally, generate the summary content...
            byte[] summaryContent = this.GenerateSummaryContent();

            if (summaryContent == null || summaryContent.Length == 0)
            {
                outputFileContents[0] = IntPtr.Zero;
                outputByteCount = 0;
            }
            else
            {
                IntPtr mem = Marshal.AllocCoTaskMem(summaryContent.Length);
                Marshal.Copy(summaryContent, 0, mem, summaryContent.Length);

                outputFileContents[0] = mem;
                outputByteCount = (uint)summaryContent.Length;
            }

            return VSConstants.S_OK;
        }

        #endregion

        // Get the project associated with the input file...
        private void GetProject()
        {
            // REVIEW: Is there an IVSHierarchy/non-DTE way to do this?  Would that be better?
            // DTE isn't supported on all project types.  However, we know that our primary client
            // is a C# project, so using DTE is relatively safe.
            this.ProjectItem = this.DTE.Solution.FindProjectItem(this.InputFilePath);
            this.Project = this.ProjectItem.ContainingProject;
        }
    }
}
