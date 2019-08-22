// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using IVsGeneratorProgress = Microsoft.VisualStudio.Shell.Interop.IVsGeneratorProgress;
using IVsSingleFileGenerator = Microsoft.VisualStudio.Shell.Interop.IVsSingleFileGenerator;

namespace WixToolset.Simplified.TypedMessageGenerator
{
    // Expose the generator to VS (for CSharp)
    [ProvideCodeGenerator(typeof(TypedMessageGenerator),
        TypedMessageGenerator.GeneratorName,
        TypedMessageGenerator.GeneratorDescription,
        true, ProjectSystem = ProvideCodeGeneratorAttribute.CSharpProjectGuid)]

    // Expose the generator to VS (for VB)
    ////[ProvideCodeGenerator(typeof(TypedMessageGenerator),
    ////    TypedMessageGenerator.GeneratorName,
    ////    TypedMessageGenerator.GeneratorDescription,
    ////    true, ProjectSystem = ProvideCodeGeneratorAttribute.VisualBasicProjectGuid)]

    // Associate ".msgs" files with this generator.
    [ProvideCodeGeneratorExtension(TypedMessageGenerator.GeneratorName,
        ".msgs",
        ProjectSystem = ProvideCodeGeneratorExtensionAttribute.CSharpProjectSystemGuid)]

    [Guid("6A07034A-32BA-4F13-B401-9ED6FC2D1489")]
    internal class TypedMessageGenerator : MultiFileGenerator<TypedMessageGenerator.SubItem>
    {
        const string GeneratorName = "TypedMessageGenerator";
        const string GeneratorDescription = "A file generator that converts message files into strongly-typed formatting methods.";

        internal enum SubItem
        {
            Resx,
            Source, // summary item, not an iteration

            IterationMin = Resx,
            IterationMax = Source,
        }

        private MessageData messages;
        private string className;

        public TypedMessageGenerator()
        {
        }

        private string GetGeneratedExtension(SubItem subItem)
        {
            string extension;

            switch (subItem)
            {
                default:
                case SubItem.Resx:
                    extension = subItem.ToString().ToLowerInvariant();
                    break;
                case SubItem.Source:
                    extension = "cs";   // We don't support VB yet...
                    break;
            }

            return string.Concat(".Generated.", extension);
        }

        protected override IEnumerable<SubItem> GetIterations()
        {
            this.className = "Message";

            if (!string.IsNullOrEmpty(this.InputFilePath))
            {
                this.className = Path.GetFileNameWithoutExtension(this.InputFilePath);
            }

            // Before returning the iterations, we have to parse the source...
            try
            {
                Parser.Parser parser = new Parser.Parser(this.InputFilePath);
                this.messages = parser.Parse(this.InputFileContents);
                this.AutoNumberMessages(messages);
            }
            catch (Exception ex)
            {
                if (ErrorHandler.IsCriticalException(ex))
                {
                    throw;
                }
            }

            // return our iterations...
            for (SubItem subItem = SubItem.IterationMin; subItem < SubItem.IterationMax; ++subItem)
            {
                yield return subItem;
            }
        }

        protected override string GetFileName(SubItem subItem)
        {
            return Path.ChangeExtension(this.InputFilePath, this.GetGeneratedExtension(subItem));
        }

        protected override byte[] GenerateContent(SubItem subItem)
        {
            return this.GenerateSubItemBytes(subItem);
        }

        protected override string GetDefaultExtension()
        {
            return this.GetGeneratedExtension(SubItem.Source);
        }

        protected override byte[] GenerateSummaryContent()
        {
            return this.GenerateSubItemBytes(SubItem.Source);
        }

        private byte[] GenerateSubItemBytes(SubItem subItem)
        {
            // Some iterations return byte arrays directly, and some create
            // string content.
            switch (subItem)
            {
                case SubItem.Resx:
                    return this.GenerateResx();

                default:
                    string content = this.GenerateSubItemContent(subItem);

                    List<byte> bytes = new List<byte>();
                    bytes.AddRange(Encoding.UTF8.GetPreamble());
                    bytes.AddRange(Encoding.UTF8.GetBytes(content));

                    return bytes.ToArray();
            }

            throw new Exception("Internal error! Unexpected code location!");
        }

        private string GenerateSubItemContent(SubItem subItem)
        {
            string content = null;
            switch (subItem)
            {
                case SubItem.Source:
                    content = this.GenerateSource();
                    break;
                default:
                case SubItem.Resx:
                    throw new ArgumentException(string.Format("Don't know how to handle subItem of '{0}'.", subItem), "subItem");
            }

            return content;
        }

        private byte[] GenerateResx()
        {
            ResxGeneration resxgen = new ResxGeneration();
            return resxgen.GenerateResx(this.DefaultNamespace, this.messages, this.className);
        }

        private string GenerateSource()
        {
            CodeGeneration codegen = new CodeGeneration();
            return codegen.GenerateCode(this.DefaultNamespace, this.messages, this.className);
        }

        // Create a semi-dependable list of message numbers.
        private void AutoNumberMessages(MessageData messages)
        {
            HashSet<int> usedIds = new HashSet<int>();

            // First, claim all explicitly authored IDs...
            messages.Messages.Where(m => m.Id != -1).ForEach(m => usedIds.Add(m.Id));

            // Now add all the rest, keeping them in the proper range...
            var needIds = messages.Messages.Where(m => m.Id == -1);

            foreach (MessageType messageType in messages.Types)
            {
                this.AutoNumberMessageRange(
                    needIds.Where(m => m.Type == messageType),
                    messageType.FirstId,
                    messageType.LastId,
                    usedIds);
            }
        }

        private void AutoNumberMessageRange(IEnumerable<Message> messagesToNumber, int idStart, int idEnd, HashSet<int> usedIds)
        {
            int nextId = idStart;
            messagesToNumber.ForEach(m =>
            {
                while (usedIds.Contains(nextId))
                {
                    ++nextId;
                }

                // TODO: Ensure we're still within range!
                usedIds.Add(nextId);
                m.SetAutoId(nextId);
                ++nextId;
            });
        }

    }
}
