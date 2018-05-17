# Changes in FBX SDK C# Bindings

**Version**: 1.5.0-preview

* Added support for physical camera attributes

**Version**: 1.4.0-preview

* First version accessible via Package Manager
* Update to FBX SDK 2018.1.1
* Add bindings for constraints: `FbxConstraint`, `FbxConstraintParent`, `FbxConstraintAim`, and related functions
* Reduced binary size on Mac (which also shrinks the package for everyone)

**Version**: 1.3.0a1

Fix Universal Windows Platform build error caused by UnityFbxSdk.dll being set as compatible with any platform instead of editor only.

**Version**: sprint43

Add bindings for FbxAnimCurveFilterUnroll

Add binding for FbxGlobalSettings SetTimeMode to set frame rate

**Version**: 1.2.0b1

Update version number

Replace meta files with meta files from release 1.0.0b1 for backwards compatibility

**Version**: sprint36

Expose bindings to set FbxNode's transformation inherit type

**Version**: sprint35

Add binding for FbxCamera's FieldOfView property

**Version**: 1.0.0b1

Enforce FbxSdk DLL only works with Unity 2017.1+

**Version**: 0.0.14a
Note: skipping some versions so that FbxSdk package version matches FbxExporter package version

Added FbxObject::GetScene

**Version**: 0.0.10a

Added documentation of vector classes.

Added test to check that the FbxSdk DLL cannot be used without the Unity Editor (This is a legal requirement).

Improve build process so it is more robust.

**Version**: 0.0.9a

Set the Doxygen homepage to be README.txt instead of README.md

Rename namespace to `Unity.FbxSdk`

Rename `FbxSharp.dll` and `fbxsdk_csharp` libaries to `UnityFbxSdk.dll` and `UnityFbxSdkNative` respectively

Change documentation title to "Unity FBXSDK C# API Reference"

Package zip file containing Doxygen documentation

Update license in README to Autodesk license

**Version**: 0.0.8a

Updated LICENCSE.txt to include Autodesk license

Use .bundle on Mac instead of .so for shared libraries

Ship bindings as binaries without source

**Version**: 0.0.7a
Note: skipping version 0.0.6a so that FbxSdk package version matches FbxExporter package version

Add bindings for FbxIOFileHeaderInfo. 
  - Exposed mCreator and mFileVersion as read-only attributes.

Made it easier for performance tests to pass.

**Version**: 0.0.5a

Added Doxygen documentation generation for C# bindings.
