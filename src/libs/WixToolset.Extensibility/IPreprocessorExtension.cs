//-------------------------------------------------------------------------------------------------
// <copyright file="IPreprocessorExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensibility
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Data;

    /// <summary>
    /// Interface for extending the WiX toolset preprocessor.
    /// </summary>
    public interface IPreprocessorExtension
    {
        /// <summary>
        /// Gets or sets the preprocessor core for the extension.
        /// </summary>
        /// <value>Preprocessor core for the extension.</value>
        IPreprocessorCore Core { get; set; }

        /// <summary>
        /// Gets the variable prefixes for the extension.
        /// </summary>
        /// <value>The variable prefixes for the extension.</value>
        string[] Prefixes { get; }

        /// <summary>
        /// Called at the beginning of the preprocessing of a source file.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets the value of a variable whose prefix matches the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the variable to be processed by the extension.</param>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The value of the variable or null if the variable is undefined.</returns>
        string GetVariableValue(string prefix, string name);

        /// <summary>
        /// Evaluates a function defined in the extension.
        /// </summary>
        /// <param name="prefix">The prefix of the function to be processed by the extension.</param>
        /// <param name="function">The name of the function.</param>
        /// <param name="args">The list of arguments.</param>
        /// <returns>The value of the function or null if the function is not defined.</returns>
        string EvaluateFunction(string prefix, string function, string[] args);

        /// <summary>
        /// Processes a pragma defined in the extension.
        /// </summary>
        /// <param name="sourceLineNumbers">The location of this pragma's PI.</param>
        /// <param name="prefix">The prefix of the pragma to be processed by the extension.</param>
        /// <param name="pragma">The name of the pragma.</param>
        /// <param name="args">The pragma's arguments.</param>
        /// <param name="parent">The parent node of the pragma.</param>
        /// <returns>false if the pragma is not defined.</returns>
        /// <comments>Don't return false for any condition except for unrecognized pragmas. Use Core.OnMessage for errors, warnings and messages.</comments>
        bool ProcessPragma(SourceLineNumber sourceLineNumbers, string prefix, string pragma, string args, XContainer parent);

        /// <summary>
        /// Preprocess a document after normal preprocessing has completed.
        /// </summary>
        /// <param name="document">The document to preprocess.</param>
        void PreprocessDocument(XDocument document);

        /// <summary>
        /// Preprocesses a parameter.
        /// </summary>
        /// <param name="name">Name of parameter that matches extension.</param>
        /// <returns>The value of the parameter after processing.</returns>
        /// <remarks>By default this method will cause an error if its called.</remarks>
        string PreprocessParameter(string name);

        /// <summary>
        /// Called at the end of the preprocessing of a source file.
        /// </summary>
        void Finish();
    }
}
