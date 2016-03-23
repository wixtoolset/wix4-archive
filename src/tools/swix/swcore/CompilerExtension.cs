//-------------------------------------------------------------------------------------------------
// <copyright file="CompilerExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Simplified
{
    using System;
    using System.IO;
    using System.Reflection;
    using CompilerBackend;

    /// <summary>
    /// Compiler extension.
    /// </summary>
    public abstract class CompilerExtension
    {
        /// <summary>
        /// Gets or sets the data provided from the compiler.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Event fired when the compiler extension needs to send information, a warning or an error.
        /// </summary>
        public event EventHandler<CompilerMessageEventArgs> Messages;

        /// <summary>
        /// Gets the optional compiler file manager.
        /// </summary>
        /// <value>The optional compiler file manager.</value>
        public virtual CompilerFileManager FileManager
        {
            get { return null; }
        }

        /// <summary>
        /// Indicates whether the extension has a backend compiler that supports the given output type
        /// </summary>
        public virtual bool HasBackendCompiler(string outputType)
        {
            return false;
        }

        /// <summary>
        /// Creates the backend compiler to use for output generation
        /// </summary>
        public virtual BackendCompiler CreateBackendCompiler()
        {
            return null;
        }

        /// <summary>
        /// Loads a CompilerExtension from a type description string.
        /// </summary>
        /// <param name="extension">The extension type description string.</param>
        /// <returns>The loaded CompilerExtension.</returns>
        /// <remarks>
        /// <paramref name="extension"/> can be in several different forms:
        /// <list type="number">
        /// <item><term>AssemblyQualifiedName (TopNamespace.SubNameSpace.ContainingClass+NestedClass, MyAssembly, Version=1.3.0.0, Culture=neutral, PublicKeyToken=b17a5c561934e089)</term></item>
        /// <item><term>AssemblyName (MyAssembly, Version=1.3.0.0, Culture=neutral, PublicKeyToken=b17a5c561934e089)</term></item>
        /// <item><term>Absolute path to an assembly (C:\MyExtensions\ExtensionAssembly.dll)</term></item>
        /// <item><term>Filename of an assembly in the application directory (ExtensionAssembly.dll)</term></item>
        /// <item><term>Relative path to an assembly (..\..\MyExtensions\ExtensionAssembly.dll)</term></item>
        /// </list>
        /// To specify a particular class to use, prefix the fully qualified class name to the assembly and separate them with a comma.
        /// For example: "TopNamespace.SubNameSpace.ContainingClass+NestedClass, C:\MyExtensions\ExtensionAssembly.dll"
        /// </remarks>
        public static CompilerExtension Load(string extension)
        {
            Type extensionType = null;
            int commaIndex = extension.IndexOf(',');
            string className = String.Empty;
            string assemblyName = extension;

            if (0 <= commaIndex)
            {
                className = extension.Substring(0, commaIndex);
                assemblyName = (extension.Length <= commaIndex + 1 ? String.Empty : extension.Substring(commaIndex + 1));
            }

            className = className.Trim();
            assemblyName = assemblyName.Trim();

            if (null == extensionType && 0 < assemblyName.Length)
            {

                Assembly extensionAssembly;

                // case 3: Absolute path to an assembly
                if (Path.IsPathRooted(assemblyName))
                {
                    extensionAssembly = ExtensionLoadFrom(assemblyName);
                }
                else
                {
                    try
                    {
                        // case 2: AssemblyName
                        extensionAssembly = Assembly.Load(assemblyName);
                    }
                    catch (IOException e)
                    {
                        if (e is FileLoadException || e is FileNotFoundException)
                        {
                            try
                            {
                                // case 4: Filename of an assembly in the application directory
                                extensionAssembly = Assembly.Load(Path.GetFileNameWithoutExtension(assemblyName));
                            }
                            catch (IOException innerE)
                            {
                                if (innerE is FileLoadException || innerE is FileNotFoundException)
                                {
                                    // case 5: Relative path to an assembly

                                    // we want to use Assembly.Load when we can because it has some benefits over Assembly.LoadFrom
                                    // (see the documentation for Assembly.LoadFrom). However, it may fail when the path is a relative
                                    // path, so we should try Assembly.LoadFrom one last time. We could have detected a directory
                                    // separator character and used Assembly.LoadFrom directly, but dealing with path canonicalization
                                    // issues is something we don't want to deal with if we don't have to.
                                    extensionAssembly = ExtensionLoadFrom(assemblyName);
                                }
                                else
                                {
                                    throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.InvalidExtension(assemblyName, innerE.Message), null, 0, 0), innerE);
                                }
                            }
                        }
                        else
                        {
                            throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.InvalidExtension(assemblyName, e.Message), null, 0, 0), e);
                        }
                    }
                }

                if (0 < className.Length)
                {
                    try
                    {
                        // case 1: AssemblyQualifiedName
                        extensionType = extensionAssembly.GetType(className, true, true);
                    }
                    catch (Exception e)
                    {
                        throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.InvalidExtensionClassName(assemblyName, className, e.Message), null, 0, 0), e);
                    }
                }
                else
                {
                    // if no class name was specified, then let's hope the assembly defined a default CompilerExtension
                    DefaultCompilerExtensionAttribute extensionAttribute = (DefaultCompilerExtensionAttribute)Attribute.GetCustomAttribute(extensionAssembly, typeof(DefaultCompilerExtensionAttribute));

                    if (null != extensionAttribute)
                    {
                        extensionType = extensionAttribute.ExtensionType;
                    }
                    else
                    {
                        throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.ExpectedDefaultCompilerExtensionAttribute(assemblyName, typeof(DefaultCompilerExtensionAttribute)), null, 0, 0));
                    }
                }
            }

            if (extensionType.IsSubclassOf(typeof(CompilerExtension)))
            {
                return Activator.CreateInstance(extensionType) as CompilerExtension;
            }
            else
            {
                throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.InvalidExtensionType(extension, extensionType, typeof(CompilerExtension)), null, 0, 0));
            }
        }

        protected void OnMessage(CompilerMessageEventArgs e)
        {
            if (this.Messages != null)
            {
                this.Messages(this, e);
            }
        }

        private static Assembly ExtensionLoadFrom(string assemblyName)
        {
            Assembly extensionAssembly = null;

            try
            {
                extensionAssembly = Assembly.LoadFrom(assemblyName);
            }
            catch (Exception e)
            {
                throw new CompilerException(new CompilerMessageEventArgs(CompilerMessage.InvalidExtension(assemblyName, e.Message), null, 0, 0), e);
            }

            return extensionAssembly;
        }
    }
}
