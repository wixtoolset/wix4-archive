* BobArnson: Add libs_minimal.proj with just the libraries needed for tools/ tree build. This prevents the build from backing up behind a full libs/ tree build, which gets more painful the more versions of Visual Studio that are installed.

* BobArnson: Project reference tweaks: 
  - Removed unnecessary reference to setupicons from x64msi.
  - Move BuildInParallel=false from global to just project that needs it