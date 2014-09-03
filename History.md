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
