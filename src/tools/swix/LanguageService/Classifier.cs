// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using WixToolset.Simplified.CompilerFrontend.Parser;
using WixToolset.Simplified.ParserCore;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace WixToolset.Simplified.LanguageService
{
    [Export(typeof(IClassifierProvider))]
    [Name("Swix Classifier")]
    [ContentType(TypeConstants.Content)]
    internal class ClassifierProvider : IClassifierProvider
    {
        /// <summary>
        /// Import the classification registry to be used for getting a reference
        /// to the custom classification type later.
        /// </summary>
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry = null; // Set via MEF

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty<Classifier>(delegate { return new Classifier(ClassificationRegistry, textBuffer); });
        }
    }

    /// <summary>
    /// Classifier for Swix files.
    /// </summary>
    class Classifier : IClassifier
    {
        IClassificationType whitespaceType;
        IClassificationType commentType;
        IClassificationType keywordType;
        IClassificationType objectType;
        IClassificationType propertyType;
        IClassificationType valueType;
        IClassificationType attachableObjectType;
        IClassificationType namespacePrefixType;
        IClassificationType assignmentType;
        IClassificationType delimiterType;

        ParserLanguage language;

        internal Classifier(IClassificationTypeRegistryService registry, ITextBuffer textBuffer)
        {
            this.language = textBuffer.ContentType.GetParserLanguage();

            this.whitespaceType = registry.GetClassificationType(PredefinedClassificationTypeNames.WhiteSpace);
            this.commentType = registry.GetClassificationType(TypeConstants.SwixComment);
            this.keywordType = registry.GetClassificationType(TypeConstants.SwixKeyword);
            this.objectType = registry.GetClassificationType(TypeConstants.SwixObject);
            this.propertyType = registry.GetClassificationType(TypeConstants.SwixProperty);
            this.valueType = registry.GetClassificationType(TypeConstants.SwixValue);
            this.attachableObjectType = registry.GetClassificationType(TypeConstants.SwixAttachableObject);
            this.namespacePrefixType = registry.GetClassificationType(TypeConstants.SwixNamespacePrefix);
            this.assignmentType = registry.GetClassificationType(TypeConstants.SwixEquals);
            this.delimiterType = registry.GetClassificationType(TypeConstants.SwixDelimiter);
        }

        const int UnknownLineNumber = -1;

        /// <summary>
        /// Scans the given SnapshotSpan for potential matches for this classification.
        /// </summary>
        /// <param name="span">The span currently being classified</param>
        /// <returns>A list of ClassificationSpans that represent spans identified to be of this classification</returns>
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            List<ClassificationSpan> classifications = new List<ClassificationSpan>();

            try
            {
                using (ReaderTextProvider textProvider = new ReaderTextProvider(new TextSnapshotToTextReader(span.Snapshot)))
                {
                    // We parse the whole buffer for statements, not just the current line,
                    // because we need to know if we're on a continuation line.
                    // TODO: Cache the "beginning of logical line" points so that we
                    // don't have to re-parse everything each time?
                    IEnumerable<Statement<StatementType, ParserTokenType>> statements = null;

                    switch (this.language)
                    {
                        case ParserLanguage.Rtype:
                            statements = RtypeStatementParser.ParseStatements(new Position(0, 0, 0), textProvider);
                            break;
                        case ParserLanguage.Xml:
                            statements = XmlStatementParser.ParseStatements(new Position(0, 0, 0), textProvider);
                            break;
                    }

                    int spanStart = span.Start.Position;
                    int spanEnd = span.End.Position;

                    foreach (var statement in statements)
                    {
                        if (statement.Range.End.Offset <= spanStart)
                        {
                            continue;
                        }

                        if (statement.Range.Start.Offset >= spanEnd)
                        {
                            break;
                        }

                        // for a comment statement, we don't have to look at the
                        // individual tokens...
                        if (statement.StatementType == StatementType.Comment)
                        {
                            var classification = this.commentType;

                            // Ensure the returned span doesn't extend past the request!
                            var classifiedSpan = span.Intersection(span.Snapshot.CreateSpanFromSwix(statement.Range));

                            if (classifiedSpan.HasValue)
                            {
                                classifications.Add(new ClassificationSpan(classifiedSpan.Value, classification));
                            }
                        }
                        else
                        {
                            foreach (var token in statement.AllTokens)
                            {
                                if (token.Range.End.Offset <= span.Start.Position)
                                {
                                    continue;
                                }

                                if (token.Range.Start.Offset >= span.End.Position)
                                {
                                    break;
                                }

                                IClassificationType classification = null;

                                switch (token.TokenType)
                                {
                                    case ParserTokenType.Unknown:
                                        classification = this.delimiterType;
                                        break;
                                    case ParserTokenType.Whitespace:
                                        classification = this.whitespaceType;
                                        break;
                                    case ParserTokenType.Comment:
                                        classification = this.commentType;
                                        break;
                                    case ParserTokenType.UseKeyword:
                                        classification = this.keywordType;
                                        break;
                                    case ParserTokenType.Object:
                                        classification = this.objectType;
                                        break;
                                    case ParserTokenType.PropertyName:
                                        classification = this.propertyType;
                                        break;
                                    case ParserTokenType.Equals:
                                        classification = this.assignmentType;
                                        break;
                                    case ParserTokenType.PropertyValue:
                                    case ParserTokenType.DoubleQuote:
                                    case ParserTokenType.SingleQuote:
                                        classification = this.valueType;
                                        break;
                                    case ParserTokenType.NamespacePrefix:
                                        classification = this.namespacePrefixType;
                                        break;
                                    case ParserTokenType.NamespacePrefixDeclaration:
                                        // TODO
                                        break;
                                    case ParserTokenType.NamespaceDeclaration:
                                        // TODO
                                        break;
                                    case ParserTokenType.AttachedPropertyObject:
                                        classification = this.attachableObjectType;
                                        break;
                                    case ParserTokenType.LeftAngle:
                                    case ParserTokenType.RightAngle:
                                    case ParserTokenType.Colon:
                                    case ParserTokenType.Slash:
                                    case ParserTokenType.Period:
                                        classification = this.delimiterType;
                                        break;
                                    default:
                                        break;
                                }

                                if (classification != null)
                                {
                                    // Ensure the returned span doesn't extend past the request!
                                    var classifiedSpan = span.Intersection(span.Snapshot.CreateSpanFromSwix(token.Range));

                                    if (classifiedSpan.HasValue)
                                    {
                                        classifications.Add(new ClassificationSpan(classifiedSpan.Value, classification));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return classifications;
        }

#pragma warning disable 67
        // This event gets raised if a non-text change would affect the classification in some way,
        // for example typing /* would cause the classification to change in C# without directly
        // affecting the span.
        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
#pragma warning restore 67
    }
}
