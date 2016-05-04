// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using WixToolset.Simplified.CompilerFrontend.Parser;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using System.Windows.Media;

namespace WixToolset.Simplified.LanguageService
{
    internal static class TypeConstants
    {
        public const string Content = "Swix.Content";

        public const string RtypeContent = "Swix.Content.Rtype";
        public const string RtypeExtension = ".swr";

        public const string XmlContent = "Swix.Content.Xml";
        public const string XmlExtension = ".swx";

        public const string SwixComment = "Swix.Comment";
        public const string SwixKeyword = "Swix.Keyword";
        public const string SwixObject = "Swix.Object";
        public const string SwixProperty = "Swix.Property";
        public const string SwixValue = "Swix.Value";
        public const string SwixAttachableObject = "Swix.AttachableObject";
        public const string SwixNamespacePrefix = "Swix.NamespacePrefix";
        public const string SwixEquals = "Swix.Equals";
        public const string SwixDelimiter = "Swix.XmlDelimiters";
    }

    internal static class ColorConstants
    {
        public static readonly Color SwixComment = Color.FromRgb(0, 128, 0);
        public static readonly Color SwixKeyword = Color.FromRgb(0, 0, 255);
        public static readonly Color SwixObject = Color.FromRgb(0, 0, 204);
        public static readonly Color SwixProperty = Color.FromRgb(0, 0, 0);
        public static readonly Color SwixValue = Color.FromRgb(153, 0, 0);
        public static readonly Color SwixAttachableObject = Color.FromRgb(43, 145, 175);
        public static readonly Color SwixNamespacePrefix = Color.FromRgb(43, 145, 175);
        public static readonly Color SwixEquals = Color.FromRgb(153, 153, 153);
        public static readonly Color SwixDelimiter = Color.FromRgb(153, 153, 153);
    }

    internal static class TypeDefinitions
    {
        [Export]
        [Name(TypeConstants.Content)]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition SwixContentTypeDefinition = null;

        // Rtype
        [Export]
        [Name(TypeConstants.RtypeContent)]
        [BaseDefinition(TypeConstants.Content)]
        internal static ContentTypeDefinition SwixRtypeContentTypeDefinition = null;

        [Export]
        [FileExtension(TypeConstants.RtypeExtension)]
        [ContentType(TypeConstants.RtypeContent)]
        internal static FileExtensionToContentTypeDefinition SwrFileExtensionDefinition = null;

        // XML
        [Export]
        [Name(TypeConstants.XmlContent)]
        [BaseDefinition(TypeConstants.Content)]
        [BaseDefinition("XML")]
        internal static ContentTypeDefinition SwixXmlContentTypeDefinition = null;

        [Export]
        [FileExtension(TypeConstants.XmlExtension)]
        [ContentType(TypeConstants.XmlContent)]
        internal static FileExtensionToContentTypeDefinition SwxFileExtensionDefinition = null;

        // Classification types...
        [Export]
        [Name(TypeConstants.SwixComment)]
        [BaseDefinition(PredefinedClassificationTypeNames.Comment)]
        internal static ClassificationTypeDefinition SwixCommentClassificationTypeDefinition = null;

        [Export]
        [Name(TypeConstants.SwixKeyword)]
        [BaseDefinition(PredefinedClassificationTypeNames.Keyword)]
        internal static ClassificationTypeDefinition SwixKeywordClassificationTypeDefinition = null;

        [Export]
        [Name(TypeConstants.SwixObject)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal static ClassificationTypeDefinition SwixObjectClassificationTypeDefinition = null;

        [Export]
        [Name(TypeConstants.SwixProperty)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal static ClassificationTypeDefinition SwixPropertyClassificationTypeDefinition = null;

        [Export]
        [Name(TypeConstants.SwixValue)]
        [BaseDefinition(PredefinedClassificationTypeNames.String)]
        internal static ClassificationTypeDefinition SwixValueClassificationTypeDefinition = null;

        [Export]
        [Name(TypeConstants.SwixAttachableObject)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal static ClassificationTypeDefinition SwixAttachableObjectClassificationTypeDefinition = null;

        [Export]
        [Name(TypeConstants.SwixNamespacePrefix)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        internal static ClassificationTypeDefinition SwixNamespacePrefixClassificationTypeDefinition = null;

        [Export]
        [Name(TypeConstants.SwixEquals)]
        [BaseDefinition(PredefinedClassificationTypeNames.Operator)]
        internal static ClassificationTypeDefinition SwixEqualsClassificationTypeDefinition = null;

        [Export]
        [Name(TypeConstants.SwixDelimiter)]
        [BaseDefinition(PredefinedClassificationTypeNames.Other)]
        internal static ClassificationTypeDefinition SwixDelimiterClassificationTypeDefinition = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixComment)]
    [Name(TypeConstants.SwixComment)]
    [DisplayName(TypeConstants.SwixComment)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixCommentFormat : ClassificationFormatDefinition
    {
        public SwixCommentFormat()
        {
            this.ForegroundColor = ColorConstants.SwixComment;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixKeyword)]
    [Name(TypeConstants.SwixKeyword)]
    [DisplayName(TypeConstants.SwixKeyword)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixKeywordFormat : ClassificationFormatDefinition
    {
        public SwixKeywordFormat()
        {
            this.ForegroundColor = ColorConstants.SwixKeyword;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixObject)]
    [Name(TypeConstants.SwixObject)]
    [DisplayName(TypeConstants.SwixObject)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixObjectFormat : ClassificationFormatDefinition
    {
        public SwixObjectFormat()
        {
            this.ForegroundColor = ColorConstants.SwixObject;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixProperty)]
    [Name(TypeConstants.SwixProperty)]
    [DisplayName(TypeConstants.SwixProperty)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixPropertyFormat : ClassificationFormatDefinition
    {
        public SwixPropertyFormat()
        {
            this.ForegroundColor = ColorConstants.SwixProperty;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixValue)]
    [Name(TypeConstants.SwixValue)]
    [DisplayName(TypeConstants.SwixValue)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixValueFormat : ClassificationFormatDefinition
    {
        public SwixValueFormat()
        {
            this.ForegroundColor = ColorConstants.SwixValue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixAttachableObject)]
    [Name(TypeConstants.SwixAttachableObject)]
    [DisplayName(TypeConstants.SwixAttachableObject)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixAttachableObjectFormat : ClassificationFormatDefinition
    {
        public SwixAttachableObjectFormat()
        {
            this.ForegroundColor = ColorConstants.SwixAttachableObject;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixNamespacePrefix)]
    [Name(TypeConstants.SwixNamespacePrefix)]
    [DisplayName(TypeConstants.SwixNamespacePrefix)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixNamespacePrefixFormat : ClassificationFormatDefinition
    {
        public SwixNamespacePrefixFormat()
        {
            this.ForegroundColor = ColorConstants.SwixNamespacePrefix;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixEquals)]
    [Name(TypeConstants.SwixEquals)]
    [DisplayName(TypeConstants.SwixEquals)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixEqualsFormat : ClassificationFormatDefinition
    {
        public SwixEqualsFormat()
        {
            this.ForegroundColor = ColorConstants.SwixEquals;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = TypeConstants.SwixDelimiter)]
    [Name(TypeConstants.SwixDelimiter)]
    [DisplayName(TypeConstants.SwixDelimiter)] // TODO: get a better display name!
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class SwixDelimiterFormat : ClassificationFormatDefinition
    {
        public SwixDelimiterFormat()
        {
            this.ForegroundColor = ColorConstants.SwixDelimiter;
        }
    }

    internal static class TypeContentExtensions
    {
        public static ParserLanguage GetParserLanguage(this IContentType contentType)
        {
            if (contentType.IsOfType(TypeConstants.RtypeContent))
            {
                return ParserLanguage.Rtype;
            }

            if (contentType.IsOfType(TypeConstants.XmlContent))
            {
                return ParserLanguage.Xml;
            }

            System.Diagnostics.Debug.Fail("Unknown content type!");
            return ParserLanguage.Unknown;
        }
    }
}
