//-------------------------------------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "hwnd", Scope = "member", Target = "WixToolset.Bootstrapper.Engine.#Apply(System.IntPtr)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "hwnd", Scope = "member", Target = "WixToolset.Bootstrapper.Engine.#Elevate(System.IntPtr)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Scope = "type", Target = "WixToolset.Bootstrapper.Engine+Variables`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "WixToolset.Bootstrapper.DetectMsiFeatureEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "WixToolset.Bootstrapper.DetectRelatedMsiPackageEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "WixToolset.Bootstrapper.DetectTargetMsiPackageEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Scope = "type", Target = "WixToolset.Bootstrapper.ResultEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Scope = "type", Target = "WixToolset.Bootstrapper.ResultStatusEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Scope = "type", Target = "WixToolset.Bootstrapper.StatusEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Scope = "type", Target = "WixToolset.Bootstrapper.BootstrapperException")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Recache", Scope = "member", Target = "WixToolset.Bootstrapper.ActionState.#Recache")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "2#", Scope = "member", Target = "WixToolset.Bootstrapper.Engine.#SetDownloadSource(System.String,System.String,System.String,System.String,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2217:DoNotMarkEnumsWithFlags", Scope = "type", Target = "WixToolset.Bootstrapper.EndSessionReasons")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "WixToolset.Bootstrapper.EndSessionReasons")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Logoff", Scope = "member", Target = "WixToolset.Bootstrapper.EndSessionReasons.#Logoff")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Addon", Scope = "member", Target = "WixToolset.Bootstrapper.RelationType.#Addon")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "WixToolset.Bootstrapper.Engine.Log(WixToolset.Bootstrapper.LogLevel,System.String)", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#OnStartup(WixToolset.Bootstrapper.StartupEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sha", Scope = "member", Target = "WixToolset.Bootstrapper.UpdateHashType.#Sha1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#OnDetectMsiFeature(WixToolset.Bootstrapper.DetectMsiFeatureEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#OnDetectRelatedMsiPackage(WixToolset.Bootstrapper.DetectRelatedMsiPackageEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#OnDetectTargetMsiPackage(WixToolset.Bootstrapper.DetectTargetMsiPackageEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#OnExecuteMsiMessage(WixToolset.Bootstrapper.ExecuteMsiMessageEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#OnPlanMsiFeature(WixToolset.Bootstrapper.PlanMsiFeatureEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#OnPlanTargetMsiPackage(WixToolset.Bootstrapper.PlanTargetMsiPackageEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#DetectMsiFeature")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#DetectRelatedMsiPackage")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#DetectTargetMsiPackage")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#ExecuteMsiMessage")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#PlanMsiFeature")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "WixToolset.Bootstrapper.ExecuteMsiMessageEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "WixToolset.Bootstrapper.PlanMsiFeatureEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "WixToolset.Bootstrapper.PlanTargetMsiPackageEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "WixToolset.Bootstrapper.BootstrapperApplication.#PlanTargetMsiPackage")]
