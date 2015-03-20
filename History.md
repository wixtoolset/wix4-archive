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
