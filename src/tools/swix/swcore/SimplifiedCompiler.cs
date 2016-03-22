//-------------------------------------------------------------------------------------------------
// <copyright file="SimplifiedCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using WixToolset.Simplified.CompilerBackend;
    using WixToolset.Simplified.CompilerFrontend;
    using WixToolset.Simplified.Lexicon;
    using IO = System.IO;

    /// <summary>
    /// Compiler for Simplified WiX toolset.
    /// </summary>
    public sealed class SimplifiedCompiler
    {
        private List<Swix> roots;
        private List<string> sourceFiles;
        private List<CompilerExtension> extensions;
        private List<Assembly> referenceAssemblies;

        /// <summary>
        /// Creates a simplified compiler.
        /// </summary>
        public SimplifiedCompiler()
        {
            this.roots = new List<Swix>();
            this.sourceFiles = new List<string>();
            this.extensions = new List<CompilerExtension>();

            // Add the reference assemblies, ensure this model is always first.
            this.referenceAssemblies = new List<Assembly>();
            this.referenceAssemblies.Add(Assembly.GetExecutingAssembly());

            this.PreprocessorDefines = new Dictionary<string, string>();
            this.SearchPaths = new List<string>();
        }

        /// <summary>
        /// Event fired when the compiler encounters a warning or error.
        /// </summary>
        public event EventHandler<CompilerMessageEventArgs> Messages;

        /// <summary>
        /// Gets the set of paths to search for files.
        /// </summary>
        public IDictionary<string, string> PreprocessorDefines { get; private set; }

        /// <summary>
        /// Gets the set of paths to search for files.
        /// </summary>
        public IList<string> SearchPaths { get; private set; }

        /// <summary>
        /// Gets the compiler extensions being used for the compile
        /// </summary>
        public IEnumerable<CompilerExtension> Extensions
        {
            get { return this.extensions; }
        }

        public CompilerFileManager FileManager { get; set; }

        public void AddFile(string container, string sourcePath)
        {
            this.AddFile(container, sourcePath, null);
        }

        public void AddFile(string container, string sourcePath, string targetName)
        {
            string fullPath = IO.Path.GetFullPath(sourcePath);
            Swix root = new Swix(
                new File(fullPath, targetName)
                );

            root.LineNumber = new FileLineNumber(fullPath, 0, 0);
            this.roots.Add(root);
        }

        /// <summary>
        /// Adds a file that will be compiled.
        /// </summary>
        /// <param name="filePath">Path to source file.</param>
        public void AddSourceFile(string filePath)
        {
            string fullPath = IO.Path.GetFullPath(filePath);
            this.sourceFiles.Add(fullPath);
        }

        /// <summary>
        /// Adds an extension that will be used during the compile
        /// </summary>
        /// <param name="extension">The extension to be added</param>
        public void AddExtension(CompilerExtension extension)
        {
            this.extensions.Add(extension);
        }

        /// <summary>
        /// Compiles the added files into a target output appropriate for the output path extension.
        /// </summary>
        /// <param name="type">Package type to generate.</param>
        /// <param name="architecture">Package architecture to generate.</param>
        /// <param name="languages">Package languages to generate.</param>
        /// <param name="outputPath">Path to generate output. Output path file extension determines the output type.</param>
        public void Compile(PackageType type, PackageArchitecture architecture, CultureInfo[] languages, string outputPath)
        {
            CoreCompile(architecture, languages, outputPath, () => BackendCompiler.Create(type, outputPath) );
        }

        /// <summary>
        /// Compiles the added files using the given CompilerExtension to generate the output
        /// </summary>
        /// <param name="outputExtension">Extension used to generated the output.</param>
        /// <param name="architecture">Package architecture to generate.</param>
        /// <param name="languages">Package languages to generate.</param>
        /// <param name="outputPath">Path to generate output.</param>
        public void Compile(CompilerExtension outputExtension, PackageArchitecture architecture, CultureInfo[] languages, string outputPath)
        {
            CoreCompile(architecture, languages, outputPath, () => outputExtension.CreateBackendCompiler());
        }

        private void CoreCompile(PackageArchitecture architecture, CultureInfo[] languages, string outputPath, Func<BackendCompiler> createBackendCompiler)
        {
            FrontendCompiler frontend = new FrontendCompiler(architecture, this.referenceAssemblies);
            frontend.Messages += this.Messages;

            foreach (KeyValuePair<string, string> define in this.PreprocessorDefines)
            {
                frontend.Substitutions.Add(define.Key, define.Value);
            }

            foreach (Swix root in this.roots)
            {
                frontend.AddRoot(root);
            }

            frontend.AddExtensions(this.extensions);

            foreach (string path in this.sourceFiles)
            {
                frontend.Parse(path);
            }

            if (frontend.EncounteredError)
            {
                return;
            }

            frontend.Resolve();
            if (frontend.EncounteredError)
            {
                return;
            }

            frontend.Harvest();
            if (frontend.EncounteredError)
            {
                return;
            }

            Intermediate[] intermediates = frontend.GetIntermediates();

            CompilerFileManager fileManager = this.FileManager ?? new CompilerFileManager();
            fileManager.Architecture = architecture;
            fileManager.Language = languages.Length == 0 ? null : languages[0];
            fileManager.OutputPath = outputPath;
            foreach (string searchPath in this.SearchPaths)
            {
                fileManager.SearchPaths.Add(searchPath);
            }

            using (BackendCompiler backend = createBackendCompiler())
            {
                backend.Messages += this.Messages;
                backend.Architecture = architecture;
                backend.FileManager = fileManager;
                backend.Languages = languages;

                backend.Generate(intermediates, outputPath);
            }
        }
    }
}
