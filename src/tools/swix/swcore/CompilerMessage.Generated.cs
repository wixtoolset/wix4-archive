// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Simplified
{
    using System;
    
    
    /// <summary>CompilerMessage generated messages class.</summary>
    /// <remarks>To change any behavior in this class, the code generator will need to change.</remarks>
    public sealed class CompilerMessage
    {
        
        private static System.Resources.ResourceManager resourceManager;
        
        private static System.Globalization.CultureInfo resourceCulture;
        
        private CompilerMessageType type;
        
        private int id;
        
        private string name;
        
        private string message;
        
        private CompilerMessage(CompilerMessageType type, int id, string name, string message)
        {
            this.type = type;
            this.id = id;
            this.name = name;
            this.message = message;
        }
        
        /// <summary>Returns the cached ResourceManager instance used by this class.</summary>
        internal static System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(CompilerMessage.resourceManager, null))
                {
                    System.Resources.ResourceManager temp = new System.Resources.ResourceManager("WixToolset.Simplified.CompilerMessage.Generated", typeof(CompilerMessage).Assembly);
                    CompilerMessage.resourceManager = temp;
                }
                return CompilerMessage.resourceManager;
            }
        }
        
        /// <summary>Overrides the current thread's CurrentUICulture property for all resource lookups using this strongly typed resource class.</summary>
        internal static System.Globalization.CultureInfo Culture
        {
            get
            {
                return CompilerMessage.resourceCulture;
            }
            set
            {
                CompilerMessage.resourceCulture = value;
            }
        }
        
        /// <summary>Gets the type (error/warning/verbose) of the message</summary>
        public CompilerMessageType Type
        {
            get
            {
                return this.type;
            }
        }
        
        /// <summary>Gets the ID of the message.</summary>
        public int Id
        {
            get
            {
                return this.id;
            }
        }
        
        /// <summary>Gets the name of the message.</summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }
        
        /// <summary>Get the message text for the message.</summary>
        public string Message
        {
            get
            {
                return this.message;
            }
        }
        
        
        #line 34 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InternalError(string message)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InternalError", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 0, "InternalError", string.Format(messageFormat, message));
        }
        
        #line default
        #line hidden
        
        
        #line 37 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DanglingAssignmentNoMember()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.DanglingAssignmentNoMember", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.LexerError, 1000, "DanglingAssignmentNoMember", message);
        }
        
        #line default
        #line hidden
        
        
        #line 40 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DanglingAssignmentNoValue()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.DanglingAssignmentNoValue", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.LexerError, 1001, "DanglingAssignmentNoValue", message);
        }
        
        #line default
        #line hidden
        
        
        #line 43 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DoubledAssignment()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.DoubledAssignment", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.LexerError, 1002, "DoubledAssignment", message);
        }
        
        #line default
        #line hidden
        
        
        #line 46 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage MustAssignToMember()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.MustAssignToMember", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.LexerError, 1003, "MustAssignToMember", message);
        }
        
        #line default
        #line hidden
        
        
        #line 49 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage ValueHasEmbeddedQuote()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.ValueHasEmbeddedQuote", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.LexerError, 1004, "ValueHasEmbeddedQuote", message);
        }
        
        #line default
        #line hidden
        
        
        #line 52 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnclosedQuotedValue()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnclosedQuotedValue", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.LexerError, 1005, "UnclosedQuotedValue", message);
        }
        
        #line default
        #line hidden
        
        
        #line 55 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InfiniteLoopInSubsitution(string value)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InfiniteLoopInSubsitution", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1100, "InfiniteLoopInSubsitution", string.Format(messageFormat, value));
        }
        
        #line default
        #line hidden
        
        
        #line 58 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnknownSubstitution(string name)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnknownSubstitution", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1101, "UnknownSubstitution", string.Format(messageFormat, name));
        }
        
        #line default
        #line hidden
        
        
        #line 61 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage GroupMissingId()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.GroupMissingId", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1102, "GroupMissingId", message);
        }
        
        #line default
        #line hidden
        
        
        #line 64 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage FileMissingNameAndSource()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.FileMissingNameAndSource", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1103, "FileMissingNameAndSource", message);
        }
        
        #line default
        #line hidden
        
        
        #line 67 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InvalidIdResolution(string targetPropertyName, string lookup, string targetTypeName, string actualTypeName)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InvalidIdResolution", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1104, "InvalidIdResolution", string.Format(messageFormat, targetPropertyName, lookup, targetTypeName, actualTypeName));
        }
        
        #line default
        #line hidden
        
        
        #line 70 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnknownIdResolution(string targetPropertyName, string lookup, string targetTypeName)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnknownIdResolution", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1105, "UnknownIdResolution", string.Format(messageFormat, targetPropertyName, lookup, targetTypeName));
        }
        
        #line default
        #line hidden
        
        
        #line 73 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnknownFileSystemResolution(string targetPropertyName, string lookup, string targetTypeName)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnknownFileSystemResolution", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1106, "UnknownFileSystemResolution", string.Format(messageFormat, targetPropertyName, lookup, targetTypeName));
        }
        
        #line default
        #line hidden
        
        
        #line 76 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage OverwritingImplicitProperty(string targetPropertyName, string lookup)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.OverwritingImplicitProperty", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Warning, 2000, "OverwritingImplicitProperty", string.Format(messageFormat, targetPropertyName, lookup));
        }
        
        #line default
        #line hidden
        
        
        #line 79 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage ApplicationNameRequired()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.ApplicationNameRequired", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1107, "ApplicationNameRequired", message);
        }
        
        #line default
        #line hidden
        
        
        #line 82 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage FileNotFound(string path)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.FileNotFound", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1108, "FileNotFound", string.Format(messageFormat, path));
        }
        
        #line default
        #line hidden
        
        
        #line 85 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InvalidFileName(string filename)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InvalidFileName", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1109, "InvalidFileName", string.Format(messageFormat, filename));
        }
        
        #line default
        #line hidden
        
        
        #line 88 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage PathTooLong(string path)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.PathTooLong", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1110, "PathTooLong", string.Format(messageFormat, path));
        }
        
        #line default
        #line hidden
        
        
        #line 91 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage CannotTransferFile(string sourcePath, string destinationPath)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.CannotTransferFile", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1111, "CannotTransferFile", string.Format(messageFormat, sourcePath, destinationPath));
        }
        
        #line default
        #line hidden
        
        
        #line 94 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage CopyFile(string sourcePath, string destinationPath)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.CopyFile", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Verbose, 4000, "CopyFile", string.Format(messageFormat, sourcePath, destinationPath));
        }
        
        #line default
        #line hidden
        
        
        #line 97 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage MoveFile(string sourcePath, string destinationPath)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.MoveFile", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Verbose, 4001, "MoveFile", string.Format(messageFormat, sourcePath, destinationPath));
        }
        
        #line default
        #line hidden
        
        
        #line 100 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage RemovingDestinationFile(string file)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.RemovingDestinationFile", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Verbose, 4002, "RemovingDestinationFile", string.Format(messageFormat, file));
        }
        
        #line default
        #line hidden
        
        
        #line 103 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage CompressFile(string file)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.CompressFile", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Verbose, 4003, "CompressFile", string.Format(messageFormat, file));
        }
        
        #line default
        #line hidden
        
        
        #line 106 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnexpectedValidationError(string detail)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnexpectedValidationError", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1112, "UnexpectedValidationError", string.Format(messageFormat, detail));
        }
        
        #line default
        #line hidden
        
        
        #line 109 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DeprecatedItem(string deprecatedElement)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.DeprecatedItem_0", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Warning, 2001, "DeprecatedItem", string.Format(messageFormat, deprecatedElement));
        }
        
        #line default
        #line hidden
        
        
        #line 110 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DeprecatedItem(string deprecatedElement, string replacementElement)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.DeprecatedItem_1", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Warning, 2001, "DeprecatedItem", string.Format(messageFormat, deprecatedElement, replacementElement));
        }
        
        #line default
        #line hidden
        
        
        #line 113 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage RequiredAttribute(string element, string attribute)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.RequiredAttribute", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1113, "RequiredAttribute", string.Format(messageFormat, element, attribute));
        }
        
        #line default
        #line hidden
        
        
        #line 116 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DeprecatedPackageFrameworkAttribute()
        {
            string message = CompilerMessage.ResourceManager.GetString("CompilerMessage.DeprecatedPackageFrameworkAttribute", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1114, "DeprecatedPackageFrameworkAttribute", message);
        }
        
        #line default
        #line hidden
        
        
        #line 119 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DeprecatedAttribute(string element, string deprecatedAttribute)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.DeprecatedAttribute_0", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Warning, 2002, "DeprecatedAttribute", string.Format(messageFormat, element, deprecatedAttribute));
        }
        
        #line default
        #line hidden
        
        
        #line 120 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DeprecatedAttribute(string element, string deprecatedAttribute, string replacementElement, string replacementAttribute)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.DeprecatedAttribute_1", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Warning, 2002, "DeprecatedAttribute", string.Format(messageFormat, element, deprecatedAttribute, replacementElement, replacementAttribute));
        }
        
        #line default
        #line hidden
        
        
        #line 123 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DeprecatedAttributeValue(string element, string attribute, object deprecatedValue)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.DeprecatedAttributeValue_0", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Warning, 2003, "DeprecatedAttributeValue", string.Format(messageFormat, element, attribute, deprecatedValue));
        }
        
        #line default
        #line hidden
        
        
        #line 124 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage DeprecatedAttributeValue(string element, string attribute, object deprecatedValue, string replacementValue)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.DeprecatedAttributeValue_1", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Warning, 2003, "DeprecatedAttributeValue", string.Format(messageFormat, element, attribute, deprecatedValue, replacementValue));
        }
        
        #line default
        #line hidden
        
        
        #line 127 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage HighlanderElement(string element)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.HighlanderElement_0", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1115, "HighlanderElement", string.Format(messageFormat, element));
        }
        
        #line default
        #line hidden
        
        
        #line 128 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage HighlanderElement(string parentElement, string childElement)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.HighlanderElement_1", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1115, "HighlanderElement", string.Format(messageFormat, parentElement, childElement));
        }
        
        #line default
        #line hidden
        
        
        #line 131 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage HighlanderElementWithAttributeValue(string element, string attribute, string value)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.HighlanderElementWithAttributeValue", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1116, "HighlanderElementWithAttributeValue", string.Format(messageFormat, element, attribute, value));
        }
        
        #line default
        #line hidden
        
        
        #line 134 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage RequiredElement(string requiredElement)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.RequiredElement_0", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1117, "RequiredElement", string.Format(messageFormat, requiredElement));
        }
        
        #line default
        #line hidden
        
        
        #line 135 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage RequiredElement(string parentElement, string requiredChildElement)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.RequiredElement_1", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1117, "RequiredElement", string.Format(messageFormat, parentElement, requiredChildElement));
        }
        
        #line default
        #line hidden
        
        
        #line 138 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InvalidAttributeValue(string element, string attribute, string invalidValue)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InvalidAttributeValue", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1118, "InvalidAttributeValue", string.Format(messageFormat, element, attribute, invalidValue));
        }
        
        #line default
        #line hidden
        
        
        #line 141 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage SavedManifest(string path)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.SavedManifest", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Verbose, 4004, "SavedManifest", string.Format(messageFormat, path));
        }
        
        #line default
        #line hidden
        
        
        #line 144 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage XamlParseError(string message)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.XamlParseError", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1119, "XamlParseError", string.Format(messageFormat, message));
        }
        
        #line default
        #line hidden
        
        
        #line 147 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage XamlWriteError(string message)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.XamlWriteError", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1120, "XamlWriteError", string.Format(messageFormat, message));
        }
        
        #line default
        #line hidden
        
        
        #line 150 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InvalidExtension(string extension, string message)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InvalidExtension", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1121, "InvalidExtension", string.Format(messageFormat, extension, message));
        }
        
        #line default
        #line hidden
        
        
        #line 153 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InvalidExtensionType(string extension, System.Type actualType, System.Type expectedType)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InvalidExtensionType", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1122, "InvalidExtensionType", string.Format(messageFormat, extension, actualType, expectedType));
        }
        
        #line default
        #line hidden
        
        
        #line 156 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InvalidExtensionClassName(string extension, string className, string message)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InvalidExtensionClassName", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1123, "InvalidExtensionClassName", string.Format(messageFormat, extension, className, message));
        }
        
        #line default
        #line hidden
        
        
        #line 159 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage ExpectedDefaultCompilerExtensionAttribute(string extension, System.Type attribute)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.ExpectedDefaultCompilerExtensionAttribute", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1124, "ExpectedDefaultCompilerExtensionAttribute", string.Format(messageFormat, extension, attribute));
        }
        
        #line default
        #line hidden
        
        
        #line 162 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage WebAppDoesNotAllowAttribute(string attribute)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.WebAppDoesNotAllowAttribute", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1125, "WebAppDoesNotAllowAttribute", string.Format(messageFormat, attribute));
        }
        
        #line default
        #line hidden
        
        
        #line 165 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage AttributeRequiresAttribute(string attribute, string otherAttribute)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.AttributeRequiresAttribute", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1126, "AttributeRequiresAttribute", string.Format(messageFormat, attribute, otherAttribute));
        }
        
        #line default
        #line hidden
        
        
        #line 168 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage AttributeIgnoringDuplicateValue(string element, string attribute, object value)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.AttributeIgnoringDuplicateValue", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Warning, 2004, "AttributeIgnoringDuplicateValue", string.Format(messageFormat, element, attribute, value));
        }
        
        #line default
        #line hidden
        
        
        #line 171 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage InvalidFolderReference(string id, string type)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.InvalidFolderReference", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1127, "InvalidFolderReference", string.Format(messageFormat, id, type));
        }
        
        #line default
        #line hidden
        
        
        #line 174 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnknownNamespacePrefix(string prefix)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnknownNamespacePrefix", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1128, "UnknownNamespacePrefix", string.Format(messageFormat, prefix));
        }
        
        #line default
        #line hidden
        
        
        #line 177 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage ExpectedToken(string token, string value)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.ExpectedToken", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1129, "ExpectedToken", string.Format(messageFormat, token, value));
        }
        
        #line default
        #line hidden
        
        
        #line 180 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnknownItem(string typenamespace, string item)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnknownItem", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1130, "UnknownItem", string.Format(messageFormat, typenamespace, item));
        }
        
        #line default
        #line hidden
        
        
        #line 183 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage AttachedPropertyRequiresNamespace(string name)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.AttachedPropertyRequiresNamespace", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1131, "AttachedPropertyRequiresNamespace", string.Format(messageFormat, name));
        }
        
        #line default
        #line hidden
        
        
        #line 186 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnknownAttachedProperty(string typenamespace, string property)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnknownAttachedProperty", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1132, "UnknownAttachedProperty", string.Format(messageFormat, typenamespace, property));
        }
        
        #line default
        #line hidden
        
        
        #line 189 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnknownAttachedPropertyValue(string typenamespace, string property, string name)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnknownAttachedPropertyValue", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1133, "UnknownAttachedPropertyValue", string.Format(messageFormat, typenamespace, property, name));
        }
        
        #line default
        #line hidden
        
        
        #line 192 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage CannotAssignAttachedPropertyVale(string typenamespace, string property, string name, string value)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.CannotAssignAttachedPropertyVale", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1134, "CannotAssignAttachedPropertyVale", string.Format(messageFormat, typenamespace, property, name, value));
        }
        
        #line default
        #line hidden
        
        
        #line 195 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage PropertyCannotSpecifyNamespace(string property, string typenamespace)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.PropertyCannotSpecifyNamespace", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1135, "PropertyCannotSpecifyNamespace", string.Format(messageFormat, property, typenamespace));
        }
        
        #line default
        #line hidden
        
        
        #line 198 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage UnknownProperty(string property)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.UnknownProperty", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1136, "UnknownProperty", string.Format(messageFormat, property));
        }
        
        #line default
        #line hidden
        
        
        #line 201 "C:\hg\swix\src\swcore\CompilerMessage.msgs"
        public static CompilerMessage CannotAssignPropertyValue(string property, string value)
        {
            string messageFormat = CompilerMessage.ResourceManager.GetString("CompilerMessage.CannotAssignPropertyValue", CompilerMessage.resourceCulture);
            return new CompilerMessage(CompilerMessageType.Error, 1137, "CannotAssignPropertyValue", string.Format(messageFormat, property, value));
        }
        
        #line default
        #line hidden
        
        /// <summary>The allowable types of messages.</summary>
        /// <remarks>To change this list, add 'type' lines to your source file.</remarks>
        public enum CompilerMessageType
        {
            
            /// <summary>'lexerError' message range: 1000-1099</summary>
            LexerError,
            
            /// <summary>'error' message range: 1100-1999</summary>
            Error,
            
            /// <summary>'warning' message range: 2000-2999</summary>
            Warning,
            
            /// <summary>'information' message range: 3000-3999</summary>
            Information,
            
            /// <summary>'verbose' message range: 4000-4999</summary>
            Verbose,
        }
    }
}
