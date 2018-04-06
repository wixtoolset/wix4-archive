## WixBuild: Version 4.0.0.5918

* RobMen - WIXBUG:5724 - fix DLL hijack of clean room when bundle launched elevated.

*SeanHall: WIXFEAT:4572 - Add OnElevateComplete callback to Burn.

## WixBuild: Version 4.0.0.5512

* RobMen: Standardize on .NET Framework v4.5

* HeathS: WIXBUG:5597 - Check VS2017 product IDs against supported SKUs

## WixBuild: Version 4.0.0.5205

* leshy84: WIXBUG:5548 - fix WiX Burn (mbahost) not to leave del*.tmp files in temp directory

## WixBuild: Version 4.0.0.5204

* @Barnson: Add VS2017 native SDK to bundle.

* @Barnson: Fix #5537 by ensuring TargetDir ends in a backslash.

* @Barnson: Prevent TargetPath project reference preprocessor variable from 
getting multiple identical items.

* SeanHall: WIXFEAT:5510 - Point to WiX VS extensions in sidebar.

* @barnson: Add WixStdBA/PreqBA loc string for ERROR_FAIL_NOACTION_REBOOT.

* BMurri: WIXBUG:5285 - Add UserUILanguageID variable

* jabo2: WIXFEAT:3163 - add ability to specify an icon for InternetShortcut

* @Barnson: Fix util:InternetShortcut.
  * Make icon optional.
  * Add query of icon and index to immediate CA.
  * Add wixtoolset.org shortcut with icon.

* HeathS: WIXFEAT:5433 - Add detection properties for VS2017

* SeanHall: WIXBUG:5521 - Add BOOTSTRAPPER_ERROR_TYPE_APPLY to managed IBootstrapperApplication definition.

* SeanHall: WIXBUG:4929 - Fix infinite loop in PathCreateTimeBasedTempFile that caused Burn to hang if it didn't have rights to create the log file. Also, write entry in Application event log when Burn is unable to create the Failed log.

* SeanHall: WIXFEAT:5435 - When loading the BA, include the BA's directory in the DLL search path.

* RobMen: WIXBUG:4903 - Remove internal WixStandardBootstrapperApplication.Foundation from documentation.

* RobMen: WIXBUG:5270 - Properly set WixMerge columns nullable.

* RobMen: WIXBUG:4813 - Properly modularize IIsWebApplication Name column.

* RobMen: WIXBUG:4812 - Properly modularize IIsAppPool Name column.

* RobMen: WIXBUG:5265 - Prevent bad use of ".." in payload names in the Binder.

* RobMen: WIXBUG:5307 - Correctly set and document SystemFolder and System64Folder in Burn.

* Barnson: Fix null-check to check the right null. Fixes wixtoolset/issues/issues#5318.

* Barnson: Add LICENSE.TXT to binaries .zip. Fixes wixtoolset/issues/issues#5474.

* @barnson: Remove MS-RL references from MS-PL VS MPF sources.

* Barnson: Add support for Visual Studio 2017 (current as of 15.0.26127.3).
Adds C++ CA template but doesn't install it because templates are per-instance.
Implements wixtoolset/issues/issues/5484.

* Barnson: Correct condition to detect missing .NET 2.0/3.5.1.

* Barnson: Correct `RemoveFolder` entry in shortcut how-to. Fixed wixtoolset/issues/issues#4869.

* Barnson: Prevent use of SSE for old-old CPUs. Fixes wixtoolset/issues/issues#4876.

* ujdhesa: Add sq-AL translation for Wix4.

* MarcSch WIXBUG:4932 - Adding german localization for SqlExtension

* @barnson: Implement wixtoolset/issues#5412: Add tooltips to ThmUtil
  - New Tooltip element as a child of a control element.
  - Add ThemeShowControlEx to make it easy to "poke" a control, to refresh its content based on variables and conditions.

* nirbar: WIXFEAT:5386 - Add initial support for MSI transactions in Burn.

* @barnson: Fix RPC_S_SERVER_UNAVAILABLE as HRESULT, which it is not.

* fperin: Add es-es and fr-fr translations for WixUtilExtension.

* zakred: Add es-es translation for WixFirewallExtension.

* apmorton: Fix app.config name translation for DLL projects.

* @barnson for @firegiantco: Update FileVersionFromStringEx to handle "vVersion" syntax like FileVersionFromString.

* FRichter: WIXBUG:5277 - burn engine: Always use the bundle source path for all purposes. The original source path is needed in all cases: for copying the bundle to the layout directory as well as for checking whether we're layouting to the bundle location.

* @barnson: Enable the WiX native SDK when the new Visual C++ Build Tools bundle is installed. Fixes WIXBUG:5279.

RobMen: WIXBUG:5282 - reduce clean room security to successfully load BA's dependent on GDI+ (including WinForms).

* @barnson: Fix WIXBUG:5293 - Document illegal MsiProperty names.

* nathan-alden: WIXBUG:5370 - Support the newly-released .NET Framework 4.6.2

* @barnson: WIXBUG:5360: Replace Markdown-style link with HTML-style link to make the Markdown processor happy.

* SeanHall: WIXBUG:5301 - Fix bug where file handles weren't being passed to the clean room process.

* SeanHall: WIXBUG:5302 - Fix bug where the command line for burn exe packages had the executable path in the middle.

* SeanHall: WIXBUG:5308 - Make embedded bundles launch a clean room process so the BA runs in a consistent environment.

* SeanHall: WIXFEAT:4950 - Specify BAFunctions DLL at compile time.

* PhillHgl: WIXBUG:5331 - fix ConfigureSmbUninstall CA failures (on install) when util:FileShare/@Description was longer than 73 chars.  Increased to 255 chars, matching table definition.

* @barnson removed license blurb from ThemeGen templates.

## WixBuild: Version 4.0.0.4506

* SeanHall: WIXBUG:4164 - In WixBA, treat closing the window during apply as if the user clicked the Cancel button and then close the app automatically on ApplyComplete.

* SeanHall: WIXFEAT:4217 - In WixBA when launched from ARP, require user interaction before starting to uninstall. When the user cancels, say that instead of failed.

* SeanHall: WIXFEAT:5195 - **BREAKING CHANGE** Changed bundle version to Major.Minor.0.BuildNumber. This allows us to publish updates as Major.Minor.(GreaterThanZero).BuildNumber. MSI product version numbers remain Major.Minor.BuildNumber so major upgrades continue to work. This bundle will not upgrade from build v4.0.4305.0. If you've installed v4.0.4305.0, you must uninstall before installing a later bundle.

* PhillHgl: WIXFEAT:5352 - New UI for the WiX Toolset setup:
      * Refactored WixBA to have a non-wizard View which is more familiar to Windows 10 users. 
      * Added support to display Update 'content' when an update is available.
      * Added support for 'Windows Ease of Access' High Contrast display modes and screen reader (i.e. Narrator) tools.
      * Added standard window chrome controls and the ability for user resizing.

## WixBuild: Version 4.0.4305.0

+* @barnson: WIXBUG:5306: Warn against ServiceConfig and ServiceConfigFailureActions.

* @barnson: Fix WIXBUG:5294 - Move MsiProperty check from 
  ProcessMsiPackageCommand to the compiler for the earliest feedback.

* @barnson: Prevent bad use of ".." in payload names. Fixes wixtoolset/issues#4265.

* FabienL: WIXBUG:4976 - Add support for .net framework 4.6.1 in netfxExtension

* SeanHall: WIXBUG:4810 - Fix bug where mbapreq tried to do something other than HELP or INSTALL.

* SeanHall: WIXBUG:5234 - Make Burn grab a file handle to the original bundle so it can still access compressed payloads even if the original bundle was deleted after initialization (e.g. when a bundle uses Burn's built-in update mechanism). Also, when launching the clean room Burn process, pass a file handle of the original exe to the process on the command line. Use this file handle when using the attached container.

* SeanHall: WIXBUG:5238 - Get the engine's file handle as soon as possible.  Also, when launching Burn processes, pass a file handle of the exe to the process on the command line.

## WixBuild: Version 4.0.3922.0

* SeanHall: WIXFEAT:4951 - Make the BAFunctions interface binary, make WixStdBA forward all BA messages to BAFunctions, and give BAFunctions access to the theme and localization.

* PavelAnfimov: WIXBUG:4772 - Add support for Windows XP for ShelExecUnelevated.

* DavidFlamme: WIXBUG:4785 - Fixing memory leak in InstallPackage.cs

MikeGC: WIXBUG:4878 - fix iniutil memory leak

* jchoover: WIXBUG:4899 - Modified WixStdBA handling of XmlGetAttribute to handle S_FALSE.

* BobArnson: Work around Dev14 change that breaks XP compatibility even when used with v140_xp toolset. See [Connect item on this very issue](https://connect.microsoft.com/VisualStudio/feedback/details/1789709/visual-c-2015-runtime-broken-on-windows-server-2003-c-11-magic-statics). Fixes WIXBUG:4902 and WIXBUG:4908.

* @barnson: Correct file name of local name for Web package per wixtoolset/issues#4904

* @barnson: Write VersionMajor/VersionMinor Uninstall values.
  * Partial fix for wixtoolset/issues#5171. (Does not write InstallLocation.)

* @barnson: WIXBUG:5185 - correct LPWSTR to LPCWSTR

* @barnson for @firegiantco: Fix wixtoolset/issues#5221.
  * Use better logic to determine when to send the Burn ancestors switches.

* MikeGC: Add simple combo box support to ThmUtil.

* @barnson: Fix up bad/old links in Learning WiX topic.

* HeathS: WIXFEAT:5230 - Support adding users to Performance Log users group

* HeathS: WIXBUG:5229 - Allow processes to terminate with 0 exit code

* HeathS: WIXBUG:5241 - Call OnExecutePackageComplete only when OnExecutePackageBegin was called

## WixBuild: Version 4.0.3826.0

* @barnson: Add support for command links to ThmUtil.
  * Command links are supported on Windows Vista and later. On Windows XP, they're plain buttons.

* @barnson: Support system colors in themes.
  * Implements wixtoolset/issues#4787.
  * The following "colors" are supported in Font/@Foreground and Font/@Background with the color indices as used by [::GetSysColor](https://msdn.microsoft.com/en-us/library/windows/desktop/ms724371%28v=vs.85%29.aspx):
    * btnface: COLOR_BTNFACE
    * btntext: COLOR_BTNTEXT
    * graytext: COLOR_GRAYTEXT
    * highlight: COLOR_HIGHLIGHT
    * highlighttext: COLOR_HIGHLIGHTTEXT
    * hotlight: COLOR_HOTLIGHT
    * window: COLOR_WINDOW
    * windowtext: COLOR_WINDOWTEXT

* @firegiantco via @barnson: ThmUtil cleanups.
  * ThemeControlExists should take a `const THEME*`.
  * ThemeSetTextControlEx exposed clunky `fInvalidateControl` and `fInvalidateParent` arguments in an attempt to force controls to redraw. Replace with `fUpdate` more generically and, hidden in the code, hide and show the control, which we found to be the cleanest way of getting transparent text redrawn over a graphical background.

* SeanHall: WIXBUG:4857 - Fix DTF custom actions in Win10 Apps and Features. 
* SeanHall: Make VS2015 C++ custom action project template reference VS2015 WiX libs.

* BMurri: WIXBUG:5132 - fix incomplete usage of kbKeyBitness parameter of RegDelete() function in DUtil's RegUtil.

## WixBuild: Version 4.0.3729.0

* jchoover: WIXBUG:5193 - Fix /layout default directory with clean room:
  * Attempt to use WixBundleSourceProcessPath / WixBundleSourceProcessFolder before defaulting to PathForCurrentProcess
  * Prevent trying to layout the origional bundle exe on top of the existing exe, if the path provided to layout is the same as the bundles working directory.

* RobMen: Improve performance of compiler when handling anonymous Directories and Components and simple references.

* @barnson: Add child control support to ThmUtil.
  * Add Panel control to hold children.
  * Billboards can now host panels for mixing text and graphics.
  * Graphic buttons can now have localized text drawn on them.
  * Graphic buttons now support four states: unselected, hover, selected, focused.
  * Controls now have a DisableAutomaticBehavior attribute to turn off variable getting and setting, EnableCondition, VisibleCondition, and conditional Text elements.

* @barnson: Add ThemeGen utility, which generates the control and page arrays to simplify control and page management for ThmUtil.

* BMurri: WIXBUG:5186 - Fix build warning MSB3277:
  * Starting with VSIP 10 SP1 the Assemblies directory was split into two subdirectories: v2.0 & v4.0. That change wasn't properly reflected in the code, rendering many of the various HintPaths ineffective.
  * MSBuild was picking a different version of Microsoft.VisualStudio.CommonIDE than the one most likely intended (and that particular assembly never was in VSIP 2010 SP1).
  * It's possible that the build warning could possibly mask some unexpected behaviors introduced after support for some VS post-2010 was added do to the different version of the assembly selected from the original code's assumption.

* MikeGC: Add StrAllocConcatFormatted to concatenate a formatted string to an existing string.

* BobArnson: Have Burn rewrite ARP DisplayName during modify so changes to WixBundleName are reflected in ARP.

* BobArnson: Change Burn's behavior to, instead of skipping all related bundles when the current bundle is embedded, skip only dependent bundles when the current bundle is a related bundle. (Burn supports embedded mode in cases other than when being executed as a related bundle.)

* @barnson: Make CBalBaseBootstrapperApplication::PromptCancel usefully overrideable.
  * Make cancellation related members protected rather than private so PromptCancel can be overridden to provide custom cancellation prompt UI.

* @barnson: Add FormatFiles custom action from FireGiant.
  * FormatFiles element as a child of File schedules a custom action to format the file's content at install time (using [Formatted](http://msdn.microsoft.com/library/aa368609.aspx) data type rules).
  * Adds PSCZ C++ class to DUtil for automatic memory management for LPWSTR strings.

* @barnson: Adding some useful WcaUtil functions from FireGiant.
  * Add QuietExecCapture to get process output as a string.
  * Add WcaExtractBinaryToBuffer, WcaExtractBinaryToFile, and WcaExtractBinaryToString to extract content from the Binary table.

* BobArnson: ThmViewer and ThmUtil usability updates.
  * ThmViewer: Update theme for ThmUtil v4 schema. Show theme load errors. Add support for specifying a .wxl on the command line to view localized strings.
  * ThmUtil: Automatically localize controls if their name is the same as a .wxl loc string id.

* SeanHall: Change the interface between the Burn engine and BA's by moving to messages instead of COM interfaces.

* RobMen: WIXBUG:5184 - Create Burn "clean room" to prevent DLL hijacking.

* RobMen: Do not continue reading in unknown file formats.

* MikeGC: Fix ability to load Wixpdbs.

## WixBuild: Version 4.0.3430.0

* HeathS: WIXFEAT:4909 - Add NuGet package for dutil

* BobArnson: Update ThmViewer theme for ThmUtil v4 schema.

* BobArnson: Add `DisableVS201x` properties to skip versions of VS during the build.
  This is useful to save build time during debugging and to diagnose codegen problems in different versions of VS.

* BobArnson: WIXFEAT:4719: Implement MsiProperty/@Condition.

* HeathS: WIXBUG:4880 - Add detection support for VS15

* BMurri: WIXBUG:3902 - Fix ability to find config files in certain circumstances.

* jmcooper8654: WIXFEAT:4437 - Modify Wix.CA.targets to add PDB files to CA Package when /p:Configuration=Debug.

* BMurri: WIXBUG:4499 - Bind MBApreq language to correct langid

* ErnestT: WIXBUG:4432 - Add burn support for only caching packages

* HeathS: Refactor cache-only support in Burn
  * Remove /cache command line support from engine
  * Allow BA to request cache-only support

* HeathS: Fix variadic macros for custom action messages

## WixBuild: Version 4.0.3226.0

* HeathS: WIXBUG:4775 - Always set WixBundleOriginalSourceFolder if not already set

* MikeGC: WIXBUG:4359 - In settings browser, display manifested displayname (instead of ID) for friendlier UI

* MikeGC: Refactor settings browser and make it more responsive, and less likely to show a "loading" screen (instead showing the most recent data until the new data is available)

* RobMen: WIXBUG:4817 - fix error message in OneTimeWixBuildInitialization.proj

* BobArnson: WIXBUG:4804 - Add DAPI to get the right calling convention.

* BobArnson: WIXBUG:4180 - Add wixtoolset.org EULA service links.

* @barnson: WIXFEAT:4789 - Phase I of .NET Framework 4.6 support (RC, full redist).

* Lakmus85: WIXBUG:4614 - Validation added for the case when the project property is not empty (as stated in the comments to this method).

* RobMen: WIXBUG:4778 - initialize logging before anything else.

* JacobHoover: WIXBUG:4512 - fix WiX BA, prevent multiple install clicks

* BobArnson: WIXBUG:4786 - Replace default WixUI license with lorem ipsum text.

* BobArnson: WIXBUG:4760 - Re-guid WixFolderNodeNonMemberProperties

* SeanHall: WIXBUG:4597 - Fix project harvesting in heat for Tools version 12.0 and 14.0.

* BobArnson: WIXFEAT:4719 - Implement ExePackage/CommandLine:
  * Add WixBundleExecutePackageAction variable: Set to the BOOTSTRAPPER_ACTION_STATE of the package as it's about to executed.
  * Add ExePackage/CommandLine to compiler and binder.
  * Update Burn to parse CommandLine table in manifest and apply it during ExePackage execution.

* SeanHall: WIXBUG:4412 - Improve cache progress.

* HeathS: WIXFEAT:4663 - Default to language-specific major upgrades with option to ignore language.

* BMurri: WIXBUG:4532 - Make it an error for InstallPrivileges and InstallScope to both be specified.

## WixBuild: Version 4.0.2926.0

* BobArnson: WIXFEAT:4772 - Replace hyperlink ShelExec with ShelExecUnelevated.

* BobArnson: Add support in locutil to add strings at runtime.

* BobArnson: WIXBUG:4734 - Rewrote type-51 CAs using SetProperty.

* BobArnson: WIXFEAT:4720 - Added bind-time variables for .NET Framework package groups detect condition, install condition, and package directories.

* BobArnson: WIXBUG:4750 - Add a note about binary (in)compatibility.

* RobMen: WIXBUG:4732 - fix documentation links to MsiServiceConfig and MsiServiceConfigFailureActions.

* BobArnson: WIXBUG:4725 - Scrub the WixStdBA license doc and add a blurb about a missing WixStdbaLicenseUrl variable.

* BobArnson: WIXBUG:4721 - Tweak RepairCommand doc.

* SeanHall: WIXFEAT:4763 - Add literal flag to Burn variables to indicate that their values shouldn't be formatted.

* BobArnson: Add libs_minimal.proj with just the libraries needed for tools/ tree build. This prevents the build from backing up behind a full libs/ tree build, which gets more painful the more versions of Visual Studio that are installed.

* BobArnson: Project reference tweaks:
  - Removed unnecessary reference to setupicons from x64msi.
  - Move BuildInParallel=false from global to just project that needs it

* BMurri: WIXBUG:3750 - Add LaunchWorkingFolder to wixstdba to facilitate processes that require a different working folder.

* HeathS: Add VSIX property for VS2015 and fix searches for previous versions.

* HeathS: WIXBUG:3060 - Do not redownload package payloads when /layout is restarted.

* SeanHall: WIXBUG:4761 - Use the package's exit code to tell if the prereq was installed.

* SeanHall: WIXFEAT: 4703 - Move the PrereqPackage information out of the NetFx extension and into the Bal extension so that the NetFx extension doesn't require the Bal extension.

* SeanHall: WIXBUG:4392 - Set WixBundleInstalled during Detect.

* MikeGC: Allow settings engine to detect installation state of products based on ARP reg key name, and add more UDMs

* MikeGC: WIXFEAT:4350 - Settings engine no longer keeps archives of all settings changes forever, it now has a reasonable expiration policy.

* SeanHall: WIXFEAT:4619 - Include WixUI dialogs and wxl files in core MSI.

* SeanHall: WIXFEAT:4618 - Include WixStdBA and mbapreq themes and wxl files in core MSI.

* SeanHall: WIXBUG:4731 - Obscure hidden variable values in the logged command line.

* SeanHall: WIXBUG:4630 - Serialize all variables to the elevated Burn process.

* SeanHall: WIXFEAT:3933 - Make WixBundleManufacturer variable writable.

* BobArnson: WIXBUG:4700 - Added blurb about SequenceType.first.

* SeanHall: Update builtin WixStdBA themes to use the new thmutil functionality.

* SeanHall: WIXFEAT:4411 - Be consistent about when a WixStdBA button is called Close or Cancel.

* SeanHall: WIXFEAT:4658 - Add builtin button functionality in thmutil.

## WixBuild: Version 4.0.2719.0

* BobArnson: WIXBUG:4520 - Added blurb about using a PayloadGroup to get offline capability for .NET redist.

* BobArnson: WIXBUG:4589 - Catch exceptions from Version when passing in arbitrary strings. For bundles, try to recover a four-part version number.

* BobArnson: WIXBUG:4545 - Resized button for de-DE.

* BobArnson: Add WixStdBALanguageId language and documentation.

* BobArnson: WIXBUG:4617 - Added 4.5.2 package group information to doc. Also mentioned that some properties are new to WiX v3.10.

* BobArnson: WIXBUG:4611 - Eliminate mysteriously doubled .pkgdef content.

* BobArnson: WIXBUG:4610 - Write RegisterPerfmonManifest CustomActionData correctly.

* BobArnson: WIXBUG:4553 - Fix Lux generator to exclude any files with non-fragment sections. Fix Lux custom actions to have proper config.

* PhillHogland: WIXBUG:4592 - Register named process, in another user's context with Restart Manager.  If Access Denied, continue install rather than fail.

* thfabba: WIXBUG:4681 - Corrected return type on the lone WOW64 redirection function that returns a BOOLEAN instead of BOOL.

* MikeGC: Fix an issue in ValueMatch() where we can in certain scenarios create unnecessary extra history entries. This is related to upcoming settings expiration feature.

* BobArnson: WIXBUG:4662 - Add WIX_IS_NETFRAMEWORK_4XX_OR_LATER_INSTALLED SetProperty custom actions to WixNetfxExtension.

* BobArnson: WixBroadcastSettingChange and WixBroadcastEnvironmentChange custom actions to WixUtilExtension.

* SeanHall: WIXBUG:4686 - Fix compiling WixBundlePackageExitCodeRow and WixBundleSlipstreamMsp.

* SeanHall: WIXBUG:4393 - Fix BOOTSTRAPPER_REQUEST_STATE_CACHE.

* SeanHall: WIXBUG:4689 - Fix hidden numeric and version variables.

* SeanHall: WIXBUG:4685 - Fix bug in mbahost where it didn't bind as the LegacyV2Runtime when necessary.

* BobArnson: WIXBUG:4654 - Add VS14 properties and custom actions. And, as it's a long topic, added anchors and links.

* SeanHall: WIXBUG:4669 - Fix bug in mbahost where it assumed that the CLRCreateInstance function was implemented when it exists.

* SeanHall: WIXBUG:3747 - Remove define statements for Log and LogLine.

* SeanHall: WIXBUG:4480 - Remove non-standard and unnecessary regex contructs from wix.xsd.

* SeanHall: WIXBUG:4647 - Format ConfirmCancelMessage in WixStdBA.

* SeanHall: WIXBUG:4646 - Allow sharing the whole drive with util:FileShare.

## WixBuild: Version 4.0.2603.0

* SeanHall: Make major breaking changes to thmutil schema while implementing the new Text element to allow setting the text based on context.

* SeanHall: WIXFEAT:4149 - Add support for variables and radio buttons in thmutil, and update WixStdBA to use the new functionality.

* SeanHall: Reimplement multiple prereq packages for v4.

* SeanHall: WIXBUG:4609 - Fix incorrect use of BVariantCopy by creating the new method BVariantSetValue.

* SeanHall: Merge in recent changes from wix3.

* SeanHall: WIXBUG:4608 - Fix bug in mbapreq where it wouldn't reload the bootstrapper if there was a mix of installed and uninstalled prerequisites.

* FireGiant: WIXBUG:4561 - update WixCop to migrate v3 source code to v4.0.

* RobMen: WIXBUG:4565 - add WixToolset.Data.dll and WixToolset.Extensibility.dll to binaries.zip.

## WixBuild: Version 4.0.2220.0

* RobMen: Massive refactor of BindBundleCommand to radically improve code maintainability.

* SeanHall: WIXFEAT:4525 - Reimplement balutil in BootstrapperCore.

* HeathS: WIXBUG:4541 - Add support for high DPI to the Burn engine

* HeathS: Add Windows "Threshold" supported to burn stub.

* MikeGC: Feature #4351: Settings Browser now allows enabling/disabling the display or deleted values and uninstalled products

* HeathS: Add logging for hash verification and registration issues.

* HeathS: WIXBUG:4542 - Pad package sequence number log file names for proper sorting

* HeathS: Redefine Exit\* macros as variadic macros

* MikeGC: Feature #4400: Store UDM Manifests as blobs instead of strings

* MikeGC: Bug #4435: Sync requests must be deduped to avoid the possibility of building up too many sync requests for the same location

* MikeGC: Feature #4355: Settings engine doesn't handle files that are always locked for write (such as database files) very well

* SeanHall: WIXFEAT:4413 - Merge OnApplyNumberOfPhases into OnApplyBegin.

## WixBuild: Version 4.0.2115.0

* RobMen: WIXBUG:4317 - Integrate preprocessor AutoVersion function lost in merges.

* RobMen: WIXBUG:1705 - Include AssemblyFileVersion in MsiAssemblyName table.

* FireGiant: WIXFEAT:4258 - complete introduction of access modifiers for identifiers.

* RobMen: Replace devenv /setup call with fast extension update mechanism.

* RobMen: Add TouchFile custom action.

* SeanHall: WIXFEAT:4505 - WixHttpExtension for URL reservations.

* BMurri: Feature #3635: Write errors to StdErr

## WixBuild: Version 4.0.2102.0

* RobMen: Merge recent changes through WiX v3.9.901.0

* MikeGC: Bug #4506: Make settings browser run non-elevated (when started from settings browser setup). Create WixUnelevatedShellExec to make this possible.

* MikeGC: Bug #4495: Delete settings engine streams safely (only delete after committing the database)

* RobMen: Merge recent changes through WiX v3.9.526.0

* MikeGC: Feature #4352: Settings Engine now has primitive cloud support (tested with dropbox, should work with other similar products)

* MikeGC: Bug #4401: MonUtil can now monitor removable drives (and allow them to be unplugged)

* MikeGC: Bug #4405: LAN Database no longer has annoying locking issue where one client's connection failure causes everyone to fail to sync for a lengthy period of time

* RobMen: Merge recent changes through WiX v3.9.521.0

* RobMen: Fix #4395 by replacing incorrectly removed AssemblyDefaultHeatExtension attribute from VSHeatExtension.

* MikeGC: Bug #4345: Make IniUtil tolerate ini files that have '[' or ']' in the name of a value

* MikeGC: Fix bug in settings browser that can in certain situations result in an inability to look at history of a conflicting value, and other minor bugfixes

* MikeGC: Fix bug in settings engine that can cause unnecessary conflicts to appear upon sync failure in certain situations

* MikeGC: In settings engine, remove parameter from CfgEnumerateProducts() that was never used and has not worked for a long time anyway

## WixBuild: Version 4.0.1621.0

* RobMen: Merge recent changes through WiX v3.9.313.0

* SeanHall: WIXBUG:3643 - Incorrect operation for detect-only package

* MikeGC: Add/tweak a few UDM manifests for settings engine

* MikeGC: In Settings Browser, display times in local time (instead of GMT), and in a more UI friendly format than RFC 3339

* MikeGC: Minor UI tweaks / bugfixes in settings browser (tray popup behavior, listview item selection, and refreshing value history listview when appropriate)

* MikeGC: Display proper state of unreachable remote databases on startup of settings browser

* MikeGC: Fix bug in settings engine where in extended retry periods autosync could accidentally give up on a sync attempt

* MikeGC: Fix bug in settings engine to make file writes more transactional to eliminate chance of losing (or deleting) any changes on the machine while syncing, and allow retry on sharing violation (if we try to write when an app is writing)

* MikeGC: Improve settings engine behavior related to conflicts (completely eliminates a few unnecessary conflicts that can occur in certain situations)

* RobMen: Merge recent changes through WiX v3.9.202.0

* RobMen: WIXBUG:4222 - put DownloadUrls back in the bundle so installs work again.

* SeanHall: Add WixToolset.Data.dll and WixToolset.Extensibility.dll to Toolset.wxs.

## WixBuild: Version 4.0.1320.0

* RobMen: Merge recent changes through WiX v3.9.120.0

* MikeGC: Fix issue running MonUtil test via msbuild from Unit.testproj

## WixBuild: Version 4.0.1216.0

* MikeGC: Fix a race condition where, in network disconnect/reconnect situations, MonUtil could incorrectly send invalid handles to WaitForMultipleObjects (and shut down because of it)

* MikeGC: Fix a few bugs in Settings Browser (UI issue, and tolerate more remote database errors such as remote databases on USB drives being unplugged)

* RobMen: Merge recent changes through WiX v3.9.16.0

* MikeGC: Preserve scroll position on Settings Browser ListView refresh.

* MikeGC: Make value history listview in Settings Browser automatically refresh when syncs occur.

* MikeGC: Allow exporting historical versions of files from Settings Browser.

* MikeGC: Make Settings Browser automatically start on install, restart on repair, and close on uninstall.

* MikeGC: Fix bug in settings engine autosync that would cause it to inadvertently stop monitoring remote databases for changes after detecting a new product had been installed.

* RobMen: Merge recent changes through WiX v3.9.10.0

## WixBuild: Version 4.0.1203.0

* RobMen: Merge recent changes through WiX v3.9.2.0

* MikeGC: Fix Settings Engine to behave better when remote database is not always available due to either USB drive unplugged or a network disconnection.

* RobMen: Merge recent changes through WiX v3.8 RTM.

* RobMen: WIXFEAT:4138 - simplify and improve extensibility model in WiX toolset.

* MikeGC: Fix bug in Settings Engine auto sync related to pushing AND pulling information automatically when first adding a remote database.

* MikeGC: Settings Engine now more reliably connects to remote databases on windows startup, even if it runs before the network has fully initialized.

* RobMen: Merge recent changes through WiX v3.8.1021.0

## WixBuild: Version 4.0.1015.0

* RobMen: Merge recent changes through WiX v3.8.1014.0

* MikeGC: Implement automatic synchronization of settings within Settings Engine / Settings Browser (using MonUtil).

* MikeGC: Make Settings Browser automatically start upon login, start as a tray icon, and minimize back to tray.

* MikeGC: Fix quite a few bugs in Settings Engine and Settings Browser.

## WixBuild: Version 4.0.1007.0

* RobMen: Merge recent changes through WiX v3.8.1007.0

* RobMen: Merge source code reorganization.

* RobMen: Merge recent changes through WiX v3.8.904.0

* MikeGC: MonUtil: Add 32-bit and 64-bit awareness, add support for large numbers of monitors (>64), carefully differentiate between recursive and non-recursive waits, and fix several bugs.

* MikeGC: SceUtil: Add interface to detect whether changes to the database have occurred during a session.

* RobMen: Merge recent changes through WiX v3.8.826.0

* MikeGC: Make Settings Browser window resizable, and enable AutoResize functionality in ThmUtil.

* MikeGC: Introducing MonUtil, which allow easily monitoring directories and registry keys for changes.

* RobMen: Merge recent changes through WiX v3.8.722.0

## WixBuild: Version 4.0.701.0

* RobMen: Merge recent changes through WiX v3.8.628.0.

* RobMen: Merge recent changes through WiX v3.8.611.0.

* MikeGC: Fix bug in settings browser "one instance" lock, switch from a mutex to a per-user lock, and fix some UI thread issues

* MikeGC: Fix pdbs zip and create new udms zip for settings engine manifests

* RobMen: Merge recent changes from WiX v3.8.

* MikeGC: Introducing WiX Settings Engine.

* RobMen: Merge recent changes from WiX v3.8.

## WixBuild: Version 4.0.424.0

* RobMen: Merge recent changes from WiX v3.8.

* RobMen: Add version to schema namespaces.

* RobMen: Move extension schema namespaces under "wxs" to align better with Simplified WiX.
* RobMen: Update Simplified WiX namespaces to match changes "wxs" namespace.

* RobMen: Fix bad old references to thmutil.xsd.

* RobMen: More SxS'ification of folders, registry keys, etc.
* RobMen: Fix Votive registration to correctly load in VS.
* RobMen: Add Simplified WiX Toolset to binaries.zip

* RobMen: Update WixCop to help with all namespace changes (including extensions).
* RobMen: Update thmutil.xsd namespace to be consistent with other changes.

## WixBuild: Version 4.0.4.0

* RobMen: Introducing Simplified WiX Toolset.

* RobMen: Rename "Windows Installer Xml Toolset" to "WiX Toolset".
* RobMen: Improve support for building WiX Toolset with VS2012.
* RobMen: Continue to fix namespace breaking changes.

* RobMen: Change namespaces to prepare for breaking changes.

* RobMen: WiX v4.0

## WixBuild: Version 4.0.0.0
