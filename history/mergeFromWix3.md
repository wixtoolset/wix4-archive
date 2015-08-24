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