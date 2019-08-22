// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace WixToolset.Simplified.TypedMessageGenerator.Editor
{
    internal static class TypeConstants
    {
        public const string Content = "TypedMessage.Content";
        public const string MsgsExtension = ".msgs";

        public const string Comment = "TypedMessage.Comment";
        public const string Whitespace = "TypedMessage.Whitespace";
        public const string Keyword = "TypedMessage.Keyword";
        public const string MessageTypeDefinition = "TypedMessage.MessageTypeDefinition";
        public const string MessageTypeRange = "TypedMessage.MessageTypeRange";
        public const string MessageType = "TypedMessage.MessageType";
        public const string MessageName = "TypedMessage.MessageName";
        public const string String = "TypedMessage.String";
        public const string Escape = "TypedMessage.Escape";
        public const string ReplacementDelimiter = "TypedMessage.ReplacementDelimiter";
        public const string ReplacementStart = "TypedMessage.ReplacementStart";
        public const string ReplacementEnd = "TypedMessage.ReplacementEnd";
        public const string ReplacementName = "TypedMessage.ReplacementName";
        public const string ReplacementType = "TypedMessage.ReplacementType";
        public const string ReplacementPosition = "TypedMessage.ReplacementPosition";
        public const string ReplacementAlignment = "TypedMessage.ReplacementAlignment";
        public const string ReplacementFormat = "TypedMessage.ReplacementFormat";
    }

    internal static class TypeDefinitions
    {
        [Export]
        [Name(TypeConstants.Content)]
        [BaseDefinition("code")]
        internal static ContentTypeDefinition TypedMessageContentTypeDefinition = null;

        [Export]
        [FileExtension(TypeConstants.MsgsExtension)]
        [ContentType(TypeConstants.Content)]
        internal static FileExtensionToContentTypeDefinition MsgsFileExtensionDefinition = null;

        [Export]
        [Name(TypeConstants.Comment)]
        [BaseDefinition("comment")]
        internal static ClassificationTypeDefinition Comment = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.Comment)]
        [Name(TypeConstants.Comment)]
        [DisplayName(TypeConstants.Comment)]
        [UserVisible(true)]
        [Order(After=Priority.Default, Before = Priority.High)]
        internal sealed class CommentFormat : ClassificationFormatDefinition
        {
            public CommentFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Green;
            }
        }

        [Export]
        [Name(TypeConstants.Whitespace)]
        [BaseDefinition("whitespace")]
        internal static ClassificationTypeDefinition Whitespace = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.Whitespace)]
        [Name(TypeConstants.Whitespace)]
        [DisplayName(TypeConstants.Whitespace)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class WhitespaceFormat : ClassificationFormatDefinition
        {
            public WhitespaceFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Black;
            }
        }

        [Export]
        [Name(TypeConstants.Keyword)]
        [BaseDefinition("keyword")]
        internal static ClassificationTypeDefinition Keyword = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.Keyword)]
        [Name(TypeConstants.Keyword)]
        [DisplayName(TypeConstants.Keyword)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class KeywordFormat : ClassificationFormatDefinition
        {
            public KeywordFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Blue;
            }
        }

        [Export]
        [Name(TypeConstants.MessageTypeDefinition)]
        [BaseDefinition("symbol definition")]
        internal static ClassificationTypeDefinition MessageTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.MessageTypeDefinition)]
        [Name(TypeConstants.MessageTypeDefinition)]
        [DisplayName(TypeConstants.MessageTypeDefinition)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class MessageTypeDefinitionFormat : ClassificationFormatDefinition
        {
            public MessageTypeDefinitionFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Teal;
            }
        }

        [Export]
        [Name(TypeConstants.MessageTypeRange)]
        [BaseDefinition("number")]
        internal static ClassificationTypeDefinition MessageTypeRange = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.MessageTypeRange)]
        [Name(TypeConstants.MessageTypeRange)]
        [DisplayName(TypeConstants.MessageTypeRange)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class MessageTypeRangeFormat : ClassificationFormatDefinition
        {
            public MessageTypeRangeFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Black;
            }
        }

        [Export]
        [Name(TypeConstants.MessageType)]
        [BaseDefinition("symbol definition")]
        internal static ClassificationTypeDefinition MessageType = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.MessageType)]
        [Name(TypeConstants.MessageType)]
        [DisplayName(TypeConstants.MessageType)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class MessageTypeFormat : ClassificationFormatDefinition
        {
            public MessageTypeFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Teal;
            }
        }

        [Export]
        [Name(TypeConstants.MessageName)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition MessageName = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.MessageName)]
        [Name(TypeConstants.MessageName)]
        [DisplayName(TypeConstants.MessageName)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class MessageNameFormat : ClassificationFormatDefinition
        {
            public MessageNameFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Black;
            }
        }

        [Export]
        [Name(TypeConstants.String)]
        [BaseDefinition("string")]
        internal static ClassificationTypeDefinition String = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.String)]
        [Name(TypeConstants.String)]
        [DisplayName(TypeConstants.String)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class StringFormat : ClassificationFormatDefinition
        {
            public StringFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Maroon;
            }
        }

        [Export]
        [Name(TypeConstants.Escape)]
        [BaseDefinition("string")]
        internal static ClassificationTypeDefinition Escape = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.Escape)]
        [Name(TypeConstants.Escape)]
        [DisplayName(TypeConstants.Escape)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class EscapeFormat : ClassificationFormatDefinition
        {
            public EscapeFormat()
            {
                this.ForegroundColor = System.Windows.Media.Colors.Red;
            }
        }

        [Export]
        [Name(TypeConstants.ReplacementDelimiter)]
        [BaseDefinition("other")]
        internal static ClassificationTypeDefinition ReplacementDelimiter = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.ReplacementDelimiter)]
        [Name(TypeConstants.ReplacementDelimiter)]
        [DisplayName(TypeConstants.ReplacementDelimiter)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class ReplacementDelimiterFormat : ClassificationFormatDefinition
        {
            public ReplacementDelimiterFormat()
            {
                this.ForegroundColor = System.Windows.Media.Colors.Silver;
            }
        }

        [Export]
        [Name(TypeConstants.ReplacementStart)]
        [BaseDefinition(TypeConstants.ReplacementDelimiter)]
        internal static ClassificationTypeDefinition ReplacementStart = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.ReplacementStart)]
        [Name(TypeConstants.ReplacementStart)]
        [DisplayName(TypeConstants.ReplacementStart)]
        [UserVisible(false)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class ReplacementStartFormat : ClassificationFormatDefinition
        {
            public ReplacementStartFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Silver;
            }
        }

        [Export]
        [Name(TypeConstants.ReplacementEnd)]
        [BaseDefinition(TypeConstants.ReplacementDelimiter)]
        internal static ClassificationTypeDefinition ReplacementEnd = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.ReplacementEnd)]
        [Name(TypeConstants.ReplacementEnd)]
        [DisplayName(TypeConstants.ReplacementEnd)]
        [UserVisible(false)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class ReplacementEndFormat : ClassificationFormatDefinition
        {
            public ReplacementEndFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Silver;
            }
        }

        [Export]
        [Name(TypeConstants.ReplacementName)]
        [BaseDefinition("identifier")]
        internal static ClassificationTypeDefinition ReplacementName = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.ReplacementName)]
        [Name(TypeConstants.ReplacementName)]
        [DisplayName(TypeConstants.ReplacementName)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class ReplacementNameFormat : ClassificationFormatDefinition
        {
            public ReplacementNameFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Black;
            }
        }

        [Export]
        [Name(TypeConstants.ReplacementType)]
        [BaseDefinition("symbol definition")]
        internal static ClassificationTypeDefinition ReplacementType = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.ReplacementType)]
        [Name(TypeConstants.ReplacementType)]
        [DisplayName(TypeConstants.ReplacementType)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class ReplacementTypeFormat : ClassificationFormatDefinition
        {
            public ReplacementTypeFormat()
            {
                ////this.ForegroundColor = System.Windows.Media.Colors.Teal;
            }
        }

        [Export]
        [Name(TypeConstants.ReplacementPosition)]
        [BaseDefinition("number")]
        internal static ClassificationTypeDefinition ReplacementPosition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.ReplacementPosition)]
        [Name(TypeConstants.ReplacementPosition)]
        [DisplayName(TypeConstants.ReplacementPosition)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class ReplacementPositionFormat : ClassificationFormatDefinition
        {
            public ReplacementPositionFormat()
            {
                this.ForegroundColor = System.Windows.Media.Colors.Gray;
            }
        }

        [Export]
        [Name(TypeConstants.ReplacementAlignment)]
        [BaseDefinition("other")]
        internal static ClassificationTypeDefinition ReplacementAlignment = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.ReplacementAlignment)]
        [Name(TypeConstants.ReplacementAlignment)]
        [DisplayName(TypeConstants.ReplacementAlignment)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class ReplacementAlignmentFormat : ClassificationFormatDefinition
        {
            public ReplacementAlignmentFormat()
            {
                this.ForegroundColor = System.Windows.Media.Colors.Silver;
            }
        }

        [Export]
        [Name(TypeConstants.ReplacementFormat)]
        [BaseDefinition("other")]
        internal static ClassificationTypeDefinition ReplacementFormat = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = TypeConstants.ReplacementFormat)]
        [Name(TypeConstants.ReplacementFormat)]
        [DisplayName(TypeConstants.ReplacementFormat)]
        [UserVisible(true)]
        [Order(After = Priority.Default, Before = Priority.High)]
        internal sealed class ReplacementFormatFormat : ClassificationFormatDefinition
        {
            public ReplacementFormatFormat()
            {
                this.ForegroundColor = System.Windows.Media.Colors.Silver;
            }
        }
    }
}
