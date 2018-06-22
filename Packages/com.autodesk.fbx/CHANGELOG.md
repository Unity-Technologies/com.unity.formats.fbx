# Changes in FBX SDK C# Bindings

## [2.0.0] - 2018-06-22

NEW FEATURES
* The C# Bindings package has been renamed to com.autodesk.fbx
* The Autodesk.Fbx assembly can now be used in standalone builds (runtime)
* Added support for physical camera attributes
* Added support for constraints: FbxConstraint, FbxConstraintParent, FbxConstraintAim, and related methods
* Updated to FBX SDK 2018.1

KNOWN ISSUES
* The FBX SDK C# Bindings package is not supported if you build using the IL2CPP backend.

## [1.3.0f1] - 2018-04-17

NEW FEATURES
* Added bindings for FbxAnimCurveFilterUnroll
* Added binding for FbxGlobalSettings SetTimeMode to set frame rate
* Exposed bindings to set FbxNode's transformation inherit type
* Added binding for FbxCamera's FieldOfView property
* Added FbxObject::GetScene
* Added bindings for FbxIOFileHeaderInfo. 
* Exposed mCreator and mFileVersion as read-only attributes.

FIXES
* Fix Universal Windows Platform build error caused by UnityFbxSdk.dll being set as compatible with any platform instead of editor only.
* Enforced FbxSdk DLL only works with Unity 2017.1+
