![WiX Toolset Logo](images\wixlogo.jpg)

# Simplified WiX Documentation

* [Overview](#overview)
* [Advantages](#advantages)
* [Syntax](#syntax)
  * [Rtype](#rtype)
  * [File System Syntax](#filesystemsyntax)
  * [Well-known Folders](#wellknownfolders)
* [Reference](#reference)
  * [Package](#package)
  * [Application](#application)
  * [Dependency](#dependency)
  * [Class](#class)
  * [ClassAttribute](#classattribute)
  * [File](#file)
  * [FileAssociation](#fileassociation)
  * [FileType](#filetype)
  * [Folder](#folder)
  * [GameExplorer](#gameexplorer)
  * [InprocServer](#inprocserver)
  * [Interface](#interface)
  * [OutprocServer](#outprocserver)
  * [Protocol](#protocol)
  * [ProxyStub](#proxystub)
* [AppX Reference](#appxreference)
  * [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)
  * [AccountPictureProvider](#appxaccountpictureprovider)
  * [AdditionalTask](#appxadditionaltask)
  * [BackgroundTask](#appxbackgroundtask)
  * [CameraSettings](#appxcamerasettings)
  * [Capability](#appxcapability)
  * [ContactPicker](#appxcontactpicker)
  * [ContentUri](#appxcontenturi)
  * [DataFormat](#appxdataformat)
  * [DeviceCapability](#appxdevicecapability)
  * [FileExtension](#appxfileextension)
  * [FilePicker](#appxfilepicker)
  * [FileOpenPicker](#appxfileopenpicker)
  * [FileSavePicker](#appxfilesavepicker)
  * [InitialRotation](#appxinitialrotation)
  * [LockScreen](#appxlockscreen)
  * [PrinterSettings](#appxprintersettings)
  * [Search](#appxsearch)
  * [SendTarget](#appxsendtarget)
  * [ShareTarget](#appxsharetarget)
  * [SplashScreen](#appxsplashscreen)
  * [Tile](#appxtile)
* [Msi Reference](#msireference)
  * [FileSearch](#msifilesearch)
  * [Ngen](#msingen)
  * [Property](#msiproperty)
* [Extensibility](#extensibility)
  * [Compiler Extension](#compilerextension)
  * [Compiler Extension File Manager](#filemanager)


<h2 id="overview">Overview</h2>

Simplfied WiX is a high level language designed to provide a
clean abstraction across multiple installation technologies.
Traditional WiX exposes a C++ compile/link model to expose
the full power of the Windows Installer. Simplified WiX uses
the a one pass compile model similar to C# and exposes only
core functionality across AppX, Windows Installer and VSIX
installation technologies.

Note: Simplified WiX supports 100% of the AppX and VSIX package
models but only a select portion of the Windows Installer package
model.

<h3 id="advantages">Advantages</h3>

Simplified WiX provides a number of advantages over editing raw
AppX manifest, VSIX manifest or coding in Traditional
WiX toolset.

* Concise - the Simplified WiX  language for representing the
data that goes into the final package is more concise than
the raw AppX manifest. This means less typing and less "noise"
in your source code. For example, the Simplified XML language
is typically 10-20% fewer lines of code than the same AppX
manifest. Using the Simplified WiX Rtype language usually
requires 50-60% less code. When comparing equivalent code in
Traditional WiX to Simplified WiX toolset, the RType language
sometimes reduced the lines up to 80%!

* Composition - Simplified WiX allows you break your package
down into separate text files and compile them all into a single
package. This feature is particularly important if you need to
ship your application as a standalone package as well as being
included in a multi-application package. Also if you have multiple
developers working on the package, composition allows you to have
multiple source files so everyone isn't checking into a single
file which increases the chances of merge conflicts.

* Clean - the final package built is the smallest, cleanest package
possible (if not, it is a bug). For example, insignificant
whitespace and (more importantly) comments are removed from the
final package manifest. Every byte in the final package is evaluated
to ensure it's necessary.

* Cross-compile - part of the Simplified WiX design is to abstract
away as many of the package implementation details so that the
same source code can target different installation technologies.
For example, Simplified WiX can currently target both AppX packages
and .wixlibs which can be used with the Traditional WiX to create MSI
packages. VSIX support is also planned.

* Minimal dependencies - Simplified WiX only depends on NETFX 4.0 being
installed, the rest of the tools can be copied locally (or checked 
into source control) and run on any operating system.

* Error checking - comprehensive and clear, actionable error messages.
There is tremendous value in having many error checks with great error
messages. Simplified WiX is being filled with error handling so that
if a package builds, it should install. Also, as new issues come up we
will be able to add error messages into Simplified WiX to prevent
future packages from being built incorrectly.


<h3 id="syntax">Syntax</h3>

The Simplified WiX syntax is built on XAML. To keep things simple, [markup
extensions](http://msdn.microsoft.com/en-us/library/ms747254.aspx) are NOT used
and [content properties](http://msdn.microsoft.com/en-us/library/system.windows.markup.contentpropertyattribute.aspx)
are used for all elements that contain child elements. This keeps the syntatic
noise in the source documents to a minimum.

Simplified WiX also introduces a custom syntax called "RType". RType is a
minimalistic, white space significant language. It was designed to address the
criticism that XML is overly verbose and noisey to serve as a proper programming
language.

As noted in the advantages above, RType typically reduces source line count by
50%-60% over the same XML version of a Simplified WiX source file.

RType is implemented as a XAML reader so it processes lines of text as start
object (element), start member (attribute), value, end member, end object just
like the XML XAML reader. The result is the compiler can read a mix of XML and
RType documents in the same compile.

The file type for Simplified WiX XML source files is ".swx". 

The file type for Simplified WiX RType source files is ".swr".

<h4 id="rtype">Rtype</h4>

The RType syntax is inspired by Python and to a lesser degree YAML. Here are 
some guideliens:

* All element and attribute names are camelCased (first word is lowercased, 
subsequent words are capitalized). Note: the XML representations for the
elements and attribues are PascalCased (all words capitalized).

* Attributes may occur on the same line as the element or on subsequent lines
indented at least one level deeper than the element.

* Indentation is also used to indicate child elements.

* Quotes are only required around values that have spaces in them. 

* Comments begin with a hash mark (#). Note: there is no multi-line comment
construct.

* Default attributes must occur immediately after the element and do not
require the attribute name to be specified.

The following is an example of RType using a fictional lexicon:

      # This is a comment
      foo first=One
          second="Second Attribute" # trailing comment
        nestedElement type="Child of foo"

      bar camelCasedAttribute=nospacesnoquotes
          #tempCommentedOut=true
          trailing="This is an attribute on bar."

      # The following two lines of code are equivalent if
      # the File element's Name attribute is defined as the
      # "default" attribute. This can lead to very concise
      # source code.
      file default.exe
      file name=default.exe

The RType syntax is used for the majority of the examples in this documentation.

<h4 id="filesystemsyntax">File System Syntax</h4>

Files are arguably the most important part of package. If not the most important
then files are certainly the most prevalent. Traditional WiX requires the
developer to define a heirarchy of Directory and Feature elements to contain 
Component elements that finally define where files get installed. In Simplified
WiX, files can be defined just about anywhere with a full or relative path. Once
defined a files is referenced by the same path.

Let's contrast a couple code fragments required to define an executable and
file assocation in Traditional WiX vs. Simplified WiX. These are not complete
examples but should demontrate the differences.

Traditional WiX:

      <Directory Id="TARGETDIR" Source="SourceDir">
        <Directory Id="ProgramFilesFolder">
          <Directory Id="CompanyFolder" Name="Contoso">
            <Directory Id="INSTALLFOLDER" Name="Application">
              <Directory Id="BinFolder" Name="bin">
                <Component>
                  <File Name="app.exe" />
                  <ProgId Id="Contoso.File.Extension.1" Description="My Extension">
                    <Extension Id="ext" ContentType="text/xml"/>
                  </ProgId>
                </Component>
              </Directory>
            </Directory>
          </Directory>
        </Directory>
      </Directory>

Simplified WiX:

      file bin\app.exe
      fileAssociation implementation=Contoso.File.Extension.1
                      displayName="My Extension"
        fileType extension=ext contentType=text/xml

The Simplified WiX provides two major savings. First, files with a relative
path are rooted in `InstallFolder` (see next section). Second, folders can
be defined inline with the `file` element. If there are many files in a folder
then it may be easier to define an explicit `folder` element. For example:

      folder bin
        file app.exe
        file app.exe.config
        file data1.ext
        file data2.ext
        file images\pkg.jpg
        file images\app.png

Notice that `file` elements under `folder` elements can have inline folders.
The result is four files in the "bin" folder and two files in the "bin\images"
folder.

To reference the files, use their path. For example, if we wanted to use the
"pkg.jpg" from the example above as the `package` element's `image` attribute:

      package name=Contso.App version=1 manufacturer=Contoso
              displayName="Contoso's App" description="App from Contoso."
              image=bin\images\pkg.jpg


<h4 id="wellknownfolders">Well-known Folders</h4>

Simplified WiX has several well-known folders. They can be referenced by placing
a colon at the end of the name. For example, `InstallFolder:`.

* `ApplicationFolder` (deprecated) - use `InstallFolder` insteaad.

* `DesktopFolder` - the desktop. This folder should rarely be used.

* `InstallFolder` - operating system recommended install location for
application files. This is the default `folder` when not specified and should
be where most files are rooted.

* `ProgramMenuFolder` - the Programs folder in the Start Menu where shortcuts
are often installed.


<h3 id="reference">Reference</h3>


<h4 id="package">Package</h4>

The `package` element contains the information that identifies the resulting
MSI or AppX file. It also has display information for the end user to
recognize the package and its manufacturer.

Parent: none

Children: none

Attributes:

* `name` - unique name for the package. Typically namespaced by the company
name. For example, `name=Microsoft.Wix.Toolset`

* `version` - multi-part version of the package. Note that AppX and VSIX packages
may use four part versions but MSI packages only support the first three parts.
For example, `version=1.2.3`

* `manufacturer` - human readable company name that creates the package.

* `publisher` - subject identifier of the certificate used to sign the package.
This attribute is optional. If the signing certificate file is provided via the
command-line, the compiler will set this attribute automatically. For example,
`publisher="CN=Microsoft\O=Microsoft Corp.\L=Redmond\S=WA\C=US"`

* `displayName` - human readable name for the package. For example,
`displayName="The WiX Toolset"`

* `description` - human readable description for the package. For example,
`description="Set of tools used to create MSI and AppX packages."`

* `framework` - boolean value indicating whether the package is a framework.
In AppX packages this creates a Framework package and in MSI packages this
sets ARPSYSTEMCOMPONENT=1 so the package will not show up in Add/Remove Programs.
For example, `framework=true`

* `image` - reference to a `file` that will be displayed to the end user. For
example, `image=wixlogo.png`


<h4 id="application">Application</h4>

The `application` element defines an `Application` in an AppX Package and a 
`Feature` in an MSI package.

Parent: none

Children: almost all

Attributes:

* `name` - programmatic identifier of application. In AppX packages this is the
ACID and in MSI packages this is a ProgId. For example, `name=Microsoft.Wix.Compiler.1.0`

* `displayName` - human readable name for the application. For example,
`displayName="WiX Toolset Compiler"`

* `logo` (deprecated) - use the [`appx.tile`](#appxtile) `image` attribute instead.

* `smallLogo` (deprecated) - use the [`appx.tile`](#appxtile) `smallImage` attribute instead.

* `file` - reference to a `file` that serves as the target of the application.
For example, `file=candle.exe`

Extension attributes:

* `appx.application.initialRotationPreference` (deprecated) use the `appx.initialRotation`
element instead.

* `appx.application.toastCapable` (optional) - boolean value indicating whether
the application is allowed to fire toast notifications. For example, 
`appx.application.toastCapable=true`


<h4 id="class">Class</h4>

The `class` element defines the classes in an in process or out of process server.

Parent: `inprocServer` or `outprocServer`

Children: `classAttribute`

Attributes:

* `id` - identifier for the class. In AppX Packages this is the activatable class
name and in MSI Packages this is the CLSID. For example,
`id=Microsoft.Wix.Compiler.Class`

* `threadModel` (optional) - specifies the threading model for in process servers.
This attribute is required for in process servers and not valid for out of process
servers. For example, `threadingModel=both`.

  There are three allowed values:
    * `sta` - single threaded apartment
    * `mta` - multi-threaded apartment
    * `both`
    * `neutral` - only supported by MSI based-packages.


<h4 id="classattribute">ClassAttribute</h4>

The `classAttribute` element registers extra data for the parent `class` element.

Parent: `class`

Children: none

Attributes:

* `name` - name of the class attribute. For example, `name=Programmable`

* `value` - specifies the value for the class attribute. For example,
`value=1`

* `type` (optional) - explicitly types the class attribute. Class
attributes support two types: `integer` or `string`. The compiler
will automatically determine the type from the value. Thus this
attribute is only necessary if you want to force an integer to be
a string. For example, `type=string`


<h4 id="dependency">Dependency</h4>

The `dependency` element defines a required relationship to another package. In
AppX packages this creates a `Dependency` element. In MSI packages this uses the
WiX `Requires` extension to create a dependency on the specified package.

Parent: none

Children: none

Attributes:

* `name` - name of the required package. For example, `name=Microsoft.Wix.Compiler`

* `publisher` (optional) - subject identifier of the certificate used to sign the
required package.  For example, `publisher="CN=Microsoft\O=Microsoft Corp.\L=Redmond\S=WA\C=US"`

* `version` - minimum version of the required package. For example, `version=1.1`

* `maxVersion` (optional) - maximum version of the required package to satisfy the dependency. Note that AppX packages do not support specifying a maximum version. For example, `maxVersion=1.1`

* `minVersion` (deprecated) - use the `version` attribute instead.


<h4 id="file">File</h4>

The `file` element defines a file to include the package, where to install
the file and where to find the file when creating the package.

Parent: none or `application` or `folder`

Children: none

Attributes:

* `name` (default) (optional) - name of the file to install. The `name` may
contain a relative path which indicates the file is installed to a nested
folder. If the `name` attribute is not defined, the `source` attribute must be
present with a file name. For example, `name=bin\candle.exe`

* `parentFolder` (optional) - reference to a `folder` that is . If this
attribute is not specified, the `file` defaults its `parentFolder` to 
`ApplicationFolder`. For example, `parentFolder=DesktopFolder:\MyBadApp` is not
recommended. :)

* `source` (optional) - path to the file on the build machine. If the `source`
ends with a backslash the `name` attribute is appended to the source path. If
the `name` is absent, the `source` must specify a file name. For example,
`source="relative\path to\"`

Extension attributes:

* `msi.ngen.*` (optional) - see the [`msi.ngen`](#msingen) extension for details.


<h4 id="fileassociation">FileAssociation</h4>

The 'fileAssociation' element defines a file association. A file association is
a collection one or more file types (also often referred to as "file extensions")
that register a file extension (i.e. ".wxs") with the operating system.

Parent: `application`

Children: `fileType`

Attributes:

* `name` - programmatic identifier for the file association. For example,
`name="wixdocuments"`

* `alwaysUnsafe` (optional) - boolean value that indicates the file association
can never be opened from the internet without prompt. If the attribute is not
specified, the value is defaulted to `false`. For example, `alwaysUnsafe=true`

* `description` (optional) - human readable description for the file association.
This attribute is optional. For example, `description="Source Files used by the WiX Toolset"`

* `displayName` (optional) - human readable name for the file association.
This attribute is optional. For example, `displayName="WiX Toolset Source File"`

* `image` (optional) - reference to a `file` that is the image displayed by the
file association. For example, `image=wixlogo.png`

* `openIsSafe` (optional) - boolean value that indicates the file association
can be opened from the internet without prompt. If the attribute is not
specified, the value is defaulted to `false`. For example, `openIsSafe=true`

* `implementation` (optional) - the ACID or ProgId that implements the file
association. If this attribute is not specified, the `application.name` is used by
default. For example, `implemenation=Microsoft.Wix.Votive.1.0`


<h4 id="filetype">FileType</h4>

The 'fileType' element defines a file type for a file association. File types are
also often referred to as "file extensions" because they register a file
extension (i.e. ".wxs") with the operating system.

Parent: `fileAssociation`

Children: none

Attributes:

* `extension` - the file extension with or without the "." separator. For
example, `extension=.wxs`

* `contentType` (optional) - MIME type associated with the file type. For
example, `contentType=text\xml`

* `mediaType` (deprecated) - use the `contentType` attribute instead.


<h4 id="folder">Folder</h4>

The 'folder' element defines a file system folder also known as a directory.

Parent: none or `application` or `folder`

Children: `file`

Attributes:

* `name` (optional) - name of the folder to install. The `name` may
contain a relative path which indicates the folder is installed to a
nested folder. For example, `name=InstallFolder:bin\`

* `parentFolder` (optional) - reference to a `folder` that is . If this
attribute is not specified, the `folder` defaults its `parentFolder` to 
`ApplicationFolder`. For example, `parentFolder=DesktopFolder:\MyCompany`


<h4 id="gameexplorer">GameExplorer</h4>

The `gameExplorer` element registers the package with the operating system
Game Explorer.

Parent: none

Children: none

Attributes:

* `file` - reference to `file` that is the "Game Definition Container" to
register with the game explorer. For example, `file=bin\game.exe`


<h4 id="inprocserver">InprocServer</h4>

The `inprocServer` element registers a file for in process activation. In
AppX Packages this creates one or more activatable classes. In MSI Packages
this creates one or more InprocServer32 CLSIDs.

Parent: none or `application`

Children: `class`

Attributes:

* `file` - reference to `file` that is an in process server. For example,
`file=bin\wix.dll`


<h4 id="interface">Interface</h4>

The `interface` element defines the interfaces exposed by a proxy stub.

Parent: `proxyStub`

Children: none

Attributes:

* `name` - name for the interface. In AppX Packages this is the activatable class
name and in MSI Packages this is the registered name of the interface. For example,
`id=Microsoft.Wix.Compiler.Class`

* `guid` - GUID for the interface. For example,
`guid=12345678-1234-1234-1234-1234567890AB`


<h4 id="outprocserver">OutprocServer</h4>

The `outprocServer` element registers a file for out of process activation.
In AppX Packages this creates one or more activatable classes. In MSI Packages
this creates one or more LocalServer32 CLSIDs.

Parent: none or `application`

Children: `class`

Attributes:

* `file` - reference to `file` that is out of process server. For example,
`file=bin\candle.exe`

* `name` - name of the out of process server. For example,
`file=WixCompilerServer`

* `arguments` (optional) - specifies the command line arguments to provide
the out of process server when starting it. For example,
`arguments="-foo bar"`

* `instance` (optional) - defines whether the out of process server creates
an instance for each activation. Allowed values are `single` and `multiple`.
If this attribute is not specifed, the default is single instance activation.
For example, `instance=multiple`


<h4 id="protocol">Protocol</h4>

The `protocol` element specifies that application supports a communication
protocol. One of the most popular protocols is "http" but "mailto" might also
be familiar.

Parent: `application`

Children: none

Attributes:

* `name` - name of the protocol. For example, `name=mailto`

* `displayName` - human readable name for the protocol. For example, 
`displayName="Launch E-mail program"`

* `image` (optional) - reference to `file` that is the image displayed with
the protocol. For example, `image=envelope.png`

* `implementation` (optional) - the ACID or ProgId that implements the protocol.
If this attribute is not specified, the `application.name` is used by default.
For example, `implemenation=Microsoft.Wix.SomeId.1.0`


<h4 id="proxystub">ProxyStub</h4>

The `proxyStub` element registers a file as proxy to an out of process server.
In AppX Packages this creates proxy stub with one or more interfaces. In MSI Packages
this creates an InprocServer32 proxy stub with one or more interfaces.

Parent: none or `application`

Children: `interface`

Attributes:

* `file` - reference to `file` that is the proxy stub. For example, 
`file=bin\wix.dll`

* `guid` - GUID for the proxy stub. For example, 
`guid=12345678-1234-1234-1234-1234567890AB`


<h3 id="appxreference">AppX Reference</h3>

The following elements are provided in the `appx` namespace and are only 
processed when creating an AppX package.

<h4 id="appxapplicationextensionactivationattributes">AppX Application
Extension Activation Attributes</h4>

Extension points, or just extensions, are systems in the operating system that
applications can register with to extend the operating system behavior.

AppX introduces a new way to register extension points with the operating system
that supports native, managed and web applications. Native and managed
applications register activatible class identifiers (aka: ACIDs) using the
`inprocServer` and `outprocServer` elements then assocate extension points with
those ACIDs. Web applications do not register ACIDs but instead point the
extension points at web pages installed by the application.

The following three attributes may be specified on any _application_ extension
points (aka: elements that parent to an `application` element). By default,
an application extension point will be registered to the `file` on the parent
`application` element. These attributes should only be used to override the
default.

* `file` (optional) - reference to a `file` element that overrides the default
executable specified by the `application` element's `file` attribute. If this
attribute references an executable (the file's extension ends in ".exe") the
`implementation` attribute MAY be specified as well. If the file references a
web page (the file's extension ends in anything not ".exe") then the
`implementation`  and `runtimeType` attributes CANNOT be specified. For example,
`file=path\to\srcsearch.exe`

* `implementation` (optional) - references an class id registered by the 
`inprocServer` or `outprocServer` elements that implements the extension. This
attribute may only be specified if the `file` attribute is an executable. If
this attribute is not specified the default is the parent `application.name`.
For example, `implementation=Wix.Votive.SearchSourceCode`

* `runtimeType` (optional) - specifies the runtime provider for an application.
It is rarely necessary to specify this attribute. The attribute is most often
required when using mixed frameworks with native applications.


<h4 id="appxaccountpictureprovider">AccountPictureProvider</h4>

The `appx.accountPictureProvider` element registers the application to provide
user identity to the operating system.

Parent: `application`

Children: none

Attributes:

* See the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxadditionaltask">AdditionalTask</h4>

The `appx.additionalTask` element allows a `appx.backgroundTask` to register
more than one task.

Parent: `appx.backgroundTask`

Children: none

Attributes

* `name` - specifies the additional task to run in the background. See the
`appx.backgroundTask` element for a list of valid values. For example,
`name=timer`


<h4 id="appxbackgroundtask">BackgroundTask</h4>

The `appx.backgroundTask` element allows applications to continue running a
process even when suspended. A background task may support multiple tasks by
adding child [`appx.additionalTask`](#appxadditionaltask) elements.

Parent: `application`

Children: `appx.additionalTask`

Attributes:

* `name` - specifies the task to run in the background. See the list below for
valid values. For example, `name=audio`

  The following is a list of tasks supported by background tasks in AppX:
    * `audio`
    * `controlChannel`
    * `pushNotification`
    * `systemEvent`
    * `timer`

* `serverName` (optional) - indicates the WinRT server instance for the
background tasks and 3rd party classes are the same instance, thus ensuring
only one instance of the server will exist at runtime regardless of how it was
activated. For example, `serverName=BackgroundTaskExampleServer`

* See also the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxcamerasettings">CameraSettings</h4>

The `appx.cameraSettings` element registers the application as a control applet
for camera devices.

Parent: `application`

Children: none

Attributes:

* See the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxcapability">Capability</h4>

The `appx.capability` element defines the operating system capabilities an AppX
package requires.

Parent: none

Children: none

Attributes:

* `name` - name of the required capability. For example, `name=musicLibrary`

  The following is a list of capabilites supported by AppX packages:
    * `defaultWindowsCredentials` - (deprecated) use `enterpriseAuthentication`
    * `enterpriseAuthentication`
    * `documentsLibrary`
    * `internetClient`
    * `internetClientServer`
    * `musicLibrary`
    * `picturesLibrary`
    * `privateNetworkClientServer`
    * `removeableStorage`
    * `sharedUserCertificates`
    * `videosLibrary`


<h4 id="appxcontactpicker">ContactPicker</h4>

The `appx.contactPicker` element indicates the application provides contact
information for other applications.

Parent: `application`

Children: none

Attributes:

* See the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxcontenturi">ContentUri</h4>

The `appx.contenturi` element defines URIs the application considers part of
itself. If a windows web application navigates to a URL outside of not included
by the `appx.contenturi` elements the default browser will be launched.

Parent: none

Children: none

Attributes:

* `match` - the URI to include or exclude from the application. Must be an
absolute URL. For example, `match=http://wixtoolset.org`

* `rule` - indicates whether the URI specified by the `match` attribute is 
included or excluded from the applications access list. Allowed values are
`include` or `exclude`. For example, `rule=include`


<h4 id="appxdataformat">DataFormat</h4>

The `appx.dataFormat` element defines the types of data supported by a specific
`appx.sendTarget` or `appx.shareTarget` element.

Parent: `appx.sendTarget` or `appx.shareTarget`

Children: none

Attributes:

* `name` - the name of the supported data format. At this time, there are no
well known values so any string is accepted. For example, `name=url`


<h4 id="appxdevicecapability">DeviceCapability</h4>

The `appx.devicecapability` element defines the device capability the AppX
package requires.

Parent: none

Children: none

Attributes:

* `name` - name or GUID of the required device. For example, `name=webcam`

  The following is a list of well known device constants supported by AppX packages:
    * `microphone`
    * `webcam`
    * `location`
    * `phoneSms`
    * `pcSms`
    * `nearFieldProximity`


<h4 id="appxfileextension">FileExtension</h4>

The `appx.fileExtension` element indicates the file extension supported by a
`appx.filePicker`, `appx.sendTarget` or `appx.shareTarget` element. This element
will not register file types with the operating system. To register a file type
see the `fileAssociation` element.

Parent: `appx.filePicker` or `appx.sendTarget` or `appx.shareTarget`

Children: none

Attributes:

* `name` - the name of the supported file extension. If the `name` does not
begin with a dot one will be added automatically. For example, `name=.wxs`


<h4 id="appxfilepicker">FilePicker</h4>

(deprecated) Use the `appx.fileOpenPicker` element to indicate the application
provides file and items that can be represented by files that other applications
may select.

Parent: `application`

Children: `appx.fileExtension`

Attributes:

* `supportsAllFileExtensions` - (deprecated) use an `appx.fileExtension` where
the `name` attribute is `*`.

* See also the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxfileopenpicker">FileOpenPicker</h4>

The `appx.fileOpenPicker` element indicates the application provides file and
items that can be represented by files that other applications may select.

Parent: `application`

Children: `appx.fileExtension`

Attributes:

* See the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxfilesavepicker">FileSavePicker</h4>

The `appx.fileSavePicker` element indicates the application accepts file and
items that can be represented by files that other applications may save.

Parent: `application`

Children: `appx.fileExtension`

Attributes:

* See the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxinitialrotation">InitialRotation</h4>

The `appx.initialRotation` element defines the orientations that application
prefers to be launched. The application may override this value programmatically.

Parent: `application`

Children: none

Attributes:

* `preference` - specifies whether the application prefers to be launched in
portrait or landscape orientation. For example, `appx.initialRotation preference=landscapeFlipped`

  The following is a list of rotation preferences supported by AppX packages:
    * `landscape`
    * `landscapeFlipped`
    * `portrait`
    * `portraitFlipped`

<h4 id="appxlockscreen">LockScreen</h4>

The `appx.lockScreen` element defines what the application notifications may be
displayed when the device is locked.

Parent: `application`

Children: none

Attributes:

* `notification` - defines how notifications from the application appear on
the lock screen. For example, `notification=image`

  The following is a list of notification constants supported by AppX packages:
    * `image`
    * `imageAndTileText`
    * `tileText` - (deprecated)

* `image` (optional) - reference to a `file` that is the image displayed beside
the application's notifications on the lock screen. This attribute is ignored
if the `notification` attribute is set to `tileText`. The image must be 24 x 24
at 1x scale. For example, `image=wixlock.png`


<h4 id="appxprintersettings">PrinterSettings</h4>

The `appx.printerSettings` element registers the application as a control applet
for printers.

Parent: `application`

Children: none

Attributes:

* See the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxsearch">Search</h4>

The `appx.search` element indicates the application has content that may be
searched by other applications.

Parent: `application`

Children: none

Attributes:

* See the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxsendtarget">SendTarget</h4>

(deprecated) The `appx.sendTarget` element indicates the application accepts other applications
sending files and other data types to it. This functionality is very similar to
the "Send to" functionality found by right clicking on a file in Windows
explorer.

Parent: `application`

Children: `appx.fileExtension` and `appx.dataFormat`

Attributes:

* `supportsAllFileExtensions` - (deprecated) use an `appx.fileExtension` where
the `name` attribute is `*`.

* See also the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxsharetarget">ShareTarget</h4>

The `appx.shareTarget` element indicates the application accepts other applications
sharing files and other data types to it.

Parent: `application`

Children: `appx.fileExtension` and `appx.dataFormat`

Attributes:

* `supportsAllFileExtensions` - (deprecated) use an `appx.fileExtension` where
the `name` attribute is `*`.

* See also the [AppX Application Extension Activation Attributes](#appxapplicationextensionactivationattributes)


<h4 id="appxsplashscreen">SplashScreen</h4>

The `appx.splashScreen` element defines what visuals are displayed while the
application loads. Every `application` in an AppX package must have one and
only one `appx.splashScreen` element.

Parent: `application`

Children: none

Attributes:

* `image` - reference to a `file` that is the image displayed while the 
application loads. The image must be 64 x 304 at 1x scale. For example,
`image=wixsplash.png`

* `background` (optional) - color for the background of the splash screen.
The color may be defined as a hexidecimal rgb sextuplet or a color constant.
For example, `background=000000`

  The following is a list of color constants supported by AppX packages:
    * `black`
    * `silver`
    * `gray`
    * `white`
    * `maroon`
    * `red`
    * `purple`
    * `fuchsia`
    * `green`
    * `lime`
    * `olive`
    * `yellow`
    * `navy`
    * `blue`
    * `teal`
    * `aqua`


<h4 id="appxtile">Tile</h4>

The `appx.tile` element is the new shortcut for AppX Applications. Every
`application` in an AppX package must have one and only one `appx.tile`
element.

Parent: `application`

Children: none

Attributes:

* `background` - color for the background of the tile. The color may be
defined as a hexidecimal rgb sextuplet or a color constant. For example,
`background=000000`

  The following is a list of color constants supported by AppX packages:
    * `black`
    * `silver`
    * `gray`
    * `white`
    * `maroon`
    * `red`
    * `purple`
    * `fuchsia`
    * `green`
    * `lime`
    * `olive`
    * `yellow`
    * `navy`
    * `blue`
    * `teal`
    * `aqua`

* `foreground` - the foreground of the tile defines the text color. There
are only two options: `light` or `dark`. For example, `foreground=dark`

* `image` - reference to a `file` that is the default image displayed as
the application. The image size must be 152 x 152 at 1x scale. For example,
`image=wixlogo.png`

* `smallImage` - reference to a `file` that is the small image typically
displayed when the full size image is too large. The image must be 32 x 32 at
1x scale. For example, `smallImage=wixlogo_small.png`

* `shortName` (optional) - name of the application to display on the tile.
The name must be no longer than 13 characters. That is why it is called the 
"short name". For example, `shortName=WiX`

* `showName` (optional) - indicates when the name should be displayed when
the tile changes from the default state. If the attribute is not
specified, the value is defaulted to `noLogos`. For example, `showName=allLogos`

  The following is a list of options:
    * `allLogos` - always show the name on tile.
    * `noLogos` - never show the name on the tile.
    * `logoOnly` - show only when the default tile is displayed.
    * `wideLogoOnly` - show only when the wide tile is displayed.


* `wideImage` (optional) - reference to a `file` that is the optional wide
image displayed when the application is widened by the user. The image size
must be 312 x 152 at 1x scale. For example, `wideImage=wixlogo_wide.png`


<h3 id="msireference">Msi Reference</h3>

The following elements are provided in the `msi` namespace and are only
processed when creating an MSI package or WiX package.


<h4 id="msifilesearch">FileSearch</h4>

The `msi.fileSearch` element returns a `file` reference by searching the computer
during the installation. The `id` of the `msi.fileSearch` may be used in many
places where a `file` reference is required.

Parent: none

Children: none

Attributes:

* `id` - required on `msi.fileSearch` elements.

* `component` (optional) - specifies the GUID of an MSI Component with a File
KeyPath to search. This attribute cannot be specified if a `registry` attribute
is provided. For example, `component=12345678-1234-1234-1234-1234567890AB`

* `registry` (optional) - specifies the registry key to search for a path to a file. If the
attribute ends in a backslash, "\", then the default value of the registry key is
searched. This attribute cannot be specified if a `component` attribute
is provided. For example, `registry=HKLM\Some\Registry\Key\Name`


<h4 id="msingen">Ngen</h4>

The `msi.ngen` extends the `file` element to indicate a managed assembly should be
processed during install to create a native image executable.

Extends: `file`

Attributes:

* `execute` - defines when the native image will be generated. This attribute is
required for an assembly to be processed by Ngen. For example, `file name=wix.dll msi.ngen.execute=idle`

  The following is a list of execute options:
    * `asynchronous` - generate without blocking the install.
    * `idle` - recommended, generate when the computer is idle.
    * `immediate` - block the install until all native images are generated.

* `application` (optional) - reference to an executable `file` with a ".exe.config".
The file's .exe.config specifies how to find dependencies for the managed .dll
assembly that is to be processed by Ngen. This attribute is not normally needed and
is not valid in conjunction with the `folder` attribute.
For example, `file name=wix.dll msi.ngen.application=path\to\candle.exe`

* `folder` (optional) - reference to a `folder` where a managed .dll assembly's
dependencies can be found. By default Ngen will look for depencencies in the same
folder as the assembly. This attribute is not normally needed and is not valid
in conjunction with the `application` attribute.
For example, `file name=wix.dll msi.ngen.folder=InstallFolder:\bin`


<h4 id="msiproperty">Property</h4>

The `msi.property` element defines a MSI Property for use in a Simplified WiX
file. A `msi.property` may be used in many cases where a `file` reference is
required.

Parent: none

Children: none

Attributes:

* `id` - required on `msi.property` elements.

* `external` (optional) - indicates that the MSI Property is defined externally.
This allows the Simplified WiX file to use a Property defined by a Traditional
WiX file. For example, `external=true`


<h3 id="extensibility">Extensibility</h3>

The Tradition WiX toolset has a rich extensiblity model that allows for
ultimate flexibility. As its name would suggest, Simplified WiX provides
a very limited extensibility to keep the system simple.

All Simplified WiX extensions inherit from the `CompilerExtension` and
should use the `DefaultCompilerExtensionAttribute` to define the extension's
class name.


<h4 id="compilerextension">Compiler Extension</h4>

All compiler extensions inherit from the `CompilerExtension`. The
following is an example compiler extension that displays a message
but does not provide a custom file manager:

    using WixToolset.Simplified;

    [assembly: DefaultCompilerExtension(typeof(TestSwixExtension.TestSwixCompilerExtension))]
    
    namespace TestSwixExtension
    {
        public class TestSwixCompilerExtension : CompilerExtension
        {
            public override CompilerFileManager FileManager
            {
                get
                {
                    this.OnMessage(new CompilerMessageEventArgs(CompilerMessageType.Information, 5000,
                                   null, 0, 0, "TODO: create and return a CompilerFileManager object."));

                    return null;
                }
            }
        }
    }


<h4 id="filemanager">Compiler File Manager</h4>

The file manager in a compiler extension allows complete control over
how the compiler resolves external files. The `source` attribute on a
`file` element is provided to the file manager and the file manager is
expected to return a path to a file on disk. All file managers must
inherit from `CompilerFileManager`.
