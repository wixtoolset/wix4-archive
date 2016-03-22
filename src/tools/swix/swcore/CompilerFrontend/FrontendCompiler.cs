//-------------------------------------------------------------------------------------------------
// <copyright file="FrontendCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified.CompilerFrontend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using WixToolset.Simplified.CompilerFrontend.Parser;
    using WixToolset.Simplified.Lexicon;
    using WixToolset.Simplified.ParserCore;
    using IO = System.IO;
    using Regex = System.Text.RegularExpressions;

    public class FrontendCompiler : IServiceProvider
    {
        private const string DefaultFileSystemManagerRootFolderId = "ApplicationFolder";

        private static readonly Regex.Regex ParseSubsitutions = new Regex.Regex(@"\$\([a-zA-Z_][a-zA-Z0-9_\-\.]*\)", Regex.RegexOptions.Compiled | Regex.RegexOptions.CultureInvariant | Regex.RegexOptions.ExplicitCapture | Regex.RegexOptions.Singleline);

        Dictionary<string, PackageItem> namedItems;
        private List<PackageItem> items;
        private List<Swix> roots;

        private Dictionary<Type, object> services;
        private FileSystemResourceManager fileSystemManager;

        private List<PackageItem> unresolved;
        private List<CompilerExtension> extensions;

        public FrontendCompiler(PackageArchitecture architecture, List<System.Reflection.Assembly> referenceAssemblies)
        {
            this.namedItems = new Dictionary<string, PackageItem>();
            this.Substitutions = new Dictionary<string, string>();

            this.Architecture = architecture;

            this.items = new List<PackageItem>();
            this.roots = new List<Swix>();

            this.services = new Dictionary<Type, object>();

            this.fileSystemManager = new FileSystemResourceManager(FrontendCompiler.DefaultFileSystemManagerRootFolderId);
            this.services.Add(this.fileSystemManager.GetType(), this.fileSystemManager);

            this.unresolved = new List<PackageItem>();
            this.extensions = new List<CompilerExtension>();

            this.AddSystemPackage(); // ensure the system resources are always loaded.
        }

        public event EventHandler<CompilerMessageEventArgs> Messages;

        public PackageArchitecture Architecture { get; private set; }

        public bool EncounteredError { get; private set; }

        public IDictionary<string, string> Substitutions { get; private set; }

        public void AddRoot(Swix root)
        {
            this.AddItems(root.LineNumber, root.Items);
            this.roots.Add(root);
        }

        public void Parse(string path)
        {
            using (IO.StreamReader reader = IO.File.OpenText(path))
            {
                this.Parse(reader, path);
            }
        }

        public void Parse(IO.TextReader textReader, string path)
        {
            StatementNode rootStatementNode = null;
            string fileExtension = IO.Path.GetExtension(path);
            //XamlReader reader;
            if (fileExtension.Equals(".swx", StringComparison.OrdinalIgnoreCase))
            {
                var parser = new XmlParser(path);
                rootStatementNode = parser.Parse(textReader);
            }
            else
            {
                var parser = new RtypeParser(path);
                rootStatementNode = parser.Parse(textReader);
            }

            // Preprocess all values
            this.ProcessStatementSubstitutions(path, rootStatementNode);
            if (this.EncounteredError)
            {
                // TODO: Should we show error that compiler aborted early because
                // substitutions could not all be processed?
                return;
            }

            // Now create objects.
            List<object> items = this.InstantiateObjects(path, rootStatementNode);
            foreach (object item in items)
            {
                if (item is PackageItem)
                {
                    PackageItem packageItem = (PackageItem)item;
                    this.AddItem(packageItem.LineNumber, packageItem);
                }
                else if (item is Swix)
                {
                    this.roots.Add((Swix)item);
                }
            }
        }

        private void ProcessStatementSubstitutions(string path, StatementNode node)
        {
            foreach (var token in node.Statement.Tokens.Where(t => t.TokenType == ParserTokenType.PropertyValue))
            {
                this.SubstituteValues(path, token);
            }

            foreach (var childNode in node.Children)
            {
                this.ProcessStatementSubstitutions(path, childNode);
            }
        }

        private void SubstituteValues(string path, Token<ParserTokenType> token)
        {
            bool processed = false;

            string updatedValue = token.Value;
            if (!String.IsNullOrEmpty(updatedValue))
            {
                int replaceCount = 0;

                Regex.Match m = FrontendCompiler.ParseSubsitutions.Match(updatedValue);
                while (m.Success)
                {
                    processed = true; // found a substitution.

                    // This is an arbitrary upper limit for variable replacements to prevent
                    // inifite loops.
                    if (replaceCount > 2000)
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.InfiniteLoopInSubsitution(updatedValue), path, token.Range.Start.Line, token.Range.Start.Column, token.Range.End.Column));
                        return;
                    }

                    string beginning = updatedValue.Substring(0, m.Index);
                    string variableName = updatedValue.Substring(m.Index + 2, m.Length - 3);
                    string end = updatedValue.Substring(m.Index + m.Length);

                    string variableValue;
                    if (!this.Substitutions.TryGetValue(variableName, out variableValue))
                    {
                        this.OnMessage(new CompilerMessageEventArgs(CompilerMessage.UnknownSubstitution(variableName), path, token.Range.Start.Line, token.Range.Start.Column, token.Range.End.Column));
                        return;
                    }

                    updatedValue = String.Concat(beginning, variableValue, end);
                    m = FrontendCompiler.ParseSubsitutions.Match(updatedValue);
                }
            }

            if (processed)
            {
                token.UpdateValue(updatedValue);
            }
        }

        private List<object> InstantiateObjects(string path, StatementNode rootStatementNode)
        {
            ObjectInstantiator instantiator = new ObjectInstantiator(this.OnMessage);
            instantiator.DefaultNamespaces.Add(String.Empty, "WixToolset.Simplified.Lexicon");
            instantiator.TypeCache.AddAssembly(Assembly.GetExecutingAssembly());

            // Include types from the assemblies containing extensions
            foreach (var extension in this.extensions)
            {
                instantiator.TypeCache.AddAssembly(extension.GetType().Assembly);
            }

            List<object> items = instantiator.Instantiate(path, rootStatementNode);
            return items;
        }

        public void Harvest()
        {
            bool harvested = false;
            foreach (PackageItem item in this.items)
            {
                IHarvestResource harvester = item as IHarvestResource;
                if (harvester != null)
                {
                    IEnumerable<Resource> harvestedResources = harvester.Harvest();
                    this.AddItems(item.LineNumber, harvestedResources);

                    harvested = true;
                }
            }

            // Resolve any harvested resources.
            if (harvested)
            {
                this.Resolve();
            }
        }

        public void Resolve()
        {
            // First propagate the group information.
            List<PackageItem> processGroups = new List<PackageItem>(this.unresolved);
            processGroups.ForEach(group => group.ResolveGroup(this));

            // Start with the list of unresolved items, then process those, then go back and calculate the list
            // of unprocessed items again. This is important because new items may be added while we are
            // resolving the ones here and that will mess up the enumerator.
            do
            {
                List<PackageItem> resolve = this.unresolved;
                this.unresolved = new List<PackageItem>();

                resolve.ForEach(item => item.BeginResolve(this));

                var fileSystemResources = from r in resolve where r is FileSystemResource select (FileSystemResource)r;
                this.fileSystemManager.ResolveResources(this, fileSystemResources.ToList());

                resolve.ForEach(item => item.EndResolve(this));
            } while (this.unresolved.Count > 0);

            this.items.ForEach(item => item.VerifyResolvedConsistency());
        }

        public Intermediate[] GetIntermediates()
        {
            return new Intermediate[] { new Intermediate(this.items) };
        }

        public void OnMessage(CompilerMessageEventArgs e)
        {
            if (e.Message.Type == CompilerMessage.CompilerMessageType.LexerError ||
                e.Message.Type == CompilerMessage.CompilerMessageType.Error)
            {
                this.EncounteredError = true;
            }

            if (this.Messages != null)
            {
                this.Messages(this, e);
            }
        }

        public object GetService(Type serviceType)
        {
            object service = null;
            if (!this.services.TryGetValue(serviceType, out service))
            {
            }

            return service;
        }

        internal void AddItem(FileLineNumber typeLine, PackageItem item)
        {
            item.LineNumber = typeLine;
            this.items.Add(item);

            if (!String.IsNullOrEmpty(item.Id))
            {
                this.namedItems.Add(item.Id, item);
            }

            this.unresolved.Add(item); // track unresolved items so that we optimize our passes through Resolve().
        }

        internal void AddItems(FileLineNumber typeLine, IEnumerable<PackageItem> items)
        {
            foreach (PackageItem item in items)
            {
                this.AddItem(typeLine, item);
            }
        }

        internal void RemoveItem(PackageItem removeItem)
        {
            if (removeItem.Group != null)
            {
                removeItem.Group.Items.Remove(removeItem);
            }

            this.items.Remove(removeItem);

            PackageItem namedItem;
            if (!String.IsNullOrEmpty(removeItem.Id) && this.namedItems.TryGetValue(removeItem.Id, out namedItem) && namedItem == removeItem)
            {
                // TODO: Could this ever happen?
                this.namedItems.Remove(removeItem.Id);
            }

            removeItem.Deleted = true;
        }

        internal bool TryGetItemById(string id, out PackageItem item)
        {
            return this.namedItems.TryGetValue(id, out item);
        }
        
        internal void AddExtensions(IEnumerable<CompilerExtension> extensions)
        {
            this.extensions.AddRange(extensions);
        }

        private void AddSystemPackage()
        {
            Swix root = new Swix(
                new Group(true, "System",
                    new Folder(true, "ApplicationFolder"),
                    new Folder(true, "DesktopFolder"),
                    new Folder(true, "GacFolder"),
                    new Folder(true, "InstallFolder"),
                    new Folder(true, "ProgramFilesFolder"),
                    new Folder(true, "StartMenuFolder"),
                    new Folder(true, "SystemFolder")
                ));

            root.LineNumber = new FileLineNumber("System", 0, 0);
            this.AddRoot(root);
        }
    }
}
