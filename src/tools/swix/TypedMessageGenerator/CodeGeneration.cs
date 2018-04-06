// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WixToolset.Simplified.TypedMessageGenerator
{
    internal class CodeGeneration
    {
        private string defaultNamespace;
        private string className;

        // TODO: Convert some internal parameters to members?
        public string GenerateCode(string codeNamespace, MessageData messages, string className)
        {
            this.defaultNamespace = codeNamespace;
            this.className = className;

            // We use "string" so much that we have a common reference...
            CodeTypeReference stringRef = new CodeTypeReference(typeof(string));

            CodeCompileUnit compileUnit = new CodeCompileUnit();

            CodeNamespace cns = new CodeNamespace(codeNamespace);
            cns.Imports.Add(new CodeNamespaceImport("System"));
            CodeTypeDeclarationCollection types = cns.Types;

            compileUnit.Namespaces.Add(cns);

            CodeTypeDeclaration theClass = new CodeTypeDeclaration(className);
            CodeTypeReference theClassRef = new CodeTypeReference(new CodeTypeParameter(className));
            theClass.IsClass = true;
            theClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;

            types.Add(theClass);

            theClass.Comments.Add(
                new CodeCommentStatement(
                    string.Format(
                        "<summary>{0} generated messages class.</summary>",
                        className),
                    true));

            theClass.Comments.Add(
                new CodeCommentStatement(
                    "<remarks>To change any behavior in this class, the code generator will need to change.</remarks>",
                    true));

            CodeMethodReferenceExpression resManGetString = this.CreateResourceManager(theClass);
            CodeFieldReferenceExpression resCultureRef = this.CreateResourceCulture(theClass);

            // Add the message types (error, warning, verbose...)
            string messageTypeName = string.Concat(className, "Type");
            CodeTypeDeclaration messageTypes = new CodeTypeDeclaration(messageTypeName);
            messageTypes.IsEnum = true;
            messageTypes.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
            theClass.Members.Add(messageTypes);

            messageTypes.Comments.Add(
                new CodeCommentStatement(
                    "<summary>The allowable types of messages.</summary>",
                    true));

            messageTypes.Comments.Add(
                new CodeCommentStatement(
                    "<remarks>To change this list, add 'type' lines to your source file.</remarks>",
                    true));

            CodeTypeReferenceExpression messageTypeRef = new CodeTypeReferenceExpression(messageTypeName);
            Dictionary<MessageType, CodeFieldReferenceExpression> typeReferences = new Dictionary<MessageType, CodeFieldReferenceExpression>();

            foreach (MessageType sourceMessageType in messages.Types)
            {
                CodeMemberField typeField = new CodeMemberField();
                // We capitalize the message type's first letter for the code.
                typeField.Name = string.Concat(
                    sourceMessageType.Name[0].ToString().ToUpperInvariant(),
                    sourceMessageType.Name.Substring(1));

                messageTypes.Members.Add(typeField);

                typeField.Comments.Add(
                    new CodeCommentStatement(
                        string.Format(
                            "<summary>'{0}' message range: {1}-{2}</summary>",
                            sourceMessageType.Name,
                            sourceMessageType.FirstId,
                            sourceMessageType.LastId),
                        true));

                typeReferences.Add(sourceMessageType, new CodeFieldReferenceExpression(messageTypeRef, typeField.Name));
            }

            CodeConstructor classConstructor = new CodeConstructor();
            classConstructor.Attributes = MemberAttributes.Private;
            theClass.Members.Add(classConstructor);

            // Add the members and private constructor...
            this.CreateProperty(theClass, classConstructor, "Type", new CodeTypeReference(messageTypeName), "Gets the type (error/warning/verbose) of the message");
            this.CreateProperty(theClass, classConstructor, "Id", new CodeTypeReference(typeof(int)), "Gets the ID of the message.");
            this.CreateProperty(theClass, classConstructor, "Name", stringRef, "Gets the name of the message.");
            this.CreateProperty(theClass, classConstructor, "Message", stringRef, "Get the message text for the message.");

            foreach (var message in messages.Messages)
            {
                if (message.Type == null)
                {
                    // TODO: throw an error?  Skip?
                    continue;
                }

                CodeFieldReferenceExpression messageType = typeReferences[message.Type];

                foreach (var instance in message.Instances)
                {
                    CodeMemberMethod method = new CodeMemberMethod();
                    method.Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final; // final == non-virtual?
                    method.ReturnType = theClassRef;
                    method.Name = message.Name;
                    method.LinePragma = new CodeLinePragma(messages.Filename, instance.PragmaLine);

                    theClass.Members.Add(method);

                    string messageVarName = "message";

                    // Ensure we don't have any variable name collisions...
                    if (instance.ParameterList.Count > 0)
                    {
                        messageVarName = "messageFormat";
                        string messageVarNameBase = messageVarName;
                        int suffixCount = 1;
                        while (instance.ParameterList.Any(t => string.Equals(t.Item1, messageVarName, StringComparison.Ordinal)))
                        {
                            messageVarName = string.Concat(messageVarNameBase, (++suffixCount).ToString());
                        }
                    }

                    ////// TODO: Inject an error if there was an error in the source file?
                    ////// This would help avoid missing these errors in a command-line scenario...
                    ////if (!string.IsNullOrEmpty(message.Error))
                    ////{
                    ////    method.Statements.Add(new CodePrimitiveExpression("ERROR!"));
                    ////    method.Statements.Add(new CodePrimitiveExpression(message.Error));
                    ////    method.Statements.Add(new CodePrimitiveExpression("ERROR!"));
                    ////}

                    ////// TODO: Inject an error if there was an error in the source file?
                    ////// This would help avoid missing these errors in a command-line scenario...
                    ////if (!string.IsNullOrEmpty(instance.Error))
                    ////{
                    ////    method.Statements.Add(new CodePrimitiveExpression("ERROR!"));
                    ////    method.Statements.Add(new CodePrimitiveExpression(instance.Error));
                    ////    method.Statements.Add(new CodePrimitiveExpression("ERROR!"));
                    ////}

                    // Get the string from the generated resources...
                    method.Statements.Add(
                        new CodeVariableDeclarationStatement(
                            stringRef,
                            messageVarName,
                            new CodeMethodInvokeExpression(
                                resManGetString,
                                new CodePrimitiveExpression(string.Concat(this.className, ".", instance.Name)),
                                resCultureRef)));

                    // Default the return expression to just the message itself.
                    CodeExpression messageExpression = new CodeVariableReferenceExpression(messageVarName);

                    // If we've got parameterList, we need a more complex expression.
                    if (instance.ParameterList.Count > 0)
                    {
                        List<CodeExpression> formatParameters = new List<CodeExpression>();

                        formatParameters.Add(messageExpression);

                        instance.ParameterList.ForEach(t =>
                        {
                            method.Parameters.Add(
                                new CodeParameterDeclarationExpression(
                                    new CodeTypeReference(t.Item2),
                                    t.Item1));

                            formatParameters.Add(new CodeVariableReferenceExpression(t.Item1));
                        });

                        messageExpression = new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(stringRef),
                            "Format",
                            formatParameters.ToArray());
                    }

                    method.Statements.Add(new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            theClassRef,
                            messageType, // type
                            new CodePrimitiveExpression(message.Id), // id
                            new CodePrimitiveExpression(message.Name), // name
                            messageExpression))); // message
                }
            }

            // Create the code...
            string output = null;

            using (CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp"))
            {
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BracingStyle = "C";

                using (StringWriter sourceWriter = new StringWriter())
                {
                    provider.GenerateCodeFromCompileUnit(compileUnit, sourceWriter, options);
                    output = sourceWriter.ToString();
                }
            }

            return output;
        }

        private void CreateProperty(CodeTypeDeclaration theClass, CodeConstructor classConstructor, string name, CodeTypeReference typeRef, string comment)
        {
            string privateMember = name.ToLowerInvariant();

            theClass.Members.Add(new CodeMemberField(typeRef, privateMember));

            CodeFieldReferenceExpression fieldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), privateMember);

            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Name = name;
            prop.Type = typeRef;
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final; // final == non-virtual?

            prop.Comments.Add(new CodeCommentStatement(string.Format("<summary>{0}</summary>", comment), true));


            prop.GetStatements.Add(new CodeMethodReturnStatement(fieldRef));

            theClass.Members.Add(prop);

            string paramName = name.ToLowerInvariant(); // same as private member, but we don't want to assume!

            classConstructor.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    typeRef,
                    paramName));

            classConstructor.Statements.Add(
                new CodeAssignStatement(
                    fieldRef,
                    new CodeVariableReferenceExpression(paramName)));
        }

        private CodeMethodReferenceExpression CreateResourceManager(CodeTypeDeclaration theClass)
        {
            CodeTypeReference typeRef = new CodeTypeReference(typeof(System.Resources.ResourceManager));
            var field = new CodeMemberField(typeRef, "resourceManager");
            field.Attributes = MemberAttributes.Static | MemberAttributes.Private;
            theClass.Members.Add(field);

            CodeFieldReferenceExpression fieldRef = new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(
                    new CodeTypeReference(theClass.Name)),
                    field.Name);

            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Name = "ResourceManager";
            prop.Type = typeRef;
            prop.Attributes = MemberAttributes.Static | MemberAttributes.Assembly;

            prop.Comments.Add(new CodeCommentStatement(string.Format("<summary>{0}</summary>", "Returns the cached ResourceManager instance used by this class."), true));

            // if the private static field hasn't been set, create to a temp, and assign.
            // (That's what ResX does!)
            prop.GetStatements.Add(
                new CodeConditionStatement(
                        new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(object)),
                            "ReferenceEquals",
                            fieldRef,
                            new CodePrimitiveExpression(null)),
                        new CodeVariableDeclarationStatement(
                            typeRef,
                            "temp",
                            new CodeObjectCreateExpression(
                                typeRef,
                                new CodePrimitiveExpression(string.Concat(this.defaultNamespace, ".", this.className, ".Generated")),
                                    new CodeFieldReferenceExpression(
                                        new CodeTypeOfExpression(new CodeTypeReference(theClass.Name)),
                                        "Assembly"))),
                        new CodeAssignStatement(
                            fieldRef,
                            new CodeVariableReferenceExpression("temp"))));

            prop.GetStatements.Add(new CodeMethodReturnStatement(fieldRef));

            theClass.Members.Add(prop);

            CodePropertyReferenceExpression propRef = new CodePropertyReferenceExpression(
                new CodeTypeReferenceExpression(
                    new CodeTypeReference(theClass.Name)),
                    prop.Name);

            CodeMethodReferenceExpression getStringRef = new CodeMethodReferenceExpression(propRef, "GetString");
            return getStringRef;
        }

        private CodeFieldReferenceExpression CreateResourceCulture(CodeTypeDeclaration theClass)
        {
            CodeTypeReference typeRef = new CodeTypeReference(typeof(System.Globalization.CultureInfo));
            var field = new CodeMemberField(typeRef, "resourceCulture");
            field.Attributes = MemberAttributes.Static | MemberAttributes.Private;
            theClass.Members.Add(field);

            CodeFieldReferenceExpression fieldRef = new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(
                    new CodeTypeReference(theClass.Name)),
                    field.Name);

            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Name = "Culture";
            prop.Type = typeRef;
            prop.Attributes = MemberAttributes.Static | MemberAttributes.Assembly;

            prop.Comments.Add(new CodeCommentStatement(string.Format("<summary>{0}</summary>", "Overrides the current thread's CurrentUICulture property for all resource lookups using this strongly typed resource class."), true));

            prop.GetStatements.Add(new CodeMethodReturnStatement(fieldRef));
            prop.SetStatements.Add(new CodeAssignStatement(fieldRef, new CodeVariableReferenceExpression("value")));

            theClass.Members.Add(prop);

            return fieldRef;
        }
    }
}
