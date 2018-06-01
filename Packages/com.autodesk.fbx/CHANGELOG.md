# Changes in FBX SDK C# Bindings

## [1.7.0] - 2018-06-01

FEATURE

* The C# Bindings package has been renamed to com.autodesk.fbx
* Fixed violations of the C# Framework Design Guidelines (FDG)
* Fixed errors reported while running the Package Validation Suite

## [1.6.0] - 2018-05-29

* The fbxsdk package can now be used in standalone builds (runtime)

## [1.5.0]

* Added support for physical camera attributes

## [1.4.0]

* First version accessible via Package Manager
* Update to FBX SDK 2018.1.1
* Add bindings for constraints: `FbxConstraint`, `FbxConstraintParent`, `FbxConstraintAim`, and related functions
* Reduced binary size on Mac (which also shrinks the package for everyone)

## [1.3.0a1]

Fix Universal Windows Platform build error caused by UnityFbxSdk.dll being set as compatible with any platform instead of editor only.

## [sprint43]

Add bindings for FbxAnimCurveFilterUnroll

Add binding for FbxGlobalSettings SetTimeMode to set frame rate

## [1.2.0b1]

Update version number

Replace meta files with meta files from release 1.0.0b1 for backwards compatibility

## [sprint36]

Expose bindings to set FbxNode's transformation inherit type

## [sprint35]

Add binding for FbxCamera's FieldOfView property

## [1.0.0b1]

Enforce FbxSdk DLL only works with Unity 2017.1+

## [0.0.14a]
Note: skipping some versions so that FbxSdk package version matches FbxExporter package version

Added FbxObject::GetScene

## [0.0.10a]

Added documentation of vector classes.

Added test to check that the FbxSdk DLL cannot be used without the Unity Editor (This is a legal requirement).

Improve build process so it is more robust.

## [0.0.9a]

Set the Doxygen homepage to be README.txt instead of README.md

Rename namespace to `Unity.FbxSdk`

Rename `FbxSharp.dll` and `fbxsdk_csharp` libaries to `UnityFbxSdk.dll` and `UnityFbxSdkNative` respectively

Change documentation title to "Unity FBXSDK C# API Reference"

Package zip file containing Doxygen documentation

Update license in README to Autodesk license

## [0.0.8a]

Updated LICENCSE.txt to include Autodesk license

Use .bundle on Mac instead of .so for shared libraries

Ship bindings as binaries without source

## [0.0.7a]
Note: skipping version 0.0.6a so that FbxSdk package version matches FbxExporter package version

Add bindings for FbxIOFileHeaderInfo. 
  - Exposed mCreator and mFileVersion as read-only attributes.

Made it easier for performance tests to pass.

## [0.0.5a]

Added Doxygen documentation generation for C# bindings.
